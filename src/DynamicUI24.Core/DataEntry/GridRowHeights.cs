using System.Collections.Immutable;

namespace DynamicUI24.Core.DataEntry;

public enum GridRowHeightCommandKind { Decrease, Increase, Set, Reset }
public sealed record GridRowHeightCommand(GridRowHeightCommandKind Kind, string Label, decimal? Percentage = null);

public static class GridRowHeightCommands
{
    public static ImmutableArray<GridRowHeightCommand> Choices { get; } =
    [
        new(GridRowHeightCommandKind.Decrease, "Shorter  -10%"),
        new(GridRowHeightCommandKind.Increase, "Taller  +10%"),
        new(GridRowHeightCommandKind.Set, "90%", 90),
        new(GridRowHeightCommandKind.Set, "100% Default", 100),
        new(GridRowHeightCommandKind.Set, "110%", 110),
        new(GridRowHeightCommandKind.Set, "125%", 125),
        new(GridRowHeightCommandKind.Set, "150%", 150),
        new(GridRowHeightCommandKind.Set, "200%", 200),
        new(GridRowHeightCommandKind.Reset, "Reset"),
    ];
}

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
    public bool ShowRowNumbers => Definition.ShowRowNumbers && CurrentViewPreference.ShowRowNumbers;
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
        MutateViewPreference(x => x with { RowHeightScalePercent = Math.Clamp(percentage, 75m, 300m) },
            "ROW_HEIGHT_PERCENTAGE");
    }

    public void IncreaseRowHeight() => SetRowHeightPercentage(RowHeightScalePercent + 10m);
    public void DecreaseRowHeight() => SetRowHeightPercentage(RowHeightScalePercent - 10m);
    public void ResetRowHeightPercentage() => SetRowHeightPercentage(100m);

    public void ExecuteRowHeightCommand(GridRowHeightCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        switch (command.Kind)
        {
            case GridRowHeightCommandKind.Decrease: DecreaseRowHeight(); break;
            case GridRowHeightCommandKind.Increase: IncreaseRowHeight(); break;
            case GridRowHeightCommandKind.Set when command.Percentage is { } value: SetRowHeightPercentage(value); break;
            case GridRowHeightCommandKind.Reset: ResetRowHeightPercentage(); break;
            default: throw new ArgumentException("GRID_ROW_HEIGHT_COMMAND_INVALID", nameof(command));
        }
    }

    public void SetRowNumbersVisible(bool visible)
    {
        MutateViewPreference(x => x with { ShowRowNumbers = Definition.ShowRowNumbers && visible },
            "ROW_NUMBERS_VISIBILITY");
    }

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
