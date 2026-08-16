using System.Collections.Immutable;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

public enum GridFindScope { CurrentRow, CurrentColumn, AllVisibleColumns }
public enum GridFindDirection { Next, Previous }

public sealed record GridFindRequest(string Query, GridFindScope Scope, RowKey? RowKey, VariableCode? VariableCode,
    ImmutableArray<VariableCode> EligibleVariableCodes, GridFindDirection Direction, int StartPosition,
    ImmutableArray<GridSortDefinition> Sorts, ImmutableArray<GridFilterDefinition> Filters, long RequestGeneration);

public sealed record GridFindResult(bool IsMatch, RowKey? RowKey = null, VariableCode? VariableCode = null,
    int? LogicalPosition = null, long RequestGeneration = 0, string? DiagnosticCode = null)
{
    public static GridFindResult Match(RowKey rowKey, VariableCode variableCode, int logicalPosition, long generation) =>
        new(true, rowKey, variableCode, logicalPosition, generation);
    public static GridFindResult NoMatch(long generation) => new(false, RequestGeneration: generation,
        DiagnosticCode: "GRID_FIND_NO_MATCH");
    public static GridFindResult Rejected(string code, long generation) =>
        new(false, RequestGeneration: generation, DiagnosticCode: code);
}

/// <summary>Optional provider-owned search over the current logical result set; it never exposes UI cells.</summary>
public interface IGridFindProvider
{
    Task<GridFindResult> FindAsync(GridProviderContext context, GridFindRequest request,
        CancellationToken cancellationToken = default);
}
