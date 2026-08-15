using System.Collections.Immutable;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

/// <summary>Opaque, stable row identity. It is intentionally unrelated to a visible row index.</summary>
public readonly record struct RowKey
{
    public RowKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }
    public string Value { get; }
    public override string ToString() => Value;
}

public enum GridProviderState { Loading, Ready, Empty, Error, Unavailable }

public sealed record GridRow
{
    public GridRow(RowKey rowKey, IReadOnlyDictionary<VariableCode, object?>? values,
        int errorCount = 0, int warningCount = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(errorCount);
        ArgumentOutOfRangeException.ThrowIfNegative(warningCount);
        RowKey = rowKey;
        Values = (values ?? new Dictionary<VariableCode, object?>()).ToImmutableDictionary();
        ErrorCount = errorCount; WarningCount = warningCount;
    }
    public RowKey RowKey { get; }
    public ImmutableDictionary<VariableCode, object?> Values { get; init; }
    public int ErrorCount { get; }
    public int WarningCount { get; }
    public bool TryGetValue(VariableCode variableCode, out object? value) => Values.TryGetValue(variableCode, out value);
    public GridRow WithValue(VariableCode variableCode, object? value) => this with { Values = Values.SetItem(variableCode, value) };
}

/// <summary>Request is intentionally extensible; 10B can add a viewport without changing grid metadata.</summary>
public sealed record GridDataRequest(ImmutableArray<GridSortDefinition> Sorts, ImmutableArray<GridFilterDefinition> Filters,
    long Generation = 0)
{
    public static GridDataRequest Empty { get; } = new([], []);
}

public sealed record GridDataResult(GridProviderState State, ImmutableArray<GridRow> Rows,
    int TotalRows, int VisibleRows, string? DiagnosticCode = null)
{
    public static GridDataResult Ready(IEnumerable<GridRow> rows, int? totalRows = null)
    {
        var values = rows.ToImmutableArray();
        return new(values.Length == 0 ? GridProviderState.Empty : GridProviderState.Ready,
            values, totalRows ?? values.Length, values.Length);
    }
    public static GridDataResult Failure(GridProviderState state, string diagnosticCode) =>
        state is GridProviderState.Error or GridProviderState.Unavailable
            ? new(state, [], 0, 0, diagnosticCode)
            : throw new ArgumentOutOfRangeException(nameof(state));
}

public sealed record GridProviderContext(CompanyDescriptor Company, string WorkspaceId);
public sealed record GridCellEdit(RowKey RowKey, VariableCode VariableCode, object? CandidateValue);
public sealed record GridCommitResult(bool IsSuccess, object? CommittedValue = null, string? DiagnosticCode = null)
{
    public static GridCommitResult Success(object? value) => new(true, value);
    public static GridCommitResult Rejected(string code) => new(false, null, code);
}

public interface IDataEntryGridProvider
{
    Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request,
        CancellationToken cancellationToken = default);
    Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit,
        CancellationToken cancellationToken = default);
}
