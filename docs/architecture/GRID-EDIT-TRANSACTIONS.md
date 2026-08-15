# Grid edit transactions

## Contract

`GridEditTransaction` is the unit for single-cell edits, paste, cut, clear, undo, and redo. It carries an ID, timestamp, source action, immutable `GridCellChange` list, validation diagnostics, and commit state. Cell identity is `RowKey + VariableCode`.

The runtime plans and validates the complete target before commit. `IGridBatchEditProvider` is optional and lets application providers apply the immutable list as one logical request. Existing `IDataEntryGridProvider.CommitAsync` remains valid; multi-cell fallback is sequential and therefore reports that it is not provider-atomic.

## Context and virtualization

Transactions do not retain rows or controls. `IGridLogicalRowProvider` resolves a bounded sorted/filtered span by logical position. After async completion the runtime compares generation, Company, and workspace. A late result is diagnosed as stale and never changes the current window/cache. Backend reconciliation remains an application concern.

## Ownership boundary

Grid Core owns generic conversion, metadata validation, transaction/history state, and presentation-safe application. Providers own persistence and stronger database atomicity. Business validation, calculations, formula execution, and domain workflows are not Grid responsibilities.

## Common failures

One command per cell when batch is available, marking a sequential fallback atomic, retaining visual objects, applying before full validation, or accepting a late Company A result into Company B.
