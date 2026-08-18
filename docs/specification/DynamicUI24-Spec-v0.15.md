# DynamicUI24 Specification v0.15 — Modern Workspace Interaction Foundation

**Document type:** Versioned DynamicUI24 specification amendment
**Target authoritative version:** v0.15
**Revision basis:** DynamicUI24 Spec v0.14 + Modern Workspace Interaction Foundation
**Architecture authority:** `DynamicUI24-ARCHITECTURE-CHARTER.md` v0.2
**Charter SHA-256:** `415d53271b6681cdd9d617e4ab751e7316e03816f736df97b5425c37620420cc`
**Previous authoritative specification:** `docs/specification/DynamicUI24-Spec-v0.14.md`
**Previous v0.14 SHA-256:** `4106e60c77a769a96a95055b0fab51c289212e945a9790b481e3cc7acc2cb199`
**Baseline after Task 10G:** `05813c63c8b77acd2402fdc5285e2b690908f2ea`

> v0.15 is additive. All requirements and invariants of v0.14 remain authoritative unless this document explicitly extends them.
>
> v0.15 adds the modern interaction primitives required for DynamicUI24 to provide the functional breadth of a mature desktop framework while presenting a calm, minimal, context-driven runtime UX inspired by modern AI workspaces.
>
> v0.15 MUST NOT duplicate Global Search, Quick Access, Notification Center, Context Panel, semantic command infrastructure, Ribbon/Bars, navigation, or existing workspace foundations already defined by earlier specifications.

---

## 1. Purpose

DynamicUI24 should provide enough reusable interaction primitives to replace legacy WinForms application UI without recreating the visual density, toolbar clutter or control-by-control coupling of traditional desktop frameworks.

The design goal is:

```text
Mature desktop capability breadth
        +
Actipro Avalonia presentation maturity
        +
DevExpress-inspired ergonomics where useful
        +
modern minimal workspace interaction
        =
DynamicUI24
```

The runtime experience should expose only the content, context and actions relevant to the user's current task.

---

## 2. Governing interaction principle

DynamicUI24 follows:

```text
Rich capability underneath
        ↓
Semantic definitions
        ↓
Authorization / policy
        ↓
Contextual presentation
        ↓
Progressive disclosure
        ↓
Calm runtime UI
```

Functional completeness must not require permanent visual clutter.

---

## 3. Existing capabilities that MUST be reused

v0.15 does not create replacements for capabilities already owned by DynamicUI24.

### 3.1 Global Search / Command Palette

Global Search is already a first-class Shell primitive.

v0.15 must reuse it for command discovery, navigation, semantic entity/result providers, workspace activation, and recent/pinned activation where applicable.

Do not create a second global-search engine.

### 3.2 Quick Access / Recent / Pin / Favorite

Existing semantic Quick Access / recent / pinned infrastructure remains authoritative.

v0.15 may add modern presentation but not a parallel persistence or identity model.

### 3.3 Notification Center / Toast / Banner

The existing notification identity/coordinator model remains authoritative.

v0.15 may standardize lightweight transient presentation and operation-related notifications, but must not create another notification engine.

### 3.4 Context Panel / Inspector

Existing Context Panel and contextual semantic resolution remain authoritative.

v0.15 may add new contextual content types, but not a second inspector framework.

### 3.5 Semantic command infrastructure

Existing semantic commands and command registries remain authoritative.

A single command may surface through Ribbon/Bars, menus, Action Bars, command palette, keyboard shortcut, inline action, hover action, or contextual toolbar.

Do not implement the same command separately for each presentation.

---

## 4. Scope of v0.15

v0.15 introduces or formalizes these modern interaction primitives:

1. Workspace Pane System.
2. Contextual / Inline Action Presentation.
3. Long-running Operation and Activity UX.
4. Standard operation states and retry/cancel semantics.
5. Resource Chips / Semantic Attachments.
6. Drag-and-Drop Foundation.
7. Review / Diff / Compare Surface.
8. Lightweight Composer Foundation.
9. Selection-driven contextual toolbar.
10. Modern empty/loading/error/offline presentation conventions.
11. Split / secondary content presentation seam.
12. Activity / change-history presentation seam.

These capabilities are presentation/orchestration foundations.

They do not become business engines.

---

