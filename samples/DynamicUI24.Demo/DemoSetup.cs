using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;
using DynamicUI24.Core.Templates;
using System.Collections.Immutable;

namespace DynamicUI24.Demo;

internal static class DemoSetup
{
    public static IReadOnlyList<SetupCategoryDefinition> Categories { get; } = CreateCategories();

    public static SetupEditorRegistry CreateEditors(TemplateRegistry templates, DemoSetupProvider provider)
    {
        var fields = new EditorFieldDefinition[]
        {
            new("name", "NAME", new("Setup.Field.Name"), EditorFieldType.Text, 0, true),
            new("description", "DESCRIPTION", new("Setup.Field.Description"), EditorFieldType.MultilineText, 10),
            new("active", "ACTIVE", new("Setup.Field.Active"), EditorFieldType.Boolean, 20, defaultValue: true),
            new("order", "ORDER", new("Setup.Field.Order"), EditorFieldType.Integer, 30),
            new("amount", "AMOUNT", new("Setup.Field.Amount"), EditorFieldType.Decimal, 40),
            new("mode", "MODE", new("Setup.Field.Mode"), EditorFieldType.Choice, 50, choices:
                [new("STANDARD", new("Setup.Choice.Standard")), new("EXTENDED", new("Setup.Choice.Extended"))]),
            new("date", "DATE", new("Setup.Field.Date"), EditorFieldType.Date, 60),
            new("optional-date", "OPTIONAL_DATE", new("Setup.Field.OptionalDate"), EditorFieldType.OptionalDate, 70),
            new("icon", "ICON_KEY", new("Setup.Field.Icon"), EditorFieldType.IconKey, 80),
            new("label", "DISPLAY_NAME_KEY", new("Setup.Field.Localization"), EditorFieldType.Localization, 90),
        };
        var registry = new SetupEditorRegistry();
        registry.Register(new GenericPropertyEditorProvider("GENERIC", fields));
        registry.Register(new DemoCustomEditorProvider());
        DynamicUI24.Template.Setup.SpecializedSetupEditorRegistration.Register(registry, templates,
            provider.GetVariableDefinitions);
        return registry;
    }

    private static IReadOnlyList<SetupCategoryDefinition> CreateCategories()
    {
        var result = new List<SetupCategoryDefinition>
        {
            Category("general", StandardSetupCategoryCodes.General, "Setup.Category.General", StandardIconKeys.Setup, 0, type: "GENERIC"),
            Category("catalogs", StandardSetupCategoryCodes.MasterCatalogs, "Setup.Category.MasterCatalogs", StandardIconKeys.Catalog, 10, type: SpecializedSetupDefinitionTypes.MasterCatalog),
            Category("workspaces", StandardSetupCategoryCodes.Workspaces, "Setup.Category.Workspaces", StandardIconKeys.Workspace, 20, type: SpecializedSetupDefinitionTypes.Workspace),
            Category("metadata", StandardSetupCategoryCodes.ColumnsVariables, "Setup.Category.ColumnsVariables", StandardIconKeys.Columns, 30),
            Category("columns", "COLUMNS", "Setup.Category.Columns", StandardIconKeys.Columns, 0, "metadata", SpecializedSetupDefinitionTypes.Column),
            Category("variables", "VARIABLES", "Setup.Category.Variables", StandardIconKeys.Columns, 10, "metadata", SpecializedSetupDefinitionTypes.Variable),
            Category("formulas", "FORMULAS", "Setup.Category.Formulas", StandardIconKeys.Columns, 20, "metadata", SpecializedSetupDefinitionTypes.Formula),
            Category("navigation", StandardSetupCategoryCodes.NavigationTree, "Setup.Category.NavigationTree", StandardIconKeys.Tree, 40, type: "NAVIGATION_TREE"),
            Category("ribbon", StandardSetupCategoryCodes.Ribbon, "Setup.Category.Ribbon", StandardIconKeys.Ribbon, 50, type: "RIBBON"),
            Category("actions", StandardSetupCategoryCodes.ActionBars, "Setup.Category.ActionBars", StandardIconKeys.Action, 60, type: "ACTION_BARS"),
            Category("dashboard", StandardSetupCategoryCodes.Dashboard, "Setup.Category.Dashboard", StandardIconKeys.Dashboard, 70, type: "DASHBOARD"),
            Category("reports", StandardSetupCategoryCodes.Reports, "Setup.Category.Reports", StandardIconKeys.Report, 80, type: "REPORTS"),
        };
        return result;
    }

