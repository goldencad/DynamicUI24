using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Notifications;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

internal sealed class DemoNotificationProvider : INotificationProvider
{
    public string ProviderCode => "DEMO.NOTIFICATIONS";

    public Task<IReadOnlyList<NotificationInstance>> GetNotificationsAsync(NotificationProviderContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = context.Now;
        NotificationSurfaceDefinition Surface(NotificationSurface surface, NotificationDisplayMode mode,
            int order = 0) => new(surface, mode, displayOrder: order);
        GuidanceAction Command(string code, string key, string command) => new(code, new(key), GuidanceActionType.Command,
            StandardIconKeys.Action, registeredCommandCode: command);
        GuidanceAction Navigate(string code, string key, string workspace, string? focus = null) => new(code, new(key),
            GuidanceActionType.Navigate, StandardIconKeys.Workspace, workspaceId: workspace,
            focusTarget: focus is null ? null : new(focus));
        NotificationInstance Item(string id, NotificationDefinition definition, bool attention = false,
            NotificationProgress? progress = null, DynamicUI24.Core.Companies.CompanyId? company = null,
            string? workspace = null, NotificationLifecycleState state = NotificationLifecycleState.Active) =>
            new(id, definition, now.AddMinutes(-2), now, state, true, attention, progress, company, workspace);

        var updateProgress = new NotificationProgress(75, 100);
        var update = new NotificationDefinition("DEMO.UPDATE_READY", NotificationSeverity.Info,
            NotificationPresentationKind.AlertCard, new("Notification.Update.Title"), new("Notification.Update.Message"),
            StandardIconKeys.Info, autoShow: true, priority: 80, deduplicationKey: "DEMO.UPDATE_READY",
            primaryAction: Command("UPDATE_RESTART", "Notification.Action.Update", "DEMO.UPDATE_AND_RESTART"),
            progress: updateProgress,
            surfaces:
            [
                Surface(NotificationSurface.NotificationCenter, NotificationDisplayMode.Detailed),
                Surface(NotificationSurface.TopActionBar, NotificationDisplayMode.Standard, 10),
                Surface(NotificationSurface.BottomActionBar, NotificationDisplayMode.Compact, 20),
                Surface(NotificationSurface.AlertCard, NotificationDisplayMode.Detailed, 30),
            ]);

        IReadOnlyList<NotificationInstance> result =
        [
            Item("refresh-complete", new("DEMO.REFRESH", NotificationSeverity.Success, NotificationPresentationKind.Toast,
                new("Notification.Refresh.Title"), new("Notification.Refresh.Message"), StandardIconKeys.Success,
                autoShow: true, deduplicationKey: "REFRESH", surfaces:
                [Surface(NotificationSurface.Toast, NotificationDisplayMode.Standard), Surface(NotificationSurface.NotificationCenter, NotificationDisplayMode.Detailed)])),
            Item("configuration", new("DEMO.CONFIG", NotificationSeverity.Warning, NotificationPresentationKind.AlertCard,
                new("Notification.Config.Title"), new("Notification.Config.Message"), StandardIconKeys.Warning,
                autoShow: true, priority: 90, deduplicationKey: "CONFIG", primaryAction:
                Navigate("REVIEW_CONFIG", "Notification.Action.Review", "setup-demo", "SETUP.GENERAL"),
                surfaces: [Surface(NotificationSurface.AlertCard, NotificationDisplayMode.Detailed), Surface(NotificationSurface.NotificationCenter, NotificationDisplayMode.Detailed)]), true),
            Item("workspace-error", new("DEMO.WORKSPACE_ERROR", NotificationSeverity.Error, NotificationPresentationKind.Banner,
                new("Notification.Error.Title"), new("Notification.Error.Message"), StandardIconKeys.Error,
                priority: 100, workspaceScope: NotificationWorkspaceScope.Workspace, workspaceId: "data-entry-demo",
                primaryAction: Navigate("OPEN_DATA", "Notification.Action.OpenWorkspace", "data-entry-demo"),
                surfaces: [Surface(NotificationSurface.Banner, NotificationDisplayMode.Detailed), Surface(NotificationSurface.NotificationCenter, NotificationDisplayMode.Detailed)]),
                true, workspace: "data-entry-demo"),
            Item("company-a", new("DEMO.COMPANY", NotificationSeverity.Warning, NotificationPresentationKind.NotificationCenterItem,
                new("Notification.Company.Title"), new("Notification.Company.Message"), StandardIconKeys.Company,
                companyScope: NotificationCompanyScope.CompanyScoped,
                surfaces: [Surface(NotificationSurface.NotificationCenter, NotificationDisplayMode.Detailed)]), true,
                company: DemoCompanyData.CompanyAId),
            Item("unknown-workspace", new("DEMO.UNKNOWN_WORKSPACE", NotificationSeverity.Info, NotificationPresentationKind.NotificationCenterItem,
                new("Notification.UnknownWorkspace.Title"), new("Notification.UnknownWorkspace.Message"),
                primaryAction: Navigate("UNKNOWN_WORKSPACE", "Notification.Action.OpenWorkspace", "missing-workspace"))),
            Item("unknown-command", new("DEMO.UNKNOWN_COMMAND", NotificationSeverity.Info, NotificationPresentationKind.NotificationCenterItem,
                new("Notification.UnknownCommand.Title"), new("Notification.UnknownCommand.Message"),
                primaryAction: Command("UNKNOWN_COMMAND", "Notification.Action.Run", "DEMO.UNKNOWN"))),
            Item("unknown-focus", new("DEMO.UNKNOWN_FOCUS", NotificationSeverity.Info, NotificationPresentationKind.NotificationCenterItem,
                new("Notification.UnknownFocus.Title"), new("Notification.UnknownFocus.Message"),
                primaryAction: Navigate("UNKNOWN_FOCUS", "Notification.Action.Review", "setup-demo", "MISSING.FOCUS"))),
            Item("blocking", new("DEMO.BLOCKING", NotificationSeverity.Critical, NotificationPresentationKind.BlockingNotice,
                new("Notification.Blocking.Title"), new("Notification.Blocking.Message"), StandardIconKeys.Warning,
                dismissible: false, primaryAction: Navigate("RESOLVE_BLOCK", "Notification.Action.Review", "setup-demo"),
                surfaces: [Surface(NotificationSurface.BlockingNotice, NotificationDisplayMode.Detailed)]), true),
            Item("expired", new("DEMO.EXPIRED", NotificationSeverity.Info, NotificationPresentationKind.Toast,
                new("Notification.Expired.Title"), new("Notification.Expired.Message"), expiration: now.AddMinutes(-1)), state: NotificationLifecycleState.Expired),
            Item("resolved", new("DEMO.RESOLVED", NotificationSeverity.Success, NotificationPresentationKind.NotificationCenterItem,
                new("Notification.Resolved.Title"), new("Notification.Resolved.Message")), state: NotificationLifecycleState.Resolved),
            Item("update-a", update, true, updateProgress),
            Item("update-b-duplicate", update, true, updateProgress),
        ];
        return Task.FromResult(result);
    }
}

internal sealed class ThrowingNotificationProvider : INotificationProvider
{
    public string ProviderCode => "DEMO.THROWING";
    public Task<IReadOnlyList<NotificationInstance>> GetNotificationsAsync(NotificationProviderContext context,
        CancellationToken cancellationToken = default) => throw new InvalidOperationException("Demo provider failure");
}

internal sealed class DemoFocusTargetService : IFocusTargetService
{
    public Task<FocusRequestResult> RequestFocusAsync(FocusTarget target, CancellationToken cancellationToken = default) =>
        Task.FromResult(target.FocusTargetCode == "SETUP.GENERAL" ? new FocusRequestResult(true) : FocusRequestResult.NotFound());
}

internal sealed class DemoNotificationMenuService : INotificationMenuService
{
    public Task<bool> OpenAsync(GuidanceAction action, CancellationToken cancellationToken = default) =>
        Task.FromResult(action.MenuItems.Length > 0);
}
