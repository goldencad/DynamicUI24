using System.Collections.Immutable;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

public enum GridRowInsertPlacement { Before, After }

public sealed record GridRowInsertRequest(RowKey AnchorRowKey, GridRowInsertPlacement Placement,
    ImmutableDictionary<VariableCode, object?> InitialValues, int AnchorLogicalPosition, long RequestGeneration)
{
    public static GridRowInsertRequest Create(RowKey anchor, GridRowInsertPlacement placement,
        IReadOnlyDictionary<VariableCode, object?>? values, int anchorLogicalPosition, long generation) =>
        new(anchor, placement, (values ?? new Dictionary<VariableCode, object?>()).ToImmutableDictionary(),
            anchorLogicalPosition, generation);
}

public sealed record GridRowDeleteRequest(ImmutableArray<RowKey> RowKeys, long RequestGeneration)
{
    public static GridRowDeleteRequest Create(IEnumerable<RowKey> rowKeys, long generation) =>
        new(rowKeys.Distinct().ToImmutableArray(), generation);
}

public sealed record GridRowInsertResult(bool IsSuccess, RowKey? InsertedRowKey = null,
    int? TotalRows = null, int? LogicalPosition = null, string? DiagnosticCode = null)
{
    public static GridRowInsertResult Success(RowKey key, int? totalRows = null, int? logicalPosition = null) =>
        new(true, key, totalRows, logicalPosition);
    public static GridRowInsertResult Rejected(string code) => new(false, null, null, null, code);
}

public sealed record GridRowDeleteResult(bool IsSuccess, ImmutableArray<RowKey> DeletedRowKeys,
    int? TotalRows = null, string? DiagnosticCode = null)
{
    public static GridRowDeleteResult Success(IEnumerable<RowKey> keys, int? totalRows = null) =>
        new(true, keys.Distinct().ToImmutableArray(), totalRows);
    public static GridRowDeleteResult Rejected(string code) => new(false, [], null, code);
}

/// <summary>Optional provider-owned row mutation capability. Core never invents RowKey values.</summary>
public interface IGridRowLifecycleProvider
{
    bool CanInsertRows { get; }
    bool CanDeleteRows { get; }
    Task<GridRowInsertResult> InsertRowAsync(GridProviderContext context, GridRowInsertRequest request,
        CancellationToken cancellationToken = default);
    Task<GridRowDeleteResult> DeleteRowsAsync(GridProviderContext context, GridRowDeleteRequest request,
        CancellationToken cancellationToken = default);
}

public interface IGridRowCalculationInvalidation
{
    Task InvalidateRowsAsync(GridProviderContext context, IEnumerable<RowKey> changedRows,
        CancellationToken cancellationToken = default);
}
