using System.Collections.Immutable;

namespace DynamicUI24.Core.DataEntry;

public sealed partial class DataEntryGridRuntime
{
    public bool CanInsertRows => Definition.AllowAdd && ResolvedDefinition.State ==
        DynamicUI24.Core.Authorization.AuthorizationPresentationState.VisibleEnabled &&
        provider is IGridRowLifecycleProvider { CanInsertRows: true };
    public bool CanDeleteRows => Definition.AllowDelete && ResolvedDefinition.State ==
        DynamicUI24.Core.Authorization.AuthorizationPresentationState.VisibleEnabled &&
        provider is IGridRowLifecycleProvider { CanDeleteRows: true };

    public async Task<GridRowInsertResult> InsertRowAsync(RowKey anchor, GridRowInsertPlacement placement,
        IReadOnlyDictionary<DynamicUI24.Core.Setup.VariableCode, object?>? initialValues = null,
        CancellationToken cancellationToken = default)
    {
        if (!CanInsertRows || provider is not IGridRowLifecycleProvider lifecycle || context is null)
            return GridRowInsertResult.Rejected("GRID_ROW_INSERT_DENIED");
        var localAnchor = Rows.FindIndex(x => x.RowKey == anchor);
        if (localAnchor < 0) return GridRowInsertResult.Rejected("GRID_ROW_ANCHOR_UNAVAILABLE");
        var requestContext = context; var requestGeneration = Generation;
        GridRowInsertResult result;
        try { result = await lifecycle.InsertRowAsync(requestContext,
            GridRowInsertRequest.Create(anchor, placement, initialValues, ViewportStartIndex + localAnchor,
                requestGeneration), cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return GridRowInsertResult.Rejected("GRID_ROW_INSERT_PROVIDER_FAILED"); }
        if (!IsCurrent(requestContext, requestGeneration)) return GridRowInsertResult.Rejected("GRID_STALE_ROW_RESULT");
        if (!result.IsSuccess || result.InsertedRowKey is null) return result.IsSuccess
            ? GridRowInsertResult.Rejected("GRID_ROW_KEY_INVALID") : result;
        if (result.LogicalPosition is { } position && viewportProvider is not null)
        {
            windowCache.Clear();
            await RequestViewportAsync(position, RequestedViewportRowCount > 0 ? RequestedViewportRowCount :
                ViewportOptions.VisibleRowCount, cancellationToken).ConfigureAwait(false);
        }
        else await RefreshRowsAfterLifecycleAsync(cancellationToken).ConfigureAwait(false);
        ActivateRow(result.InsertedRowKey.Value, Rows.FindIndex(x => x.RowKey == result.InsertedRowKey.Value));
        if (provider is IGridRowCalculationInvalidation calculation)
            await calculation.InvalidateRowsAsync(requestContext, [result.InsertedRowKey.Value], cancellationToken).ConfigureAwait(false);
        OnChanged("ROW_INSERTED"); return result;
    }

    public Task<GridRowDeleteResult> DeleteRowAsync(RowKey rowKey, CancellationToken cancellationToken = default) =>
        DeleteRowsAsync([rowKey], cancellationToken);

    public async Task<GridRowDeleteResult> DeleteSelectedRowsAsync(CancellationToken cancellationToken = default)
    {
        var keys = SelectedRowKeys.Count > 0 ? SelectedRowKeys :
            CellSelection.SelectedRanges.SelectMany(range => Rows.Where((_, index) =>
                ViewportStartIndex + index >= range.MinimumRowPosition && ViewportStartIndex + index <= range.MaximumRowPosition)
                .Select(x => x.RowKey)).ToImmutableHashSet();
        return await DeleteRowsAsync(keys, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GridRowDeleteResult> DeleteRowsAsync(IEnumerable<RowKey> rowKeys,
        CancellationToken cancellationToken = default)
    {
        if (!CanDeleteRows || provider is not IGridRowLifecycleProvider lifecycle || context is null)
            return GridRowDeleteResult.Rejected("GRID_ROW_DELETE_DENIED");
        var keys = rowKeys.Distinct().ToImmutableArray();
        if (keys.IsEmpty) return GridRowDeleteResult.Rejected("GRID_ROW_SELECTION_EMPTY");
        var current = Rows.Select(x => x.RowKey).ToHashSet();
        if (keys.Any(x => !current.Contains(x))) return GridRowDeleteResult.Rejected("GRID_ROW_UNAVAILABLE");
        var oldPosition = Math.Max(0, Rows.FindIndex(x => keys.Contains(x.RowKey)));
        var requestContext = context; var requestGeneration = Generation;
        GridRowDeleteResult result;
        try { result = await lifecycle.DeleteRowsAsync(requestContext,
            GridRowDeleteRequest.Create(keys, requestGeneration), cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return GridRowDeleteResult.Rejected("GRID_ROW_DELETE_PROVIDER_FAILED"); }
        if (!IsCurrent(requestContext, requestGeneration)) return GridRowDeleteResult.Rejected("GRID_STALE_ROW_RESULT");
        if (!result.IsSuccess) return result;
        var deleted = result.DeletedRowKeys.ToHashSet();
        foreach (var key in pendingChanges.Keys.Where(x => deleted.Contains(x.RowKey)).ToArray()) pendingChanges.Remove(key);
        if (EditBuffer is { } edit && deleted.Contains(edit.RowKey)) EditBuffer = null;
        editHistory.Clear(); SelectedRowKeys = SelectedRowKeys.Except(deleted); CellSelection = GridSelectionState.Empty;
        await RefreshRowsAfterLifecycleAsync(cancellationToken).ConfigureAwait(false);
        if (Rows.Length > 0) ActivateRow(Rows[Math.Min(oldPosition, Rows.Length - 1)].RowKey, Math.Min(oldPosition, Rows.Length - 1));
        if (provider is IGridRowCalculationInvalidation calculation)
            await calculation.InvalidateRowsAsync(requestContext, deleted, cancellationToken).ConfigureAwait(false);
        OnChanged("ROWS_DELETED"); return result;
    }

    private async Task RefreshRowsAfterLifecycleAsync(CancellationToken cancellationToken)
    {
        windowCache.Clear();
        if (viewportProvider is not null)
            await RequestViewportAsync(RequestedViewportStartIndex,
                RequestedViewportRowCount > 0 ? RequestedViewportRowCount : ViewportOptions.VisibleRowCount,
                cancellationToken).ConfigureAwait(false);
        else if (context is not null) await LoadAsync(context, authorization, cancellationToken).ConfigureAwait(false);
    }

    private void ActivateRow(RowKey rowKey, int localIndex)
    {
        if (localIndex < 0) return;
        SelectedRowKeys = [rowKey];
        var column = PresentedColumns.FirstOrDefault(x => x.Column.CanEdit)?.VariableCode ??
            PresentedColumns.FirstOrDefault()?.VariableCode;
        if (column is { } code) SelectCell(new(rowKey, code), ViewportStartIndex + localIndex);
        else Select([rowKey]);
    }
}
