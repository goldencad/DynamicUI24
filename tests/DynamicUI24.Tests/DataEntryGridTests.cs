using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;
using Xunit;
using Avalonia;
using DynamicUI24.Avalonia.Presentation;

namespace DynamicUI24.Tests;

public sealed class DataEntryGridTests
{
    private static readonly CompanyDescriptor CompanyA = new(new("a"), "A", "Company A");
    private static readonly CompanyDescriptor CompanyB = new(new("b"), "B", "Company B");
    private static readonly GridProviderContext ContextA = new(CompanyA, "workspace");

    [Fact]
    public void ExpandedCellLayoutGrowsForLongAndMultilineContentButRemainsViewportBounded()
    {
        var source = new Size(120, 32); var viewport = new Size(900, 600);
        var longText = DataEntryExpandedCellLayout.Resolve(new string('x', 240), source, viewport);
        var multiline = DataEntryExpandedCellLayout.Resolve("one\ntwo\nthree\nfour\nfive\nsix", source, viewport);
        Assert.True(longText.Width > source.Width); Assert.True(longText.Height > source.Height);
        Assert.True(multiline.Height >= 92 + 6 * 22);
        Assert.InRange(longText.Width, 320, viewport.Width * .82);
        Assert.InRange(multiline.Height, 140, viewport.Height * .68);
    }

    [Fact]
    public void ValidDefinitionReusesTask9ColumnsAndOrdersGeometry()
    {
        var definition = Definition([Column("b", "B", "B", 20, width: 900, min: 90, max: 200), Column("a", "A", "A", 10)]);
        var resolved = GridMetadataResolver.Resolve(definition, null);
        Assert.Equal(["A", "B"], resolved.Columns.Select(x => x.Definition.ColumnCode));
        Assert.Equal(200m, resolved.Columns[1].Width);
        Assert.IsType<ColumnDefinition>(resolved.Columns[0].Definition);
    }

    [Fact]
    public void DuplicateIdentifiersCodesAndVariableCodesAreDiagnosedAndExcluded()
    {
        var definition = Definition([
            Column("same", "A", "X", 0), Column("same", "B", "Y", 1),
            Column("c", "C", "Z", 2), Column("d", "C", "W", 3),
            Column("e", "E", "V", 4), Column("f", "F", "V", 5)]);
        var resolved = GridMetadataResolver.Resolve(definition, null);
        Assert.Contains(resolved.Diagnostics, x => x.Code == "GRID_DUPLICATE_COLUMN_ID");
        Assert.Contains(resolved.Diagnostics, x => x.Code == "GRID_DUPLICATE_COLUMN_CODE");
        Assert.Contains(resolved.Diagnostics, x => x.Code == "GRID_DUPLICATE_VARIABLE_CODE");
        Assert.Empty(resolved.Columns);
    }

    [Fact]
    public void FormulaSystemUnknownTypesAndInvalidGeometryFailSafely()
    {
        var formula = Column("f", "F", "F", 0, mode: ColumnMode.Formula, editor: ColumnEditorKind.TextBox);
        var system = Column("s", "S", "S", 1, mode: ColumnMode.System, editor: ColumnEditorKind.TextBox);
        var unknown = Column("u", "U", "U", 2) with { DataType = (ColumnDataType)999, EditorKind = (ColumnEditorKind)999,
            MinWidth = 300, MaxWidth = 100 };
        var resolved = GridMetadataResolver.Resolve(Definition([formula, system, unknown]), null);
        Assert.False(resolved.Columns[0].CanEdit); Assert.False(resolved.Columns[1].CanEdit);
        Assert.True(resolved.Columns[0].IsFormulaDerived); Assert.False(resolved.Columns[1].IsFormulaDerived);
        var inputWithFormulaEditor = GridMetadataResolver.Resolve(Definition([
            Column("i", "I", "I", 0, mode: ColumnMode.Input, editor: ColumnEditorKind.Formula)]), null).Columns.Single();
        Assert.False(inputWithFormulaEditor.CanEdit); Assert.False(inputWithFormulaEditor.IsFormulaDerived);
        Assert.Contains(resolved.Diagnostics, x => x.Code == "GRID_COLUMN_DATA_TYPE_UNKNOWN");
        Assert.Contains(resolved.Diagnostics, x => x.Code == "GRID_COLUMN_EDITOR_UNKNOWN");
        Assert.Contains(resolved.Diagnostics, x => x.Code == "GRID_COLUMN_WIDTH_INVALID");
    }