    private static SetupCategoryDefinition Category(string id, string code, string key, IconKey icon, int order,
        string? parent = null, string? type = null) => new(id, code, new(key), icon, order, parent, type,
            new PresentationRequirement(new PermissionCode("SETUP.VIEW"), UnauthorizedBehavior: UnauthorizedBehavior.Hide));

    private sealed class DemoCustomEditorProvider : ISetupDefinitionEditorProvider
    {
        public string DefinitionType => "DEMO_CUSTOM";
        public SetupEditorDescriptor CreateEditor(SetupDefinitionDescriptor definition) => new(DefinitionType,
            SetupEditorKind.Custom,
            [new("custom", "CUSTOM_NOTE", new("Setup.Field.CustomNote"), EditorFieldType.MultilineText)]);
    }
}

internal sealed class DemoSetupProvider : ISpecializedSetupDefinitionProvider
{
    private readonly List<SetupDefinitionDescriptor> definitions = CreateDefinitions();

    public IReadOnlyList<SetupDefinitionDescriptor> GetDefinitions(string categoryId, string? scopeKey = null) => definitions
        .Where(x => string.Equals(x.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase))
        .Where(x => x.ScopeKey is null || string.Equals(x.ScopeKey, scopeKey, StringComparison.OrdinalIgnoreCase))
        .ToArray();

    public IReadOnlyList<SetupDefinitionDescriptor> GetDefinitionsByType(string definitionType, string? scopeKey = null) => definitions
        .Where(x => x.DefinitionType.Equals(definitionType, StringComparison.OrdinalIgnoreCase))
        .Where(x => x.ScopeKey is null || string.Equals(x.ScopeKey, scopeKey, StringComparison.OrdinalIgnoreCase))
        .ToArray();

    public SetupDefinitionDescriptor SaveDraft(SetupDefinitionDescriptor candidate) => Upsert(candidate with
    { Status = SetupDefinitionStatus.Draft, ValidationState = candidate.ValidationState });

    public SetupDefinitionDescriptor Publish(SetupDefinitionDescriptor candidate)
    {
        if (candidate.Values.GetValueOrDefault("FAIL_PUBLISH") is true) throw new InvalidOperationException("SETUP_PUBLISH_FAILED");
        return Upsert(candidate with { Status = SetupDefinitionStatus.Published, ValidationState = SetupValidationState.Valid });
    }

    public SetupDefinitionDescriptor Retire(SetupDefinitionDescriptor definition) =>
        Upsert(definition with { Status = SetupDefinitionStatus.Retired, IsEditable = false });

    public IReadOnlyList<VariableDefinition> GetVariableDefinitions() => definitions
        .Where(x => x.DefinitionType == SpecializedSetupDefinitionTypes.Variable)
        .Select(x => new VariableDefinition(x.DefinitionId,
            new VariableCode(x.Values[SpecializedSetupFieldCodes.VariableCode]!.ToString()!),
            x.Values[SpecializedSetupFieldCodes.DisplayNameKey]!.ToString()!,
            x.Values.GetValueOrDefault(SpecializedSetupFieldCodes.DescriptionKey)?.ToString(),
            Enum.Parse<ColumnDataType>(x.Values[SpecializedSetupFieldCodes.DataType]!.ToString()!.Replace("_", string.Empty), true),
            Enum.Parse<VariableScope>(x.Values[SpecializedSetupFieldCodes.VariableScope]!.ToString()!, true),
            x.Version, x.Status, x.IsSystem)).ToArray();

    private SetupDefinitionDescriptor Upsert(SetupDefinitionDescriptor definition)
    {
        var index = definitions.FindIndex(x => x.DefinitionId.Equals(definition.DefinitionId, StringComparison.OrdinalIgnoreCase));
        if (index < 0) definitions.Add(definition); else definitions[index] = definition;
        return definition;
    }

