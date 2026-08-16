# Generic import/export engine

## What import/export owns

The engine owns format-provider registration, stream parsing/writing, source schema inspection, `VariableCode` mapping, generic conversion, bounded preview/diagnostics, import session state, commit orchestration, export field resolution, cancellation and progress. The flow is `Stream → parser → source records → mapping → validation → provider`. Export is the inverse provider-driven flow.

## What it does not own

It does not own file pickers, business rules, databases, identity generation, formula execution, OCR, ETL scheduling, cloud connectors or report design. Avalonia chooses streams; application providers authorize and persist data. XLSX is only an adapter in `DynamicUI24.Extensions.Excel`.

## Public contracts

The main contracts are `ImportDefinition`, `ImportFieldMapping`, `ExportDefinition`, `ExportFieldDefinition`, `IImportParserProvider`, `IExportWriterProvider`, `IImportValueConverter`, `IGridBatchRowImportProvider`, `IGridExportProvider`, `ImportEngine`, `ExportEngine` and `ImportSession`.

## Streaming rule

Parsers and data providers expose `IAsyncEnumerable`. Preview retains at most `MaxPreviewRows`; diagnostics retain the first configured N details plus a total. Batched commit never accumulates the complete source. `ALL_ROWS` is enumerated by the provider, never by materializing Grid visuals. Atomic import requires a replayable stream so the engine can validate before mutation.

## Security rules

Definitions, mappings and providers are registered application components. Files cannot load assemblies or scripts. XML prohibits DTD and external resolution. XLSX reads ZIP/XML data without macros, VBA, automation or formula execution. Authorization and visible-column resolution occur before values reach writers. Company/workspace context changes invalidate a session.

## Focused verification

```sh
dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --filter ImportExport
dotnet test tests/DynamicUI24.ArchitectureTests/DynamicUI24.ArchitectureTests.csproj --filter ImportExport
```

Common failures are unknown provider codes, non-editable targets, duplicate targets, malformed content, exceeded safety limits, a non-replayable stream in atomic mode, provider rejection, cancellation and stale context. All produce safe diagnostic codes; internal stack traces are not user-facing.