    [Fact]
    public void PermissionColumnIsHiddenAndFailsClosed()
    {
        var secured = Column("secret", "SECRET", "SECRET", 0) with { PermissionRequirement = "SECRET.VIEW" };
        var resolved = GridMetadataResolver.Resolve(Definition([secured]), null);
        Assert.False(resolved.Columns.Single().IsVisible);
        var authorized = Authorization(CompanyA, "SECRET.VIEW");
        Assert.True(GridMetadataResolver.Resolve(Definition([secured]), authorized).Columns.Single().IsVisible);
    }

    [Fact]
    public async Task ProviderRowsUseStableOpaqueKeysAndUnknownVariablesAreUnavailable()
    {
        var rows = Rows("a", 3); var runtime = Runtime(rows);
        await runtime.LoadAsync(ContextA, null);
        Assert.Equal(new RowKey("a:2"), runtime.Rows[1].RowKey);
        Assert.Null(runtime.GetValue(runtime.Rows[0].RowKey, new("UNKNOWN"), out var diagnostic));
        Assert.Equal("GRID_VARIABLE_UNAVAILABLE", diagnostic);
    }

    [Fact]
    public async Task DuplicateRowKeyBecomesSafeError()
    {
        var duplicate = new GridRow(new("same"), new Dictionary<VariableCode, object?> { [new("VALUE")] = "x" });
        var runtime = Runtime([duplicate, duplicate]);
        await runtime.LoadAsync(ContextA, null);
        Assert.Equal(GridProviderState.Error, runtime.State);
        Assert.Equal("GRID_DUPLICATE_ROW_KEY", runtime.DiagnosticCode);
        Assert.Empty(runtime.Rows);
    }

    [Theory]
    [InlineData(GridProviderState.Empty)]
    [InlineData(GridProviderState.Error)]
    [InlineData(GridProviderState.Unavailable)]
    public async Task ProviderPresentationStatesAreRetained(GridProviderState state)
    {
        var result = state == GridProviderState.Empty ? GridDataResult.Ready([]) : GridDataResult.Failure(state, "SAFE");
        var runtime = new DataEntryGridRuntime(Definition(), new StubProvider((_, _) => Task.FromResult(result)));
        await runtime.LoadAsync(ContextA, null);
        Assert.Equal(state, runtime.State);
    }

    [Fact]
    public async Task ProviderExceptionIsIsolated()
    {
        var runtime = new DataEntryGridRuntime(Definition(), new StubProvider((_, _) => throw new InvalidOperationException("raw")));
        await runtime.LoadAsync(ContextA, null);
        Assert.Equal(GridProviderState.Error, runtime.State);
        Assert.Equal("GRID_PROVIDER_FAILED", runtime.DiagnosticCode);
    }

    [Fact]
    public async Task RuntimePublishesLoadingBeforeAsyncProviderCompletes()
    {
        var release = new TaskCompletionSource<GridDataResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = new DataEntryGridRuntime(Definition(), new StubProvider((_, _) => release.Task));
        var load = runtime.LoadAsync(ContextA, null);
        Assert.Equal(GridProviderState.Loading, runtime.State);
        release.SetResult(GridDataResult.Ready(Rows("a", 1)));
        await load;
        Assert.Equal(GridProviderState.Ready, runtime.State);
    }

    [Fact]
    public async Task NoneSingleAndMultipleSelectionUseRowKeys()
    {
        var rows = Rows("a", 3);
        var none = Runtime(rows, GridSelectionMode.None); await none.LoadAsync(ContextA, null); none.Select(rows.Select(x => x.RowKey)); Assert.Empty(none.SelectedRowKeys);
        var single = Runtime(rows, GridSelectionMode.Single); await single.LoadAsync(ContextA, null); single.Select(rows.Select(x => x.RowKey)); Assert.Single(single.SelectedRowKeys);
        var multiple = Runtime(rows, GridSelectionMode.Multiple); await multiple.LoadAsync(ContextA, null); multiple.Select(rows.Select(x => x.RowKey)); Assert.Equal(3, multiple.SelectionCount);
    }

