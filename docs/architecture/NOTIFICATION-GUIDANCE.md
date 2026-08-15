# Notification & Guidance Foundation

## What this capability owns

DynamicUI24 owns generic notification metadata, runtime lifecycle, provider isolation, context and authorization resolution, deduplication, anti-spam state, presentation models, and safe guidance dispatch. Providers detect conditions; the framework explains them and routes the user to an existing workspace or registered command.

## What it does not own

It does not detect business conditions, poll, persist history, run background jobs, update an application, execute scripts, send OS/email/SMS notifications, or implement Search, Favorites, Context panels, workflows, Odoo, or PayCalc24 behavior.

## Public contracts

- `NotificationDefinition` is immutable metadata. It contains no delegate, control, view, script, or executable payload.
- `NotificationInstance` carries runtime identity, timestamps, progress, context, unread/attention and lifecycle.
- `INotificationProvider` supplies current instances for a resolution context.
- `NotificationCoordinator` isolates providers and creates one `NotificationPresentationModel` for every renderer.
- `GuidanceAction`, `FocusTarget`, and `NotificationActionDispatcher` provide safe Navigate, Command, OpenMenu, and Dismiss operations.

## Dependency boundaries

Core is Avalonia-free. Navigate uses `IWorkspaceNavigationService`; Command uses `IActionCommandRegistry`; menu metadata reuses `ActionMenuItemDefinition`. `NotificationActionBarAdapter` creates ordinary `ActionBarDefinition` contributions, so top and bottom surfaces use the existing action resolver, host, geometry, authorization, localization, icon, and command pipeline. The Avalonia renderer consumes only resolved state and never instantiates templates.

## Safe extension points

Applications may add providers, semantic localization/icon keys, registered commands, focus resolvers, and a minimal menu service. Add a surface by extending presentation metadata and a renderer; do not fork lifecycle state per surface. Provider exceptions, malformed actions, unknown workspaces/commands/focus targets, stale company context, and unavailable authorization must remain non-fatal and fail closed.

## Focused test commands

```sh
dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --filter NotificationFoundationTests
dotnet test tests/DynamicUI24.ArchitectureTests/DynamicUI24.ArchitectureTests.csproj --filter NotificationArchitectureTests
```

## Common failure modes

- Emitting a different deduplication key on each refresh causes repeated logical items.
- Mutating definition metadata loses the definition/runtime boundary.
- Putting privileged copy on an unfiltered surface leaks information; authorization must filter the logical notification before any surface renders.
- Dispatching navigation by creating a view bypasses Tree/Ribbon/Action Bar synchronization.
- Treating dismissal as provider resolution makes unresolved conditions disappear permanently.
