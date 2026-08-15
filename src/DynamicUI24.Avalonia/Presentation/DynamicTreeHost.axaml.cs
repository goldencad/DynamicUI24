using System.Collections.Immutable;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Theme-safe Avalonia adapter for the reusable resolved tree.</summary>
public sealed partial class DynamicTreeHost : UserControl
{
    private readonly ILocalizationService localization;
    private readonly IIconRegistry icons;
    private readonly TreeOverflowController overflow;
    private readonly HashSet<string> expandedNodeIds = new(StringComparer.OrdinalIgnoreCase);
    private ImmutableArray<ResolvedTreeNode> resolved = [];
    private bool applyingSelection;

    public DynamicTreeHost()
        : this(new DictionaryLocalizationService(), new SemanticIconRegistry(), null)
    {
    }

    public DynamicTreeHost(ILocalizationService localization, IIconRegistry icons, TreeOverflowOptions? overflowOptions = null)
    {
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.icons = icons ?? throw new ArgumentNullException(nameof(icons));
        overflow = new(overflowOptions);
        InitializeComponent();
        localization.CultureChanged += (_, _) => Render();
        TitleText.Text = localization.Get(new("Tree.Navigation"));
    }

    public string? SelectedNodeId { get; private set; }
    public string? SelectedWorkspaceId { get; private set; }
    public TreeOverflowOptions OverflowOptions => overflow.Options;
    public bool ShowTitle { get => TitleText.IsVisible; set => TitleText.IsVisible = value; }
    public event EventHandler<TreeNodeSelectedEventArgs>? NodeSelected;

    public void Show(ResolvedTree tree)
    {
        resolved = tree.RootNodes;
        Render();
    }

    public void SelectWorkspace(string? workspaceId)
    {
        if (workspaceId is null) return;
        var nodeId = FlattenResolved(resolved).FirstOrDefault(x =>
            string.Equals(x.Definition.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))?.Definition.NodeId;
        if (nodeId is not null) EnsureNodeVisible(nodeId);
        Render();
        var item = Flatten(Tree.ItemsSource as IEnumerable<ITreeItemView>).OfType<TreeNodeView>().FirstOrDefault(x =>
            string.Equals(x.Definition.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase));
        if (item is null) return;
        applyingSelection = true;
        Tree.SelectedItem = item;
        SelectedNodeId = item.Definition.NodeId;
        SelectedWorkspaceId = item.Definition.WorkspaceId;
        applyingSelection = false;
    }

    public bool SelectNode(string? nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || !EnsureNodeVisible(nodeId)) return false;
        Render();
        var item = Flatten(Tree.ItemsSource as IEnumerable<ITreeItemView>).OfType<TreeNodeView>()
            .FirstOrDefault(x => x.Definition.NodeId.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
        if (item is null) return false;
        applyingSelection = true;
        Tree.SelectedItem = item;
        SelectedNodeId = item.Definition.NodeId;
        SelectedWorkspaceId = item.IsEnabled ? item.Definition.WorkspaceId : null;
        applyingSelection = false;
        return true;
    }

    public bool SetNodeExpanded(string nodeId, bool isExpanded)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        var item = Flatten(Tree.ItemsSource as IEnumerable<ITreeItemView>).OfType<TreeNodeView>()
            .FirstOrDefault(x => x.Definition.NodeId.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
        if (item is null) return false;
        item.IsExpanded = isExpanded;
        if (isExpanded) expandedNodeIds.Add(nodeId); else expandedNodeIds.Remove(nodeId);
        Render();
        return true;
    }

    public bool IsNodeExpanded(string nodeId) => expandedNodeIds.Contains(nodeId);

    public TreeChildWindow GetChildWindow(string? parentNodeId)
    {
        var children = ChildrenFor(parentNodeId);
        var window = overflow.GetWindow(parentNodeId, children.Length);
        return RequiredVisibleForSelection(children) > overflow.Options.InitialVisibleChildCount
            ? window with { CanShowLess = false }
            : window;
    }

    public bool ShowMore(string? parentNodeId)
    {
        var children = ChildrenFor(parentNodeId);
        if (!overflow.GetWindow(parentNodeId, children.Length).CanShowMore) return false;
        overflow.ShowMore(parentNodeId, children.Length);
        Render();
        return true;
    }

    public bool ShowLess(string? parentNodeId)
    {
        var children = ChildrenFor(parentNodeId);
        if (!overflow.GetWindow(parentNodeId, children.Length).CanShowLess ||
            RequiredVisibleForSelection(children) > overflow.Options.InitialVisibleChildCount) return false;
        overflow.ShowLess(parentNodeId, children.Length);
        if (SelectedNodeId is not null) EnsureNodeVisible(SelectedNodeId);
        Render();
        return true;
    }

    private void Render()
    {
        CaptureExpandedState();
        var scroll = Tree.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        var offset = scroll?.Offset;
        var selected = SelectedNodeId;
        var wasApplyingSelection = applyingSelection;
        applyingSelection = true;
        TitleText.Text = localization.Get(new("Tree.Navigation"));
        Tree.ItemsSource = CreateViews(resolved, null);
        if (selected is not null)
        {
            var item = Flatten(Tree.ItemsSource as IEnumerable<ITreeItemView>).OfType<TreeNodeView>()
                .FirstOrDefault(x => x.Definition.NodeId == selected);
            if (item is not null) Tree.SelectedItem = item;
        }
        applyingSelection = wasApplyingSelection;
        if (offset is { } preservedOffset)
            Dispatcher.UIThread.Post(() =>
            {
                var current = Tree.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
                if (current is not null) current.Offset = preservedOffset;
            }, DispatcherPriority.Loaded);
    }

