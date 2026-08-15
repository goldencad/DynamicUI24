using DynamicUI24.Core.Workspaces;

namespace DynamicUI24.Core.Templates;

/// <summary>Provides the generic Task-1 validation and descriptor creation behavior.</summary>
public abstract class DynamicTemplateBase : IDynamicTemplate
{
    public abstract TemplateCode TemplateCode { get; }
    public abstract string ModuleName { get; }
    public virtual TemplateVersion TemplateVersion => new(0, 1);
    public virtual IReadOnlyCollection<TemplateCapability> SupportedCapabilities => [];

    public virtual TemplateValidationResult ValidateDefinition(WorkspaceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.TemplateCode == TemplateCode
            ? TemplateValidationResult.Success
            : TemplateValidationResult.Failure(
                $"Workspace template '{definition.TemplateCode}' does not match '{TemplateCode}'.");
    }

    public virtual WorkspaceDescriptor CreateWorkspace(WorkspaceDefinition definition)
    {
        var validation = ValidateDefinition(definition);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.Error, nameof(definition));
        }

        return new WorkspaceDescriptor(
            definition.WorkspaceId,
            definition.DisplayName,
            TemplateCode,
            ModuleName,
            TemplateVersion,
            SupportedCapabilities);
    }
}
