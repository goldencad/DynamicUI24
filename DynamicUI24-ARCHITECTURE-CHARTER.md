# DynamicUI24 Architecture Charter

**Charter version:** 0.2
**Revision basis:** v0.1 + cross-platform interaction, native-input, edit-boundary and AI-governance invariants

**Document type:** Architecture Charter / Project Constitution
**Project:** DynamicUI24
**Status:** Governing architecture principles
**Applies to:** All DynamicUI24 tasks, specifications, implementations, reviews, migrations, AI/Codex work, and consuming TS24 applications.

## 1. Purpose

DynamicUI24 is the reusable cross-platform presentation foundation for TS24 applications. Versioned specifications define task/release requirements; this Charter preserves the architectural intent that must survive across many tasks and new Chat/Codex sessions.

If a task/spec appears to conflict with this Charter, stop and surface the conflict. Do not silently reinterpret either document.

## 2. North Star

DynamicUI24 lets an application describe what users need to see and do through stable metadata, semantic contracts, providers, commands and policies while the framework owns reusable presentation mechanics.

```text
Application semantics
        ↓
Stable metadata / contracts
        ↓
DynamicUI24 runtime
        ↓
Clean cross-platform UI
```

Applications should not repeatedly rebuild shell, navigation, grids, search, privacy presentation, notifications, context panel, multi-sheet composition, generic import/export, document preview plumbing, localization, theme or accessibility.

## 3. Semantics before visuals

Authoritative identity is semantic:

`WorkspaceCode`, `SheetCode`, `VariableCode`, `RowKey`, `CommandCode`, `HelpContextCode`, `PolicyCode`, `ProviderCode`.

Localized labels, tab captions, visual indexes, screen coordinates, theme, font, current order and Avalonia control instances are presentation only.

Renaming, reordering, hiding, translating, theming or rematerializing UI must not break business identity.

## 4. Definition, runtime state and presentation are separate

```text
Definition / Metadata != Runtime State != Rendered UI
```

Published metadata remains stable/read-focused. Runtime state changes frequently. Rendered controls are disposable presentation.

## 5. Application-neutral Core

DynamicUI24 Core must not contain payroll, tax, accounting, social-insurance, customs, Odoo, PayCalc24 or other product-specific business rules.

Applications register metadata/providers/adapters containing application semantics.

## 6. Business and calculation authority remain outside UI

```text
UI != Business Core != Calculation Engine
```

Where TS24 already has an authoritative Calculation Engine, DynamicUI24 must not create a second engine.

For cross-sheet scenarios, DynamicUI24 preserves stable `SheetCode` / `VariableCode`, presents recalculation outcomes and diagnostics, and leaves formula evaluation, dependency resolution, cycle detection and function semantics to the authoritative calculation layer.

## 7. Metadata-driven, not metadata-everything

Use metadata when it creates reusable structure: labels, columns, sheets, actions, menus, permissions, privacy, help, layout, providers.

Do not encode arbitrary executable scripts, opaque imperative workflows or hidden business logic into metadata.

## 8. Provider and adapter boundaries

Replaceable mechanics live behind interfaces/providers/adapters such as search, context, import/export, sheet lifecycle, document processing and platform services.

Core providers return semantic models/results, not arbitrary UI controls.

## 9. Vendor-neutral Core

`DynamicUI24.Core` should not expose DevExpress, Chilkat, Avalonia-control or native-platform types in generic contracts.

Vendor/platform implementations live in extension/adapter projects.

## 10. DevExpress document-processing policy

TS24 owns a valid DevExpress Universal Subscription.

Preferred document-processing implementations:

```text
XLS/XLSX     -> DevExpress Spreadsheet Document API
DOC/DOCX/RTF -> DevExpress Word Processing Document API
PDF          -> approved stable DevExpress PDF API
PPT/PPTX     -> DevExpress Presentation API
```

Use TS24-authorized licensed packages/feed. Never acquire or activate a DevExpress trial. Do not introduce Office COM/Interop.

