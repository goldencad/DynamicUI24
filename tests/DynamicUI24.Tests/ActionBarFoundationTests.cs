using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class ActionBarFoundationTests
{
    private static readonly CompanyDescriptor CompanyA = new(new("a"), "A", "Company A");
    private static readonly CompanyDescriptor CompanyB = new(new("b"), "B", "Company B");
    private static readonly WorkspaceDefinition Data = new("data", "Data", StandardTemplateCodes.DataEntry);
    private static readonly WorkspaceDefinition Report = new("report", "Report", StandardTemplateCodes.Report);

    [Fact]
    public void DefinitionsSupportTopBottomOrderingImmutabilityAndDuplicateRejection()
    {
        ActionDefinition[] input = [Action("B", 20), Action("A", 10)];
        var top = new ActionBarDefinition("top", "top", ActionBarPosition.Top, input);
        input[0] = Action("CHANGED", 0);
        var bottom = new ActionBarDefinition("bottom", "bottom", ActionBarPosition.Bottom, []);

        Assert.Equal(ActionBarPosition.Top, top.Position);
        Assert.Equal(ActionBarPosition.Bottom, bottom.Position);
        Assert.Equal(["A", "B"], top.Actions.Select(x => x.ActionCode));
        Assert.Throws<ArgumentException>(() => new ActionBarDefinition("bar", "bar", ActionBarPosition.Top,
            [Action("A", 0), new("A", "OTHER", new("other"), StandardIconKeys.Info, ActionType.Refresh)]));
        Assert.Throws<ArgumentException>(() => new WorkspaceActionBarDefinitions(
            [new("data", [top, new ActionBarDefinition("top", "other", ActionBarPosition.Bottom, [])])]));
    }

    [Fact]
    public void ActionDefinitionRejectsEmptyCodeAndInvalidSelectionConstraints()
    {
        Assert.Throws<ArgumentException>(() => new ActionDefinition("id", "", new("name"),
            StandardIconKeys.Info, ActionType.Refresh));
        Assert.Throws<ArgumentException>(() => new ActionDefinition("id", "bad code!", new("name"),
            StandardIconKeys.Info, ActionType.Refresh));
        Assert.Throws<ArgumentException>(() => new ActionDefinition("id", "code", new("name"),
            StandardIconKeys.Info, ActionType.Edit, minSelection: 2, maxSelection: 1));
        var valid = new ActionDefinition("id", "edit", new("name"), StandardIconKeys.Edit,
            ActionType.Edit, requiresSelection: true, minSelection: 1, maxSelection: 1);
        Assert.Equal("EDIT", valid.ActionCode);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActionSelectionContext(-1));
    }

    [Fact]
    public void ResolverAppliesVisibilityPermissionCapabilityFailClosedAndCompanySwitch()
    {
        var permission = new PermissionCode("DATA.EDIT");
        var capability = new CapabilityCode("DATA.CAPABILITY");
        var bar = new ActionBarDefinition("top", "top", ActionBarPosition.Top,
        [
            Action("VISIBLE", 0),
            new("hidden", "HIDDEN", new("hidden"), StandardIconKeys.Info, ActionType.Refresh, isVisible: false),
            new("permission", "PERMISSION", new("permission"), StandardIconKeys.Edit, ActionType.Edit,
                permissionRequirement: new(permission, UnauthorizedBehavior: UnauthorizedBehavior.Disable)),
            new("capability", "CAPABILITY", new("capability"), StandardIconKeys.Info, ActionType.Refresh,
                permissionRequirement: new(CapabilityCode: capability, UnauthorizedBehavior: UnauthorizedBehavior.Hide)),
        ]);
        var resolver = new DynamicActionBarResolver();

        var companyA = resolver.Resolve(bar, Context(CompanyA, Authorization(CompanyA, [permission], [capability]), 0));
        Assert.Equal(3, companyA.Actions.Length);
        Assert.All(companyA.Actions, action => Assert.True(action.IsEnabled));

        var companyB = resolver.Resolve(bar, Context(CompanyB, Authorization(CompanyB, [], []), 0));
        Assert.Equal(["PERMISSION", "VISIBLE"], companyB.Actions.Select(x => x.Definition.ActionCode).Order());
        Assert.False(companyB.Actions.Single(x => x.Definition.ActionCode == "PERMISSION").IsEnabled);

        var unavailable = resolver.Resolve(bar, Context(CompanyB, null, 0));
        Assert.False(unavailable.Actions.Single(x => x.Definition.ActionCode == "PERMISSION").IsEnabled);
        Assert.DoesNotContain(unavailable.Actions, x => x.Definition.ActionCode == "CAPABILITY");
    }

    [Theory]
    [InlineData(0, false, false, true)]
    [InlineData(1, true, true, true)]
    [InlineData(5, true, false, true)]
    [InlineData(6, true, false, false)]
    public void ResolverAppliesSelectionCountMinMaxAndRequiresSelection(int count,
        bool requiresEnabled, bool singleEnabled, bool batchEnabled)
    {
        var bar = new ActionBarDefinition("top", "top", ActionBarPosition.Top,
        [
            new("requires", "REQUIRES", new("requires"), StandardIconKeys.Edit, ActionType.Edit,
                requiresSelection: true),
            new("single", "SINGLE", new("single"), StandardIconKeys.Edit, ActionType.Edit,
                minSelection: 1, maxSelection: 1),
            new("batch", "BATCH", new("batch"), StandardIconKeys.Delete, ActionType.Delete,
                minSelection: 0, maxSelection: 5),
        ]);
        var result = new DynamicActionBarResolver().Resolve(bar, Context(CompanyA, Authorization(CompanyA, [], []), count));

        Assert.Equal(requiresEnabled, Find(result, "REQUIRES").IsEnabled);
        Assert.Equal(singleEnabled, Find(result, "SINGLE").IsEnabled);
        Assert.Equal(batchEnabled, Find(result, "BATCH").IsEnabled);
    }

    [Fact]
    public void BottomStatusPreservesAllValuesAndDistinguishesZeroFromUnavailable()
    {
        var bar = new ActionBarDefinition("bottom", "bottom", ActionBarPosition.Bottom, []);
        var status = new ActionBarStatus(0, 0, 0, 0, 0, 0, false);
        var resolved = new DynamicActionBarResolver().Resolve(bar,
            Context(CompanyA, Authorization(CompanyA, [], []), 0) with { Status = status });

        Assert.Equal(status, resolved.Status);
        Assert.Equal(0, resolved.Status!.TotalRows);
        Assert.NotNull(resolved.Status.ErrorCount);
        Assert.Null(new ActionBarStatus().TotalRows);
    }

    [Fact]
    public void ResolverSafelyOmitsMalformedNavigateAndUnknownWorkspace()
    {
        var bar = new ActionBarDefinition("top", "top", ActionBarPosition.Top,
        [
            new("missing", "MISSING", new("missing"), StandardIconKeys.Info, ActionType.Navigate),
            new("unknown", "UNKNOWN", new("unknown"), StandardIconKeys.Info, ActionType.Navigate,
                targetWorkspaceId: "unknown"),
        ]);
        var result = new DynamicActionBarResolver().Resolve(bar,
            Context(CompanyA, Authorization(CompanyA, [], []), 0), [Data, Report]);

        Assert.Empty(result.Actions);
        Assert.Contains(result.Diagnostics, x => x.Code == "ACTION_NAVIGATE_TARGET_MISSING");
        Assert.Contains(result.Diagnostics, x => x.Code == "ACTION_UNKNOWN_WORKSPACE");
    }

    [Fact]
    public async Task DispatcherHandlesNavigateRefreshRegisteredUnknownDeniedUnavailableAndFailed()
    {
        var navigation = new WorkspaceNavigationService([Data, Report]);
        var refresh = new FakeRefresh();
        var registry = new ActionCommandRegistry();
        registry.Register("OK", (_, _) => Task.FromResult(ActionCommandResult.Success()));
        registry.Register("DENIED", (_, _) => Task.FromResult(ActionCommandResult.Denied()));
        registry.Register("UNAVAILABLE", (_, _) => Task.FromResult(ActionCommandResult.Unavailable("PROOF")));
        registry.Register("FAIL", (_, _) => throw new InvalidOperationException("proof"));
        var dispatcher = new ActionBarCommandDispatcher(navigation, refresh, registry);
        var context = new ActionCommandExecutionContext(Context(CompanyA, Authorization(CompanyA, [], []), 0));

        Assert.Equal(ActionCommandResultStatus.Success, (await dispatcher.DispatchAsync(Resolved(
            new("nav", "NAV", new("nav"), StandardIconKeys.Info, ActionType.Navigate, targetWorkspaceId: "report")), context)).Status);
        Assert.Equal("report", navigation.CurrentWorkspace!.WorkspaceId);
        Assert.Equal(ActionCommandResultStatus.Unavailable, (await dispatcher.DispatchAsync(Resolved(
            new("nav", "NAV", new("nav"), StandardIconKeys.Info, ActionType.Navigate, targetWorkspaceId: "missing")), context)).Status);
        Assert.Equal(ActionCommandResultStatus.Success, (await dispatcher.DispatchAsync(Resolved(Action("REFRESH", 0)), context)).Status);
        Assert.Equal(ActionCommandResultStatus.Success, (await dispatcher.DispatchAsync(Resolved(Registered("OK")), context)).Status);
        Assert.Equal(ActionCommandResultStatus.Unavailable, (await dispatcher.DispatchAsync(Resolved(Registered("UNKNOWN")), context)).Status);
        Assert.Equal(ActionCommandResultStatus.Denied, (await dispatcher.DispatchAsync(Resolved(Registered("DENIED")), context)).Status);
        Assert.Equal(ActionCommandResultStatus.Unavailable, (await dispatcher.DispatchAsync(Resolved(Registered("UNAVAILABLE")), context)).Status);
        Assert.Equal(ActionCommandResultStatus.Failed, (await dispatcher.DispatchAsync(Resolved(Registered("FAIL")), context)).Status);
        Assert.Equal(ActionCommandResultStatus.Denied, (await dispatcher.DispatchAsync(
            new(Registered("OK"), AuthorizationPresentationState.VisibleDisabled), context)).Status);
        Assert.Equal(1, refresh.Count);
    }

    private static ActionDefinition Action(string code, int order) =>
        new(code, code, new(code), StandardIconKeys.Refresh, ActionType.Refresh, order);
    private static ActionDefinition Registered(string code) =>
        new(code, code, new(code), StandardIconKeys.Info, ActionType.CustomRegistered, registeredCommandCode: code);
    private static ResolvedAction Resolved(ActionDefinition definition) =>
        new(definition, AuthorizationPresentationState.VisibleEnabled);
    private static ResolvedAction Find(ResolvedActionBar bar, string code) =>
        bar.Actions.Single(x => x.Definition.ActionCode == code);
    private static EffectiveAuthorizationContext Authorization(CompanyDescriptor company,
        IEnumerable<PermissionCode> permissions, IEnumerable<CapabilityCode> capabilities) =>
        new(new("user"), company.CompanyId, permissions, capabilities, "r1");
    private static ActionBarResolutionContext Context(CompanyDescriptor company,
        EffectiveAuthorizationContext? authorization, int selection) =>
        new(company, Data, Data.TemplateCode, authorization, new(selection), PresentationState.Ready);

    private sealed class FakeRefresh : IActionRefreshService
    {
        public int Count { get; private set; }
        public Task<ActionCommandResult> RefreshAsync(ActionCommandExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return Task.FromResult(ActionCommandResult.Success());
        }
    }
}
