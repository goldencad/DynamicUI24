using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.Setup;
using DynamicUI24.Core.Privacy;

namespace DynamicUI24.Core.DataEntry;

public sealed partial class DataEntryGridRuntime
{
    public GridCellAddress? ActiveCell => CellSelection.ActiveCell;
    public GridRangeEndpoint? AnchorCell => CellSelection.AnchorCell;
    public ImmutableArray<GridCellRange> SelectedRanges => CellSelection.SelectedRanges;

    public bool SelectCell(GridCellAddress address, int logicalRowPosition, bool extend = false, bool additive = false)
    {
        if (!IsVisibleColumn(address.VariableCode) || !IsKnownRow(address.RowKey, logicalRowPosition)) return false;
        var endpoint = new GridRangeEndpoint(address, logicalRowPosition);
        var anchor = extend ? CellSelection.AnchorCell ?? endpoint : endpoint;
        var range = new GridCellRange(anchor, endpoint);
        var ranges = additive && !CellSelection.SelectedRanges.IsDefaultOrEmpty
            ? CellSelection.SelectedRanges.Add(range)
            : ImmutableArray.Create(range);
        CellSelection = new(address, anchor, ranges, SelectedRowKeys,
            extend || range.RowCount > 1 || RangeColumnCount(range) > 1 ? GridCellSelectionMode.Range : GridCellSelectionMode.Cell);
        OnChanged("CELL_SELECTION");
        return true;
    }

    public bool MoveActiveCell(int rowDelta, int columnDelta, bool extend = false)
    {
        var columns = VisibleVariableCodes();
        if (Rows.Length == 0 || columns.Length == 0) return false;
        var active = CellSelection.ActiveCell;
        var rowIndex = active is null ? 0 : Rows.FindIndex(x => x.RowKey == active.Value.RowKey);
        var columnIndex = active is null ? 0 : columns.IndexOf(active.Value.VariableCode);
        if (rowIndex < 0 || columnIndex < 0) return false;
        rowIndex = Math.Clamp(rowIndex + rowDelta, 0, Rows.Length - 1);
        columnIndex = Math.Clamp(columnIndex + columnDelta, 0, columns.Length - 1);
        return SelectCell(new(Rows[rowIndex].RowKey, columns[columnIndex]), ViewportStartIndex + rowIndex, extend);
    }

    public bool MoveToNextCell(bool backwards = false, bool editableOnly = true, bool extend = false)
    {
        var columns = VisibleVariableCodes();
        if (Rows.Length == 0 || columns.Length == 0) return false;
        var active = CellSelection.ActiveCell;
        var row = active is null ? 0 : Math.Max(0, Rows.FindIndex(x => x.RowKey == active.Value.RowKey));
        var column = active is null ? (backwards ? columns.Length : -1) : columns.IndexOf(active.Value.VariableCode);
        var direction = backwards ? -1 : 1;
        for (var attempts = 0; attempts < Rows.Length * columns.Length; attempts++)
        {
            column += direction;
            if (column >= columns.Length) { column = 0; row++; }
            if (column < 0) { column = columns.Length - 1; row--; }
            if (row < 0 || row >= Rows.Length) return false;
            if (!editableOnly || CanEditColumn(columns[column]))
                return SelectCell(new(Rows[row].RowKey, columns[column]), ViewportStartIndex + row, extend);
        }
        return false;
    }

    public void SelectAllCells()
    {
        if (TotalRows <= 0 || VisibleVariableCodes().Length == 0) return;
        var active = CellSelection.ActiveCell ?? (Rows.Length == 0 ? null : new GridCellAddress(Rows[0].RowKey, VisibleVariableCodes()[0]));
        CellSelection = new(active, active is null ? null : new(active.Value, ViewportStartIndex), [], SelectedRowKeys,
            GridCellSelectionMode.Range, true);
        OnChanged("CELL_SELECT_ALL");
    }

    public void ClearCellSelection()
    {
        if (!CellSelection.HasCellSelection) return;
        CellSelection = new(null, null, [], SelectedRowKeys, GridCellSelectionMode.Row);
        OnChanged("CELL_SELECTION_CLEAR");
    }

