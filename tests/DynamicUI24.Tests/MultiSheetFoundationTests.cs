using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Sheets;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class MultiSheetFoundationTests
{
    [Fact]
    public void SheetCodeIsNormalizedAndIndependentOfPresentation()
    {
        var code = new SheetCode(" detail ");
        var sheet = Sheet(code, "Title.A", 1);
        Assert.Equal("DETAIL", code.Value);
        Assert.Equal(code, (sheet with { TitleKey = new("Title.B"), DisplayOrder = 99 }).SheetCode);
    }

    [Fact]
    public void HostRejectsDuplicateSemanticIdentityButAllowsDuplicateTitles() =>
        Assert.Throws<ArgumentException>(() => Host(Sheet(new("A"), "Same", 1), Sheet(new("a"), "Same", 2)));

    [Fact]
    public void RenameReorderAndLocalizationKeysPreserveActiveIdentity()
    {
        var runtime = Runtime(Host(Sheet(new("A"), "Title.En", 1), Sheet(new("B"), "Title.En", 2)));
        Assert.True(runtime.TryActivate(new("B")));
        Assert.True(runtime.Rename(new("B"), new("Title.Vi"), new("Subtitle.Vi")));
        Assert.True(runtime.Reorder(new("B"), -10));
        Assert.Equal(new SheetCode("B"), runtime.ActiveSheetCode);
    }

    [Fact]
    public void HidingActiveSheetFailsClosedToFirstEligibleAndHiddenCannotActivate()
    {
        var runtime = Runtime(Host(Sheet(new("A"), "A", 1), Sheet(new("B"), "B", 2)));
        Assert.True(runtime.SetHidden(new("A"), true));
        Assert.Equal(new SheetCode("B"), runtime.ActiveSheetCode);
        Assert.False(runtime.TryActivate(new("A")));
        Assert.True(runtime.SetHidden(new("A"), false));
        Assert.True(runtime.TryActivate(new("A")));
    }

    [Fact]
    public void PerSheetStateIsIsolatedAcrossBoundedEvictionAndRehydration()
    {
        var materializer = new FakeMaterializer();
        var runtime = Runtime(Host(1, Sheet(new("A"), "A", 1), Sheet(new("B"), "B", 2)), materializer);
        runtime.GetActiveRuntime();
        runtime.RetainState(SheetRuntimeState.Empty(new("A")) with { ViewportStartIndex = 99980,
            Filters = [new(new("NAME"), GridFilterOperator.Contains, "alpha")] });
        Assert.True(runtime.TryActivate(new("B")));
        runtime.RetainState(SheetRuntimeState.Empty(new("B")) with { ViewportStartIndex = 50000 });
        Assert.True(runtime.TryActivate(new("A")));
        Assert.Equal(1, runtime.MaterializedSheetCount);
        Assert.Equal(99980, runtime.GetRetainedState(new("A")).ViewportStartIndex);
        Assert.Equal(50000, runtime.GetRetainedState(new("B")).ViewportStartIndex);
        Assert.True(materializer.Releases >= 2);
    }

    [Fact]
    public async Task ExistingDataEntryPercentageSizingRangeAndClipboardRemainIsolatedPerSheet()
    {
        var materializer = new DataEntryMaterializer();
        var definition = Host(2, DataSheet(new("A"), 1), DataSheet(new("B"), 2), DataSheet(new("C"), 3));
        var host = Runtime(definition, materializer);
        var a = Assert.IsType<DataEntryGridRuntime>(host.GetActiveRuntime());
        await a.LoadAsync(DataContext, null);
        Assert.True(a.SetColumnWidthPercentage(new("TEXT"), 150));
        a.SetRowHeightPercentage(125);
        Assert.True(a.ResizeRow(new("company:1"), 92));
        a.SelectCell(new(new("company:1"), new("TEXT")), 0);
        a.SelectCell(new(new("company:2"), new("NUMBER")), 1, extend: true);
        Assert.Equal(4, a.SelectedCellCount);
        Assert.Equal("row-1\t1\nrow-2\t2", (await a.BuildCopyTextAsync()).Result);
        Assert.Equal(4, (await a.PasteTextAsync("changed\t9")).AppliedCellCount);

        Assert.True(host.TryActivate(new("B")));
        var b = Assert.IsType<DataEntryGridRuntime>(host.GetActiveRuntime());
        await b.LoadAsync(DataContext, null);
        Assert.Null(b.ActiveCell);
        Assert.NotEqual(180, b.PresentedColumns.First(x => x.VariableCode == new VariableCode("TEXT")).Width);
        Assert.True(b.SetColumnWidthPercentage(new("TEXT"), 100));
        b.SetRowHeightPercentage(100);
        Assert.True(b.ResizeRow(new("company:1"), 55));
        b.SelectCell(new(new("company:3"), new("TEXT")), 2);

        Assert.True(host.TryActivate(new("A")));
        Assert.Same(a, host.GetActiveRuntime());
        Assert.Equal(4, a.SelectedCellCount);
        Assert.Equal(150, a.GetColumnWidthPercentage(new("TEXT")));
        Assert.Equal(125, a.RowHeightScalePercent);
        Assert.Equal(100, b.GetColumnWidthPercentage(new("TEXT")));
        Assert.Equal(100, b.RowHeightScalePercent);
        Assert.Equal(92, a.GetRowHeight(new("company:1")));
        Assert.Equal(55, b.GetRowHeight(new("company:1")));
        Assert.Equal(2, host.MaterializedSheetCount);

        Assert.True(host.TryActivate(new("C")));
        var c = Assert.IsType<DataEntryGridRuntime>(host.GetActiveRuntime()); await c.LoadAsync(DataContext, null);
        Assert.True(host.TryActivate(new("B")));
        var bRehydrated = Assert.IsType<DataEntryGridRuntime>(host.GetActiveRuntime());
        await bRehydrated.LoadAsync(DataContext, null);
        Assert.NotSame(b, bRehydrated);
        Assert.Equal(100, bRehydrated.GetColumnWidthPercentage(new("TEXT")));
        Assert.Equal(100, bRehydrated.RowHeightScalePercent);
        Assert.Equal(55, bRehydrated.GetRowHeight(new("company:1")));
        Assert.Equal(2, host.MaterializedSheetCount);
    }

    [Fact]
    public async Task DuplicateAndSaveAsAreDistinctPoliciesAndRequireNewProviderIdentity()
    {
        var provider = new FakeLifecycle(); var calculation = new FakeCalculation();
        var runtime = Runtime(Host(Sheet(new("A"), "A", 1)), lifecycle: provider, calculation: calculation);
        var duplicate = SheetCloneRequest.Create(new("A"), new("A_COPY"), "Copy", SheetClonePolicy.DuplicateFull);
        var saveAs = SheetCloneRequest.Create(new("A"), new("A_TARGET"), "Target", SheetClonePolicy.NewDataContext(),
            [new(new("A"), new("A_TARGET"))], "opaque-context");
        Assert.True((await runtime.DuplicateAsync(duplicate)).IsSuccess);
        Assert.True((await runtime.SaveAsAsync(saveAs)).IsSuccess);
        Assert.Equal(SheetCloneMode.DuplicateFull, provider.Requests[0].Policy.Mode);
        Assert.Equal(SheetCloneMode.NewDataContext, provider.Requests[1].Policy.Mode);
        Assert.True(provider.Requests[1].Policy.ResetRowKeys);
        Assert.Equal(2, calculation.CloneValidations);
        provider.ReturnSourceIdentity = true;
        Assert.Equal("SHEET_PROVIDER_IDENTITY_INVALID", (await runtime.DuplicateAsync(
            SheetCloneRequest.Create(new("A"), new("BAD"), "Bad", SheetClonePolicy.DuplicateFull))).DiagnosticCode);
    }

    [Fact]
    public async Task CreateIsProviderOwnedAndRejectsDuplicateIdentity()
    {
        var provider = new FakeLifecycle { CreateResult = SheetLifecycleResult.Success(Sheet(new("NEW"), "New", 3)) };
        var runtime = Runtime(Host(Sheet(new("A"), "A", 1)), lifecycle: provider);
        Assert.True((await runtime.CreateAsync()).IsSuccess);
        provider.CreateResult = SheetLifecycleResult.Success(Sheet(new("A"), "Collision", 4));
        Assert.Equal("SHEET_PROVIDER_IDENTITY_INVALID", (await runtime.CreateAsync()).DiagnosticCode);
    }

    [Fact]
    public void SheetPresentationMasksRestrictedHeaderAcrossSharedSurfaces()
    {
        var sensitive = new SensitiveContentDefinition(Sensitivity.Restricted, PrivacyPresentation.Mask);
        var sheet = new SheetDefinition(new("SECRET"), new("raw-title"), 1, SheetContentType.Custom, "CUSTOM",
            subtitleKey: new("raw-subtitle"), privacyMetadata: sensitive);
        var resolver = new SheetPresentationResolver(new PrivacyPolicyResolver(), new SensitiveValuePresenter(), new EchoLocalization());
        var model = resolver.Resolve(sheet, true, null, PrivacyMode.On, null, "workspace");
        Assert.DoesNotContain("raw", model.Title, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw", model.Subtitle!, StringComparison.OrdinalIgnoreCase);
        Assert.False(model.Privacy.CanExposeToAccessibility);
    }

    [Fact]
    public async Task DeleteAndRecalculationAreDelegatedAndCycleDiagnosticsRemainData()
    {
        var calculation = new FakeCalculation { BlockDelete = true, Cycle = true };
        var runtime = Runtime(Host(Sheet(new("A"), "A", 1)), calculation: calculation);
        var deleted = await runtime.DeleteAsync(new("A"));
        Assert.False(deleted.IsSuccess); Assert.Equal(1, calculation.DeleteValidations);
        var result = await runtime.RequestRecalculationAsync([new("A"), new("A")]);
        Assert.False(result.IsSuccess);
        Assert.Equal("CALC_CYCLE", Assert.Single(result.Diagnostics).Code);
        Assert.Equal(1, calculation.Recalculations);
    }

    [Fact]
    public void ReferenceMappingsRejectIdentityAndCollisions()
    {
        Assert.Throws<ArgumentException>(() => new SheetReferenceMapping(new("A"), new("A")));
        Assert.Throws<ArgumentException>(() => SheetCloneRequest.Create(new("A"), new("B"), "B",
            SheetClonePolicy.NewDataContext(), [new(new("A"), new("B")), new(new("A"), new("C"))]));
    }

    private static SheetDefinition Sheet(SheetCode code, string title, int order) =>
        new(code, new(title), order, SheetContentType.Custom, "CUSTOM");
    private static SheetDefinition DataSheet(SheetCode code, int order) =>
        new(code, new($"Sheet.{code.Value}"), order, SheetContentType.DataEntryGrid, $"GRID_{code.Value}",
            gridDefinition: DataDefinition());
    private static readonly CompanyDescriptor DataCompany = new(new("company"), "DEMO", "Demo");
    private static readonly GridProviderContext DataContext = new(DataCompany, "workspace");
    private static GridDefinition DataDefinition() => new("sheet-grid", "SHEET_GRID", [
        DataColumn("TEXT", ColumnDataType.Text, 0), DataColumn("NUMBER", ColumnDataType.Integer, 1)
    ], selectionMode: GridSelectionMode.Multiple, allowEdit: true);
    private static ColumnDefinition DataColumn(string variable, ColumnDataType type, int order) =>
        new(variable, variable, new(variable), $"Grid.{variable}", null, type, ColumnEditorKind.TextBox,
            ColumnMode.Input, order, 100, 60, 200, true, false, null, null, null, null, null, 1,
            SetupDefinitionStatus.Published);
    private static SheetHostDefinition Host(params SheetDefinition[] sheets) => Host(1, sheets);
    private static SheetHostDefinition Host(int max, params SheetDefinition[] sheets) => new("HOST", sheets,
        new(true, true, true, true, true, true, true), maximumMaterializedSheets: max);
    private static SheetHostRuntime Runtime(SheetHostDefinition definition, ISheetRuntimeMaterializer? materializer = null,
        FakeLifecycle? lifecycle = null, FakeCalculation? calculation = null) =>
        new(definition, materializer ?? new FakeMaterializer(), lifecycle ?? new(), calculation ?? new());

    private sealed class FakeMaterializer : ISheetRuntimeMaterializer
    {
        public int Releases { get; private set; }
        public object Materialize(SheetDefinition definition, SheetRuntimeState retainedState) => new object();
        public SheetRuntimeState Capture(SheetDefinition definition, object runtime, SheetRuntimeState retainedState) => retainedState;
        public void Release(SheetDefinition definition, object runtime) => Releases++;
    }
    private sealed class DataEntryMaterializer : ISheetRuntimeMaterializer
    {
        public object Materialize(SheetDefinition definition, SheetRuntimeState retainedState)
        {
            var runtime = new DataEntryGridRuntime(definition.GridDefinition!, new SheetDataProvider());
            runtime.ApplyViewPreference(retainedState.ViewPreference);
            runtime.ApplyRowHeights(retainedState.RowHeightOverrides);
            return runtime;
        }
        public SheetRuntimeState Capture(SheetDefinition definition, object value, SheetRuntimeState retainedState)
        {
            var runtime = (DataEntryGridRuntime)value;
            return retainedState with { Selection = runtime.CellSelection, Filters = runtime.Filters,
                Sorts = runtime.Sorts, ViewportStartIndex = runtime.RequestedViewportStartIndex,
                ViewPreference = runtime.CurrentViewPreference, RowHeightOverrides = runtime.CaptureRowHeights() };
        }
        public void Release(SheetDefinition definition, object runtime) { }
    }
    private sealed class SheetDataProvider : IDataEntryGridProvider, IGridBatchEditProvider
    {
        private readonly Dictionary<(RowKey, VariableCode), object?> edits = [];
        public Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request,
            CancellationToken cancellationToken = default) => Task.FromResult(GridDataResult.Ready(
                Enumerable.Range(1, 4).Select(index => Row(context, index))));
        public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit,
            CancellationToken cancellationToken = default) => Task.FromResult(GridCommitResult.Success(edit.CandidateValue));
        public Task<GridBatchCommitResult> CommitBatchAsync(GridProviderContext context, GridEditTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            foreach (var change in transaction.CellChanges) edits[(change.RowKey, change.VariableCode)] = change.CandidateValue;
            return Task.FromResult(GridBatchCommitResult.Success);
        }
        private GridRow Row(GridProviderContext context, int index)
        {
            var key = new RowKey($"{context.Company.CompanyId.Value}:{index}");
            object? Value(string variable, object fallback) => edits.GetValueOrDefault((key, new(variable)), fallback);
            return new(key, new Dictionary<VariableCode, object?> {
                [new("TEXT")] = Value("TEXT", $"row-{index}"), [new("NUMBER")] = Value("NUMBER", index) });
        }
    }
    private sealed class FakeLifecycle : ISheetLifecycleProvider
    {
        public List<SheetCloneRequest> Requests { get; } = [];
        public bool ReturnSourceIdentity { get; set; }
        public SheetLifecycleResult CreateResult { get; set; } = SheetLifecycleResult.Rejected("NOT_USED");
        public Task<SheetLifecycleResult> CreateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult);
        public Task<SheetLifecycleResult> CloneAsync(SheetCloneRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request); var code = ReturnSourceIdentity ? request.SourceSheetCode : request.TargetSheetCode;
            return Task.FromResult(SheetLifecycleResult.Success(Sheet(code, request.TargetTitle, 100 + Requests.Count)));
        }
        public Task<SheetLifecycleResult> DeleteAsync(SheetCode sheetCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(SheetLifecycleResult.Success(Sheet(sheetCode, "Deleted", 0)));
    }
    private sealed class FakeCalculation : ISheetCalculationCompatibility
    {
        public int CloneValidations, DeleteValidations, Recalculations; public bool BlockDelete, Cycle;
        public Task<SheetCalculationResult> ValidateCloneAsync(SheetCloneRequest request, CancellationToken cancellationToken = default)
        { CloneValidations++; return Task.FromResult(SheetCalculationResult.Success()); }
        public Task<SheetCalculationResult> ValidateDeleteAsync(SheetCode sheetCode, CancellationToken cancellationToken = default)
        { DeleteValidations++; return Task.FromResult(BlockDelete ? new(false, [], [new("CALC_DELETE_REFERENCED", SheetCalculationDiagnosticSeverity.Error, sheetCode)]) : SheetCalculationResult.Success()); }
        public Task<SheetCalculationResult> RequestRecalculationAsync(IEnumerable<SheetCode> changedSheets, CancellationToken cancellationToken = default)
        { Recalculations++; return Task.FromResult(Cycle ? new(false, [], [new("CALC_CYCLE", SheetCalculationDiagnosticSeverity.Error)]) : SheetCalculationResult.Success(changedSheets)); }
    }
    private sealed class EchoLocalization : ILocalizationService
    {
        public System.Globalization.CultureInfo CurrentCulture => System.Globalization.CultureInfo.InvariantCulture;
        public event EventHandler? CultureChanged { add { } remove { } }
        public string Get(LocalizationKey key) => key.Value;
        public bool TrySetCulture(string cultureName) => false;
    }
}
