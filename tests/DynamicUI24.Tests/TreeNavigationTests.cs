using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;
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

    private static WorkspaceDefinition[] Workspaces() =>
        [new("one", "One", StandardTemplateCodes.Setup), new("two", "Two", StandardTemplateCodes.Report)];
}