    public bool IsCellSelected(GridCellAddress address)
    {
        if (CellSelection.IsAllSelected) return IsVisibleColumn(address.VariableCode);
        var local = Rows.FindIndex(x => x.RowKey == address.RowKey);
        if (local < 0) return false;
        var position = ViewportStartIndex + local;
        var columns = VisibleVariableCodes();
        return CellSelection.SelectedRanges.Any(x => x.Contains(address, position, columns));
    }

    public bool CanClearCellSelection()
    {
        if (!CellSelection.HasCellSelection || CellSelection.IsAllSelected) return false;
        var range = EffectivePrimaryRange();
        if (range is null || range.RowCount > Rows.Length) return false;
        var bounds = range.ResolveColumnBounds(VisibleVariableCodes());
        return bounds is { } value && Enumerable.Range(value.Start, value.End - value.Start + 1)
            .Any(index => CanEditColumn(VisibleVariableCodes()[index]));
    }

    public async Task<GridPasteResult> CopyAsync(IGridClipboardService clipboard,
        CultureInfo? culture = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clipboard);
        try
        {
            var text = await BuildCopyTextAsync(culture, cancellationToken).ConfigureAwait(false);
            if (text.Result is not null) await clipboard.WriteTextAsync(text.Result, cancellationToken).ConfigureAwait(false);
            return text.Diagnostic ?? new(0, 0, [], [], true, false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return GridPasteResult.Rejected("GRID_CLIPBOARD_UNAVAILABLE"); }
    }

    public async Task<(string? Result, GridPasteResult? Diagnostic)> BuildCopyTextAsync(
        CultureInfo? culture = null, CancellationToken cancellationToken = default)
    {
        if (!CellSelection.HasCellSelection) return (null, GridPasteResult.Rejected("GRID_SELECTION_EMPTY"));
        if (CellSelection.IsAllSelected)
            return (null, GridPasteResult.Rejected("GRID_COPY_REQUIRES_CONFIRMATION", checked(TotalRows * VisibleVariableCodes().Length),
                requiresConfirmation: true));
        var range = EffectivePrimaryRange();
        if (range is null) return (null, GridPasteResult.Rejected("GRID_SELECTION_EMPTY"));
        var columns = VisibleColumnsIn(range);
        var cellCount = (long)range.RowCount * columns.Length;
        if (cellCount > PasteOptions.LargeTargetThreshold)
            return (null, GridPasteResult.Rejected("GRID_COPY_REQUIRES_CONFIRMATION", (int)Math.Min(int.MaxValue, cellCount),
                requiresConfirmation: true));
        var rows = await ResolveRowsAsync(range.MinimumRowPosition, range.RowCount, cancellationToken).ConfigureAwait(false);
        if (rows.Length != range.RowCount) return (null, GridPasteResult.Rejected("GRID_RANGE_ROWS_UNAVAILABLE"));
        var values = rows.Select(row => columns.Select(column =>
        {
            var fieldKey = $"{row.RowKey}:{column.Definition.VariableCode}";
            var resolution = privacyResolver.Resolve(new(true, column.Definition.SensitiveContent,
                privacyState.RequestedMode, CompanyId: context?.Company.CompanyId, WorkspaceId: context?.WorkspaceId,
                IsTemporarilyRevealed: privacyState.IsRevealed(fieldKey, privacyState.Generation),
                Generation: privacyState.Generation));
            return new PrivacyClipboardValue(row.Values.GetValueOrDefault(column.Definition.VariableCode), column.Definition, resolution);
        }));
        return (PrivacyClipboardPolicy.Serialize(values, sensitiveValuePresenter, culture: culture), null);
    }

    public async Task<GridPasteResult> PasteAsync(IGridClipboardService clipboard, bool confirmLargePaste = false,
        CultureInfo? culture = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clipboard);
        try
        {
            var text = await clipboard.ReadTextAsync(cancellationToken).ConfigureAwait(false);
            return await PasteTextAsync(text, confirmLargePaste, culture, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return SetLastPaste(GridPasteResult.Rejected("GRID_CLIPBOARD_UNAVAILABLE")); }
    }

