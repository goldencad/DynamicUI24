using System.Collections.Immutable;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Setup;

public enum EditorFieldType { Text, MultilineText, Boolean, Integer, Decimal, Choice, Date, OptionalDate, IconKey, Localization }

public sealed record EditorChoice(string Value, LocalizationKey DisplayNameKey);

public sealed record EditorFieldDefinition
{
    public EditorFieldDefinition(string fieldId, string fieldCode, LocalizationKey displayNameKey,
        EditorFieldType fieldType, int displayOrder = 0, bool isRequired = false, bool isReadOnly = false,
        object? defaultValue = null, IEnumerable<EditorChoice>? choices = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldCode);
        if (!TechnicalCode.IsValid(fieldCode)) throw new ArgumentException("Field code contains unsupported characters.", nameof(fieldCode));
        var materializedChoices = (choices ?? []).ToImmutableArray();
        if (fieldType == EditorFieldType.Choice && materializedChoices.Length == 0)
            throw new ArgumentException("A choice field requires at least one choice.", nameof(choices));
        FieldId = fieldId.Trim(); FieldCode = fieldCode.Trim().ToUpperInvariant(); DisplayNameKey = displayNameKey;
        FieldType = fieldType; DisplayOrder = displayOrder; IsRequired = isRequired; IsReadOnly = isReadOnly;
        DefaultValue = defaultValue; Choices = materializedChoices;
    }
    public string FieldId { get; }
    public string FieldCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public EditorFieldType FieldType { get; }
    public int DisplayOrder { get; }
    public bool IsRequired { get; }
    public bool IsReadOnly { get; }
    public object? DefaultValue { get; }
    public ImmutableArray<EditorChoice> Choices { get; }
}

public enum SetupEditorKind { PropertyForm, Custom, Unavailable }
public sealed record SetupEditorDescriptor(string DefinitionType, SetupEditorKind Kind,
    ImmutableArray<EditorFieldDefinition> Fields, LocalizationKey? MessageKey = null);

public interface ISetupDefinitionEditorProvider
{
    string DefinitionType { get; }
    SetupEditorDescriptor CreateEditor(SetupDefinitionDescriptor definition);
}

public sealed class SetupEditorRegistry
{
    private readonly Dictionary<string, ISetupDefinitionEditorProvider> providers = new(StringComparer.OrdinalIgnoreCase);
    public bool Register(ISetupDefinitionEditorProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider.DefinitionType);
        return providers.TryAdd(provider.DefinitionType.Trim(), provider);
    }
    public SetupEditorDescriptor Resolve(SetupDefinitionDescriptor definition) =>
        providers.TryGetValue(definition.DefinitionType, out var provider)
            ? provider.CreateEditor(definition)
            : new(definition.DefinitionType, SetupEditorKind.Unavailable, [], new("Setup.Editor.Unavailable"));
}

public sealed class GenericPropertyEditorProvider : ISetupDefinitionEditorProvider
{
    private readonly ImmutableArray<EditorFieldDefinition> fields;
    public GenericPropertyEditorProvider(string definitionType, IEnumerable<EditorFieldDefinition> fields)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionType);
        DefinitionType = definitionType.Trim().ToUpperInvariant();
        this.fields = (fields ?? throw new ArgumentNullException(nameof(fields))).OrderBy(x => x.DisplayOrder).ToImmutableArray();
        if (this.fields.GroupBy(x => x.FieldId, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new ArgumentException("Field identifiers must be unique.", nameof(fields));
    }
    public string DefinitionType { get; }
    public SetupEditorDescriptor CreateEditor(SetupDefinitionDescriptor definition) =>
        new(DefinitionType, SetupEditorKind.PropertyForm, fields);
}