DevExpress remains behind vendor-neutral document adapters so future upgrades are localized to package configuration, adapters and tests.

## 11. Digital signing is a separate subsystem

Document processing is not digital signing.

TS24 digital signing belongs to a separate TS24 Signing Module. The selected signing technology (including Chilkat where TS24 has standardized on it) is isolated behind signing contracts.

Document adapters do not own private keys, PINs, certificate selection, USB token/YubiHSM/PKCS#11/remote-signing communication, signing authorization, audit or workflow state.

```text
Document Processing
    -> bytes/hash/prepared document
    -> TS24 Signing Module
    -> signature/signed document
    -> optional post-processing/preview
```

DevExpress is not the TS24 signing engine.

## 12. Cross-platform is first-class

Target Windows, macOS ARM64, macOS x64 where published, Linux x64, and server/Docker scenarios where applicable.

Do not call a feature cross-platform merely because it compiles. Representative runtime evidence is required.

## 13. The Shell stays clean

Main work stays visually dominant. Secondary complexity uses progressive disclosure. Search/commands are immediate. Right-side context is optional. Dynamic menus remain contextual. Empty space is preferable to permanent clutter.

The design may learn from modern ChatGPT/Codex clarity without copying product-specific UI.

## 14. One menu/command/navigation system

Reuse shared command registry, `MenuDefinition`, action bars, overflow, shortcuts and navigation services.

One semantic command may surface in toolbar, context menu, command palette, keyboard shortcut or app menu without becoming multiple implementations.

## 15. Search is a navigation layer

Global Search/Command Palette uses semantic providers, respects permission/privacy/company context, activates through shared navigation/command services, and protects against stale async results.

Search does not instantiate application screens directly.

## 16. Quick Access is preference, not authority

Pinned/Favorite/Recent store semantic IDs, never grant permission, never persist raw sensitive labels, and always re-resolve metadata/security at render time.

## 17. Context Panel is secondary context

Context Panel is optional, collapsible, resizable and semantic. It does not directly depend on Grid/Tree controls and is not a second business form engine.

## 18. Privacy is not authorization

```text
Authorization != Privacy Presentation != Capture Protection != DLP
```

Privacy can mask/hide/reveal presentation but cannot grant access. Privacy OFF cannot bypass mandatory policy. Temporary reveal is bounded, revocable, context-bound, and does not automatically allow copy/export.

## 19. Sensitive data must not leak through secondary surfaces

P1 privacy applies to Grid/Form, Search, notifications, Context Panel, tooltips, clipboard, import preview, export, accessibility, diagnostics, tabs/subtitles, formula presentation, duplicate/save-as and inactive caches.

## 20. Fail closed

If permission/privacy resolution is uncertain for protected content, do not expose raw values. Use safe hidden/masked/disabled outcomes.

## 21. Honest capability reporting

Do not pretend a platform feature exists. Supported/partial/unsupported/unknown must be explicit. Safe fallback is required where applicable.

## 22. Virtualization is an invariant

```text
100,000 logical rows != 100,000 visual controls
5 x 100,000 logical rows != 500,000 materialized rows
```

100K is a proof size, not a hard maximum.

Personalization, selection, privacy, context, sheets, sort/filter and search must preserve bounded materialization.

## 23. DataEntry identity

Authoritative DataEntry identity:

```text
RowKey + VariableCode
```

It must survive rematerialization, reorder, hide/show, pin/unpin, localization and theme changes.

## 24. Multi-Sheet identity

Authoritative sheet identity:

```text
WorkspaceCode + SheetCode
```

Tab title/order are presentation. Rename/reorder/hide/show must not break formulas, preferences, search, context or navigation.

## 25. Sheet lifecycle

Lifecycle includes Create, Duplicate, Save As, Rename, Reorder, Hide, Show and Delete.

Duplicate/Save As create a NEW `SheetCode`.

UI does not clone business data by enumerating visual rows; data/provider layers own physical clone mechanics.

