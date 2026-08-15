using DynamicUI24.Core.Setup;
using DynamicUI24.Core.Templates;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Template.Setup;

/// <summary>Registers Setup's specialized metadata designers without introducing central type branching.</summary>
public static class SpecializedSetupEditorRegistration
{
    public static void Register(SetupEditorRegistry editors, TemplateRegistry templates,
        Func<IReadOnlyList<VariableDefinition>> variables)
    {
        ArgumentNullException.ThrowIfNull(editors);
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(variables);
        editors.Register(new MasterCatalogEditorProvider());
        editors.Register(new WorkspaceEditorProvider(templates));
        editors.Register(new ColumnEditorProvider());
        editors.Register(new VariableEditorProvider());
        editors.Register(new FormulaEditorProvider(variables));
    }
}

internal static class SetupEditorFields
{
    public static EditorFieldDefinition Text(string code, int order, bool required = false, bool readOnly = false) =>
        new(code.ToLowerInvariant(), code, new($"Setup.Field.{Key(code)}"), EditorFieldType.Text, order, required, readOnly);
    public static EditorFieldDefinition Multiline(string code, int order, bool required = false, bool readOnly = false) =>
        new(code.ToLowerInvariant(), code, new($"Setup.Field.{Key(code)}"), EditorFieldType.MultilineText, order, required, readOnly);
    public static EditorFieldDefinition Integer(string code, int order, bool required = false) =>
        new(code.ToLowerInvariant(), code, new($"Setup.Field.{Key(code)}"), EditorFieldType.Integer, order, required);
    public static EditorFieldDefinition Decimal(string code, int order) =>
        new(code.ToLowerInvariant(), code, new($"Setup.Field.{Key(code)}"), EditorFieldType.Decimal, order);
    public static EditorFieldDefinition Boolean(string code, int order, bool readOnly = false) =>
        new(code.ToLowerInvariant(), code, new($"Setup.Field.{Key(code)}"), EditorFieldType.Boolean, order, isReadOnly: readOnly);
    public static EditorFieldDefinition Choice(string code, int order, IEnumerable<string> choices) =>
        new(code.ToLowerInvariant(), code, new($"Setup.Field.{Key(code)}"), EditorFieldType.Choice, order, true,
            choices: choices.Select(x => new EditorChoice(x, new($"Setup.Choice.{Key(x)}"))));
    public static string Key(string value) => string.Concat(value.Split('_').Select(x => char.ToUpperInvariant(x[0]) + x[1..].ToLowerInvariant()));
    public static string EnumCode(string value) => value == nameof(ColumnDataType.DateTime) ? "DATETIME" :
        string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + c : c.ToString())).ToUpperInvariant();
}

public sealed class MasterCatalogEditorProvider : ISetupDefinitionEditorProvider
{
    public string DefinitionType => SpecializedSetupDefinitionTypes.MasterCatalog;
    public SetupEditorDescriptor CreateEditor(SetupDefinitionDescriptor definition) => new(DefinitionType, SetupEditorKind.Custom,
        [SetupEditorFields.Text(SpecializedSetupFieldCodes.DisplayNameKey, 0, true),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.DescriptionKey, 10),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.ParentCatalogId, 20),
         SetupEditorFields.Integer(SpecializedSetupFieldCodes.DisplayOrder, 30, true),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.IconKey, 40, true),
         SetupEditorFields.Boolean(SpecializedSetupFieldCodes.IsActive, 50),
         SetupEditorFields.Boolean(SpecializedSetupFieldCodes.IsEditable, 60),
         SetupEditorFields.Choice(SpecializedSetupFieldCodes.CompanyScope, 70, Enum.GetNames<SetupCompanyScope>().Select(x => x.ToUpperInvariant())),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.PermissionRequirement, 80)]);
}

public sealed class WorkspaceEditorProvider(TemplateRegistry templates) : ISetupDefinitionEditorProvider
{
    public string DefinitionType => SpecializedSetupDefinitionTypes.Workspace;
    public SetupEditorDescriptor CreateEditor(SetupDefinitionDescriptor definition) => new(DefinitionType, SetupEditorKind.Custom,
        [SetupEditorFields.Text(SpecializedSetupFieldCodes.DisplayNameKey, 0, true),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.DescriptionKey, 10),
         new("template-code", SpecializedSetupFieldCodes.TemplateCode, new("Setup.Field.TemplateCode"), EditorFieldType.Choice, 20, true,
             choices: templates.GetRegisteredTemplates().Select(x => new EditorChoice(x.TemplateCode.Value, new($"Template.{x.TemplateCode.Value}")))),
         SetupEditorFields.Integer(SpecializedSetupFieldCodes.DisplayOrder, 30, true),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.IconKey, 40, true),
         SetupEditorFields.Boolean(SpecializedSetupFieldCodes.IsActive, 50),
         SetupEditorFields.Choice(SpecializedSetupFieldCodes.CompanyScope, 60, Enum.GetNames<SetupCompanyScope>().Select(x => x.ToUpperInvariant())),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.PermissionRequirement, 70)]);
}

