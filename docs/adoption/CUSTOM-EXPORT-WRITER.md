# Custom export writer

Implement `IExportWriterProvider` and register a stable `WriterCode`. Write the supplied async `ExportRecord` sequence to the supplied stream, honor cancellation and return a safe `ExportWriteResult`. Fields are already authorized and ordered.

The writer must not query a database, inspect Grid controls, load arbitrary code or add business semantics. Add an `ExportDefinition` referencing the code; generic UI and `ExportEngine` remain unchanged. Test escaping/formatting, cancellation, writer failure and a large streaming sequence.
