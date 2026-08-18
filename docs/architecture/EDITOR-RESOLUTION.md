# Editor Resolution

There is one `EditorResolver`. It receives an `EditorDefinition`, platform capabilities and the existing authorization context, then returns an `EditorResolution` with kind, support status and final interaction state (`Hidden`, `Disabled`, `ReadOnly`, `Editable`). Unknown authorization fails closed.

Defaults map semantic value types to canonical editor kinds. Explicit overrides are accepted only when compatible. Unsupported platform capability and incompatible overrides return diagnostics rather than constructing a misleading control. MultiChoice and DateRange are basic; TreeLookup is an honest deferred presentation seam.

Formatting is culture-aware presentation. Parsing returns a typed candidate or diagnostic without throwing for ordinary invalid input. Percentage storage is explicitly `Fraction` or `WholeNumber`; `EditorDefinition.Increment` independently controls numeric stepping and is never inferred from display text. Currency formatting never changes numeric meaning. Theme, density, font and culture do not change identity or typed values.
