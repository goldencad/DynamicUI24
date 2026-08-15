# Grid clipboard

## Ownership and format

Core owns `IGridClipboardService`, `ClipboardMatrix`, and deterministic tabular text. Avalonia alone owns the platform bridge. Columns use TAB, rows use LF, CRLF/CR input is normalized, empty cells and trailing empty columns are retained, and one terminal newline is ignored. There is no CSV quoting or binary workbook payload.

Copy uses current visible column order and current logical row order. Hidden permission-gated columns are absent. Formula/system cells copy their displayed value only; expressions are never exported. Multi-range copy uses the primary range in 10C.

`GridClipboardText` uses stable date/datetime defaults and presentation culture for formattable values. Tabs inside values are replaced with spaces; embedded line breaks remain value text and are not a general quoted-table format.

## Safety

Clipboard denial, absence, empty text, oversized text, unavailable virtual rows, and huge selections return one structured diagnostic. Symbolic select-all never creates a giant string. Applications should surface a single N1 notification for the logical failure, not one message per cell.

## Does not own

File import/export, headers, CSV rules, workbook formats, and application security decisions beyond resolved column visibility belong elsewhere. Task 10D may consume the batch/typed matrix seam for import/export.

Focused tests: `GridEditingTests.ClipboardMatrixPreservesTabsEmptyCellsAndNormalizesRows` and copy tests.
