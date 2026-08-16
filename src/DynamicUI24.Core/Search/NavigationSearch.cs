using System.Collections.Immutable;
using DynamicUI24.Core.Navigation;

namespace DynamicUI24.Core.Search;

public enum NavigationSearchMode { Contains, Prefix }
public sealed record NavigationSearchDefinition(bool Enabled = true, string PlaceholderKey = "Search.Navigation.Placeholder",
    int? AutoShowAboveItemCount = null, NavigationSearchMode SearchMode = NavigationSearchMode.Contains,
    bool IncludeCollapsedDescendants = true, bool PreserveHierarchy = true);

public sealed class NavigationTreeSearch
{
    public ImmutableArray<ResolvedTreeNode> Filter(IEnumerable<ResolvedTreeNode> roots, string? query,
        NavigationSearchDefinition? definition = null)
    {
        definition ??= new();
        var source = roots.ToImmutableArray();
        var text = SearchText.Normalize(query);
        if (!definition.Enabled || text.Length == 0) return source;
        ResolvedTreeNode? Visit(ResolvedTreeNode node)
        {
            var children = node.Children.Select(Visit).Where(x => x is not null).Cast<ResolvedTreeNode>().ToImmutableArray();
            var label = SearchText.Normalize(node.Definition.DisplayNameKey.Value);
            var code = SearchText.Normalize(node.Definition.NodeCode);
            var match = definition.SearchMode == NavigationSearchMode.Prefix
                ? label.StartsWith(text, StringComparison.Ordinal) || code.StartsWith(text, StringComparison.Ordinal)
                : label.Contains(text, StringComparison.Ordinal) || code.Contains(text, StringComparison.Ordinal);
            return match || children.Length > 0 ? node with { Children = children } : null;
        }
        return source.Select(Visit).Where(x => x is not null).Cast<ResolvedTreeNode>().ToImmutableArray();
    }
}
