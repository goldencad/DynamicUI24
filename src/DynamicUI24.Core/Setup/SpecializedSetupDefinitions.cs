using System.Collections.Immutable;
using DynamicUI24.Core.Templates;

namespace DynamicUI24.Core.Setup;

public static class SpecializedSetupDefinitionTypes
{
    public const string MasterCatalog = "MASTER_CATALOG";
    public const string Workspace = "WORKSPACE";
    public const string Column = "COLUMN";
    public const string Variable = "VARIABLE";
    public const string Formula = "FORMULA";
}

public static class SpecializedSetupFieldCodes
{
    public const string DisplayNameKey = "DISPLAY_NAME_KEY";
    public const string DescriptionKey = "DESCRIPTION_KEY";
    public const string ParentCatalogId = "PARENT_CATALOG_ID";
    public const string DisplayOrder = "DISPLAY_ORDER";
    public const string IconKey = "ICON_KEY";
    public const string IsActive = "IS_ACTIVE";
    public const string IsEditable = "IS_EDITABLE";
    public const string CompanyScope = "COMPANY_SCOPE";
    public const string PermissionRequirement = "PERMISSION_REQUIREMENT";
    public const string TemplateCode = "TEMPLATE_CODE";
    public const string VariableCode = "VARIABLE_CODE";
    public const string DataType = "DATA_TYPE";
    public const string EditorKind = "EDITOR_KIND";
    public const string ColumnMode = "COLUMN_MODE";
    public const string Width = "WIDTH";
    public const string MinWidth = "MIN_WIDTH";
    public const string MaxWidth = "MAX_WIDTH";
    public const string IsVisible = "IS_VISIBLE";
    public const string IsRequired = "IS_REQUIRED";
    public const string Format = "FORMAT";
    public const string DefaultValue = "DEFAULT_VALUE";
    public const string ValidationDefinition = "VALIDATION_DEFINITION";
    public const string FormulaDefinitionId = "FORMULA_DEFINITION_ID";
    public const string VariableScope = "VARIABLE_SCOPE";
    public const string FormulaCode = "FORMULA_CODE";
    public const string ResultVariableCode = "RESULT_VARIABLE_CODE";
    public const string ExpressionText = "EXPRESSION_TEXT";
    public const string ReferencedVariableCodes = "REFERENCED_VARIABLE_CODES";
}

public enum SetupCompanyScope { Global, Company }
public enum ColumnDataType { Text, MultilineText, Integer, Decimal, Boolean, Date, DateTime, Choice, Reference, Formula, System }
public enum ColumnEditorKind { TextBox, Number, Checkbox, DatePicker, ComboBox, Lookup, ReadOnly, Formula }
public enum ColumnMode { Input, Formula, System }
public enum VariableScope { Row, Workspace, Document, Company, Application }

/// <summary>Stable technical identifiers compare after trim and invariant uppercase normalization.</summary>
public readonly record struct VariableCode
{
    public VariableCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        if (!TechnicalCode.IsValid(normalized))
            throw new ArgumentException("VariableCode contains unsupported characters.", nameof(value));
        Value = normalized;
    }
    public string Value { get; }
    public override string ToString() => Value;
}

public sealed record MasterCatalogDefinition(string CatalogId, string CatalogCode, string DisplayNameKey,
    string? DescriptionKey, string? ParentCatalogId, int DisplayOrder, string IconKey, bool IsActive,
    bool IsSystem, bool IsEditable, SetupCompanyScope CompanyScope, string? PermissionRequirement,
    int Version, SetupDefinitionStatus Status, DateOnly? EffectiveFrom = null, DateOnly? EffectiveTo = null);

public sealed record WorkspaceSetupDefinition(string WorkspaceId, string WorkspaceCode, string DisplayNameKey,
    string? DescriptionKey, string TemplateCode, int DisplayOrder, string IconKey, bool IsActive,
    SetupCompanyScope CompanyScope, string? PermissionRequirement, int Version, SetupDefinitionStatus Status,
    DateOnly? EffectiveFrom = null, DateOnly? EffectiveTo = null);

