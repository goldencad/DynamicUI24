# DataEntry metadata

`GridDefinition` identifies the grid and owns default sort/filter metadata, `NONE`/`SINGLE`/`MULTIPLE` selection mode, edit/add/delete presentation flags, row-number/status flags, empty-state localization and typed presentation requirements.

Columns are the existing Task 9 `DynamicUI24.Core.Setup.ColumnDefinition`; do not create a second column model. Runtime order is `DisplayOrder`, then `ColumnCode`. Width is clamped to a valid `MinWidth`/`MaxWidth`; malformed ranges fall back safely. `IsVisible` and the existing authorization presentation resolver determine whether a column is rendered. A column permission string is a permission code by default and may use `PERMISSION:` or `CAPABILITY:` prefixes; unavailable privileged columns fail closed.

`INPUT` can be editable only when grid, column, authorization and editor metadata all allow it. `FORMULA` and `SYSTEM` are always runtime read-only. Formula values are supplied by the provider; no expression is evaluated.

`VariableCode` is the stable semantic binding key from column to `GridRow.Values`. Labels are localization keys only. Language, theme, reorder and renamed labels never alter the binding. An unknown/missing value is rendered unavailable and never resolved through the displayed title.

Duplicate `ColumnId`, `ColumnCode` or `VariableCode`, missing labels, unknown data/editor enums, invalid widths and contradictory modes produce `GridDiagnostic` values and exclude or downgrade unsafe presentation rather than crashing the shell.
