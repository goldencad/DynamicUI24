# DynamicUI24 Specification v0.13 — Universal Editor Foundation

**Document type:** Versioned DynamicUI24 specification amendment  
**Target authoritative version:** v0.13  
**Revision basis:** DynamicUI24 Spec v0.12 + Universal Editor Foundation  
**Architecture authority:** `DynamicUI24-ARCHITECTURE-CHARTER.md` v0.2  
**Charter SHA-256:** `415d53271b6681cdd9d617e4ab751e7316e03816f736df97b5425c37620420cc`  
**Previous authoritative specification:** `docs/specification/DynamicUI24-Spec-v0.12.md`  
**Previous v0.12 SHA-256:** `66cfdd715e4a8726f03b9ecfb06d08eac169a3e724c8d97cc455f42aa54434fb`

> v0.13 is additive. All requirements and invariants of v0.12 remain authoritative unless this document explicitly extends them.  
> v0.13 introduces a reusable, application-neutral Universal Editor Foundation so DataEntry, Report Parameters, Filters, Forms, Setup/Configuration and future DynamicUI24 surfaces do not create independent editor systems.

---

## 1. Purpose

DynamicUI24 must provide a common editor foundation capable of replacing the ordinary data-entry/editor layer used by legacy WinForms applications while preserving DynamicUI24's cross-platform, metadata-driven and vendor-neutral architecture.

The goal is capability parity at the architectural level, not API or visual cloning of DevExpress WinForms controls.

Applications describe semantic fields and editor requirements. DynamicUI24 resolves and presents the appropriate editor.

```text
Application metadata
        ↓
EditorDefinition / semantic value type
        ↓
Editor policy + runtime resolver
        ↓
DynamicUI24 presentation adapter
        ↓
Avalonia/native control
        ↓
Operating-system keyboard / Unicode / IME
```

---

## 2. Ownership

### DynamicUI24 owns

- reusable editor metadata/contracts;
- editor selection/resolution;
- generic formatting and parsing coordination;
- generic validation presentation;
- masks that are safe for native composition;
- common editor chrome;
- editor state and commit/cancel coordination;
- lookup presentation/provider contracts;
- localization/theme/accessibility integration;
- P1-aware presentation;
- generic keyboard/focus/edit UX;
- reusable Avalonia editor implementations.

### DynamicUI24 does not own

- payroll/tax/accounting/business validation truth;
- application database queries;
- authoritative business persistence;
- formula evaluation;
- business workflow;
- application-specific lookup semantics;
- language-specific input methods;
- transliteration engines;
- digital signing;
- arbitrary executable validation scripts.

Business/domain validation remains application/provider owned where it represents business truth.

---

## 3. Semantic identity

Editor instances must be resolved from stable semantic metadata.

Typical identities may include:

```text
WorkspaceCode
SheetCode
GridCode
VariableCode
ReportCode
ReportParameterCode
FieldCode
EditorCode
HelpContextCode
PolicyCode
RowKey
```

Visual control instances, localized captions, current order, screen coordinates and visual indexes are never durable identity.

An editor may be rematerialized without changing the semantic field it represents.

---

## 4. Definition, runtime and presentation separation

```text
EditorDefinition
!=
EditorRuntimeState
!=
Avalonia Control
```

`EditorDefinition` is stable metadata.

`EditorRuntimeState` may contain current typed value, validation state, dirty/pending state, lookup generation and interaction state.

Avalonia controls are presentation and may be created, recycled or replaced without becoming business identity.

Core contracts must not expose Avalonia, WinForms or DevExpress control types.

---

## 5. Canonical editor families

v0.13 establishes the following canonical editor families.

### 5.1 Text

- single-line Unicode text;
- placeholder/null text;
- optional maximum length;
- read-only/disabled states;
- culture-independent storage unless application semantics state otherwise.

### 5.2 Multiline Text / Memo

- multiline Unicode input;
- native caret and selection;
- configurable wrapping;
- bounded presentation;
- no requirement for rich-text semantics.

### 5.3 Integer

- typed integer value;
- culture-aware display/parsing;
- optional minimum/maximum;
- optional step.

### 5.4 Decimal

- typed decimal value;
- culture-aware decimal/group separators;
- precision/scale policy;
- optional minimum/maximum.

### 5.5 Currency

- typed numeric value;
- currency formatting is presentation;
- currency code/symbol must not silently change stored numeric meaning.

### 5.6 Percentage

