using DynamicUI24.Core.Templates;

namespace DynamicUI24.Core.Workspaces;

public sealed record WorkspaceResolutionResult(
    bool IsSuccess,
    WorkspaceDescriptor? Workspace,
    string? Error);

/// <summary>Resolves workspace metadata through the registry without template-specific dispatch.</summary>
public sealed class WorkspaceResolver(TemplateRegistry registry)
{
    public WorkspaceResolutionResult Resolve(WorkspaceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var resolution = registry.Resolve(definition.TemplateCode);
        if (!resolution.IsSuccess)
        {
            return new(false, null, resolution.Message);
        }

        var validation = resolution.Template!.ValidateDefinition(definition);
        return validation.IsValid
            ? new(true, resolution.Template.CreateWorkspace(definition), null)
            : new(false, null, validation.Error);
    }
}
