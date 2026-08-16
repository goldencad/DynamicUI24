# DataEntry import

DataEntry hosts import through `ImportExportWorkspaceHost`; the host only presents source, mapping, bounded preview, diagnostics and actions. It never parses files. Platform file pickers open a safe stream and pass it to `ImportEngine`.

Target choices come from the current resolved Grid columns. Only visible, authorized, editable INPUT `VariableCode` values appear. The action bar uses a dropdown whose items come from registered profiles/providers. Validation precedes Import; Cancel propagates a cancellation token. Mapping controls are keyboard reachable, diagnostics include text severity, focus remains visible, and progress has textual stage/count semantics in System/Light/Dark themes.

Import mutation goes through `IGridBatchRowImportProvider`. Existing cell-edit workflows continue to use `GridEditTransaction`; applications can adapt imported rows into the same batch transaction infrastructure rather than creating a second persistence path.
