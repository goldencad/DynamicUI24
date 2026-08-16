using System.Collections.Immutable;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

public sealed partial class DataEntryGridRuntime
{
    public bool CanFind => provider is IGridFindProvider && context is not null;

    public GridFindScope ResolveFindScope(GridFindScope naturalScope, RowKey? rowKey, VariableCode? variableCode)
    {
        var requested = RememberedFindScope ?? naturalScope;
        return requested switch
        {
            GridFindScope.CurrentRow when rowKey is { } row && Rows.Any(x => x.RowKey == row) => requested,
            GridFindScope.CurrentColumn when variableCode is { } code && FindEligibleColumns().Contains(code) => requested,
            GridFindScope.AllVisibleColumns => requested,
            _ => GridFindScope.AllVisibleColumns,
        };
    }

    public async Task<GridFindResult> FindAsync(string query, GridFindScope scope,
        VariableCode? currentColumn = null, RowKey? currentRow = null,
        GridFindDirection direction = GridFindDirection.Next,
        CancellationToken cancellationToken = default)
    {
        var text = query?.Trim() ?? string.Empty;
        if (text.Length == 0 || provider is not IGridFindProvider finder || context is null)
            return GridFindResult.Rejected("GRID_FIND_UNAVAILABLE", Generation);
        var eligible = FindEligibleColumns();
        if (scope == GridFindScope.CurrentColumn && (currentColumn is null || !eligible.Contains(currentColumn.Value)))
            return GridFindResult.Rejected("GRID_FIND_COLUMN_RESTRICTED", Generation);
        if (scope == GridFindScope.CurrentRow && (currentRow is null || !Rows.Any(x => x.RowKey == currentRow.Value)))
            return GridFindResult.Rejected("GRID_FIND_ROW_UNAVAILABLE", Generation);
        var start = scope == GridFindScope.CurrentRow && currentRow is { } targetRow
            ? ViewportStartIndex + Rows.FindIndex(x => x.RowKey == targetRow) : CellSelection.ActiveCell is { } active
            ? ViewportStartIndex + Math.Max(0, Rows.FindIndex(x => x.RowKey == active.RowKey))
            : direction == GridFindDirection.Next ? -1 : TotalRows;
        var requestContext = context; var requestGeneration = Generation;
        GridFindResult result;
        try
        {
            result = await finder.FindAsync(requestContext, new(text, scope, currentRow, currentColumn, eligible,
                direction, start, Sorts, Filters, requestGeneration), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return GridFindResult.Rejected("GRID_FIND_PROVIDER_FAILED", requestGeneration); }
        if (!IsCurrent(requestContext, requestGeneration) || result.RequestGeneration != requestGeneration)
            return GridFindResult.Rejected("GRID_STALE_FIND_RESULT", requestGeneration);
        if (!result.IsMatch || result.RowKey is null || result.VariableCode is null || result.LogicalPosition is null)
            return result;
        if (!eligible.Contains(result.VariableCode.Value) || result.LogicalPosition < 0 || result.LogicalPosition >= TotalRows)
            return GridFindResult.Rejected("GRID_FIND_RESULT_INVALID", requestGeneration);
        if (result.LogicalPosition < ViewportStartIndex || result.LogicalPosition >= ViewportStartIndex + Rows.Length)
            await RequestViewportAsync(result.LogicalPosition.Value,
                RequestedViewportRowCount > 0 ? RequestedViewportRowCount : ViewportOptions.VisibleRowCount,
                cancellationToken).ConfigureAwait(false);
        var local = Rows.FindIndex(x => x.RowKey == result.RowKey.Value);
        if (local < 0) return GridFindResult.Rejected("GRID_FIND_TARGET_UNAVAILABLE", Generation);
        SelectCell(new(result.RowKey.Value, result.VariableCode.Value), ViewportStartIndex + local);
        return result;
    }

    private ImmutableArray<VariableCode> FindEligibleColumns() => PresentedColumns
        .Where(x => x.Column.Definition.SensitiveContent is null)
        .Select(x => x.VariableCode).ToImmutableArray();
}
