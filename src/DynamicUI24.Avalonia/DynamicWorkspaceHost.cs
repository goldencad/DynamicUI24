using Avalonia.Controls;
using System.Globalization;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia;

/// <summary>A stateful view that can localize itself without rematerializing its active control tree.</summary>
public interface IRuntimeLocalizationAware
{
    void RefreshLocalization(CultureInfo culture);
}

/// <summary>Receives deterministic activation after a retained workspace control is assigned to the visual host.</summary>
public interface IRuntimeWorkspaceActivationAware
{
    void WorkspaceActivated();
    void WorkspaceDeactivated();
}

/// <summary>Minimal visual adapter for a registry-resolved workspace descriptor.</summary>
public sealed class DynamicWorkspaceHost : ContentControl
{
    private readonly WorkspaceResolver resolver;
    private readonly ILocalizationService localization;
    private readonly Dictionary<TemplateCode, Func<WorkspaceDefinition, Control>> viewFactories = new();
    private readonly Dictionary<string, Control> statefulViews = new(StringComparer.OrdinalIgnoreCase);
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

    public bool RegisterViewFactory(TemplateCode templateCode, Func<WorkspaceDefinition, Control> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return viewFactories.TryAdd(templateCode, factory);
    }

    public WorkspaceResolutionResult ShowWorkspace(WorkspaceDefinition definition)
    {
        var previous = Content as IRuntimeWorkspaceActivationAware;
        currentDefinition = definition ?? throw new ArgumentNullException(nameof(definition));
        var result = resolver.Resolve(definition);
        CurrentResult = result;
        var next = result.IsSuccess ? ResolveContent(definition, result.Workspace!) :
            CreateFailureContent(definition, result.Error!, localization);
        if (!ReferenceEquals(previous, next)) previous?.WorkspaceDeactivated();
        Content = next;
        (next as IRuntimeWorkspaceActivationAware)?.WorkspaceActivated();
        return result;
    }

    private Control ResolveContent(WorkspaceDefinition definition, WorkspaceDescriptor workspace)
    {
        if (statefulViews.TryGetValue(definition.WorkspaceId, out var cached)) return cached;
        var content = viewFactories.TryGetValue(definition.TemplateCode, out var factory)
            ? factory(definition) : CreateSuccessContent(workspace, localization);
        if (content is IRuntimeLocalizationAware) statefulViews[definition.WorkspaceId] = content;
        return content;
    }

    /// <summary>Clears the active workspace when navigation has no safe target.</summary>
    public void Clear()
    {
        (Content as IRuntimeWorkspaceActivationAware)?.WorkspaceDeactivated();
        currentDefinition = null;
        CurrentResult = null;
        Content = new TextBlock { Text = localization.Get(new("State.Empty")) };
    }

    private void Refresh()
    {
        if (Content is IRuntimeLocalizationAware localizationAware)
        {
            localizationAware.RefreshLocalization(localization.CurrentCulture);
            return;
        }
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
