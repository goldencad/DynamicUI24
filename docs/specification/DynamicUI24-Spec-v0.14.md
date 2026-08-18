# DynamicUI24 Specification v0.14 — Developer UI Authoring + Dynamic Feature Authorization

**Document type:** Versioned DynamicUI24 specification amendment  
**Target authoritative version:** v0.14  
**Revision basis:** DynamicUI24 Spec v0.13 + Developer UI Authoring / Configuration + Dynamic Feature/UI Authorization  
**Architecture authority:** `DynamicUI24-ARCHITECTURE-CHARTER.md` v0.2  
**Charter SHA-256:** `415d53271b6681cdd9d617e4ab751e7316e03816f736df97b5425c37620420cc`  
**Previous authoritative specification:** `docs/specification/DynamicUI24-Spec-v0.13.md`  
**Previous v0.13 SHA-256:** `4c26fa08960d8e40bbf390664dbe5d30aba891dec1f79aa265c390079089fccb`

> v0.14 is additive. All requirements and invariants of v0.13 remain authoritative unless this document explicitly extends them.
> v0.14 defines one coherent Task 10H foundation combining Developer UI Authoring / Configuration and Dynamic Feature/UI Authorization.

---

## 1. Purpose

DynamicUI24 must allow application developers/administrators to define the initial UI of an application through semantic metadata while normal users receive only the published, authorized runtime UI.

The same foundation must support dynamic feature authorization so UI visibility, enablement, editability and available actions can be resolved at runtime from application security/capability policy.

The intended model is:

```text
Framework Defaults
        ↓
Developer / Application Definition
        ↓
Dynamic Authorization / Capability Resolution
        ↓
User Preference Overlay
        ↓
Rendered Runtime UI
```

Authorization and policy form the hard security ceiling.

---

## 2. Task 10H scope

Task 10H combines:

1. Developer UI Authoring / Configuration Foundation.
2. Dynamic Feature/UI Authorization Foundation.
3. Definition validation and preview.
4. Draft / Publish / Version lifecycle.
5. Runtime application of published UI definitions.
6. Permission/capability bindings.
7. Safe user-preference overlays.
8. Auditability and deterministic repair.

Task 10H is one coherent foundation task.

---

## 3. Core principle

DynamicUI24 should provide the functional breadth required to replace mature WinForms application UI while preserving a minimal, modern runtime experience.

Developer complexity belongs in Dev/Authoring Mode.

Normal users should see only the UI required for their task and authorization context.

```text
Rich capability underneath
        ↓
Semantic configuration
        ↓
Authorization
        ↓
Progressive disclosure
        ↓
Calm runtime UI
```

---

## 4. Developer Mode versus User Runtime

Developer/Authoring Mode is distinct from normal runtime.

Developer Mode may expose:

- semantic UI tree;
- metadata properties;
- layout configuration;
- editor configuration;
- command bindings;
- permission/capability bindings;
- default preferences;
- preview;
- validation;
- publish/version actions.

Normal users must not see Developer Mode unless explicitly authorized.

Normal runtime does not expose designer chrome merely because metadata was authored with it.

---

## 5. Developer Mode is not a scripting engine

Developer Mode authors metadata and semantic bindings.

It must not become:

- arbitrary C# execution;
- arbitrary SQL execution;
- JavaScript/VBA/macros;
- reflection-based plugin execution;
- hidden business workflow scripting;
- a second formula engine.

Allowed authoring concepts include:

- semantic IDs;
- layout;
- visibility;
- editor definitions;
- command references;
- provider references;
- validation references;
- permission/capability references;
- HelpContextCode;
- presentation defaults.

Business logic remains application/provider owned.

---

## 6. Definition lifecycle

UI definitions follow an explicit lifecycle:

```text
Draft
  ↓ Validate
Preview
  ↓ Publish
Published Definition Version N
  ↓
Runtime
```

