# Paste policy

## Shape rules

Paste starts at the active cell or targets the primary selected rectangle:

- 1×1 into one cell;
- N×M from one active cell, growing right/down without truncation;
- N×M into a matching selection;
- 1×1 fills a rectangle;
- N×M tiles only when both selected dimensions are exact multiples.

Incompatible or out-of-grid shapes are rejected. Mapping is positional in current visible column order, never by headers.

## Conversion and validation

Core conversion supports text, multiline text, integer, decimal, boolean (`true/false/1/0`), date, datetime, choice, and basic reference text. Current culture is used for parsing while structural separators remain invariant. Required metadata is checked after conversion.

A target must be a visible, authorized INPUT column with an editable non-read-only editor. Formula/system/hidden cells fail closed. `PasteCommitMode.Atomic` is the conservative default: any invalid target rejects the whole planned transaction. `PartialValid` applies valid cells and reports the exact rejected count. No subset is silent.

## Virtualization and safety

`IGridLogicalRowProvider` optionally resolves only the target logical span without moving or materializing the UI viewport. `IGridBatchEditProvider` optionally commits a `GridEditTransaction` as one logical request. Presentation generation and Company/workspace identity are checked after async commits; late results never mutate the new context.

Targets beyond `GridPasteOptions.LargeTargetThreshold` require explicit confirmation. Clipboard text also has a configurable character ceiling. Complexity is proportional to target cells, never total rows.

If an editor is active, Avalonia cancels its uncommitted candidate before grid paste. This prevents two candidate states. Provider/backend atomicity is reported accurately; the legacy per-cell provider path is atomic only for a single cell.

## Non-goals and failure modes

No header mapping, CSV/file import, formulas, business validation, arbitrary truncation, or full-dataset scan. Common mistakes are skipping pre-validation, targeting hidden columns, losing empty cells, or updating UI after a stale batch response.
