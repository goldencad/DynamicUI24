using Avalonia.Controls;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia;

/// <summary>Minimal visual adapter for a registry-resolved workspace descriptor.</summary>
public sealed class DynamicWorkspaceHost : ContentControl
{
    private readonly WorkspaceResolver resolver;
    private readonly ILocalizationService localization;
    private WorkspaceDefinition? currentDefinition;

    public DynamicWorkspaceHost(TemplateRegistry registry, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(registry);
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        resolver = new WorkspaceResolver(registry);
        localization.CultureChanged += (_, _) => Refresh();
    }

    public WorkspaceDefinition? CurrentDefinition => currentDefinition;
    public WorkspaceResolutionResult? CurrentResult { get; private set; }

    public WorkspaceResolutionResult ShowWorkspace(WorkspaceDefinition definition)
    {
        currentDefinition = definition ?? throw new ArgumentNullException(nameof(definition));
        var result = resolver.Resolve(definition);
        CurrentResult = result;
        Content = result.IsSuccess
            ? CreateSuccessContent(result.Workspace!, localization)
            : CreateFailureContent(definition, result.Error!, localization);
        return result;
    }

    /// <summary>Clears the active workspace when navigation has no safe target.</summary>
    public void Clear()
    {
        currentDefinition = null;
        CurrentResult = null;
        Content = new TextBlock { Text = localization.Get(new("State.Empty")) };
    }

    private void Refresh()
    {
        if (currentDefinition is not null)
        {
            ShowWorkspace(currentDefinition);
        }
    }

    private static Control CreateSuccessContent(WorkspaceDescriptor workspace, ILocalizationService localization) =>
        new StackPanel
        {
            Spacing = 6,
            Children =
            {
                Line(localization, "Shell.Workspace", workspace.WorkspaceName),
                Line(localization, "Shell.Template", workspace.TemplateCode.ToString()),
                Line(localization, "Shell.Module", workspace.TemplateModule),
                Line(localization, "Shell.Version", workspace.TemplateVersion.ToString()),
                new TextBlock
                {
                    Text = $"{localization.Get(new("Shell.Capabilities"))}: " +
                           string.Join(", ", workspace.SupportedCapabilities),
                },
            },
        };

    private static Control CreateFailureContent(WorkspaceDefinition definition, string error, ILocalizationService localization) =>
        new StackPanel
        {
            Spacing = 6,
            Children =
            {
                Line(localization, "Shell.Workspace", definition.DisplayName),
                Line(localization, "Shell.Template", definition.TemplateCode.ToString()),
                Line(localization, "Shell.ResolutionError", error),
            },
        };

    private static TextBlock Line(ILocalizationService localization, string key, string value) =>
        new() { Text = $"{localization.Get(new(key))}: {value}" };
}