    [Fact]
    public async Task SelectionSurvivesSortAndFilterWhenRowRemainsVisible()
    {
        var rows = Rows("a", 3);
        var provider = new StubProvider((_, request) => Task.FromResult(GridDataResult.Ready(
            request.Sorts.FirstOrDefault()?.Direction == GridSortDirection.Descending ? rows.Reverse() : rows)));
        var runtime = new DataEntryGridRuntime(Definition(selectionMode: GridSelectionMode.Multiple), provider);
        await runtime.LoadAsync(ContextA, null); runtime.Select([rows[1].RowKey]);
        await runtime.SetSortAsync([new(new("VALUE"), GridSortDirection.Descending)], null);
        Assert.Contains(rows[1].RowKey, runtime.SelectedRowKeys);
    }

    [Fact]
    public async Task CompanySwitchClearsSelectionAndLateResponseCannotReplaceCurrentRows()
    {
        var aEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseA = new TaskCompletionSource<GridDataResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new StubProvider((context, _) =>
        {
            if (context.Company.CompanyId == CompanyA.CompanyId) { aEntered.SetResult(); return releaseA.Task; }
            return Task.FromResult(GridDataResult.Ready(Rows("b", 2)));
        });
        var runtime = new DataEntryGridRuntime(Definition(selectionMode: GridSelectionMode.Multiple), provider);
        var loadA = runtime.LoadAsync(ContextA, null); await aEntered.Task;
        var loadB = runtime.LoadAsync(new(CompanyB, "workspace"), null); await loadB;
        releaseA.SetResult(GridDataResult.Ready(Rows("a", 2))); await loadA;
        Assert.All(runtime.Rows, x => Assert.StartsWith("b:", x.RowKey.Value, StringComparison.Ordinal));
        Assert.Empty(runtime.SelectedRowKeys);
    }

    [Fact]
    public async Task InputEditUsesCandidateValidationCommitAndCancel()
    {
        var rows = Rows("a", 1); var providerCommits = 0;
        var provider = new StubProvider((_, _) => Task.FromResult(GridDataResult.Ready(rows)),
            (_, edit) => { providerCommits++; return Task.FromResult(GridCommitResult.Success(int.Parse(edit.CandidateValue!.ToString()!))); });
        var runtime = new DataEntryGridRuntime(Definition([Column("value", "VALUE", "VALUE", 0, required: true, type: ColumnDataType.Integer)]), provider);
        await runtime.LoadAsync(ContextA, null);
        Assert.True(runtime.BeginEdit(rows[0].RowKey, new("VALUE")));
        Assert.Equal("GRID_VALUE_REQUIRED", runtime.SetCandidate("")?.Code);
        Assert.Equal("GRID_VALUE_TYPE_INVALID", runtime.SetCandidate("bad")?.Code);
        Assert.Null(runtime.SetCandidate("42")); Assert.True(runtime.EditBuffer!.IsDirty);
        Assert.True((await runtime.CommitEditAsync()).IsSuccess);
        Assert.Equal("42", runtime.GetValue(rows[0].RowKey, new("VALUE"), out _));
        Assert.Equal(1, runtime.PendingChangeCount); Assert.Equal(0, providerCommits);
        Assert.True(runtime.BeginEdit(rows[0].RowKey, new("VALUE")));
        Assert.Equal("42", runtime.EditBuffer!.SourceValue);
        Assert.Equal("42", runtime.EditBuffer.CandidateValue);
        runtime.CancelEdit();
        Assert.Equal(1, (await runtime.UndoAsync()).AppliedCellCount);
        Assert.Equal("value-1", runtime.GetValue(rows[0].RowKey, new("VALUE"), out _));
        Assert.Equal(0, runtime.PendingChangeCount); Assert.Equal(0, providerCommits);
        Assert.Equal(1, (await runtime.RedoAsync()).AppliedCellCount);
        Assert.Equal("42", runtime.GetValue(rows[0].RowKey, new("VALUE"), out _));
        Assert.Equal(1, runtime.PendingChangeCount); Assert.Equal(0, providerCommits);
        runtime.BeginEdit(rows[0].RowKey, new("VALUE")); runtime.SetCandidate("77"); runtime.CancelEdit();
        Assert.Equal("42", runtime.GetValue(rows[0].RowKey, new("VALUE"), out _));
        Assert.True((await runtime.SavePendingChangesAsync()).IsSuccess);
        Assert.Equal(1, providerCommits); Assert.Equal(0, runtime.PendingChangeCount);
    }