## 5. Workspace Pane System

A modern DynamicUI24 workspace may consist of semantic pane roles:

```text
Workspace
├── PrimaryContent
├── LeftNavigation
├── RightContext
├── SecondaryContent
└── BottomActivity
```

Not every workspace uses every pane.

Pane roles are semantic; current screen coordinates are presentation.

---

## 6. Pane behavior

A pane may support visible/hidden, collapsible, resizable, min/max size, remembered size where allowed, contextual activation, responsive collapse, full-height/full-width presentation, and overlay presentation on constrained widths.

User preference may remember eligible pane state.

Authorization and application policy remain the ceiling.

---

## 7. Pane identity

Use stable semantic identity:

```text
WorkspaceCode + PaneCode
```

Do not persist current control instance, raw pixel coordinates as identity, localized pane title, or visual child index.

---

## 8. Pane persistence

User preference may store safe presentation state such as collapsed/expanded, width/height, selected secondary tab, and splitter ratio.

Preferences must be repaired when definitions change.

Unauthorized panes must not be resurrected by saved preference.

---

## 9. Actipro reuse for panes

At the Avalonia presentation layer, evaluate mature Actipro components before custom implementation.

Potentially reusable capabilities include AdvancedTabControl, Docking/MDI primitives where a workspace truly requires docking, Settings/Fundamentals presentation primitives, InfoBar, and prompt primitives.

DynamicUI24 Core remains Actipro-free.

Do not build a heavy docking environment for ordinary business workspaces when simple semantic panes are sufficient.

---

## 10. Split / secondary content seam

A workspace may expose a secondary content pane for side-by-side tasks such as record + preview, document + metadata, list + details, before + after, or report + drill-down result.

The split system is presentation only.

Business selection/state stays in semantic workspace runtime.

---

## 11. Contextual actions

DynamicUI24 should support actions that appear near the object currently being used rather than forcing all commands into Ribbon or menus.

Presentation options may include inline trailing action, hover/focus action, selected-item action bar, contextual mini-toolbar, or compact overflow menu.

These surfaces reuse existing semantic commands.

---

## 12. Inline action rule

Inline actions must remain sparse.

Use them for common immediate actions, reversible local actions, navigation/open, copy, edit, or approve/reject when contextually clear.

Rare or destructive actions belong in overflow/menu/confirmation paths.

---

## 13. Selection-driven contextual toolbar

When the user selects one or more semantic items, DynamicUI24 may present a compact contextual toolbar.

Example:

```text
Selected: 3 documents
[Open] [Download] [Move] [More ⌄]
```

The toolbar resolves commands from current semantic selection, respects authorization, disappears when context is invalid, and must not become a second Action Bar engine.

---

## 14. Command presentation independence

A semantic command definition must remain independent from its presentation.

Conceptually:

```text
CommandCode
        ↓
authorization/capability
        ↓
presentation resolver
        ├── Ribbon
        ├── Menu
        ├── Command Palette
        ├── Inline
        ├── Contextual Toolbar
        └── Shortcut
```

No duplicate business action implementations.

---

## 15. Long-running Operation Foundation

DynamicUI24 needs a vendor-neutral UI model for long-running application operations such as upload, download, import/export, report generation, signing workflow invocation, synchronization, large provider operation, and AI/application task execution.

DynamicUI24 coordinates presentation only.

The application/provider owns actual work.

---

## 16. Operation identity

Use stable semantic operation identity:

```text
OperationId
OperationKind
SourceFeatureCode
WorkspaceCode?
TargetSemanticId?
```

Operation identity must not depend on current toast/control instance.

---

## 17. Canonical operation states

The generic operation state model includes:

```text
Pending
Running
Succeeded
Failed
Cancelled
NeedsAttention
```

Optional finer progress states may be exposed by providers.

---

## 18. Operation progress

Operation presentation may show title, safe description, indeterminate progress, determinate progress, elapsed state, current safe step, result link, cancel, retry, dismiss, or open details.

Do not fabricate progress values when provider cannot supply meaningful progress.

---

## 19. Cancel semantics

Cancel is a requested capability, not a guarantee.

If operation/provider supports cancellation, DynamicUI24 may expose Cancel.

If cancellation is unsupported, do not show a misleading Cancel action.

