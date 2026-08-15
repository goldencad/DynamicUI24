# Grid window cache

`GridWindowCache` is a small Core-internal LRU keyed by visible start and requested visible count. Default capacity is three windows. Each entry is already constrained by `GridViewportOptions.MaximumMaterializedRows` (300 by default), so retained cache rows have a deterministic structural bound independent of `TotalRowCount`.

An access promotes a window. Adding beyond capacity removes the least recently used window. Commit updates matching cached RowKeys. Sort, filter, refresh, Company change, and workspace deactivation clear all entries because their index mapping or context is obsolete.

The cache intentionally does not key by generation: generations protect asynchronous adoption, while a cache exists only inside the currently valid sort/filter/context epoch. Every epoch-changing operation clears it first. Metrics `CachedWindowCount` and `CachedRowCount` support tests and diagnostics without using unstable process-memory thresholds.
