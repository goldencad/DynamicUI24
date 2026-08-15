# DataEntry editing

Task 10A supports one active cell. `BeginEdit` copies the source value into `GridEditBuffer`; `SetCandidate` changes only the candidate. `CommitEditAsync` validates and then calls the provider. A successful commit updates the visible row; `CancelEdit` discards the buffer without mutating the provider.

Generic validation covers required values and integer, decimal, boolean, date and datetime compatibility. Unknown/specialized types use a safe text fallback. Choice validity can be added when the column metadata contract carries choices. Business validation belongs in the application/provider.

Editing requires: grid `AllowEdit`, enabled grid presentation, enabled/visible column presentation, `INPUT` mode and an editable editor kind. `FORMULA`, `SYSTEM`, `ReadOnly` and `Formula` editors reject begin-edit. Authorization re-resolution removes an edit that is no longer permitted; safe theme/language/scale changes rebuild presentation around the same runtime candidate.

Keyboard basics are delegated through the host: arrows change the selected row, Enter begins/confirms an eligible edit, Escape cancels, and TextBox Tab/Shift+Tab behavior remains native. Multi-cell ranges, paste, fill, batch edits and formula recalculation are non-goals.
