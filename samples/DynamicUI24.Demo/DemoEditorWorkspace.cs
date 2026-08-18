using System.Collections.Immutable;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicUI24.Avalonia;
using DynamicUI24.Avalonia.Presentation.Editors;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

public sealed class DemoEditorWorkspace : ScrollViewer, IRuntimeLocalizationAware, IRuntimeWorkspaceActivationAware
{
    private readonly EditorResolver resolver = new();
    private readonly StackPanel content = new() { Margin = new(18), Spacing = 16 };
    private readonly ILocalizationService localization;
    private readonly List<AvaloniaEditorPresenter> presenters = [];
    private readonly List<LargeDemoLookupProvider> lookupProviders = [];
    private readonly EditorActionDispatcher? actionDispatcher;
    private readonly Func<ActionCommandExecutionContext>? actionContext;
    private readonly TextBlock actionStatus = new() { Text = "Embedded action status: ready" };
    private CultureInfo culture;

    public DemoEditorWorkspace(ILocalizationService localization, IActionCommandRegistry? commands = null,
        Func<ActionCommandExecutionContext>? actionContext = null)
    {
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        if (commands is not null)
        { actionDispatcher = new(commands); this.actionContext = actionContext ?? throw new ArgumentNullException(nameof(actionContext)); }
        culture = localization.CurrentCulture;
        CultureSelector = new ComboBox { ItemsSource = new[] { "en-US", "vi-VN" }, SelectedItem = culture.Name, Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left };
        CultureSelector.SelectionChanged += (_, _) =>
        {
            if (CultureSelector.SelectedItem is string selected && localization.TrySetCulture(selected))
                RefreshLocalization(localization.CurrentCulture);
        };
        BuildOnce(); Content = content;
        RefreshLocalization(culture);
    }

    public ComboBox CultureSelector { get; }
    public IReadOnlyList<AvaloniaEditorPresenter> Presenters => presenters;
    public int LookupRequestCount => lookupProviders.Sum(x => x.RequestCount);
    public string ActionStatusText => actionStatus.Text ?? string.Empty;
    public int WorkspaceActivationCount { get; private set; }
    public int WorkspaceDeactivationCount { get; private set; }

    public void WorkspaceActivated()
    {
        WorkspaceActivationCount++;
        foreach (var text in presenters.SelectMany(x => x.NativeTextInputs))
            NativeEditorInputOwnership.WorkspaceActivated(text);
    }

    public void WorkspaceDeactivated() => WorkspaceDeactivationCount++;

    public void RefreshLocalization(CultureInfo value)
    {
        culture = value ?? throw new ArgumentNullException(nameof(value));
        if (!Equals(CultureSelector.SelectedItem, culture.Name)) CultureSelector.SelectedItem = culture.Name;
        foreach (var presenter in presenters) presenter.RefreshLocalizedPresentation(culture, localization.Get);
    }

