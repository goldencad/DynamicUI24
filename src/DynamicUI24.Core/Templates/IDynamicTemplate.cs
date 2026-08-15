using DynamicUI24.Core.Workspaces;

namespace DynamicUI24.Core.Templates;

/// <summary>Domain-neutral contract implemented independently by each template module.</summary>
public interface IDynamicTemplate
{
    TemplateCode TemplateCode { get; }
    string ModuleName { get; }
    TemplateVersion TemplateVersion { get; }
    IReadOnlyCollection<TemplateCapability> SupportedCapabilities { get; }
    TemplateValidationResult ValidateDefinition(WorkspaceDefinition definition);
    WorkspaceDescriptor CreateWorkspace(WorkspaceDefinition definition);
}

/// <summary>Immutable discovery metadata projected by the registry.</summary>
public sealed record TemplateDescriptor(
    TemplateCode TemplateCode,
    string ModuleName,
    TemplateVersion TemplateVersion,
    IReadOnlyCollection<TemplateCapability> SupportedCapabilities);

public sealed record TemplateValidationResult(bool IsValid, string? Error)
{
    public static TemplateValidationResult Success { get; } = new(true, null);

    public static TemplateValidationResult Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new(false, error);
    }
}

public sealed record WorkspaceDescriptor(
    string WorkspaceId,
    string WorkspaceName,
    TemplateCode TemplateCode,
    string TemplateModule,
    TemplateVersion TemplateVersion,
    IReadOnlyCollection<TemplateCapability> SupportedCapabilities);
