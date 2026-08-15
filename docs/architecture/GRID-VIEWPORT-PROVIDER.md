# Grid viewport provider

The optional `IVirtualizedGridDataProvider` capability keeps the 10A small-data path intact. Runtime capability discovery selects `LoadViewportAsync` only when implemented. Company and workspace remain explicit `GridProviderContext`; sort/filter and generation travel in `GridViewportRequest`; cancellation travels as the async method token.

Providers return stable unique RowKeys, a bounded immutable range, and the logical filtered total. They may return fewer rows at the end or an empty valid range. They must not return controls, translated technical values, raw exception text, formula execution, or application business logic. Real database/API integrations translate the request to server paging and translate failures to safe diagnostics.

Runtime validates generation, start, maximum count, logical extent, and duplicate keys before adoption. Initial failures show the normal localized grid error state. A later region failure retains the prior window in memory and exposes one explicit retry path; there is no automatic retry loop.