Cancellation does not imply rollback unless provider explicitly guarantees it.

---

## 20. Retry semantics

Retry re-invokes a semantic operation through the application/provider contract.

DynamicUI24 must not assume retry is idempotent.

Provider/application policy determines whether Retry is offered.

---

## 21. Operation Activity presentation

Long-running operations may appear through lightweight toast, Notification Center, BottomActivity pane, workspace inline status, or contextual status card.

One `OperationId` may have multiple presentations without becoming multiple operations.

---

## 22. Background completion

When the user navigates away, an operation may continue if the application/provider owns a background lifetime.

UI state must reattach to the semantic `OperationId` when revisited.

Do not bind operation truth to a disposed workspace control.

---

## 23. Operation privacy

Operation presentation must not leak P1 content through title, progress text, filenames, diagnostics, notification, accessibility, or activity history.

Use safe semantic summaries.

---

## 24. Standard state presentation

DynamicUI24 should standardize visual states:

```text
Initial
Loading
Empty
FilteredEmpty
Unavailable
Offline
Unauthorized
Error
Partial
Ready
```

Not every component requires every state.

These are presentation semantics, not business state engines.

---

## 25. State presentation rules

State UI should be concise and actionable.

Examples:

- Empty → explanation + primary next action.
- FilteredEmpty → clear filter action.
- Offline → retry/reconnect if meaningful.
- Error → safe message + retry/details.
- Unauthorized → no leaked protected metadata.
- Unavailable → honest capability message.

Avoid modal dialogs for ordinary recoverable states.

---

## 26. Resource Chip Foundation

DynamicUI24 should provide a compact semantic resource-chip presentation.

A chip may represent file/document, user/contact, company, record, date/range, tag, filter, selected lookup item, AI/context resource, or application-defined semantic object.

---

## 27. Resource chip identity

A chip uses:

```text
ResourceKind
SemanticResourceId
SafeDisplayLabel
OptionalIconCode
```

Localized display text is not identity.

---

## 28. Resource chip capabilities

A resource chip may support select/focus, remove, open, preview, copy, status, error state, or contextual menu.

Capabilities resolve through existing command/authorization infrastructure.

---

## 29. Attachment model

Attachment presentation is a specialization of Resource Chip.

The UI foundation may represent attachment metadata such as semantic file/document ID, safe filename, type, size, upload state, capability, and icon/thumbnail seam.

DynamicUI24 does not become file storage.

---

## 30. Drag-and-Drop Foundation

DynamicUI24 should expose vendor-neutral drag/drop semantics for approved surfaces.

Potential sources/targets include files from OS, resource chips, grid rows where application permits, workspace items, attachments, reorderable UI definitions, and document resources.

---

## 31. Drag payload

Use semantic payloads.

Conceptually:

```text
DragPayload
- ResourceKind
- SemanticIds
- SafeDisplayMetadata
- AllowedOperations
```

Do not rely solely on visual control references.

---

## 32. Drop negotiation

Drop target resolves accepted resource kinds, allowed operation, authorization, application capability, and P1 policy.

Fail closed.

No mutation occurs merely because a pointer enters a drop target.

---

## 33. OS file drop

Where supported, OS file drag/drop may be adapted to an application attachment/import contract.

DynamicUI24 must not assume file storage destination.

Application/provider owns upload, storage, validation, virus/security checks, and persistence.

---

## 34. Review / Diff / Compare Surface

DynamicUI24 should provide a generic semantic compare/review presentation for metadata changes, configuration revisions, document revisions, form changes, before/after values, approval changes, and AI/application-proposed changes.

It is not code-specific.

---

## 35. Compare identity

A compare session may identify:

```text
CompareSessionId
LeftRevisionId
RightRevisionId
TargetSemanticId
```

Visual line/row numbers are presentation only unless the domain makes them semantic.

---

## 36. Canonical difference kinds

Generic difference kinds:

```text
Added
Removed
Changed
Moved
Unchanged
Conflict
```

Applications may extend display semantics through adapters.

---

## 37. Review actions

Where application policy allows, review UI may expose Accept, Reject, Apply, Restore, Open source, or Comment/annotation seam.

DynamicUI24 does not own the authoritative merge/apply engine.

The application/provider performs mutation.

---

## 38. Structured diff

