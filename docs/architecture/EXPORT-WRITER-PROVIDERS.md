# Export writer providers

`IExportWriterProvider` receives an output `Stream`, an already-resolved field list and an async record sequence. It formats syntax; it does not query application data or decide authorization.

Built-ins prove CSV, TSV, JSON, XML and fixed-width. XLSX lives in the Excel extension. A custom writer registers by `WriterCode`. CSV quoting and XML name encoding occur in adapters; no formula or macro content is executed.

`IGridExportProvider` is the application boundary for `CURRENT_VIEW`, `SELECTED_ROWS`, `ALL_FILTERED` and `ALL_ROWS`. Selected records are resolved from stable `RowKey` values. Large export streams logical rows directly; Grid visuals are never the data source. `ExportEngine` removes hidden or unauthorized fields before invoking a writer and reports progress per logical operation.