    private static List<SetupDefinitionDescriptor> CreateDefinitions()
    {
        var list = new List<SetupDefinitionDescriptor>
        {
            Definition("general-1", "GENERAL_SETTINGS", "General settings", "GENERIC", "general", SetupDefinitionStatus.Draft,
                new Dictionary<string, object?> { ["NAME"] = "General settings", ["ACTIVE"] = true, ["MODE"] = "STANDARD" }),
            Definition("system-1", "SYSTEM_DEFAULTS", "System defaults", "GENERIC", "general", SetupDefinitionStatus.Published,
                new Dictionary<string, object?> { ["NAME"] = "System defaults", ["ACTIVE"] = true }, true, false),
            Definition("invalid-1", "INVALID_SAMPLE", "Invalid sample", "GENERIC", "general", SetupDefinitionStatus.Invalid,
                new Dictionary<string, object?> { ["NAME"] = "" }),
            Definition("retired-1", "RETIRED_SAMPLE", "Retired sample", "GENERIC", "general", SetupDefinitionStatus.Retired,
                new Dictionary<string, object?> { ["NAME"] = "Retired sample" }, false, false),
            Specialized("workspace-1", "PRIMARY_WORKSPACE", "Primary workspace", SpecializedSetupDefinitionTypes.Workspace, "workspaces", SetupDefinitionStatus.Published,
                new() { ["DISPLAY_NAME_KEY"] = "Workspace.Primary", ["TEMPLATE_CODE"] = "DATA_ENTRY", ["DISPLAY_ORDER"] = 10, ["ICON_KEY"] = "WORKSPACE", ["IS_ACTIVE"] = true, ["COMPANY_SCOPE"] = "GLOBAL" }),
            Specialized("workspace-2", "ANALYTICS_WORKSPACE", "Analytics workspace", SpecializedSetupDefinitionTypes.Workspace, "workspaces", SetupDefinitionStatus.Draft,
                new() { ["DISPLAY_NAME_KEY"] = "Workspace.Analytics", ["TEMPLATE_CODE"] = "DASHBOARD", ["DISPLAY_ORDER"] = 20, ["ICON_KEY"] = "DASHBOARD", ["IS_ACTIVE"] = true, ["COMPANY_SCOPE"] = "GLOBAL" }),
            Specialized("workspace-3", "CALENDAR_WORKSPACE", "Calendar workspace", SpecializedSetupDefinitionTypes.Workspace, "workspaces", SetupDefinitionStatus.Draft,
                new() { ["DISPLAY_NAME_KEY"] = "Workspace.Calendar", ["TEMPLATE_CODE"] = "CALENDAR", ["DISPLAY_ORDER"] = 30, ["ICON_KEY"] = "CALENDAR", ["IS_ACTIVE"] = true, ["COMPANY_SCOPE"] = "COMPANY" }, scope: "demo-company-a"),
            Specialized("workspace-unknown", "UNKNOWN_WORKSPACE", "Unknown template safety proof", SpecializedSetupDefinitionTypes.Workspace, "workspaces", SetupDefinitionStatus.Invalid,
                new() { ["DISPLAY_NAME_KEY"] = "Workspace.Unknown", ["TEMPLATE_CODE"] = "UNKNOWN", ["DISPLAY_ORDER"] = 40, ["ICON_KEY"] = "WORKSPACE", ["IS_ACTIVE"] = false, ["COMPANY_SCOPE"] = "GLOBAL" }),
        };
        foreach (var type in new[] { "navigation", "ribbon", "actions", "dashboard", "reports" })
            list.Add(Definition(type + "-1", type.ToUpperInvariant() + "_SAMPLE", "Foundation placeholder",
                type.ToUpperInvariant() switch { "COLUMNS" => "COLUMNS_VARIABLES", "NAVIGATION" => "NAVIGATION_TREE", "ACTIONS" => "ACTION_BARS", _ => type.ToUpperInvariant() },
                type, SetupDefinitionStatus.Draft, new Dictionary<string, object?>()));
        var catalogNames = new[] { "People", "Organization", "Employee type", "Department", "Position", "Finance", "Currency", "Bank", "Region", "Unit" };
        for (var index = 0; index < catalogNames.Length; index++)
        {
            var id = $"catalog-{index + 1:00}";
            var parent = index is 2 or 3 or 4 ? "catalog-01" : index is 6 or 7 ? "catalog-06" : null;
            list.Add(Specialized(id, catalogNames[index].Replace(' ', '_').ToUpperInvariant(), catalogNames[index], SpecializedSetupDefinitionTypes.MasterCatalog, "catalogs",
                index == 0 ? SetupDefinitionStatus.Published : SetupDefinitionStatus.Draft,
                new() { ["DISPLAY_NAME_KEY"] = $"Catalog.{index + 1:00}", ["PARENT_CATALOG_ID"] = parent, ["DISPLAY_ORDER"] = (index + 1) * 10,
                    ["ICON_KEY"] = "CATALOG", ["IS_ACTIVE"] = true, ["IS_EDITABLE"] = true, ["COMPANY_SCOPE"] = index == 9 ? "COMPANY" : "GLOBAL" },
                scope: index == 9 ? "demo-company-a" : null));
        }
        var variableCodes = new[] { "QUANTITY", "UNIT_PRICE", "TOTAL_AMOUNT", "TAX_RATE", "TAX_AMOUNT", "NET_AMOUNT", "CREATED_AT", "IS_ACTIVE", "REFERENCE_CODE", "NOTES" };
        for (var index = 0; index < variableCodes.Length; index++)
            list.Add(Specialized($"variable-{index + 1:00}", variableCodes[index], variableCodes[index], SpecializedSetupDefinitionTypes.Variable, "variables",
                index == 6 ? SetupDefinitionStatus.Published : SetupDefinitionStatus.Draft,
                new() { ["VARIABLE_CODE"] = variableCodes[index], ["DISPLAY_NAME_KEY"] = $"Variable.{variableCodes[index]}",
                    ["DATA_TYPE"] = index switch { 6 => "DATETIME", 7 => "BOOLEAN", 9 => "MULTILINE_TEXT", _ => "DECIMAL" },
                    ["VARIABLE_SCOPE"] = "ROW", ["IS_SYSTEM"] = index == 6 }, system: index == 6, editable: index != 6));
        for (var index = 0; index < 10; index++)
        {
            var mode = index switch { 7 => "FORMULA", 8 => "SYSTEM", _ => "INPUT" };
            list.Add(Specialized($"column-{index + 1:00}", $"COL_{index + 1:00}", $"Column {index + 1:00}", SpecializedSetupDefinitionTypes.Column, "columns", SetupDefinitionStatus.Draft,
                new() { ["VARIABLE_CODE"] = variableCodes[index], ["DISPLAY_NAME_KEY"] = $"Column.{index + 1:00}", ["DATA_TYPE"] = index == 8 ? "SYSTEM" : "DECIMAL",
                    ["EDITOR_KIND"] = mode == "INPUT" ? "NUMBER" : mode, ["COLUMN_MODE"] = mode, ["DISPLAY_ORDER"] = (index + 1) * 10,
                    ["WIDTH"] = 140m, ["MIN_WIDTH"] = 80m, ["MAX_WIDTH"] = 280m, ["IS_VISIBLE"] = true, ["IS_REQUIRED"] = false, ["IS_EDITABLE"] = mode == "INPUT",
                    ["FORMULA_DEFINITION_ID"] = mode == "FORMULA" ? "formula-total" : null }));
        }
        list.Add(Specialized("formula-total", "TOTAL", "Line total", SpecializedSetupDefinitionTypes.Formula, "formulas", SetupDefinitionStatus.Draft,
            new() { ["FORMULA_CODE"] = "TOTAL", ["DISPLAY_NAME_KEY"] = "Formula.Total", ["RESULT_VARIABLE_CODE"] = "TOTAL_AMOUNT", ["EXPRESSION_TEXT"] = "QUANTITY * UNIT_PRICE", ["REFERENCED_VARIABLE_CODES"] = "QUANTITY,UNIT_PRICE" }));
        list.Add(Specialized("formula-net", "NET", "Net amount", SpecializedSetupDefinitionTypes.Formula, "formulas", SetupDefinitionStatus.Published,
            new() { ["FORMULA_CODE"] = "NET", ["DISPLAY_NAME_KEY"] = "Formula.Net", ["RESULT_VARIABLE_CODE"] = "NET_AMOUNT", ["EXPRESSION_TEXT"] = "TOTAL_AMOUNT + TAX_AMOUNT", ["REFERENCED_VARIABLE_CODES"] = "TOTAL_AMOUNT,TAX_AMOUNT" }, editable: false));
        list.Add(Specialized("formula-invalid", "INVALID_REFERENCE", "Invalid reference proof", SpecializedSetupDefinitionTypes.Formula, "formulas", SetupDefinitionStatus.Invalid,
            new() { ["FORMULA_CODE"] = "INVALID_REFERENCE", ["DISPLAY_NAME_KEY"] = "Formula.Invalid", ["RESULT_VARIABLE_CODE"] = "TOTAL_AMOUNT", ["EXPRESSION_TEXT"] = "UNKNOWN_VALUE", ["REFERENCED_VARIABLE_CODES"] = "UNKNOWN_VALUE" }));
        return list;
    }

    private static SetupDefinitionDescriptor Specialized(string id, string code, string name, string type, string category,
        SetupDefinitionStatus status, Dictionary<string, object?> values, bool system = false, bool editable = true, string? scope = null) =>
        Definition(id, code, name, type, category, status, values, system, editable, scope);

    private static SetupDefinitionDescriptor Definition(string id, string code, string name, string type, string category,
        SetupDefinitionStatus status, IReadOnlyDictionary<string, object?> values, bool system = false, bool editable = true,
        string? scope = null) => new(id, code, name, type, status: status, isSystem: system, isEditable: editable,
            validationState: status == SetupDefinitionStatus.Invalid ? SetupValidationState.Invalid :
                status == SetupDefinitionStatus.Published ? SetupValidationState.Valid : SetupValidationState.NotValidated,
            values: values, categoryId: category, scopeKey: scope);
}