For metadata/forms/configuration, prefer semantic field-level diff.

Example:

```text
FieldCode: BASIC_SALARY

Before: ...
After: ...
State: Changed
```

Do not compare localized labels as identity.

---

## 39. Text diff seam

A text compare adapter may provide block/line/word spans, change classifications, and safe display content.

Full rich document diff is not required by the core foundation.

---

## 40. P1 in compare/review

Review/Diff is not a privacy bypass.

Protected content must remain masked/hidden according to current authorization/privacy context.

Do not leak removed/previous values merely because they appear in history.

---

## 41. Lightweight Composer Foundation

DynamicUI24 needs a modern free-form interaction control distinct from field editors.

The Composer is designed for comments, notes, messages, instructions, AI/application prompts, support requests, and semantic action input.

It is not a full rich-text editor.

---

## 42. Composer anatomy

A Composer may include:

```text
Composer
├── Native multiline text input
├── Resource/attachment chips
├── Optional mentions
├── Optional slash/action picker
├── Primary Submit/Run action
└── Optional Cancel-running action
```

---

## 43. Composer native input

Composer text uses the Universal Editor/native-input rules established by Charter v0.2 and Task 10G.

OS input method owns Unicode, IME composition, caret, selection, clipboard, and native editing shortcuts.

Do not create another text engine.

---

## 44. Composer state

Composer runtime may track draft text, attached semantic resources, validation, submitting/running state, and error state.

Draft persistence is application policy.

DynamicUI24 must not automatically persist potentially sensitive composer text as user preference.

---

## 45. Composer submit

Submit invokes an existing semantic command/application callback.

DynamicUI24 does not decide business meaning of submitted content.

The same Composer may support Send, Run, Search, Comment, Create, Ask, or application-defined semantic action.

---

## 46. Mentions seam

A Composer may support semantic mentions such as `@Person`, `@Document`, `@Record`, or `@Workspace`.

Mention lookup must reuse provider/bounded lookup principles.

No separate unbounded mention engine.

---

## 47. Slash/action picker seam

A Composer may expose a compact action picker triggered through a UI convention such as `/`.

It resolves existing semantic actions/tools.

Do not create a parallel command registry.

The exact trigger character is presentation policy, not Core semantics.

---

## 48. Composer attachments

Composer attachments reuse Resource Chip / Attachment semantics.

Do not create a second attachment model.

---

## 49. Activity / change-history presentation seam

DynamicUI24 may expose a generic Activity surface for semantic events supplied by the application.

Examples include operation started/completed, configuration published, document updated, approval changed, or record edited.

DynamicUI24 does not invent authoritative audit history.

---

## 50. Activity identity

Use application-supplied semantic event identity:

```text
ActivityId
ActivityKind
Timestamp
ActorSemanticId?
TargetSemanticId?
SafeSummary
```

Application/provider owns authoritative event/audit storage.

---

## 51. Activity versus Notification

Activity history and Notification Center are different presentations.

Notification is attention-oriented.

Activity is history/change-oriented.

A single semantic event may generate both if application policy chooses.

Do not duplicate truth.

---

## 52. Progressive disclosure

Modern interaction surfaces should default to the smallest useful presentation.

Examples include inline primary action + overflow, collapsed secondary pane, concise operation toast + details on demand, resource chip rather than full metadata card, and empty state with one primary action.

Advanced features should remain discoverable without being permanently visible.

---

## 53. Hover-only action caution

Essential actions must not be available only through hover.

Hover/focus actions may supplement visible primary action, keyboard access, context menu, command palette, or accessibility action.

Touch/keyboard/accessibility users must retain equivalent capability.

---

## 54. Keyboard-first navigation

New v0.15 surfaces should integrate with existing keyboard architecture.

Examples include focus panes, open overflow, activate selected contextual action, Composer submit, cancel operation, open resource, or review next/previous change.

Do not hard-code macOS-specific shortcuts into Core.

---

## 55. Accessibility

All v0.15 primitives must expose semantic accessible roles/names/states.

Especially panes, operation progress, cancel/retry, chips, drop targets, compare changes, Composer attachments/actions, and contextual toolbars.

P1 remains authoritative over exposed values.

---

## 56. Localization

Runtime language switching updates presentation only.

