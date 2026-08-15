# Integrate a large-data grid

1. Keep the normal `GridDefinition`, Task 9 columns, stable `RowKey`, Action Bars, and notifications.
2. Implement `IVirtualizedGridDataProvider`. Translate materialized start/count, sort, filter, Company, and workspace into one bounded database/API request.
3. Echo `RequestGeneration`, report the logical filtered `TotalRowCount`, and honor cancellation. Never fetch all rows merely to calculate a screen.
4. Construct `DataEntryGridRuntime` with optional `GridViewportOptions`; defaults target a 60-row viewport plus 20/20 overscan and three cached windows.
5. Call `LoadAsync` on activation/Company resolution, `Deactivate` when leaving the workspace, `RefreshAsync` for Refresh, and `RetryViewportAsync` only after a current failure.
6. Connect provider failures to the existing notification/guidance foundation with a deduplicated generic retry notice; do not expose exception messages or emit one notification per cancelled internal request.
7. Test initial/far windows, stale responses, cancellation, Company/workspace changes, RowKey selection, edit candidates, sort/filter totals, refresh, and cache metrics.

The framework does not define SQL, REST parameters, application authorization, business validation, formula calculation, search, import/export, or Excel behavior. Those are explicit non-goals of this seam.
