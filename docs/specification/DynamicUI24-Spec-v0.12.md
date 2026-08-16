# DynamicUI24 Specification v0.12

**Status:** Architecture baseline / additive successor to v0.11
**Product:** DynamicUI24
**Primary platform:** .NET 9 + Avalonia, cross-platform
**Primary topic:** Multi-Sheet Data Workspace, Grid Header Composition, Sheet Lifecycle & Clone/Save-As

---

## 1. Purpose

DynamicUI24 v0.12 formalizes a reusable multi-sheet workspace composition model for DataEntry and future compatible templates.

The design borrows the usability of spreadsheet tabs, but DynamicUI24 MUST NOT become an Excel clone or a workbook engine.

Core separation:

```text
Sheet UI
!=
Sheet Data Model
!=
Calculation Engine
```

DynamicUI24 owns sheet composition, presentation state and lifecycle coordination.

The existing TS24 calculation engine remains authoritative for cross-sheet calculations. v0.12 does NOT introduce a second formula engine.

---

## 2. Primary Goals

v0.12 defines:

1. Multiple sheets inside one workspace.
2. Stable semantic `SheetCode`.
3. Grid/sheet Title and Subtitle.
4. Sheet tabs and overflow.
5. Sheet-specific runtime state.
6. Sheet-specific user preferences.
7. Create / Duplicate / Save As / Rename / Reorder / Hide / Show / Delete lifecycle.
8. Clone/save-as policies.
9. Explicit cross-sheet reference-remapping hooks.
10. Compatibility with the existing TS24 cross-sheet calculation layer.
11. 100K+ row virtualization safety across many sheets.
12. S1 Search integration: `Go to Sheet`.
13. S2 Context/Breadcrumb compatibility.
14. P1 Privacy and permission resolution per sheet.
15. 10E DataEntry personalization isolation per sheet.
16. Reuse by future Report, History/Document and Dashboard workspaces.

---

## 3. Relationship to v0.11

v0.11 remains authoritative for DevExpress document processing, Office/PDF adapter boundaries, Universal Subscription policy and digital-signing separation.

v0.12 adds workspace/sheet composition only and does not alter the v0.11 document architecture.

---

## 4. Relationship to DataEntry 10A–10E

Existing DataEntry capabilities remain authoritative:

- stable `RowKey`;
- stable `VariableCode`;
- 100K+ virtualization;
- active cell/range selection;
- clipboard;
- undo/redo;
- import/export;
- column personalization;
- typed filters/sort;
- privacy;
- Search/Quick Access;
- Context Panel/Breadcrumb/Help.

v0.12 composes one or more DataEntry instances under a stable Sheet Host.

---

## 5. Existing Calculation Engine Remains Authoritative

TS24 already has calculation functions capable of cross-sheet calculation.

Therefore:

```text
Existing TS24 Calculation Engine
= authoritative calculation engine

DynamicUI24 v0.12
= sheet identity + composition + lifecycle + reference compatibility
```

DynamicUI24 MUST NOT implement a parallel formula engine.

---

## 6. Cross-Sheet Compatibility Rule

Multi-sheet UI MUST preserve stable identities required by the existing calculation engine.

At minimum:

- `SheetCode`
- `VariableCode`
- row/business identity required by consumer logic

Tab title, localization, tab order, hidden state and overflow MUST NOT become formula identity.

---

## 7. Stable Sheet Identity

Each sheet has a stable semantic `SheetCode`.

Examples:

```text
SUMMARY
DETAIL
ADJUSTMENT
```

Do NOT use localized title, visual index, current order or caption as authoritative sheet identity.

Rename/reorder must not break existing cross-sheet formulas.

---

## 8. DataWorkspaceDefinition

Conceptually:

```text
DataWorkspaceDefinition
- WorkspaceCode
- TitleKey?
- SubtitleKey?
- SheetHostDefinition
- PermissionCode?
- CapabilityCode?
- HelpContextCode?
- CompanyScope?
```

---

## 9. SheetHostDefinition

Conceptually:

```text
SheetHostDefinition
- HostCode
- Sheets[]
- AllowCreate
- AllowDuplicate
- AllowSaveAs
- AllowRename
- AllowReorder
- AllowHide
- AllowDelete
- OverflowPolicy
- TabPlacement
- PreferenceScope
- PermissionCode?
- CapabilityCode?
```

Not every application enables every lifecycle operation.

---

