namespace DynamicUI24.Shared.Presentation;

/// <summary>Semantic navigation state kept independent from theme, culture, density, and control instances.</summary>
public sealed class NavigationTreeSessionState
{
    private readonly HashSet<string> expandedNodeIds = new(StringComparer.OrdinalIgnoreCase);

    public string? SelectedNodeId { get; private set; }
    public string? SelectedWorkspaceId { get; private set; }
    public IReadOnlySet<string> ExpandedNodeIds => expandedNodeIds;

    public void Select(string nodeId, string? workspaceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        SelectedNodeId = nodeId;
        SelectedWorkspaceId = workspaceId;
    }

    public void SetExpanded(string nodeId, bool expanded)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        if (expanded) expandedNodeIds.Add(nodeId); else expandedNodeIds.Remove(nodeId);
    }

    public bool IsExpanded(string nodeId) => expandedNodeIds.Contains(nodeId);
}