    private void BuildOnce()
    {
        content.Children.Add(new TextBlock { Text = "EDITOR DEMO", FontSize = 24 });
        content.Children.Add(new TextBlock { Text = "Native Avalonia input · Unicode/IME owned by the OS · explicit editor commit boundary" });
        content.Children.Add(actionStatus);
        content.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
            Children = { new TextBlock { Text = "Culture", VerticalAlignment = VerticalAlignment.Center }, CultureSelector } });
        AddSection("TEXT",
            Def("TEXT", EditorValueType.String, helper: "Vietnamese, emoji, selection and native clipboard", required: true, maxLength: 80),
            Def("MEMO", EditorValueType.LongString),
            Def("PASSWORD", EditorValueType.Secret, sensitivity: new(Sensitivity.Restricted, PrivacyPresentation.Mask)),
            Def("HYPERLINK", EditorValueType.Hyperlink, capabilities: EditorCapability.ExternalNavigation,
                actions: [new(DemoEditorActions.HyperlinkOpen, EditorActionKind.Open, new("Open safely"))]),
            Def("BUTTON_EDIT", EditorValueType.String, EditorKind.ButtonEdit,
                actions: [new(DemoEditorActions.ButtonEditBrowse, EditorActionKind.Browse, new("Browse…"))]));
        AddSection("NUMERIC",
            Def("INTEGER", EditorValueType.Integer, minimum: 0, maximum: 100),
            Def("DECIMAL", EditorValueType.Decimal),
            Def("CURRENCY", EditorValueType.Currency, formatting: new("C", "USD")),
            Def("PERCENTAGE", EditorValueType.Percentage,
                formatting: new("P1", PercentageScale: PercentageStorageScale.Fraction), increment: .01m));
        AddSection("DATE / TIME",
            Def("DATE", EditorValueType.Date), Def("TIME", EditorValueType.Time),
            Def("DATETIME", EditorValueType.DateTime), Def("DATE_RANGE", EditorValueType.DateRange));
        AddSection("CHOICE",
            Def("BOOLEAN", EditorValueType.Boolean),
            Def("CHOICE", EditorValueType.Choice, choices: Options("ALPHA", "BETA", "GAMMA")),
            Def("MULTICHOICE", EditorValueType.MultiChoice, choices: Options("ONE", "TWO", "THREE")));
        AddSection("LOOKUP",
            Def("LOOKUP", EditorValueType.LookupKey, EditorKind.SearchLookup, helper: "100,000 logical records; 50-item query window"),
            Def("TREE_LOOKUP", EditorValueType.LookupKey, EditorKind.TreeLookup));
        AddSection("STATES / VALIDATION",
            Def("PATTERN", EditorValueType.String, helper: "Three uppercase letters", pattern: "^[A-Z]{3}$"),
            Def("READ_ONLY", EditorValueType.String, readOnly: true),
            Def("DISABLED", EditorValueType.String, disabled: true));
    }

    private void AddSection(string title, params EditorDefinition[] definitions)
    {
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 18 });
        foreach (var definition in definitions)
        {
            object? value = definition.ValueType switch
            {
                EditorValueType.String when definition.EditorCode.Value == "READ_ONLY" => "Inspect-only Unicode: Tiếng Việt 🌏",
                EditorValueType.Hyperlink => "Open neutral demo destination",
                EditorValueType.Currency => 1234.56m, EditorValueType.Percentage => .15m,
                EditorValueType.Integer => 10L, EditorValueType.Decimal => 12.5m,
                EditorValueType.Date => new DateOnly(2026, 8, 18), EditorValueType.Time => new TimeOnly(9, 30),
                EditorValueType.DateTime => new DateTime(2026, 8, 18, 9, 30, 0),
                EditorValueType.Secret => "not-persisted-demo-secret", _ => null,
            };
            var resolution = resolver.Resolve(definition, EditorPlatformCapabilities.AllNative);
            LargeDemoLookupProvider? lookupProvider = null;
            if (definition.ValueType == EditorValueType.LookupKey)
            { lookupProvider = new(); lookupProviders.Add(lookupProvider); }
            var presenter = new AvaloniaEditorPresenter(definition, new(definition, value), resolution, culture,
                lookupProvider: lookupProvider);
            presenter.ActionInvoked += DispatchEmbeddedAction;
            presenters.Add(presenter); panel.Children.Add(presenter);
        }
        content.Children.Add(panel);
    }

    private async void DispatchEmbeddedAction(object? sender, EditorActionInvokedEventArgs e)
    {
        if (sender is not AvaloniaEditorPresenter presenter)
        {
            actionStatus.Text = $"Embedded action unavailable: {e.Action.ActionCode}";
            return;
        }
        if (actionDispatcher is null || actionContext is null)
        {
            SetActionResult(presenter, $"Embedded action unavailable: {e.Action.ActionCode}");
            return;
        }
        try
        {
            var result = await actionDispatcher.DispatchAsync(e.Action, presenter.Resolution, actionContext());
            SetActionResult(presenter, result.Status == ActionCommandResultStatus.Success
                ? result.Message ?? $"Action completed: {e.Action.ActionCode}"
                : $"Action {result.Status}: {result.DiagnosticCode ?? e.Action.ActionCode}");
        }
        catch (Exception ex)
        {
            SetActionResult(presenter, $"Action Failed: {ex.GetType().Name}");
        }
    }

    private void SetActionResult(AvaloniaEditorPresenter presenter, string result)
    {
        actionStatus.Text = result;
        presenter.ShowActionFeedback(result);
    }

    private static EditorDefinition Def(string code, EditorValueType type, EditorKind? kind = null,
        string? helper = null, bool required = false, int? maxLength = null, decimal? minimum = null,
        decimal? maximum = null, decimal? increment = null, EditorFormattingDefinition? formatting = null,
        IEnumerable<EditorChoiceOption>? choices = null, string? pattern = null, bool readOnly = false,
        bool disabled = false, EditorCapability capabilities = EditorCapability.None,
        SensitiveContentDefinition? sensitivity = null, IEnumerable<EditorActionDefinition>? actions = null) =>
        new(new(code), new($"EDITOR_DEMO.{code}"), type, kind, capabilities,
            new(new($"Editor Demo · {code}"), new("Enter a value"), helper is null ? null : new(helper)), formatting,
            new(required, MaximumLength: maxLength, Pattern: pattern, Minimum: minimum, Maximum: maximum), choices, actions,
            sensitiveContent: sensitivity, helpContextCode: new HelpContextCode($"EDITOR.{code}"),
            isReadOnly: readOnly, isDisabled: disabled, maxLength: maxLength, minimum: minimum, maximum: maximum,
            increment: increment);

    private static EditorChoiceOption[] Options(params string[] ids) => ids.Select(x => new EditorChoiceOption(x, new(x), x)).ToArray();

    private sealed class LargeDemoLookupProvider : IEditorLookupProvider
    {
        public string ProviderCode => "DEMO.EDITOR.100K";
        public int RequestCount { get; private set; }
        public ValueTask<EditorLookupResult> QueryAsync(EditorLookupRequest request)
        {
            RequestCount++;
            request.CancellationToken.ThrowIfCancellationRequested();
            var matches = Enumerable.Range(0, 100_000).Where(i => $"Record {i:D6}".Contains(request.SearchText, StringComparison.OrdinalIgnoreCase))
                .Skip(request.Offset).Take(request.BoundedWindowSize)
                .Select(i => new EditorLookupOption($"REC-{i:D6}", $"Record {i:D6}", $"Semantic ID REC-{i:D6}"))
                .ToImmutableArray();
            var continuation = matches.Length == request.BoundedWindowSize ? (request.Offset + matches.Length).ToString(CultureInfo.InvariantCulture) : null;
            return ValueTask.FromResult(new EditorLookupResult(matches, continuation, 100_000, request.Generation,
                request.CompanyId, request.ContextRevision, matches.Length == 0 ? EditorLookupStatus.NoMatch : EditorLookupStatus.Ready));
        }
    }
}