Do not rebuild/reparent stateful Composer, operation, review or pane controls unnecessarily.

Preserve semantic IDs, selected resources, draft text, operation identity, compare target, and pane state.

---

## 57. Theme / density

All v0.15 surfaces reuse System/Light/Dark, scale and density infrastructure.

Theme change must not mutate semantic/runtime state.

---

## 58. Lazy construction

Task 10G established a critical startup rule:

> Heavy inactive workspaces must not be eagerly constructed during cold startup.

v0.15 extends this principle.

Do not eagerly construct secondary heavy panes, diff viewers, Composer adjuncts, large attachment previews, activity surfaces, or operation details until their presentation is required.

Lightweight semantic registration may remain eager if side-effect free.

---

## 59. Performance

New interaction primitives must remain bounded.

Examples:

- activity feeds use bounded windows;
- resource-chip collections virtualize or limit presentation when large;
- compare adapters do not materialize full enormous datasets unnecessarily;
- attachment preview is lazy;
- operation history is provider/window based where large;
- Composer mention lookup is bounded.

---

## 60. Provider ownership

DynamicUI24 may define vendor-neutral providers for activity, compare data, resource metadata, attachment metadata, mention lookup, and operation state.

Providers own acquisition.

DynamicUI24 owns orchestration/presentation.

No SQL/query engine.

---

## 61. Error isolation

Failure in one secondary interaction surface must not crash the Shell.

Examples include attachment preview failure, activity provider failure, compare adapter failure, and mention provider failure.

Use safe local error states.

---

## 62. Dynamic authorization

All v0.15 capabilities resolve through v0.14 authorization.

Examples:

- pane hidden;
- operation Cancel denied;
- attachment Remove denied;
- diff Accept denied;
- Composer Submit denied;
- resource Open denied;
- contextual action hidden.

User preference cannot override authorization.

---

## 63. Developer authoring integration

Task 10H Developer UI Authoring should be able to configure v0.15 presentation metadata where appropriate.

Examples include pane roles/defaults, contextual action placements, operation presentation policy, Composer enabled capabilities, allowed attachment kinds, and review surface availability.

Task 10H remains authoritative for developer configuration UX.

---

## 64. Design language

DynamicUI24 runtime should avoid legacy control-gallery density.

Prefer quiet backgrounds, meaningful whitespace, concise typography, clear content hierarchy, sparse separators, contextual controls, compact status, and progressive disclosure.

Functional breadth belongs beneath the surface.

---

## 65. Actipro-first presentation rule

Before building a new Avalonia presentation control:

1. Audit Actipro Avalonia Pro.
2. Reuse mature Actipro control when it cleanly satisfies the requirement.
3. Otherwise use native Avalonia.
4. Build custom presentation only where semantic behavior is genuinely missing.

Core remains vendor-neutral.

---

## 66. DevExpress benchmark rule

DevExpress WinForms may be used as a feature/ergonomics benchmark for legacy migration.

Usefully inspired areas include clear validation, embedded actions, contextual menus, polished selection, and data presentation ergonomics.

Do not clone its control hierarchy, API surface, property explosion, or heavyweight designer semantics.

---

## 67. Non-goals of v0.15

v0.15 does NOT introduce:

- another Global Search engine;
- another command registry;
- another notification engine;
- another Context Panel;
- another Quick Access system;
- AI/LLM runtime;
- automation scheduler;
- Git/worktree engine;
- terminal;
- arbitrary plugins/scripts;
- SQL/query engine;
- formula engine;
- business workflow engine;
- full rich-text word processor;
- full desktop IDE;
- universal heavy docking environment.

---

## 68. Suggested implementation task

The implementation task following adoption of v0.15 should be a focused foundation task such as:

```text
Task 10I — Modern Workspace Interaction Foundation
```

Task 10I should implement only the reusable primitives defined here.

Task 10H remains:

```text
Developer UI Authoring + Dynamic Feature Authorization
```

Recommended order:

```text
Task 10G — CLOSED
        ↓
Spec v0.15 — adopt
        ↓
Task 10H — Developer UI Authoring + Dynamic Authorization
        ↓
Task 10I — Modern Workspace Interaction Foundation
        ↓
restore/migrate Task 11 Report work as architect schedules
```

The architect may reorder 10H/10I if integration evidence warrants it.

