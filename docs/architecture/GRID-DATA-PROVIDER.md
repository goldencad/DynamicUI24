# Grid data-provider boundary

`IDataEntryGridProvider` is the only data mutation/loading boundary in Grid Core. It accepts generic `GridProviderContext`, `GridDataRequest` and `GridCellEdit`; it returns immutable/read-focused `GridDataResult`, `GridRow` and `GridCommitResult`. No contract references UI controls.

Each row has an opaque stable `RowKey` and values keyed by Task 9 `VariableCode`. Total and visible counts are distinct. States explicitly distinguish loading, ready, empty, error and unavailable. Raw exceptions are caught at the runtime boundary.

Sort/filter definitions are request metadata, not a committed server-query architecture. The deterministic Demo virtual provider proves a 100,000-row logical contract without pre-creating those rows. Applications may translate the request to their own data layer outside DynamicUI24.

The 10B seam is the optional `IVirtualizedGridDataProvider` with `GridViewportRequest`/`GridViewportResult`, cancellation and logical-row-count semantics. Generation protects late responses. This supports windowed loading without changing column metadata, row identity, selection or edit-buffer contracts.