public sealed record ColumnDefinition(string ColumnId, string ColumnCode, VariableCode VariableCode,
    string DisplayNameKey, string? DescriptionKey, ColumnDataType DataType, ColumnEditorKind EditorKind,
    ColumnMode Mode, int DisplayOrder, decimal? Width, decimal? MinWidth, decimal? MaxWidth,
    bool IsVisible, bool IsRequired, string? PermissionRequirement, string? Format, string? DefaultValue,
    string? ValidationDefinition, string? FormulaDefinitionId, int Version, SetupDefinitionStatus Status);

public sealed record VariableDefinition(string VariableId, VariableCode VariableCode, string DisplayNameKey,
    string? DescriptionKey, ColumnDataType DataType, VariableScope Scope, int Version,
    SetupDefinitionStatus Status, bool IsSystem, string? PermissionRequirement = null);

/// <summary>Declarative metadata only. ExpressionText is never executed by the Setup framework.</summary>
public sealed record FormulaDefinition(string FormulaId, string FormulaCode, string DisplayNameKey,
    VariableCode ResultVariableCode, string ExpressionText, ImmutableArray<VariableCode> ReferencedVariableCodes,
    int Version, SetupDefinitionStatus Status, bool IsReadOnly);

public interface ISpecializedSetupDefinitionProvider : ISetupDefinitionProvider
{
    IReadOnlyList<SetupDefinitionDescriptor> GetDefinitionsByType(string definitionType, string? scopeKey = null);
}

