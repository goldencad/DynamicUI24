using System.Collections.Immutable;

namespace DynamicUI24.Core.DataEntry;

/// <summary>Bounds sparse presentation-only row-height overrides; RowKey remains authoritative identity.</summary>
public sealed record GridRowHeightOptions
{
    public GridRowHeightOptions(decimal defaultHeight = 38m, decimal minimumHeight = 24m,
        decimal maximumHeight = 240m, int maximumOverrides = 256)
    {
        if (defaultHeight <= 0 || minimumHeight <= 0 || maximumHeight < minimumHeight ||
            defaultHeight < minimumHeight || defaultHeight > maximumHeight || maximumOverrides <= 0)
            throw new ArgumentOutOfRangeException(nameof(defaultHeight));
        DefaultHeight = defaultHeight; MinimumHeight = minimumHeight; MaximumHeight = maximumHeight;
        MaximumOverrides = maximumOverrides;
    }
    public decimal DefaultHeight { get; }
    public decimal MinimumHeight { get; }
    public decimal MaximumHeight { get; }
    public int MaximumOverrides { get; }
}

public sealed partial class DataEntryGridRuntime
{
    private readonly Dictionary<RowKey, (decimal Height, LinkedListNode<RowKey> Node)> rowHeightOverrides = [];
    private readonly LinkedList<RowKey> rowHeightRecency = [];

    public GridRowHeightOptions RowHeightOptions { get; private set; } = new();
    public int RowHeightOverrideCount => rowHeightOverrides.Count;
    public decimal RowHeightScalePercent => CurrentViewPreference.RowHeightScalePercent;
    public bool TryGetRowHeight(RowKey rowKey, out decimal height)
    {
        if (rowHeightOverrides.TryGetValue(rowKey, out var value)) { height = value.Height; return true; }
        height = RowHeightOptions.DefaultHeight; return false;
    }
    public decimal GetRowHeight(RowKey rowKey) => rowHeightOverrides.TryGetValue(rowKey, out var value)
        ? value.Height : RowHeightOptions.DefaultHeight;

    public decimal ResolveRowHeight(RowKey rowKey, decimal densityDefaultHeight) =>
        rowHeightOverrides.TryGetValue(rowKey, out var value) ? value.Height :
        densityDefaultHeight * RowHeightScalePercent / 100m;

    public void SetRowHeightPercentage(decimal percentage)
    {
        viewPreference = CurrentViewPreference with { RowHeightScalePercent = Math.Clamp(percentage, 75m, 300m) };
        OnChanged("ROW_HEIGHT_PERCENTAGE");
    }

    public void IncreaseRowHeight() => SetRowHeightPercentage(RowHeightScalePercent + 10m);
    public void DecreaseRowHeight() => SetRowHeightPercentage(RowHeightScalePercent - 10m);
    public void ResetRowHeightPercentage() => SetRowHeightPercentage(100m);

    public bool ResizeRow(RowKey rowKey, decimal height)
    {
        if (!Rows.Any(x => x.RowKey == rowKey) || height <= 0) return false;
        var bounded = Math.Clamp(height, RowHeightOptions.MinimumHeight, RowHeightOptions.MaximumHeight);
        if (rowHeightOverrides.Remove(rowKey, out var previous)) rowHeightRecency.Remove(previous.Node);
        var node = rowHeightRecency.AddFirst(rowKey); rowHeightOverrides[rowKey] = (bounded, node);
        while (rowHeightOverrides.Count > RowHeightOptions.MaximumOverrides)
        {
            var last = rowHeightRecency.Last!; rowHeightRecency.RemoveLast(); rowHeightOverrides.Remove(last.Value);
        }
        OnChanged("ROW_RESIZE"); return true;
    }

    public bool ResetRowHeight(RowKey rowKey)
    {
        if (!rowHeightOverrides.Remove(rowKey, out var value)) return false;
        rowHeightRecency.Remove(value.Node); OnChanged("ROW_HEIGHT_RESET"); return true;
    }

    public ImmutableDictionary<RowKey, decimal> CaptureRowHeights() => rowHeightOverrides
        .ToImmutableDictionary(x => x.Key, x => x.Value.Height);

    public void ApplyRowHeights(IReadOnlyDictionary<RowKey, decimal>? values)
    {
        rowHeightOverrides.Clear(); rowHeightRecency.Clear();
        foreach (var item in (values ?? new Dictionary<RowKey, decimal>()).Take(RowHeightOptions.MaximumOverrides).Reverse())
        {
            var node = rowHeightRecency.AddFirst(item.Key);
            rowHeightOverrides[item.Key] = (Math.Clamp(item.Value, RowHeightOptions.MinimumHeight,
                RowHeightOptions.MaximumHeight), node);
        }
        OnChanged("ROW_HEIGHTS_APPLIED");
    }
}
