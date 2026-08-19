# Adopting Report Workspace

Create an immutable definition, implement `IReportProvider`, construct `ReportRuntime`, and present it with `ReportWorkspaceHost`. Supply `ReportExecutionContext` from the current company/context. Optional output, Find, and drill-down interfaces add capabilities without changing Core metadata.

Register the Report template with a lightweight lazy factory. Cold startup must not construct the provider, runtime, result Grid, parameter presenters, or heavy controls. The host composes primary result content with a progressively disclosed parameter pane whose controls are existing `AvaloniaEditorPresenter` instances. It reuses `ContentPresentationState`, Dynamic Authorization, P1, shared Operation/status, commands, Navigation, Notification, Context Panel, Search, and Quick Access rather than duplicating them.

Register report-level Run, Parameters, Find, Export, Print, drill-down, and Reset Layout commands with existing S1/action infrastructure. Publish `ReportCode + RowKey + ReportColumnCode` to S2 Context Panel and breadcrumb presentation; never publish cells to global search or persist raw sensitive parameters in Recent/Favorites. Provider/application and document adapters own acquisition and output; the workspace contains no SQL/query language or formula engine.

`Run/Refresh` dispatches the semantic `REPORT.{ReportCode}.RUN_REFRESH` command through the shared action-command registry. It exposes `Loading`, advances generation, acquires exactly one initial provider window and presents a localized completion acknowledgement even when the refreshed values are identical. `Reset` dispatches `REPORT.{ReportCode}.RESET`; it restores definition defaults for parameters, sort, filter and grouping, clears selection/result-window presentation state, and performs exactly one authorized refresh. Reset does not mutate provider/business data.

Use localization keys for all display strings and HelpContextCode for report, parameter, and column help. Validate Vietnamese and other native input physically on each target OS; unit tests only prove Unicode preservation.

## Task 11 acceptance record

User-declared Real-Mac PASS: Report workspace launch; parameter-pane open; observable Run/Refresh; Initial to Ready; bounded 100K logical-result presentation; Reset restoration of pre-sort/default state; shared compact Date/DateRange presentation improvement; continued native/report UI functionality; and configurable action-placement presentation review.

Automated only, not claimed as physical PASS: repeated vi-VN/en-US switching, every System/Light/Dark combination, Dynamic Authorization/P1 denial paths, grouping/aggregate/filter edge cases, stale-generation cancellation, Find preservation, output artifact identity, Export dispatch, Print capability resolution, contextual/overflow/Hidden action variants, accessibility automation properties, and all failure paths.

Application/provider-owned: authoritative acquisition, business filtering/grouping/aggregates, production document serialization, storage/persistence, PDF/DOCX/XLSX/XML generation, and production Print output. The Demo synthetic CSV receipt proves only semantic dispatch and artifact acknowledgement.

Future DocsView24: document-engine selection and actual PDF/Office/XML/image viewing. Task 11 supplies only `ReportOutputArtifact → DocumentViewRequest → IDocumentViewLauncher`; absence of a viewer fails safely and is not a physical viewing PASS.
