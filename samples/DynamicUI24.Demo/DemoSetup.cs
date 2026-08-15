using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;
using System.Collections.Immutable;

namespace DynamicUI24.Demo;

internal static class DemoSetup
{
    public static IReadOnlyList<SetupCategoryDefinition> Categories { get; } = CreateCategories();

    public static SetupEditorRegistry CreateEditors()
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
        registry.Register(new GenericPropertyEditorProvider("CATALOG", fields));
        registry.Register(new GenericPropertyEditorProvider("WORKSPACE", fields));
        registry.Register(new DemoCustomEditorProvider());
        return registry;
    }

    private static IReadOnlyList<SetupCategoryDefinition> CreateCategories()
    {
        var result = new List<SetupCategoryDefinition>
        {
            Category("general", StandardSetupCategoryCodes.General, "Setup.Category.General", StandardIconKeys.Setup, 0, type: "GENERIC"),
            Category("catalogs", StandardSetupCategoryCodes.MasterCatalogs, "Setup.Category.MasterCatalogs", StandardIconKeys.Catalog, 10),
            Category("workspaces", StandardSetupCategoryCodes.Workspaces, "Setup.Category.Workspaces", StandardIconKeys.Workspace, 20, type: "WORKSPACE"),
            Category("columns", StandardSetupCategoryCodes.ColumnsVariables, "Setup.Category.ColumnsVariables", StandardIconKeys.Columns, 30, type: "COLUMNS_VARIABLES"),
            Category("navigation", StandardSetupCategoryCodes.NavigationTree, "Setup.Category.NavigationTree", StandardIconKeys.Tree, 40, type: "NAVIGATION_TREE"),
            Category("ribbon", StandardSetupCategoryCodes.Ribbon, "Setup.Category.Ribbon", StandardIconKeys.Ribbon, 50, type: "RIBBON"),
            Category("actions", StandardSetupCategoryCodes.ActionBars, "Setup.Category.ActionBars", StandardIconKeys.Action, 60, type: "ACTION_BARS"),
            Category("dashboard", StandardSetupCategoryCodes.Dashboard, "Setup.Category.Dashboard", StandardIconKeys.Dashboard, 70, type: "DASHBOARD"),
            Category("reports", StandardSetupCategoryCodes.Reports, "Setup.Category.Reports", StandardIconKeys.Report, 80, type: "REPORTS"),
        };
        for (var index = 1; index <= 10; index++)
            result.Add(Category($"catalog-{index:00}", $"CATALOG_{index:00}", $"Setup.Catalog.{index:00}",
                StandardIconKeys.Catalog, index, "catalogs", "CATALOG"));
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

internal sealed class DemoSetupValidator : ISetupDefinitionValidator
{
    public SetupValidationResult Validate(SetupDefinitionDescriptor candidate)
    {
        var diagnostics = new List<SetupValidationDiagnostic>();
        if (candidate.Values.TryGetValue("NAME", out var name) && string.IsNullOrWhiteSpace(name?.ToString()))
            diagnostics.Add(new(SetupDiagnosticSeverity.Error, "SETUP_REQUIRED", new("Setup.Validation.Required"), FieldCode: "NAME"));
        if (candidate.EffectiveFrom is { } from && candidate.EffectiveTo is { } to && to < from)
            diagnostics.Add(new(SetupDiagnosticSeverity.Error, "SETUP_EFFECTIVE_RANGE", new("Setup.Validation.EffectiveRange")));
        if (candidate.Values.TryGetValue("WARNING", out var warning) && Equals(warning, true))
            diagnostics.Add(new(SetupDiagnosticSeverity.Warning, "SETUP_DEMO_WARNING", new("Setup.Validation.Warning")));
        return new(diagnostics.ToImmutableArray());
    }
}

internal sealed class DemoSetupProvider : ISetupDefinitionProvider
{
    private readonly List<SetupDefinitionDescriptor> definitions = CreateDefinitions();

    public IReadOnlyList<SetupDefinitionDescriptor> GetDefinitions(string categoryId, string? scopeKey = null) => definitions
        .Where(x => string.Equals(x.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase))
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
            Definition("workspace-1", "PRIMARY_WORKSPACE", "Primary workspace", "WORKSPACE", "workspaces", SetupDefinitionStatus.Published,
                new Dictionary<string, object?> { ["NAME"] = "Primary workspace", ["ICON_KEY"] = "WORKSPACE" }),
        };
        foreach (var type in new[] { "columns", "navigation", "ribbon", "actions", "dashboard", "reports" })
            list.Add(Definition(type + "-1", type.ToUpperInvariant() + "_SAMPLE", "Foundation placeholder",
                type.ToUpperInvariant() switch { "COLUMNS" => "COLUMNS_VARIABLES", "NAVIGATION" => "NAVIGATION_TREE", "ACTIONS" => "ACTION_BARS", _ => type.ToUpperInvariant() },
                type, SetupDefinitionStatus.Draft, new Dictionary<string, object?>()));
        for (var index = 1; index <= 10; index++)
        {
            var category = $"catalog-{index:00}";
            list.Add(Definition($"catalog-definition-{index:00}-a", $"ITEM_{index:00}_A", $"Catalog {index:00} item A", "CATALOG", category,
                SetupDefinitionStatus.Published, new Dictionary<string, object?> { ["NAME"] = $"Catalog {index:00} item A", ["ACTIVE"] = true }, scope: "demo-company-a"));
            list.Add(Definition($"catalog-definition-{index:00}-b", $"ITEM_{index:00}_B", $"Catalog {index:00} item B", "CATALOG", category,
                SetupDefinitionStatus.Draft, new Dictionary<string, object?> { ["NAME"] = $"Catalog {index:00} item B", ["ACTIVE"] = true }, scope: "demo-company-b"));
        }
        return list;
    }

    private static SetupDefinitionDescriptor Definition(string id, string code, string name, string type, string category,
        SetupDefinitionStatus status, IReadOnlyDictionary<string, object?> values, bool system = false, bool editable = true,
        string? scope = null) => new(id, code, name, type, status: status, isSystem: system, isEditable: editable,
            validationState: status == SetupDefinitionStatus.Invalid ? SetupValidationState.Invalid :
                status == SetupDefinitionStatus.Published ? SetupValidationState.Valid : SetupValidationState.NotValidated,
            values: values, categoryId: category, scopeKey: scope);
}
