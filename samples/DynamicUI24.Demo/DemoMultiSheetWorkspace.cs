using System.Collections.Immutable;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Sheets;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

/// <summary>Neutral, in-memory 10F smoke surface. It owns no formula or business semantics.</summary>
internal sealed class DemoMultiSheetWorkspace
{
    private readonly Dictionary<SheetCode, DemoSheetContent> content = [];
    private readonly DemoSheetMaterializer materializer;
    private readonly DemoSheetLifecycleProvider lifecycle;
    private readonly DemoCalculationCompatibility calculation = new();
    private readonly SheetPresentationResolver presentation;
    private readonly SheetHostView hostView;
    private readonly TextBlock status = new() { TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
    private readonly TextBox titleInput = new() { Watermark = "New title", Width = 150 };
    private readonly ComboBox hiddenSheets = new() { Width = 150 };
    private CompanyDescriptor company;
    private EffectiveAuthorizationContext? authorization;
    private int identitySequence = 1;

    public DemoMultiSheetWorkspace(DataEntryGridHost primary, DemoDataEntryProvider primaryProvider,
        ILocalizationService localization, AppearancePreferenceService appearance, IPrivacyStateService privacyState,
        IPrivacyPolicyResolver privacyResolver, ISensitiveValuePresenter sensitivePresenter,
        CompanyDescriptor initialCompany, Action activeSheetChanged)
    {
        company = initialCompany;
        presentation = new(privacyResolver, sensitivePresenter, localization);
        materializer = new(primary, primaryProvider, localization, appearance, privacyResolver, privacyState, sensitivePresenter,
            () => company, () => authorization);
        materializer.Released += code => content.Remove(code);
        var sheets = new[]
        {
            Sheet("SHEET_A", "Sheet A — Detail", "100,000 logical rows", 10),
            Sheet("SHEET_B", "Sheet B — Summary", "100,000 logical rows", 20),
            Sheet("SHEET_C", "Sheet C — Adjustment", "100,000 logical rows", 30),
            Sheet("SHEET_D", "Sheet D — Overflow", "100,000 logical rows", 40),
            Sheet("SHEET_PRIVATE", "Restricted sheet title", "Restricted sheet subtitle", 50,
                new(Sensitivity.Restricted, PrivacyPresentation.Mask)),
        };
        IReadOnlyList<SheetDefinition> currentSheets = sheets;
        lifecycle = new(() => currentSheets, NewCode, DemoDataEntry.CreateDefinition);
        Host = new(new("DEMO_MULTI_SHEET", sheets,
            new(true, true, true, true, true, true, true), maximumMaterializedSheets: 3),
            materializer, lifecycle, calculation, preferredActiveSheet: new("SHEET_A"));
        hostView = new(Host, Present, Content, maximumVisibleTabs: 3);
        Host.Changed += (_, args) =>
        {
            currentSheets = Host.Sheets;
            if (args.Reason == "ACTIVATED") activeSheetChanged();
            Dispatcher.UIThread.Post(() => { hostView.Rebuild(); RefreshStatus(args.Reason); RefreshHidden(); });
        };
        privacyState.StateChanged += (_, _) => Dispatcher.UIThread.Post(() => { hostView.Rebuild(); RefreshStatus("PRIVACY_CHANGED"); });
        View = Build(); RefreshHidden(); RefreshStatus("READY");
    }

    public SheetHostRuntime Host { get; }
    public Control View { get; }
    public SheetCode? ActiveSheetCode => Host.ActiveSheetCode;
    public DataEntryGridRuntime? ActiveRuntime => ActiveSheetCode is { } code && content.TryGetValue(code, out var item)
        ? item.Runtime : Host.GetActiveRuntime() is DemoSheetContent value ? value.Runtime : null;

    public bool Activate(SheetCode code)
    {
        var activated = Host.TryActivate(code);
        if (activated && content.TryGetValue(code, out var item)) _ = item.LoadAsync(company, authorization);
        RefreshStatus(activated ? "S1_ACTIVATED" : "S1_ACTIVATION_REJECTED");
        return activated;
    }

    public Task UpdateContextAsync(CompanyDescriptor value, EffectiveAuthorizationContext? effectiveAuthorization)
    {
        company = value; authorization = effectiveAuthorization; Host.UpdateAuthorization(effectiveAuthorization);
        return Task.WhenAll(content.Values.Select(item => item.LoadAsync(company, authorization)));
    }

    private SheetDefinition Sheet(string code, string title, string subtitle, int order,
        SensitiveContentDefinition? privacy = null) => new(new(code), new(title), order,
        SheetContentType.DataEntryGrid, $"GRID_{code}", new(subtitle), DemoDataEntry.CreateDefinition(),
        new(new(title), new(subtitle), ShowRowCount: true, ShowSelectionCount: true,
            TitlePrivacy: privacy, SubtitlePrivacy: privacy), privacyMetadata: privacy);

    private SheetPresentation Present(SheetDefinition sheet) => presentation.Resolve(sheet,
        Host.ActiveSheetCode == sheet.SheetCode, authorization, materializer.PrivacyMode,
        company.CompanyId, "data-entry-demo");

    private Control Content(SheetDefinition sheet, object value)
    {
        var item = (DemoSheetContent)value; content[sheet.SheetCode] = item;
        return item.Host;
    }

    private Control Build()
    {
        var actions = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 6, LineSpacing = 6 };
        actions.Children.Add(titleInput);
        Add(actions, "Create", async () => await CreateAsync());
        Add(actions, "Duplicate Full", async () => await CloneAsync(false, SheetClonePolicy.DuplicateFull));
        Add(actions, "Duplicate Structure", async () => await CloneAsync(false, SheetClonePolicy.StructureOnly));
        Add(actions, "Save As", async () => await CloneAsync(true, SheetClonePolicy.NewDataContext()));
        Add(actions, "Rename", () => { if (Host.ActiveSheetCode is { } code) Host.Rename(code,
            new(string.IsNullOrWhiteSpace(titleInput.Text) ? $"Renamed {code.Value}" : titleInput.Text!)); return Task.CompletedTask; });
        Add(actions, "Move Left", () => Move(-15)); Add(actions, "Move Right", () => Move(15));
        Add(actions, "Hide", () => { if (Host.ActiveSheetCode is { } code) Host.SetHidden(code, true); return Task.CompletedTask; });
        actions.Children.Add(hiddenSheets); Add(actions, "Show", () =>
        { if (hiddenSheets.SelectedItem is SheetDefinition sheet) Host.SetHidden(sheet.SheetCode, false); return Task.CompletedTask; });
        Add(actions, "Delete", async () => { if (Host.ActiveSheetCode is { } code) Show(await Host.DeleteAsync(code)); });
        Add(actions, "Cycle diagnostic", async () => Show(await Host.RequestRecalculationAsync(
            Host.ActiveSheetCode is { } code ? [code] : [])));
        var panel = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        actions.Margin = new(8); status.Margin = new(8);
        Grid.SetRow(actions, 0); Grid.SetRow(hostView, 1); Grid.SetRow(status, 2);
        panel.Children.Add(actions); panel.Children.Add(hostView); panel.Children.Add(status);
        return panel;
    }