    [Fact]
    public async Task CellCommitIdentifiesChangedSemanticCellAndPendingValueSurvivesRematerialization()
    {
        var rows = Rows("a", 1); GridCellAddress? invalidated = null; var providerCommits = 0;
        var provider = new StubProvider((_, _) => Task.FromResult(GridDataResult.Ready(rows)),
            (_, edit) => { providerCommits++; return Task.FromResult(GridCommitResult.Success(edit.CandidateValue)); });
        var runtime = new DataEntryGridRuntime(Definition([Column("value", "VALUE", "VALUE", 0)]), provider);
        await runtime.LoadAsync(ContextA, null);
        runtime.Changed += (_, args) => { if (args.Reason == "EDIT_COMMIT") invalidated = args.Cell; };
        var address = new GridCellAddress(rows[0].RowKey, new("VALUE"));

        Assert.True(runtime.BeginEdit(address.RowKey, address.VariableCode));
        runtime.SetCandidate("pending");
        Assert.True((await runtime.CommitEditAsync()).IsSuccess);
        Assert.Equal(address, invalidated);
        Assert.Equal("pending", runtime.GetValue(address.RowKey, address.VariableCode, out _));
        Assert.Equal(0, providerCommits);

        await runtime.LoadAsync(ContextA, null);
        Assert.Equal("pending", runtime.GetValue(address.RowKey, address.VariableCode, out _));
        Assert.True((await runtime.SavePendingChangesAsync()).IsSuccess);
        Assert.Equal("pending", runtime.GetValue(address.RowKey, address.VariableCode, out _));
        Assert.Equal(1, providerCommits);
    }

    [Fact]
    public async Task UnicodeEditorCandidateRemainsUnchangedUntilCellCommit()
    {
        var rows = Rows("a", 1); var providerCommits = 0;
        var provider = new StubProvider((_, _) => Task.FromResult(GridDataResult.Ready(rows)),
            (_, edit) => { providerCommits++; return Task.FromResult(GridCommitResult.Success(edit.CandidateValue)); });
        var runtime = new DataEntryGridRuntime(Definition([Column("value", "VALUE", "VALUE", 0)]), provider);
        await runtime.LoadAsync(ContextA, null);
        Assert.True(runtime.BeginEdit(rows[0].RowKey, new("VALUE")));
        const string composed = "Tiếng Việt — Nguyễn Văn Anh — 日本語 — 한국어 — العربية";
        Assert.Null(runtime.SetCandidate(composed));
        Assert.Equal(composed, runtime.EditBuffer!.CandidateValue);
        Assert.Equal("value-1", runtime.GetValue(rows[0].RowKey, new("VALUE"), out _));
        Assert.Equal(0, providerCommits);
        Assert.True((await runtime.CommitEditAsync()).IsSuccess);
        Assert.Equal(composed, runtime.GetValue(rows[0].RowKey, new("VALUE"), out _));
        Assert.Equal(1, runtime.PendingChangeCount); Assert.Equal(0, providerCommits);
    }

    [Fact]
    public async Task PercentageSizingChangesPresentationWithoutProviderReloadOrMaterialization()
    {
        var rows = Rows("a", 3); var loads = 0;
        var provider = new StubProvider((_, _) => { loads++; return Task.FromResult(GridDataResult.Ready(rows)); });
        var runtime = new DataEntryGridRuntime(Definition([Column("value", "VALUE", "VALUE", 0, width: 120,
            min: 40, max: 400)]), provider);
        await runtime.LoadAsync(ContextA, null);
        var materialized = runtime.Rows.Length;
        Assert.True(runtime.SetColumnWidthPercentage(new("VALUE"), 150));
        runtime.SetRowHeightPercentage(125);
        Assert.Equal(180, runtime.PresentedColumns.Single().Width);
        Assert.Equal(47.5m, runtime.ResolveRowHeight(rows[0].RowKey, 38m));
        Assert.Equal(1, loads); Assert.Equal(materialized, runtime.Rows.Length);
    }