    public async Task<GridPasteResult> PasteTextAsync(string? text, bool confirmLargePaste = false,
        CultureInfo? culture = null, CancellationToken cancellationToken = default)
    {
        if (text is null) return SetLastPaste(GridPasteResult.Rejected("GRID_CLIPBOARD_EMPTY"));
        if (text.Length > PasteOptions.MaximumClipboardCharacters)
            return SetLastPaste(GridPasteResult.Rejected("GRID_CLIPBOARD_OVERSIZED", requiresConfirmation: true));
        var matrix = ClipboardMatrix.Parse(text);
        if (matrix.IsEmpty) return SetLastPaste(GridPasteResult.Rejected("GRID_CLIPBOARD_EMPTY"));
        var target = ResolvePasteTarget(matrix);
        if (target.Diagnostic is not null) return SetLastPaste(target.Diagnostic);
        var range = target.Range!;
        var columns = VisibleColumnsIn(range);
        var targetCount = checked(range.RowCount * columns.Length);
        if (targetCount > PasteOptions.LargeTargetThreshold && !confirmLargePaste)
            return SetLastPaste(GridPasteResult.Rejected("GRID_PASTE_REQUIRES_CONFIRMATION", targetCount, requiresConfirmation: true));
        var rows = await ResolveRowsAsync(range.MinimumRowPosition, range.RowCount, cancellationToken).ConfigureAwait(false);
        if (rows.Length != range.RowCount) return SetLastPaste(GridPasteResult.Rejected("GRID_RANGE_ROWS_UNAVAILABLE", targetCount));

        var changes = ImmutableArray.CreateBuilder<GridCellChange>();
        var errors = ImmutableArray.CreateBuilder<GridValidationDiagnostic>();
        for (var row = 0; row < rows.Length; row++)
        for (var column = 0; column < columns.Length; column++)
        {
            var definition = columns[column];
            GridValidationDiagnostic? diagnostic = null;
            object? candidate = null;
            if (!CanEditColumn(definition.Definition.VariableCode))
                diagnostic = new("GRID_CELL_READ_ONLY", "Grid.Validation.ReadOnly");
            else
                (candidate, diagnostic) = GridPasteConverter.Convert(definition.Definition,
                    matrix[row % matrix.RowCount, column % matrix.ColumnCount], culture);
            if (diagnostic is not null) errors.Add(diagnostic);
            changes.Add(new(rows[row].RowKey, definition.Definition.VariableCode,
                rows[row].Values.GetValueOrDefault(definition.Definition.VariableCode), candidate,
                diagnostic is null ? GridCellValidationState.Valid : GridCellValidationState.Invalid, diagnostic));
        }
        if (errors.Count > 0 && PasteOptions.CommitMode == PasteCommitMode.Atomic)
            return SetLastPaste(GridPasteResult.Rejected("GRID_PASTE_ATOMIC_REJECTED", changes.Count, errors));
        var valid = changes.Where(x => x.ValidationState == GridCellValidationState.Valid).ToImmutableArray();
        if (valid.Length == 0) return SetLastPaste(GridPasteResult.Rejected("GRID_PASTE_NO_VALID_CELLS", changes.Count, errors));
        var applied = await ApplyChangesAsync(valid, GridEditSourceAction.Paste, true, cancellationToken).ConfigureAwait(false);
        var rejected = changes.Count - (applied.Success ? valid.Length : 0);
        var result = new GridPasteResult(applied.Success ? valid.Length : 0, rejected, errors.ToImmutable(), [],
            PasteOptions.CommitMode == PasteCommitMode.Atomic && applied.WasAtomic,
            errors.Count > 0 && applied.Success, DiagnosticCode: applied.DiagnosticCode);
        return SetLastPaste(result);
    }