## 10. SheetDefinition

Conceptually:

```text
SheetDefinition
- SheetCode
- TitleKey
- SubtitleKey?
- IconKey?
- Order
- ContentType
- ContentDefinitionCode
- IsVisible
- IsClosable
- IsReorderable
- IsHideable
- IsDuplicable
- IsSaveAsEnabled
- PermissionCode?
- CapabilityCode?
- HelpContextCode?
- CompanyScope?
- PrivacyPolicyCode?
- ClonePolicyCode?
```

---

## 11. Generic Content Types

Sheet Host is generic.

Possible content types:

```text
DATAENTRY_GRID
REPORT
HISTORY
DOCUMENT
DASHBOARD
CUSTOM
```

10F must prove `DATAENTRY_GRID`.

Future tasks may reuse the same host.

---

## 12. GridHeaderDefinition

Every DataEntry Grid may expose a reusable header:

```text
GridHeaderDefinition
- TitleKey
- SubtitleKey?
- IconKey?
- ShowRowCount?
- ShowSelectionCount?
- ShowFilteredCount?
- ShowStatus?
- Actions[]
- OverflowMenuCode?
- HelpContextCode?
```

Title/subtitle are metadata-driven or provider-resolved, never hard-coded in XAML.

---

## 13. Grid Header Presentation

Recommended:

```text
Grid Title
Grid Subtitle / runtime summary

[primary actions] [...]
---------------------------------
Grid
```

Example:

```text
Chi tiết dữ liệu
12,540 bản ghi · 23 đang chọn
```

Runtime status supplements rather than replaces metadata title/subtitle.

---

## 14. Workspace Header vs Grid Header

Workspace Header describes the whole workspace.

Grid Header describes the active sheet/grid.

These are distinct semantic layers.

---

## 15. Sheet Tabs

Recommended UX:

```text
[ Summary ] [ Detail ] [ Adjustment ] [ ... ]
```

Tabs support selection, keyboard navigation, overflow, stable identity, optional icons/status and permission-aware visibility.

---

## 16. Tab Overflow

When tabs do not fit:

```text
[ Summary ] [ Detail ] [ ... 12 ]
```

Overflow MUST reuse DynamicUI24's existing Menu/Flyout infrastructure.

---

## 17. Tab Reorder

Reordering changes presentation order only.

It MUST NOT change `SheetCode` or calculation identity.

---

## 18. Hide / Show Sheet

Hide removes a sheet from normal tab presentation but does not delete definition or data.

A hidden sheet may continue to participate in existing cross-sheet calculations if the application/calculation policy says so.

UI must not infer calculation semantics.

---

## 19. Active Sheet

Runtime uses semantic:

```text
ActiveSheetCode
```

Never visual tab index.

---

## 20. SheetRuntimeState

Conceptually:

```text
SheetRuntimeState
- SheetCode
- IsLoaded
- IsActive
- ScrollState?
- SelectionState?
- ActiveCell?
- GridQueryState?
- ContextState?
- DirtyState?
- Generation
```

---

## 21. DataEntry Sheet State

Each DataEntry sheet independently preserves:

- active cell;
- selection;
- sort/filter;
- column order/width/visibility/pinning;
- horizontal/vertical viewport;
- Context Panel selection;
- dirty/edit state where applicable.

A → B → A must restore A predictably.

---

## 22. Lazy Visual Materialization

Critical invariant:

```text
5 sheets x 100,000 logical rows
!=
500,000 materialized rows
```

Only active/needed visual content is materialized.

Inactive sheets retain semantic/runtime state without rendering entire datasets.

---

## 23. Large-Sheet Safety

Do NOT:

- instantiate every Grid fully on workspace startup;
- fetch all sheet datasets eagerly;
- retain every row visual;
- scan every sheet merely to render tabs.

Activation should be lazy where practical.

---

## 24. SheetPreference

Conceptually:

```text
SheetPreference
- WorkspaceCode
- SheetCode
- TabOrder?
- IsHidden?
- LastActive?
- ContentPreference?
```

For DataEntry, reuse 10E grid preferences instead of duplicating them.

---

## 25. Preference Identity

Preferences use:

```text
WorkspaceCode + SheetCode
```

not title or visual index.

---

## 26. Last Active Sheet

Workspace may remember the last active sheet.

If unavailable/unauthorized/removed, resolve the first eligible sheet deterministically.

---

## 27. S1 Search Integration

