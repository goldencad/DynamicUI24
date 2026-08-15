# DataEntry change guide

When adding grid metadata, update `GridDefinition` only for reusable declarative behavior. Keep columns in the Task 9 `ColumnDefinition` contract and preserve `VariableCode`. Add a diagnostic and fail-safe fallback for every new malformed state.

When adding providers, keep them async, cancellation-aware and UI-free. Preserve opaque stable `RowKey` values. Never place database clients, business calculations or authorization decisions in the renderer.

When adding an editor, keep candidate state in `GridEditBuffer`; do not mutate the provider before commit. Add generic validator coverage, renderer fallback, localization keys and accessibility text. Business-specific editors belong to application extensions.

When adding actions or status, reuse Dynamic Action Bars. When adding guidance, reuse Notifications. Grid code must not directly reference Tree or Ribbon implementations.

Safe 10B work: add optional viewport request/result metadata, generation/cancellation tests and a windowed provider. Unsafe 10A expansion: persistent layouts, 100K preload, Excel range behavior, formula execution, import/export or business-specific semantics.
