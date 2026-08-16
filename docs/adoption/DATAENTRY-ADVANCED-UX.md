# Adopt DataEntry advanced UX

Use the existing `DataEntryGridRuntime` and `DataEntryGridHost`; 10E is an overlay, not a replacement engine. Supply stable `RowKey`, stable `VariableCode`, async provider capabilities, privacy services, and existing Import/Export/Context/Search command seams.

The host exposes resize, reorder, show/hide, left pin/unpin, reset, sort/filter, copy/cut/paste/clear, Fill Down/Right, Undo/Redo, semantic select-all, and header/cell menus. Register Import, Export, Reset Layout, Columns, Clear Filters, and Context Panel under the application's existing registered-command provider so Action Bar, menus, and Cmd+K dispatch the same command.

Keyboard uses Command on macOS and Control on Windows/Linux for clipboard, undo/redo, and select all. Arrows, Tab/Shift+Tab, Enter, Escape, Delete/Backspace and shift-range selection use visible semantic order. Hidden columns are skipped; read-only cells may receive focus but never edit.

Import mapping and export column policy must be explicit and remain `VariableCode`-based. “Visible columns” is an export option, not an implicit redefinition of business data. Context Panel continues to follow `RowKey` and optional `VariableCode`.

Keep provider virtualization for 100K+ logical rows. Layout operations must not reload or enumerate rows. Distinguish dataset empty from filtered empty (`No rows match current filters`) and offer Clear filters; provider errors show a safe message and Retry.

Non-goals include formulas, workbook/pivot/chart/report/query engines, scripting/macros, AI, collaboration, workflow, printing, and business logic.