Global Search / Command Palette supports semantic results such as:

```text
Go to Sheet: Detail
```

Activation:

```text
WorkspaceNavigationService
→ Workspace
→ SheetNavigationService
→ SheetCode
```

Search must not manipulate tab controls directly.

---

## 28. Quick Access

If an application exposes a sheet as a Quick Access destination, identity is:

```text
WorkspaceCode + SheetCode
```

Never localized text.

---

## 29. S2 Breadcrumb

Default breadcrumb represents workspace/navigation hierarchy.

Sheet is local workspace context.

An application may optionally expose active sheet as a final crumb, but this is not mandatory.

---

## 30. S2 Context Panel

Context Panel follows active sheet and selection.

Sheet switch invalidates stale context and prevents prior-sheet data leakage.

---

## 31. HelpContextCode

Workspace, sheet and Grid Header may define HelpContextCode.

Use existing S2 specificity/precedence.

---

## 32. P1 Privacy

Sheet UI reuses P1.

Inactive, hidden, restricted or sensitive sheet data must not leak through any presentation or lifecycle surface.

At minimum protected data must not leak via:

- sheet title/subtitle;
- tooltip;
- tab/overflow menu;
- Search/Recent/Quick Access;
- Context Panel;
- notification;
- formula presentation;
- formula diagnostics;
- clipboard;
- import preview;
- export;
- accessibility/automation text;
- duplicate;
- Save As;
- clone diagnostics;
- lifecycle confirmation;
- inactive-sheet visual cache.

Cross-sheet calculation does not grant presentation permission.

A formula may be authorized to calculate from protected data while source/reference presentation remains governed by P1.

Duplicate/Save As MUST re-resolve privacy, permission and export/copy policy for the target sheet or data context. Cloning must never become a bypass for restricted data.

Export from a sheet or formula-derived result remains subject to the P1 export-security boundary.

Accessibility presentation must never expose raw restricted values merely because the value originated from another sheet or a formula result.

---

## 33. Permission / Capability

Permission can apply to workspace, host, sheet and lifecycle actions.

Unauthorized sheet cannot become visible because of stored user preference.

Fail closed.

---

## 34. Company Context

Company switch:

- invalidates stale sheet loads;
- re-resolves eligible sheets;
- clears stale Context Panel data;
- protects preferences semantically;
- blocks late results from the previous Company.

---

## 35. Sheet Lifecycle

v0.12 formalizes:

```text
CREATE
DUPLICATE
SAVE_AS
RENAME
REORDER
HIDE
SHOW
DELETE
```

Eligibility is metadata/policy driven.

---

## 36. Duplicate vs Save As

`Duplicate` creates another sheet derived from the current sheet in the same workspace.

`Save As` creates a new semantic sheet/data context derived from the current sheet.

Example:

```text
Period Current
→ Save As
→ Period Next
```

Core does not assume year/month/payroll/tax semantics.

---

## 37. New Identity Rule

Duplicate and Save As MUST create a NEW `SheetCode`.

Never clone source semantic identity.

---

## 38. SheetClonePolicy

Conceptually:

```text
SheetClonePolicy
- CloneMode
- CopyStructure
- CopyFormulas
- CopyValues
- CopyLayout
- CopyFilters
- CopySort
- CopyPermissionsMetadata
- CopyContentPreferences
- ResetRowKeys
- ResetEditHistory
- ResetUndoRedo
- ResetImportRuntime
- ReferenceMappingPolicy
```

Implementation may split this into smaller immutable contracts.

---

## 39. Clone Modes

Support semantic modes:

```text
DUPLICATE_FULL
STRUCTURE_ONLY
STRUCTURE_AND_FORMULAS
NEW_PERIOD
CUSTOM
```

---

## 40. Default Clone Safety

Recommended new-period defaults:

```text
CopyStructure = true
CopyFormulas = true
CopyLayout = true
CopyValues = configurable
CopyFilters = false
CopySort = optional
ResetRowKeys = true
ResetEditHistory = true
ResetUndoRedo = true
ResetImportRuntime = true
```

Never silently clone transient editing state.

---

## 41. RowKey Clone Rule

When cloning row data into a logically new dataset, new RowKeys are generated by provider/application policy.

Do not reuse source RowKeys by default.

---

## 42. Formula Compatibility

If existing TS24 calculation logic stores cross-sheet formulas, cloning preserves formula semantics only through authoritative calculation-layer APIs/policies.