## 26. Duplicate vs Save As

Duplicate creates another sheet from the current one. Save As creates a new semantic sheet/data context.

Clone policy explicitly controls structure, formulas, values, layout, filters, sort, preferences, RowKey reset, edit/undo reset and reference mapping.

No mutable runtime state aliasing.

## 27. Cross-sheet formulas use the existing Calculation Engine

The authoritative TS24 Calculation Engine owns deterministic dependency resolution, recalculation propagation, cycle detection, formula evaluation and function semantics.

DynamicUI24 coordinates and presents outcomes only.

Never rewrite formulas using localized titles, tab index or blind string replacement.

## 28. Import/Export identity is semantic

Import/export maps by semantic IDs such as `VariableCode`, not visible column position. Visual personalization must not silently alter data meaning.

Export security is independent of visual masking.

## 29. Preferences overlay metadata

Preferences may store layout, active sheet, panel width, tab order and Quick Access state.

Preferences never mutate published metadata and never override permission/capability.

## 30. Company Context invalidates stale work

Late Company A results must never appear after switching to Company B.

Cancellation is optimization. Generation/context validation is correctness.

This applies to Grid, Search, Context, Preview, Sheets, Privacy and document operations.

## 31. Provider failure is isolated

One provider failure should not crash the Shell. Use safe error states, bounded diagnostics and retry when meaningful.

## 32. Accessibility is correctness

Accessibility semantics are required, but privacy remains enforced. Do not create accessibility nodes for all virtualized data or expose raw protected values through automation.

## 33. Localization/theme do not change semantics

vi-VN/en-US, System/Light/Dark and UI/font scale must not change semantic IDs, calculations, authorization, privacy or business data.

## 34. UI state is not business dirty state

Presentation changes such as theme, privacy toggle, panel width, column reorder or sheet tab reorder must not mark business data dirty.

## 35. Documents are streams and semantic references

Prefer `DocumentReference`, streams, bounded buffers and explicit capabilities. Do not assume every document is a local file path.

## 36. Large documents are bounded

Use lazy/bounded preview, cache, thumbnails, cancellation and stale-result protection. Never render or retain unlimited content merely to open a document.

## 37. No arbitrary execution

No arbitrary scripts, VBA/macros runtime, untrusted reflection/plugin scanning, embedded executable execution or automatic external-link execution.

Extensibility is explicit and registered.

## 38. Upgradeability is deliberate

A vendor upgrade should affect the narrowest possible layer.

```text
DevExpress upgrade -> central package config -> adapter -> focused tests
Signing implementation upgrade -> signing adapter/module -> focused tests
```

Business applications should not be rewritten because a vendor library changed.

## 39. AI is a consumer, not an architecture shortcut

AI/agents may consume stable contracts and APIs. AI must not become authoritative for calculation correctness, permission, signing, basic navigation or core document parsing.

DynamicUI24 remains usable without cloud AI.

## 40. Local-AI maintainability

Subsystem documentation must state what it owns, what it does not own, identity rules, state boundaries, provider contracts, security rules, focused tests and common failure modes.

A future AI agent should not need the entire repo to make a safe local change.

## 41. Versioned specs are task contracts

Do not silently modify the authoritative specification while implementing a feature.

Spec changes have their own baseline, SHA, commit, audit and CI closure.

## 42. Git safety is engineering

Before tasks: verify branch, baseline HEAD, origin/main, spec SHA and clean worktree.

Before commit: focused tests, architecture tests, required smoke, `git diff --check`, spec SHA unchanged.

No force-push, silent amend/rebase/reset or history rewrite.

## 43. CI owns full regression

Local work runs focused build/tests/architecture guards and representative real-platform smoke.

CI owns Ubuntu/Windows/macOS full regression and five-RID publish.

## 44. Real platform evidence matters

Distinguish environment failure, harness failure and product failure. Do not weaken a native smoke gate because an automation host cannot create a native render timer.

## 45. Do not blindly port legacy structure

