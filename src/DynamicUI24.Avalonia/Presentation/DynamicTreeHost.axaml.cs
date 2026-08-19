using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Core.Search;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Theme-safe Avalonia adapter for the reusable resolved tree.</summary>
public sealed partial class DynamicTreeHost : UserControl
{
    private readonly ILocalizationService localization;
    private readonly IIconRegistry icons;
    private readonly TreeOverflowController overflow;
    private readonly IAppearancePreferenceService? appearance;
    private readonly NavigationTreeSessionState session = new();
    private ImmutableArray<ResolvedTreeNode> resolved = [];
    private ImmutableArray<ResolvedTreeNode> visible = [];
    private bool applyingSelection;

    public DynamicTreeHost()
        : this(new DictionaryLocalizationService(), new SemanticIconRegistry(), null, null)
    {
    }

    public DynamicTreeHost(ILocalizationService localization, IIconRegistry icons,
        TreeOverflowOptions? overflowOptions = null, IAppearancePreferenceService? appearance = null)
    {
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.icons = icons ?? throw new ArgumentNullException(nameof(icons));
        this.appearance = appearance;
        overflow = new(overflowOptions);
        InitializeComponent();
        AvaloniaTypography.ApplyUiFont(this);
        localization.CultureChanged += (_, _) => { ApplySearch(); Render(); };
        TitleText.Text = localization.Get(new("Tree.Navigation"));
        SearchDefinition = new();
        ApplyDensity();
        if (appearance is not null) appearance.PreferencesChanged += (_, _) => ApplyDensity();
    }

    public string? SelectedNodeId => session.SelectedNodeId;
    public string? SelectedWorkspaceId => session.SelectedWorkspaceId;
    public TreeOverflowOptions OverflowOptions => overflow.Options;
    public NavigationSearchDefinition SearchDefinition { get; set; }
    public bool ShowTitle { get => TitleText.IsVisible; set => TitleText.IsVisible = value; }
    public event EventHandler<TreeNodeSelectedEventArgs>? NodeSelected;
    public string NavigationQuery { get => SearchBox.Text ?? string.Empty; set => SearchBox.Text = value; }
    public IReadOnlyList<string> VisibleNodeIds => FlattenResolved(visible).Select(x => x.Definition.NodeId).ToArray();
    public double RowHeight => Resources["DuiNavigationRowHeight"] as double? ?? 34;
    public FontFamily ResolvedUiFontFamily => TextElement.GetFontFamily(this);
    public bool UsesSharedUiTypography => ResolvedUiFontFamily.Equals(AvaloniaTypography.UiFontFamily);

    public void Show(ResolvedTree tree)
    {
        resolved = tree.RootNodes;
        ApplySearch();
        Render();
    }

    public void SelectWorkspace(string? workspaceId)
    {
        if (workspaceId is null) return;
        var nodeId = FlattenResolved(resolved).FirstOrDefault(x =>
            string.Equals(x.Definition.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))?.Definition.NodeId;
        if (nodeId is null) return;
        EnsureNodeVisible(nodeId);
        session.Select(nodeId, workspaceId);
        Render();
        var item = Flatten(Tree.ItemsSource as IEnumerable<ITreeItemView>).OfType<TreeNodeView>().FirstOrDefault(x =>
            string.Equals(x.Definition.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase));
        if (item is null) return;
        applyingSelection = true;
        Tree.SelectedItem = item;
        applyingSelection = false;
    }

    public bool SelectNode(string? nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || !EnsureNodeVisible(nodeId)) return false;
        var resolvedNode = FlattenResolved(resolved).First(x =>
            x.Definition.NodeId.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
        session.Select(resolvedNode.Definition.NodeId,
            resolvedNode.IsNavigable ? resolvedNode.Definition.WorkspaceId : null);
        Render();
        var item = Flatten(Tree.ItemsSource as IEnumerable<ITreeItemView>).OfType<TreeNodeView>()
            .FirstOrDefault(x => x.Definition.NodeId.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
        if (item is null) return false;
        applyingSelection = true;
        Tree.SelectedItem = item;
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
        session.SetExpanded(nodeId, isExpanded);
        Render();
        return true;
    }

    public bool IsNodeExpanded(string nodeId) => session.IsExpanded(nodeId);

    public TreeRowVisualState GetNodeVisualState(string nodeId, bool isPointerOver = false,
        bool hasKeyboardFocus = false)
    {
        var node = FlattenResolved(resolved).FirstOrDefault(x =>
            x.Definition.NodeId.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
        if (node is null) throw new ArgumentException("Unknown tree node.", nameof(nodeId));
        return TreeRowVisualStateResolver.Resolve(SelectedNodeId?.Equals(nodeId, StringComparison.OrdinalIgnoreCase) == true,
            isPointerOver, node.IsNavigable, hasKeyboardFocus);
    }

    public static TreeRowVisualState GetOverflowVisualState(bool isPointerOver = false,
        bool hasKeyboardFocus = false) => TreeRowVisualStateResolver.Resolve(false, isPointerOver, true, hasKeyboardFocus);

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
        Tree.ItemsSource = CreateViews(visible, null);
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
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            return nodes.Select(CreateView).Cast<ITreeItemView>().ToList();
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
        session.IsExpanded(node.Definition.NodeId),
        SelectedNodeId?.Equals(node.Definition.NodeId, StringComparison.OrdinalIgnoreCase) == true);

    private void TreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (applyingSelection || Tree.SelectedItem is not TreeNodeView selected) return;
        foreach (var node in Flatten(Tree.ItemsSource as IEnumerable<ITreeItemView>).OfType<TreeNodeView>())
            node.IsSelected = ReferenceEquals(node, selected);
        session.Select(selected.Definition.NodeId, selected.IsEnabled ? selected.Definition.WorkspaceId : null);
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

    private void SearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplySearch();
        Render();
    }

