# Context Panel Architecture

## What Context Panel owns

The shell owns one optional right-side region, its open state, bounded width, selected section, loading/empty/error presentation, and the generation that identifies the latest request. `ContextPanelDefinition` is immutable application metadata; `ContextPanelState` is user/runtime state. Never mutate definitions to store preferences.

## What it does not own

It does not own navigation, workspace lifetime, grid/tree controls, business editing, activity persistence, search, collaboration, or AI. Closing the panel leaves workspace and selection state intact.

## Panel definition and state

Definitions declare semantic panel/section codes, provider codes, width bounds, scope, permissions, capabilities and help codes. State supports open, close, toggle, bounded resize and section selection. Applications may persist `ContextPanelPreference`; the framework ships an in-memory seam only.

## Selection identity and stale-result rule

`ContextSelection` carries only semantic entity, RowKey, VariableCode and document keys. Providers must never retain visual rows or scan a dataset. `ContextPanelCoordinator` cancels the previous call and increments generation for selection, company, workspace and refresh changes. Cancellation is an optimization: a result publishes only when its generation is still current.

## Privacy and permissions

`ContextItemPresenter` routes every value through P1 `IPrivacyPolicyResolver` and `ISensitiveValuePresenter`, and uses `AuthorizationPresentationResolver`. Resolver failure for protected content fails closed. The same safe projection must feed visible text, copy, tooltips and accessibility.

## Layout

`ShellSplitLayoutState` extends the shared split foundation for left navigation, a minimum-width main workspace and a bounded right region. Both splitters resize columns; the panel never overlays search or notifications.

## Focused tests

`dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --filter ContextFoundationTests`

## Common failure modes

Duplicate codes, invalid widths and malformed duplicate result IDs become validation errors. Unknown/throwing providers produce a safe panel error. Missing authorization fails closed. Late results are ignored. A missing selection is an empty state, not an error.
