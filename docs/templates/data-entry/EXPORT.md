# DataEntry export

Export actions are metadata-driven dropdown items for current view, selected `RowKey` values, all filtered rows and all rows. `ExportDefinition` selects writer and fields; field resolution excludes hidden or unauthorized columns.

The Grid control is not the source of truth. `IGridExportProvider` streams provider rows, including 100K+ logical rows, to `ExportEngine`. The Avalonia save picker only supplies the destination stream. Progress and cancellation use one logical N1 operation rather than a notification per batch.
