using System.Collections.Immutable;
using DynamicUI24.Core.Editors;

namespace DynamicUI24.Core.Authoring;

public enum UiDefinitionDiagnosticSeverity { Information, Warning, Error }
public sealed record UiDefinitionDiagnostic(string Code, UiDefinitionDiagnosticSeverity Severity,
    UiElementCode? ElementCode = null, string? SafeMessage = null);
public sealed record UiDefinitionValidationResult(ImmutableArray<UiDefinitionDiagnostic> Diagnostics)
{ public bool CanPublish => Diagnostics.All(x => x.Severity != UiDefinitionDiagnosticSeverity.Error); }

public interface IUiDefinitionReferenceCatalog
{
    bool CommandExists(string commandCode);
    bool PermissionExists(string permissionCode);
    bool CapabilityExists(string capabilityCode);
    bool HelpContextExists(string helpContextCode);
}

public sealed class UiDefinitionValidator
{
    private readonly IUiDefinitionReferenceCatalog? catalog;
    public UiDefinitionValidator(IUiDefinitionReferenceCatalog? catalog = null) => this.catalog = catalog;

    public UiDefinitionValidationResult Validate(UiDefinitionDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var diagnostics = ImmutableArray.CreateBuilder<UiDefinitionDiagnostic>();
        var elements = draft.Elements;
        foreach (var duplicate in elements.GroupBy(x => x.Code).Where(x => x.Count() > 1))
            diagnostics.Add(new("UI_DUPLICATE_SEMANTIC_ID", UiDefinitionDiagnosticSeverity.Error, duplicate.Key));
        var codes = elements.Select(x => x.Code).ToHashSet();
        foreach (var element in elements)
        {
            if (element.ParentCode is { } parent && !codes.Contains(parent))
                diagnostics.Add(new("UI_MISSING_PARENT", UiDefinitionDiagnosticSeverity.Error, element.Code));
            if (element.Layout.MinimumWidth is { } min && element.Layout.MaximumWidth is { } max && min > max)
                diagnostics.Add(new("UI_INVALID_LAYOUT_RANGE", UiDefinitionDiagnosticSeverity.Error, element.Code));
            ValidateEditor(element, diagnostics);
            ValidateReferences(element, diagnostics);
        }
        return new(diagnostics.ToImmutable());
    }

    private static void ValidateEditor(UiElementDefinition element, ImmutableArray<UiDefinitionDiagnostic>.Builder diagnostics)
    {
        if (element.Kind == UiElementKind.Field && element.Editor is null)
            diagnostics.Add(new("UI_FIELD_EDITOR_MISSING", UiDefinitionDiagnosticSeverity.Error, element.Code));
        if (element.Editor is { ExplicitKind: EditorKind.Password, ValueType: not EditorValueType.Secret and not EditorValueType.String })
            diagnostics.Add(new("UI_EDITOR_VALUE_TYPE_INCOMPATIBLE", UiDefinitionDiagnosticSeverity.Error, element.Code));
    }

    private void ValidateReferences(UiElementDefinition element, ImmutableArray<UiDefinitionDiagnostic>.Builder diagnostics)
    {
        if (catalog is null) return;
        if (element.Kind == UiElementKind.Command && element.SemanticReference is { } command && !catalog.CommandExists(command))
            diagnostics.Add(new("UI_COMMAND_REFERENCE_MISSING", UiDefinitionDiagnosticSeverity.Error, element.Code));
        if (element.Authorization?.Permission is { } permission && !catalog.PermissionExists(permission.Value))
            diagnostics.Add(new("UI_PERMISSION_REFERENCE_UNKNOWN", UiDefinitionDiagnosticSeverity.Error, element.Code));
        if (element.Authorization?.Capability is { } capability && !catalog.CapabilityExists(capability.Value))
            diagnostics.Add(new("UI_CAPABILITY_REFERENCE_UNKNOWN", UiDefinitionDiagnosticSeverity.Error, element.Code));
        if (element.HelpContextCode is { } help && !catalog.HelpContextExists(help.Value))
            diagnostics.Add(new("UI_HELP_REFERENCE_UNKNOWN", UiDefinitionDiagnosticSeverity.Warning, element.Code));
    }
}