Legacy .NET Framework code is a source of proven business behavior, not automatically the desired architecture.

Preserve authoritative rules; discard obsolete UI/platform coupling.

## 46. Framework maturity makes apps thinner

A mature consumer app should increasingly provide metadata, providers, commands, business/calculation APIs and app-specific policies while DynamicUI24 supplies reusable UX mechanics.

## 47. Explicit non-goals

DynamicUI24 is not a second calculation engine, Excel clone, Office clone, database, ORM, Odoo replacement, signing engine, token/HSM manager, endpoint DLP, default workflow engine, cloud-only framework or AI-only framework.

## 48. Decision test for new features

Before adding a feature ask:
1. Is it reusable across TS24 apps?
2. Is it presentation responsibility or business responsibility?
3. Is semantic identity preserved?
4. Can existing command/menu/navigation/privacy/context infrastructure be reused?
5. Is bounded behavior preserved?
6. Does it introduce vendor coupling into Core?
7. Does it create a second authoritative engine?
8. Does it weaken privacy/permission?
9. Will upgrades remain localized?

## 49. Governing statement

> Build a clean, metadata-driven, semantic, cross-platform UI foundation that stays small at its core, strong at its boundaries, honest about capabilities, strict about identity/security, and easy to upgrade without rewriting application logic.

> DynamicUI24 coordinates presentation. It does not steal ownership from authoritative business, calculation, signing, storage or security engines.

## 50. Unicode-first and native input

DynamicUI24 text input is Unicode-first and OS-native.

All user-editable text surfaces, including DataEntry editors, forms, setup/configuration fields, report parameters, Search, Find, Filter, metadata editors, comments and future reusable text-entry surfaces, must preserve Unicode text end-to-end.

The user's operating-system input method determines how text is entered.

DynamicUI24 must not implement language-specific keyboard, transliteration or composition engines.

This includes, but is not limited to:
- Vietnamese input methods
- Chinese IME
- Japanese IME
- Korean IME
- Arabic input
- Indic input
- dead-key/accent composition

Language support is achieved through Unicode plus native OS input, not through language-specific input implementations inside DynamicUI24.

## 51. Native editor owns active text interaction

When a native text editor owns keyboard focus, the editor and operating system own the active text-input session.

This includes:
- Unicode input
- IME composition
- pre-edit/candidate state
- dead keys
- caret movement
- text selection
- Backspace/Delete
- native clipboard shortcuts
- native editing shortcuts
- platform-standard navigation inside the editor

Parent Grid, Workspace, Shell or command handlers must not intercept these events in a way that breaks native text editing.

Global commands must yield appropriately while an active editor owns the corresponding interaction.

## 52. Composition state is presentation state

IME composition/pre-edit state is transient presentation state. It is not authoritative business data.

Intermediate composition events must not be treated as completed business input merely because a `TextChanged`-style event occurred.

```text
IME / pre-edit candidate
        ↓
native editor presentation
        ↓
completed editor value
        ↓
semantic edit candidate
        ↓
validation / commit
        ↓
runtime pending state
```

Core contracts should receive ordinary completed Unicode values rather than language-specific composition mechanics.

DynamicUI24 must not normalize, transliterate, strip accents or convert Unicode text into legacy code pages unless an explicit application/provider contract requires a separate transformation.

## 53. Font is presentation, not language semantics

Font selection and Unicode glyph fallback belong to presentation/theme/platform policy.

Core semantic contracts must not depend on a particular font.

Do not hard-code language-specific fonts into Core merely to support text input.

A presentation layer may select appropriate system/application fonts and fallback fonts while preserving the underlying Unicode value unchanged.

Changing font, culture, theme or platform must not change business text.

## 54. Edit commit, workspace save and persistence are distinct

Editing boundaries must be explicit.

```text
CONTROL / CELL EDIT COMMIT
!=
WORKSPACE / GRID SAVE
!=
PROVIDER PERSISTENCE
```

A control-level commit may validate and stage a semantic pending value without immediately persisting the entire business object or dataset.