- typed numeric value;
- explicit percentage scaling policy;
- formatting and stored-value semantics must be deterministic.

### 5.7 Boolean

Presentation may resolve to:

- CheckBox;
- Toggle;
- two-state choice;

according to metadata/theme/platform policy.

Tri-state is supported only when semantic null/indeterminate is explicitly allowed.

### 5.8 Date

- typed date value;
- culture-aware display;
- native or framework date picker where supported;
- optional min/max.

### 5.9 Time

- typed time value;
- culture-aware display;
- optional min/max.

### 5.10 DateTime

- typed date/time value;
- explicit timezone/offset semantics remain application/domain responsibility;
- editor must not silently reinterpret timezone meaning.

### 5.11 Date Range

- start/end values;
- deterministic validation of ordering where configured;
- no business-period semantics invented by DynamicUI24.

### 5.12 Choice / ComboBox

- bounded fixed or provider-supplied choices;
- stable semantic option identity;
- localized display text is presentation only.

### 5.13 MultiChoice

- multiple semantic selections;
- bounded presentation;
- explicit maximum selection where configured;
- no raw sensitive values in chips/tokens when P1 forbids them.

### 5.14 Lookup

- semantic key/value selection;
- provider-owned acquisition;
- async and generation-safe;
- bounded result windows;
- searchable where capability allows.

### 5.15 Search Lookup

A lookup may expose a Grid-like searchable selection surface for larger datasets.

It must reuse shared search/filter/provider concepts where practical and must not materialize an entire large lookup dataset.

### 5.16 Tree Lookup

Hierarchical lookup may be supported through a vendor-neutral hierarchy provider.

Tree identity must be semantic, not visual path/index.

### 5.17 AutoComplete / AutoSuggest

- asynchronous provider capability;
- generation-safe;
- bounded suggestions;
- no stale Company/context result publication;
- no P1 leakage through suggestion text.

### 5.18 Button Edit

A text/value editor may expose one or more embedded semantic actions.

Examples include browse, select, clear or open.

Embedded buttons invoke registered semantic commands/callback contracts; they must not embed business logic into Core.

### 5.19 Hyperlink

- presents a semantic value/reference as an actionable link where authorized;
- external navigation must use explicit capability/policy;
- no automatic unsafe external execution.

### 5.20 Password / Secret

- masked presentation;
- no reveal/copy unless explicit policy permits;
- must not be logged, persisted as UI preference or exposed through accessibility text;
- DynamicUI24 is not a secret vault.

### 5.21 Extensible editor family

Applications/adapters may register additional editor implementations behind typed, explicit contracts.

Unknown editor types must fail safely to an approved fallback or unsupported state. They must not trigger arbitrary reflection/plugin execution.

---

## 6. Common editor chrome

An editor may declaratively support:

- Label;
- Floating Label;
- Placeholder / Null Text;
- Leading Icon;
- Trailing Icon;
- Embedded Buttons;
- Helper Text;
- Error Text;
- Required indicator;
- Read-only state;
- Disabled state;
- Tooltip / contextual hint;
- Help action through `HelpContextCode`.

These are presentation capabilities.

Localized label/helper/error text must not become semantic identity.

---

## 7. Unicode-first and native input

All editable text surfaces are Unicode-first.

The operating-system input method owns text composition.

DynamicUI24 must preserve native behavior for:

- Vietnamese IME;
- Japanese IME;
- Korean IME;
- Chinese IME;
- Arabic and other Unicode scripts;
- dead keys;
- emoji;
- composed characters;
- candidate selection;
- caret;
- selection;
- native editing shortcuts.

DynamicUI24 must not implement language-specific keyboard conversion, transliteration, accent composition or legacy encoding logic.

Default text storage is ordinary Unicode strings.

Font selection must permit normal application/system Unicode fallback. A field must not require a language-specific font merely to accept Unicode input.

---

## 8. Native editor ownership while focused

When a native text editor owns keyboard focus, parent Grid/Form/Workspace handlers must not steal keys required by native editing or IME.

This includes, as applicable:

- arrows;
- Home/End;
- Backspace/Delete;
- Escape where editor semantics own it;
- Enter where editor semantics own it;
- Undo/Redo;
- Cut/Copy/Paste;
- Select All;
- composition/candidate navigation.

Parent-level commands may resume after editor commit/cancel/focus transition.

Composition pre-edit states must not be treated as authoritative committed business values.

---

## 9. Formatting and parsing

Formatting and parsing are explicit editor capabilities.

