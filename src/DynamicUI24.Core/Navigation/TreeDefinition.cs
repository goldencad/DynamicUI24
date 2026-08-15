using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Navigation;

/// <summary>Immutable, application-neutral metadata for a navigable tree.</summary>
public sealed record TreeDefinition
{
    public TreeDefinition(string treeId, string code, int version, IEnumerable<TreeNodeDefinition> nodes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(treeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        ArgumentNullException.ThrowIfNull(nodes);
        TreeId = treeId.Trim();
        Code = code.Trim().ToUpperInvariant();
        Version = version;
        Nodes = nodes.ToImmutableArray();
        TreeDefinitionValidator.ThrowIfInvalid(this);
    }

    public string TreeId { get; }
    public string Code { get; }
    public int Version { get; }
    /// <summary>Flat representation makes arbitrary-depth parent relationships explicit and serializable.</summary>
    public ImmutableArray<TreeNodeDefinition> Nodes { get; }
}

public sealed record TreeNodeDefinition
{
    public TreeNodeDefinition(string nodeId, string nodeCode, LocalizationKey displayNameKey,
        string? parentNodeId = null, IconKey? iconKey = null, int displayOrder = 0,
        string? workspaceId = null, bool isVisible = true, PresentationRequirement? permissionRequirement = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeCode);
        NodeId = nodeId.Trim();
        NodeCode = nodeCode.Trim().ToUpperInvariant();
        ParentNodeId = string.IsNullOrWhiteSpace(parentNodeId) ? null : parentNodeId.Trim();
        IconKey = iconKey;
        DisplayNameKey = displayNameKey;
        DisplayOrder = displayOrder;
        WorkspaceId = string.IsNullOrWhiteSpace(workspaceId) ? null : workspaceId.Trim();
        IsVisible = isVisible;
        PermissionRequirement = permissionRequirement;
    }

    public string NodeId { get; }
    public string? ParentNodeId { get; }
    public string NodeCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public IconKey? IconKey { get; }
    public int DisplayOrder { get; }
    public string? WorkspaceId { get; }
    public bool IsVisible { get; }
    public PresentationRequirement? PermissionRequirement { get; }
}

public sealed record TreeDiagnostic(string Code, string Message);
public sealed record TreeValidationResult(bool IsValid, ImmutableArray<TreeDiagnostic> Diagnostics);

public static class TreeDefinitionValidator
{
    public static TreeValidationResult Validate(TreeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var diagnostics = ImmutableArray.CreateBuilder<TreeDiagnostic>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in definition.Nodes)
            if (!ids.Add(node.NodeId)) diagnostics.Add(new("TREE_DUPLICATE_NODE_ID", $"Duplicate NodeId '{node.NodeId}'."));

        var byId = definition.Nodes.GroupBy(x => x.NodeId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var node in definition.Nodes)
        {
            if (node.ParentNodeId is { } parent)
            {
                if (parent.Equals(node.NodeId, StringComparison.OrdinalIgnoreCase))
                    diagnostics.Add(new("TREE_SELF_PARENT", $"Node '{node.NodeId}' cannot parent itself."));
                else if (!byId.ContainsKey(parent))
                    diagnostics.Add(new("TREE_INVALID_PARENT", $"Parent '{parent}' for node '{node.NodeId}' does not exist."));
            }
        }
        foreach (var node in definition.Nodes)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = node;
            while (true)
            {
                if (!visited.Add(current.NodeId))
                {
                    diagnostics.Add(new("TREE_CYCLE", $"A parent cycle includes node '{node.NodeId}'."));
                    break;
                }

                if (current.ParentNodeId is not { } parent || !byId.TryGetValue(parent, out var next))
                {
                    break;
                }

                current = next;
            }
        }
        return new(!diagnostics.Any(), diagnostics.ToImmutable());
    }

    public static void ThrowIfInvalid(TreeDefinition definition)
    {
        var result = Validate(definition);
        if (!result.IsValid) throw new ArgumentException(string.Join(" ", result.Diagnostics.Select(x => x.Message)), nameof(definition));
    }
}