Published definitions are immutable/read-focused.

Editing a published definition creates a new draft/revision rather than mutating active runtime metadata in place.

Rollback to a previously valid published version must be possible at the definition-management layer.

---

## 7. Semantic UI definition identity

Introduce/reuse stable semantic identities such as:

- `UiDefinitionCode`
- `UiDefinitionVersion`
- `WorkspaceCode`
- `FeatureCode`
- `CommandCode`
- `MenuCode`
- `GridCode`
- `SheetCode`
- `VariableCode`
- `FormCode`
- `FieldCode`
- `ReportCode`
- `EditorCode`
- `PermissionCode`
- `CapabilityCode`
- `PolicyCode`
- `HelpContextCode`

Localized labels, current visual positions and control instances are never durable identity.

---

## 8. UI definition model

A published application UI definition may contain semantic configuration for:

- Shell/workspaces;
- Ribbon tabs/groups/commands;
- menus;
- action bars;
- forms/fields;
- editors;
- grids;
- columns;
- row/column actions;
- reports;
- parameters;
- filters;
- context panels;
- dialogs;
- notifications;
- document surfaces;
- default layout;
- theme/density policy;
- HelpContextCode;
- permission/capability requirements.

The model should compose smaller immutable definitions rather than one giant mutable object.

---

## 9. Framework defaults

DynamicUI24 supplies safe framework defaults.

Examples:

- default editor resolution;
- default column width;
- default row height;
- default responsive behavior;
- default command placement;
- default theme behavior;
- default empty/loading/error presentation.

Applications override only what they need.

Missing metadata must resolve to deterministic safe defaults.

---

## 10. Application definition

Application developers configure the intended initial UX.

Examples:

```text
Workspace: Employees
VisibleByDefault: true
Permission: HR.EMPLOYEE.VIEW

Field: EmployeeCode
Editor: SearchLookup
Required: true
HelpContextCode: HR.EMPLOYEE.CODE

Grid Column: Salary
Width: 125%
Permission: HR.SALARY.READ
UserCanHide: false
```

Definitions express intent, not control-instance manipulation.

---

## 11. Dynamic Feature Authorization

Introduce/reuse a semantic authorization resolver.

Conceptually:

```text
FeatureCode
PermissionCode
CapabilityCode
PolicyCode
User/Role/Application Context
Company Context
        ↓
IUiAuthorizationResolver
        ↓
UiAuthorizationResult
```

DynamicUI24 consumes application/security decisions; it does not own enterprise identity or role truth.

---

## 12. Authorization result

A UI authorization result may resolve semantic capabilities such as:

- Visibility;
- Enabled;
- ReadOnly;
- Editable;
- CanExecute;
- CanOpen;
- CanCopy;
- CanPaste;
- CanClear;
- CanFind;
- CanFilter;
- CanSort;
- CanGroup;
- CanExport;
- CanPrint;
- CanReveal;
- CanConfigure;
- CanPersonalize;
- CanDrillDown.

Do not assume one boolean permission can describe every feature.

---

## 13. Canonical presentation states

Common resolved states:

```text
HIDDEN
DISABLED
READ_ONLY
ENABLED
```

Applications may supply finer capability decisions.

`READ_ONLY` and `DISABLED` are not interchangeable.

A hidden feature must not leak through Search, Quick Access, Help, accessibility, menus, shortcuts or diagnostics.

---

## 14. Authorization is not security implementation

Hiding or disabling UI is not sufficient authorization.

Backend/provider/application APIs must enforce authoritative security independently.

Correct model:

```text
Application / Backend Authorization
        ↓
Semantic Capability Result
        ↓
DynamicUI24 Presentation Resolution
        ↓
Hide / Disable / ReadOnly / Enable
```

Incorrect model:

```text
Button hidden = secure
```

DynamicUI24 must never grant permission.

---

## 15. Dynamic context

Authorization may change when context changes, including:

- current user;
- role/claims;
- Company;
- workspace;
- selected semantic object;
- feature state;
- licensing/capability;
- platform capability;
- application policy.

Context change invalidates stale authorization results.

Late authorization result for old context must not overwrite current UI state.

---

## 16. FeatureCode

`FeatureCode` is a stable semantic identity for an application capability exposed through UI.

Examples:

```text
EMPLOYEE.VIEW
EMPLOYEE.EDIT
REPORT.RUN
REPORT.EXPORT
DOCUMENT.OPEN
SETTINGS.MANAGE
```

DynamicUI24 Core must not hard-code TS24 business feature names.

Applications define actual codes.

---

## 17. Permission and capability separation

`PermissionCode` represents application/security authorization input.

`CapabilityCode` may represent functional availability.

Examples:

- user may have permission to export;
- current provider may not support PDF;
- platform may not support a specific native feature.

Final UI state must combine authorization and capability honestly.

---

## 18. Policy resolution

`PolicyCode` may apply additional application/framework presentation policy.

Examples:

- sensitive data reveal policy;
- export policy;
- user-personalization policy;
- administrative configuration policy.

Privacy, authorization and capability remain distinct concerns.

---

## 19. Precedence

Runtime resolution must be deterministic.

Conceptually:

```text
Framework Default
        ↓
Published App Definition
        ↓
User Preference
        ↓
Authorization / Policy Ceiling
        ↓
Platform Capability
        ↓
Final Rendered State
```

Authorization/policy/capability may restrict lower layers.

User preferences must never resurrect unauthorized or unsupported UI.

---

## 20. User preference overlay

Users may personalize only explicitly allowed properties.

Possible preference areas:

- column order;
- column widths;
- hidden visible-eligible columns;
- density;
- panel widths;
- active workspace/sheet;
- saved view state;
- Find scope.

Preferences do not modify published definitions.

Preferences never grant permission.

---

## 21. Developer controls personalization policy

Developer metadata may declare:

- `UserCanHide`
- `UserCanReorder`
- `UserCanResize`
- `UserCanPin`
- `UserCanSaveView`
- `UserCanConfigure`
- equivalent semantic personalization capabilities.

Normal runtime must honor these limits.

---

## 22. Authorization versus P1 privacy

Authorization and P1 remain separate.

A user may be authorized to access a field while privacy presentation masks it in a particular context.

Likewise privacy OFF cannot grant unauthorized access.

Final presentation must satisfy both systems.

---

## 23. P1 propagation

Dynamic authorization and UI definitions must not leak protected data through:

- labels derived from sensitive content;
- menus;
- Search/S1;
- Quick Access;
- Context Panel/S2;
- tooltips;
- validation text;
- accessibility;
- diagnostics;
- preferences;
- authoring preview where developer lacks reveal permission.

Fail closed.

---

## 24. Authoring permission

Developer Mode itself requires explicit capability.

Examples:

- `CanOpenUiAuthoring`
- `CanEditUiDefinition`
- `CanPublishUiDefinition`
- `CanRollbackUiDefinition`
- `CanEditAuthorizationBindings`

These are application-level permissions/capabilities.

Do not expose Dev Mode based on a local hidden shortcut alone.

---

## 25. Authoring UX

The initial Developer UI Authoring workspace should remain modern and minimal.

Recommended composition:

```text
Semantic UI Tree / Search
        |
        |---- Preview
        |
        `---- Properties / Definition Inspector
