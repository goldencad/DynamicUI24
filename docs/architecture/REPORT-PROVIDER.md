# Report Provider

`IReportProvider` receives semantic `ReportRequest`: report identity, typed parameter snapshot, semantic sorts/filters/groups, requested columns, a bounded window, generation, and company/context. It owns acquisition and the mapping to SQL, REST, files, or any other application technology; none of those technologies cross into Core.

Rows use stable `RowKey` plus `ReportColumnCode` values. Providers should supply logical/filtered counts and aggregates when economical. They must return only the requested bounded window and echo the report generation. The runtime rejects stale generations independently of cancellation.

`IReportFindProvider` searches the provider's logical result, never rendered controls. It maps back through the existing Grid Find contract. `IReportDrillDownProvider` resolves semantic context; a separate dispatcher navigates. Permission, capability, P1, and generation must be resolved before dispatch.

Do not return raw exceptions, SQL text, database column names as identity, UI objects, or sensitive diagnostic values.

Provider/runtime construction is lazy on first actual Report activation. Execution projects generic UI state through `ContentPresentationState` and long-running status through the shared Operation seam. Dynamic Authorization and P1 repair requested columns and capabilities before provider/output dispatch. Providers own global sort/filter/group knowledge and business aggregates; the UI never materializes the 100K logical proof or creates a SQL/query/formula engine.
