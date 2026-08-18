# Universal Editor Foundation

## Ownership and boundaries

The foundation owns generic editor metadata, deterministic resolution, candidate state, formatting/parsing coordination, validation coordination, bounded lookup coordination, privacy-aware presentation seams and Avalonia presentation. It does not own business validation, persistence, formulas, queries, workflow, IME engines or navigation execution.

`EditorDefinition != EditorRuntimeState != AvaloniaEditorPresenter`. `EditorCode` identifies reusable policy; `EditorSemanticId` identifies the consumer field. Labels, indexes and controls are never identity.

Value types are application-neutral: string, long string, integer, decimal, currency, percentage, Boolean, date, time, date-time, date range, choice, multi-choice, lookup key, secret and hyperlink. Applications map domain concepts to these types.

## Presentation reuse audit

The restored Actipro Avalonia Pro 25.2 package contains Bars, Docking and Fundamentals but no general numeric/date/mask/lookup editor catalog. Task 10G therefore uses mature native Avalonia input controls. DevExpress WinForms is only a capability benchmark. Core exposes no Actipro, Avalonia, DevExpress or WinForms types.

Native `TextBox` owns Unicode, IME composition, caret, selection, undo and clipboard. Presentation never commits transient composition. Masks validate completed input/commit and never replace native input.

## Lifecycle and adoption

Controls are lazy and bounded. A presenter creates one stable native visual tree and culture changes update it in place. Opening is not dirty; candidate, editor commit, consumer save and provider persistence remain distinct. DataEntry uses `GridEditorDefinitionAdapter` while retaining Task 10F transactions. Report parameter, filter and form target records are future adoption seams only.

Focused tests: `dotnet test tests/DynamicUI24.Tests --filter UniversalEditorFoundationTests` and `dotnet test tests/DynamicUI24.ArchitectureTests --filter UniversalEditorArchitectureTests`.

Common failures: using labels as identity, committing `TextChanged`, enumerating a lookup, trusting cancellation without generation checks, exposing protected text in tooltip/accessibility, or rebuilding a focused editor.
