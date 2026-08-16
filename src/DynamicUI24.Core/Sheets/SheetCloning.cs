using System.Collections.Immutable;

namespace DynamicUI24.Core.Sheets;

public enum SheetCloneMode { DuplicateFull, StructureOnly, StructureAndFormulas, NewDataContext, Custom }
public enum SheetReferenceMappingPolicy { None, Explicit, AuthoritativeProvider }

public sealed record SheetClonePolicy(
    SheetCloneMode Mode, bool CopyStructure, bool CopyFormulas, bool CopyValues, bool CopyLayout,
    bool CopyFilters, bool CopySort, bool CopyPermissionsMetadata, bool CopyContentPreferences,
    bool ResetRowKeys, bool ResetEditHistory, bool ResetUndoRedo, bool ResetImportRuntime,
    SheetReferenceMappingPolicy ReferenceMappingPolicy)
{
    public static SheetClonePolicy DuplicateFull { get; } = new(SheetCloneMode.DuplicateFull, true, true, true,
        true, true, true, true, true, true, true, true, true, SheetReferenceMappingPolicy.AuthoritativeProvider);
    public static SheetClonePolicy StructureOnly { get; } = new(SheetCloneMode.StructureOnly, true, false, false,
        true, false, false, false, false, true, true, true, true, SheetReferenceMappingPolicy.None);
    /// <summary>Safe generic profile for a logically new provider/application-defined data context.</summary>
    public static SheetClonePolicy NewDataContext(bool copyValues = false, bool copySort = false) => new(
        SheetCloneMode.NewDataContext, true, true, copyValues, true, false, copySort, true, true,
        true, true, true, true, SheetReferenceMappingPolicy.Explicit);
}

public sealed record SheetReferenceMapping
{
    public SheetReferenceMapping(SheetCode sourceSheetCode, SheetCode targetSheetCode)
    {
        if (sourceSheetCode == targetSheetCode) throw new ArgumentException("A sheet mapping must change identity.");
        SourceSheetCode = sourceSheetCode; TargetSheetCode = targetSheetCode;
    }
    public SheetCode SourceSheetCode { get; }
    public SheetCode TargetSheetCode { get; }
}

public sealed record SheetCloneRequest(SheetCode SourceSheetCode, SheetCode TargetSheetCode,
    string TargetTitle, SheetClonePolicy Policy, ImmutableArray<SheetReferenceMapping> ReferenceMappings,
    string? TargetDataContext = null)
{
    public static SheetCloneRequest Create(SheetCode source, SheetCode target, string title,
        SheetClonePolicy policy, IEnumerable<SheetReferenceMapping>? mappings = null, string? targetDataContext = null)
    {
        if (source == target) throw new ArgumentException("Clone target must have a new SheetCode.", nameof(target));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var values = (mappings ?? []).ToImmutableArray();
        if (values.GroupBy(x => x.SourceSheetCode).Any(x => x.Count() > 1) ||
            values.GroupBy(x => x.TargetSheetCode).Any(x => x.Count() > 1))
            throw new ArgumentException("Reference mappings contain collisions.", nameof(mappings));
        return new(source, target, title.Trim(), policy, values, targetDataContext);
    }
}

public sealed record SheetLifecycleResult(bool IsSuccess, SheetDefinition? Sheet = null,
    string? DiagnosticCode = null, bool RequiresConfirmation = false)
{
    public static SheetLifecycleResult Success(SheetDefinition sheet) => new(true, sheet);
    public static SheetLifecycleResult Rejected(string code, bool confirmation = false) => new(false, null, code, confirmation);
}

public interface ISheetLifecycleProvider
{
    Task<SheetLifecycleResult> CreateAsync(CancellationToken cancellationToken = default);
    Task<SheetLifecycleResult> CloneAsync(SheetCloneRequest request, CancellationToken cancellationToken = default);
    Task<SheetLifecycleResult> DeleteAsync(SheetCode sheetCode, CancellationToken cancellationToken = default);
}

public enum SheetCalculationOperation { CloneValidation, DeleteValidation, Recalculation }
public enum SheetCalculationDiagnosticSeverity { Information, Warning, Error }
public sealed record SheetCalculationDiagnostic(string Code, SheetCalculationDiagnosticSeverity Severity,
    SheetCode? SheetCode = null, string? SafeReferenceCode = null);
public sealed record SheetCalculationResult(bool IsSuccess, ImmutableArray<SheetCode> AffectedSheets,
    ImmutableArray<SheetCalculationDiagnostic> Diagnostics, bool RequiresConfirmation = false)
{
    public static SheetCalculationResult Success(IEnumerable<SheetCode>? affected = null) => new(true,
        (affected ?? []).Distinct().OrderBy(x => x.Value, StringComparer.Ordinal).ToImmutableArray(), []);
}

/// <summary>Compatibility seam only. Implementations own parsing, dependencies, cycles and formula evaluation.</summary>
public interface ISheetCalculationCompatibility
{
    Task<SheetCalculationResult> ValidateCloneAsync(SheetCloneRequest request, CancellationToken cancellationToken = default);
    Task<SheetCalculationResult> ValidateDeleteAsync(SheetCode sheetCode, CancellationToken cancellationToken = default);
    Task<SheetCalculationResult> RequestRecalculationAsync(IEnumerable<SheetCode> changedSheets,
        CancellationToken cancellationToken = default);
}
