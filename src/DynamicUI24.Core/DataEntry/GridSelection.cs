using System.Collections.Immutable;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

public enum GridCellSelectionMode { Row, Cell, Range }

/// <summary>Stable semantic identity for one cell; visual indexes are deliberately excluded.</summary>
public readonly record struct GridCellAddress(RowKey RowKey, VariableCode VariableCode);

/// <summary>
/// One endpoint captured in the current logical row order. Position is navigation context only;
/// identity remains <see cref="GridCellAddress"/> and is re-resolved after ordering changes.
/// </summary>
public readonly record struct GridRangeEndpoint
{
    public GridRangeEndpoint(GridCellAddress address, int logicalRowPosition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(logicalRowPosition);
        Address = address;
        LogicalRowPosition = logicalRowPosition;
    }
    public GridCellAddress Address { get; init; }
    public int LogicalRowPosition { get; init; }
}

/// <summary>Compact rectangle. It never expands into one object per selected cell.</summary>
public sealed record GridCellRange(GridRangeEndpoint Start, GridRangeEndpoint End)
{
    public int MinimumRowPosition => Math.Min(Start.LogicalRowPosition, End.LogicalRowPosition);
    public int MaximumRowPosition => Math.Max(Start.LogicalRowPosition, End.LogicalRowPosition);
    public int RowCount => checked(MaximumRowPosition - MinimumRowPosition + 1);

    public (int Start, int End)? ResolveColumnBounds(IReadOnlyList<VariableCode> visibleColumns)
    {
        var start = IndexOf(visibleColumns, Start.Address.VariableCode);
        var end = IndexOf(visibleColumns, End.Address.VariableCode);
        return start < 0 || end < 0 ? null : (Math.Min(start, end), Math.Max(start, end));
    }

    public bool Contains(GridCellAddress address, int logicalRowPosition, IReadOnlyList<VariableCode> visibleColumns)
    {
        var bounds = ResolveColumnBounds(visibleColumns);
        var column = IndexOf(visibleColumns, address.VariableCode);
        return bounds is { } value && logicalRowPosition >= MinimumRowPosition &&
            logicalRowPosition <= MaximumRowPosition && column >= value.Start && column <= value.End;
    }

    private static int IndexOf(IReadOnlyList<VariableCode> values, VariableCode value)
    {
        for (var index = 0; index < values.Count; index++) if (values[index] == value) return index;
        return -1;
    }
}

public sealed record GridSelectionState(
    GridCellAddress? ActiveCell,
    GridRangeEndpoint? AnchorCell,
    ImmutableArray<GridCellRange> SelectedRanges,
    ImmutableHashSet<RowKey> SelectedRowKeys,
    GridCellSelectionMode SelectionMode,
    bool IsAllSelected = false)
{
    public static GridSelectionState Empty { get; } = new(null, null, [], [], GridCellSelectionMode.Row);
    public bool HasCellSelection => ActiveCell is not null && (IsAllSelected || SelectedRanges.Length > 0);
    public GridCellRange? PrimaryRange => SelectedRanges.IsDefaultOrEmpty ? null : SelectedRanges[^1];
}
