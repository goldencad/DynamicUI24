using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Ribbon;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class RibbonFoundationTests
{
    private static readonly CompanyDescriptor Company = new(new("company"), "COMPANY", "Company");
    private static readonly WorkspaceDefinition DataWorkspace = new("data", "Data", StandardTemplateCodes.DataEntry);
    private static readonly WorkspaceDefinition ReportWorkspace = new("report", "Report", StandardTemplateCodes.Report);

    [Fact]
    public void DefinitionsAreImmutableAndDeterministicallyOrdered()
    {
        RibbonTabDefinition[] input =
        [
            Tab("B", 20, Group("B", 20, Command("B", 20))),
            Tab("A", 10, Group("A", 10, Command("A", 10))),
        ];
        var definition = new RibbonDefinition("id", "code", 1, input);
        input[0] = Tab("CHANGED", 0, Group("CHANGED", 0, Command("CHANGED", 0)));

        Assert.Equal(["A", "B"], definition.Tabs.Select(x => x.TabCode));
        Assert.Equal("A", definition.Tabs[0].Groups[0].Commands[0].CommandCode);
    }

    [Fact]
    public void DefinitionValidationRejectsDuplicatesAndBadTargets()
    {
        var definition = new RibbonDefinition("id", "code", 1,
        [
            Tab("A", 0, new RibbonGroupDefinition("g1", "G", new("g"),
            [
                new("c1", "C", new("c"), StandardIconKeys.Info, RibbonCommandType.Navigate,
                    targetWorkspaceId: "missing"),
                Command("C", 2),
            ])),
            Tab("A", 2, Group("G2", 0, Command("C2", 0))),
        ]);

        var result = RibbonDefinitionValidator.Validate(definition, [DataWorkspace]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, x => x.Code == "RIBBON_DUPLICATE_TAB");
        Assert.Contains(result.Diagnostics, x => x.Code == "RIBBON_DUPLICATE_COMMAND");
        Assert.Contains(result.Diagnostics, x => x.Code == "RIBBON_UNKNOWN_WORKSPACE");
        Assert.Throws<ArgumentException>(() => new RibbonDefinition("", "CODE", 1, []));
    }

    [Fact]
    public void ResolverAppliesVisibilityContextPermissionCapabilityAndSelection()
    {
        var permission = new PermissionCode("DATA.EDIT");
        var capability = new CapabilityCode("REPORT.EXPORT");
        var definition = new RibbonDefinition("id", "code", 1,
        [
            new RibbonTabDefinition("main", "MAIN", new("main"),
            [
                new RibbonGroupDefinition("always", "ALWAYS", new("always"),
                [
                    Command("OPEN", 0),
                    new("selection", "SELECTION", new("selection"), StandardIconKeys.Edit,
                        RibbonCommandType.Refresh, requiresSelection: true),
                    new("edit", "EDIT", new("edit"), StandardIconKeys.Edit,
                        RibbonCommandType.Refresh, displayOrder: 2,
                        permissionRequirement: new(permission, UnauthorizedBehavior: UnauthorizedBehavior.Disable)),
                ]),
                new RibbonGroupDefinition("context", "CONTEXT", new("context"),
                [Command("EXPORT", 0)], displayOrder: 10,
                    contextRule: new(TemplateCode: StandardTemplateCodes.Report, CapabilityCode: capability)),
            ]),
            new RibbonTabDefinition("hidden", "HIDDEN", new("hidden"),
                [Group("G", 0, Command("C", 0))], displayOrder: 10, isVisible: false),
        ]);
        var auth = Authorization([permission], [capability]);
        var resolver = new DynamicRibbonResolver();

        var data = resolver.Resolve(definition, Context(DataWorkspace, auth, 0), [DataWorkspace, ReportWorkspace]);
        Assert.Single(data.Tabs);
        Assert.Single(data.Tabs[0].Groups);
        Assert.False(data.Tabs[0].Groups[0].Commands.Single(x => x.Definition.CommandCode == "SELECTION").IsEnabled);

        var report = resolver.Resolve(definition, Context(ReportWorkspace, auth, 1), [DataWorkspace, ReportWorkspace]);
        Assert.Equal(2, report.Tabs[0].Groups.Length);
        Assert.True(report.Tabs[0].Groups[0].Commands.Single(x => x.Definition.CommandCode == "SELECTION").IsEnabled);

        var noAccess = resolver.Resolve(definition, Context(DataWorkspace, Authorization([], []), 1));
        Assert.False(noAccess.Tabs[0].Groups[0].Commands.Single(x => x.Definition.CommandCode == "EDIT").IsEnabled);

        var unavailable = resolver.Resolve(definition, Context(ReportWorkspace, null, 1));
        Assert.Single(unavailable.Tabs[0].Groups);
        Assert.False(unavailable.Tabs[0].Groups[0].Commands.Single(x => x.Definition.CommandCode == "EDIT").IsEnabled);
    }

    [Fact]
    public async Task DispatcherHandlesNavigateRefreshCustomUnknownDeniedAndFailure()
    {
        var registry = new UiCommandRegistry();
        Assert.True(registry.Register("OK", (_, _) => Task.FromResult(RibbonCommandResult.Success())));
        Assert.True(registry.Register("FAIL", (_, _) => throw new InvalidOperationException("proof")));
        var navigation = new FakeNavigation();
        var refresh = new FakeRefresh();
        var dispatcher = new RibbonCommandDispatcher(navigation, refresh, registry);
        var context = new RibbonCommandExecutionContext(Context(DataWorkspace, Authorization([], []), 0));

        Assert.Equal(RibbonCommandResultStatus.Success,
            (await dispatcher.DispatchAsync(Resolved(new("n", "N", new("n"), StandardIconKeys.Info,
                RibbonCommandType.Navigate, targetWorkspaceId: "data")), context)).Status);
        Assert.Equal(RibbonCommandResultStatus.Unavailable,
            (await dispatcher.DispatchAsync(Resolved(new("n", "N", new("n"), StandardIconKeys.Info,
                RibbonCommandType.Navigate, targetWorkspaceId: "missing")), context)).Status);
        Assert.Equal(RibbonCommandResultStatus.Success,
            (await dispatcher.DispatchAsync(Resolved(Command("REFRESH", 0)), context)).Status);
        Assert.Equal(RibbonCommandResultStatus.Success,
            (await dispatcher.DispatchAsync(Resolved(Registered("OK")), context)).Status);
        Assert.Equal(RibbonCommandResultStatus.Unavailable,
            (await dispatcher.DispatchAsync(Resolved(Registered("UNKNOWN")), context)).Status);
        Assert.Equal(RibbonCommandResultStatus.Failed,
            (await dispatcher.DispatchAsync(Resolved(Registered("FAIL")), context)).Status);
        Assert.Equal(RibbonCommandResultStatus.Denied,
            (await dispatcher.DispatchAsync(new(Registered("OK"), AuthorizationPresentationState.VisibleDisabled), context)).Status);
    }

    private static RibbonTabDefinition Tab(string code, int order, RibbonGroupDefinition group) =>
        new(code, code, new(code), [group], order);
    private static RibbonGroupDefinition Group(string code, int order, RibbonCommandDefinition command) =>
        new(code, code, new(code), [command], order);
    private static RibbonCommandDefinition Command(string code, int order) =>
        new(code, code, new(code), StandardIconKeys.Refresh, RibbonCommandType.Refresh, order);
    private static RibbonCommandDefinition Registered(string code) =>
        new(code, code, new(code), StandardIconKeys.Info, RibbonCommandType.CustomRegistered,
            registeredCommandCode: code);
    private static ResolvedRibbonCommand Resolved(RibbonCommandDefinition command) =>
        new(command, AuthorizationPresentationState.VisibleEnabled);
    private static EffectiveAuthorizationContext Authorization(IEnumerable<PermissionCode> permissions,
        IEnumerable<CapabilityCode> capabilities) => new(new("user"), Company.CompanyId, permissions, capabilities, "r1");
    private static RibbonResolutionContext Context(WorkspaceDefinition workspace,
        EffectiveAuthorizationContext? authorization, int selection) =>
        new(Company, workspace, workspace.TemplateCode, authorization, new(selection));

    private sealed class FakeNavigation : IRibbonNavigationService
    {
        public Task<RibbonCommandResult> NavigateAsync(string? workspaceId, TemplateCode? templateCode,
            CancellationToken cancellationToken = default) => Task.FromResult(workspaceId == "data"
                ? RibbonCommandResult.Success()
                : RibbonCommandResult.Unavailable("UNKNOWN"));
    }

    private sealed class FakeRefresh : IRibbonRefreshService
    {
        public Task<RibbonCommandResult> RefreshAsync(RibbonCommandExecutionContext context,
            CancellationToken cancellationToken = default) => Task.FromResult(RibbonCommandResult.Success());
    }
}
