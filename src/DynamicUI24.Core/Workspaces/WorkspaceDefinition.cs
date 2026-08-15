using DynamicUI24.Core.Templates;

namespace DynamicUI24.Core.Workspaces;

/// <summary>The minimum metadata needed to select and create a workspace.</summary>
public sealed record WorkspaceDefinition
{
    public WorkspaceDefinition(string workspaceId, string displayName, TemplateCode templateCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(templateCode);

        WorkspaceId = workspaceId.Trim();
        DisplayName = displayName.Trim();
        TemplateCode = templateCode;
    }

    public string WorkspaceId { get; }
    public string DisplayName { get; }
    public TemplateCode TemplateCode { get; }
}
