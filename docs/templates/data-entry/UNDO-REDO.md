# Grid undo and redo

The runtime stores bounded semantic `GridEditTransaction` history. Each `GridCellChange` contains `RowKey`, `VariableCode`, original value, candidate value, and validation state—never a control reference. Single edit, paste, cut, and clear become one history entry each.

Undo applies inverse changes; redo reapplies candidates. Both use the same batch/provider seam, so offscreen rows work without scrolling. A new recorded edit clears redo. `GridPasteOptions.HistoryDepth` bounds memory; history is cleared on Company/workspace change, deactivation, and failure. It is not persisted across restart.

Provider errors leave the history operation available for retry. A stale completion may have changed backend state, but it cannot update the new presentation; applications reconcile by refresh according to their persistence policy.

Undo does not own database transaction guarantees, cross-workspace history, application commands, or collaborative conflict resolution.

Focused tests cover paste/fill undo-redo, redo invalidation, depth bounds, and virtual semantic changes in `GridEditingTests`.
