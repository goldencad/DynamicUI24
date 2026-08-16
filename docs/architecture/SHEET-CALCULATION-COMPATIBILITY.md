# Sheet calculation compatibility

`ISheetCalculationCompatibility` is a vendor-neutral coordination seam for clone validation, delete dependency validation and recalculation requests. The external TS24 calculation layer remains authoritative for parsing, evaluation, dependency graphs, cross-sheet propagation, cycles and function semantics.

Results contain safe diagnostic codes and affected `SheetCode` values. The host treats cycle/failure diagnostics as bounded data, never recurses or repairs semantics, and exposes no restricted raw values. Determinism cannot depend on tab order, localized text, hidden state or materialization order.
