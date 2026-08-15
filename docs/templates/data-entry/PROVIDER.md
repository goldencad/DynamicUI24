# DataEntry provider

Implement `IDataEntryGridProvider` outside the renderer. `LoadAsync(GridProviderContext, GridDataRequest, CancellationToken)` returns `LOADING`, `READY`, `EMPTY`, `ERROR` or `UNAVAILABLE` presentation data through `GridDataResult`. `CommitAsync` accepts one `GridCellEdit` candidate after framework validation.

For large logical datasets, optionally implement `IVirtualizedGridDataProvider.LoadViewportAsync`. It returns only the requested overscanned range and logical filtered total. See [VIRTUALIZATION.md](VIRTUALIZATION.md).

`GridProviderContext` carries the current company and workspace. `GridDataRequest` carries ordered sort/filter metadata plus a generation number. Providers must not return Avalonia controls, localize technical codes or expose raw exceptions. Framework code catches failures and presents `GRID_PROVIDER_FAILED` while the shell remains usable.

`GridRow` is a generic immutable/read-focused value map keyed by `VariableCode`; applications are not forced into a generated POCO. `RowKey` is opaque, nonblank, independent of row index and deterministically comparable. It must remain stable across sorting/filtering and unique in the returned row scope.

The runtime rejects a late generation after a newer company/workspace request. Providers should still honor cancellation and avoid expensive work when canceled. Backend authorization remains mandatory; presentation permission checks are not a security boundary.

For 10B, extend the request/result boundary with optional viewport/window information, row count and cancellation-aware window loading. Do not replace `GridDefinition`, column metadata, `RowKey` or `GridRow` value binding.
