# Adopting Notification & Guidance

1. Implement `INotificationProvider` and return immutable definitions plus runtime instances. Keep detection and business resolution in the application.
2. Compose providers into one `NotificationCoordinator` with the current company, workspace, and effective authorization context.
3. Bind one `NotificationHost` to the coordinator and refresh after provider, workspace, company, or authorization state changes.
4. Dispatch guidance with `NotificationActionDispatcher`. Register commands in the existing `ActionCommandRegistry`; use the existing workspace navigation service; optionally supply focus and menu services.
5. Feed `NotificationActionBarAdapter` output through the existing `DynamicActionBarResolver` and `DynamicActionBarHost`.

Use a stable `DeduplicationKey` for repeated emissions. Set `CompanyContext` on company-scoped instances and `WorkspaceContext` on workspace-scoped instances. Apply a `PresentationRequirement` to the definition when the notification copy itself is privileged; action-only requirements are insufficient to prevent copy leakage.

The Demo includes vi-VN/en-US, all theme modes, provider failure, malformed/unknown targets, company/workspace filtering, progress, explicit blocking, multi-surface single-state, and an application-update-ready presentation. `DEMO.UPDATE_AND_RESTART` only proves registered-command dispatch and performs no updater work.
