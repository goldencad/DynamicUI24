using System.Collections.Immutable;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;
using DynamicUI24.Core.Privacy;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class GridEditingTests
{
    private static readonly CompanyDescriptor CompanyA = new(new("a"), "A", "Company A");
    private static readonly CompanyDescriptor CompanyB = new(new("b"), "B", "Company B");
    private static readonly GridProviderContext ContextA = new(CompanyA, "workspace");

    [Fact]
    public void ClipboardMatrixPreservesTabsEmptyCellsAndNormalizesRows()
    {
        var matrix = ClipboardMatrix.Parse("a\t\tb\r\n1\t2\t\r\n");
        Assert.Equal(2, matrix.RowCount); Assert.Equal(3, matrix.ColumnCount);
        Assert.Equal("", matrix[0, 1]); Assert.Equal("", matrix[1, 2]);
        Assert.True(ClipboardMatrix.Parse(null).IsEmpty);
    }

    [Fact]
    public async Task ActiveAnchorShiftRangeAndNavigationUseSemanticCellIdentity()
    {
        var provider = Provider(4);
        var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        var a1 = Address(provider.Rows[0], "TEXT");
        Assert.True(runtime.SelectCell(a1, 0));
        Assert.True(runtime.MoveActiveCell(1, 1, extend: true));
        Assert.Equal(a1, runtime.AnchorCell?.Address);
        Assert.Equal(new VariableCode("NUMBER"), runtime.ActiveCell?.VariableCode);
        Assert.Equal(4, runtime.SelectedCellCount);
        Assert.Single(runtime.SelectedRanges);
        Assert.True(runtime.IsCellSelected(Address(provider.Rows[1], "NUMBER")));
    }

    [Fact]
    public async Task CopySingleAndRectangleUseCurrentVisibleOrder()
    {
        var provider = Provider(3); var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        runtime.SelectCell(Address(provider.Rows[0], "TEXT"), 0);
        runtime.SelectCell(Address(provider.Rows[1], "NUMBER"), 1, extend: true);
        var copy = await runtime.BuildCopyTextAsync();
        Assert.Null(copy.Diagnostic);
        Assert.Equal("row-1\t1\nrow-2\t2", copy.Result);
    }

    [Fact]
    public async Task PointerAdapterReplacesOneCompactSemanticRangeAndRetainsModifierRanges()
    {
        var provider = Provider(4); var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        var anchor = new GridRangeEndpoint(Address(provider.Rows[0], "TEXT"), 0);
        Assert.True(runtime.SelectRange(anchor, new(Address(provider.Rows[1], "NUMBER"), 1)));
        Assert.True(runtime.SelectRange(anchor, new(Address(provider.Rows[2], "NUMBER"), 2)));
        Assert.Single(runtime.SelectedRanges); Assert.Equal(6, runtime.SelectedCellCount);
        Assert.Equal("row-1\t1\nrow-2\t2\nrow-3\t3", (await runtime.BuildCopyTextAsync()).Result);

        var retained = runtime.SelectedRanges;
        var second = new GridRangeEndpoint(Address(provider.Rows[3], "TEXT"), 3);
        Assert.True(runtime.SelectRange(second, second, retained));
        Assert.Equal(2, runtime.SelectedRanges.Length);
        Assert.Equal(second.Address, runtime.ActiveCell);
        Assert.Equal(second, runtime.AnchorCell);
    }

    [Fact]
    public async Task RowHeightOverridesAreSemanticClampedResettableAndBounded()
    {
        var provider = Provider(4);
        var runtime = new DataEntryGridRuntime(Definition(), provider,
            rowHeightOptions: new(38, 24, 240, 2));
        await runtime.LoadAsync(ContextA, null);
        runtime.SelectCell(Address(provider.Rows[0], "TEXT"), 0);
        var active = runtime.ActiveCell;

        Assert.True(runtime.ResizeRow(provider.Rows[0].RowKey, 999));
        Assert.True(runtime.ResizeRow(provider.Rows[1].RowKey, 1));
        Assert.Equal(240, runtime.GetRowHeight(provider.Rows[0].RowKey));
        Assert.Equal(24, runtime.GetRowHeight(provider.Rows[1].RowKey));
        Assert.True(runtime.ResizeRow(provider.Rows[2].RowKey, 80));
        Assert.Equal(2, runtime.RowHeightOverrideCount);
        Assert.False(runtime.TryGetRowHeight(provider.Rows[0].RowKey, out _));
        Assert.True(runtime.ResetRowHeight(provider.Rows[1].RowKey));
        Assert.Equal(38, runtime.GetRowHeight(provider.Rows[1].RowKey));
        Assert.Equal(active, runtime.ActiveCell);
        Assert.Equal(4, runtime.Rows.Length);
        Assert.Equal(0, runtime.PendingChangeCount);
    }

    [Fact]
    public async Task GridRowHeightPercentageUsesDensityBaseAndSparseOverridePrecedence()
    {
        var provider = Provider(4); var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        runtime.SetRowHeightPercentage(125);
        Assert.Equal(47.5m, runtime.ResolveRowHeight(provider.Rows[0].RowKey, 38m));
        runtime.IncreaseRowHeight(); Assert.Equal(135, runtime.RowHeightScalePercent);
        runtime.DecreaseRowHeight(); Assert.Equal(125, runtime.RowHeightScalePercent);
        Assert.True(runtime.ResizeRow(provider.Rows[0].RowKey, 80));
        Assert.Equal(80, runtime.ResolveRowHeight(provider.Rows[0].RowKey, 38m));
        Assert.Equal(1, runtime.RowHeightOverrideCount);
        runtime.SetRowHeightPercentage(999); Assert.Equal(300, runtime.RowHeightScalePercent);
        runtime.ResetRowHeightPercentage(); Assert.Equal(100, runtime.RowHeightScalePercent);
        Assert.Equal(1, runtime.RowHeightOverrideCount);
    }

    [Fact]
    public async Task PasteSupportsSingleFillMatchingAndExactTileThenUndoRedo()
    {
        var provider = Provider(4); var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        runtime.SelectCell(Address(provider.Rows[0], "TEXT"), 0);
        runtime.SelectCell(Address(provider.Rows[1], "TEXT"), 1, extend: true);
        var fill = await runtime.PasteTextAsync("filled");
        Assert.Equal(2, fill.AppliedCellCount); Assert.True(fill.WasAtomic);
        Assert.Equal("filled", runtime.GetValue(provider.Rows[1].RowKey, new("TEXT"), out _));
        Assert.True(runtime.CanUndo);
        Assert.Equal(2, (await runtime.UndoAsync()).AppliedCellCount);
        Assert.Equal("row-2", runtime.GetValue(provider.Rows[1].RowKey, new("TEXT"), out _));
        Assert.Equal(2, (await runtime.RedoAsync()).AppliedCellCount);

        runtime.SelectCell(Address(provider.Rows[0], "TEXT"), 0);
        runtime.SelectCell(Address(provider.Rows[3], "NUMBER"), 3, extend: true);
        var tile = await runtime.PasteTextAsync("a\t7\nb\t8");
        Assert.Equal(8, tile.AppliedCellCount);
        Assert.Equal("a", runtime.GetValue(provider.Rows[2].RowKey, new("TEXT"), out _));
    }

    [Fact]
    public async Task PasteRejectsIncompatibleShapeAndAtomicInvalidOrReadOnlyTargets()
    {
        var provider = Provider(3); var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        runtime.SelectCell(Address(provider.Rows[0], "TEXT"), 0);
        runtime.SelectCell(Address(provider.Rows[2], "NUMBER"), 2, extend: true);
        Assert.Equal("GRID_PASTE_SHAPE_INCOMPATIBLE", (await runtime.PasteTextAsync("a\tb\nc\td")).DiagnosticCode);
        Assert.Equal("GRID_PASTE_ATOMIC_REJECTED", (await runtime.PasteTextAsync("ok\tbad\nok\t2\nok\t3")).DiagnosticCode);

        runtime.SelectCell(Address(provider.Rows[0], "NUMBER"), 0);
        runtime.SelectCell(Address(provider.Rows[0], "FORMULA"), 0, extend: true);
        var readOnly = await runtime.PasteTextAsync("1\t2");
        Assert.Equal("GRID_PASTE_ATOMIC_REJECTED", readOnly.DiagnosticCode);
        Assert.Equal(0, readOnly.AppliedCellCount);
    }

    [Fact]
    public async Task PartialValidPolicyAppliesKnownGoodCellsWithExplicitDiagnostics()
    {
        var provider = Provider(1);
        var runtime = Runtime(provider, new(PasteCommitMode.PartialValid)); await runtime.LoadAsync(ContextA, null);
        runtime.SelectCell(Address(provider.Rows[0], "NUMBER"), 0);
        runtime.SelectCell(Address(provider.Rows[0], "FORMULA"), 0, extend: true);
        var result = await runtime.PasteTextAsync("42\tblocked");
        Assert.Equal(1, result.AppliedCellCount); Assert.Equal(1, result.RejectedCellCount);
        Assert.True(result.WasPartial); Assert.Single(result.ValidationErrors);
    }

    [Fact]
    public async Task ClearAndCutRespectRequiredAndClipboardFailures()
    {
        var provider = Provider(1); var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        runtime.SelectCell(Address(provider.Rows[0], "TEXT"), 0);
        Assert.Equal("GRID_CLEAR_ATOMIC_REJECTED", (await runtime.ClearSelectedCellsAsync()).DiagnosticCode);
        var unavailable = new MemoryClipboard { Throw = true };
        Assert.Equal("GRID_CLIPBOARD_UNAVAILABLE", (await runtime.CutAsync(unavailable)).DiagnosticCode);
        Assert.Equal("row-1", runtime.GetValue(provider.Rows[0].RowKey, new("TEXT"), out _));
    }

    [Fact]
    public async Task LargePasteAndSymbolicSelectAllRequireConfirmationWithoutMaterialization()
    {
        var provider = Provider(20);
        var runtime = Runtime(provider, new(largeTargetThreshold: 10)); await runtime.LoadAsync(ContextA, null);
        runtime.SelectCell(Address(provider.Rows[0], "TEXT"), 0);
        runtime.SelectCell(Address(provider.Rows[10], "TEXT"), 10, extend: true);
        var result = await runtime.PasteTextAsync("x");
        Assert.True(result.RequiresConfirmation); Assert.Equal("GRID_PASTE_REQUIRES_CONFIRMATION", result.DiagnosticCode);
        runtime.SelectAllCells();
        Assert.True(runtime.CellSelection.IsAllSelected); Assert.Empty(runtime.SelectedRanges);
        Assert.True((await runtime.BuildCopyTextAsync()).Diagnostic?.RequiresConfirmation);
    }

    [Fact]
    public async Task NewEditClearsRedoAndHistoryDepthIsBounded()
    {
        var provider = Provider(1); var runtime = Runtime(provider, new(historyDepth: 2)); await runtime.LoadAsync(ContextA, null);
        runtime.SelectCell(Address(provider.Rows[0], "NUMBER"), 0);
        await runtime.PasteTextAsync("2"); await runtime.PasteTextAsync("3"); await runtime.PasteTextAsync("4");
        await runtime.UndoAsync(); Assert.True(runtime.CanRedo);
        await runtime.PasteTextAsync("5"); Assert.False(runtime.CanRedo);
        Assert.True((await runtime.UndoAsync()).AppliedCellCount > 0);
        Assert.True((await runtime.UndoAsync()).AppliedCellCount > 0);
        Assert.Equal("GRID_UNDO_EMPTY", (await runtime.UndoAsync()).DiagnosticCode);
    }

    [Fact]
    public async Task LateBatchResultCannotMutateNewCompanyPresentation()
    {
        var release = new TaskCompletionSource<GridBatchCommitResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = Provider(2); provider.BatchOverride = (_, _, _) => release.Task;
        var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        runtime.SelectCell(Address(provider.Rows[0], "NUMBER"), 0);
        var paste = runtime.PasteTextAsync("99");
        await runtime.LoadAsync(new(CompanyB, "workspace"), null);
        release.SetResult(GridBatchCommitResult.Success);
        var result = await paste;
        Assert.Equal("GRID_STALE_EDIT_RESULT", result.DiagnosticCode);
        Assert.All(runtime.Rows, row => Assert.StartsWith("b:", row.RowKey.Value, StringComparison.Ordinal));
        Assert.Null(runtime.ActiveCell);
    }

    [Fact]
    public async Task VirtualRangeAcrossWindowsResolvesRowsWithoutExpandingSelectionState()
    {
        var provider = new VirtualProvider();
        var runtime = new DataEntryGridRuntime(Definition(), provider, new(5, 0, 0, 2, 10));
        await runtime.LoadAsync(ContextA, null);
        runtime.SelectCell(new(new("a:1"), new("TEXT")), 0);
        await runtime.RequestViewportAsync(5, 5);
        runtime.SelectCell(new(new("a:7"), new("TEXT")), 6, extend: true);
        Assert.Single(runtime.SelectedRanges); Assert.Equal(7, runtime.SelectedRanges[0].RowCount);
        var result = await runtime.PasteTextAsync("cross-window");
        Assert.Equal(7, result.AppliedCellCount); Assert.Equal(7, provider.LastBatchSize);
        Assert.InRange(runtime.CachedRowCount, 1, 10);
    }

    [Fact]
    public async Task FormulaAndSystemRemainSelectableCopyableButAllMutationPathsRejectThem()
    {
        var provider = Provider(3); var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        Assert.True(runtime.BeginEdit(provider.Rows[0].RowKey, new("TEXT")));
        runtime.CancelEdit();
        Assert.False(runtime.BeginEdit(provider.Rows[0].RowKey, new("FORMULA")));
        Assert.False(runtime.BeginEdit(provider.Rows[0].RowKey, new("SYSTEM")));

        runtime.SelectCell(Address(provider.Rows[0], "FORMULA"), 0);
        Assert.Equal(1, runtime.SelectedCellCount);
        var formulaCopy = await runtime.BuildCopyTextAsync();
        Assert.Null(formulaCopy.Diagnostic);
        Assert.DoesNotContain("10", formulaCopy.Result!);
        var clipboard = new MemoryClipboard();
        var cut = await runtime.CutAsync(clipboard);
        Assert.Equal("GRID_CLEAR_ATOMIC_REJECTED", cut.DiagnosticCode);
        Assert.Equal(10, runtime.GetValue(provider.Rows[0].RowKey, new("FORMULA"), out _));
        Assert.Equal("GRID_CLEAR_ATOMIC_REJECTED", (await runtime.ClearSelectedCellsAsync()).DiagnosticCode);

        runtime.SelectCell(Address(provider.Rows[0], "FORMULA"), 0);
        runtime.SelectCell(Address(provider.Rows[1], "FORMULA"), 1, extend: true);
        Assert.Equal("GRID_FILL_ATOMIC_REJECTED", (await runtime.FillDownAsync()).DiagnosticCode);
        Assert.Equal(20, runtime.GetValue(provider.Rows[1].RowKey, new("FORMULA"), out _));

        runtime.SelectCell(Address(provider.Rows[0], "TEXT"), 0);
        runtime.SelectCell(Address(provider.Rows[0], "SYSTEM"), 0, extend: true);
        var atomic = await runtime.PasteTextAsync("ok\t2\t999\toverwrite");
        Assert.Equal("GRID_PASTE_ATOMIC_REJECTED", atomic.DiagnosticCode);
        Assert.Equal("row-1", runtime.GetValue(provider.Rows[0].RowKey, new("TEXT"), out _));

        var partialProvider = Provider(1);
        var partial = Runtime(partialProvider, new(PasteCommitMode.PartialValid)); await partial.LoadAsync(ContextA, null);
        partial.SelectCell(Address(partialProvider.Rows[0], "TEXT"), 0);
        partial.SelectCell(Address(partialProvider.Rows[0], "SYSTEM"), 0, extend: true);
        var partialResult = await partial.PasteTextAsync("changed\t7\t999\toverwrite");
        Assert.Equal(2, partialResult.AppliedCellCount); Assert.Equal(2, partialResult.RejectedCellCount);
        Assert.Equal(10, partial.GetValue(partialProvider.Rows[0].RowKey, new("FORMULA"), out _));
        Assert.Equal("system-1", partial.GetValue(partialProvider.Rows[0].RowKey, new("SYSTEM"), out _));
    }

    private static DataEntryGridRuntime Runtime(EditingProvider provider, GridPasteOptions? options = null) =>
        new(Definition(), provider, pasteOptions: options);
    private static GridCellAddress Address(GridRow row, string variable) => new(row.RowKey, new(variable));
    private static EditingProvider Provider(int count) => new(count);
    private static GridDefinition Definition() => new("editing", "EDITING", [
        Column("text", "TEXT", ColumnDataType.Text, 0, required: true),
        Column("number", "NUMBER", ColumnDataType.Integer, 1),
        Column("formula", "FORMULA", ColumnDataType.Formula, 2, mode: ColumnMode.Formula, editor: ColumnEditorKind.Formula,
            sensitive: new(Sensitivity.Restricted, PrivacyPresentation.Mask)),
        Column("system", "SYSTEM", ColumnDataType.System, 3, mode: ColumnMode.System, editor: ColumnEditorKind.ReadOnly),
    ], selectionMode: GridSelectionMode.Multiple, allowEdit: true);
    private static ColumnDefinition Column(string id, string variable, ColumnDataType type, int order,
        bool required = false, ColumnMode mode = ColumnMode.Input, ColumnEditorKind editor = ColumnEditorKind.TextBox,
        SensitiveContentDefinition? sensitive = null) =>
        new(id, variable, new(variable), $"Grid.{variable}", null, type, editor, mode, order, 100, 60, 200,
            true, required, null, null, null, null, null, 1, SetupDefinitionStatus.Published, sensitive);

    private sealed class EditingProvider : IDataEntryGridProvider, IGridBatchEditProvider
    {
        private readonly Dictionary<(string Company, RowKey Row, VariableCode Variable), object?> values = [];
        public EditingProvider(int count) => Rows = Enumerable.Range(1, count).Select(index => Row("a", index)).ToImmutableArray();
        public ImmutableArray<GridRow> Rows { get; private set; }
        public Func<GridProviderContext, GridEditTransaction, CancellationToken, Task<GridBatchCommitResult>>? BatchOverride { get; set; }
        public Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request, CancellationToken cancellationToken = default)
        {
            var rows = Enumerable.Range(1, Rows.Length).Select(index => Row(context.Company.CompanyId.Value, index)).ToImmutableArray();
            Rows = rows; return Task.FromResult(GridDataResult.Ready(rows));
        }
        public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit, CancellationToken cancellationToken = default) =>
            Task.FromResult(GridCommitResult.Success(edit.CandidateValue));
        public Task<GridBatchCommitResult> CommitBatchAsync(GridProviderContext context, GridEditTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            if (BatchOverride is not null) return BatchOverride(context, transaction, cancellationToken);
            foreach (var change in transaction.CellChanges) values[(context.Company.CompanyId.Value, change.RowKey, change.VariableCode)] = change.CandidateValue;
            return Task.FromResult(GridBatchCommitResult.Success);
        }
        private GridRow Row(string company, int index)
        {
            var key = new RowKey($"{company}:{index}");
            object? Value(string variable, object? fallback) => values.GetValueOrDefault((company, key, new(variable)), fallback);
            return new(key, new Dictionary<VariableCode, object?> {
                [new("TEXT")] = Value("TEXT", $"row-{index}"), [new("NUMBER")] = Value("NUMBER", index),
                [new("FORMULA")] = index * 10,
                [new("SYSTEM")] = $"system-{index}",
            });
        }
    }

    private sealed class VirtualProvider : IVirtualizedGridDataProvider, IGridLogicalRowProvider, IGridBatchEditProvider
    {
        public int LastBatchSize { get; private set; }
        public Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit, CancellationToken cancellationToken = default) =>
            Task.FromResult(GridCommitResult.Success(edit.CandidateValue));
        public Task<GridViewportResult> LoadViewportAsync(GridProviderContext context, GridViewportRequest request,
            CancellationToken cancellationToken = default) => Task.FromResult(GridViewportResult.Ready(request,
                MakeRows(context, request.MaterializedStartIndex, Math.Min(request.MaterializedRowCount, 20 - request.MaterializedStartIndex)), 20));
        public Task<ImmutableArray<GridRow>> ResolveRowsAsync(GridProviderContext context, int startPosition, int rowCount,
            ImmutableArray<GridSortDefinition> sorts, ImmutableArray<GridFilterDefinition> filters, long requestGeneration,
            CancellationToken cancellationToken = default) => Task.FromResult(MakeRows(context, startPosition, rowCount));
        public Task<GridBatchCommitResult> CommitBatchAsync(GridProviderContext context, GridEditTransaction transaction,
            CancellationToken cancellationToken = default)
        { LastBatchSize = transaction.CellChanges.Length; return Task.FromResult(GridBatchCommitResult.Success); }
        private static ImmutableArray<GridRow> MakeRows(GridProviderContext context, int start, int count) =>
            Enumerable.Range(start + 1, count).Select(index => new GridRow(new($"{context.Company.CompanyId.Value}:{index}"),
                new Dictionary<VariableCode, object?> { [new("TEXT")] = $"row-{index}", [new("NUMBER")] = index,
                    [new("FORMULA")] = index * 10, [new("SYSTEM")] = $"system-{index}" })).ToImmutableArray();
    }

    private sealed class MemoryClipboard : IGridClipboardService
    {
        public bool Throw { get; init; }
        public string? Text { get; set; }
        public Task<string?> ReadTextAsync(CancellationToken cancellationToken = default) => Throw
            ? Task.FromException<string?>(new InvalidOperationException()) : Task.FromResult(Text);
        public Task WriteTextAsync(string text, CancellationToken cancellationToken = default)
        { if (Throw) throw new InvalidOperationException(); Text = text; return Task.CompletedTask; }
    }
}