    private IReadOnlyList<ITreeItemView> CreateViews(ImmutableArray<ResolvedTreeNode> nodes, string? parentNodeId)
    {
        var window = overflow.GetWindow(parentNodeId, nodes.Length);
        var result = nodes.Take(window.VisibleCount).Select(CreateView).Cast<ITreeItemView>().ToList();
        var canShowLess = window.CanShowLess &&
            RequiredVisibleForSelection(nodes) <= overflow.Options.InitialVisibleChildCount;
        if (window.CanShowMore || canShowLess)
            result.Add(new TreeOverflowView(parentNodeId, window.RemainingCount,
                localization.Get(new("Tree.SeeMore")), localization.Get(new("Tree.ShowLess")),
                window.CanShowMore, canShowLess));
        return result;
    }

    private TreeNodeView CreateView(ResolvedTreeNode node) => new(node.Definition,
        localization.Get(node.Definition.DisplayNameKey),
        Geometry.Parse(icons.Resolve(node.Definition.IconKey ?? StandardIconKeys.Application).SvgPathData),
        node.IsNavigable, CreateViews(node.Children, node.Definition.NodeId),
        expandedNodeIds.Contains(node.Definition.NodeId));

    private void TreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (applyingSelection || Tree.SelectedItem is not TreeNodeView selected) return;
        SelectedNodeId = selected.Definition.NodeId;
        SelectedWorkspaceId = selected.IsEnabled ? selected.Definition.WorkspaceId : null;
        if (selected.IsEnabled) NodeSelected?.Invoke(this, new(selected.Definition));
    }

    private void ShowMoreClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: TreeOverflowView view }) ShowMore(view.ParentNodeId);
    }

    private void ShowLessClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: TreeOverflowView view }) ShowLess(view.ParentNodeId);
    }

    private bool EnsureNodeVisible(string nodeId) => EnsureNodeVisible(resolved, null, nodeId);

    private bool EnsureNodeVisible(ImmutableArray<ResolvedTreeNode> nodes, string? parentNodeId, string nodeId)
    {
        for (var index = 0; index < nodes.Length; index++)
        {
            var node = nodes[index];
            if (node.Definition.NodeId.Equals(nodeId, StringComparison.OrdinalIgnoreCase) ||
                EnsureNodeVisible(node.Children, node.Definition.NodeId, nodeId))
            {
                overflow.EnsureVisible(parentNodeId, index, nodes.Length);
                if (!node.Definition.NodeId.Equals(nodeId, StringComparison.OrdinalIgnoreCase))
                    expandedNodeIds.Add(node.Definition.NodeId);
                return true;
            }
        }
        return false;
    }

    private ImmutableArray<ResolvedTreeNode> ChildrenFor(string? parentNodeId)
    {
        if (parentNodeId is null) return resolved;
        return FlattenResolved(resolved).FirstOrDefault(x =>
            x.Definition.NodeId.Equals(parentNodeId, StringComparison.OrdinalIgnoreCase))?.Children ?? [];
    }

    private int RequiredVisibleForSelection(ImmutableArray<ResolvedTreeNode> nodes)
    {
        if (SelectedNodeId is null) return 0;
        for (var index = 0; index < nodes.Length; index++)
            if (nodes[index].Definition.NodeId.Equals(SelectedNodeId, StringComparison.OrdinalIgnoreCase) ||
                FlattenResolved(nodes[index].Children).Any(x =>
                    x.Definition.NodeId.Equals(SelectedNodeId, StringComparison.OrdinalIgnoreCase)))
                return index + 1;
        return 0;
    }

    private void CaptureExpandedState()
    {
        foreach (var node in Flatten(Tree.ItemsSource as IEnumerable<ITreeItemView>).OfType<TreeNodeView>())
        {
            if (node.IsExpanded) expandedNodeIds.Add(node.Definition.NodeId);
            else expandedNodeIds.Remove(node.Definition.NodeId);
        }
    }

    private static IEnumerable<ITreeItemView> Flatten(IEnumerable<ITreeItemView>? nodes) => (nodes ?? [])
        .SelectMany(node => new[] { node }.Concat(node is TreeNodeView treeNode ? Flatten(treeNode.Children) : []));
    private static IEnumerable<ResolvedTreeNode> FlattenResolved(IEnumerable<ResolvedTreeNode> nodes) => nodes
        .SelectMany(node => new[] { node }.Concat(FlattenResolved(node.Children)));
}

public interface ITreeItemView;
public sealed class TreeNodeView(TreeNodeDefinition definition, string label, Geometry iconPath, bool isEnabled,
    IReadOnlyList<ITreeItemView> children, bool isExpanded) : ITreeItemView
{
    public TreeNodeDefinition Definition { get; } = definition;
    public string Label { get; } = label;
    public Geometry IconPath { get; } = iconPath;
    public bool IsEnabled { get; } = isEnabled;
    public IReadOnlyList<ITreeItemView> Children { get; } = children;
    public bool IsExpanded { get; set; } = isExpanded;
}
public sealed record TreeOverflowView(string? ParentNodeId, int RemainingCount, string ShowMoreLabel,
    string ShowLessLabel, bool CanShowMore, bool CanShowLess) : ITreeItemView;
public sealed class TreeNodeSelectedEventArgs(TreeNodeDefinition node) : EventArgs { public TreeNodeDefinition Node { get; } = node; }