DynamicUI24 MUST NOT speculate about formula syntax.

---

## 43. SheetReferenceMapping

For Save As/new-data-context scenarios, applications may provide explicit mapping:

```text
DETAIL_CURRENT -> DETAIL_NEXT
SUMMARY_CURRENT -> SUMMARY_NEXT
```

Conceptually:

```text
SheetReferenceMapping
- SourceSheetCode
- TargetSheetCode
```

The existing calculation layer consumes/validates it.

---

## 44. No Blind Formula Rewrite

Forbidden:

```text
string.Replace(...)
```

or any heuristic based on title, year text or tab index.

Reference remapping must use semantic calculation contracts.

---

## 45. Cross-Sheet Dependency Validation and Recalculation Contract

DynamicUI24 remains a presenter/coordinator, not the calculation engine.

The existing TS24 Calculation Engine remains authoritative and MUST provide deterministic cross-sheet dependency semantics.

For the same published metadata, data state and calculation inputs:

- dependency resolution order must be deterministic;
- affected cross-sheet dependencies must resolve consistently;
- recalculation must propagate to dependent sheets and variables;
- changing an input on one sheet may invalidate and recalculate dependent results on other sheets;
- UI tab order, localized title, hidden state and visual activation order must not affect calculation results.

DynamicUI24 may request or observe recalculation through an application-neutral compatibility seam, but MUST NOT evaluate formulas itself.

The existing calculation layer may return diagnostics including:

- unresolved SheetCode;
- unresolved VariableCode;
- broken references;
- clone-mapping collisions;
- delete dependencies;
- recalculation failures;
- circular or cyclic dependencies.

Circular or cyclic cross-sheet dependencies MUST be detected by the authoritative calculation layer and MUST fail safely.

A cycle MUST NOT cause uncontrolled recursion, infinite recalculation, UI-thread hangs, unbounded task creation or process crashes.

DynamicUI24 presents these diagnostics safely and MUST NOT repair formula semantics automatically.

---

## 46. Delete Sheet

Delete differs from Hide.

Before deletion:

- resolve permission;
- check application/provider policy;
- allow dependency validation;
- block/confirm referenced sheet deletion;
- never silently break cross-sheet references.

---

## 47. Hide Instead of Delete

Applications may disable Delete entirely and allow Hide only.

---

## 48. Rename Sheet

Normal Rename changes title/subtitle only.

It MUST NOT change `SheetCode`.

Changing semantic code is an administrative metadata migration, not ordinary UI rename.

---

## 49. Duplicate Titles

Titles may duplicate if allowed.

`SheetCode` must remain unique.

UI may suggest a localized unique caption.

---

## 50. SheetSaveAsRequest

Conceptually:

```text
SheetSaveAsRequest
- SourceSheetCode
- TargetSheetCode
- TargetTitle
- CloneMode
- TargetDataContext?
- ReferenceMappings[]
- Options
```

No business-specific period fields in Core.

---

## 51. ISheetLifecycleProvider

Lifecycle must use a semantic provider/coordinator seam.

Provider responsibilities may include:

- allocate target identity;
- create content;
- clone structure/data;
- validate delete;
- persist runtime-created definitions where appropriate.

Sheet UI must not copy business data itself.

---

## 52. Transaction Boundary

Duplicate/Save As should be a bounded operation.

Failure must not leave an ambiguous half-created visible sheet.

Provider/application owns physical transaction mechanics.

---

## 53. Progress

Large clone/save-as operations may use existing N1 progress.

One logical operation notification; no per-row toast spam.

---

## 54. Cancellation

Use cancellation where supported.

Generation/context protection remains correctness even when an underlying operation cannot be interrupted.

---

## 55. 100K Clone Rule

Large clones must happen through data/provider layer, never Grid visual objects.

Correct:

```text
Provider/data layer
→ bounded batch/stream
→ target dataset
```

Forbidden:

```text
visual rows
→ enumerate controls
→ clone
```

---

## 56. Import/Export Integration

Import/export is scoped to active SheetCode/content definition.

Tab reorder/hide/pin never changes `VariableCode` mapping identity.

Clone policy decides whether import/export profile association is copied.

---

## 57. Grid Header Actions

Grid Header may expose existing actions:

- Import
- Export
- Refresh
- Columns
- Filter
- Context
- sheet lifecycle
- overflow

