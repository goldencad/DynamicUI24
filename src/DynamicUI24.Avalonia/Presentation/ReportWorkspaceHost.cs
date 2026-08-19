using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using System.Diagnostics;
using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.ModernWorkspace;
using DynamicUI24.Core.Reports;
using DynamicUI24.Avalonia.Presentation.Editors;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Thin native-input presentation over the platform-free report coordinator and shared grid host.</summary>
public sealed class ReportWorkspaceHost : UserControl
{
    public sealed record ParameterInteractionTiming(bool IsOpening, TimeSpan VisibilityApplied,
        TimeSpan LayoutCompleted, TimeSpan RenderCompleted, int ProviderRequestsBefore,
        int ProviderRequestsAfter, int MaterializedRowsBefore, int MaterializedRowsAfter,
        long GenerationBefore, long GenerationAfter);
    private readonly ReportRuntime runtime;
    private readonly ILocalizationService localization;
    private readonly Func<ReportExecutionContext> context;
    private readonly DataEntryGridHost grid;
    private readonly WorkspacePaneSessionStateStore paneState;
    private readonly WorkspaceCode workspaceCode;
    private readonly PaneDefinition parameterPaneDefinition = new(new("PARAMETERS"), PaneRole.LeftNavigation,
        defaultSize: 300, minSize: 220, maxSize: 520, canCollapse: true, canResize: true,
        helpContextCode: "REPORT.PARAMETERS");
    private PaneRuntimeState parameterPaneState;
    private readonly StackPanel parameterFields = new() { Spacing = 8 };
    private readonly Border parameterPanel = new() { Padding = new Thickness(12) };
    private readonly TextBlock title = new() { FontSize = 24 };
    private readonly TextBlock subtitle = new() { Opacity = .72 };
    private readonly TextBlock state = new();
    private readonly TextBlock totals = new();
    private readonly TextBlock commandFeedback = new() { Opacity = .78 };
    private readonly Button toggle = new();
    private readonly ComboBox outputFormat = new();
    private readonly Dictionary<ReportParameterCode, TextBlock> parameterLabels = [];
    private readonly Dictionary<ReportParameterCode, AvaloniaEditorPresenter> parameterPresenters = [];
    private TimeSpan parameterConstruction;
    private readonly List<ParameterInteractionTiming> parameterInteractionHistory = [];
    private int parameterLayoutTransition;
    private int completedParameterLayoutTransition;
    private int workspaceBuildCount;
    private int parameterControlBuildCount;
    private readonly IActionCommandRegistry? commandRegistry;
    private readonly Func<ActionCommandExecutionContext>? commandContext;
    private readonly Func<CancellationToken, ValueTask<ReportAuthorizationSnapshot?>>? authorization;
    private readonly DynamicActionBarHost? topActionBar;
    private readonly DynamicActionBarHost? bottomActionBar;
    private readonly DynamicActionBarHost? overflowActionBar;
    private readonly ContextualToolbarHost contextualActions = new();
    private readonly DynamicActionBarResolver actionBarResolver = new();
    private ReportAuthorizationSnapshot? lastAuthorization;
    private bool commandExecuting;
    public sealed record ConstructionTiming(TimeSpan GridHost, TimeSpan ParameterControls, TimeSpan Total);

