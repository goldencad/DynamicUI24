using System.Collections.Immutable;

namespace DynamicUI24.Core.DataEntry;

/// <summary>Configurable bounded materialization policy for a virtualized grid.</summary>
public sealed record GridViewportOptions
{
    public GridViewportOptions(int visibleRowCount = 60, int overscanBefore = 20, int overscanAfter = 20,
        int maximumCachedWindows = 3, int maximumMaterializedRows = 300)
    {
        if (visibleRowCount <= 0) throw new ArgumentOutOfRangeException(nameof(visibleRowCount));
        ArgumentOutOfRangeException.ThrowIfNegative(overscanBefore);
        ArgumentOutOfRangeException.ThrowIfNegative(overscanAfter);
        if (maximumCachedWindows <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCachedWindows));
        if (maximumMaterializedRows <= 0 ||
            (long)visibleRowCount + overscanBefore + overscanAfter > maximumMaterializedRows)
            throw new ArgumentOutOfRangeException(nameof(maximumMaterializedRows));
        VisibleRowCount = visibleRowCount;
        OverscanBefore = overscanBefore;
        OverscanAfter = overscanAfter;
        MaximumCachedWindows = maximumCachedWindows;
        MaximumMaterializedRows = maximumMaterializedRows;
    }

    public int VisibleRowCount { get; }
    public int OverscanBefore { get; }
    public int OverscanAfter { get; }
    public int MaximumCachedWindows { get; }
    public int MaximumMaterializedRows { get; }
}

/// <summary>Presentation-coordinate request. Row identity is always <see cref="RowKey"/>, never an index.</summary>
public sealed record GridViewportRequest
{
    public GridViewportRequest(int startIndex, int requestedRowCount, int overscanBefore, int overscanAfter,
        IEnumerable<GridSortDefinition>? sortDefinitions = null,
        IEnumerable<GridFilterDefinition>? filterDefinitions = null, long requestGeneration = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        if (requestedRowCount <= 0) throw new ArgumentOutOfRangeException(nameof(requestedRowCount));
        ArgumentOutOfRangeException.ThrowIfNegative(overscanBefore);
        ArgumentOutOfRangeException.ThrowIfNegative(overscanAfter);
        StartIndex = startIndex;
        RequestedRowCount = requestedRowCount;
        OverscanBefore = overscanBefore;
        OverscanAfter = overscanAfter;
        SortDefinitions = (sortDefinitions ?? []).OrderBy(x => x.Priority).ToImmutableArray();
        FilterDefinitions = (filterDefinitions ?? []).ToImmutableArray();
        RequestGeneration = requestGeneration;
    }

    public int StartIndex { get; }
    public int RequestedRowCount { get; }
    public int OverscanBefore { get; }
    public int OverscanAfter { get; }
    public ImmutableArray<GridSortDefinition> SortDefinitions { get; }
    public ImmutableArray<GridFilterDefinition> FilterDefinitions { get; }
    public long RequestGeneration { get; }
    public int MaterializedStartIndex => Math.Max(0, StartIndex - OverscanBefore);
    public int MaterializedRowCount => checked(RequestedRowCount + Math.Min(StartIndex, OverscanBefore) + OverscanAfter);
}

/// <summary>Immutable provider window. Runtime validates all range metadata before adoption.</summary>
public sealed record GridViewportResult(GridProviderState State, int StartIndex, ImmutableArray<GridRow> Rows,
    int TotalRowCount, long RequestGeneration, bool HasPrevious, bool HasNext,
    string? ProviderState = null, string? DiagnosticCode = null)
{
    public static GridViewportResult Ready(GridViewportRequest request, IEnumerable<GridRow> rows, int totalRowCount,
        string? providerState = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var values = rows.ToImmutableArray();
        var start = request.MaterializedStartIndex;
        return new(values.Length == 0 ? GridProviderState.Empty : GridProviderState.Ready, start, values,
            totalRowCount, request.RequestGeneration, start > 0, start + values.Length < totalRowCount, providerState);
    }

    public static GridViewportResult Failure(GridViewportRequest request, GridProviderState state, string diagnosticCode) =>
        state is GridProviderState.Error or GridProviderState.Unavailable
            ? new(state, request.MaterializedStartIndex, [], 0, request.RequestGeneration,
                request.MaterializedStartIndex > 0, false, DiagnosticCode: diagnosticCode)
            : throw new ArgumentOutOfRangeException(nameof(state));
}

internal readonly record struct GridWindowKey(int StartIndex, int RequestedRowCount);

/// <summary>Least-recently-used cache bounded by both window count and the per-window materialization guard.</summary>
internal sealed class GridWindowCache
{
    private readonly int capacity;
    private readonly Dictionary<GridWindowKey, LinkedListNode<(GridWindowKey Key, GridViewportResult Value)>> entries = [];
    private readonly LinkedList<(GridWindowKey Key, GridViewportResult Value)> recency = [];

    public GridWindowCache(int capacity) => this.capacity = capacity;
    public int WindowCount => entries.Count;
    public int RowCount => entries.Values.Sum(x => x.Value.Value.Rows.Length);

    public bool TryGet(GridWindowKey key, out GridViewportResult result)
    {
        if (!entries.TryGetValue(key, out var node)) { result = default!; return false; }
        recency.Remove(node); recency.AddFirst(node); result = node.Value.Value; return true;
    }

    public void Set(GridWindowKey key, GridViewportResult value)
    {
        if (entries.Remove(key, out var existing)) recency.Remove(existing);
        var node = recency.AddFirst((key, value)); entries[key] = node;
        while (entries.Count > capacity)
        {
            var last = recency.Last!; recency.RemoveLast(); entries.Remove(last.Value.Key);
        }
    }

    public void UpdateCell(RowKey rowKey, DynamicUI24.Core.Setup.VariableCode variableCode, object? value)
    {
        foreach (var node in entries.Values)
        {
            var index = -1;
            for (var candidate = 0; candidate < node.Value.Value.Rows.Length; candidate++)
                if (node.Value.Value.Rows[candidate].RowKey == rowKey) { index = candidate; break; }
            if (index >= 0)
            {
                var row = node.Value.Value.Rows[index].WithValue(variableCode, value);
                node.Value = (node.Value.Key, node.Value.Value with { Rows = node.Value.Value.Rows.SetItem(index, row) });
            }
        }
    }

    public void Clear() { entries.Clear(); recency.Clear(); }
}