public sealed class ColumnEditorProvider : ISetupDefinitionEditorProvider
{
    public string DefinitionType => SpecializedSetupDefinitionTypes.Column;
    public SetupEditorDescriptor CreateEditor(SetupDefinitionDescriptor definition)
    {
        var immutableCode = definition.IsPublished;
        return new(DefinitionType, SetupEditorKind.Custom,
        [SetupEditorFields.Text(SpecializedSetupFieldCodes.VariableCode, 0, true, immutableCode),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.DisplayNameKey, 10, true),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.DescriptionKey, 20),
         SetupEditorFields.Choice(SpecializedSetupFieldCodes.DataType, 30, Enum.GetNames<ColumnDataType>().Select(SetupEditorFields.EnumCode)),
         SetupEditorFields.Choice(SpecializedSetupFieldCodes.EditorKind, 40, Enum.GetNames<ColumnEditorKind>().Select(SetupEditorFields.EnumCode)),
         SetupEditorFields.Choice(SpecializedSetupFieldCodes.ColumnMode, 50, Enum.GetNames<ColumnMode>().Select(x => x.ToUpperInvariant())),
         SetupEditorFields.Integer(SpecializedSetupFieldCodes.DisplayOrder, 60, true),
         SetupEditorFields.Decimal(SpecializedSetupFieldCodes.Width, 70),
         SetupEditorFields.Decimal(SpecializedSetupFieldCodes.MinWidth, 80),
         SetupEditorFields.Decimal(SpecializedSetupFieldCodes.MaxWidth, 90),
         SetupEditorFields.Boolean(SpecializedSetupFieldCodes.IsVisible, 100),
         SetupEditorFields.Boolean(SpecializedSetupFieldCodes.IsRequired, 110),
         SetupEditorFields.Boolean("IS_EDITABLE", 120),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.PermissionRequirement, 130),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.Format, 140),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.DefaultValue, 150),
         SetupEditorFields.Multiline(SpecializedSetupFieldCodes.ValidationDefinition, 160),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.FormulaDefinitionId, 170)]);
    }
}

public sealed class VariableEditorProvider : ISetupDefinitionEditorProvider
{
    public string DefinitionType => SpecializedSetupDefinitionTypes.Variable;
    public SetupEditorDescriptor CreateEditor(SetupDefinitionDescriptor definition) => new(DefinitionType, SetupEditorKind.Custom,
        [SetupEditorFields.Text(SpecializedSetupFieldCodes.VariableCode, 0, true, definition.IsPublished),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.DisplayNameKey, 10, true),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.DescriptionKey, 20),
         SetupEditorFields.Choice(SpecializedSetupFieldCodes.DataType, 30, Enum.GetNames<ColumnDataType>().Select(SetupEditorFields.EnumCode)),
         SetupEditorFields.Choice(SpecializedSetupFieldCodes.VariableScope, 40, Enum.GetNames<VariableScope>().Select(x => x.ToUpperInvariant())),
         SetupEditorFields.Boolean("IS_SYSTEM", 50, definition.IsSystem),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.PermissionRequirement, 60)]);
}

public sealed class FormulaEditorProvider(Func<IReadOnlyList<VariableDefinition>> variables) : ISetupDefinitionEditorProvider
{
    public string DefinitionType => SpecializedSetupDefinitionTypes.Formula;
    public SetupEditorDescriptor CreateEditor(SetupDefinitionDescriptor definition)
    {
        var choices = variables().OrderBy(x => x.VariableCode.Value, StringComparer.Ordinal)
            .Select(x => new EditorChoice(x.VariableCode.Value, new(x.DisplayNameKey))).ToArray();
        return new(DefinitionType, SetupEditorKind.Custom,
        [SetupEditorFields.Text(SpecializedSetupFieldCodes.FormulaCode, 0, true, definition.IsPublished),
         SetupEditorFields.Text(SpecializedSetupFieldCodes.DisplayNameKey, 10, true),
         new("result-variable-code", SpecializedSetupFieldCodes.ResultVariableCode, new("Setup.Field.ResultVariableCode"), EditorFieldType.Choice, 20, true, choices: choices),
         SetupEditorFields.Multiline(SpecializedSetupFieldCodes.ExpressionText, 30, true, definition.IsPublished),
         new("referenced-variable-codes", SpecializedSetupFieldCodes.ReferencedVariableCodes, new("Setup.Field.ReferencedVariableCodes"), EditorFieldType.MultiChoice, 40, choices: choices)]);
    }
}
