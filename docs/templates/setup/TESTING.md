# Setup testing

Run focused tests with:

```text
dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --filter FullyQualifiedName~SetupFoundationTests
```

Coverage includes hierarchy/order, 9+ catalogs, duplicate/cycle/orphan safety, permission states, definition identity validation, candidate isolation and cancel, create/clone, valid and invalid validation, publish, retire, read-only definitions, dirty navigation guard, editor resolution, all generic field types, Action Bar permissions/selection, and deterministic Company scope.

Shared Tree tests cover configurable initial/page sizes, incremental expansion, Show less, independent hierarchy windows, selection reveal, and permission/Company filtering before paging. The macOS smoke verifies Setup's 5 → 10 → 5 catalog window and preservation across vi/en and Light/Dark changes.

Split-layout tests cover width bounds, runtime-only reset behavior, and candidate identity preservation. GUI smoke resizes the rendered Setup navigation pane from 260 to 390 to 215 pixels and verifies the category, candidate, culture/theme, and active workspace remain unchanged.

Action variant tests cover menu metadata/order/depth, permission filtering, registered and unknown command dispatch, and all shared button variants. The macOS smoke covers dropdown mouse and keyboard open/close, menu selection, split default execution, hidden/disabled items, toggle dispatch, localization/theme changes, and safe unknown command/icon fallbacks.

Shared Tree row tests cover Normal, Hover, Selected, Selected+Hover, Disabled, and KeyboardFocus precedence. The GUI smoke checks those token-based full-row states for the global and Setup trees, including overflow rows.

Architecture tests guard backend neutrality, registry-based editor resolution, semantic icons, shared Action Bars, template isolation, and absence of consumer-specific semantics.
