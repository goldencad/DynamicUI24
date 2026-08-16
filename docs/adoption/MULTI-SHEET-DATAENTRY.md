# Adopt multi-sheet DataEntry

Define one stable `SheetCode` and existing `GridDefinition` per logical grid. Supply a materializer that creates the existing `DataEntryGridRuntime` lazily and captures compact selection, viewport, filter, sort, personalization and generation state on eviction. Never load all rows to render tabs; each grid retains its own bounded 10E viewport cache.

Resolve authorization and P1 metadata before presenting any title, subtitle, tab, overflow, search or context value. Re-resolve after company/permission/privacy changes.