```text
Typed Value
   ↓ format
Display Text

Display/Input Text
   ↓ parse
Typed Candidate Value
```

Supported generic policies may include:

- standard format specifiers;
- custom format specifiers;
- numeric formatting;
- date/time formatting;
- percentage formatting;
- currency formatting;
- application-provided typed formatter/parser adapters.

HTML formatting is not a required generic text-editor capability.

Formatting must not change semantic identity or silently mutate the stored typed value.

Parsing failures produce safe validation state rather than process failure.

---

## 10. Input masks

Masks are input-assistance/validation policy, not a replacement text engine.

Generic mask categories may include:

- Simple;
- Numeric;
- Date/Time;
- TimeSpan;
- Regex or equivalent constrained pattern.

Mask behavior must be compatible with native Unicode/IME composition.

A mask must not process transient composition states as final committed text.

A mask must not introduce language-specific transliteration.

Where a mask cannot safely coexist with native composition on a platform, the implementation must prefer correct native input and validate at an appropriate commit boundary.

---

## 11. Validation

DynamicUI24 supports generic validation coordination.

Validation categories:

- required;
- type/parse;
- range;
- length;
- mask/pattern;
- application-provided synchronous rule;
- application/provider-owned asynchronous rule;
- cross-field validation through explicit semantic contracts.

Validation result is semantic and may contain:

```text
IsValid
Severity
MessageCode
SafeLocalizedMessage
TargetSemanticId
```

Presentation may show:

- error text;
- warning text;
- icon;
- tooltip;
- field state.

Validation must not leak restricted values in messages or diagnostics.

Business validation remains authoritative in the application/provider where applicable.

---

## 12. Commit, cancel and dirty boundaries

Editor interaction must distinguish:

```text
Native Composition
!=
Editor Candidate
!=
Editor Commit
!=
Grid/Form Save
!=
Provider Persistence
```

A control losing focus does not automatically imply provider persistence.

DataEntry's established rule remains:

```text
CELL COMMIT != GRID SAVE != PROVIDER PERSISTENCE
```

Other consumers may define their own explicit save boundary without creating a second editor engine.

---

## 13. Text selection and context menu

Native editing behavior is authoritative wherever possible.

Common capabilities:

- mouse selection;
- keyboard selection;
- caret navigation;
- Undo;
- Redo;
- Cut;
- Copy;
- Paste;
- Delete;
- Select All.

A generic editor context menu may expose these actions.

DynamicUI24 should reuse one semantic command/menu infrastructure rather than implement independent command engines per editor.

P1/permission may disable or alter clipboard commands.

---

## 14. Lookup provider contract

Large lookup data must be provider-owned and bounded.

Conceptual request:

```text
EditorLookupRequest
- EditorCode / Field semantic identity
- SearchText
- Filter/Context
- Offset / continuation
- WindowSize
- CompanyContext
- Generation
```

Conceptual result:

```text
EditorLookupResult
- SemanticOptionId
- SafeDisplayText
- Optional secondary safe presentation
- Continuation / logical count where supported
- Generation
```

Provider contracts must not return Avalonia/DevExpress controls.

Opening a lookup must not eagerly enumerate the complete logical dataset.

---

## 15. Lookup generation and Company safety

Canonical rule:

```text
lookup request A
→ Company/context/search changes
→ request B
→ B completes
→ A completes late
→ A ignored
```

Cancellation is optimization.

Generation/context validation is correctness.

No Company A lookup result may appear after Company B becomes authoritative.

---

## 16. P1 privacy and permission

Editors are subject to existing permission and P1 policy.

Protected data must not leak through:

- editor text;
- placeholder;
- helper/error text;
- lookup suggestions;
- lookup secondary labels;
- tooltips;
- context menu;
- clipboard;
- accessibility;
- diagnostics;
- preferences;
- recent values;
- autocomplete history.

Permission/capability may resolve presentation to:

```text
HIDE
DISABLE
READ_ONLY
EDITABLE
```

Fail closed.

---

## 17. Localization and culture

Localization changes presentation, not semantic identity.

Culture may affect:

- labels;
- placeholder/helper/error text;
- number display;
- date/time display;
- currency/percentage display;
- parsing rules where explicitly culture-aware.

Runtime language switching must not unnecessarily rebuild/reparent stateful editor controls.

Changing UI language must preserve valid editor state and native input ownership.

---

## 18. Theme, density and scale

Editors must integrate with shared:

- System/Light/Dark appearance;
- UI scale;
- font scale;
- density;
- focus/error/read-only/disabled visual states.

Theme or scale changes must not change typed values, validation truth, semantic identity or business dirty state.

---

## 19. Accessibility

Editors must expose appropriate accessible:

- name;
- role;
- state;
- required/read-only/disabled status;
- validation status;
- safe help/error text.

P1 overrides accessibility exposure of protected raw values.

Large lookup surfaces remain virtualized/bounded.

---

## 20. Help integration

Editors may expose `HelpContextCode`.

Help invocation must reuse the shared DynamicUI24 help/context infrastructure.

Editor implementations must not hard-code application help URLs or business documentation paths into Core.

---

## 21. Consumer reuse rule

The following consumers must converge on the Universal Editor Foundation rather than creating separate editor engines:

```text
DataEntry INPUT cells
Report Parameters
Grid Filter editors
Search/Find text surfaces where applicable
Metadata-driven Forms
Setup / Configuration fields
Dialog inputs
Future reusable field-based workspaces
```

Consumers may apply context-specific layout and commit policy, but editor semantics, native-input ownership, validation presentation and typed editor resolution should be shared.

---

## 22. DataEntry integration

DataEntry remains authoritative for:

- `RowKey + VariableCode` cell identity;
- selection;
- clipboard transaction semantics;
- pending values;
- undo/redo;
- Grid Save;
- provider persistence boundary;
- virtualization.

Universal Editor Foundation supplies the resolved editing surface for eligible INPUT cells.

FORMULA/SYSTEM remain read-only unless an explicit viewer capability is used.

No second DataEntry transaction engine may be introduced.

---

## 23. Report Parameter integration

Report Parameters must use Universal Editor Foundation.

`ReportParameterCode` remains authoritative parameter identity.

Opening/closing the parameter panel must:

- use metadata/runtime parameter state;
- perform zero report-result provider query merely because the panel opens;
- perform zero report-row materialization;
- avoid rebuilding the Report Workspace;
- preserve editor state;
- remain responsive on first physical open.

Running a report remains a separate explicit action.

---

## 24. Filter integration

Typed filters should resolve suitable editors through the same foundation.

Examples:

- text → Text;
- number → Integer/Decimal;
- date → Date/DateTime;
- boolean → Boolean;
- enumerated field → Choice/Lookup.

Filter semantics remain owned by the Grid/query/filter subsystem.

The Editor Foundation does not become a query engine.

---

## 25. Metadata-driven Form integration seam

v0.13 establishes the editor seam required for future Universal Form Runtime.

A future form definition should be able to reference semantic fields and editor metadata without creating new editor implementations.

Full Form layout/workflow is not required by v0.13 unless separately tasked.

---

## 26. Editor resolution

A central resolver selects an editor from semantic metadata.

Conceptually:

```text
Resolve(
    ValueType,
    ExplicitEditorKind?,
    Capabilities,
    Permission,
    PlatformCapabilities
)
→ EditorResolution
```

Resolution must be deterministic and testable.

An explicit compatible editor kind may override the default.

Incompatible combinations fail safely.

---

## 27. Default resolution policy

A generic default mapping should exist.

Conceptually:

```text
String        → Text
LongString    → MultilineText
Int           → Integer
Decimal       → Decimal
Money         → Currency
Percentage    → Percentage
Boolean       → Boolean
Date          → Date
Time          → Time
DateTime      → DateTime
Enum/Choice   → Choice
LookupKey     → Lookup
Secret        → Password
```

Applications should not need to specify an editor for every ordinary field.

---

## 28. Editor capability model

Capabilities should be explicit rather than inferred from visual control type.

Examples:

```text
CanEdit
CanClear
CanCopy
CanPaste
CanReveal
CanBrowse
CanSearch
CanSuggest
CanOpen
SupportsMultiple
SupportsMask
SupportsMinMax
SupportsEmbeddedActions
```

Permission/policy remains authoritative over requested capability.

---

## 29. Embedded actions

Embedded editor buttons/actions must use semantic commands.

Examples:

- browse;
- select;
- open;
- clear;
- refresh lookup.

The button visual is presentation.

The action is semantic.

No application-specific service call should be hard-coded into a generic Avalonia editor.

---

## 30. Performance and lifecycle

Editor foundation must avoid expensive cold-path behavior.

Requirements:

- do not instantiate controls for unmaterialized 100K Grid cells;
- create only materialized/active editors;
- reuse lightweight presenter/runtime state where safe;
- avoid visual-tree rebuild for simple visibility/language/theme changes;
- do not query provider merely to open an editor unless that editor explicitly requires provider data;
- provider-backed suggestions/lookups remain async and bounded;
- no unbounded caches of editor controls or sensitive values.

---

## 31. Virtualized Grid rule

```text
100,000 logical cells/rows
!=
100,000 editor controls
```

Ordinary Grid display cells are presenters.

A native editor is created/activated only for the active materialized editing context as required.

Scrolling/rematerialization must preserve semantic value/edit state through runtime models, not retained control instances.

---

## 32. Error and provider failure isolation

Editor/lookup/provider failure must not crash the Shell.

Use safe editor states and bounded diagnostics.

A failed lookup provider must not invalidate unrelated editors/workspaces.

Raw provider exceptions must not become user-facing sensitive diagnostics.

---

## 33. Vendor neutrality

`DynamicUI24.Core` must not expose:

- DevExpress WinForms editor types;
- WinForms control types;
- Avalonia control types;
- vendor-specific mask/validation objects.

Avalonia is the current cross-platform presentation implementation.

DevExpress WinForms Data Editors are a capability benchmark for legacy migration, not an implementation dependency for DynamicUI24 Core.

---

## 34. Legacy WinForms migration objective

DynamicUI24 should allow TS24 teams to replace common legacy WinForms editor usage with semantic metadata rather than porting control-by-control code.

Migration target:

```text
Legacy Form
Label + TextEdit + ComboBoxEdit + DateEdit + LookUpEdit + validation code
        ↓
Semantic field/editor metadata + providers
        ↓
DynamicUI24 Universal Editor Foundation
```

Do not blindly preserve legacy event-driven UI coupling.

Preserve business truth and user capability; replace obsolete UI architecture.

---

## 35. Capability benchmark — required v0.13 foundation

v0.13 Task 10G must provide or establish tested seams for:

### Required implementation

- Text;
- Multiline Text;
- Integer;
- Decimal;
- Currency;
- Percentage;
- Boolean;
- Date;
- Time;
- DateTime;
- Choice/ComboBox;
- Lookup;
- SearchLookup/AutoSuggest basic provider path;
- ButtonEdit semantic embedded action;
- Hyperlink;
- Password/Secret presentation;
- label/placeholder/helper/error;
- formatting/parsing;
- required/range/length/pattern validation;
- native selection/edit commands;
- context menu;
- Unicode/native IME;
- localization/theme/accessibility;
- P1/permission;
- HelpContextCode;
- deterministic editor resolver.

### May be capability seam / deferred richer implementation

- MultiChoice;
- DateRange;
- TreeLookup;
- advanced token/chip editor;
- rich RTF editor;
- color/image editors;
- spell checker;
- advanced mask designer;
- advanced rich tooltip authoring.

Deferred items must be reported honestly as unsupported/partial rather than simulated.

---

## 36. Non-goals of v0.13 / Task 10G

Task 10G must not become:

- a full Form Runtime;
- a new Grid engine;
- a Report engine;
- a formula engine;
- a business validation engine;
- a database/query engine;
- a DevExpress WinForms compatibility wrapper;
- an RTF/Word processor;
- an arbitrary scripting engine;
- a workflow engine;
- a second command/menu system.

---

## 37. Task 10G acceptance surface

A neutral Editor Demo must physically expose representative editors in one discoverable workspace.

At minimum the user must be able to test:

1. Text Unicode input.
2. Vietnamese composed input.
3. Japanese/Korean/other IME-ready native ownership where environment permits.
4. Emoji.
5. Multiline text.
6. Integer.
7. Decimal.
8. Currency.
9. Percentage.
10. Boolean.
11. Date.
12. Time/DateTime.
13. ComboBox/Choice.
14. Lookup.
15. SearchLookup/AutoSuggest.
16. ButtonEdit.
17. Hyperlink.
18. Password masking.
19. Placeholder/helper text.
20. Required validation.
21. range/length/pattern validation.
22. error indicator/text.
23. formatting/parsing.
24. Undo/Cut/Copy/Paste/Delete/Select All.
25. native caret/selection.
26. context menu.
27. read-only.
28. disabled.
29. runtime vi-VN/en-US.
30. System/Light/Dark.
31. accessibility basics.
32. P1-safe behavior.
33. clean exit.

---

## 38. Integration acceptance