```

Prefer:

- semantic selection;
- searchable properties;
- grouped sections;
- progressive disclosure;
- reusable Editor Foundation;
- live preview;
- clear validation.

Avoid a dense legacy property-grid clone unless needed as an optional advanced surface.

---

## 26. Preview

Developer preview renders the current draft definition without publishing it.

Preview must:

- use the same runtime renderer where practical;
- clearly indicate Draft/Preview state;
- respect developer's own authorization/privacy;
- not persist business data merely by previewing;
- not become authoritative runtime metadata.

---

## 27. Validation before publish

A definition cannot publish when critical validation fails.

Validate at least:

- duplicate semantic IDs;
- missing referenced semantic objects;
- invalid command references;
- invalid editor definitions;
- invalid permission/capability references where resolvable;
- invalid layout metadata;
- invalid provider references;
- incompatible feature combinations;
- unsafe circular metadata relationships where applicable.

Warnings may remain publishable only according to policy.

---

## 28. Deterministic repair

Runtime must safely repair non-critical stale metadata/preferences.

Examples:

- removed column preference ignored;
- unavailable command hidden/disabled;
- removed editor type falls back safely;
- invalid width resets to default;
- unauthorized saved view cannot resurrect feature;
- stale workspace order repairs deterministically.

No crash.

---

## 29. Versioning

Published UI definitions carry explicit version identity.

Conceptually:

```text
UiDefinitionCode
UiDefinitionVersion
PublishedAt
SchemaVersion
```

Runtime should be able to identify which definition version produced a UI.

Do not use localized title or timestamp as identity.

---

## 30. Migration

UI-definition schema changes require explicit migration/repair rules.

Older published definitions must fail safely or migrate deterministically.

Do not silently reinterpret metadata in a way that changes business meaning.

---

## 31. Audit

Definition lifecycle operations should support audit metadata such as:

- draft created;
- validation result;
- published;
- rolled back;
- author identity supplied by application context;
- version;
- timestamp;
- safe change summary.

DynamicUI24 provides generic audit hooks/contracts; application storage owns authoritative audit persistence.

---

## 32. Runtime cache

Published metadata/authorization may be cached only with bounded, context-aware semantics.

Cache keys must include appropriate:

- definition version;
- user/security context generation;
- Company/context generation;
- policy generation.

Stale cache results must not survive authoritative context changes.

---

## 33. Feature authorization in Shell

Authorization applies to:

- Ribbon tabs/groups;
- menu items;
- commands;
- workspace navigation;
- Backstage;
- Quick Access;
- Search/S1 results;
- shortcuts.

A hidden command must not remain executable through an alternate surface unless policy explicitly allows that alternate capability.

---

## 34. Feature authorization in Editors/Forms

Authorization may resolve fields to:

- Hidden;
- Disabled;
- ReadOnly;
- Editable.

Editor Foundation remains responsible for presentation/editor behavior.

Task 10H does not create a second editor system.

---

## 35. Feature authorization in Grid/DataEntry

Authorization may control:

- Grid availability;
- column visibility;
- INPUT edit capability;
- copy;
- paste;
- clear;
- Find;
- Filter;
- Sort;
- row actions;
- export;
- reveal.

Existing `RowKey + VariableCode` identity remains authoritative.

No visual-index authorization.

---

## 36. Feature authorization in Report

Authorization may control:

- report visibility;
- Run;
- parameter edit;
- Find;
- grouping;
- drill-down;
- export scope/format;
- print/output.

`ReportCode`, `ReportParameterCode`, `ReportColumnCode` remain semantic identity.

---

## 37. Feature authorization in Context Panel / S2

S2 content must be re-resolved through authorization/policy whenever context changes.

A previously visible detail cannot remain stale after permission/company/context changes.

---

## 38. Search / S1

Global Search/Command Palette must respect dynamic authorization.

Unauthorized features must not leak through:

- result title;
- description;
- shortcut;
- ranking metadata;
- recent history.

Authorization change invalidates stale search results.

---

## 39. Quick Access / Recent

Pinned/Recent items store semantic IDs only.

At render time they re-resolve:

- current definition;
- permission;
- capability;
- privacy.

A previously pinned feature may disappear safely when no longer authorized.

---

## 40. Help

HelpContextCode is subject to feature visibility and privacy policy.

Help must not expose hidden feature names or sensitive field semantics to unauthorized users.

---

## 41. Localization

Authoring definitions store localization identities/semantic resource keys, not only one current language string.

Runtime vi-VN/en-US and future cultures must not change semantic identity.

Developer preview may switch cultures without mutating definition semantics.

---

## 42. Unicode/native input

Developer Mode and runtime configuration surfaces use Universal Editor Foundation and Charter v0.2 native Unicode/IME rules.

No language-specific input engine is introduced by Task 10H.

---

## 43. Theme / density / scale

Authoring preview should support existing System/Light/Dark and relevant responsive/scale checks.

Theme is presentation, not definition identity.

Application may define default theme policy, but runtime/user policy remains governed by existing framework settings architecture.

---

## 44. Responsive definition

Developer metadata may provide responsive presentation hints.

Avoid encoding pixel-perfect screen coordinates as semantic truth.

Prefer:

- sections;
- groups;
- relative sizing;
- min/max width;
- priority;
- overflow;
- collapsibility.

DynamicUI24 renderer owns responsive mechanics.

---

## 45. Actipro reuse

At the Avalonia presentation layer, Task 10H should audit and reuse mature Actipro Avalonia controls where they clearly fit.

Potential examples:

- SettingsCard;
- SettingsGroup;
- SettingsExpander;
- AdvancedTabControl;
- InfoBar;
- UserPrompt;
- Docking/MDI seam where relevant to future work.

Core remains vendor-neutral.

Do not reimplement mature Actipro functionality without architectural reason.

---

## 46. DevExpress benchmark principle

DevExpress WinForms may be used as a capability benchmark for legacy migration.

Do not clone its control catalog or object model.

Task 10H should preserve DynamicUI24's minimal modern runtime UX while providing sufficient configuration capability underneath.

---

## 47. Modern runtime design rule

Functional breadth should not cause permanent visual clutter.

Use:

- progressive disclosure;
- contextual actions;
- clean Action Bars;
- compact menus;
- optional Context Panel;
- searchable authoring;
- sensible defaults;
- whitespace.

Normal runtime should remain visually calm even when the framework supports many features.

---

## 48. Definition storage seam

DynamicUI24 defines vendor-neutral definition repository contracts where needed.

Application decides authoritative storage technology.

Core must not require:

- MariaDB;
- MongoDB;
- filesystem;
- cloud;
- Odoo;
- a particular API.

---

## 49. Publish atomicity

Publishing a definition should produce one coherent version.

Runtime must not observe half-published metadata.

Application repository/adoption layer owns transactional persistence; DynamicUI24 contracts must allow atomic version activation.

---

## 50. Runtime activation

A runtime activates one authoritative published definition version for the relevant app/context.

Switching version must:

- invalidate stale definition-dependent state;
- repair preferences;
- re-resolve authorization;
- preserve business data;
- avoid cross-context leakage.

---

## 51. Performance

Developer metadata must not cause unbounded runtime work.

Opening normal runtime must not:

- instantiate controls for hidden/unmaterialized large datasets;
- evaluate every permission for every logical row synchronously;
- materialize 100K rows;
- scan all business data merely to author layout.

Authorization should resolve at appropriate semantic granularity and cache safely where possible.

---

## 52. Async authorization

If application authorization resolution is asynchronous:

- use generation/context protection;
- provide safe pending state;
- fail closed;
- late stale result ignored.

Do not display protected UI optimistically unless policy explicitly permits it.

---

## 53. Authorization failure

Provider/resolver failure must not crash Shell.

Protected/uncertain features fail closed.

Use bounded diagnostics and safe retry where meaningful.

---

## 54. Developer definition failure

Invalid draft metadata must not crash preview/runtime.

Preview may show a safe error marker/diagnostic attached to the semantic definition node.

Published runtime only uses validated published definitions.

---

## 55. Task 10H Demo

Create a neutral Developer UI Authoring Demo.

It should allow an authorized developer persona to configure a small application definition containing:

- Workspace;
- Ribbon/commands;
- Form fields;
- EditorDefinitions;
- Grid columns;
- Report entry;
- default layout;
- Help;
- permission/capability bindings.

Provide a separate normal-user runtime preview.

Business-neutral only.

---

## 56. Authorization Demo

Demonstrate at least three neutral runtime authorization profiles, for example:

```text
Viewer
Editor
Administrator
```

These names are Demo-only and not Core role semantics.

Demonstrate:

- hidden workspace;
- disabled command;
- read-only field;
- editable field;
- hidden Grid column;
- denied export;
- permitted export;
- denied developer configuration;
- permitted developer configuration.

---

## 57. Dynamic authorization change Demo

Demonstrate runtime context change without application restart where architecture allows.

Examples:

- switch Demo profile;
- switch Company/context;
- capability becomes unavailable.

UI must re-resolve safely without stale feature leakage.

---

## 58. User preference Demo

Show that user preference can personalize eligible UI but cannot override authorization.

Example:

1. User chooses to show Column A.
2. Authorization later denies Column A.
3. Column A disappears.
4. Authorization later permits it.
5. Preference may become effective again according to policy.

No raw sensitive metadata is persisted.

---

## 59. Draft / Preview / Publish Demo

Developer workflow:

```text
Open Developer Mode
→ edit draft metadata
→ validate
→ preview
→ publish version
→ normal runtime uses published version
```

Preview and Publish must be visibly distinct.

---

## 60. Rollback seam

Task 10H establishes a generic rollback seam.

Full production repository/storage UX may be application-adopted, but contracts must support activating a previous valid published definition version.

Rollback never rewrites historical version identity.

---

## 61. Architecture guards

Architecture tests must prove at least:

1. Core authoring contracts are Avalonia-free.
2. Core authoring contracts are Actipro-free.
3. Core authoring contracts are DevExpress-free.
4. semantic IDs are authoritative.
5. no arbitrary scripts.
6. no SQL engine.
7. no formula engine.
8. authorization resolver cannot grant backend authority.
9. user preferences cannot override denied capability.
10. hidden feature does not leak through S1.
11. P1 remains separate and authoritative.
12. stale context authorization ignored.
13. published definition is immutable/read-focused.
14. draft != published runtime definition.
15. normal user cannot access Developer Mode without capability.
16. no business-specific roles/codes in Core.
17. Editor Foundation is reused.
18. no duplicate menu/command/navigation systems.
19. metadata changes do not materialize large datasets.
20. version activation repairs preferences safely.

---

## 62. Behavioral tests — definition lifecycle

Cover:

- draft creation;
- validation;
- preview;
- publish;
- new version;
- rollback seam;
- immutable published definition;
- invalid definition rejection;
- semantic reference validation.

---

## 63. Behavioral tests — authorization

Cover:

- Hidden;
- Disabled;
- ReadOnly;
- Enabled;
- permission denied;
- capability unavailable;
- policy restriction;
- stale async result;
- Company switch;
- dynamic context rerender.

---

## 64. Behavioral tests — preference precedence

Cover:

- default;
- application definition;
- user preference;
- authorization ceiling;
- platform capability;
- stale preference repair;
- unauthorized preference resurrection prevention.

---

## 65. Behavioral tests — Shell

Cover:

- workspace visibility;
- Ribbon command visibility;
- command enablement;
- S1 filtering;
- Quick Access re-resolution;
- Help visibility.

---

## 66. Behavioral tests — Editor/Form seam

Cover:

- hidden field;
- read-only editor;
- editable editor;
- developer-configured EditorDefinition;
- Unicode/native input remains intact;
- validation unaffected by authorization presentation.

---

## 67. Behavioral tests — Grid/Report seams

Cover:

- column visibility;
- copy/export capability;
- report Run permission;
- report export permission;
- semantic identity retained;
- no visual-index rules.

---

## 68. Behavioral tests — P1

Cover:

- authorized but masked;
- unauthorized and hidden;
- preference cannot reveal;
- Search cannot leak;
- accessibility cannot leak;
- authoring preview cannot bypass developer privacy policy.

---

## 69. Performance tests

Cover:

- bounded definition load;
- no 100K visual materialization;
- authorization resolution does not enumerate 100K rows;
- preference repair bounded;
- dynamic re-resolution only affects relevant semantic surfaces.

Avoid arbitrary microsecond thresholds unless existing benchmark policy defines them.

---

## 70. Documentation

Task 10H should add focused non-authoritative documentation such as:

- `docs/architecture/UI-AUTHORING.md`
- `docs/architecture/UI-AUTHORIZATION.md`
- `docs/architecture/UI-DEFINITION-LIFECYCLE.md`
- `docs/architecture/UI-PREFERENCE-PRECEDENCE.md`
- `docs/adoption/UI-DEFINITIONS.md`
- `docs/adoption/UI-AUTHORIZATION.md`
- `docs/design-system/DEVELOPER-MODE.md`
- `docs/backlog/TASK-10H-BACKLOG.md`

Avoid contradictory duplicates.

---

## 71. Local-AI maintainability

Documentation must state:

- what Developer UI Authoring owns;
- what it does not own;
- semantic identities;
- Draft/Preview/Publish model;
- authorization boundary;
- precedence;
- P1 relationship;
- user preference rules;
- runtime activation;
- storage seam;
- failure/repair rules;
- focused test commands;
- common failure modes.

---

## 72. Non-goals

Task 10H must not become:

- full business IDE;
- arbitrary code designer;
- SQL/query designer;
- formula designer/engine;
- workflow engine;
- database schema designer;
- full visual pixel-perfect designer;
- endpoint security system;
- identity provider;
- role-management product;
- DevExpress clone;
- WinForms Designer clone.

---

## 73. Task ordering

Required roadmap:

```text
Task 10G — Universal Editor Foundation
        ↓ CLOSED

