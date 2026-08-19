# Report Runtime

## Ownership and boundaries

Report Runtime owns immutable semantic metadata, parameter candidates and executed snapshots, provider coordination, generation safety, read-only result presentation, sort/filter/group state, presentation totals, drill-down/output coordination, and integration-ready semantic context. It does not own business report definitions, authoritative business aggregates/calculation, SQL, storage schemas, application formulas, workflow, signing, document storage, or a BI/query designer.

`ReportCode`, `ReportColumnCode`, `ReportParameterCode`, `RowKey`, and `ReportAggregateCode` are stable non-localized identities. Titles, headers, order, visible indexes, and controls are presentation only. `ReportDefinition`, `ReportRuntime`, and `ReportWorkspaceHost` are deliberately separate.

Parameters reuse `EditorDefinition`, `EditorResolver`, validation/formatting/parsing, and `AvaloniaEditorPresenter`; native editors own Unicode/IME interaction. Report definitions project into the existing Developer Authoring `UiElementKind.Report`, `ReportParameter`, and `ReportColumn` seams. Dynamic Authorization supplies Hidden/Disabled/ReadOnly/Enabled and fails closed; preferences cannot resurrect denial. Runtime presentation uses `ContentPresentationState`, and long execution may project through `OperationCoordinator`. None of these foundations is duplicated.

The runtime adapts report windows to the existing `DataEntryGridRuntime`. This reuses its bounded cache, viewport, semantic selection, personalization, clipboard protection, and the one Grid Find engine. Report grids are read-only: generic insert/delete and editing are unavailable.

Every run increments a generation. Parameter, sort, filter, group, report, company, or context changes invalidate older work. Cancellation saves resources; generation comparison provides correctness. Company changes clear grid windows, selection, aggregates, and pending context.

P1 policy remains authoritative for cell presentation, clipboard, Find eligibility, grouping, accessibility, drill-down, and output. Sensitive columns cannot be grouped by default and are excluded from shared Grid Find. Aggregate visibility is provider/application policy and is never inferred from source visibility.

Core is Avalonia-free and vendor-neutral. DevExpress may appear only behind output adapters. Generic Count/Sum/Min/Max/Average are report summaries, never TS24 formula evaluation. No SQL/query or formula engine exists here.

Report registration and pane content are lazy: cold startup constructs no Report provider/runtime, result Grid, parameter presenter, or heavy workspace controls. Existing command/navigation, P1, Notification, Context Panel, Search, and Quick Access systems remain authoritative.

Run and Reset are semantic shared-registry commands, not direct button-only handlers. Run provides an observable `Ready → Loading → Ready` lifecycle and completion status. Reset owns definition-default parameter/sort/filter/group restoration plus selection and result-window invalidation, then issues one fresh provider request; it never changes authoritative provider/business data. Both commands resolve current Dynamic Authorization before mutation or acquisition.

Unicode strings are preserved without normalization or transliteration. Avalonia `TextBox` owns native IME composition, candidates, caret, selection, dead keys, and editing shortcuts. Physical IME acceptance still requires the real-Mac checklist.

## Scale and failure modes

Logical results of 100K+ rows are requested through bounded provider windows; cache limits come from `GridViewportOptions`. Totals should be provider-supplied. Export uses an output provider and never walks the UI cache. Common failures are duplicate semantic IDs, stale generations, malformed windows, invalid typed filters, restricted grouping, unsupported output adapters, or storing raw parameters in preferences; all fail with safe diagnostic codes rather than raw exceptions.

Focused tests: `dotnet test tests/DynamicUI24.Tests --filter ReportRuntimeTests` and `dotnet test tests/DynamicUI24.ArchitectureTests --filter ReportArchitectureTests`.