    private static void Add(Panel panel, string title, Func<Task> action)
    {
        var button = new Button { Content = title }; button.Click += async (_, _) => await action(); panel.Children.Add(button);
    }
    private Task Move(int delta)
    { if (Host.ActiveSheetCode is { } code) Host.Reorder(code, Host.Sheets.First(x => x.SheetCode == code).DisplayOrder + delta); return Task.CompletedTask; }
    private async Task CreateAsync()
    {
        lifecycle.PendingCreateCode = NewCode("CREATED");
        var result = await Host.CreateAsync(); Show(result);
        if (result.IsSuccess && result.Sheet is { } sheet) Host.TryActivate(sheet.SheetCode);
    }
    private async Task CloneAsync(bool saveAs, SheetClonePolicy policy)
    {
        if (Host.ActiveSheetCode is not { } source) return;
        var target = NewCode(saveAs ? "SAVED" : "COPY");
        var request = SheetCloneRequest.Create(source, target,
            string.IsNullOrWhiteSpace(titleInput.Text) ? $"{(saveAs ? "Saved" : "Copy")} {target.Value}" : titleInput.Text!,
            policy, saveAs ? [new(source, target)] : [], saveAs ? "DEMO_TARGET_CONTEXT" : null);
        var result = saveAs ? await Host.SaveAsAsync(request) : await Host.DuplicateAsync(request);
        Show(result); if (result.IsSuccess) Host.TryActivate(target);
    }
    private SheetCode NewCode(string prefix) => new($"{prefix}_{identitySequence++:000}");
    private void Show(SheetLifecycleResult result) => RefreshStatus(result.IsSuccess ? "LIFECYCLE_OK" : result.DiagnosticCode ?? "LIFECYCLE_FAILED");
    private void Show(SheetCalculationResult result) => RefreshStatus(result.IsSuccess ? "CALC_OK" :
        string.Join(", ", result.Diagnostics.Select(x => x.Code)));
    private void RefreshHidden() { hiddenSheets.ItemsSource = Host.HiddenSheets; hiddenSheets.SelectedIndex = Host.HiddenSheets.Length > 0 ? 0 : -1; }
    private void RefreshStatus(string reason)
    {
        var active = Host.ActiveSheetCode;
        var item = active is { } code && content.TryGetValue(code, out var found) ? found : null;
        status.Text = $"10F · {reason} · ActiveSheetCode={active?.Value ?? "NONE"} · " +
            $"DataIdentity=CompanyCode:{company.Code} · " +
            $"LogicalRows={(item?.Runtime.TotalRows ?? DemoDataEntryProvider.LogicalRowCount):N0} · " +
            $"ActiveRows={item?.Runtime.Rows.Length ?? 0} · CachedRows={item?.Runtime.CachedRowCount ?? 0} · " +
            $"MaterializedSheets={Host.MaterializedSheetCount}/{Host.Definition.MaximumMaterializedSheets} · " +
            $"Calculation={calculation.LastSummary} · Mapping={lifecycle.LastMappingSummary}";
    }
}