public static class MasterCatalogHierarchyValidator
{
    public static ImmutableArray<SetupMetadataDiagnostic> Validate(IEnumerable<MasterCatalogDefinition> catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        var items = catalogs.OrderBy(x => x.DisplayOrder).ThenBy(x => x.CatalogCode, StringComparer.Ordinal).ToArray();
        var diagnostics = ImmutableArray.CreateBuilder<SetupMetadataDiagnostic>();
        foreach (var group in items.GroupBy(x => x.CatalogId, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
            diagnostics.Add(new("CATALOG_DUPLICATE_ID", $"Duplicate CatalogId '{group.Key}'.", group.Key));
        foreach (var group in items.GroupBy(x => x.CatalogCode, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
            diagnostics.Add(new("CATALOG_DUPLICATE_CODE", $"Duplicate CatalogCode '{group.Key}'.", group.Key));
        var byId = items.GroupBy(x => x.CatalogId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var item in items.Where(x => x.ParentCatalogId is not null))
            if (!byId.ContainsKey(item.ParentCatalogId!))
                diagnostics.Add(new("CATALOG_ORPHAN", $"Parent catalog '{item.ParentCatalogId}' does not exist.", item.CatalogId));
        foreach (var item in items)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = item;
            while (visited.Add(current.CatalogId) && current.ParentCatalogId is { } parent && byId.TryGetValue(parent, out var next))
                current = next;
            if (current.ParentCatalogId is not null && !visited.Add(current.CatalogId))
                diagnostics.Add(new("CATALOG_CYCLE", $"A catalog hierarchy cycle includes '{item.CatalogCode}'.", item.CatalogId));
        }
        return diagnostics.Distinct().ToImmutableArray();
    }
}

public sealed class SpecializedSetupValidator : ISetupDefinitionValidator
{
    private static readonly string[] ForbiddenFormulaTokens = ["C#", "ASSEMBLY", "SELECT ", "INSERT ", "UPDATE ", "DELETE ",
        "DROP ", "EXEC ", "JAVASCRIPT", "<SCRIPT", "SHELL", "PROCESS.START"];
    private readonly ISetupDefinitionProvider provider;
    private readonly TemplateRegistry templates;

    public SpecializedSetupValidator(ISetupDefinitionProvider provider, TemplateRegistry templates)
    { this.provider = provider; this.templates = templates; }

    public SetupValidationResult Validate(SetupDefinitionDescriptor candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var errors = ImmutableArray.CreateBuilder<SetupValidationDiagnostic>();
        if (candidate.Values.TryGetValue("NAME", out var name) && string.IsNullOrWhiteSpace(name?.ToString()))
            Error("SETUP_REQUIRED", "A required name is missing.", errors, "NAME");
        if (candidate.EffectiveFrom is { } from && candidate.EffectiveTo is { } to && to < from)
            Error("SETUP_EFFECTIVE_RANGE", "EffectiveTo cannot precede EffectiveFrom.", errors);
        switch (candidate.DefinitionType)
        {
            case SpecializedSetupDefinitionTypes.MasterCatalog: ValidateCatalog(candidate, errors); break;
            case SpecializedSetupDefinitionTypes.Workspace: ValidateWorkspace(candidate, errors); break;
            case SpecializedSetupDefinitionTypes.Column: ValidateColumn(candidate, errors); break;
            case SpecializedSetupDefinitionTypes.Variable: ValidateVariable(candidate, errors); break;
            case SpecializedSetupDefinitionTypes.Formula: ValidateFormula(candidate, errors); break;
        }
        return new(errors.ToImmutable());
    }

    private void ValidateCatalog(SetupDefinitionDescriptor item, ImmutableArray<SetupValidationDiagnostic>.Builder errors)
    {
        RequireCode(item.DefinitionCode, "CATALOG_CODE_REQUIRED", errors);
        var definitions = provider.GetDefinitions(item.CategoryId ?? string.Empty, item.ScopeKey)
            .Where(x => !x.DefinitionId.Equals(item.DefinitionId, StringComparison.OrdinalIgnoreCase)).Select(ToCatalog)
            .Append(ToCatalog(item));
        foreach (var diagnostic in MasterCatalogHierarchyValidator.Validate(definitions))
            Error(diagnostic.Code, diagnostic.Message, errors, SpecializedSetupFieldCodes.ParentCatalogId);
    }

    private void ValidateWorkspace(SetupDefinitionDescriptor item, ImmutableArray<SetupValidationDiagnostic>.Builder errors)
    {
        RequireCode(item.DefinitionCode, "WORKSPACE_CODE_REQUIRED", errors);
        var template = Text(item, SpecializedSetupFieldCodes.TemplateCode);
        if (string.IsNullOrWhiteSpace(template) || !TryTemplate(template))
            Error("WORKSPACE_UNKNOWN_TEMPLATE", $"TemplateCode '{template}' is not registered.", errors, SpecializedSetupFieldCodes.TemplateCode);
        Duplicate(item, "WORKSPACE_DUPLICATE_CODE", errors);
    }

    private void ValidateColumn(SetupDefinitionDescriptor item, ImmutableArray<SetupValidationDiagnostic>.Builder errors)
    {
        RequireCode(item.DefinitionCode, "COLUMN_CODE_REQUIRED", errors);
        var variable = Text(item, SpecializedSetupFieldCodes.VariableCode);
        if (!TryVariableCode(variable)) Error("COLUMN_VARIABLE_CODE_INVALID", "VariableCode is required and must be a valid technical code.", errors, SpecializedSetupFieldCodes.VariableCode);
        RequireEnum<ColumnDataType>(item, SpecializedSetupFieldCodes.DataType, "COLUMN_DATA_TYPE_INVALID", errors);
        RequireEnum<ColumnEditorKind>(item, SpecializedSetupFieldCodes.EditorKind, "COLUMN_EDITOR_KIND_INVALID", errors);
        var mode = RequireEnum<ColumnMode>(item, SpecializedSetupFieldCodes.ColumnMode, "COLUMN_MODE_INVALID", errors);
        var width = Decimal(item, SpecializedSetupFieldCodes.Width); var min = Decimal(item, SpecializedSetupFieldCodes.MinWidth); var max = Decimal(item, SpecializedSetupFieldCodes.MaxWidth);
        if (width is <= 0 || min is <= 0 || max is <= 0 || (min.HasValue && max.HasValue && min > max) ||
            (width.HasValue && min.HasValue && width < min) || (width.HasValue && max.HasValue && width > max))
            Error("COLUMN_GEOMETRY_INVALID", "Width must be positive and within MinWidth/MaxWidth.", errors, SpecializedSetupFieldCodes.Width);
        if (mode is ColumnMode.Formula or ColumnMode.System && Bool(item, "IS_EDITABLE"))
            Error("COLUMN_READ_ONLY_REQUIRED", "FORMULA and SYSTEM columns must be read-only.", errors, SpecializedSetupFieldCodes.ColumnMode);
        Duplicate(item, "COLUMN_DUPLICATE_CODE", errors);
        DuplicateValue(item, SpecializedSetupFieldCodes.VariableCode, "COLUMN_DUPLICATE_VARIABLE_CODE", errors);
    }

    private void ValidateVariable(SetupDefinitionDescriptor item, ImmutableArray<SetupValidationDiagnostic>.Builder errors)
    {
        if (!TryVariableCode(Text(item, SpecializedSetupFieldCodes.VariableCode)))
            Error("VARIABLE_CODE_INVALID", "VariableCode is required and must be a valid technical code.", errors, SpecializedSetupFieldCodes.VariableCode);
        RequireEnum<ColumnDataType>(item, SpecializedSetupFieldCodes.DataType, "VARIABLE_DATA_TYPE_INVALID", errors);
        RequireEnum<VariableScope>(item, SpecializedSetupFieldCodes.VariableScope, "VARIABLE_SCOPE_INVALID", errors);
        DuplicateValue(item, SpecializedSetupFieldCodes.VariableCode, "VARIABLE_DUPLICATE_CODE", errors);
        if (item.IsPublished && !string.Equals(Text(item, SpecializedSetupFieldCodes.VariableCode),
                Text(FindStored(item), SpecializedSetupFieldCodes.VariableCode), StringComparison.OrdinalIgnoreCase))
            Error("VARIABLE_CODE_IMMUTABLE", "A published VariableCode can change only in a new version.", errors, SpecializedSetupFieldCodes.VariableCode);
    }

    private void ValidateFormula(SetupDefinitionDescriptor item, ImmutableArray<SetupValidationDiagnostic>.Builder errors)
    {
        var formulaCode = Text(item, SpecializedSetupFieldCodes.FormulaCode);
        RequireCode(formulaCode, "FORMULA_CODE_INVALID", errors);
        DuplicateValue(item, SpecializedSetupFieldCodes.FormulaCode, "FORMULA_DUPLICATE_CODE", errors);
        var result = Text(item, SpecializedSetupFieldCodes.ResultVariableCode);
        var references = Codes(item, SpecializedSetupFieldCodes.ReferencedVariableCodes);
        var variables = (provider as ISpecializedSetupDefinitionProvider)?.GetDefinitionsByType(SpecializedSetupDefinitionTypes.Variable, item.ScopeKey) ?? [];
        var variableCodes = variables
            .Select(x => Text(x, SpecializedSetupFieldCodes.VariableCode)).Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!variableCodes.Contains(result)) Error("FORMULA_RESULT_UNKNOWN", $"Result VariableCode '{result}' does not exist.", errors, SpecializedSetupFieldCodes.ResultVariableCode);
        foreach (var reference in references.Where(x => !variableCodes.Contains(x)))
            Error("FORMULA_REFERENCE_UNKNOWN", $"Referenced VariableCode '{reference}' does not exist.", errors, SpecializedSetupFieldCodes.ReferencedVariableCodes);
        if (references.Any(x => x.Equals(result, StringComparison.OrdinalIgnoreCase)))
            Error("FORMULA_SELF_REFERENCE", "A formula cannot reference its result VariableCode.", errors, SpecializedSetupFieldCodes.ReferencedVariableCodes);
        if (references.Distinct(StringComparer.OrdinalIgnoreCase).Count() != references.Length)
            Error("FORMULA_DUPLICATE_REFERENCE", "Referenced VariableCodes must be unique.", errors, SpecializedSetupFieldCodes.ReferencedVariableCodes);
        var expression = Text(item, SpecializedSetupFieldCodes.ExpressionText);
        if (string.IsNullOrWhiteSpace(expression))
            Error("FORMULA_EXPRESSION_REQUIRED", "Declarative expression metadata is required.", errors, SpecializedSetupFieldCodes.ExpressionText);
        if (ForbiddenFormulaTokens.Any(x => expression.Contains(x, StringComparison.OrdinalIgnoreCase)))
            Error("FORMULA_EXECUTABLE_SYNTAX_FORBIDDEN", "Formula metadata cannot contain code, SQL, shell, JavaScript, or executable scripts.", errors, SpecializedSetupFieldCodes.ExpressionText);
    }

    private SetupDefinitionDescriptor? FindStored(SetupDefinitionDescriptor item) => provider
        .GetDefinitions(item.CategoryId ?? string.Empty, item.ScopeKey).FirstOrDefault(x => x.DefinitionId.Equals(item.DefinitionId, StringComparison.OrdinalIgnoreCase));
    private void Duplicate(SetupDefinitionDescriptor item, string code, ImmutableArray<SetupValidationDiagnostic>.Builder errors)
    {
        if (provider.GetDefinitions(item.CategoryId ?? string.Empty, item.ScopeKey).Any(x => x.DefinitionId != item.DefinitionId && x.DefinitionCode.Equals(item.DefinitionCode, StringComparison.OrdinalIgnoreCase)))
            Error(code, $"Duplicate definition code '{item.DefinitionCode}'.", errors);
    }
    private void DuplicateValue(SetupDefinitionDescriptor item, string field, string code, ImmutableArray<SetupValidationDiagnostic>.Builder errors)
    {
        var value = Text(item, field);
        if (provider.GetDefinitions(item.CategoryId ?? string.Empty, item.ScopeKey).Any(x => x.DefinitionId != item.DefinitionId && Text(x, field).Equals(value, StringComparison.OrdinalIgnoreCase)))
            Error(code, $"Duplicate {field} '{value}'.", errors, field);
    }
    private bool TryTemplate(string code) { try { return templates.Resolve(new TemplateCode(code)).IsSuccess; } catch { return false; } }
    private static bool TryVariableCode(string value) { try { _ = new VariableCode(value); return true; } catch { return false; } }
    private static T? RequireEnum<T>(SetupDefinitionDescriptor item, string field, string code, ImmutableArray<SetupValidationDiagnostic>.Builder errors) where T : struct
    { if (Enum.TryParse<T>(Text(item, field).Replace("_", string.Empty), true, out var value)) return value; Error(code, $"{field} is unknown.", errors, field); return null; }
    private static void RequireCode(string code, string error, ImmutableArray<SetupValidationDiagnostic>.Builder errors)
    { if (string.IsNullOrWhiteSpace(code) || !TechnicalCode.IsValid(code)) Error(error, "A valid technical code is required.", errors); }
    private static string Text(SetupDefinitionDescriptor? item, string field) => item?.Values.GetValueOrDefault(field)?.ToString()?.Trim() ?? string.Empty;
    private static decimal? Decimal(SetupDefinitionDescriptor item, string field) => decimal.TryParse(Text(item, field), out var value) ? value : null;
    private static bool Bool(SetupDefinitionDescriptor item, string field) => item.Values.GetValueOrDefault(field) is true;
    private static string[] Codes(SetupDefinitionDescriptor item, string field) => Text(item, field).Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static void Error(string code, string message, ImmutableArray<SetupValidationDiagnostic>.Builder errors, string? field = null) =>
        errors.Add(new(SetupDiagnosticSeverity.Error, code, new("Setup.Validation.Specialized"), message, field));
    private static MasterCatalogDefinition ToCatalog(SetupDefinitionDescriptor x) => new(x.DefinitionId, x.DefinitionCode,
        Text(x, SpecializedSetupFieldCodes.DisplayNameKey), Text(x, SpecializedSetupFieldCodes.DescriptionKey), Null(Text(x, SpecializedSetupFieldCodes.ParentCatalogId)),
        int.TryParse(Text(x, SpecializedSetupFieldCodes.DisplayOrder), out var order) ? order : 0, Text(x, SpecializedSetupFieldCodes.IconKey), Bool(x, SpecializedSetupFieldCodes.IsActive),
        x.IsSystem, x.IsEditable, Enum.TryParse<SetupCompanyScope>(Text(x, SpecializedSetupFieldCodes.CompanyScope), true, out var scope) ? scope : SetupCompanyScope.Global,
        Text(x, SpecializedSetupFieldCodes.PermissionRequirement), x.Version, x.Status, x.EffectiveFrom, x.EffectiveTo);
    private static string? Null(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