Workspace-level Save coordinates the application's intended save boundary.

Provider/application layers own authoritative persistence.

Presentation-only interaction must not mark business data dirty.

Opening an editor, moving a caret, selecting text, opening expanded view, changing focus or cancelling an unchanged edit must not itself persist business data.

Subsystems may define different save policies, but they must not silently collapse these boundaries where doing so changes application semantics.

## 55. Active interaction owns a stable presentation session

Rendered controls are disposable presentation, but an active physical interaction must have a stable presentation session for its lifetime.

Do not unnecessarily destroy/rebuild the visual presenter that currently owns:
- pointer capture
- drag/range selection
- native text editing
- IME composition
- menu interaction
- focus/caret
- another stateful native gesture

Prefer updating existing materialized presentation when semantic state changes during an active interaction.

After the interaction completes, normal rematerialization/rebuild policy may resume.

Semantic state remains authoritative; visual-tree stability is an interaction correctness requirement, not a new business-state model.

## 56. Deferred UI actions retain semantic identity

Callbacks, deferred handlers, menus, asynchronous continuations and physical gestures must retain or re-resolve semantic identity.

Do not treat captured visual indexes, control positions or localized labels as durable identity.

Examples:

```text
Column action    -> VariableCode or another semantic ColumnCode
Row action       -> RowKey
Sheet action     -> SheetCode
Workspace action -> WorkspaceCode
```

A visual index may be used transiently for rendering/hit testing, but semantic identity must be resolved before authoritative state mutation.

Reorder, virtualization and rematerialization must not cause a deferred action to operate on the wrong semantic object.

## 57. Honest interaction fallback

Cross-platform consistency means consistent capability and semantics, not forcing an unreliable physical gesture onto every platform.

If a gesture or native interaction cannot be made sufficiently reliable, discoverable and maintainable across supported platforms, DynamicUI24 may use a deterministic alternative interaction.

The fallback must:
- preserve semantic behavior
- remain discoverable
- report capability honestly
- avoid misleading affordances
- preserve accessibility
- preserve keyboard access where applicable

If physical drag resizing is unsupported, for example, do not leave resize cursors, invisible drag regions, grips or splitters that imply support.

A reliable explicit command/menu/preset interaction is preferable to a nominally supported but unstable gesture.

## 58. Automated tests do not prove physical UX

Automated tests can prove contracts, state transitions, boundedness, architecture and many event paths.

They do not by themselves prove physical user experience involving:
- pointer gestures
- native focus
- caret behavior
- IME/composition
- popup placement
- native menus
- platform-specific keyboard routing
- perceived interaction continuity

```text
AUTOMATED PASS != PHYSICAL PASS
```

Automation may establish:

```text
READY FOR PHYSICAL ACCEPTANCE
```

Only appropriate real-platform evidence may establish physical acceptance where the task requires it.

Do not weaken a physical acceptance requirement merely because an automation harness cannot synthesize the interaction reliably.

## 59. AI-assisted implementation preserves task-level coherence

AI/Codex work must optimize for coherent subsystem completion, not a sequence of uncoordinated local patches.

Before implementation, gather the known task requirements into one coherent task packet where practical:
- architecture boundaries
- semantic identities
- required UX
- provider ownership
- privacy/security
- performance/boundedness
- cross-platform behavior
- tests
- physical acceptance
- Git/CI closure

During a task, collect non-blocking findings and address related issues as a coherent correction set rather than repeatedly redesigning the same subsystem.

Interrupt immediately when necessary for P0 defects such as:
- crash
- data corruption
- privacy/security leak
- architectural boundary violation
- blocker preventing meaningful continuation

Once a task has passed acceptance, been committed, pushed and closed through its required CI gate, substantial later issues should normally be handled through a separate maintenance task rather than silently reopening historical feature scope.

AI may propose implementation.
AI may verify automated evidence.
AI must not claim physical acceptance that has not been physically established.

**End of DynamicUI24 Architecture Charter v0.2**
