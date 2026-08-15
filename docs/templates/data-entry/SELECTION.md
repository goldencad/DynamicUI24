# DataEntry selection

Selection is an immutable set of `RowKey`, never visible row indexes. `NONE` ignores requests, `SINGLE` retains at most one key and `MULTIPLE` retains all distinct keys present in the current row set. Sort/filter reloads intersect the selection with still-visible rows, so stable keys survive reordering.

A company/workspace context change clears selection and the active edit before loading. This prevents a row or action from Company A remaining actionable in Company B.

`SelectionCount` feeds `ActionSelectionContext`; the existing Dynamic Action Bar resolver handles min/max selection rules. `DataEntryGridRuntime.Status` supplies total, visible, selected, error, warning, pending and read-only values to the existing bottom Action Bar status presentation. There is no separate grid status framework.