    public async Task<GridPasteResult> ClearSelectedCellsAsync(GridEditSourceAction source = GridEditSourceAction.Clear,
        CancellationToken cancellationToken = default)
    {
        if (!CellSelection.HasCellSelection || CellSelection.IsAllSelected)
            return SetLastPaste(GridPasteResult.Rejected("GRID_SELECTION_EMPTY"));
        var range = EffectivePrimaryRange();
        if (range is null) return SetLastPaste(GridPasteResult.Rejected("GRID_SELECTION_EMPTY"));
        var columns = VisibleColumnsIn(range);
        var rows = await ResolveRowsAsync(range.MinimumRowPosition, range.RowCount, cancellationToken).ConfigureAwait(false);
        var changes = ImmutableArray.CreateBuilder<GridCellChange>();
        var errors = ImmutableArray.CreateBuilder<GridValidationDiagnostic>();
        foreach (var row in rows)
        foreach (var column in columns)
        {
            var diagnostic = !CanEditColumn(column.Definition.VariableCode)
                ? new GridValidationDiagnostic("GRID_CELL_READ_ONLY", "Grid.Validation.ReadOnly")
                : GridValueValidator.Validate(column.Definition, null);
            if (diagnostic is not null) errors.Add(diagnostic);
            changes.Add(new(row.RowKey, column.Definition.VariableCode,
                row.Values.GetValueOrDefault(column.Definition.VariableCode), null,
                diagnostic is null ? GridCellValidationState.Valid : GridCellValidationState.Invalid, diagnostic));
        }
        if (errors.Count > 0 && PasteOptions.CommitMode == PasteCommitMode.Atomic)
            return SetLastPaste(GridPasteResult.Rejected("GRID_CLEAR_ATOMIC_REJECTED", changes.Count, errors));
        var valid = changes.Where(x => x.ValidationState == GridCellValidationState.Valid).ToImmutableArray();
        if (valid.Length == 0) return SetLastPaste(GridPasteResult.Rejected("GRID_CLEAR_NO_VALID_CELLS", changes.Count, errors));
        var applied = await ApplyChangesAsync(valid, source, true, cancellationToken).ConfigureAwait(false);
        return SetLastPaste(new(applied.Success ? valid.Length : 0, changes.Count - (applied.Success ? valid.Length : 0),
            errors.ToImmutable(), [], PasteOptions.CommitMode == PasteCommitMode.Atomic && applied.WasAtomic,
            errors.Count > 0 && applied.Success, DiagnosticCode: applied.DiagnosticCode));
    }

