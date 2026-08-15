using Avalonia.Controls;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;

namespace DynamicUI24.Avalonia;

/// <summary>Minimal visual adapter for a registry-resolved workspace descriptor.</summary>
public sealed class DynamicWorkspaceHost : ContentControl
{
    private readonly WorkspaceResolver resolver;

    public DynamicWorkspaceHost(TemplateRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        resolver = new WorkspaceResolver(registry);
    }

    public WorkspaceResolutionResult ShowWorkspace(WorkspaceDefinition definition)
    {
        var result = resolver.Resolve(definition);
        Content = result.IsSuccess
            ? CreateSuccessContent(result.Workspace!)
            : CreateFailureContent(definition, result.Error!);
        return result;
    }

    private static Control CreateSuccessContent(WorkspaceDescriptor workspace) =>
        new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = $"Workspace: {workspace.WorkspaceName}" },
                new TextBlock { Text = $"Template: {workspace.TemplateCode}" },
                new TextBlock { Text = $"Module: {workspace.TemplateModule}" },
                new TextBlock { Text = $"Version: {workspace.TemplateVersion}" },
                new TextBlock
                {
                    Text = $"Capabilities: {string.Join(", ", workspace.SupportedCapabilities)}",
                },
            },
        };

    private static Control CreateFailureContent(WorkspaceDefinition definition, string error) =>
        new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = $"Workspace: {definition.DisplayName}" },
                new TextBlock { Text = $"Template: {definition.TemplateCode}" },
                new TextBlock { Text = $"Resolution error: {error}" },
            },
        };
}