internal sealed record DemoSheetContent(DataEntryGridRuntime Runtime, DataEntryGridHost Host)
{
    private CompanyId? loadedCompany;
    private Task? loadTask;
    public Task LoadAsync(CompanyDescriptor company, EffectiveAuthorizationContext? authorization)
    {
        if (loadedCompany == company.CompanyId && Runtime.Rows.Length > 0)
        {
            Host.UpdateAuthorization(authorization);
            return loadTask ?? Task.CompletedTask;
        }
        loadedCompany = company.CompanyId;
        return loadTask = Host.LoadAsync(new(company, "data-entry-demo"), authorization);
    }
}

internal sealed class DemoSheetMaterializer : ISheetRuntimeMaterializer
{
    private readonly DataEntryGridHost primary; private readonly DemoDataEntryProvider primaryProvider;
    private readonly ILocalizationService localization; private readonly AppearancePreferenceService appearance;
    private readonly IPrivacyPolicyResolver resolver; private readonly IPrivacyStateService privacy;
    private readonly ISensitiveValuePresenter presenter; private bool primaryUsed;
    public DemoSheetMaterializer(DataEntryGridHost primary, DemoDataEntryProvider primaryProvider,
        ILocalizationService localization, AppearancePreferenceService appearance, IPrivacyPolicyResolver resolver,
        IPrivacyStateService privacy, ISensitiveValuePresenter presenter, Func<CompanyDescriptor> company,
        Func<EffectiveAuthorizationContext?> authorization)
    { this.primary = primary; this.primaryProvider = primaryProvider; this.localization = localization; this.appearance = appearance;
      this.resolver = resolver; this.privacy = privacy; this.presenter = presenter; }
    public PrivacyMode PrivacyMode => privacy.RequestedMode;
    public event Action<SheetCode>? Released;
    public object Materialize(SheetDefinition definition, SheetRuntimeState retained)
    {
        DataEntryGridHost host;
        if (!primaryUsed && definition.SheetCode == new SheetCode("SHEET_A")) { primaryUsed = true; host = primary; }
        else
        {
            var provider = new DemoDataEntryProvider();
            host = new(new(definition.GridDefinition!, provider, privacyResolver: resolver, privacyState: privacy,
                sensitiveValuePresenter: presenter), localization, appearance, privacyResolver: resolver,
                privacyState: privacy, sensitivePresenter: presenter);
        }
        host.Runtime.ApplyViewPreference(retained.ViewPreference);
        host.Runtime.ApplyRowHeights(retained.RowHeightOverrides);
        return new DemoSheetContent(host.Runtime, host);
    }
    public SheetRuntimeState Capture(SheetDefinition definition, object value, SheetRuntimeState retained)
    {
        var runtime = ((DemoSheetContent)value).Runtime;
        return retained with { Selection = runtime.CellSelection, ViewportStartIndex = runtime.RequestedViewportStartIndex,
            Filters = runtime.Filters, Sorts = runtime.Sorts, Generation = runtime.Generation,
            IsDirty = runtime.PendingChangeCount > 0, ViewPreference = runtime.CurrentViewPreference,
            RowHeightOverrides = runtime.CaptureRowHeights() };
    }
    public void Release(SheetDefinition definition, object runtime) => Released?.Invoke(definition.SheetCode);
}