    private void ApplySearch()
    {
        SearchBox.Watermark = Localized(SearchDefinition.PlaceholderKey, "Search navigation…");
        var query = SearchText.Normalize(SearchBox.Text);
        if (!SearchDefinition.Enabled || query.Length == 0) { visible = resolved; return; }
        ResolvedTreeNode? Visit(ResolvedTreeNode node)
        {
            var children = node.Children.Select(Visit).Where(x => x is not null).Cast<ResolvedTreeNode>().ToImmutableArray();
            var label = SearchText.Normalize(localization.Get(node.Definition.DisplayNameKey));
            var code = SearchText.Normalize(node.Definition.NodeCode);
            var matches = SearchDefinition.SearchMode == NavigationSearchMode.Prefix
                ? label.StartsWith(query, StringComparison.Ordinal) || code.StartsWith(query, StringComparison.Ordinal)
                : label.Contains(query, StringComparison.Ordinal) || code.Contains(query, StringComparison.Ordinal);
            if (!matches && children.Length == 0) return null;
            if (children.Length > 0) session.SetExpanded(node.Definition.NodeId, true);
            return node with { Children = children };
        }
        visible = resolved.Select(Visit).Where(x => x is not null).Cast<ResolvedTreeNode>().ToImmutableArray();
    }

    private string Localized(string key, string fallback)
    { var value = localization.Get(new(key)); return value == key ? fallback : value; }

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
                    session.SetExpanded(node.Definition.NodeId, true);
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
            session.SetExpanded(node.Definition.NodeId, node.IsExpanded);
        }
    }

    private void ApplyDensity()
    {
        var token = appearance?.Current.GridDensity switch
        {
            GridDensityPreference.Compact => "DuiControlHeightCompact",
            GridDensityPreference.Large => "DuiControlHeightLarge",
            _ => "DuiControlHeightStandard",
        };
        var height = Application.Current?.TryFindResource(token, out var value) == true && value is double number
            ? number : token == "DuiControlHeightCompact" ? 28 : token == "DuiControlHeightLarge" ? 40 : 34;
        Resources["DuiNavigationRowHeight"] = height * (appearance?.Current.UiScale ?? 1d);
    }

    private static IEnumerable<ITreeItemView> Flatten(IEnumerable<ITreeItemView>? nodes) => (nodes ?? [])
        .SelectMany(node => new[] { node }.Concat(node is TreeNodeView treeNode ? Flatten(treeNode.Children) : []));
    private static IEnumerable<ResolvedTreeNode> FlattenResolved(IEnumerable<ResolvedTreeNode> nodes) => nodes
        .SelectMany(node => new[] { node }.Concat(FlattenResolved(node.Children)));
}

public interface ITreeItemView;
public sealed class TreeNodeView(TreeNodeDefinition definition, string label, Geometry iconPath, bool isEnabled,
    IReadOnlyList<ITreeItemView> children, bool isExpanded, bool isSelected) : ITreeItemView, INotifyPropertyChanged
{
    public TreeNodeDefinition Definition { get; } = definition;
    public string Label { get; } = label;
    public Geometry IconPath { get; } = iconPath;
    public bool IsEnabled { get; } = isEnabled;
    public IReadOnlyList<ITreeItemView> Children { get; } = children;
    private bool expanded = isExpanded;
    private bool selected = isSelected;
    public bool IsDisabled => !IsEnabled;
    public bool HasChildren => Children.Count > 0;
    public bool IsExpanded { get => expanded; set => Set(ref expanded, value); }
    public bool IsSelected { get => selected; set => Set(ref selected, value); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
    }
}
public sealed record TreeOverflowView(string? ParentNodeId, int RemainingCount, string ShowMoreLabel,
    string ShowLessLabel, bool CanShowMore, bool CanShowLess) : ITreeItemView;
public sealed class TreeNodeSelectedEventArgs(TreeNodeDefinition node) : EventArgs { public TreeNodeDefinition Node { get; } = node; }
