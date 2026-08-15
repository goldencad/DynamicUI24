# Adopt the DataEntry Grid

1. Create a `GridDefinition` and pass existing published Task 9 `ColumnDefinition` values.
2. Use stable technical `VariableCode` values for every row binding; keep labels as localization keys.
3. Implement `IDataEntryGridProvider` with stable unique `RowKey`, async cancellation, current/visible counts and safe provider states.
4. Compose `DataEntryGridRuntime` and `DataEntryGridHost`, then register the host with the existing `DATA_ENTRY` workspace factory.
5. On company authorization snapshots, call `LoadAsync` with a new `GridProviderContext`; do not cache old selection/edit actions.
6. Feed `runtime.SelectionCount` and `runtime.Status` into the existing Action Bar resolution context. Register executable custom actions in `ActionCommandRegistry`.
7. Use the shared localization, theme, appearance and Notification services. Do not add a grid-specific preference or notification engine.

Before release, test malformed metadata, permission hiding, provider errors, duplicate rows, stale company responses, editing, sort/filter selection, both cultures, all themes and clean application exit. Backend authorization is still required.
