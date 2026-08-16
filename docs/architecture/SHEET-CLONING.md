# Sheet cloning

Duplicate derives a sheet in the current context; Save As creates a new semantic data context. Both require a new provider-allocated `SheetCode`. `SheetClonePolicy` is immutable and covers structure, formulas, values, layout, filters, sort, permissions/preferences and transient-state resets.

The generic `NewDataContext` safety profile resets RowKeys, edit history, undo/redo and import runtime; applications/providers generate RowKeys and assign any business meaning outside Core. Explicit `SheetReferenceMapping` uses source/target `SheetCode`. DynamicUI24 never rewrites formulas or interprets business-specific context semantics.
