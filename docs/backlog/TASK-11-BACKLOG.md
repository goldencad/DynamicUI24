# Task 11 Backlog

Current foundation is authoritative: lazy Report construction, Universal Editor parameters, Developer Authoring/Dynamic Authorization, P1, bounded providers, `ContentPresentationState`, shared Operation/commands/Notification/Context Panel/Search/Quick Access, and semantic identities. Report owns presentation coordination only; provider/application owns acquisition and business truth. No SQL/query language or formula engine is planned.

Deferred intentionally:

- Test infrastructure: `DataEntryGridTests.CellContextPreservesContainingRangeAndActivatesOutsideCellWithoutProviderWork` passes 3/3 when run alone in fresh processes and the focused `DataEntryGridTests` band passes 22/22, while the broader DataEntry/Grid process reproducibly raises a duplicate `Avalonia.Controls.MenuItem` registration exception from Avalonia global property-registry initialization. Task 11 does not change the implicated `DataEntryGridHost` menu path. Future investigation should cover test-process isolation, Avalonia App initialization, and global-state teardown; the underlying Avalonia root cause is not yet proven.
- Remaining physical macOS coverage beyond the accepted Task 11 matrix: every vi-VN/en-US and System/Light/Dark combination, complete keyboard/accessibility paths, unavailable output states, and clean-exit permutations. The user has already declared PASS for Report launch, parameter-pane open, observable Run/Refresh and Initial-to-Ready behavior, bounded 100K presentation, Reset restoration, compact shared Date/DateRange improvement, native/report UI functionality, and configurable action placement.
- Production XLSX/PDF/print adapters; the demo reports these capabilities as unavailable until configured.
- Rich visual nested-group headers, subtotal rows, and expand/collapse controls beyond the provider/runtime contracts.
- Report-specific S1 registrations and S2 provider composition in an adopting application; semantic contracts are ready.
- Persistent report preference store adapter over the existing Grid preference infrastructure.
- Task 16 batch/time-filter strengthening and Task 12 document workflow remain unstarted.
