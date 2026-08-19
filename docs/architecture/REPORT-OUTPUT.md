# Report Output

`IReportOutputProvider` is the vendor-neutral streaming boundary. Every request states format and scope explicitly: full eligible report, filtered report, selection, visible columns, or all eligible columns. It also carries semantic columns, selected `RowKey`s, generation, and company/context. Supported formats and actions come from `ReportExportCapability`; the UI does not infer support from a file extension.

DynamicUI24 Report Runtime does not own document generation. The application/provider creates authoritative content and returns a `ReportOutputArtifact` that retains the semantic `ReportCode` and format. DynamicUI24 owns command presentation, shared operation-state projection, localized acknowledgement, and the semantic Open/View or Print action.

The future viewing boundary is `ReportOutputArtifact → DocumentViewRequest → IDocumentViewLauncher → DocsView24`. DocsView24 is intended to be a reusable PDF/Office/XML/image viewing core with format engines behind adapters, potentially including DevExpress Document APIs where appropriate. DynamicUI24 does not map formats to engines. When no launcher is registered, View fails safely with `REPORT_DOCUMENT_VIEW_UNAVAILABLE`; no fake preview is shown.

Core does not expose DevExpress. XLSX/PDF/DOCX adapters may use DevExpress in extension assemblies; CSV can be implemented independently. Unsupported formats fail honestly. P1 and eligibility are resolved for every output, and large exports must stream/provider-page rather than materialize the grid cache. Signing is outside this boundary.

Export, Print, Open/Download, and View are separate semantic capabilities and may carry separate Task 10H authorization bindings. P1 remains an independent ceiling: only authorized eligible columns are handed to output providers, and a viewer does not grant access to protected report values. Long work projects through the shared Operation/Notification surfaces. Report adds no output progress, retry, cancellation, notification, SQL/query, formula, Search, Quick Access, Context Panel, PDF, Office, or XML engine; provider/application/document adapters own actual output and business truth.

## Report action placement

`ReportActionDefinition` contributes semantic actions to the existing Top/Bottom Action Bars, contextual toolbar, or shared overflow menu. A definition can also mark an action Hidden. `ActionCode` and `CommandCode` are stable identities; localized labels are presentation only. Contributions are ordered by `Order` and then semantic action code. Primary actions use the shared primary geometry token so responsive presentation can retain them before lower-priority contributions.

Example: Run/Refresh is Top, order 10, primary; Reset is Top, order 20; Export is Bottom, order 10; Print is Bottom, order 20; View Output is Contextual. Actions without an explicit contribution are absent. Hidden contributions are not presented.

Configuration controls presentation. Task 10H authorization controls permission and always wins over placement. A definition or future preference cannot resurrect a hidden/denied action. Placement contains no coordinates, widths, business behavior, or executable code; responsive layout remains presentation-owned. The same metadata projects to Task 10H Developer UI Authoring through `UiElementKind.Command` and existing eligible-surface metadata.
