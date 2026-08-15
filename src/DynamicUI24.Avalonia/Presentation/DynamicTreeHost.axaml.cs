using System.Collections.Immutable;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Theme-safe Avalonia adapter for the reusable resolved tree.</summary>
public sealed partial class DynamicTreeHost : UserControl
{
    private readonly ILocalizationService localization;
    private readonly IIconRegistry icons;
    private ImmutableArray<ResolvedTreeNode> resolved = [];
    private bool applyingSelection;

    public DynamicTreeHost()
        : this(new DictionaryLocalizationService(), new SemanticIconRegistry())
    {
    }

    public DynamicTreeHost(ILocalizationService localization, IIconRegistry icons)
    {
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.icons = icons ?? throw new ArgumentNullException(nameof(icons));
        InitializeComponent();
        localization.CultureChanged += (_, _) => Render();
        TitleText.Text = localization.Get(new("Tree.Navigation"));
    }

    public string? SelectedNodeId { get; private set; }
    public string? SelectedWorkspaceId { get; private set; }
    public event EventHandler<TreeNodeSelectedEventArgs>? NodeSelected;

    public void Show(ResolvedTree tree)
    {
        resolved = tree.RootNodes;
        Render();
    }

    public void SelectWorkspace(string? workspaceId)
    {
        if (workspaceId is null) return;
        var item = Flatten(Tree.ItemsSource as IEnumerable<TreeNodeView>).FirstOrDefault(x =>
            string.Equals(x.Definition.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase));
        if (item is null) return;
        applyingSelection = true;
        Tree.SelectedItem = item;
        SelectedNodeId = item.Definition.NodeId;
        SelectedWorkspaceId = item.Definition.WorkspaceId;
        applyingSelection = false;
    }

    private void Render()
    {
        var selected = SelectedNodeId;
        TitleText.Text = localization.Get(new("Tree.Navigation"));
        Tree.ItemsSource = resolved.Select(CreateView).ToArray();
        if (selected is not null)
        {
            var item = Flatten(Tree.ItemsSource as IEnumerable<TreeNodeView>).FirstOrDefault(x => x.Definition.NodeId == selected);
            if (item is not null) Tree.SelectedItem = item;
        }
    }

    private TreeNodeView CreateView(ResolvedTreeNode node) => new(node.Definition,
        localization.Get(node.Definition.DisplayNameKey),
        Geometry.Parse(icons.Resolve(node.Definition.IconKey ?? StandardIconKeys.Application).SvgPathData),
        node.IsNavigable, node.Children.Select(CreateView).ToArray());

    private void TreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (applyingSelection || Tree.SelectedItem is not TreeNodeView selected) return;
        SelectedNodeId = selected.Definition.NodeId;
        SelectedWorkspaceId = selected.IsEnabled ? selected.Definition.WorkspaceId : null;
        if (selected.IsEnabled) NodeSelected?.Invoke(this, new(selected.Definition));
    }

    private static IEnumerable<TreeNodeView> Flatten(IEnumerable<TreeNodeView>? nodes) => (nodes ?? [])
        .SelectMany(node => new[] { node }.Concat(Flatten(node.Children)));
}

public sealed record TreeNodeView(TreeNodeDefinition Definition, string Label, Geometry IconPath, bool IsEnabled,
    IReadOnlyList<TreeNodeView> Children);
public sealed class TreeNodeSelectedEventArgs(TreeNodeDefinition node) : EventArgs { public TreeNodeDefinition Node { get; } = node; }
