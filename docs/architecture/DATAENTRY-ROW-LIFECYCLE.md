# DataEntry row lifecycle

DataEntry coordinates insert-before, insert-after, single-row delete, and selected-row delete through the optional vendor-neutral `IGridRowLifecycleProvider`. The provider owns physical mutation, validation, and allocation of every new `RowKey`; the UI never treats a visual row index as durable identity.

Eligibility is fail-closed and requires both Grid metadata and provider capability. UI commands await the provider result. Insert results include the new `RowKey` and may include its logical position so a virtualized runtime can perform a bounded far-jump, refresh the affected window, and activate the inserted row. Provider rejection is surfaced without exposing exception details. Deleting rows removes their semantic selection, active edit, pending cell changes, and edit history before refreshing the bounded viewport and selecting a deterministic nearby row.

Row changes may notify `IGridRowCalculationInvalidation`; calculation and formula semantics remain outside DynamicUI24. Columns remain published metadata: runtime insert/delete-column operations are not provided.

Each materialized Avalonia row exposes one compact leading `⌄` action menu. It targets only its semantic `RowKey` and reuses the existing expanded editor/viewer, lifecycle provider, privacy-aware clipboard serialization, semantic clear transaction, Grid Find engine, and percentage row-height preference. No affordance is allocated for non-materialized logical rows.

The permanent Row Header is framework presentation infrastructure, not a metadata column. Its 1-based ordinal is derived from the current bounded viewport position and may change after query operations; identity remains `RowKey`. The Row Header and data use separate presentation panes with synchronized vertical offsets, keeping the leading header fixed during horizontal data scrolling. It is excluded from `VariableCode`, Find columns, clipboard data, validation, formulas, sorting/filtering, and column preferences.
