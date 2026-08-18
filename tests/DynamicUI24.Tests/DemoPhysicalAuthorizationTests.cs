using DynamicUI24.Core.Search;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Demo;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class DemoPhysicalAuthorizationTests
{
    private static readonly WorkspaceDefinition Safe = new("safe-demo", "Safe", StandardTemplateCodes.Dashboard);
    private static readonly WorkspaceDefinition Authoring = new("ui-authoring-demo", "Developer UI Authoring", StandardTemplateCodes.Dashboard);

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void ProfileResolvesAuthoringWorkspaceVisibility(int profileValue, bool expected)
    {
        var profile = (DemoAuthoringProfile)profileValue;
        var context = new DemoProfileContext(); context.Select(profile);
        Assert.Equal(expected, context.ResolveWorkspaces([Safe, Authoring], null).VisibleWorkspaces.Contains(Authoring));
    }

    [Fact]
    public void ProfileSwitchReResolvesAndEvictsUnauthorizedActiveWorkspaceBySemanticCode()
    {
        var context = new DemoProfileContext(); context.Select(DemoAuthoringProfile.Administrator);
        var allowed = context.ResolveWorkspaces([Safe, Authoring], Authoring.WorkspaceId);
        Assert.Same(Authoring, allowed.ActiveWorkspace); Assert.False(allowed.WasEvicted);
        context.Select(DemoAuthoringProfile.Viewer);
        var denied = context.ResolveWorkspaces([Safe, Authoring], Authoring.WorkspaceId);
        Assert.True(denied.WasEvicted); Assert.Same(Safe, denied.ActiveWorkspace);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task SearchAndQuickAccessCannotResurrectUnauthorizedAuthoring(int profileValue, bool expected)
    {
        var profile = (DemoAuthoringProfile)profileValue;
        var context = new DemoProfileContext(); context.Select(profile);
        var quick = new InMemoryQuickAccessStore();
        var providers = DemoSearch.CreateProviders([Safe, Authoring], quick, out _);
        quick.Pin(new("workspace:ui-authoring-demo", SearchResultKind.Workspace, Authoring.WorkspaceId, "WORKSPACES"));
        var coordinator = new SearchCoordinator(providers);
        var response = await coordinator.SearchAsync(new("Developer UI Authoring", SearchScope.GlobalSearch,
            PermissionContext: context.Merge(null, DemoCompanyData.CompanyAId)));
        Assert.Equal(expected, response.Results.Any(x => x.WorkspaceId == Authoring.WorkspaceId));
        var quickResponse = await coordinator.SearchAsync(new("", SearchScope.GlobalSearch,
            PermissionContext: context.Merge(null, DemoCompanyData.CompanyAId)));
        Assert.Equal(expected, quickResponse.Results.Any(x => x.WorkspaceId == Authoring.WorkspaceId));
    }

    [Fact]
    public async Task AuthorizationRequestUsesCurrentProfileGeneration()
    {
        var context = new DemoProfileContext();
        Assert.False(await context.CanOpenAuthoringAsync(DemoCompanyData.CompanyAId));
        var before = context.Generation;
        context.Select(DemoAuthoringProfile.Administrator);
        Assert.True(context.Generation > before);
        Assert.True(await context.CanOpenAuthoringAsync(DemoCompanyData.CompanyAId));
    }

    [Fact]
    public async Task InspectorEditMutatesDraftWhilePublishedDefinitionRemainsImmutable()
    {
        var session = AdministratorSession(); await session.InitializeAsync();
        var code = new DynamicUI24.Core.Authoring.UiElementCode("WORKSPACE.PEOPLE");
        var original = session.ActiveLabel(code);
        session.EditSafeLabel(code, "Không gian tiếng Việt");
        Assert.True(session.Draft.IsDirty);
        Assert.Equal("Không gian tiếng Việt", session.DraftLabel(code));
        Assert.Equal(original, session.ActiveLabel(code));
    }

    [Fact]
    public async Task RealValidationBlocksInvalidDraftPublish()
    {
        var session = AdministratorSession(); await session.InitializeAsync();
        var code = new DynamicUI24.Core.Authoring.UiElementCode("FIELD.PERSON_NAME");
        session.SetMissingParentInvalid(code, true);
        var validation = await session.ValidateAsync();
        Assert.False(validation.CanPublish);
        Assert.Contains(validation.Diagnostics, x => x.Code == "UI_MISSING_PARENT");
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.PublishAsync().AsTask());
        Assert.Equal(1, session.ActiveDefinition.Version.Value);
    }

    [Fact]
    public async Task PreviewUsesLifecycleAndDoesNotActivateDraft()
    {
        var session = AdministratorSession(); await session.InitializeAsync();
        var code = new DynamicUI24.Core.Authoring.UiElementCode("WORKSPACE.PEOPLE");
        session.EditSafeLabel(code, "Bản xem trước");
        var preview = await session.PreviewAsync();
        Assert.Equal("Bản xem trước", preview.Elements.First(x => x.Code == code).TitleKey.Value);
        Assert.NotSame(session.ActiveDefinition, preview);
        Assert.Equal(1, session.ActiveDefinition.Version.Value);
        Assert.Single(session.Versions);
    }

    [Fact]
    public async Task PublishAndRollbackActivateVersionsWithoutDeletingHistory()
    {
        var session = AdministratorSession(); await session.InitializeAsync();
        var code = new DynamicUI24.Core.Authoring.UiElementCode("WORKSPACE.PEOPLE");
        var original = session.ActiveLabel(code);
        session.EditSafeLabel(code, "Đã phát hành");
        Assert.True((await session.ValidateAsync()).CanPublish);
        await session.PublishAsync();
        Assert.Equal(2, session.ActiveDefinition.Version.Value);
        Assert.Equal("Đã phát hành", session.ActiveLabel(code));
        Assert.Equal(2, session.Versions.Count);
        await session.RollbackPreviousAsync();
        Assert.Equal(1, session.ActiveDefinition.Version.Value);
        Assert.Equal(original, session.ActiveLabel(code));
        Assert.Equal(2, session.Versions.Count);
        Assert.Contains(session.Versions, x => x.Version.Value == 2 && !x.IsActive);
    }

    [Fact]
    public async Task NonAdministratorCapabilitiesGateLifecycleOperations()
    {
        var viewer = new DemoUiAuthoringSession(() => DemoUiAuthoring.Security(DemoAuthoringProfile.Viewer, 1));
        await viewer.InitializeAsync();
        var code = new DynamicUI24.Core.Authoring.UiElementCode("WORKSPACE.PEOPLE");
        Assert.Throws<UnauthorizedAccessException>(() => viewer.EditSafeLabel(code, "Denied"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => viewer.PreviewAsync().AsTask());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => viewer.PublishAsync().AsTask());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => viewer.RollbackPreviousAsync().AsTask());
    }

    private static DemoUiAuthoringSession AdministratorSession() => new(() =>
        DemoUiAuthoring.Security(DemoAuthoringProfile.Administrator, 1));
}
