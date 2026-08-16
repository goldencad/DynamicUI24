# Adopt an import profile

Create an immutable `ImportDefinition` with an identity, `ParserCode`, extension hints and `ImportFieldMapping` entries. Each mapping targets a workspace `VariableCode`. Configure headers/record paths/fixed-width schema only when relevant to that parser, plus preview limit, null policy, commit mode, mutation mode and optional match keys.

Register built-ins with `BuiltInImportExportProviders.Register(registry)` and XLSX with `ExcelImportExportRegistration.Register(registry)`. Resolve the Grid with current authorization, run `ImportDefinitionValidator`, inspect, auto-map or edit mappings, preview, then commit through the application `IGridBatchRowImportProvider`.

Profiles may be stored by a consumer later; the framework has no profile or history database. Do not put business validation or credentials in profile metadata.
