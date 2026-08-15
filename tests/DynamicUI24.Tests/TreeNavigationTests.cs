using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;
using DynamicUI24.Avalonia.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class TreeNavigationTests
{
    [Fact]
    public void InvalidParentAndCyclesAreRejected() 
    {
        Assert.Throws<ArgumentException>(() => new TreeDefinition("t", "T", 1,
            [new("a", "A", new("A"), "missing")]));
        Assert.Throws<ArgumentException>(() => new TreeDefinition("t", "T", 1,
            [new("a", "A", new("A"), "b"), new("b", "B", new("B"), "a")]));
    }

    [Fact]
    public void ResolverHidesPrivilegedNodesAndOrdersRemainingNodesDeterministically()
    {
        var tree = new TreeDefinition("t", "T", 1,
        [
            new("b", "SECOND", new("B"), displayOrder: 1, workspaceId: "two"),
            new("a", "FIRST", new("A"), displayOrder: 1, workspaceId: "one"),
            new("hidden", "HIDDEN", new("H"), workspaceId: "one", permissionRequirement: new(new PermissionCode("X"), UnauthorizedBehavior: UnauthorizedBehavior.Hide)),
        ]);
        var result = new DynamicTreeResolver().Resolve(tree,
            new(new(new("c"), "C", "Company"), AuthorizationValueTests.Ready("c", [], [])), Workspaces());
        Assert.Equal(["FIRST", "SECOND"], result.RootNodes.Select(x => x.Definition.NodeCode));
    }

    [Fact]
    public async Task NavigationSafelyRejectsUnknownWorkspace()
    {
        var navigation = new WorkspaceNavigationService(Workspaces());
        Assert.False((await navigation.NavigateAsync("missing")).IsSuccess);
        Assert.True((await navigation.NavigateAsync("one")).IsSuccess);
        Assert.Equal("one", navigation.CurrentWorkspace!.WorkspaceId);
    }

    [Fact]
    public void OverflowUsesConfigurableInitialWindowPagesAndShowLess()
    {
        var overflow = new TreeOverflowController(new(5, 3));
        Assert.Equal(new TreeChildWindow(12, 5, true, false), overflow.GetWindow("parent", 12));
        Assert.Equal(8, overflow.ShowMore("parent", 12).VisibleCount);
        Assert.Equal(11, overflow.ShowMore("parent", 12).VisibleCount);
        var final = overflow.ShowMore("parent", 12);
        Assert.Equal(12, final.VisibleCount);
        Assert.False(final.CanShowMore);
        Assert.True(final.CanShowLess);
        Assert.Equal(5, overflow.ShowLess("parent", 12).VisibleCount);
    }

    [Fact]
    public void OverflowPreservesIndependentHierarchyWindowsAndCanRevealSelection()
    {
        var overflow = new TreeOverflowController(new(4, 4));
        overflow.ShowMore("parent-a", 20);
        overflow.EnsureVisible("parent-b", 11, 20);
        Assert.Equal(8, overflow.GetWindow("parent-a", 20).VisibleCount);
        Assert.Equal(12, overflow.GetWindow("parent-b", 20).VisibleCount);
        Assert.Equal(4, overflow.GetWindow("parent-c", 20).VisibleCount);
    }

    [Fact]
    public void PermissionAndCompanyFilteringHappensBeforeOverflowWindowing()
    {
        var permission = new PermissionCode("TREE.EXTRA");
        var nodes = Enumerable.Range(1, 10).Select(index => new TreeNodeDefinition($"n{index}", $"N_{index}",
            new($"N{index}"), permissionRequirement: index > 6
                ? new(permission, UnauthorizedBehavior: UnauthorizedBehavior.Hide) : null));
        var tree = new TreeDefinition("overflow", "OVERFLOW", 1, nodes);
        var company = new CompanyDescriptor(new("company"), "COMPANY", "Company");
        var resolved = new DynamicTreeResolver().Resolve(tree,
            new(company, AuthorizationValueTests.Ready("company", [], [])), []);
        Assert.Equal(6, resolved.RootNodes.Length);
        var window = new TreeOverflowController(new TreeOverflowOptions(5, 5)).GetWindow(null, resolved.RootNodes.Length);
        Assert.Equal(5, window.VisibleCount);
        Assert.Equal(1, window.RemainingCount);
    }

    [Fact]
    public void OverflowRejectsInvalidConfigurationAndIndices()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreeOverflowOptions(0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreeOverflowOptions(5, 0));
        var overflow = new TreeOverflowController();
        Assert.Throws<ArgumentOutOfRangeException>(() => overflow.EnsureVisible("parent", 10, 10));
    }

    [Fact]
    public void OverflowLabelsAreLocalizedInEnglishAndVietnamese()
    {
        var localization = new DictionaryLocalizationService("en-US");
        Assert.Equal("See more", localization.Get(new("Tree.SeeMore")));
        Assert.Equal("Show less", localization.Get(new("Tree.ShowLess")));
        Assert.True(localization.TrySetCulture("vi-VN"));
        Assert.Equal("Xem thêm", localization.Get(new("Tree.SeeMore")));
        Assert.Equal("Thu gọn", localization.Get(new("Tree.ShowLess")));
    }

    private static WorkspaceDefinition[] Workspaces() =>
        [new("one", "One", StandardTemplateCodes.Setup), new("two", "Two", StandardTemplateCodes.Report)];
}