After Task 10G foundation is accepted, Task 11 must migrate Report Parameters to consume the shared editor foundation before Task 11 is finalized.

At least one existing DataEntry edit path should demonstrate compatible adoption or a documented migration seam without regressing accepted 10F behavior.

Do not destabilize DataEntry merely to force immediate internal code reuse; preserve accepted behavior and migrate through a bounded explicit integration step.

---

## 39. Architecture guards

Architecture tests must guard at least:

- no DevExpress/WinForms/Avalonia types in Core editor contracts;
- one editor resolver/foundation;
- no Report-specific second editor engine after integration;
- no Form-specific editor engine;
- native IME ownership not intercepted by parent keyboard routing;
- semantic identity, not visual index/title;
- lookup providers bounded and generation-safe;
- P1 cannot be bypassed by lookup/helper/error/accessibility;
- no arbitrary executable validation scripts;
- no 100K editor materialization;
- no business-specific rules in DynamicUI24 Core.

---

## 40. Focused test requirements

Task 10G should include focused coverage for:

- default editor resolution by value type;
- explicit compatible override;
- incompatible resolution safe failure;
- Unicode round-trip;
- composition-safe text update boundary;
- typed numeric/date parsing;
- culture-aware formatting;
- null/required behavior;
- validation severity/result;
- read-only/disabled capability;
- clipboard permission;
- P1 masking;
- lookup generation rejection;
- Company switch rejection;
- bounded lookup window;
- no full lookup enumeration;
- embedded semantic action;
- runtime localization without semantic mutation;
- theme change without value mutation;
- accessibility safe text;
- editor rematerialization preserving runtime value;
- DataEntry integration regression;
- Report Parameter integration regression when adopted.

---

## 41. Documentation requirements

Task 10G must add focused subsystem documentation covering:

- ownership;
- non-ownership;
- semantic identity;
- editor families;
- resolver;
- native input/IME boundary;
- formatting/parsing;
- validation;
- lookup provider;
- P1;
- localization/theme/accessibility;
- lifecycle/performance;
- consumer adoption;
- common failure modes;
- focused test commands.

A future AI agent should not need the entire repository to safely modify one editor family.

---

## 42. Specification versioning and adoption

v0.12 remains immutable historical authority for completed work.

Adoption procedure for v0.13:

1. Verify repository baseline, branch, `HEAD`, `origin/main` and clean worktree.
2. Verify Charter v0.2 SHA.
3. Verify v0.12/v0.11/v0.10/v0.9 SHA values.
4. Create `docs/specification/DynamicUI24-Spec-v0.13.md`.
5. v0.13 must preserve v0.12 and add this Universal Editor Foundation specification.
6. Run `git diff --check`.
7. Commit the specification as a dedicated spec commit.
8. Push only after explicit human approval.
9. Wait for authoritative CI GREEN.
10. Record final v0.13 SHA-256.
11. Only then GO Task 10G from the new baseline.
12. Task 11 remains HOLD until Task 10G is accepted and the Report Parameter integration is migrated.

Do not modify v0.12 in place.

---

## 43. Required authoritative hashes before adoption

Expected existing hashes:

```text
DynamicUI24 Architecture Charter v0.2
415d53271b6681cdd9d617e4ab751e7316e03816f736df97b5425c37620420cc

DynamicUI24 Spec v0.12
66cfdd715e4a8726f03b9ecfb06d08eac169a3e724c8d97cc455f42aa54434fb

DynamicUI24 Spec v0.11
5eb9de1cd43db592234f191dc78abb60cd7d26ad790289f0e941da6ab694d5c2

DynamicUI24 Spec v0.10
00a8d6a4a02a6b0152d171133b392861f3c2d38ebd05b421b2aad67fc1137c42

DynamicUI24 Spec v0.9
af99f4adf9bb4004a70c8c7d920e84894bc5aa62d5dd0ac62c329b27b94e4a0a
```

If any existing authoritative hash differs, STOP and investigate before adopting v0.13.

---

## 44. Governing v0.13 rule

> DynamicUI24 applications should describe a field's semantic value and required capabilities, not repeatedly hand-build TextBox, ComboBox, DatePicker, Lookup and validation behavior.

> One Universal Editor Foundation serves DataEntry, Reports, Filters, Forms, Setup/Configuration and future field-based surfaces while native OS input, application business truth, provider ownership, privacy and cross-platform boundaries remain authoritative.

---

**End of DynamicUI24 Specification v0.13 — Universal Editor Foundation**