Use existing Action/Menu infrastructure.

---

## 58. Sheet Menu

Recommended dynamic menu:

```text
Duplicate
Save As...
Rename
Move
Hide
Delete
```

Only eligible actions appear.

---

## 59. Optional Plus Button

`+` may create a sheet only when metadata/capability enables it.

Do not show universally.

---

## 60. Sheet Status

Optional semantic states:

- dirty;
- error;
- warning;
- loading.

Do not use status as identity.

---

## 61. Dirty State

Each sheet may have independent dirty state.

Switching sheets must not silently discard edits.

Workspace navigation/close uses existing dirty guard.

---

## 62. Duplicate / Save As Dirty Policy

Application decides whether lifecycle uses committed state, current candidate state, or requires commit first.

Framework must not guess.

---

## 63. Active Editing

Sheet switch while editing follows existing DataEntry commit/cancel policy and transaction integrity.

---

## 64. Keyboard

Support platform-appropriate sheet navigation if clean, e.g. Ctrl+PageUp/PageDown or command equivalents.

Do not conflict with existing shortcuts.

---

## 65. Accessibility

Tabs expose tab role, selected state, title, status, keyboard reachability and overflow access.

Grid Title/Subtitle are associated with active content.

---

## 66. Responsive Layout

At narrow width:

- overflow tabs;
- keep active sheet visible;
- move secondary Grid Header actions into overflow;
- truncate title/subtitle safely.

Do not squeeze the Grid merely to show every tab.

---

## 67. Localization

Required:

```text
vi-VN
en-US
```

Language switch updates titles/subtitles/menus while preserving SheetCode and runtime state.

---

## 68. Theme / Scale

System/Light/Dark and UI/font scaling must not reset active sheet or state.

---

## 69. Malformed Metadata Safety

Handle duplicate SheetCode, unknown content type, invalid order, unknown clone policy, unauthorized default sheet, invalid reference mapping and unknown icon safely.

No Shell crash.

---

## 70. Retired Sheet

If metadata removes a sheet:

- ignore stale preference;
- re-resolve active sheet;
- clear stale context;
- remove visual tab.

Calculation-layer migration remains application-owned.

---

## 71. Runtime-Created Sheets

User-created/runtime-created sheets still require stable semantic identity and lifecycle ownership.

Do not derive identity from title alone.

---

## 72. Persistence Boundary

Persistence of runtime-created sheets is application/provider responsibility.

v0.12 defines contracts/seams, not a new database.

---

## 73. Sheet Template

Optional future/current concept:

```text
SheetTemplateDefinition
```

for Create/Save As from a known composition template.

This is not an Office workbook template.

---

## 74. STRUCTURE_ONLY

Copies metadata/content structure but no business row values.

---

## 75. STRUCTURE_AND_FORMULAS

Preserves formula definitions only through the existing authoritative calculation layer.

No new parser/evaluator.

---

## 76. DUPLICATE_FULL

May copy committed values through provider operations while creating new semantic identity.

---

## 77. NEW_PERIOD

Generic clone mode only.

Consumer supplies target data context and mappings.

Core does not assume period semantics.

---

## 78. CUSTOM

Application-registered clone provider/policy.

No arbitrary scripts or untrusted plugin loading.

---

## 79. Semantic Events

Framework may emit:

```text
SheetCreated
SheetDuplicated
SheetSavedAs
SheetRenamed
SheetReordered
SheetHidden
SheetShown
SheetDeleted
SheetActivated
```

No raw sensitive payload required.

Authoritative audit storage is application-owned.

---

## 80. Search Safety

S1 `Go to Sheet` re-resolves permission/privacy before activation.

Unauthorized hidden sheets are not exposed via Search/Recent.

---

## 81. Context Switching Safety

Rapid A→B sheet switching uses generation/context guards.

Late A result must not overwrite B.

---

## 82. Sheet Load Failure

One sheet failing to load must not crash the workspace.

Other sheets remain usable.

---

## 83. Lazy Load

Inactive sheets may remain unloaded until activation.

Tab rendering does not imply full content initialization.

---

## 84. Preload Policy

Apps may preload small sheets explicitly.

Large DataEntry sheets are not preloaded by default.

---

## 85. Inactive Visual Cache

Sheet Host may keep a bounded small inactive visual cache, but semantic state must survive visual eviction.

---

## 86. Rehydration

Recreate evicted visual hosts from definition and restore valid state/preferences.