Task 10H — Developer UI Authoring + Dynamic Feature Authorization
        ↓ CLOSED

Task 11 — restore/migrate Report WIP and continue
```

Task 11 remains HOLD during 10G and 10H unless explicitly re-planned by the architect.

---

## 74. Specification adoption

v0.13 remains immutable historical authority.

Adoption procedure for v0.14:

1. Verify clean repository baseline.
2. Verify Charter v0.2 SHA.
3. Verify v0.13/v0.12/v0.11/v0.10/v0.9 hashes.
4. Create `docs/specification/DynamicUI24-Spec-v0.14.md`.
5. Run `git diff --check`.
6. Commit as a dedicated specification/governance commit.
7. Push only after human approval.
8. Wait for CI GREEN.
9. Record authoritative v0.14 SHA-256.
10. Task 10G continues under v0.14 only if the specification is adopted before 10G feature commit.
11. Task 10H must not begin until Task 10G is CLOSED.

---

## 75. Current authoritative hashes before v0.14 adoption

```text
DynamicUI24 Architecture Charter v0.2
415d53271b6681cdd9d617e4ab751e7316e03816f736df97b5425c37620420cc

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

If any existing authoritative hash differs, STOP.

---

## 76. Governing v0.14 rule

> Developers define the intended application UI and bind semantic features to permissions/capabilities; DynamicUI24 resolves the published definition, authorization, user preferences and platform capabilities into a clean runtime UI.

> Normal users see only the features they are allowed and need to use. Developer authoring complexity remains hidden from ordinary runtime.

> User personalization can refine permitted presentation but can never override authorization, privacy, application policy or platform capability.

---

**End of DynamicUI24 Specification v0.14 — Developer UI Authoring + Dynamic Feature Authorization**
