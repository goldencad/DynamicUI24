# Import parser providers

## Format adapter boundary

`IImportParserProvider` recognizes one `ParserCode`, contributes extension hints, inspects a `Stream`, and incrementally returns `ImportSourceRecord`. A parser knows source syntax only. It must not know business meaning, `GridEditTransaction`, Avalonia storage, permissions or persistence.

Built-ins prove CSV, TSV, JSON, XML and fixed-width. `DynamicUI24.Extensions.Excel` supplies XLSX. Applications register custom providers such as `CUSTOM_DEMO`; no core or UI switch changes are required.

Source fields use semantic codes and optional paths/ordinals. JSON supports a simple dot-separated record path and nested property paths. XML uses a limited record element name and field/attribute names; DTD processing is prohibited. CSV/TSV handles quoting, escaped quotes, empty/trailing fields and CRLF/LF without `Split`. Fixed-width uses zero-based `Start` and `Length` metadata.

Providers must honor cancellation and `ImportSafetyLimits`. They should avoid complete scans during inspection and return only bounded samples. A malformed record throws to the engine, which converts it to a safe diagnostic.