Re-query stale data safely.

---

## 87. DataEntry Preference Scope

Existing 10E grid preferences become sheet-aware:

```text
WorkspaceCode + SheetCode + GridCode
```

This prevents collisions between repeated GridDefinitions.

---

## 88. Filter/Sort Per Sheet

Each DataEntry sheet owns its own filter/sort state.

Clone policy determines carry-over.

---

## 89. Import Profile Per Sheet

Import profile association is copied only by explicit clone policy.

---

## 90. Export Per Sheet

Export targets active sheet/context.

Workspace-wide multi-sheet export is separate unless application provides it.

---

## 91. Title / Subtitle Safety

Dynamic subtitles may show counts/status but must not leak raw sensitive data.

Reuse P1.

---

## 92. Rename UI

Rename edits visible title, not SheetCode.

---

## 93. Confirmation UX

Delete and destructive overwrite-style Save As use appropriate confirmation.

Harmless switch/reorder/duplicate should stay lightweight.

---

## 94. Collision Safety

Duplicate target SheetCode is rejected.

Reference mapping collision is rejected unless the authoritative calculation layer explicitly supports it.

---

## 95. Broken Reference Diagnostics

Dependency diagnostics may include safe SheetCode/FormulaCode references and warning/error severity.

No raw stack traces.

---

## 96. Existing Formula Designer Compatibility

If the existing TS24 formula designer already supports cross-sheet references, 10F exposes stable eligible SheetCode/VariableCode context through an adapter/provider seam.

Do not reimplement the formula designer.

---

## 97. ISheetRegistry

Provide deterministic semantic sheet registry.

Responsibilities:

- resolve SheetCode;
- enumerate eligible sheets;
- validate uniqueness;
- optionally support runtime additions.

No UI references.

---

## 98. ISheetNavigationService

Shared activation service:

```text
ActivateSheet(workspaceCode, sheetCode)
```

Tab clicks, S1 Search and lifecycle outcomes use the same state path.

---

## 99. Sheet Commands

Lifecycle actions use existing command infrastructure:

```text
SHEET.DUPLICATE
SHEET.SAVE_AS
SHEET.RENAME
SHEET.HIDE
SHEET.DELETE
```

No new command system.

---

## 100. Action Eligibility

Resolve metadata, permission, capability, dirty state, lifecycle provider support and application policy.

---

## 101. Grid Header Runtime Status

Safe status can include row counts, filtered/selected rows, updated time and data-context label.

Avoid expensive synchronous counts.

---

## 102. Multiple Grids Per Sheet

The architecture does not require exactly one Grid per sheet.

10F may prove one primary DataEntry Grid per sheet.

Complex composition can be future work.

---

## 103. Nested Sheet Hosts

Not required.

Avoid recursive tab complexity.

---

## 104. Tab Pinning

Optional local presentation feature.

Do not confuse with S1 Favorite/Pinned destination semantics.

---

## 105. Calculation Engine Boundary

Mandatory:

```text
DynamicUI24 MUST NOT:
- evaluate cross-sheet formulas;
- maintain a parallel formula dependency graph;
- redefine function semantics;
- silently rewrite formulas.

Existing TS24 Calculation Engine remains authoritative.
```

The authoritative TS24 Calculation Engine is expected to provide:

```text
- deterministic dependency resolution;
- affected-node recalculation;
- cross-sheet recalculation propagation;
- circular/cyclic dependency detection;
- safe calculation failure;
- semantic SheetCode / VariableCode reference handling.
```

DynamicUI24 only coordinates and presents these outcomes through semantic contracts.

Sheet activation, rename, reorder, hide/show and localization MUST NOT influence calculation ordering or results.

---

## 106. Formula Reference Contract

DynamicUI24 may expose semantic:

```text
SheetCode
VariableCode
RowKey / business key where relevant
```

for interoperability only.

---

## 107. No A1 Identity Requirement

Excel-style A1 coordinates are not authoritative business identity.

Existing formula UI may display A1-like notation if desired, but semantic contracts remain preferred.

---

## 108. DevExpress Boundary

v0.11 remains authoritative.

A DynamicUI24 Sheet Host is NOT a DevExpress workbook and does not use DevExpress worksheet objects as runtime identity.

DevExpress continues to process physical Office files behind adapters.

---

## 109. Digital Signing Boundary

Unchanged from v0.11.

Sheet composition has no signing responsibility.