internal sealed class DemoSheetLifecycleProvider(Func<IReadOnlyList<SheetDefinition>> sheets,
    Func<string, SheetCode> newCode, Func<GridDefinition> grid) : ISheetLifecycleProvider
{
    public SheetCode? PendingCreateCode { get; set; }
    public string LastMappingSummary { get; private set; } = "none";
    public Task<SheetLifecycleResult> CreateAsync(CancellationToken cancellationToken = default)
    {
        var code = PendingCreateCode ?? newCode("CREATED"); PendingCreateCode = null;
        return Task.FromResult(SheetLifecycleResult.Success(New(code, $"Created {code.Value}")));
    }
    public Task<SheetLifecycleResult> CloneAsync(SheetCloneRequest request, CancellationToken cancellationToken = default)
    {
        LastMappingSummary = request.ReferenceMappings.Length == 0 ? "none" :
            string.Join(",", request.ReferenceMappings.Select(x => $"{x.SourceSheetCode}->{x.TargetSheetCode}"));
        return Task.FromResult(SheetLifecycleResult.Success(New(request.TargetSheetCode, request.TargetTitle)));
    }
    public Task<SheetLifecycleResult> DeleteAsync(SheetCode sheetCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(SheetLifecycleResult.Success(sheets().First(x => x.SheetCode == sheetCode)));
    private SheetDefinition New(SheetCode code, string title) => new(code, new(title),
        sheets().Select(x => x.DisplayOrder).DefaultIfEmpty().Max() + 10, SheetContentType.DataEntryGrid,
        $"GRID_{code.Value}", new("100,000 logical rows"), grid(), new(new(title), new("100,000 logical rows")));
}

internal sealed class DemoCalculationCompatibility : ISheetCalculationCompatibility
{
    public string LastSummary { get; private set; } = "ready";
    public Task<SheetCalculationResult> ValidateCloneAsync(SheetCloneRequest request, CancellationToken cancellationToken = default)
    { LastSummary = "clone validated externally"; return Task.FromResult(SheetCalculationResult.Success()); }
    public Task<SheetCalculationResult> ValidateDeleteAsync(SheetCode code, CancellationToken cancellationToken = default)
    {
        if (code == new SheetCode("SHEET_B")) { LastSummary = "delete dependency blocked";
            return Task.FromResult(new SheetCalculationResult(false, [], [new("CALC_DELETE_REFERENCED", SheetCalculationDiagnosticSeverity.Error, code)])); }
        LastSummary = "delete validated externally"; return Task.FromResult(SheetCalculationResult.Success());
    }
    public Task<SheetCalculationResult> RequestRecalculationAsync(IEnumerable<SheetCode> changedSheets, CancellationToken cancellationToken = default)
    { LastSummary = "cycle failed safely"; return Task.FromResult(new SheetCalculationResult(false, [],
        [new("CALC_CYCLE", SheetCalculationDiagnosticSeverity.Error, changedSheets.FirstOrDefault())])); }
}
