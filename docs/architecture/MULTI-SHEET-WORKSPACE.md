# Multi-sheet workspace

`SheetHostRuntime` owns semantic sheet metadata, `ActiveSheetCode`, compact per-sheet state, activation and bounded LRU materialization. `SheetCode` is normalized, stable, non-localized and independent of title/order/tab position. Titles and subtitles are presentation metadata only.

The host does not own data virtualization, formula execution, persistence, or business-period meaning. DataEntry sheets reuse `GridDefinition`, providers and viewport caches. The default maximum materialized sheet count is two; inactive content is released after its compact state is captured. Preferences must be scoped by workspace + sheet + existing grid scope.

S1 targets workspace + `SheetCode`; S2 is invalidated immediately on activation; P1 presentation is shared by header, tab, overflow, search and accessibility. Common failures are duplicate codes, unauthorized/hidden active sheets and stale provider results; these fail closed.

Focused tests: `dotnet test tests/DynamicUI24.Tests --filter MultiSheet` and `dotnet test tests/DynamicUI24.ArchitectureTests --filter MultiSheet`.