---

## 69. Task 10I minimum acceptance surface

A neutral Modern Workspace Demo should physically demonstrate:

1. Primary workspace content.
2. Collapsible/resizable secondary pane.
3. Contextual inline action.
4. Selection-driven contextual toolbar.
5. Long-running operation state.
6. Cancel-capable operation.
7. Retry-capable failed operation.
8. Notification/Toast reuse for operation completion.
9. Resource chips.
10. Attachment chip.
11. Drag/drop into an approved drop target.
12. Compare/Review surface.
13. Accept/Reject semantic action seam.
14. Lightweight Composer.
15. Composer attachment/resource chip.
16. Composer submit through semantic command.
17. Unicode/native IME in Composer.
18. vi-VN/en-US runtime switch.
19. System/Light/Dark.
20. Accessibility basics.
21. Lazy construction of heavy secondary surfaces.
22. Clean exit.

---

## 70. Architecture guards

Future Task 10I architecture tests should prove:

1. no duplicate global search.
2. no duplicate command registry.
3. no duplicate notification coordinator.
4. no duplicate Context Panel engine.
5. semantic pane identity.
6. operation identity independent from controls.
7. operation provider owns actual work.
8. Cancel/Retry are capability-driven.
9. resource chips use semantic IDs.
10. drag/drop payload is semantic.
11. compare/review does not own business merge truth.
12. Composer reuses native input/editor rules.
13. Composer submit reuses semantic commands.
14. authorization reused.
15. P1 reused.
16. heavy secondary surfaces remain lazy.
17. Core contains no Actipro/Avalonia UI types.
18. no SQL.
19. no formula engine.
20. no business-specific UI logic.

---

## 71. Specification adoption procedure

v0.14 remains immutable historical authority.

Adopt v0.15 by:

1. Verify current branch and clean worktree.
2. Verify `HEAD == origin/main`.
3. Verify Charter v0.2 SHA.
4. Verify v0.14/v0.13/v0.12/v0.11/v0.10/v0.9 hashes.
5. Create `docs/specification/DynamicUI24-Spec-v0.15.md`.
6. Run `git diff --check`.
7. Review that v0.15 does not duplicate existing Search/Quick Access/Notification/Context/Command systems.
8. Commit v0.15 as a dedicated specification/governance commit.
9. Push only with explicit human approval.
10. Wait for required CI GREEN.
11. Record authoritative v0.15 SHA-256.
12. Only then start the next implementation task governed by v0.15.

---

## 72. Current authoritative hashes before v0.15 adoption

```text
DynamicUI24 Architecture Charter v0.2
415d53271b6681cdd9d617e4ab751e7316e03816f736df97b5425c37620420cc

DynamicUI24 Spec v0.14
4106e60c77a769a96a95055b0fab51c289212e945a9790b481e3cc7acc2cb199

DynamicUI24 Spec v0.13
4c26fa08960d8e40bbf390664dbe5d30aba891dec1f79aa265c390079089fccb

DynamicUI24 Spec v0.12
66cfdd715e4a8726f03b9ecfb06d08eac169a3e724c8d97cc455f42aa54434fb

DynamicUI24 Spec v0.11
5eb9de1cd43db592234f191dc78abb60cd7d26ad790289f0e941da6ab694d5c2

DynamicUI24 Spec v0.10
00a8d6a4a02a6b0152d171133b392861f3c2d38ebd05b421b2aad67fc1137c42

DynamicUI24 Spec v0.9
af99f4adf9bb4004a70c8c7d920e84894bc5aa62d5dd0ac62c329b27b94e4a0a
```

If any existing authoritative hash differs, STOP and investigate.

---

## 73. Governing v0.15 statement

> DynamicUI24 should provide mature desktop capability without forcing mature-desktop visual clutter.

> Global Search, commands, notifications, Quick Access and Context already exist and must be reused. v0.15 adds the missing modern interaction primitives around them: panes, contextual actions, long-running operation UX, semantic resource chips, drag/drop, compare/review, and a lightweight Composer.

> Applications own business work, data, security and persistence. DynamicUI24 owns semantic presentation, orchestration, progressive disclosure and cross-platform interaction.

---

**End of DynamicUI24 Specification v0.15 — Modern Workspace Interaction Foundation**
