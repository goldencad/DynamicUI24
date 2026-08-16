using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Navigation;

namespace DynamicUI24.Core.Context;

public sealed record BreadcrumbItem(string ItemCode, string DisplayNameKey, string? IconKey = null,
    string? NavigationTarget = null, bool IsCurrent = false, PermissionCode? PermissionCode = null,
    CapabilityCode? CapabilityCode = null);
public sealed record BreadcrumbPath(ImmutableArray<BreadcrumbItem> Items)
{
    public BreadcrumbPath(IEnumerable<BreadcrumbItem> items) : this(items.ToImmutableArray())
    {
        if (Items.Length == 0 || Items.Count(x => x.IsCurrent) != 1 || !Items[^1].IsCurrent)
            throw new ArgumentException("Breadcrumb requires one final current item.", nameof(items));
    }
}
public sealed record BreadcrumbLayout(ImmutableArray<BreadcrumbItem> Visible,
    ImmutableArray<BreadcrumbItem> Overflow, bool HasOverflow);
public static class BreadcrumbOverflowResolver
{
    public static BreadcrumbLayout Resolve(BreadcrumbPath path, int maximumVisible)
    {
        if (maximumVisible < 2) maximumVisible = 2;
        if (path.Items.Length <= maximumVisible) return new(path.Items, [], false);
        var tailCount = maximumVisible - 1;
        return new([path.Items[0], .. path.Items.Skip(path.Items.Length - tailCount)],
            path.Items.Skip(1).Take(path.Items.Length - tailCount - 1).ToImmutableArray(), true);
    }
}
public sealed class BreadcrumbNavigator(IWorkspaceNavigationService navigation)
{
    public Task<WorkspaceNavigationResult> ActivateAsync(BreadcrumbItem item, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(item.NavigationTarget)
            ? Task.FromResult(WorkspaceNavigationResult.Unavailable("BREADCRUMB_TARGET_MISSING"))
            : navigation.NavigateAsync(item.NavigationTarget, cancellationToken);
}
