# Adopting desktop-style grid editing

1. Keep the existing `GridDefinition`, `ColumnDefinition`, stable `RowKey`, and `IDataEntryGridProvider`.
2. Configure `GridPasteOptions`; use conservative `Atomic` unless partial application is an explicit product decision.
3. Implement `IGridBatchEditProvider` for logical multi-cell commits. Implement `IGridLogicalRowProvider` when paste/copy must cross unloaded virtual windows.
4. Host the runtime in `DataEntryGridHost`; it supplies native clipboard and Ctrl/Cmd mappings.
5. Route Copy, Cut, Paste, Undo, Redo, and Clear through the existing Dynamic Action Bar. Use `ActionAvailability` for selection/history-aware enabled state.
6. Convert a failed `GridPasteResult` into one deduplicated N1 guidance notification. Show counts/status, not one toast per cell.
7. On provider/backend stale completion, refresh/reconcile in the current Company context; never replay presentation state from the old context.

Cell identity is always `RowKey + VariableCode`. A range adds logical positions only to describe the current sorted/filtered rectangle. Do not materialize one object per selected cell.

The 10C implementation does not provide file import/export, formula execution, fill handles/series, grouping, layout persistence, collaborative editing, or application business rules. Task 10D can build import/export on the typed transaction/provider seam.

Focused validation:

```sh
dotnet build samples/DynamicUI24.Demo/DynamicUI24.Demo.csproj --no-restore -m:1
dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --no-restore --filter 'FullyQualifiedName~GridEditingTests|FullyQualifiedName~DataEntryGridTests|FullyQualifiedName~GridViewportTests' -m:1
dotnet test tests/DynamicUI24.ArchitectureTests/DynamicUI24.ArchitectureTests.csproj --no-restore --filter FullyQualifiedName~DataEntryGridArchitectureTests -m:1
```

Common failure modes: unstable row identities, hidden-column leakage, culture-dependent structural parsing, silent truncation, sequential commits labeled atomic, history retaining controls, or range expansion proportional to total dataset size.
