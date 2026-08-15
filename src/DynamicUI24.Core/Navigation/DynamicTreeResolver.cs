using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Workspaces;

namespace DynamicUI24.Core.Navigation;

public sealed record TreeResolutionContext(CompanyDescriptor Company, EffectiveAuthorizationContext? Authorization);
public sealed record ResolvedTreeNode(TreeNodeDefinition Definition, AuthorizationPresentationState State,
    bool HasValidWorkspaceTarget, ImmutableArray<ResolvedTreeNode> Children)
{
    public bool IsNavigable => HasValidWorkspaceTarget && State is AuthorizationPresentationState.VisibleEnabled or AuthorizationPresentationState.VisibleReadOnly;
}
public sealed record ResolvedTree(TreeDefinition Definition, ImmutableArray<ResolvedTreeNode> RootNodes,
    ImmutableArray<TreeDiagnostic> Diagnostics);

/// <summary>Converts pure metadata into a visible presentation tree without application dispatch.</summary>
public sealed class DynamicTreeResolver
{
    public ResolvedTree Resolve(TreeDefinition definition, TreeResolutionContext context,
        IEnumerable<WorkspaceDefinition> knownWorkspaces)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(knownWorkspaces);
        var validation = TreeDefinitionValidator.Validate(definition);
        if (!validation.IsValid) return new(definition, [], validation.Diagnostics);
        var workspaces = knownWorkspaces.Select(x => x.WorkspaceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var children = definition.Nodes.ToLookup(x => x.ParentNodeId, StringComparer.OrdinalIgnoreCase);
        var diagnostics = ImmutableArray.CreateBuilder<TreeDiagnostic>();
        ImmutableArray<ResolvedTreeNode> Build(string? parent) => children[parent]
            .OrderBy(n => n.DisplayOrder).ThenBy(n => n.NodeCode, StringComparer.Ordinal).ThenBy(n => n.NodeId, StringComparer.Ordinal)
            .Where(n => n.IsVisible).Select(n =>
            {
                var state = n.PermissionRequirement is null ? AuthorizationPresentationState.VisibleEnabled : AuthorizationPresentationResolver.Resolve(n.PermissionRequirement, context.Authorization);
                var validWorkspace = n.WorkspaceId is null || workspaces.Contains(n.WorkspaceId);
                if (!validWorkspace && n.WorkspaceId is not null) diagnostics.Add(new("TREE_WORKSPACE_UNKNOWN", $"Node '{n.NodeId}' targets unknown workspace '{n.WorkspaceId}'."));
                return new ResolvedTreeNode(n, state, validWorkspace, Build(n.NodeId));
            }).Where(n => n.State != AuthorizationPresentationState.Hidden).ToImmutableArray();
        return new(definition, Build(null), validation.Diagnostics.AddRange(diagnostics));
    }
}