    public async Task<GridPasteResult> CutAsync(IGridClipboardService clipboard, CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        var copy = await CopyAsync(clipboard, culture, cancellationToken).ConfigureAwait(false);
        if (copy.DiagnosticCode is not null) return SetLastPaste(copy);
        return await ClearSelectedCellsAsync(GridEditSourceAction.Cut, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GridPasteResult> UndoAsync(CancellationToken cancellationToken = default)
    {
        var transaction = editHistory.TakeUndo();
        if (transaction is null) return GridPasteResult.Rejected("GRID_UNDO_EMPTY");
        var changes = transaction.CellChanges.Select(x => x with
        {
            OriginalValue = x.CandidateValue, CandidateValue = x.OriginalValue,
            ValidationState = GridCellValidationState.Valid, Diagnostic = null,
        }).ToImmutableArray();
        var result = await ApplyChangesAsync(changes, GridEditSourceAction.Undo, false, cancellationToken).ConfigureAwait(false);
        if (!result.Success) editHistory.RestoreUndoFailure(transaction);
        return new(result.Success ? changes.Length : 0, result.Success ? 0 : changes.Length, [], [], result.WasAtomic, false,
            DiagnosticCode: result.DiagnosticCode);
    }

    public async Task<GridPasteResult> RedoAsync(CancellationToken cancellationToken = default)
    {
        var transaction = editHistory.TakeRedo();
        if (transaction is null) return GridPasteResult.Rejected("GRID_REDO_EMPTY");
        var result = await ApplyChangesAsync(transaction.CellChanges, GridEditSourceAction.Redo, false, cancellationToken).ConfigureAwait(false);
        if (!result.Success) editHistory.RestoreRedoFailure(transaction);
        return new(result.Success ? transaction.CellChanges.Length : 0, result.Success ? 0 : transaction.CellChanges.Length,
            [], [], result.WasAtomic, false, DiagnosticCode: result.DiagnosticCode);
    }

    private (GridCellRange? Range, GridPasteResult? Diagnostic) ResolvePasteTarget(ClipboardMatrix matrix)
    {
        var selected = EffectivePrimaryRange();
        if (selected is null) return (null, GridPasteResult.Rejected("GRID_SELECTION_EMPTY"));
        var bounds = selected.ResolveColumnBounds(VisibleVariableCodes());
        if (bounds is null) return (null, GridPasteResult.Rejected("GRID_VARIABLE_UNAVAILABLE"));
        var selectedColumns = bounds.Value.End - bounds.Value.Start + 1;
        var singleTarget = selected.RowCount == 1 && selectedColumns == 1;
        if (singleTarget && (matrix.RowCount > 1 || matrix.ColumnCount > 1))
        {
            var columns = VisibleVariableCodes();
            if (bounds.Value.Start + matrix.ColumnCount > columns.Length || selected.MinimumRowPosition + matrix.RowCount > TotalRows)
                return (null, GridPasteResult.Rejected("GRID_PASTE_TARGET_OUT_OF_RANGE"));
            var start = selected.Start;
            var endPosition = start.LogicalRowPosition + matrix.RowCount - 1;
            var endRow = RowAtPosition(endPosition)?.RowKey ?? start.Address.RowKey;
            return (new(start, new(new(endRow, columns[bounds.Value.Start + matrix.ColumnCount - 1]), endPosition)), null);
        }
        var compatible = matrix.RowCount == selected.RowCount && matrix.ColumnCount == selectedColumns ||
            matrix.RowCount == 1 && matrix.ColumnCount == 1 ||
            PasteOptions.AllowExactTiling && selected.RowCount % matrix.RowCount == 0 && selectedColumns % matrix.ColumnCount == 0;
        return compatible ? (selected, null) : (null, GridPasteResult.Rejected("GRID_PASTE_SHAPE_INCOMPATIBLE"));
    }

    private async Task<(bool Success, bool WasAtomic, string? DiagnosticCode)> ApplyChangesAsync(
        ImmutableArray<GridCellChange> changes, GridEditSourceAction source, bool recordHistory,
        CancellationToken cancellationToken)
    {
        if (context is null) return (false, true, "GRID_CONTEXT_UNAVAILABLE");
        var transaction = GridEditTransaction.Create(changes, source);
        var commitContext = context;
        var commitGeneration = Generation;
        if (provider is IGridBatchEditProvider batch)
        {
            GridBatchCommitResult result;
            try { result = await batch.CommitBatchAsync(commitContext, transaction, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { result = GridBatchCommitResult.Rejected("GRID_PROVIDER_BATCH_COMMIT_FAILED"); }
            if (!IsCurrent(commitContext, commitGeneration)) return (false, true, "GRID_STALE_EDIT_RESULT");
            if (!result.IsSuccess) return (false, true, result.DiagnosticCode ?? "GRID_PROVIDER_BATCH_COMMIT_FAILED");
            ApplyValues(changes);
            if (recordHistory) editHistory.Record(transaction with { CommitState = GridEditCommitState.Committed });
            OnChanged(source.ToString().ToUpperInvariant());
            return (true, true, null);
        }

        var committed = ImmutableArray.CreateBuilder<GridCellChange>();
        foreach (var change in changes)
        {
            GridCommitResult result;
            try { result = await provider.CommitAsync(commitContext,
                new(change.RowKey, change.VariableCode, change.CandidateValue), cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { result = GridCommitResult.Rejected("GRID_PROVIDER_COMMIT_FAILED"); }
            if (!IsCurrent(commitContext, commitGeneration)) return (false, false, "GRID_STALE_EDIT_RESULT");
            if (!result.IsSuccess) return (false, false, result.DiagnosticCode ?? "GRID_PROVIDER_COMMIT_FAILED");
            committed.Add(change with { CandidateValue = result.CommittedValue });
        }
        ApplyValues(committed.ToImmutable());
        if (recordHistory) editHistory.Record(transaction with { CellChanges = committed.ToImmutable(), CommitState = GridEditCommitState.Committed });
        OnChanged(source.ToString().ToUpperInvariant());
        return (true, changes.Length <= 1, null);
    }

    private void ApplyValues(IEnumerable<GridCellChange> changes)
    {
        foreach (var change in changes)
        {
            Rows = Rows.Select(row => row.RowKey == change.RowKey ? row.WithValue(change.VariableCode, change.CandidateValue) : row).ToImmutableArray();
            windowCache.UpdateCell(change.RowKey, change.VariableCode, change.CandidateValue);
        }
    }

    private async Task<ImmutableArray<GridRow>> ResolveRowsAsync(int startPosition, int rowCount,
        CancellationToken cancellationToken)
    {
        if (startPosition >= ViewportStartIndex && startPosition + rowCount <= ViewportStartIndex + Rows.Length)
            return Rows.Skip(startPosition - ViewportStartIndex).Take(rowCount).ToImmutableArray();
        if (context is null || provider is not IGridLogicalRowProvider resolver) return [];
        var requestGeneration = Generation;
        var result = await resolver.ResolveRowsAsync(context, startPosition, rowCount, Sorts, Filters,
            requestGeneration, cancellationToken).ConfigureAwait(false);
        return requestGeneration == Generation ? result : [];
    }

    private GridCellRange? EffectivePrimaryRange()
    {
        if (CellSelection.PrimaryRange is { } range) return range;
        if (CellSelection.ActiveCell is not { } active) return null;
        var local = Rows.FindIndex(x => x.RowKey == active.RowKey);
        if (local < 0) return null;
        var endpoint = new GridRangeEndpoint(active, ViewportStartIndex + local);
        return new(endpoint, endpoint);
    }

    private ImmutableArray<ResolvedGridColumn> VisibleColumnsIn(GridCellRange range)
    {
        var visible = View.VisibleColumns.Select(x => x.Column).ToImmutableArray();
        var bounds = range.ResolveColumnBounds(visible.Select(x => x.Definition.VariableCode).ToArray());
        return bounds is null ? [] : visible[bounds.Value.Start..(bounds.Value.End + 1)];
    }

    private ImmutableArray<VariableCode> VisibleVariableCodes() => View.VisibleColumns
        .Select(x => x.VariableCode).ToImmutableArray();
    private bool IsVisibleColumn(VariableCode variableCode) => View.VisibleColumns
        .Any(x => x.VariableCode == variableCode);
    private bool CanEditColumn(VariableCode variableCode) => ResolvedDefinition.CanEdit && ResolvedDefinition.Columns
        .Any(x => x.IsVisible && x.CanEdit && x.Definition.VariableCode == variableCode);
    private bool IsKnownRow(RowKey rowKey, int position) => position >= 0 && position < TotalRows &&
        (IsVirtualized || Rows.Any(x => x.RowKey == rowKey));
    private GridRow? RowAtPosition(int position) => position >= ViewportStartIndex && position < ViewportStartIndex + Rows.Length
        ? Rows[position - ViewportStartIndex] : null;
    private int RangeColumnCount(GridCellRange range) => range.ResolveColumnBounds(VisibleVariableCodes()) is { } bounds
        ? bounds.End - bounds.Start + 1 : 0;
    private int GetSelectedCellCount()
    {
        if (CellSelection.IsAllSelected) return checked((int)Math.Min(int.MaxValue, (long)TotalRows * VisibleVariableCodes().Length));
        long count = 0;
        foreach (var range in CellSelection.SelectedRanges) count += (long)range.RowCount * RangeColumnCount(range);
        return (int)Math.Min(int.MaxValue, count);
    }
    private bool IsCurrent(GridProviderContext expected, long expectedGeneration) => context is not null &&
        context.Company.CompanyId == expected.Company.CompanyId &&
        context.WorkspaceId.Equals(expected.WorkspaceId, StringComparison.OrdinalIgnoreCase) && Generation == expectedGeneration;
    private GridPasteResult SetLastPaste(GridPasteResult result) { LastPasteResult = result; OnChanged("PASTE_RESULT"); return result; }

    private void ReconcileCellSelection()
    {
        if (!CellSelection.HasCellSelection || CellSelection.IsAllSelected) return;
        if (!IsVirtualized && CellSelection.ActiveCell is { } active && !Rows.Any(x => x.RowKey == active.RowKey))
        {
            CellSelection = new(null, null, [], SelectedRowKeys, GridCellSelectionMode.Row);
            return;
        }
        GridRangeEndpoint Reposition(GridRangeEndpoint endpoint)
        {
            var index = Rows.FindIndex(x => x.RowKey == endpoint.Address.RowKey);
            return index < 0 ? endpoint : endpoint with { LogicalRowPosition = ViewportStartIndex + index };
        }
        GridRangeEndpoint? anchor = CellSelection.AnchorCell is { } value ? Reposition(value) : null;
        CellSelection = CellSelection with
        {
            AnchorCell = anchor,
            SelectedRanges = CellSelection.SelectedRanges.Select(x => new GridCellRange(Reposition(x.Start), Reposition(x.End))).ToImmutableArray(),
        };
    }
}

internal static class GridImmutableArrayExtensions
{
    public static int FindIndex<T>(this ImmutableArray<T> values, Func<T, bool> predicate)
    {
        for (var index = 0; index < values.Length; index++) if (predicate(values[index])) return index;
        return -1;
    }
}