    public ReportWorkspaceHost(ReportRuntime runtime, ILocalizationService localization,
        Func<ReportExecutionContext> context, AppearancePreferenceService? appearance = null,
        WorkspacePaneSessionStateStore? paneState = null, IActionCommandRegistry? commandRegistry = null,
        Func<ActionCommandExecutionContext>? commandContext = null,
        Func<CancellationToken, ValueTask<ReportAuthorizationSnapshot?>>? authorization = null,
        ActionBarCommandDispatcher? actionDispatcher = null, IIconRegistry? icons = null)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.commandRegistry = commandRegistry;
        this.commandContext = commandContext;
        this.authorization = authorization;
        if (actionDispatcher is not null && icons is not null)
        {
            topActionBar = new(actionDispatcher, localization, icons, appearance);
            bottomActionBar = new(actionDispatcher, localization, icons, appearance);
            overflowActionBar = new(actionDispatcher, localization, icons, appearance);
            topActionBar.CommandCompleted += ActionCompleted;
            bottomActionBar.CommandCompleted += ActionCompleted;
            overflowActionBar.CommandCompleted += ActionCompleted;
        }
        this.paneState = paneState ?? new();
        workspaceCode = new($"REPORT:{runtime.Definition.ReportCode.Value}");
        if (runtime.Definition.Presentation.ParametersInitiallyCollapsed &&
            this.paneState.GetPreference(workspaceCode, parameterPaneDefinition.PaneCode) is null)
            this.paneState.SetPreference(workspaceCode, new(parameterPaneDefinition.PaneCode, Collapsed: true));
        parameterPaneState = this.paneState.Resolve(workspaceCode, parameterPaneDefinition,
            UiAuthorizationState.Enabled, capabilityAvailable: true);
        var construction = Stopwatch.StartNew();
        var gridClock = Stopwatch.StartNew();
        grid = new(runtime.Grid, localization, appearance);
        gridClock.Stop();
        runtime.Changed += (_, _) => Dispatcher.UIThread.Post(UpdatePresentation);
        localization.CultureChanged += (_, _) => RefreshLocalizedText();
        RegisterCommands();
        BuildVisualTree();
        construction.Stop();
        LastConstructionTiming = new(gridClock.Elapsed, parameterConstruction, construction.Elapsed);
    }

    public ReportRuntime Runtime => runtime;
    public DataEntryGridHost ResultGrid => grid;
    public async Task RunAsync(EffectiveAuthorizationContext? effectiveAuthorization = null, CancellationToken token = default,
        ReportAuthorizationSnapshot? reportAuthorization = null)
    {
        foreach (var (code, presenter) in parameterPresenters)
        {
            if (!await presenter.CommitAsync(token)) return;
            runtime.SetParameter(code, presenter.State.CommittedValue);
        }
        await runtime.RunAsync(context(), effectiveAuthorization, token, reportAuthorization);
    }
    public string? PresentedTitle => title.Text;
    public int ParameterEditorCount => parameterLabels.Count;
    public int MaterializedResultRowCount => runtime.Grid.Rows.Length;
    public ConstructionTiming LastConstructionTiming { get; }
    public bool AreParametersOpen => parameterPaneState.Visible && !parameterPaneState.Collapsed;
    public int WorkspaceBuildCount => workspaceBuildCount;
    public int ParameterControlBuildCount => parameterControlBuildCount;
    public object ParameterPresenterIdentity => parameterPanel;
    public string RunCommandCode => ReportCommandCodes.RunRefresh(runtime.Definition.ReportCode);
    public string ResetCommandCode => ReportCommandCodes.Reset(runtime.Definition.ReportCode);
    public string ExportCommandCode => ReportCommandCodes.Export(runtime.Definition.ReportCode);
    public string PrintCommandCode => ReportCommandCodes.Print(runtime.Definition.ReportCode);
    public string ViewOutputCommandCode => ReportCommandCodes.ViewOutput(runtime.Definition.ReportCode);
    public string CommandStatusText => commandFeedback.Text ?? string.Empty;
    public ReportActionContributions ActionContributions => ReportActionContributionAdapter.Create(runtime.Definition);
    public IReadOnlyList<ParameterInteractionTiming> ParameterInteractionHistory => parameterInteractionHistory;
    public void SetParametersOpen(bool isOpen)
    {
        if (AreParametersOpen == isOpen) return;
        var clock = Stopwatch.StartNew();
        var requestsBefore = runtime.ResultProviderRequestCount;
        var rowsBefore = runtime.Grid.Rows.Length;
        var generationBefore = runtime.Generation;
        var transition = ++parameterLayoutTransition;
        // Immediate pointer feedback: visibility changes in the command handler, before deferred layout.
        parameterPanel.IsVisible = isOpen;
        var visibilityApplied = clock.Elapsed;
        parameterPaneState = paneState.SetCollapsed(workspaceCode, parameterPaneDefinition, !isOpen,
            UiAuthorizationState.Enabled, capabilityAvailable: true);
        EventHandler? layoutHandler = null;
        void Complete(TimeSpan layoutCompleted)
        {
            if (transition != parameterLayoutTransition || completedParameterLayoutTransition >= transition) return;
            completedParameterLayoutTransition = transition;
            LayoutUpdated -= layoutHandler;
            clock.Stop();
            parameterInteractionHistory.Add(new(isOpen, visibilityApplied, layoutCompleted, clock.Elapsed,
                requestsBefore, runtime.ResultProviderRequestCount, rowsBefore, runtime.Grid.Rows.Length,
                generationBefore, runtime.Generation));
            Trace.WriteLine($"REPORT_PARAMETERS {(isOpen ? "OPEN" : "CLOSE")} " +
                $"visibility={visibilityApplied.TotalMilliseconds:F3}ms " +
                $"layout={layoutCompleted.TotalMilliseconds:F3}ms render={clock.Elapsed.TotalMilliseconds:F3}ms " +
                $"requests={requestsBefore}->{runtime.ResultProviderRequestCount} rows={rowsBefore}->{runtime.Grid.Rows.Length} " +
                $"generation={generationBefore}->{runtime.Generation}");
        }
        layoutHandler = (_, _) =>
        {
            var layoutCompleted = clock.Elapsed;
            Dispatcher.UIThread.Post(() => Complete(layoutCompleted), DispatcherPriority.Render);
        };
        LayoutUpdated += layoutHandler;
        // Detached/headless hosts may not produce layout; release suppression without scheduling data work.
        Dispatcher.UIThread.Post(() => Complete(clock.Elapsed), DispatcherPriority.Background);
    }

    private void BuildVisualTree()
    {
        workspaceBuildCount++;
        var header = new StackPanel { Spacing = 3, Children = { title } };
        if (runtime.Definition.SubtitleKey is not null) header.Children.Add(subtitle);
        parameterFields.Children.Clear();
        parameterLabels.Clear();
        parameterPresenters.Clear();
        var parameterClock = Stopwatch.StartNew();
        foreach (var definition in runtime.Definition.Parameters)
        {
            var editor = CreateEditor(definition);
            parameterLabels[definition.ParameterCode] = new TextBlock();
            parameterFields.Children.Add(editor);
            parameterControlBuildCount++;
        }
        parameterClock.Stop(); parameterConstruction = parameterClock.Elapsed;
        toggle.Click += (_, _) => SetParametersOpen(!AreParametersOpen);
        outputFormat.ItemsSource = runtime.OutputCapabilities
            .Where(x => x.Capabilities.HasFlag(ReportOutputCapability.Export)).Select(x => x.Format).Distinct().ToArray();
        outputFormat.SelectedIndex = outputFormat.ItemCount > 0 ? 0 : -1;
        parameterFields.Children.Add(outputFormat);
        parameterPanel.Child = parameterFields;
        var topRegion = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6,
            Children = { toggle, topActionBar ?? new Border(), overflowActionBar ?? new Border(), contextualActions } };
        Content = new Grid
        {
            RowDefinitions = new("Auto,Auto,*,Auto,Auto"),
            Margin = new Thickness(16), RowSpacing = 10,
            Children =
            {
                header,
                At(topRegion, 1),
                At(new Grid { RowDefinitions = new("Auto,*"), Children = { parameterPanel, At(grid, 1) } }, 2),
                At(bottomActionBar ?? new Border(), 3),
                At(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Children = { state, totals, commandFeedback } }, 4),
            }
        };
        RefreshLocalizedText();
        UpdatePresentation();
        _ = RefreshActionContributionsAsync();
    }

    private void RefreshLocalizedText()
    {
        title.Text = localization.Get(runtime.Definition.TitleKey);
        AutomationProperties.SetName(title, title.Text);
        if (runtime.Definition.SubtitleKey is { } subtitleKey) subtitle.Text = localization.Get(subtitleKey);
        foreach (var definition in runtime.Definition.Parameters)
            parameterPresenters[definition.ParameterCode].RefreshLocalizedPresentation(localization.CurrentCulture, localization.Get);
        toggle.Content = localization.Get(new("Report.Action.Parameters"));
        UpdatePresentation();
    }

    private AvaloniaEditorPresenter CreateEditor(ReportParameterDefinition definition)
    {
        runtime.Parameters.TryGetValue(definition.ParameterCode, out var value);
        var state = new EditorRuntimeState(definition.Editor, value);
        var resolution = new EditorResolver().Resolve(definition.Editor, EditorPlatformCapabilities.AllNative);
        var presenter = new AvaloniaEditorPresenter(definition.Editor, state, resolution, localization.CurrentCulture);
        parameterPresenters[definition.ParameterCode] = presenter;
        return presenter;
    }
    private void RegisterCommands()
    {
        if (commandRegistry is null) return;
        if (!commandRegistry.Register(RunCommandCode, async (_, token) => await ExecuteRunCommandAsync(token)))
            throw new InvalidOperationException("REPORT_RUN_COMMAND_ALREADY_REGISTERED");
        if (!commandRegistry.Register(ResetCommandCode, async (_, token) => await ExecuteResetCommandAsync(token)))
            throw new InvalidOperationException("REPORT_RESET_COMMAND_ALREADY_REGISTERED");
        if (!commandRegistry.Register(ExportCommandCode, async (_, token) => await ExecuteExportCommandAsync(token)) ||
            !commandRegistry.Register(PrintCommandCode, async (_, token) => await ExecutePrintCommandAsync(token)) ||
            !commandRegistry.Register(ViewOutputCommandCode, async (_, token) => await ExecuteViewOutputCommandAsync(token)))
            throw new InvalidOperationException("REPORT_OUTPUT_COMMAND_ALREADY_REGISTERED");
    }
    public async Task<ActionCommandResult> DispatchCommandAsync(string commandCode, CancellationToken token = default)
    {
        if (commandExecuting) return ActionCommandResult.Unavailable("REPORT_COMMAND_BUSY");
        commandExecuting = true;
        commandFeedback.Text = localization.Get(new(commandCode == RunCommandCode ? "Report.Status.Refreshing" :
            commandCode == ResetCommandCode ? "Report.Status.RestoringDefaults" : "Report.Status.CreatingOutput"));
        try
        {
            var result = commandRegistry is not null && commandContext is not null
                ? await commandRegistry.ExecuteAsync(commandCode, commandContext(), token)
                : commandCode == RunCommandCode ? await ExecuteRunCommandAsync(token)
                : commandCode == ResetCommandCode ? await ExecuteResetCommandAsync(token)
                : commandCode == ExportCommandCode ? await ExecuteExportCommandAsync(token)
                : commandCode == PrintCommandCode ? await ExecutePrintCommandAsync(token)
                : commandCode == ViewOutputCommandCode ? await ExecuteViewOutputCommandAsync(token)
                : ActionCommandResult.Unavailable("REPORT_COMMAND_UNKNOWN");
            commandFeedback.Text = result.Message ?? result.DiagnosticCode ?? result.Status.ToString();
            return result;
        }
        finally { commandExecuting = false; }
    }
    private async Task<ActionCommandResult> ExecuteRunCommandAsync(CancellationToken token)
    {
        var resolved = authorization is null ? null : await authorization(token);
        if (resolved is not null && !resolved.CanRun)
            return ActionCommandResult.Denied("REPORT_RUN_DENIED", localization.Get(new("Report.Status.RunDenied")));
        var before = runtime.ResultProviderRequestCount;
        await RunAsync(token: token, reportAuthorization: resolved);
        return runtime.State == ContentPresentationState.Ready && runtime.ResultProviderRequestCount == before + 1
            ? ActionCommandResult.Success($"{localization.Get(new("Report.Status.Refreshed"))} · generation {runtime.Generation} · {DateTimeOffset.Now:t}")
            : ActionCommandResult.Failed("REPORT_REFRESH_INCOMPLETE", $"Refresh ended in {runtime.State}.");
    }
    private async Task<ActionCommandResult> ExecuteResetCommandAsync(CancellationToken token)
    {
        var resolved = authorization is null ? null : await authorization(token);
        if (resolved is not null && !resolved.CanRun)
            return ActionCommandResult.Denied("REPORT_RESET_DENIED", localization.Get(new("Report.Status.ResetDenied")));
        runtime.ResetQueryState();
        foreach (var definition in runtime.Definition.Parameters)
        {
            var presenter = parameterPresenters[definition.ParameterCode];
            presenter.State.SetCandidate(definition.DefaultValue);
            presenter.State.Commit(EditorValidationResult.Valid);
            presenter.Cancel();
        }
        var before = runtime.ResultProviderRequestCount;
        await RunAsync(token: token, reportAuthorization: resolved);
        return runtime.State == ContentPresentationState.Ready && runtime.ResultProviderRequestCount == before + 1
            ? ActionCommandResult.Success(localization.Get(new("Report.Status.DefaultsRestored")))
            : ActionCommandResult.Failed("REPORT_RESET_INCOMPLETE", $"Reset ended in {runtime.State}.");
    }
    private async Task<ActionCommandResult> ExecuteExportCommandAsync(CancellationToken token)
    {
        if (outputFormat.SelectedItem is not ReportOutputFormat format)
            return ActionCommandResult.Unavailable("REPORT_EXPORT_UNAVAILABLE", localization.Get(new("Report.Status.OutputUnavailable")));
        var resolved = authorization is null ? null : await authorization(token);
        if (resolved is not null && (!resolved.CanRun || !resolved.CanOutput(format, ReportOutputCapability.Export)))
            return ActionCommandResult.Denied("REPORT_EXPORT_DENIED", localization.Get(new("Report.Status.OutputDenied")));
        var columns = runtime.Definition.Columns.Where(x => x.IsVisible &&
            (resolved is null || resolved.Columns.GetValueOrDefault(x.ColumnCode)?.State == UiAuthorizationState.Enabled))
            .Select(x => x.ColumnCode);
        var result = await runtime.ExportAsync(format, ReportExportScope.FilteredReport, columns, runtime.Grid.SelectedRowKeys, token);
        return result.IsSuccess ? ActionCommandResult.Success(localization.Get(new("Report.Status.OutputCreated")))
            : ActionCommandResult.Unavailable(result.DiagnosticCode ?? "REPORT_EXPORT_UNAVAILABLE", localization.Get(new("Report.Status.OutputUnavailable")));
    }
    private async Task<ActionCommandResult> ExecutePrintCommandAsync(CancellationToken token)
    {
        if (outputFormat.SelectedItem is not ReportOutputFormat format)
            return ActionCommandResult.Unavailable("REPORT_PRINT_UNAVAILABLE", localization.Get(new("Report.Status.OutputUnavailable")));
        var resolved = authorization is null ? null : await authorization(token);
        if (resolved is not null && (!resolved.CanRun || !resolved.CanOutput(format, ReportOutputCapability.Print)))
            return ActionCommandResult.Denied("REPORT_PRINT_DENIED", localization.Get(new("Report.Status.OutputDenied")));
        var columns = runtime.Definition.Columns.Where(x => x.IsVisible &&
            (resolved is null || resolved.Columns.GetValueOrDefault(x.ColumnCode)?.State == UiAuthorizationState.Enabled))
            .Select(x => x.ColumnCode);
        var result = await runtime.PrintAsync(format, ReportExportScope.FilteredReport, columns, token);
        return result.IsSuccess ? ActionCommandResult.Success(localization.Get(new("Report.Status.OutputCreated")))
            : ActionCommandResult.Unavailable(result.DiagnosticCode ?? "REPORT_PRINT_UNAVAILABLE", localization.Get(new("Report.Status.OutputUnavailable")));
    }
    private async Task<ActionCommandResult> ExecuteViewOutputCommandAsync(CancellationToken token)
    {
        var result = await runtime.ViewOutputAsync(token);
        return result.IsSuccess ? ActionCommandResult.Success(localization.Get(new("Report.Status.OutputOpened")))
            : ActionCommandResult.Unavailable(result.DiagnosticCode ?? "REPORT_DOCUMENT_VIEW_UNAVAILABLE", localization.Get(new("Report.Status.ViewUnavailable")));
    }
    private void UpdatePresentation()
    {
        parameterPanel.IsVisible = AreParametersOpen;
        state.Text = runtime.State.ToString();
        totals.Text = runtime.GeneratedAt is { } generated
            ? $"Rows: {runtime.Grid.VisibleRows:N0} · Generated: {generated.ToLocalTime():g}" : string.Empty;
        AutomationProperties.SetName(state, $"Report state: {runtime.State}");
        _ = RefreshActionContributionsAsync();
    }
    private async Task RefreshActionContributionsAsync()
    {
        if (commandContext is null || topActionBar is null || bottomActionBar is null || overflowActionBar is null) return;
        lastAuthorization = authorization is null ? null : await authorization(CancellationToken.None);
        var contributions = ActionContributions;
        var execution = commandContext();
        topActionBar.Show(ApplyAuthorization(actionBarResolver.Resolve(contributions.Top, execution.ResolutionContext)), execution);
        bottomActionBar.Show(ApplyAuthorization(actionBarResolver.Resolve(contributions.Bottom, execution.ResolutionContext)), execution);
        overflowActionBar.Show(ApplyAuthorization(actionBarResolver.Resolve(contributions.Overflow, execution.ResolutionContext)), execution);
        var selection = runtime.IsDocumentViewingAvailable && runtime.LastOutputArtifact is { } artifact
            ? new SemanticSelection("REPORT_OUTPUT", [artifact.ArtifactReference], runtime.Generation) : null;
        contextualActions.Show(ContextualActionResolver.Resolve(selection, contributions.Contextual,
            action => ActionState(new(action.ActionCode))), code => DispatchCommandAsync(code));
    }
    private ResolvedActionBar ApplyAuthorization(ResolvedActionBar bar) => bar with
    {
        Actions = bar.Actions.Select(x => x with { State = ToPresentationState(ActionState(new(x.Definition.ActionCode))) })
            .Where(x => x.State != AuthorizationPresentationState.Hidden).ToImmutableArray()
    };
    private UiAuthorizationState ActionState(ReportActionCode code)
    {
        var definition = runtime.Definition.Actions.FirstOrDefault(x => x.ActionCode == code);
        if (definition is null || definition.Placement == ReportActionPlacement.Hidden) return UiAuthorizationState.Hidden;
        if (code.Value == "PRINT" && !runtime.OutputCapabilities.Any(x => x.Capabilities.HasFlag(ReportOutputCapability.Print)))
            return UiAuthorizationState.Hidden;
        if (code.Value == "VIEW_OUTPUT" && !runtime.IsDocumentViewingAvailable) return UiAuthorizationState.Hidden;
        return lastAuthorization?.ActionState(code) ?? UiAuthorizationState.Enabled;
    }
    private static AuthorizationPresentationState ToPresentationState(UiAuthorizationState state) => state switch
    {
        UiAuthorizationState.Hidden => AuthorizationPresentationState.Hidden,
        UiAuthorizationState.Disabled => AuthorizationPresentationState.VisibleDisabled,
        UiAuthorizationState.ReadOnly => AuthorizationPresentationState.VisibleReadOnly,
        _ => AuthorizationPresentationState.VisibleEnabled,
    };
    private void ActionCompleted(object? sender, ActionCommandResult result) =>
        commandFeedback.Text = result.Message ?? result.DiagnosticCode ?? result.Status.ToString();
    private static T At<T>(T control, int row) where T : Control { Grid.SetRow(control, row); return control; }
}
