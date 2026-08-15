using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Core.Notifications;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class NotificationFoundationTests
{
    private static readonly CompanyDescriptor CompanyA = new(new("A"), "A", "Company A");
    private static readonly CompanyDescriptor CompanyB = new(new("B"), "B", "Company B");
    private static readonly WorkspaceDefinition Setup = new("setup", "Setup", StandardTemplateCodes.Setup);

    [Fact]
    public void DefinitionIsImmutableAndSeverityDoesNotEscalatePresentation()
    {
        var definition = Definition("critical", NotificationSeverity.Critical, NotificationPresentationKind.Toast);
        Assert.Equal(NotificationPresentationKind.Toast, definition.PresentationKind);
        Assert.Contains(definition.Surfaces, x => x.Surface == NotificationSurface.Toast);
        Assert.Throws<ArgumentException>(() => Definition(" "));
    }

    [Theory]
    [InlineData(-1, 0, 0, 1)]
    [InlineData(150, 100, 100, 100)]
    [InlineData(double.NaN, 100, 0, 100)]
    public void ProgressNormalizesMalformedValues(double current, double maximum, double expectedCurrent, double expectedMaximum)
    {
        var progress = new NotificationProgress(current, maximum);
        Assert.Equal(expectedCurrent, progress.CurrentValue);
        Assert.Equal(expectedMaximum, progress.MaximumValue);
        Assert.True(progress.WasNormalized);
    }

    [Fact]
    public async Task CoordinatorDeduplicatesAndSharesOneLogicalStateAcrossSurfaces()
    {
        var clock = new FakeClock();
        var definition = Definition("multi", surfaces:
        [
            new(NotificationSurface.NotificationCenter, NotificationDisplayMode.Detailed),
            new(NotificationSurface.TopActionBar), new(NotificationSurface.BottomActionBar), new(NotificationSurface.AlertCard),
        ], dedup: "CONDITION", autoShow: true, progress: new(75, 100));
        var provider = new DelegateProvider("P", context =>
        [
            Instance("one", definition, context.Now), Instance("two", definition, context.Now),
        ]);
        var coordinator = new NotificationCoordinator([provider], clock);
        var model = await coordinator.RefreshAsync(CompanyA, "setup", Authorization(CompanyA));
        var single = Assert.Single(model.Notifications);
        Assert.Equal(4, single.Surfaces.Length);
        Assert.Single(model.ForSurface(NotificationSurface.TopActionBar));
        Assert.Single(model.ForSurface(NotificationSurface.BottomActionBar));
        Assert.True(coordinator.Dismiss(single.Instance.InstanceId));
        Assert.Empty(coordinator.Current.Notifications);
    }

    [Fact]
    public async Task DismissedAndResolvedRemainDistinct()
    {
        var clock = new FakeClock();
        var coordinator = new NotificationCoordinator([Provider(Definition("dismiss"))], clock);
        var item = Assert.Single((await coordinator.RefreshAsync(CompanyA, "setup", Authorization(CompanyA))).Notifications);
        Assert.True(coordinator.Dismiss(item.Instance.InstanceId));
        Assert.Equal(NotificationLifecycleState.Dismissed, ProviderState(coordinator, item.Instance.InstanceId));

        var second = new NotificationCoordinator([Provider(Definition("resolve"))], clock);
        var resolved = Assert.Single((await second.RefreshAsync(CompanyA, "setup", Authorization(CompanyA))).Notifications);
        Assert.True(second.Resolve(resolved.Instance.InstanceId));
        Assert.Equal(NotificationLifecycleState.Resolved, ProviderState(second, resolved.Instance.InstanceId));
    }

    [Fact]
    public async Task ProviderFailureAndDuplicateIdsAreIsolated()
    {
        var clock = new FakeClock();
        var definition = Definition("safe");
        var duplicate = new DelegateProvider("DUP", c => [Instance("same", definition, c.Now), Instance("same", definition, c.Now)]);
        var coordinator = new NotificationCoordinator([new ThrowingProvider(), duplicate, Provider(definition)], clock);
        var model = await coordinator.RefreshAsync(CompanyA, "setup", Authorization(CompanyA));
        Assert.Single(model.Notifications);
        Assert.Contains(model.Diagnostics, x => x.Code == "NOTIFICATION_PROVIDER_FAILED");
        Assert.Contains(model.Diagnostics, x => x.Code == "NOTIFICATION_DUPLICATE_INSTANCE_ID");
    }

    [Fact]
    public async Task CompanyAndWorkspaceContextFailClosed()
    {
        var clock = new FakeClock();
        var companyDefinition = Definition("company", companyScope: NotificationCompanyScope.CompanyScoped);
        var workspaceDefinition = Definition("workspace", workspaceScope: NotificationWorkspaceScope.Workspace,
            workspaceId: "setup", surfaces: [new(NotificationSurface.Banner), new(NotificationSurface.NotificationCenter)]);
        var provider = new DelegateProvider("P", c =>
        [
            Instance("company", companyDefinition, c.Now, company: CompanyA.CompanyId),
            Instance("workspace", workspaceDefinition, c.Now, workspace: "setup"),
        ]);
        var coordinator = new NotificationCoordinator([provider], clock);
        var companyA = await coordinator.RefreshAsync(CompanyA, "other", Authorization(CompanyA));
        Assert.DoesNotContain(companyA.ForSurface(NotificationSurface.Banner), x => x.Instance.InstanceId == "workspace");
        var companyB = await coordinator.RefreshAsync(CompanyB, "setup", Authorization(CompanyB));
        Assert.DoesNotContain(companyB.Notifications, x => x.Instance.InstanceId == "company");
        Assert.Contains(companyB.ForSurface(NotificationSurface.Banner), x => x.Instance.InstanceId == "workspace");
    }

    [Fact]
    public async Task PermissionHideDoesNotLeakNotificationAndUnavailableDisables()
    {
        var permission = new PermissionCode("SECRET.VIEW");
        var hidden = Definition("secret", requirement: new(permission, UnauthorizedBehavior: UnauthorizedBehavior.Hide));
        var coordinator = new NotificationCoordinator([Provider(hidden)], new FakeClock());
        Assert.Empty((await coordinator.RefreshAsync(CompanyA, "setup", Authorization(CompanyA))).Notifications);

        var disabled = Definition("disabled", requirement: new(permission, UnauthorizedBehavior: UnauthorizedBehavior.Disable),
            primary: new("RUN", new("Run"), GuidanceActionType.Command, registeredCommandCode: "RUN"));
        coordinator = new([Provider(disabled)], new FakeClock());
        var result = Assert.Single((await coordinator.RefreshAsync(CompanyA, "setup", null)).Notifications);
        Assert.False(Assert.IsType<ResolvedGuidanceAction>(result.PrimaryAction).IsEnabled);
    }

    [Fact]
    public async Task AutoShowCooldownIsDeterministic()
    {
        var clock = new FakeClock();
        var coordinator = new NotificationCoordinator([Provider(Definition("toast", autoShow: true))], clock,
            TimeSpan.FromMinutes(5));
        Assert.True(Assert.Single((await coordinator.RefreshAsync(CompanyA, "setup", Authorization(CompanyA))).Notifications).ShouldAutoShow);
        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        Assert.False(Assert.Single((await coordinator.RefreshAsync(CompanyA, "setup", Authorization(CompanyA))).Notifications).ShouldAutoShow);
        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        Assert.True(Assert.Single((await coordinator.RefreshAsync(CompanyA, "setup", Authorization(CompanyA))).Notifications).ShouldAutoShow);
    }

    [Fact]
    public async Task ActionsReuseNavigationCommandAndFocusWithSafeFailures()
    {
        var navigation = new WorkspaceNavigationService([Setup]);
        var registry = new ActionCommandRegistry();
        registry.Register("KNOWN", (_, _) => Task.FromResult(ActionCommandResult.Success()));
        var dispatcher = new NotificationActionDispatcher(navigation, registry, CommandContext, new MissingFocus());
        var navigate = new ResolvedGuidanceAction(new("NAV", new("Nav"), GuidanceActionType.Navigate,
            workspaceId: "setup", focusTarget: new("UNKNOWN")), AuthorizationPresentationState.VisibleEnabled);
        var navigationResult = await dispatcher.DispatchAsync(navigate);
        Assert.Equal(GuidanceActionResultStatus.PartialSuccess, navigationResult.Status);
        Assert.Equal("setup", navigation.CurrentWorkspace?.WorkspaceId);
        var command = new ResolvedGuidanceAction(new("RUN", new("Run"), GuidanceActionType.Command,
            registeredCommandCode: "KNOWN"), AuthorizationPresentationState.VisibleEnabled);
        Assert.Equal(GuidanceActionResultStatus.Success, (await dispatcher.DispatchAsync(command)).Status);
        var unknown = new ResolvedGuidanceAction(new("UNKNOWN", new("Unknown"), GuidanceActionType.Command,
            registeredCommandCode: "UNKNOWN"), AuthorizationPresentationState.VisibleEnabled);
        Assert.Equal(GuidanceActionResultStatus.Unavailable, (await dispatcher.DispatchAsync(unknown)).Status);
    }

    [Fact]
    public async Task ActionBarAdapterReusesTopAndBottomDefinitions()
    {
        var action = new GuidanceAction("OPEN", new("Open"), GuidanceActionType.Navigate, workspaceId: "setup");
        var definition = Definition("bars", primary: action, surfaces:
            [new(NotificationSurface.TopActionBar), new(NotificationSurface.BottomActionBar)]);
        var coordinator = new NotificationCoordinator([Provider(definition)], new FakeClock());
        var notification = Assert.Single((await coordinator.RefreshAsync(CompanyA, "setup", Authorization(CompanyA))).Notifications);
        var adapter = new NotificationActionBarAdapter();
        Assert.Equal(ActionBarPosition.Top, adapter.Create(NotificationSurface.TopActionBar, [notification]).Position);
        Assert.Equal(ActionBarPosition.Bottom, adapter.Create(NotificationSurface.BottomActionBar, [notification]).Position);
    }

    private static NotificationDefinition Definition(string code, NotificationSeverity severity = NotificationSeverity.Info,
        NotificationPresentationKind kind = NotificationPresentationKind.NotificationCenterItem,
        IEnumerable<NotificationSurfaceDefinition>? surfaces = null, string? dedup = null, bool autoShow = false,
        NotificationProgress? progress = null, NotificationCompanyScope companyScope = NotificationCompanyScope.Global,
        NotificationWorkspaceScope workspaceScope = NotificationWorkspaceScope.Application, string? workspaceId = null,
        PresentationRequirement? requirement = null, GuidanceAction? primary = null) =>
        new(code, severity, kind, new($"{code}.title"), new($"{code}.message"), autoShow: autoShow,
            deduplicationKey: dedup, companyScope: companyScope, workspaceScope: workspaceScope,
            workspaceId: workspaceId, requirement: requirement, primaryAction: primary, progress: progress, surfaces: surfaces);

    private static NotificationInstance Instance(string id, NotificationDefinition definition, DateTimeOffset now,
        CompanyId? company = null, string? workspace = null) => new(id, definition, now, companyContext: company, workspaceContext: workspace);
    private static INotificationProvider Provider(NotificationDefinition definition) =>
        new DelegateProvider("P", c => [Instance(definition.NotificationCode, definition, c.Now)]);
    private static EffectiveAuthorizationContext Authorization(CompanyDescriptor company) =>
        new(new("user"), company.CompanyId, [], [], "1");
    private static ActionCommandExecutionContext CommandContext() => new(new(CompanyA, Setup, Setup.TemplateCode,
        Authorization(CompanyA), new(0), PresentationState.Ready));
    private static NotificationLifecycleState ProviderState(NotificationCoordinator coordinator, string id)
    {
        return Assert.IsType<NotificationInstance>(coordinator.FindInstance(id)).LifecycleState;
    }

    private sealed class FakeClock : INotificationClock { public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero); }
    private sealed class DelegateProvider(string code, Func<NotificationProviderContext, IReadOnlyList<NotificationInstance>> get) : INotificationProvider
    {
        public string ProviderCode => code;
        public Task<IReadOnlyList<NotificationInstance>> GetNotificationsAsync(NotificationProviderContext context, CancellationToken cancellationToken = default) => Task.FromResult(get(context));
    }
    private sealed class ThrowingProvider : INotificationProvider
    {
        public string ProviderCode => "THROW";
        public Task<IReadOnlyList<NotificationInstance>> GetNotificationsAsync(NotificationProviderContext context, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    }
    private sealed class MissingFocus : IFocusTargetService
    {
        public Task<FocusRequestResult> RequestFocusAsync(FocusTarget target, CancellationToken cancellationToken = default) => Task.FromResult(FocusRequestResult.NotFound());
    }
}