    [Fact]
    public async Task CellContextPreservesContainingRangeAndActivatesOutsideCellWithoutProviderWork()
    {
        var rows = Rows("a", 3); var loads = 0;
        var provider = new StubProvider((_, _) => { loads++; return Task.FromResult(GridDataResult.Ready(rows)); });
        var runtime = new DataEntryGridRuntime(Definition(selectionMode: GridSelectionMode.Multiple), provider);
        await runtime.LoadAsync(ContextA, null);
        var host = new DataEntryGridHost(runtime, new DictionaryLocalizationService("en-US"));
        var first = new GridCellAddress(rows[0].RowKey, new("VALUE"));
        var second = new GridCellAddress(rows[1].RowKey, new("VALUE"));
        var outside = new GridCellAddress(rows[2].RowKey, new("VALUE"));
        runtime.SelectCell(first, 0); runtime.SelectCell(second, 1, extend: true);
        var selected = runtime.CellSelection;
        Assert.False(host.PrepareCellContext(second, 1));
        Assert.Equal(selected, runtime.CellSelection);
        Assert.True(host.PrepareCellContext(outside, 2));
        Assert.Equal(outside, runtime.ActiveCell);
        Assert.Single(runtime.SelectedRanges); Assert.Equal(1, runtime.SelectedRanges[0].RowCount);
        Assert.Equal(1, loads); Assert.Equal(3, runtime.Rows.Length);
    }

    [Fact]
    public async Task FormulaSystemAndPermissionReadOnlyCannotEdit()
    {
        var columns = new[] { Column("f", "F", "F", 0, mode: ColumnMode.Formula), Column("s", "S", "S", 1, mode: ColumnMode.System),
            Column("p", "P", "P", 2) with { PermissionRequirement = "DATA.EDIT" } };
        var row = new GridRow(new("r"), columns.ToDictionary(x => x.VariableCode, _ => (object?)1));
        var runtime = new DataEntryGridRuntime(Definition(columns), new StubProvider((_, _) => Task.FromResult(GridDataResult.Ready([row]))));
        await runtime.LoadAsync(ContextA, null);
        Assert.False(runtime.BeginEdit(row.RowKey, new("F"))); Assert.False(runtime.BeginEdit(row.RowKey, new("S")));
        Assert.False(runtime.BeginEdit(row.RowKey, new("P")));
    }

    [Fact]
    public async Task ActiveCandidateSurvivesSafeAuthorizationReresolution()
    {
        var rows = Rows("a", 1); var runtime = Runtime(rows); await runtime.LoadAsync(ContextA, null);
        runtime.BeginEdit(rows[0].RowKey, new("VALUE")); runtime.SetCandidate("draft");
        runtime.UpdateAuthorization(Authorization(CompanyA));
        Assert.Equal("draft", runtime.EditBuffer?.CandidateValue);
    }

    private static DataEntryGridRuntime Runtime(ImmutableArray<GridRow> rows, GridSelectionMode mode = GridSelectionMode.Multiple) =>
        new(Definition(selectionMode: mode), new StubProvider((_, _) => Task.FromResult(GridDataResult.Ready(rows))));

    private static GridDefinition Definition(IEnumerable<ColumnDefinition>? columns = null,
        GridSelectionMode selectionMode = GridSelectionMode.Single) =>
        new("grid", "GRID", columns ?? [Column("value", "VALUE", "VALUE", 0)], selectionMode: selectionMode, allowEdit: true);

    private static ColumnDefinition Column(string id, string code, string variable, int order, decimal width = 120,
        decimal min = 60, decimal max = 300, bool required = false, ColumnDataType type = ColumnDataType.Text,
        ColumnMode mode = ColumnMode.Input, ColumnEditorKind editor = ColumnEditorKind.TextBox) =>
        new(id, code, new(variable), $"Grid.{code}", null, type, editor, mode, order, width, min, max, true,
            required, null, null, null, null, null, 1, SetupDefinitionStatus.Published);

    private static ImmutableArray<GridRow> Rows(string prefix, int count) => Enumerable.Range(1, count)
        .Select(x => new GridRow(new($"{prefix}:{x}"), new Dictionary<VariableCode, object?> { [new("VALUE")] = $"value-{x}" })).ToImmutableArray();

    private static EffectiveAuthorizationContext Authorization(CompanyDescriptor company, params string[] permissions) =>
        new(new("user"), company.CompanyId, permissions.Select(x => new PermissionCode(x)), [], "1");

    private sealed class StubProvider(
        Func<GridProviderContext, GridDataRequest, Task<GridDataResult>> load,
        Func<GridProviderContext, GridCellEdit, Task<GridCommitResult>>? commit = null) : IDataEntryGridProvider
    {
        public Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request, CancellationToken cancellationToken = default) => load(context, request);
        public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit, CancellationToken cancellationToken = default) =>
            commit?.Invoke(context, edit) ?? Task.FromResult(GridCommitResult.Success(edit.CandidateValue));
    }
}
