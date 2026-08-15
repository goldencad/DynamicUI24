using DynamicUI24.Core.Setup;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Template.Setup;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class SpecializedSetupDesignerTests
{
    [Fact]
    public void VariableCodeIsNormalizedAndHasDeterministicEquality()
    {
        Assert.Equal(new VariableCode(" net_amount "), new VariableCode("NET_AMOUNT"));
        Assert.Throws<ArgumentException>(() => new VariableCode("   "));
        Assert.Throws<ArgumentException>(() => new VariableCode("BAD CODE"));
    }

    [Fact]
    public void CatalogHierarchyAllowsDepthAndRejectsOrphansAndCycles()
    {
        MasterCatalogDefinition Item(string id, string? parent) => new(id, id.ToUpperInvariant(), id, null, parent, 1,
            "CATALOG", true, false, true, SetupCompanyScope.Global, null, 1, SetupDefinitionStatus.Draft);
        Assert.Empty(MasterCatalogHierarchyValidator.Validate([Item("a", null), Item("b", "a"), Item("c", "b")]));
        Assert.Contains(MasterCatalogHierarchyValidator.Validate([Item("a", "missing")]), x => x.Code == "CATALOG_ORPHAN");
        Assert.Contains(MasterCatalogHierarchyValidator.Validate([Item("a", "b"), Item("b", "a")]), x => x.Code == "CATALOG_CYCLE");
    }

    [Fact]
    public void CatalogHierarchyHasNoFixedNineItemLimitAndOrdersDeterministically()
    {
        var items = Enumerable.Range(1, 12).Select(x => new MasterCatalogDefinition($"id{x}", $"CODE_{x}", $"Name {x}", null,
            null, 13 - x, "CATALOG", true, false, true, SetupCompanyScope.Global, null, 1, SetupDefinitionStatus.Draft));
        Assert.Empty(MasterCatalogHierarchyValidator.Validate(items));
        Assert.Equal(12, items.Count());
    }

    [Fact]
    public void CatalogLifecycleSupportsCreateEditAndClone()
    {
        var lifecycle = new SetupDefinitionLifecycle(new MemoryProvider(), new PassValidator());
        var created = lifecycle.CreateDraft("catalogs", SpecializedSetupDefinitionTypes.MasterCatalog, "NEW_CATALOG", "New catalog");
        lifecycle.Buffer!.SetValue(SpecializedSetupFieldCodes.DisplayOrder, 20);
        Assert.True(lifecycle.Buffer.IsDirty);
        var saved = lifecycle.SaveDraft();
        Assert.Equal(created.DefinitionId, saved.DefinitionId);
        var clone = lifecycle.Clone("catalog-clone", "NEW_CATALOG_COPY");
        Assert.Equal(2, clone.Version);
        Assert.Equal(SetupDefinitionStatus.Draft, clone.Status);
    }

    [Fact]
    public void WorkspaceValidationUsesRegistryForKnownUnknownAndCalendarTemplates()
    {
        var registry = Registry(); var provider = new MemoryProvider(); var validator = new SpecializedSetupValidator(provider, registry);
        Assert.True(validator.Validate(Workspace("CALENDAR")).IsValid);
        Assert.False(validator.Validate(Workspace("UNKNOWN")).IsValid);
    }

    [Fact]
    public void WorkspaceEditorChoicesComeDirectlyFromTemplateRegistry()
    {
        var registry = Registry();
        var descriptor = new WorkspaceEditorProvider(registry).CreateEditor(Workspace("CALENDAR"));
        var choices = descriptor.Fields.Single(x => x.FieldCode == SpecializedSetupFieldCodes.TemplateCode).Choices;
        Assert.Contains(choices, x => x.Value == "CALENDAR");
        Assert.Contains(choices, x => x.Value == "SETUP");
    }

    [Fact]
    public void ColumnValidationCoversModeGeometryAndUnknownEditor()
    {
        var validator = new SpecializedSetupValidator(new MemoryProvider(), Registry());
        Assert.True(validator.Validate(Column("INPUT", "NUMBER", 120, 80, 200, true)).IsValid);
        var invalid = validator.Validate(Column("FORMULA", "UNKNOWN", 40, 80, 200, true));
        Assert.Contains(invalid.Diagnostics, x => x.Code == "COLUMN_EDITOR_KIND_INVALID");
        Assert.Contains(invalid.Diagnostics, x => x.Code == "COLUMN_GEOMETRY_INVALID");
        Assert.Contains(invalid.Diagnostics, x => x.Code == "COLUMN_READ_ONLY_REQUIRED");
    }

    [Fact]
    public void VariableValidationRejectsDuplicateCodeAndUnknownScope()
    {
        var existing = Variable("QTY", "ROW"); var provider = new MemoryProvider(existing);
        var candidate = Variable("qty", "UNKNOWN") with { DefinitionId = "other" };
        var result = new SpecializedSetupValidator(provider, Registry()).Validate(candidate);
        Assert.Contains(result.Diagnostics, x => x.Code == "VARIABLE_DUPLICATE_CODE");
        Assert.Contains(result.Diagnostics, x => x.Code == "VARIABLE_SCOPE_INVALID");
    }

    [Fact]
    public void FormulaValidationAcceptsKnownReferencesAndRejectsUnknownSelfAndDuplicates()
    {
        var provider = new MemoryProvider(Variable("QUANTITY", "ROW"), Variable("TOTAL", "ROW"));
        var validator = new SpecializedSetupValidator(provider, Registry());
        Assert.True(validator.Validate(Formula("TOTAL", "QUANTITY")).IsValid);
        var invalid = Formula("TOTAL", "TOTAL,TOTAL,UNKNOWN");
        var result = validator.Validate(invalid);
        Assert.Contains(result.Diagnostics, x => x.Code == "FORMULA_SELF_REFERENCE");
        Assert.Contains(result.Diagnostics, x => x.Code == "FORMULA_DUPLICATE_REFERENCE");
        Assert.Contains(result.Diagnostics, x => x.Code == "FORMULA_REFERENCE_UNKNOWN");
        Assert.Contains(validator.Validate(Formula("MISSING_RESULT", "QUANTITY")).Diagnostics,
            x => x.Code == "FORMULA_RESULT_UNKNOWN");
    }

    [Fact]
    public void FormulaMetadataRejectsExecutableSyntaxWithoutExecutingAnything()
    {
        var provider = new MemoryProvider(Variable("TOTAL", "ROW"), Variable("QUANTITY", "ROW"));
        var formula = Formula("TOTAL", "QUANTITY") with
        { Values = Formula("TOTAL", "QUANTITY").Values.SetItem(SpecializedSetupFieldCodes.ExpressionText, "SELECT * FROM values") };
        Assert.Contains(new SpecializedSetupValidator(provider, Registry()).Validate(formula).Diagnostics,
            x => x.Code == "FORMULA_EXECUTABLE_SYNTAX_FORBIDDEN");
    }

    [Fact]
    public void AllFiveEditorsResolveAndMissingEditorIsSafelyUnavailable()
    {
        var registry = new SetupEditorRegistry(); var templates = Registry();
        SpecializedSetupEditorRegistration.Register(registry, templates, () =>
            [new("v", new("VALUE"), "Variable.VALUE", null, ColumnDataType.Decimal, VariableScope.Row, 1, SetupDefinitionStatus.Draft, false)]);
        foreach (var type in new[] { "MASTER_CATALOG", "WORKSPACE", "COLUMN", "VARIABLE", "FORMULA" })
            Assert.NotEqual(SetupEditorKind.Unavailable, registry.Resolve(Basic(type, type.ToLowerInvariant())).Kind);
        Assert.Equal(SetupEditorKind.Unavailable, registry.Resolve(Basic("MISSING", "missing")).Kind);
    }

    [Fact]
    public void PublishedDefinitionRequiresCloneBeforeMutation()
    {
        var published = Basic("VARIABLE", "published") with { Status = SetupDefinitionStatus.Published };
        var lifecycle = new SetupDefinitionLifecycle(new MemoryProvider(published), new PassValidator());
        lifecycle.Select(published);
        Assert.Throws<InvalidOperationException>(() => lifecycle.SaveDraft());
        var clone = lifecycle.Clone("draft-v2", "PUBLISHED_V2");
        Assert.Equal(SetupDefinitionStatus.Draft, clone.Status);
        Assert.Equal(2, clone.Version);
    }

    private static TemplateRegistry Registry()
    {
        var registry = new TemplateRegistry(); registry.Register(new TestTemplate("SETUP")); registry.Register(new TestTemplate("CALENDAR")); return registry;
    }
    private static SetupDefinitionDescriptor Workspace(string template) => Basic("WORKSPACE", "workspace") with
    { CategoryId = "workspaces", Values = Basic("WORKSPACE", "workspace").Values.SetItem("TEMPLATE_CODE", template) };
    private static SetupDefinitionDescriptor Variable(string code, string scope) => Basic("VARIABLE", code.ToLowerInvariant()) with
    { CategoryId = "variables", Values = Basic("VARIABLE", code.ToLowerInvariant()).Values
        .SetItem("VARIABLE_CODE", code).SetItem("DATA_TYPE", "DECIMAL").SetItem("VARIABLE_SCOPE", scope) };
    private static SetupDefinitionDescriptor Formula(string result, string references) => Basic("FORMULA", "formula") with
    { CategoryId = "formulas", Values = Basic("FORMULA", "formula").Values.SetItem("FORMULA_CODE", "FORMULA")
        .SetItem("RESULT_VARIABLE_CODE", result).SetItem("REFERENCED_VARIABLE_CODES", references).SetItem("EXPRESSION_TEXT", "QUANTITY") };
    private static SetupDefinitionDescriptor Column(string mode, string editor, decimal width, decimal min, decimal max, bool editable) => Basic("COLUMN", "column") with
    { CategoryId = "columns", Values = Basic("COLUMN", "column").Values.SetItem("VARIABLE_CODE", "VALUE").SetItem("DATA_TYPE", "DECIMAL")
        .SetItem("EDITOR_KIND", editor).SetItem("COLUMN_MODE", mode).SetItem("WIDTH", width).SetItem("MIN_WIDTH", min).SetItem("MAX_WIDTH", max).SetItem("IS_EDITABLE", editable) };
    private static SetupDefinitionDescriptor Basic(string type, string id) => new(id, id.Replace('-', '_').ToUpperInvariant(), id, type, categoryId: "category");

    private sealed class TestTemplate(string code) : DynamicTemplateBase
    { public override TemplateCode TemplateCode { get; } = new(code); public override string ModuleName => "Test"; }
    private sealed class PassValidator : ISetupDefinitionValidator
    { public SetupValidationResult Validate(SetupDefinitionDescriptor candidate) => SetupValidationResult.Success; }
    private sealed class MemoryProvider(params SetupDefinitionDescriptor[] seed) : ISpecializedSetupDefinitionProvider
    {
        private readonly List<SetupDefinitionDescriptor> items = [.. seed];
        public IReadOnlyList<SetupDefinitionDescriptor> GetDefinitions(string categoryId, string? scopeKey = null) => items.Where(x => x.CategoryId == categoryId).ToArray();
        public IReadOnlyList<SetupDefinitionDescriptor> GetDefinitionsByType(string definitionType, string? scopeKey = null) => items.Where(x => x.DefinitionType == definitionType).ToArray();
        public SetupDefinitionDescriptor SaveDraft(SetupDefinitionDescriptor candidate) => candidate;
        public SetupDefinitionDescriptor Publish(SetupDefinitionDescriptor candidate) => candidate with { Status = SetupDefinitionStatus.Published };
        public SetupDefinitionDescriptor Retire(SetupDefinitionDescriptor definition) => definition with { Status = SetupDefinitionStatus.Retired };
    }
}
