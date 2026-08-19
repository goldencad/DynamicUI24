# Report Grouping and Aggregates

`ReportGroupDescriptor` identifies columns semantically and supports ordered nesting without OLAP/cube semantics. Provider group keys—not localized labels—are durable group identity. Restricted sensitive columns are not automatically eligible to group because headers can reveal values.

`ReportAggregateDefinition` supports Count, Sum, Min, Max, and Average at report or group scope. Provider results are preferred and the UI must never fetch all logical rows to calculate a total. These summaries are presentation semantics, not application formula evaluation or a replacement for the TS24 Calculation Engine.

Collapse state and group layout are presentation preferences over immutable metadata. Removed, hidden, or unauthorized group columns are dropped safely.

Grouping, sort, and filter descriptors use `ReportColumnCode`, are repaired after Dynamic Authorization/P1 changes, and are interpreted by the provider. They are not SQL or a query language. Runtime may coordinate permitted generic presentation aggregates, while provider/application remains authoritative for business aggregate truth. Large-result operations never enumerate all logical rows in the UI.
