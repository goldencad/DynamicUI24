# Import session

`ImportSession` is a bounded state machine: `SELECT_SOURCE → INSPECT → MAP → PREVIEW → VALIDATE → READY → COMMITTING → COMPLETED`, with terminal `FAILED`, `CANCELLED` and `INVALIDATED` states.

The session captures Company, workspace and generation. A Company switch or navigation invalidates pending work; stale async results are ignored and commits fail closed. Preview is a dry run and never calls mutation providers.

`ATOMIC` validates a replayable stream completely before the first provider call. `PARTIAL_VALID` excludes invalid rows. `BATCHED` commits valid rows in configured bounded batches. Insert/update/upsert and match keys are metadata; the application provider owns actual matching and identity semantics. Completion summarizes total, imported, skipped, invalid, warnings, elapsed time and bounded diagnostics.
