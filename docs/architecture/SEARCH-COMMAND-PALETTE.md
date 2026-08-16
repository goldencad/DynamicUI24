# Search / Command Palette

## What search owns

The Shell owns one keyboard-first Global Search surface, query orchestration, deterministic merge/ranking, bounded grouping, cancellation and generation validation. `Ctrl+K` (Windows/Linux), `Cmd+K` (macOS), and the visible Top Shell trigger open the same state. Up/Down selects, Enter activates, and Escape closes.

## What search does not own

Search does not authorize backend operations, instantiate workspace UI, execute arbitrary code/URLs, index application databases, retain query history, or implement business, AI, embedding, Context Panel, Breadcrumb, Help, OCR, cloud, DLP, or telemetry runtimes.

## Search scopes

- `GlobalSearch`: Shell destinations and registered actions; implemented in S1.
- `NavigationSearch`: current resolved navigation metadata; implemented in S1.
- `WorkspaceSearch`: application/Grid provider seam only; applications opt in.

## Ranking rule

Exact label, exact stable code, prefix, then contains. Pinned and Favorite state break equal-match ties, followed by recent time, result-kind priority, provider rank, and ordinal stable ID. Results are limited per kind and globally.

## Stale query rule

Every request receives a monotonic generation. A new query cancels the prior token; generation validation remains the correctness boundary when a provider ignores cancellation. Company/workspace changes call `Invalidate()` before re-resolution.

## Privacy and permission rules

Authorization uses `AuthorizationPresentationResolver`: HIDE removes the result, DISABLE keeps it non-actionable, and resolver failure is fail-closed. Sensitive subtitles use `PrivacySearchPresentation`; semantic identity and navigation targets never derive from masked text.

## Activation rule

Workspace/tree/record-like results route through `IWorkspaceNavigationService`; commands route through `IUiCommandRegistry`; settings use an explicit setting-navigation seam. Unknown targets remain unavailable and no raw exceptions reach the palette.

## Focused test commands

```sh
dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --filter FullyQualifiedName~SearchFoundationTests
dotnet test tests/DynamicUI24.ArchitectureTests/DynamicUI24.ArchitectureTests.csproj --filter FullyQualifiedName~SearchArchitectureTests
```

Common failures: localized labels used as identity, missing generation invalidation after context changes, provider exceptions escaping orchestration, or direct UI construction during activation.
