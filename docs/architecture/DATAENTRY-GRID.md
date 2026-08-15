# DataEntry Grid architecture

The DataEntry Grid is split into a UI-platform-free runtime and a presentation adapter.

```text
GridDefinition + Task 9 ColumnDefinition
                 + IDataEntryGridProvider / optional viewport capability
                 + Company/Workspace/Authorization
                              |
                    DataEntryGridRuntime
          metadata · bounded rows · logical count · selection
          edit buffer · sort/filter · generation · cache · status
                              |
                      DataEntryGridHost
```

Core owns deterministic resolution, state and diagnostics. The Avalonia host builds columns dynamically, applies semantic resources, listens to localization/appearance services, exposes native focus/editor behavior and fills the available workspace through scrolling layout. It contains no Demo columns.

The grid has no implementation dependency on Tree or Ribbon. Workspace navigation selects the existing `DATA_ENTRY` factory. Grid selection feeds shared Action Bar context; runtime counts feed shared bottom status. Provider failures may be represented through the existing Notification capability, but no notification engine exists inside the grid.

Authorization presentation resolves grid and column requirements and fails closed. It never replaces provider/backend authorization. A context generation ensures stale company results are ignored and context changes clear selection/edit state.

Task 10B adds bounded viewport paging; its detailed boundaries are in [Grid viewport provider](GRID-VIEWPORT-PROVIDER.md) and [Grid window cache](GRID-WINDOW-CACHE.md). Explicit non-goals remain persistent layout, copy/paste ranges, fill, formula calculation, import/export, grouping/pivot/chart/report design and application business semantics.