---

## 110. 10F Implementation Scope

10F should implement:

- DataWorkspace/Sheet contracts;
- Sheet Host;
- Grid Header Title/Subtitle;
- tabs and overflow;
- shared sheet activation;
- per-sheet runtime state;
- lazy visual materialization;
- sheet-aware 10E preferences;
- Create seam;
- Duplicate;
- Save As;
- Rename;
- Reorder;
- Hide/Show;
- Delete seam/policy;
- clone policy;
- reference-mapping seam;
- S1 Go to Sheet;
- S2 Context integration;
- P1 privacy/permission;
- multiple-100K-sheet bounded proof.

---

## 111. 10F Non-Goals

Do NOT implement:

- new formula engine;
- formula parser;
- calculation dependency engine;
- Excel workbook runtime;
- pivot tables;
- charts;
- Report runtime;
- Office automation;
- business-period logic;
- payroll/tax clone logic;
- user-created-sheet database;
- collaboration;
- workflow;
- AI/LLM.

---

## 112. Demo

Use neutral sheets such as:

```text
SUMMARY
DETAIL
ADJUSTMENT
ARCHIVE
```

At least 3 eligible sheets and 2 DataEntry sheets.

---

## 113. Demo Cross-Sheet Compatibility

Prove:

- SheetCode stable after rename/reorder;
- clone emits explicit reference mapping;
- no formula evaluation occurs in Sheet Host.

Use an existing/fake calculation compatibility service only; do not create an engine.

---

## 114. Demo Duplicate

Example:

```text
DETAIL
→ Duplicate Full
→ DETAIL_COPY
```

New SheetCode, new RowKeys where data is copied, reset edit/undo state.

---

## 115. Demo Save As

Example:

```text
SUMMARY
→ Save As
→ SUMMARY_NEXT
```

with explicit mapping request and no business-period assumptions.

---

## 116. Demo Structure Only

Create blank structure-only sheet with no copied row values.

---

## 117. Demo State Isolation

Set different filters, scroll, active cells and layouts across two sheets; switch repeatedly and verify isolation.

---

## 118. Demo 100K x Multiple Sheets

At minimum:

- Sheet A = 100K logical rows;
- Sheet B = 100K logical rows.

Switch A/B and assert bounded materialized rows/cache.

---

## 119. Demo Overflow

Force overflow and activate a hidden tab through the shared menu.

---

## 120. Demo Privacy/Search/Context

Prove:

- restricted subtitle safe;
- Cmd+K Go to Sheet;
- Context Panel follows active sheet;
- stale prior sheet context blocked.

---

## 121. Demo Rename/Reorder/Hide

Rename title while SheetCode remains unchanged.

Reorder without affecting formula identity.

Hide/show without deleting semantic identity.

---

## 122. Demo Delete Safety

Use dependency validation seam to block/warn referenced-sheet delete.

No silent reference break.

---

## 123. Tests — Identity

Cover unique SheetCode, duplicate titles, rename, reorder, hide/show, localization and preference restore.

---

## 124. Tests — State Isolation

Cover per-sheet active cell, filter/sort, preferences, viewport, dirty state and visual rehydration.

---

## 125. Tests — Lifecycle

Cover Create, Duplicate, Save As, Rename, Reorder, Hide, Show, Delete eligibility and provider failure.

---

## 126. Tests — Clone Policy

Cover full duplicate, structure-only, structure+formulas, new-period generic mode, copy-values flag, RowKey reset, history reset and filter/sort carry-over.

---

## 127. Tests — Reference Mapping

Cover valid mapping, collisions, duplicate target, missing source and calculation-layer rejection.

No blind rewrite.

---

## 128. Tests — 100K

Cover 2+ large sheets, lazy activation, bounded materialization, visual eviction/rehydration and far jumps per sheet.

---

## 129. Tests — Search/Context/Privacy

Cover S1 activation, stale search result, S2 context switch, Company switch, permission loss and sensitive subtitle/overflow behavior.

---

## 130. Architecture Guards

Future 10F guards must prove:

1. Sheet contracts are Avalonia-free where appropriate.
2. SheetCode is semantic identity.
3. Title/index is never authoritative identity.
4. 10E preferences include sheet scope.
5. Inactive sheets do not require full visual materialization.
6. Lifecycle UI does not copy business data directly.
7. Provider owns physical clone/data mechanics.
8. Existing calculation engine remains external.
9. Sheet Host performs no formula execution.
10. No blind formula rewrite.
11. S1 navigation reused.
12. S2 context reused.
13. P1 privacy reused.
14. Menu/Action infrastructure reused.
15. v0.11 DevExpress boundary preserved.
16. No business-specific period logic.
17. No PayCalc24/Odoo dependency.
18. No AI/LLM.
19. No new database.
20. No platform-specific Core code.

---

## 131. Required 10F Documentation

```text
docs/architecture/MULTI-SHEET-WORKSPACE.md
docs/architecture/SHEET-LIFECYCLE.md
docs/architecture/SHEET-CLONING.md
docs/architecture/SHEET-CALCULATION-COMPATIBILITY.md

docs/adoption/MULTI-SHEET-DATAENTRY.md
docs/adoption/SHEET-DUPLICATE-SAVE-AS.md

docs/design-system/SHEET-TABS.md
docs/design-system/GRID-HEADER.md

docs/backlog/TASK-10F-BACKLOG.md
```

---

## 132. Local-AI Maintainability

Docs must clearly state:

```text
WHAT SHEET HOST OWNS
WHAT IT DOES NOT OWN
SHEETCODE RULE
TITLE/SUBTITLE RULE
RUNTIME STATE RULE
LAZY MATERIALIZATION RULE
PREFERENCE RULE
DUPLICATE VS SAVE-AS
CLONE POLICY
ROWKEY CLONE RULE
REFERENCE MAPPING RULE
CALCULATION ENGINE BOUNDARY
SEARCH INTEGRATION
CONTEXT INTEGRATION
PRIVACY RULE
FOCUSED TEST COMMANDS
COMMON FAILURE MODES
```

---

## 133. Real macOS Smoke for 10F

Verify:

1. launch;
2. workspace opens;
3. Grid Header title/subtitle;
4. multiple tabs;
5. switch sheets;
6. state isolation;
7. two 100K logical sheets;
8. bounded materialization;
9. overflow;
10. rename;
11. reorder;
12. hide/show;
13. duplicate;
14. structure-only duplicate;
15. Save As;
16. new SheetCode;
17. RowKey reset where applicable;
18. explicit reference mapping;
19. no formula execution in Sheet Host;
20. S1 Go to Sheet;
21. S2 Context Panel;
22. privacy/permission;
23. vi-VN;
24. en-US;
25. System;
26. Light;
27. Dark;
28. clean Exit.

---

## 134. 10F Acceptance

10F is complete only when stable SheetCode, multi-sheet host, Grid title/subtitle, tabs/overflow, state isolation, bounded multi-100K behavior, lifecycle operations, new identity on duplicate/save-as, clone policy, RowKey reset, explicit reference mapping, S1/S2/P1/10E integration, focused tests, architecture tests, real macOS smoke and five-RID CI all pass.

The existing TS24 calculation engine must remain authoritative and no new formula engine may be introduced.

---

## 135. Mandatory Codex Rule for 10F

```text
Existing TS24 Calculation Engine already supports cross-sheet calculations.

Treat the existing TS24 Calculation Engine as authoritative for:

- deterministic cross-sheet dependency resolution;
- recalculation propagation;
- circular/cyclic dependency detection and safe failure.

Do NOT create a second formula engine.

Multi-sheet DynamicUI24 must preserve stable SheetCode / VariableCode
identities and expose explicit reference-mapping seams for the existing
calculation layer.

Duplicate / Save As must create a new SheetCode.

Never rewrite cross-sheet formulas by string replacement, localized title,
visual tab order or tab index.

Large-sheet cloning must happen through provider/data-layer operations,
not Grid visual rows.
```

---

## 136. Final Architectural Rule

```text
Workspace
   |
   +-- SheetHost
          |
          +-- Sheet A
          |      +-- Grid Header
          |      +-- DataEntry / other content
          |
          +-- Sheet B
          |
          +-- Sheet C
```

Calculation remains:

```text
Stable SheetCode / VariableCode
          |
          v
Existing TS24 Calculation Engine
```

Lifecycle remains:

```text
Sheet UI
   |
   v
Sheet Lifecycle Provider
   |
   +-- clone structure
   +-- clone data
   +-- allocate new identity
   +-- explicit reference mapping
   |
   v
Application / Data Layer
```

No visual tab operation is allowed to become business identity.

---

**End of DynamicUI24 Specification v0.12**
