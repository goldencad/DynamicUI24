# Workspace panes

## Ownership and identity

The foundation owns vendor-neutral `WorkspaceCode + PaneCode`, immutable pane definitions, transient runtime state, repairable presentation preferences, and a lazy content seam. It does not own business selection, navigation, authorization decisions, or a docking/MDI environment.

## State, authorization, privacy

Definitions, runtime state, preferences, and rendered controls are separate. `PaneStateResolver` clamps stale sizes, drops removed secondary selections, and applies authorization/capability as a ceiling. Preferences contain presentation values only and never P1 content.

`WorkspacePaneSessionStateStore` owns minimum session retention across navigation and rematerialization. Its key is exactly `WorkspaceCode + PaneCode`; it never contains a rendered control or visual position. A denied pane ignores the eligible preference without deleting it, so it may become effective again if authorization later returns. Durable user preference storage is an optional application adapter over the same safe `PanePreference` shape.

## Provider and command seams

Pane actions use the existing semantic command registry. Applications provide content factories; heavy content is created only through `LazyPaneContent<T>` when shown.

## Actipro audit

Actipro Avalonia Pro 25.2.0 is installed. `AdvancedTabControl` is suitable for optional secondary tabs, while Docking/MDI is disproportionate for the required bounded layout. The first adapter therefore uses native Avalonia `Grid` and controls; no vendor type enters Core.

## Test and failures

Run `dotnet test --filter ModernWorkspaceFoundationTests`. Common failures are using position as identity, storing controls in runtime state, applying preferences after denial, or eagerly constructing secondary content.
