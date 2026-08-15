# TS24 Dynamic UI Framework — Specification v0.8

**Status:** Draft
**Version:** 0.8
**Initial Consumer:** PayCalc24
**Purpose:** Reusable dynamic UI framework for TS24 desktop applications built on Avalonia/Actipro.
**Source context:** PayCalc24 product requirements and the UI architecture decisions derived from them.


## 0. Version History

### v0.8

Formalizes verified shared UX/design-system capabilities introduced through Tasks 8–9
and adds the **Dynamic Notification & Guidance System** as a first-class shared
Shell/Foundation capability.

Major additions:

1. **Shared UX / Design-System Formalization**
   - reusable resizable split-navigation layout;
   - whole-row Tree interaction states: Normal, Hover, Selected, Selected+Hover,
     Disabled, KeyboardFocus;
   - Tree overflow behavior with `See more / Xem thêm` and `Show less / Thu gọn`;
   - metadata-driven `BUTTON`, `DROPDOWN_BUTTON`, `SPLIT_BUTTON`, `ICON_BUTTON`,
     and `TOGGLE_BUTTON`;
   - action-menu metadata, groups/separators, bounded submenu depth, and safe command
     dispatch;
   - XS/Small/Medium/Large/XL action geometry, bounded width/height overrides,
     typography, icon size/position, padding, and gap;
   - global UI/font scaling combined with component tokens without losing runtime state;
   - `IconKey` remains the public contract while SVG resources and font glyphs are
     resolved behind the icon registry;
   - consumer customization is expected through metadata, semantic tokens, registries,
     providers, and extension points rather than direct modification of shared controls.

2. **Setup Metadata Contracts Clarification**
   - Master Catalog, Workspace, Column, Variable, and Formula metadata contracts;
   - `VariableCode` as a stable semantic identifier;
   - `INPUT`, `FORMULA`, and `SYSTEM` column modes;
   - published geometry remains separate from per-user presentation preferences;
   - Formula definitions remain declarative metadata only and never embed arbitrary
     executable code.

3. **Dynamic Notification & Guidance System**
   - `TOAST`, `BANNER`, `ALERT_CARD`, `BLOCKING_NOTICE`, and
     `NOTIFICATION_CENTER_ITEM`;
   - notification severity, progress, priority, deduplication, dismissal, lifecycle,
     cooldown/throttling, and expiration;
   - `GuidanceAction`, `NavigationTarget`, and semantic `FocusTarget`;
   - integration with existing workspace navigation, registered command dispatch,
     permission/capability presentation, Company Context, localization, themes, and
     accessibility;
   - provider-driven detection and state; DynamicUI24 owns generic normalization,
     resolution, presentation, and guidance rather than application business rules;
   - design principle: **detected state → explanation → actionable resolution →
     correct workspace/context**.

v0.8 is an additive evolution of the authoritative v0.7 repository specification.

Authoritative v0.7 lineage:

```text
docs/specification/DynamicUI24-Spec-v0.7.md
SHA-256: 54f891741b843eacc68e18a1b724b4c4ef6e8bc45df49f372965d96570904e1b
```

Historical specification files remain immutable. v0.8 does not modify or replace the
v0.7 artifact; it supersedes it as the next specification baseline after review and
commit.

### v0.7

Adds the formal **Supported Desktop Platform Matrix** and cross-platform release policy.

- Tier-1/P0 official targets: Windows x64, Ubuntu LTS x64, macOS Apple Silicon.
- Tier-2/P1 compatibility targets: Windows ARM64, macOS Intel.
- Tier-3/P2 future/best-effort: Linux ARM64.
- Every UI task from v0.7 forward validates five publish RIDs: `win-x64`, `win-arm64`, `osx-arm64`, `osx-x64`, `linux-x64`.
- Publish PASS is explicitly distinct from native GUI certification.
- Ubuntu LTS x64 is a strategic enterprise target so consuming applications may be deployed without requiring a Windows desktop license.
- Consumer applications inherit the DynamicUI24 platform matrix unless an explicit app-specific compatibility exception is documented.
- Tasks 0–4 remain CLOSED and are retrofitted through a single maintenance task rather than reopened individually.

### v0.6

Adds two major framework capabilities:

1. **Dynamic Action Bars**
   - every template may expose metadata-driven Top and Bottom Action Bars;
   - actions can be added, reordered, localized, permission-gated, selection-aware, and bound to registered commands;
   - action bars remain presentation/dispatch-only and never contain business logic;
   - bottom bars may also present row count, selection count, validation/error count, and pending-change state.

2. **Signing / Approval Presentation Template**
   - introduces `TS24.DynamicUI.Template.Signing`;
   - provides UI-only presentation for signing and approval workflows;
   - supports document/history list, preview, actor/signing information, status, comments/reasons, timeline/history, and dynamic actions;
   - all signing/approval execution is delegated to consuming application APIs/providers;
   - the framework never handles private keys, PKCS#11, HSM operations, cryptographic signing, or authoritative workflow rules.

### v0.5

Refactors the framework into a **modular template architecture** so every template can
be developed, tested, versioned, replaced, and released independently.

Major changes:

- each template becomes its own module/project;
- a generic `IDynamicTemplate` contract and `TemplateRegistry` are introduced;
- template modules depend only on Core/Shared contracts and explicitly required extensions;
- applications resolve templates by metadata/`TemplateCode`, not by hard-coded view types;
- new template modules can be added later without modifying existing templates;
- module-level versioning, compatibility, tests, documentation, and change guides are required;
- shared capabilities are separated from full template modules to prevent template-to-template coupling;
- PayCalc24 remains a consumer/reference implementation, never a dependency of the framework.
- adds a reusable **Application Menu / App Shell Menu** for global app settings such as language, appearance, account/context, license, about, and exit;
- standard shell items remain framework-defined while app-specific settings can be added through registered contributors.
- Ribbon tabs, groups, and commands are metadata-driven and configurable through Setup;
- contextual Ribbon commands are resolved from current workspace/template context and application capabilities;
- Ribbon metadata performs presentation/navigation only and never embeds business logic.

### v0.4

Adds the fifth reusable template: **`DynamicDashboardTemplate`**.

Major capabilities:

- standard two-column Dashboard shell: Dynamic Tree / View Selector on the left, dynamic Dashboard Canvas on the right;
- metadata-driven parent/child dashboard tree;
- node selection resolves a dashboard definition without hard-coded views;
- reusable KPI cards, Pie/Donut, Column/Bar, Line/Area, Data Grid, Top-N, Status List, Progress, Text, Image, and Custom widget contracts;
- widget data binding through application-exposed variables/datasets;
- dashboard-wide Universal Time Filter context with explicit per-widget override where allowed;
- drill-down/navigation from chart segment, KPI, grid row, or status item;
- responsive widget sizing/order;
- empty/loading/error/permission-denied states per widget and per dashboard;
- refresh metadata and permission metadata;
- Dashboard remains presentation/query-only and does not become a calculation engine.

### v0.3

Extends the framework with three major capabilities:

1. **Visual XML Report / Document Designer**
   - user-designed report/form layouts;
   - variable/field catalog binding;
   - scalar and collection variables;
   - safe XML layout DSL;
   - template versioning;
   - preview and export;
   - reusable for payroll reports, tax declarations, invoices, forms, statements, contracts, and other document types.

2. **Universal Time Filter**
   - reusable date/time/period filtering on every applicable grid/template;
   - configurable primary time dimension;
   - date range, month, quarter, year, and period modes;
   - standard presets such as Today, This Month, This Quarter, This Year;
   - filter state remains presentation/query state only.

3. **Generic Batch Export / Share / Execute Framework**
   - multi-row/multi-document selection;
   - export selected;
   - share selected;
   - execute application-defined actions in batch;
   - progress and partial-failure presentation;
   - retry failed;
   - provider-driven actions rather than hard-coded domain behavior.

4. **Canonical XML Layout + Compiled Render Model**
   - XML is the canonical portable report/document layout format;
   - runtime preview/export uses a validated compiled render model;
   - compiled layouts may be cached by TemplateId + Version;
   - standard outputs include Screen Preview, PDF, Excel, and XML;
   - Layout XML and Business/Data XML remain distinct concepts.

### v0.2

Adds the fourth reusable template:

`DynamicHistoryDocumentTemplate`

and introduces:

- history/document grid;
- metadata search/filter;
- master/detail document inspection;
- exact document-version selection;
- document preview adapter boundary;
- PDF, DOC/DOCX, XML, JSON, text, image, and unsupported-format presentation strategy;
- capability-controlled preview/download/export;
- document audit/provenance presentation.

### v0.1

Initial specification with:
- DynamicSetupTemplate;
- DynamicDataEntryTemplate;
- DynamicReportTemplate;
- Dynamic Tree;
- Dynamic Columns;
- VariableCode;
- formula-column protection;
- Excel-like interaction;
- Excel/PDF import/export boundaries.

## 1. Objective

TS24 Dynamic UI Framework provides a reusable presentation framework for applications whose catalogs, data-entry grids, formulas, and reports are driven by metadata rather than hard-coded screens.

The framework standardizes six primary UI templates:

1. `DynamicSetupTemplate`
2. `DynamicDataEntryTemplate`
3. `DynamicReportTemplate`
4. `DynamicHistoryDocumentTemplate`
5. `DynamicDashboardTemplate`
6. `DynamicSigningTemplate`

Version 0.3 also standardizes three shared cross-template capabilities:

- `UniversalTimeFilter`
- `DynamicDocumentLayoutDesigner`
- `BatchActionFramework`

PayCalc24 is the first consumer. The framework must remain domain-neutral so it can later be reused by other TS24 applications.

The framework does **not** calculate payroll or other domain business logic. It renders metadata-driven UI, collects user input, invokes application/domain services, and presents returned results.

## 2. Core Principles

### 2.1 Metadata-driven

UI structure is generated from definitions stored/configured by the application.

The framework must not require source-code or XAML changes when an authorized user:
- adds a navigation node;
- creates a child node;
- adds a dynamic column;
- changes a display name;
- changes display order;
- enables/disables a column;
- changes an editor type;
- associates a variable with a formula;
- changes report columns/filter/grouping;
- changes effective dates or versions.

### 2.2 Presentation-only

The UI layer must never perform authoritative business calculations.

```text
UI metadata
   ↓
Dynamic UI
   ↓
VariableCode + values
   ↓
Application service
   ↓
Versioned formula/rule definition
   ↓
Business / Calculation Engine
   ↓
Result
   ↓
Dynamic UI renders result
```

Formula definitions may be authored through Setup UI and persisted in the application's data store, but formula execution occurs in the application/domain calculation engine.

### 2.3 Dynamic tree + dynamic workspace

```text
Dynamic Tree
   ↓ selected node
Workspace Definition
   ↓
Grid / Form / Designer / Wizard / Report
```

Tree depth is not fixed. Nodes may have arbitrary parent/child relationships according to configured metadata.

### 2.4 Stable variable identity

Dynamic data columns use a stable `VariableCode`.

`DisplayName` is presentation metadata and may change or be localized.

`VariableCode` is the canonical identity used by formulas, mappings, validation, integration, and import/export.

Example:

```text
VariableCode: NEW_CR
DisplayName.vi-VN: Doanh thu mới
DisplayName.en-US: New Revenue
```

Changing the display name must not break formulas.

### 2.5 Excel-like interaction with system control

The grid should feel familiar to users who work with Excel while preserving:
- typed data;
- validation;
- permissions;
- versioning;
- audit;
- formula protection;
- controlled commit.

## 3. Framework Scope

### Included

- dynamic navigation tree;
- dynamic columns;
- semantic variable metadata;
- editable/read-only/calculated column states;
- search;
- sorting;
- filtering;
- cell/range selection;
- multi-cell copy;
- multi-cell paste;
- keyboard navigation;
- batch candidate editing;
- validation;
- import/export;
- localization;
- Light/Dark/System theme support;
- semantic SVG/IconKey mapping;
- version/effective-date presentation;
- permission/capability binding;
- empty/loading/error/read-only states;
- report definition and preview shell;
- reusable dialogs and import wizard presentation;
- history/document grids;
- full-text/metadata search presentation;
- document detail/master-detail presentation;
- document preview adapter boundary for PDF, DOC/DOCX, XML, JSON, text, image, and other supported formats;
- version/audit/provenance presentation for historical records;
- configurable universal time filtering;
- visual XML document/report layout design;
- scalar/collection variable binding for layouts;
- metadata-driven batch export/share/execute;
- batch progress, partial-failure, retry, and selection-state presentation.
- metadata-driven dashboards with a standard Tree/View Selector + Dashboard Canvas layout;
- reusable dashboard widgets, shared time context, drill-down, refresh, and widget state presentation.
- metadata-driven Top/Bottom Action Bars across templates;
- UI-only signing/approval presentation with preview, status, comments, history, and registered command dispatch.
- company-scoped permission/capability presentation with `HIDE`, `DISABLE`, and `READ_ONLY` behaviors;
- runtime permission refresh when Company Context changes.
- large-data grid architecture for 100,000+ rows with virtualization/windowed loading and bounded memory behavior.
- global Appearance preferences and per-grid layout preferences for font scale, grid density, column width, row height, and layout reset.
- framework-wide design tokens and semantic SVG IconKey resolution with app-level brand overrides.

### Excluded

- payroll calculation;
- KPI calculation;
- attendance calculation;
- statutory calculation;
- domain-specific workflow logic;
- direct database access from UI;
- domain-specific formula execution;
- application-specific authorization rules;
- PDF OCR/extraction engine;
- ERP/provider implementation.

## 4. Project Structure

Recommended modular project structure:

```text
TS24.DynamicUI/
├─ src/
│  ├─ TS24.DynamicUI.Core
│  ├─ TS24.DynamicUI.Shared
│  │
│  ├─ Templates/
│  │  ├─ TS24.DynamicUI.Template.Setup
│  │  ├─ TS24.DynamicUI.Template.DataEntry
│  │  ├─ TS24.DynamicUI.Template.Report
│  │  ├─ TS24.DynamicUI.Template.HistoryDocument
│  │  ├─ TS24.DynamicUI.Template.Dashboard
│  │  └─ TS24.DynamicUI.Template.Signing
│  │
│  ├─ Extensions/
│  │  ├─ TS24.DynamicUI.Excel
│  │  ├─ TS24.DynamicUI.Reporting
│  │  ├─ TS24.DynamicUI.Documents
│  │  ├─ TS24.DynamicUI.Batch
│  │  └─ future extensions
│  │
│  └─ Hosting/
│     └─ TS24.DynamicUI.Avalonia
│
├─ samples/
│  └─ TS24.DynamicUI.Demo
│
├─ tests/
└─ docs/
```

### `TS24.DynamicUI.Core`

Domain-neutral metadata/contracts.

Must not depend on Avalonia, Actipro, PayCalc24, or database implementation.

Contains tree definitions, workspace definitions, column definitions, variable definitions, editor metadata, validation metadata, capability metadata, import/export contracts, localization metadata, version/effective-date metadata, and large-grid data-provider/query contracts.

### `TS24.DynamicUI.Shared`

Reusable presentation primitives and contracts shared by multiple template modules.

May contain:

- common grid primitives;
- common tree primitives;
- standard states;
- semantic IconKey contracts;
- localization helpers;
- theme abstractions;
- design token contracts;
- semantic IconKey contracts;
- brand override contracts;
- common dialogs;
- shared selection abstractions;
- shared time-filter abstractions;
- authorization-presentation abstractions;
- Company-scoped capability context contracts.

It must not contain a complete business/template workflow and must not depend on
PayCalc24 or any consuming application.

### `TS24.DynamicUI.Avalonia`

Generic Avalonia/Actipro hosting and registration layer.

Responsibilities:

- template module discovery/registration;
- common Avalonia host;
- shared editor factory;
- Ribbon/toolbar hosting contracts;
- System/Light/Dark token resource resolution;
- Actipro/Avalonia theme integration;
- IconKey/SVG resolution;
- app-brand resource overlay;
- template workspace host;
- application composition helpers.

It must not own the implementation details of all templates.

### Supported Platforms / Cross-Platform Release
- `win-x64` publish PASS;
- `win-arm64` publish PASS;
- `osx-arm64` publish PASS;
- `osx-x64` publish PASS;
- `linux-x64` publish PASS;
- Tier-1 release certification covers Windows x64, Ubuntu LTS x64, and macOS Apple Silicon;
- publish success is not treated as native GUI certification;
- platform-sensitive dependencies are validated/documented;
- Core/contracts remain platform-neutral;
- consumers inherit the matrix unless an explicit compatibility exception is documented;
- Tasks 0–4 use one Maintenance M1 retrofit task.

### Design Tokens / Semantic Icons
- reusable templates contain no hard-coded app-specific color values outside canonical theme resources;
- Light/Dark/System resolve the same semantic token contract;
- templates reference `IconKey` rather than direct SVG paths;
- app brand overlay can change logo/accent/icons without modifying template code;
- app-specific IconKeys can be registered without changing framework defaults;
- Grid, Ribbon, App Menu, Dashboard, Report, History/Document, Signing, dialogs, and states remain theme-aware;
- chart/status colors use semantic theme tokens and do not encode business logic by color alone.

### Appearance / Grid Preferences
- Application Menu exposes global Theme, UI Scale, Font Size, and Grid Density settings;
- Small/Normal/Large font modes update loaded templates consistently;
- Compact/Comfortable/Large grid density modes render correctly;
- users can resize grid columns;
- users can change row-height/density presentation;
- Auto Fit Column / Auto Fit All Columns work where supported;
- per-grid layout preferences can be persisted by stable User + Company + Workspace/Grid identity;
- published grid definition remains distinct from user presentation preferences;
- Reset UI Layout restores current published defaults;
- appearance changes recalculate viewport/prefetch geometry without full-dataset materialization.

### Large Data Grid / 100K+ Rows
- provider-based large-data mode supports at least 100,000 rows;
- 30+ dynamic columns can be defined for benchmark scenarios;
- row virtualization/windowed loading is active;
- viewport-driven loading materializes only visible/nearby buffered rows;
- direct scroll/jump to distant positions queries the target window without traversing intermediate rows;
- total logical row count remains represented by scrollbar/navigation;
- row identity, selection, candidate edits, and diagnostics survive viewport unload/reload;
- rapid scrolling does not allow stale viewport responses to overwrite newer requested windows;
- window/prefetch cache remains bounded;
- sorting/filtering/searching large datasets use provider/query boundaries;
- selection remains correct across virtualization;
- sparse edit buffer avoids cloning the full dataset;
- large multi-cell paste is chunked and does not overwrite protected Formula/System columns;
- large import/export operates independently of visual row materialization;
- long-running operations do not block the UI thread for the entire operation;
- benchmark shows bounded memory behavior and no routine crash;
- real macOS ARM64 and Windows x64 GUI performance smoke are required before final release acceptance.

### Company-Scoped Permission / Capability Presentation
- consuming application can provide an effective capability set for the active Company;
- the same user may receive different UI presentation in different Companies;
- Company switch refreshes effective capabilities without application restart;
- Ribbon, Tree, Workspace, Grid/Columns, Action Bars, Dashboard, Reports, History/Documents, and Signing UI re-resolve after context change;
- `HIDE`, `DISABLE`, and `READ_ONLY` behaviors are supported;
- unknown/unavailable capability state fails closed;
- UI permission presentation never bypasses or replaces backend/application authorization;
- architecture contains no independent role-assignment system inside Dynamic UI.

### Dynamic Action Bars
- Top and Bottom Action Bars render from metadata;
- actions are localized and IconKey-driven;
- selection-aware enablement behaves deterministically;
- bottom bar can show row/selection/error/pending-change counts;
- actions dispatch only through registered navigation/application/batch commands;
- malformed/unknown actions do not crash the template.

### Dynamic Signing Template
- work queue/list renders structured signing items;
- document preview reuses registered viewer infrastructure;
- signer/approver/status/timeline/history presentation works;
- Submit/Approve/Reject/Sign actions are metadata/registered-command driven;
- no cryptographic signing is implemented in the UI;
- no private key/PKCS#11/HSM logic exists in Dynamic UI;
- application/API remains authoritative for permissions, workflow, and signing execution;
- read-only history remains immutable in the UI.

### Application Menu / App Shell Menu

The hosting layer provides a standard application-level menu opened from a configurable
shell affordance such as:

- application logo;
- menu button (`☰`);
- overflow button (`…`);
- Backstage/Application button.

The recommended default is the **application logo / application button** at the top-left
of the shell.


## 4.5 Template Modules

Each reusable template is a first-class module.

### `TS24.DynamicUI.Template.Setup`

Owns:

- `DynamicSetupTemplate`;
- dynamic setup tree;
- metadata editor host;
- column/variable designer;
- version/effective-date presentation;
- validate/publish/retire presentation.

May depend on:

```text
TS24.DynamicUI.Core
TS24.DynamicUI.Shared
```

and explicitly required generic extensions only.

### `TS24.DynamicUI.Template.DataEntry`

Owns:

- `DynamicDataEntryTemplate`;
- dynamic columns;
- edit/read/calc modes;
- selection;
- sort/filter/search;
- clipboard interaction;
- candidate edit buffer;
- grid-state presentation.

May use `TS24.DynamicUI.Excel` through contracts, but must not depend on Setup, Report,
Dashboard, or HistoryDocument modules.

### `TS24.DynamicUI.Template.Report`

Owns:

- `DynamicReportTemplate`;
- report/document designer shell;
- variable catalog binding;
- XML layout designer;
- compiled render-model integration;
- preview/export presentation.

May use `TS24.DynamicUI.Reporting`, but must not depend on PayCalc24 or DataEntry template.

### `TS24.DynamicUI.Template.HistoryDocument`

Owns:

- `DynamicHistoryDocumentTemplate`;
- history grid;
- search/filter;
- version selection;
- master/detail;
- document preview host.

May use `TS24.DynamicUI.Documents`.

### `TS24.DynamicUI.Template.Dashboard`

Owns:

- `DynamicDashboardTemplate`;
- dynamic dashboard tree/view selector;
- dashboard canvas;
- widget definitions;
- drill-down;
- shared dashboard time context;
- widget states.

Must consume shared chart/grid primitives rather than referencing another complete
template module.

### `TS24.DynamicUI.Template.Signing`

Owns:

- `DynamicSigningTemplate`;
- signing/approval work queue presentation;
- selected item detail;
- document preview host;
- signer/approver presentation;
- comments/reasons;
- workflow status;
- timeline/history;
- signing/approval contextual actions.

This module is **UI-only**.

It must not:

- hold private keys;
- access PKCS#11 directly;
- perform HSM/token operations;
- calculate cryptographic signatures;
- mutate authoritative workflow state directly;
- decide who is allowed to sign/approve.

All execution is delegated through registered application/provider boundaries.

### `TS24.DynamicUI.Excel`

Shared spreadsheet exchange functionality:
- Excel import;
- Excel export;
- export template;
- clipboard TSV conversion;
- column mapping;
- sheet selection;
- preview model.

### `TS24.DynamicUI.Reporting`

Shared report/document presentation and rendering boundary:
- report definition;
- visual XML layout definition;
- XML validation;
- compiled immutable render model;
- render-model cache;
- screen preview;
- PDF export contract;
- Excel export contract;
- XML export contract;
- grouping/sorting/total presentation.

The runtime renderer should prefer compiled/validated layout models over reparsing published XML on every preview.

### `TS24.DynamicUI.Documents`

Shared document/history presentation boundary.

Contains reusable contracts/adapters for:

- document history grids;
- metadata search/filter;
- document preview;
- version history;
- audit/provenance;
- download/export capability;
- file-type viewer resolution.

It must not contain application-specific document business rules.

Recommended adapter model:

```text
IDocumentViewerAdapter
  ├─ PdfViewerAdapter
  ├─ OfficeViewerAdapter
  ├─ XmlViewerAdapter
  ├─ JsonViewerAdapter
  ├─ TextViewerAdapter
  └─ ImageViewerAdapter
```

Unsupported formats must fail gracefully to metadata-only or open/export behavior.

### `TS24.DynamicUI.Batch`

Shared batch-action presentation/contracts.

Contains:

- selected-item descriptors;
- batch action metadata;
- batch confirmation models;
- progress/result presentation;
- partial-failure presentation;
- retry-failed presentation;
- export/share/execute provider contracts.

It must not contain application-specific execution logic.


## 5. Template Module Contract

Every template module implements a common generic contract.

Conceptual contract:

```text
IDynamicTemplate
  TemplateCode
  TemplateVersion
  SupportedCapabilities
  ValidateDefinition(...)
  CreateWorkspace(...)
```

The exact API may evolve during implementation, but the architectural responsibilities
are fixed.

### 5.1 TemplateCode

Standard initial codes:

```text
SETUP
DATA_ENTRY
REPORT
HISTORY_DOCUMENT
DASHBOARD
SIGNING
```

Applications store/use `TemplateCode` in workspace metadata.

The host resolves the code through `TemplateRegistry`.

### 5.2 TemplateRegistry

Conceptual flow:

```text
WorkspaceDefinition
   ↓
TemplateCode
   ↓
TemplateRegistry
   ↓
Resolved Template Module
   ↓
Workspace
```

The host must not use hard-coded `if/else` branches for every known template.

### 5.3 Extension Model

Future modules may register additional template codes such as:

```text
WORKFLOW
KANBAN
TIMELINE
CALENDAR
MAP
SIGNING
FORM_DESIGNER
```

Adding a new template must not require modifying the five existing template modules.

### 5.4 Template Isolation

Forbidden dependencies:

```text
Template.Dashboard → Template.DataEntry
Template.Report → Template.Setup
Template.HistoryDocument → PayCalc24
TS24.DynamicUI.Core → any template module
```

When two templates need the same behavior, extract a generic primitive into Core/Shared
or an Extension module instead of introducing template-to-template coupling.

### 5.5 Independent Versioning

Template modules may evolve independently.

Example:

```text
TS24.DynamicUI.Core                 0.5.0
TS24.DynamicUI.Template.Setup       0.5.0
TS24.DynamicUI.Template.DataEntry   0.6.1
TS24.DynamicUI.Template.Report      0.7.0
TS24.DynamicUI.Template.HistoryDocument 0.5.2
TS24.DynamicUI.Template.Dashboard   0.5.0
```

A consuming app upgrades only the modules it needs, subject to declared Core compatibility.


## 5. Four Standard Templates

### 5.1 DynamicSetupTemplate

Purpose: create and maintain metadata/catalog definitions.

```text
┌──────────────────────┬────────────────────────────────────────────────────┐
│ Dynamic Tree         │ Selected Setup Workspace                           │
│                      │                                                    │
│ ▾ Catalog Group      │ Search / Filter / Add / Edit / Validate / Publish │
│   ├─ Node            │                                                    │
│   └─ Child Node      │ Dynamic Grid / Form / Designer                     │
│                      │                                                    │
│ ▾ Another Group      │ Detail / Metadata / Version / Audit                │
└──────────────────────┴────────────────────────────────────────────────────┘
```

Capabilities:
- create node;
- create child node;
- rename;
- reorder;
- enable/disable;
- show/hide;
- move node;
- drag/drop where supported;
- define workspace type;
- define columns;
- define variables;
- bind formulas;
- configure validation;
- configure localization;
- configure import/export;
- configure version/effective date;
- validate;
- publish;
- retire.

### 5.2 DynamicDataEntryTemplate

Purpose: provide a spreadsheet-like data-entry and data-review experience.

```text
Fixed Columns
+
Dynamic Variable Columns
+
Data Rows
```

Example:

```text
EmployeeCode | EmployeeName | WORK_DAYS | OT_HOURS | NEW_CR | ƒx ACHIEVEMENT
-------------|--------------|-----------|----------|--------|----------------
E001         | Nguyen An    | 22        | 10       | 80M    | 80%
E002         | Tran Binh    | 21        | 5        | 120M   | 120%
```

Modes:
- `GRID_EDIT`
- `GRID_CALC`
- `GRID_READ`

### 5.3 DynamicReportTemplate

Purpose: build, preview, and export metadata-driven reports.

```text
Report Tree
   ↓
Report Definition
   ↓
Data Source
Columns
Filter
Group
Sort
Formula Column
Subtotal
Total
   ↓
Preview
   ↓
Excel / PDF
```

#### 5.3.1 Report / Document Designer Modes

`DynamicReportTemplate` supports two presentation modes:

```text
REPORT
FORM_DOCUMENT
```

`REPORT` is optimized for tabular/grouped analytical output.

`FORM_DOCUMENT` is optimized for fixed or semi-structured forms such as:

- tax declarations;
- invoices;
- statements;
- certificates;
- vouchers;
- contracts;
- internal forms.

Both use the same safe data-binding model.

#### 5.3.2 Visual XML Layout Designer

Users should normally design visually rather than hand-author XML.

Recommended layout:

```text
┌──────────────────────┬──────────────────────────────┬────────────────────┐
│ VARIABLES            │ DESIGN SURFACE               │ PROPERTIES         │
│                      │                              │                    │
│ Company              │ Header                       │ Binding            │
│ Period               │ Table / Sections             │ Format             │
│ Employee             │ Footer                       │ Font               │
│ Items                │                              │ Alignment          │
│ Totals               │                              │ Visibility         │
└──────────────────────┴──────────────────────────────┴────────────────────┘
```

Recommended tabs:

```text
Design | Preview | XML
```

The XML tab is optional/advanced and must not be required for normal users.

#### 5.3.3 Safe XML Layout DSL

XML is a persistence/layout DSL, not executable application code.

Permitted concepts may include:

- Page;
- Section;
- Header;
- Footer;
- Text;
- Label;
- Image/Logo;
- Table;
- Repeater;
- GroupBand;
- Line/Border;
- Field;
- ConditionalVisibility;
- Font;
- Alignment;
- Format;
- Subtotal;
- Total.

Forbidden inside template XML:

- embedded C#;
- arbitrary script;
- arbitrary SQL;
- arbitrary file-system access;
- direct service calls;
- unrestricted expression execution.

All bindings resolve only to fields/variables exposed by the owning application.

### 5.4 DynamicHistoryDocumentTemplate

Purpose:

Provide a reusable history/document workspace for applications that must search,
inspect, preview, and trace historical records or files.

Standard layout:

```text
┌──────────────────────────────────┬─────────────────────────────────────────┐
│ HISTORY / DOCUMENT GRID          │ DETAIL / PREVIEW                        │
│                                  │                                         │
│ Search                           │ Metadata                                │
│ Type / Date / User / Status      │ Version / Source / Hash                 │
│                                  │ Audit / Provenance                      │
│ Date | Type | Name | Ver | State │                                         │
│ ...                              │ PDF / DOCX / XML / JSON / Text / Image │
└──────────────────────────────────┴─────────────────────────────────────────┘
```

The left pane is a structured read-oriented grid.

The right pane presents:
- canonical metadata;
- version identity;
- audit/provenance;
- document preview when supported;
- download/export actions when allowed by the consuming application.

History records are read-only by default.

If the consuming application supports correction/replacement, it must create a new
version/revision rather than mutating historical content in place.

## 6. Dynamic Tree Definition

Minimum metadata:

```text
NodeId
ParentNodeId
Code
DisplayName
Description
NodeType
WorkspaceType
WorkspaceDefinitionId
DisplayOrder
IconKey
IsVisible
IsEnabled
PermissionCode
EffectiveFrom
EffectiveTo
Version
Status
```

Tree hierarchy is defined by `ParentNodeId`, with arbitrary depth.

## 7. Dynamic Column Definition

Minimum definition:

```text
ColumnId
WorkspaceDefinitionId
VariableCode
DisplayName
Description
ColumnType
DataType
Unit
EditorType
DisplayOrder
Width
Visible
Required
Editable
CopyAllowed
PasteAllowed
Importable
Exportable
Format
DecimalPlaces
MinValue
MaxValue
LookupDefinitionId
FormulaDefinitionId
ValidationDefinitionId
EffectiveFrom
EffectiveTo
Version
Status
```

## 8. Column Types

Required standard types:

```text
INPUT
FORMULA
LOOKUP
SYSTEM
```

### INPUT

Default:
```text
Editable = true
PasteAllowed = true
Importable = true
CopyAllowed = true
```

### FORMULA

Mandatory framework rules:

```text
Editable = false
PasteAllowed = false
Importable = false
CopyAllowed = true
```

The column should visually indicate that it is calculated, e.g. an `fx` semantic icon.

### LOOKUP

Value selected/resolved from a configured lookup source.

### SYSTEM

Default:

```text
Editable = false
PasteAllowed = false
Importable = false
```

## 9. VariableCode

`VariableCode` is the canonical semantic identity of a dynamic variable.

Requirements:
- unique within the appropriate scope;
- stable after publication;
- independent of display name;
- usable by formulas;
- usable by import/export mapping;
- usable by integrations;
- version-aware where necessary.

Recommended scoped examples:

```text
COMPANY.TOTAL_CR
ORG.HEADCOUNT
EMP.WORK_DAYS
EMP.KPI_SCORE
PERIOD.STANDARD_WORK_DAYS
```

## 10. Formula Definition in Setup

Formula definitions are authored/configured in Setup.

Example:

```text
Display Name: Achievement
VariableCode: ACHIEVEMENT
ColumnType: FORMULA
DataType: DECIMAL
Unit: PERCENT
Formula: NEW_CR / SALES_TARGET
Editable: false
PasteAllowed: false
```

Setup should display dependencies and reverse usage.

The framework does not execute authoritative formula logic.

```text
Setup Formula Definition
   ↓
Application persists/version-controls definition
   ↓
Dynamic Data Entry submits input values
   ↓
Application Formula/Calculation Engine executes
   ↓
Calculated results returned
   ↓
Formula columns refreshed read-only
```

## 11. Dependency Validation

Before publish, validate:
- unknown variables;
- duplicate variable codes;
- incompatible data types;
- missing dependencies;
- circular references.

Example invalid graph:

```text
A = B + 1
B = A * 2
```

Expected diagnostic:

```text
FORMULA.CIRCULAR_REFERENCE
A → B → A
```

## 12. Excel-like Grid Interaction

Required capabilities:
- row/cell selection;
- range selection;
- single-column sort;
- multi-column sort where supported;
- global search;
- column filter;
- clear filters;
- column resize;
- column reorder;
- show/hide columns;
- frozen identity columns where configured;
- copy selected range;
- paste selected block;
- clipboard compatibility with Excel;
- keyboard navigation;
- batch clear;
- fill/copy down where supported;
- multi-row operation;
- visible validation state;
- row numbering;
- status summary.

Recommended shortcuts:

macOS:
`Cmd+C`, `Cmd+V`, `Cmd+X`, `Cmd+A`, `Cmd+Z`, `Shift+Cmd+Z`

Windows:
`Ctrl+C`, `Ctrl+V`, `Ctrl+X`, `Ctrl+A`, `Ctrl+Z`, `Ctrl+Y`

Navigation:
`Tab`, `Shift+Tab`, `Enter`, arrows, `PageUp`, `PageDown`, `Escape`, `F2` where practical.

## 13. Multi-cell Copy

Copying a range must produce clipboard data compatible with spreadsheets using tab/newline representation.

## 14. Multi-cell Paste

Canonical pipeline:

```text
Clipboard
   ↓
Parse tab/newline matrix
   ↓
Resolve target cells
   ↓
Check column capabilities
   ↓
Type conversion
   ↓
Cell validation
   ↓
Cross-field/application validation
   ↓
Candidate changes
   ↓
Preview / Commit
```

Paste must not directly write authoritative application data.

If a paste range intersects `FORMULA` or `SYSTEM` columns:
- do not overwrite;
- preserve calculated/system values;
- provide deterministic feedback.

## 15. Candidate Edit Buffer

Recommended model:

```text
Source Rows
   ↓
View Filter/Sort
   ↓
Selection
   ↓
Edit Buffer
   ↓
Validation
   ↓
Commit through Application service
```

## 16. Search / Sort / Filter

Search, sorting, and filtering are view-state only and must never mutate source data.

## 17. Import / Export Capability

Workspace-level metadata:

```text
CanImportExcel
CanExportExcel
CanExportPdf
CanExportTemplate
```

Column-level metadata:

```text
Importable
Exportable
Editable
PasteAllowed
```

## 18. Excel Import

Canonical workflow:

```text
Select File
→ Select Sheet
→ Map Columns
→ Validate
→ Preview
→ Commit
```

Mapping uses `VariableCode` as the canonical target.

`FORMULA` and `SYSTEM` columns do not accept authoritative imported values by default.

## 19. Excel Export

Required modes:

```text
Export Current View
Export All Rows
Export Template
```

`Export Template` includes required identity columns and importable input columns.

## 20. PDF Export

Dynamic grids may support PDF export when enabled.

PDF should capture:
- title;
- application context;
- current filters;
- visible columns;
- rows;
- generated date/time;
- generated by;
- definition/report version where available.

## 21. PDF Import

PDF extraction/OCR is outside Dynamic UI Core.

Recommended pipeline:

```text
PDF
→ External extraction/OCR provider
→ Candidate structured rows
→ Column mapping
→ Validate
→ Preview
→ Commit
```

Never allow `PDF → direct database write`.

## 22. Localization

Dynamic metadata should support at least:

```text
DisplayName.vi-VN
DisplayName.en-US
```

Variable codes and technical IDs are not translated.

## 23. Themes

Required themes:

```text
System
Light
Dark
```

## 24. Semantic SVG / IconKey

Icons are referenced by semantic key, such as:

```text
IconKey.Import
IconKey.Export
IconKey.Search
IconKey.Filter
IconKey.Formula
IconKey.Validate
IconKey.Publish
IconKey.Report
```

No business module references SVG file paths.

## 25. Permissions / Capabilities

The framework consumes capability state from the owning application and does not implement authorization rules.

Examples:

```text
CanAddNode
CanEditDefinition
CanPublish
CanImport
CanExport
CanEditCell
```

## 26. Versioning / Effective Dating

The framework presents:

```text
Version
EffectiveFrom
EffectiveTo
Status
```

Typical lifecycle:

```text
Draft
→ Validate
→ Publish
→ Retire
```

Actual lifecycle semantics belong to the consuming application.

## 27. Audit / Provenance Presentation

Reusable presentation should support:
- CreatedAt;
- CreatedBy;
- ModifiedAt;
- ModifiedBy;
- PublishedAt;
- PublishedBy;
- Version;
- Source;
- Import identity;
- result provenance.

## 28. Standard UI States

Every template supports:
- Empty;
- Loading;
- Error;
- Read-only.

## 29. Performance

Dynamic grids should support virtualization and remain responsive for practical business datasets with several thousand rows and dynamic columns.

Do not implement a custom spreadsheet renderer unless Avalonia DataGrid plus an interaction layer cannot meet acceptance criteria.



### 5.5 DynamicDashboardTemplate

Purpose:

Provide a reusable, configuration-driven dashboard experience that users can understand
immediately and that applications can compose without creating one-off dashboard views.

Standard layout:

```text
┌──────────────────────────┬──────────────────────────────────────────────────┐
│ DASHBOARD TREE / SELECT  │ DASHBOARD CANVAS                                 │
│                          │                                                  │
│ ▾ Company                │ [KPI Card] [KPI Card] [KPI Card]                │
│   ├─ Overview            │                                                  │
│   ├─ Sales               │ [Pie / Donut]       [Column / Bar]              │
│   └─ Operations          │                                                  │
│                          │ [Line / Area Trend]                              │
│ ▾ Exceptions             │                                                  │
│   ├─ Blocking            │ [Data Grid / Top-N / Status List]               │
│   └─ Warnings            │                                                  │
└──────────────────────────┴──────────────────────────────────────────────────┘
```

The left pane is a metadata-driven tree/view selector.

The right pane renders the dashboard definition associated with the selected node.

The template must not require a dedicated XAML/View class for every dashboard node.

#### 5.5.1 Dynamic Dashboard Tree

Dashboard nodes support arbitrary parent/child hierarchy.

Suggested node metadata:

```text
NodeId
ParentNodeId
DisplayName
IconKey
DisplayOrder
DashboardDefinitionId
PermissionCode
IsVisible
```

Selecting a node resolves `DashboardDefinitionId` and renders its widget composition.

#### 5.5.2 Standard Dashboard Widgets

The framework should provide reusable widget contracts for at least:

```text
KPI_CARD
PIE_CHART
DONUT_CHART
COLUMN_CHART
BAR_CHART
LINE_CHART
AREA_CHART
DATA_GRID
TOP_N
STATUS_LIST
PROGRESS
TEXT
IMAGE
CUSTOM
```

`CUSTOM` is an extension boundary and must not allow arbitrary executable metadata.

#### 5.5.3 Widget Definition

Suggested metadata:

```text
WidgetId
WidgetType
Title
DataSourceId
VariableBindings
FilterDefinition
GroupBy
SortDefinition
TimeDimension
Width
Height
DisplayOrder
RefreshMode
DrillDownTarget
PermissionCode
```

The framework may extend this metadata while preserving backward compatibility.

#### 5.5.4 Widget Data Binding

Dashboard widgets bind only to application-exposed data sources and variables.

Example Pie/Donut binding:

```text
Category = ORG.DEPARTMENT_NAME
Value    = PAYROLL.GROSS_PAY
```

Example Column binding:

```text
X = PERIOD.MONTH
Y = PAYROLL.GROSS_PAY
```

Example Grid binding:

```text
Columns:
- ORG.DEPARTMENT_NAME
- FUND.DEMAND
- FUND.FUNDED
- FUND.COVERAGE
```

The Dashboard template does not calculate authoritative business values.

#### 5.5.5 Dashboard Time Context

`DynamicDashboardTemplate` consumes the Universal Time Filter defined by the framework.

A dashboard may declare a shared time context that applies to all compatible widgets.

Example:

```text
TimeMode = MONTH
From     = 2026-08-01
To       = 2026-08-31
```

A widget may override the shared time dimension only when its metadata explicitly permits it.

Changing dashboard time context changes query/presentation state only.

#### 5.5.6 Drill-Down

Widgets may expose navigation/drill-down targets.

Examples:

```text
KPI Card
   ↓
Workspace / Dashboard Node / Filtered Grid

Pie Segment
   ↓
Filtered Detail Grid

Status Item
   ↓
Validation / Exception Workspace
```

Suggested target metadata:

```text
TargetType
TargetId
ParameterBindings
FilterBindings
```

Drill-down must use registered application navigation targets; it must not construct arbitrary routes or commands.

#### 5.5.7 Dashboard States

Dashboard-level states:

```text
EMPTY
LOADING
READY
ERROR
PERMISSION_DENIED
```

Each widget must independently support:

```text
LOADING
READY
NO_DATA
ERROR
PERMISSION_DENIED
```

A failed widget must not unnecessarily destroy the entire dashboard when other widgets can still render.

#### 5.5.8 Refresh

Suggested refresh modes:

```text
MANUAL
ON_OPEN
INTERVAL
APPLICATION_DEFINED
```

The framework presents refresh state and invokes the consuming application's query/provider boundary.

It does not implement domain polling rules on its own.

#### 5.5.9 Responsive Composition

Dashboard definitions should support metadata-driven:

- width/span;
- height;
- display order;
- minimum size;
- responsive wrapping/reflow.

The initial standard remains conceptually two columns:

```text
Tree/View Selector | Dashboard Canvas
```

The Dashboard Canvas itself may contain a responsive widget grid.

#### 5.5.10 Dashboard Security

Each dashboard node and widget may declare a permission requirement.

The framework must:

- hide or disable inaccessible nodes according to application policy;
- avoid querying widget data when the user lacks permission;
- show permission-denied state where appropriate.

The application remains authoritative for authorization.


## 30. Dynamic History / Document Metadata

Minimum reusable document metadata:

```text
DocumentId
RelatedEntityId
RelatedEntityType
DocumentType
FileName
MimeType
Version
CreatedAt
CreatedBy
ModifiedAt
ModifiedBy
Status
Source
Hash
CanPreview
CanDownload
CanExport
```

Applications may extend metadata without changing the template contract.

The framework must not assume that a document is a payroll file, contract, invoice,
HR record, or any other specific business entity.

## 31. History Grid Behavior

The history/document grid should support:

- search;
- metadata filtering;
- date range filtering;
- file-type filtering;
- creator/user filtering;
- status filtering;
- sort;
- row selection;
- multi-select where meaningful;
- copy selected metadata;
- export selected/current view;
- virtualization/paging for large histories.

Search may operate over metadata supplied by the application.

Full-text document search is an application/search-service responsibility; the template
only consumes search results.

## 32. Document Detail

Selecting a history row should show a reusable detail panel containing available:

- file name;
- document type;
- MIME type;
- version;
- status;
- source;
- created/modified time;
- created/modified by;
- related entity;
- hash/checksum;
- audit/provenance.

Technical IDs and hashes should remain copyable.

## 33. Document Preview Adapter Boundary

Viewer selection is based on MIME type/file metadata.

Conceptual flow:

```text
Selected History Row
   ↓
Document Descriptor
   ↓
Viewer Resolver
   ↓
IDocumentViewerAdapter
   ↓
Preview Surface
```

Suggested supported classes:

### PDF
- embedded preview where supported;
- page navigation;
- zoom;
- search if viewer/provider supports it.

### DOC / DOCX
- preview through a supported Office/document renderer or conversion adapter;
- do not implement a word-processing engine in Dynamic UI Core.

### XML
- formatted tree view;
- raw/source view;
- copy node/value;
- optional schema/validation information supplied by application.

### JSON
- formatted tree/source view;
- syntax-aware presentation where practical.

### Text
- read-only text viewer;
- search/copy.

### Image
- image preview;
- zoom/fit.

### Unsupported format
- metadata view;
- Open/Download/Export when capability permits;
- never crash the application.

## 34. Document Security and Capability Rules

The framework consumes capabilities from the application:

```text
CanViewMetadata
CanPreview
CanDownload
CanExport
CanViewAudit
```

The UI must never grant access merely because a file reference is present.

Document bytes/streams are obtained through application-controlled interfaces.

The framework must not bypass authorization or read arbitrary local/server file paths.

## 35. Document Versioning

History/document presentation must treat versions as explicit identities.

Example:

```text
Document A
  v1
  v2
  v3
```

Selecting a version must preview that exact version.

The template must never silently substitute the latest version when a historical
version was selected.

## 36. History Export

When enabled, support:

- Export Current View;
- Export Selected Metadata;
- Export Original Document;
- Export PDF representation where supported by the consuming application.

Export Original Document must preserve the exact selected version.

## 37. History Search States

The template must support:

- no search query;
- searching;
- results;
- no results;
- search error;
- permission denied;
- document unavailable.

No-results and unavailable are different states.



## 38. Report Variable / Field Catalog

Each report/document definition receives an explicit variable catalog from the consuming application.

The designer must not expose every application variable automatically.

A report definition declares allowed variable sets.

Example:

```text
ReportCode = PAYROLL_DETAIL

AllowedVariableSets:
COMPANY.*
PERIOD.*
EMPLOYEE.*
PAYROLL.*
STATUTORY.*
```

A Fund report may instead expose:

```text
COMPANY.*
PERIOD.*
ORG.*
FUND.*
```

### 38.1 Scalar Variables

Scalar variables are single values suitable for headers, footers, labels, and document metadata.

Examples:

```text
COMPANY.NAME
COMPANY.TAX_CODE
PERIOD.NAME
REPORT.GENERATED_AT
```

### 38.2 Collection Variables

Collection variables represent repeatable row/item sets.

Examples:

```text
PAYROLL.ROWS
INVOICE.ITEMS
KPI.ROWS
FUND.ROWS
```

Fields within collections are explicitly described:

```text
PAYROLL.ROWS.EMPLOYEE_CODE
PAYROLL.ROWS.EMPLOYEE_NAME
PAYROLL.ROWS.GROSS_PAY
PAYROLL.ROWS.NET_PAY
```

The designer uses collection fields for tables/repeaters.

### 38.3 Visual Variable Selection

Variables should appear in a browsable tree:

```text
Company
 ├─ COMPANY.NAME
 └─ COMPANY.TAX_CODE

Payroll Period
 ├─ PERIOD.NAME
 └─ PERIOD.FROM

Employee
 ├─ EMPLOYEE.CODE
 └─ EMPLOYEE.NAME

Payroll
 ├─ PAYROLL.GROSS_PAY
 └─ PAYROLL.NET_PAY
```

Users may drag/drop or double-click variables into the design surface.

The designer generates/stores XML bindings automatically.

## 39. XML Layout Template Versioning

Every published layout template must have explicit identity/version.

Example:

```text
PAYROLL_DETAIL
 ├─ Layout v1
 ├─ Layout v2
 └─ Layout v3
```

Historical generation/replay must be capable of selecting the exact pinned layout version.

The framework must never silently substitute the latest layout when an exact historical version is requested.

Recommended report/document identity includes:

```text
ReportDefinitionVersion
LayoutTemplateVersion
DataContextVersion
Policy/Calculation Version where supplied by the application
```


## 40. Canonical XML Layout and Runtime Rendering

XML is the canonical portable persistence format for report/document layouts.

Recommended lifecycle:

```text
Draft XML Layout
   ↓
Validate
   ↓
Publish
   ↓
Compile to Immutable Render Model
   ↓
Cache by TemplateId + Version
   ↓
Screen / PDF / Excel / XML outputs
```

A published layout should be validated and compiled once per exact template version where practical. The compiled render model is immutable, safe, deterministic, optimized for repeated preview/export, and rebuildable from canonical XML. XML remains the authoritative portable template source; the compiled model is a runtime derivative.

### 40.1 Standard Render Targets

```text
SCREEN
PDF
EXCEL
XML
```

**SCREEN** renders directly from the compiled layout model plus the bound data context for fast viewing.

**PDF** prioritizes layout fidelity for official reports, forms, invoices, declarations, certificates, and archived snapshots.

**EXCEL** prioritizes structured tabular data. For visually complex documents, meaningful tables/sections should be exported rather than attempting pixel-for-pixel reproduction.

**XML** export must explicitly distinguish `Layout XML`, `Data XML`, and `Document Package XML` where those modes are enabled.

## 41. Layout XML vs Business/Data XML

Layout XML describes presentation and bindings. Business/Data XML represents application-provided data. They must remain separate concepts.

Example Layout XML:

```xml
<ReportTemplate code="PAYROLL_DETAIL" version="2">
  <Header><Text value="{COMPANY.NAME}" /></Header>
  <Table dataSource="PAYROLL.ROWS">
    <Column field="EMPLOYEE.NAME" />
    <Column field="PAYROLL.GROSS_PAY" />
  </Table>
</ReportTemplate>
```

Example Data XML:

```xml
<PayrollData period="2026-08">
  <Employee code="E001">
    <GrossPay>25000000</GrossPay>
  </Employee>
</PayrollData>
```

The framework does not define the business meaning of Data XML. An application may optionally export a Document Package XML containing layout identity/version, data identity/version, binding context, generated metadata, and integrity/hash metadata.

## 42. Direct Preview Performance

Direct viewing should use published/compiled templates:

```text
Resolve TemplateId + exact Version
   ↓
Get cached compiled render model
   ↓
Resolve variable/data context
   ↓
Bind
   ↓
Render Screen Preview
```

The runtime should not invoke the visual designer or reconstruct a layout for every preview. This supports fast repeated viewing, deterministic exact-version rendering, and safer runtime execution.

## 43. Layout Template Clone / Derivation

Users may clone an existing template.

Example:

```text
Standard Payroll Report
   ↓ Clone
Company ABC Payroll Report
   ↓
Change logo / labels / columns / grouping
```

Cloning must create a new template identity/version, not mutate the source template.

## 44. Report Formula / Presentation Formula Boundary

Report/document templates may define presentation-only calculated fields where permitted.

Examples:

- percentage of total;
- display concatenation;
- report subtotal;
- ratio used only in presentation.

Such calculations must never become authoritative application/business results.

Preferred architecture:

- reuse the consuming application's safe formula service in a `REPORT` scope when available;
- otherwise use a tightly limited presentation expression capability;
- never introduce a second unrestricted business expression engine.

## 45. Universal Time Filter

All grid/templates may expose a standard time filter when the workspace definition supports time-based data.

Workspace metadata:

```text
SupportsTimeFilter
PrimaryTimeField
TimeFilterModes
DefaultTimePreset
```

Supported modes:

```text
DATE_RANGE
MONTH
QUARTER
YEAR
PERIOD
```

Suggested presets:

```text
Today
This Week
This Month
This Quarter
This Year
Custom
```

Example toolbar:

```text
[From 01/08/2026] [To 31/08/2026]
[Today] [This Month] [This Quarter] [This Year]
[Apply] [Clear]
```

### 45.1 Time Dimensions

A workspace may expose multiple available time dimensions, such as:

```text
BusinessDate
CreatedAt
EffectiveFrom
ImportedAt
ApprovedAt
```

Setup chooses the primary/default time dimension.

The framework must not assume payroll month semantics.

### 45.2 Time Filter Behavior

Time filtering is query/view state only.

It must not:

- alter underlying authoritative data;
- change calculation logic;
- rewrite effective dates.

For Reports, time filter values may become report parameters.

For History/Documents, time filtering is a standard first-class filter.

## 46. Batch Action Framework

Dynamic grids, reports, and history/document views may support multi-item batch actions.

The framework provides generic selection and execution UX.

Application-specific actions are registered through metadata/providers.

### 46.1 Batch Action Metadata

Suggested model:

```text
ActionCode
DisplayName
IconKey
Scope
RequiresSelection
MinSelection
MaxSelection
AllowedItemTypes
PermissionCode
RequiresConfirmation
ExecutionMode
ProviderCode
```

Supported scopes:

```text
SINGLE
MULTI
SINGLE_OR_MULTI
```

### 46.2 Standard Batch Action Categories

The framework may expose semantic groups:

```text
Generate
Export
Share
Execute
```

The exact actions underneath are application-defined.

Examples from consuming applications may include:

```text
TAX.VALIDATE
TAX.SIGN
TAX.SUBMIT

INVOICE.ISSUE
INVOICE.SIGN
INVOICE.SEND

PAYROLL.GENERATE
PAYROLL.EXPORT
PAYROLL.SEND_ACCOUNTING
```

The framework must not hard-code these domain actions.

## 47. Batch Selection UX

The grid must always make selection scope explicit.

Example:

```text
125 records
18 selected
```

Selection behavior across filtering/sorting must be deterministic and documented.

The framework must never silently execute on all filtered rows when only selected rows were intended.

Provide explicit alternatives such as:

```text
Selected Items
All Visible
All Matching
```

only where the consuming application enables them.

## 48. Batch Export

For selected reports/documents/rows, supported application-defined exports may include:

- original;
- Excel;
- PDF;
- XML;
- CSV;
- ZIP package.

The framework routes export through configured providers.

It must not invent a file format that the application/provider does not support.

## 49. Batch Share

Sharing is provider-driven.

Suggested provider boundary:

```text
IShareProvider
```

Possible application-provided capabilities:

- Email;
- secure link;
- download package;
- send to external integration;
- internal application sharing.

Dynamic UI Framework must not own SMTP credentials or application-specific sharing policy.

## 50. Batch Execute

The framework may invoke a single application batch command with selected item identities.

Do not implement:

```text
for each selected row:
    call API independently
```

as the default architecture for large authoritative operations.

Preferred flow:

```text
Selected IDs
   ↓
Batch Action
   ↓
Application API
   ↓
Batch Job
   ↓
Progress / Results
```

Actual batch execution semantics belong to the consuming application.

## 51. Batch Job Presentation

The framework must support standard states:

```text
Queued
Running
Completed
Partial
Failed
Cancelled
```

Progress example:

```text
Processing 137 / 500

Succeeded 132
Failed      5
```

Required UX where supported:

- View Failures;
- Retry Failed;
- Cancel pending/running operation if application permits;
- export batch result.

Partial failure is a first-class state.

The UI must not claim all-or-nothing rollback unless the application guarantees it.

## 52. Batch Result Model

Suggested presentation contract:

```text
BatchJobId
ActionCode
Total
Processed
Succeeded
Failed
Status
StartedAt
CompletedAt
Items[]
```

Per-item result:

```text
ItemId
Status
DiagnosticCode
Message
ExternalReference
```

No credential or sensitive provider secret belongs in the result model.

## 53. Reusable Document/Form Use Cases

`DynamicReportTemplate` + XML Layout Designer may be reused for:

- payroll reports;
- tax forms/declarations;
- invoices;
- insurance forms;
- vouchers;
- accounting forms;
- contracts;
- certificates;
- internal forms;
- management statements.

The owning application remains responsible for:

- valid business data;
- statutory/business calculations;
- legal validation;
- signing/submission rules;
- authoritative document state.

The Dynamic UI Framework is responsible for:

- variable binding;
- layout;
- preview;
- presentation export;
- user-driven template design;
- versioning presentation.


## 54. PayCalc24 Mapping

PayCalc24 is the first consumer.

`DynamicSetupTemplate` may drive:
- Organization Catalog;
- Payroll Fund Catalog;
- Pay Component Catalog;
- Compensation Scheme Catalog;
- Attendance Catalog & Policy;
- Performance & KPI Catalog;
- Payroll Input Catalog;
- Calculation Rule & Formula Catalog;
- Report & Output Catalog.

`DynamicDataEntryTemplate` may drive:
- Payroll Assignment;
- Payroll Inputs;
- Attendance;
- KPI/Performance entry;
- Parameters.

`DynamicReportTemplate` may drive:
- Payroll;
- Attendance;
- KPI;
- Fund;
- Accounting;
- Management;
- Custom reports.

`DynamicHistoryDocumentTemplate` may drive:
- payroll import history;
- calculation/report output history;
- approval/adjustment evidence;
- integration delivery history;
- generated PDF/Excel/XML outputs;
- versioned supporting documents where exposed by PayCalc24.

PayCalc24 business logic remains in PayCalc24 application/domain modules.

## 55. Example Dynamic Grid Definition

```text
Workspace: SALES_MONTHLY_INPUT

Fixed Columns:
EmployeeCode
EmployeeName

Dynamic Columns:

NEW_CR
  ColumnType = INPUT
  DataType = DECIMAL
  Unit = VND
  Editable = true
  PasteAllowed = true
  Importable = true

SALES_TARGET
  ColumnType = INPUT
  DataType = DECIMAL
  Unit = VND
  Editable = true
  PasteAllowed = true
  Importable = true

ACHIEVEMENT
  ColumnType = FORMULA
  DataType = DECIMAL
  Unit = PERCENT
  Formula = NEW_CR / SALES_TARGET
  Editable = false
  PasteAllowed = false
  Importable = false
```

Generated grid:

```text
EmployeeCode | EmployeeName | NEW_CR | SALES_TARGET | ƒx ACHIEVEMENT
-------------|--------------|--------|--------------|----------------
E001         | Nguyen An    | 80M    | 100M         | 80%
E002         | Tran Binh    | 120M   | 100M         | 120%
```

The UI does not calculate `ACHIEVEMENT`; it renders the result returned by the consuming application's engine.

## 56. Core Acceptance Criteria v0.7

### Dynamic tree
- create parent node;
- create child node;
- arbitrary nesting;
- reorder;
- node selection changes workspace;
- no XAML change required to add a configured node.

### Dynamic columns
- add configured column;
- column appears without XAML modification;
- change display name without changing VariableCode;
- display order follows metadata;
- Input column editable;
- Formula column read-only.

### Formula columns
- Formula definition authored from Setup;
- calculated state visible;
- paste into formula cell blocked/ignored;
- import into formula column blocked;
- application service can return calculated result for display.

### Grid interaction
- sort;
- search;
- filter;
- range selection;
- multi-cell copy;
- multi-cell paste from Excel;
- deterministic handling of protected cells;
- keyboard navigation;
- validation markers.

### Import/export
- Excel import;
- sheet selection;
- mapping;
- validation;
- preview;
- commit boundary;
- Export Current View;
- Export All;
- Export Template;
- Export PDF.

### Presentation
- en-US;
- vi-VN;
- System/Light/Dark;
- SVG IconKey;
- empty/loading/error/read-only states.

### History / Documents
- history grid supports search/filter/sort;
- selecting a row shows metadata/detail;
- exact selected version is preserved;
- PDF preview adapter resolves when available;
- DOC/DOCX/XML/JSON/Text/Image viewer adapters resolve by descriptor;
- unsupported formats fail gracefully;
- preview/download/export are capability-controlled;
- no direct file-system bypass from the UI.

### Visual XML Report / Document Designer
- allowed variable catalog is provided per report/document type;
- scalar variables can bind to labels/header/footer;
- collection variables can bind to table/repeater rows;
- users can design visually without writing XML;
- XML persists layout safely;
- no arbitrary C#/SQL/script execution;
- template clone creates a new identity/version;
- historical layout version can be selected exactly.

### Dynamic Dashboard
- standard two-column Tree/View Selector + Dashboard Canvas renders correctly;
- arbitrary parent/child dashboard nodes are metadata-driven;
- selecting a node changes dashboard definition without a dedicated hard-coded view;
- KPI, Pie/Donut, Column/Bar, Line/Area, Data Grid, Top-N, Status List, and Progress widgets render from metadata;
- widgets bind only to application-exposed data/variables;
- shared Universal Time Filter context updates compatible widgets;
- drill-down routes through registered navigation targets;
- widget-level loading/no-data/error/permission states work independently;
- Dashboard does not calculate authoritative business values.

### Canonical XML Rendering
- published XML validates before runtime use;
- published layout compiles to an immutable render model;
- exact template version resolves deterministically;
- repeated preview may reuse cached compiled layout;
- Screen Preview renders from compiled layout + bound data;
- PDF export uses the renderer boundary;
- Excel export preserves structured tabular data;
- XML export distinguishes Layout XML, Data XML, and Document Package XML where enabled;
- runtime preview does not require reopening the visual designer.

### Universal Time Filter
- supported grids expose standard time filter controls;
- primary time dimension comes from metadata;
- date range/month/quarter/year/period modes work where enabled;
- time filter does not mutate authoritative data.

### Batch Actions
- multi-selection clearly displays selected count;
- registered application actions appear contextually;
- batch export works through provider boundary;
- batch share works through provider boundary;
- batch execute submits selected identities through application boundary;
- progress and partial failures are visible;
- failed items can be identified/retried where supported.

### Dynamic Ribbon
- Ribbon tabs/groups/commands render from metadata;
- Setup can create/reorder/configure Ribbon definitions;
- localized labels and semantic IconKey/SVG resolve correctly;
- contextual groups change with current workspace/template context;
- permission/capability state controls visibility/enablement without replacing application authorization;
- navigation commands resolve registered workspace/template targets;
- application commands resolve through registered command/provider boundaries;
- malformed or unknown command metadata fails safely;
- no application-specific Ribbon tabs are required in hard-coded XAML.

### Modular Template Architecture
- each of the five templates builds as an independent project/module;
- `TemplateRegistry` resolves `TemplateCode` to the correct module;
- adding a sample sixth template does not require changing existing template implementations;
- template modules do not reference PayCalc24;
- template-to-template dependency rules are architecture-tested;
- shared primitives are located in Core/Shared/Extensions;
- each template has its own tests and documentation.

### Architecture
- framework has no dependency on PayCalc24;
- formula execution is not implemented in UI;
- no direct database access from UI;
- application business services remain authoritative.

## 57. Initial Test Cases

### DUI-TREE-001
Create Root → Child → Grandchild.

Expected: all render without code/XAML change.

### DUI-COL-001
Add `CUSTOM_VALUE` metadata column.

Expected: column appears in configured position without View modification.

### DUI-VAR-001
Rename display text while keeping `VariableCode = NEW_CR`.

Expected: formula dependency unchanged.

### DUI-FORMULA-001
`ColumnType = FORMULA`.

Expected:
`Editable=false`, `PasteAllowed=false`, `Importable=false`.

### DUI-PASTE-001
Paste a 2×2 clipboard block into editable region.

Expected: 4 candidate values mapped correctly.

### DUI-PASTE-002
Paste intersects Formula column.

Expected: input cells accepted; formula cells unchanged; deterministic feedback.

### DUI-COPY-001
Copy 3×4 block and paste into Excel.

Expected: row/column shape preserved.

### DUI-IMPORT-001
Import Excel with matching VariableCode headers.

Expected: automatic mapping where unambiguous, then validation/preview before commit.

### DUI-EXPORT-001
Export Template.

Expected: identity + importable input columns; Formula/System columns excluded by default.

### DUI-THEME-001
Light → Dark → System.

Expected: tree/grid/editors/dialogs remain readable.

### DUI-LOCALIZATION-001
en-US → vi-VN.

Expected: display names change; VariableCode unchanged.

### DUI-HISTORY-001
Provide three document-history rows with two versions of the same document.

Expected:
- history grid renders rows;
- selecting v1 previews exact v1 metadata/content descriptor;
- selecting v2 previews exact v2.

### DUI-DOC-001
Provide PDF descriptor with preview capability.

Expected:
PDF viewer adapter resolves and preview surface opens.

### DUI-DOC-002
Provide XML descriptor.

Expected:
XML viewer supports formatted tree/source presentation.

### DUI-DOC-003
Provide unsupported MIME type.

Expected:
no crash; metadata remains visible; permitted Open/Export actions remain available.

### DUI-DOC-004
Set `CanPreview=false`.

Expected:
preview command is unavailable even when a viewer adapter exists.

### DUI-REPORTXML-001
Create a document layout using scalar variable `COMPANY.NAME`.

Expected:
visual designer persists a safe XML binding and preview renders the supplied value.

### DUI-REPORTXML-002
Bind collection `INVOICE.ITEMS` to a table.

Expected:
repeater/table renders one row per supplied collection item.

### DUI-REPORTXML-003
Attempt to persist embedded script/arbitrary SQL.

Expected:
template validation rejects it.

### DUI-PLATFORM-001
Publish Demo for `win-x64`, `win-arm64`, `osx-arm64`, `osx-x64`, and `linux-x64`.

Expected: all five publish successfully.

### DUI-PLATFORM-002
Inspect framework dependencies.

Expected: Core/Shared contain no intentional single-OS dependency without an explicit abstraction/exception.

### DUI-PLATFORM-003
Milestone/release native GUI certification.

Expected: Windows x64, Ubuntu LTS x64, and macOS Apple Silicon each launch and pass the defined smoke checklist.

### DUI-PLATFORM-004
Introduce/review a platform-sensitive dependency.

Expected: compatibility assessment is required before accepting any reduction in supported targets.

### DUI-PLATFORM-005
Reference-consumer adoption.

Expected: consumer inherits the framework platform matrix and separately reports any app-specific exception.

### DUI-THEME-TOKEN-001
Render all standard templates in Light and Dark themes.

Expected:
semantic token resources resolve correctly; no critical text/control becomes unreadable.

### DUI-THEME-TOKEN-002
Override application Accent and ApplicationLogo in a consumer app.

Expected:
brand presentation changes without modifying template source code.

### DUI-ICON-001
Resolve standard semantic keys:
Search, Import, Export, Approve, Warning, Formula.

Expected:
each resolves through `IIconRegistry` to a valid SVG/resource.

### DUI-ICON-002
Override `IconKey.Export` in the consumer app.

Expected:
app override is rendered while framework command semantics remain unchanged.

### DUI-ICON-003
Register an app-specific semantic icon key.

Expected:
icon resolves through registry without modifying framework template projects.

### DUI-THEME-TOKEN-003
Switch System → Light → Dark at runtime with DataEntry, Dashboard, Report, and Signing templates open.

Expected:
theme resources and semantic icons update safely without losing application state.

### DUI-APPEAR-001
Switch global font:
Small → Normal → Large.

Expected:
shell/templates/grid text scale consistently and remain readable.

### DUI-APPEAR-002
Switch grid density:
Compact → Comfortable → Large.

Expected:
row/header geometry changes while data and VariableCodes remain unchanged.

### DUI-GRIDLAYOUT-001
Resize multiple columns and reorder visible columns.

Expected:
layout changes immediately and can be persisted as user preference.

### DUI-GRIDLAYOUT-002
Set a user preference, then reload the workspace.

Expected:
the preference is restored for the same User + Company + Workspace/Grid identity.

### DUI-GRIDLAYOUT-003
Remove a column from the published definition while an old user width preference exists.

Expected:
the removed column does not reappear from stale preference data.

### DUI-GRIDLAYOUT-004
Invoke Reset UI Layout.

Expected:
user overrides are cleared and the current published layout defaults are restored.

### DUI-APPEAR-PERF-001
Open a 100K-row grid, then change font size/density/row height.

Expected:
viewport capacity and bounded prefetch are recalculated without materializing all 100K rows.

### DUI-PERF-100K-001
Dataset:
- 100,000 rows
- at least 30 dynamic columns

Verify:
- open grid;
- first meaningful paint;
- scroll top → middle → bottom;
- no crash;
- no full dataset materialization requirement;
- bounded row-object growth.

### DUI-PERF-VIEWPORT-001
Dataset:
- 100,000 rows
- viewport displays approximately 40 rows
- configured prefetch buffer

Expected:
only visible rows plus bounded buffer are materialized.

### DUI-PERF-VIEWPORT-002
Start near row 1,000, then drag scrollbar directly to approximately row 90,000.

Expected:
provider queries target window directly; intermediate 89,000 rows are not sequentially loaded.

### DUI-PERF-VIEWPORT-003
Edit a cell on row 5,000, then scroll far enough that row 5,000 is unloaded.

Expected:
candidate edit remains in sparse edit buffer and is restored when row 5,000 returns to viewport.

### DUI-PERF-VIEWPORT-004
Select rows/ranges, scroll them out of viewport, then return.

Expected:
logical selection remains correct without retaining visual row instances.

### DUI-PERF-VIEWPORT-005
Rapidly scroll through multiple distant windows.

Expected:
obsolete requests are cancelled/coalesced where supported; stale responses do not replace the final viewport.

### DUI-PERF-VIEWPORT-006
Navigate repeatedly through distant windows.

Expected:
window cache remains bounded and does not grow toward full-dataset materialization.

### DUI-PERF-100K-002
On the same 100K dataset perform:
- sort;
- filter;
- search;
- clear filter.

Expected:
operations execute through provider/query boundary and grid remains responsive/stable.

### DUI-PERF-PASTE-001
Paste 10,000 candidate cells into an editable region.

Expected:
- chunked processing;
- progress/state available;
- validation results structured;
- Formula/System cells remain unchanged;
- no full-dataset clone;
- UI remains operational.

### DUI-PERF-SELECT-001
Select all 100,000 rows.

Expected:
selection model represents the scope without requiring 100,000 visual row objects.

### DUI-PERF-EXPORT-001
Export all matching 100,000 rows.

Expected:
export uses provider/data source and does not require visual materialization/scrolling.

### DUI-PERF-IMPORT-001
Import an Excel dataset containing 100,000 rows.

Expected:
streaming/chunked import path, validation in batches, stable memory behavior, and no UI crash.

### DUI-PERM-001
Provide the same user with different permission sets for Company A and Company B.

Expected:
switching Company changes resolved Ribbon/Tree/Workspace/Action presentation according to
the new Company-scoped permission context without restarting the app.

### DUI-PERM-002
Configure a grid column with:
`UnauthorizedBehavior = READ_ONLY`.

Expected:
user lacking the edit permission can view but cannot edit/paste/import into that column.

### DUI-PERM-003
Configure a Ribbon command with:
`UnauthorizedBehavior = HIDE`.

Expected:
command is not rendered for a user lacking the required permission.

### DUI-PERM-004
Authorization presentation provider becomes unavailable.

Expected:
privileged commands fail closed; shell remains usable; no optimistic allow behavior.

### DUI-PERM-005
UI displays an action as enabled, but backend rejects the authoritative command.

Expected:
backend rejection is surfaced; UI does not override or retry by bypassing authorization.

### DUI-PERM-006
Switch from Company A to Company B while a permission cache exists.

Expected:
Company A capability set is not reused for Company B; cache key/invalidation is Company-scoped.

### DUI-ACTIONBAR-001
Define Top and Bottom Action Bars with multiple actions.

Expected:
both bars render in configured order without hard-coded template XAML actions.

### DUI-ACTIONBAR-002
Select 0, 1, and multiple rows.

Expected:
selection-aware actions enable/disable according to metadata and supplied capabilities.

### DUI-ACTIONBAR-003
Bottom bar receives row/error/pending-change state.

Expected:
status summary renders deterministically.

### DUI-ACTIONBAR-004
Provide an unknown registered action.

Expected:
template remains stable and shows unavailable/error state.

### DUI-SIGN-001
Provide signing work queue items with multiple statuses.

Expected:
list, selected detail, preview descriptor, status, and timeline render correctly.

### DUI-SIGN-002
Register Submit/Approve/Reject/Sign commands.

Expected:
UI dispatches exact selected item identity through command provider and performs no local signing logic.

### DUI-SIGN-003
Search source tree for PKCS#11/private-key/HSM signing implementation inside Dynamic UI.

Expected:
NONE.

### DUI-SIGN-004
Application denies Sign after UI previously displayed it as enabled.

Expected:
application denial is surfaced; UI does not override or bypass it.

### DUI-SIGN-005
Signing item has historical events.

Expected:
history renders read-only and exact event order/timestamps are preserved.

### DUI-APPMENU-001
Launch the shell with standard services only.

Expected:
Application button/logo opens Language, Appearance, About, and Exit.

### DUI-APPMENU-002
Register Company Context, Account, and License services.

Expected:
corresponding sections appear without modifying shell XAML.

### DUI-APPMENU-003
Register an app-specific `IApplicationMenuContributor`.

Expected:
contributed settings section appears in configured order.

### DUI-APPMENU-004
Switch vi-VN ↔ en-US and Light ↔ Dark ↔ System.

Expected:
shell and loaded templates update without restart where runtime switching is supported.

### DUI-APPMENU-005
Contributor throws/fails during resolution.

Expected:
shell remains usable and presents deterministic unavailable/error behavior.

### DUI-APPMENU-006
Invoke Exit.

Expected:
registered clean-shutdown command executes and application exits cleanly.

### DUI-RIBBON-001
Define two tabs, multiple groups, and commands in metadata.

Expected:
Ribbon renders the configured hierarchy without application-specific XAML changes.

### DUI-RIBBON-002
Reorder a tab/group/command in Setup and publish a new definition version.

Expected:
runtime renders the new order after loading the published definition.

### DUI-RIBBON-003
Switch from a normal workspace to a contextual workspace.

Expected:
contextual groups/commands appear/disappear according to metadata and supplied capabilities.

### DUI-RIBBON-004
Provide a command requiring a permission/capability the user does not have.

Expected:
command follows configured hidden/disabled behavior and cannot bypass application authorization.

### DUI-RIBBON-005
Provide an unknown or malformed registered command code.

Expected:
shell remains stable and presents deterministic unavailable/error feedback.

### DUI-MODULE-001
Register the five standard template modules.

Expected:
`TemplateRegistry` resolves all five by `TemplateCode`.

### DUI-MODULE-002
Register a sample custom template `CALENDAR`.

Expected:
host resolves the new module without modifying existing five template implementations.

### DUI-MODULE-003
Architecture test template dependencies.

Expected:
- no template references PayCalc24;
- Core references no template;
- Dashboard does not reference DataEntry template;
- shared behavior resolves through Core/Shared/Extensions.

### DUI-DASHBOARD-001
Provide a dashboard tree with parent and child nodes.

Expected:
- tree renders hierarchy from metadata;
- selecting each child resolves its own dashboard definition;
- no dedicated view class is required for each node.

### DUI-DASHBOARD-002
Provide KPI, Pie, Column, Line, and Data Grid widgets.

Expected:
- all widgets render from metadata and supplied data bindings;
- widget order/size follows definition.

### DUI-DASHBOARD-003
Change dashboard shared time filter.

Expected:
- compatible widgets refresh using the new time context;
- authoritative source data is not mutated.

### DUI-DASHBOARD-004
Configure a chart segment drill-down to a registered detail target.

Expected:
- selecting the segment navigates to the target with the declared filter binding.

### DUI-DASHBOARD-005
Return an error for one widget while other widgets succeed.

Expected:
- failed widget shows its own error state;
- successful widgets remain usable.

### DUI-TIME-001
Workspace metadata enables `MONTH` and `DATE_RANGE`.

Expected:
time filter appears and changes query/view state without modifying source rows.

### DUI-BATCH-001
Select 18 of 125 records.

Expected:
UI explicitly displays `18 selected`; batch action receives only those 18 identities.

### DUI-BATCH-002
Batch job completes 92 success / 8 failure.

Expected:
status = Partial; failed items are visible and eligible for Retry Failed when provider supports retry.

## 58. Implementation Order

### Phase A — Modular Foundation
- Core project
- Shared project
- `IDynamicTemplate`
- `TemplateRegistry`
- module discovery/registration
- architecture dependency tests
- Demo host

### Phase B — Core Metadata
- Tree definition
- Workspace definition
- Column definition
- Variable definition
- capability metadata
- localization metadata

### Phase C — DynamicSetupTemplate
- dynamic tree
- selected workspace
- metadata grid/editor
- parent/child management

### Phase D — DynamicDataEntryTemplate
- dynamic columns
- editors
- selection
- sort/filter/search
- keyboard interaction

### Phase E — Clipboard
- TSV copy
- multi-cell paste
- protected-column handling
- candidate edit buffer

### Phase F — Import/Export
- Excel import/export
- template export
- PDF export

### Phase G — DynamicReportTemplate
- report tree
- metadata designer
- preview
- Excel/PDF export

### Phase H — DynamicHistoryDocumentTemplate
- history grid
- search/filter
- master/detail
- version selection
- viewer adapter resolver
- PDF/XML/text/image proof adapters
- capability-controlled preview/download/export

### Phase I — Visual XML Report / Document Designer
- variable catalog
- scalar/collection bindings
- visual design surface
- safe XML persistence
- preview
- template version/clone

### Phase J — Universal Time Filter
- metadata
- reusable toolbar/filter controls
- date range/month/quarter/year/period modes

### Phase K — Batch Action Framework
- selection model
- action metadata
- export/share/execute provider contracts
- progress/partial failure/retry presentation

### Phase L — DynamicDashboardTemplate
- dynamic dashboard tree/view selector
- dashboard definition host
- responsive widget canvas
- KPI cards
- Pie/Donut charts
- Column/Bar charts
- Line/Area charts
- Data Grid / Top-N / Status / Progress widgets
- shared time context
- drill-down/navigation
- widget-level states
- refresh and permission metadata

### Phase M — Design Tokens and Semantic Icons
- shared design token contracts
- Light/Dark/System resource dictionaries
- Actipro/Avalonia token mapping
- semantic IconKey registry
- default SVG catalog
- consumer brand overlay
- runtime theme/icon switching tests

### Phase N — Appearance and Grid Preferences
- global font/UI scale settings
- grid density presets
- column resize/order/visibility preferences
- row-height preference
- auto-fit
- preference persistence
- Reset UI Layout
- viewport geometry recalculation integration

### Phase O — Large Data Grid Performance Foundation
- `IDataGridDataProvider`
- viewport-driven window coordinator
- row/window virtualization path
- bounded prefetch before/after viewport
- direct distant-offset loading
- stable row identity across unload/reload
- stale request cancellation/coalescing
- bounded recent-window cache
- provider-based sort/filter/search
- scalable selection model
- sparse candidate edit buffer
- chunked paste/import/export
- async progress/cancellation
- benchmark/demo harness
- 100K-row tests

### Phase P — Company-Scoped Permission Presentation
- Company Context integration contract
- authorization presentation provider
- effective capability context
- HIDE/DISABLE/READ_ONLY resolver
- Company-switch invalidation/refresh
- fail-closed behavior
- architecture/security tests

### Phase Q — Dynamic Action Bars
- Top/Bottom Action Bar metadata
- registered command dispatch
- selection-aware actions
- status/selection summary
- shared template integration

### Phase R — DynamicSigningTemplate
- signing work queue
- detail/preview host
- actor/status presentation
- comments/reasons
- timeline/history
- registered Submit/Approve/Reject/Sign actions
- security boundary tests

### Phase S — PayCalc24 Adoption
Use PayCalc24 as first real consumer without moving PayCalc24 business logic into the framework.

## 59. Non-Negotiable Rules

1. Dynamic UI Framework contains no payroll semantics.
2. Dynamic UI Framework does not calculate authoritative formula results.
3. Formula definitions may be configured through Setup but are executed by the consuming application's engine.
4. `VariableCode` is stable semantic identity.
5. Display names are localization/presentation metadata.
6. `FORMULA` and `SYSTEM` columns are read-only and cannot be pasted/imported by default.
7. Tree navigation is dynamic and parent/child metadata-driven.
8. Dynamic columns are metadata-driven.
9. Import/paste never bypass validation/application commit boundaries.
10. Excel-like UX must not sacrifice audit/version/permission controls.
11. Import/export is a reusable framework capability.
12. Framework must be reusable by applications other than PayCalc24.
13. Historical document/version selection must resolve the exact selected version.
14. Document preview/download/export must remain capability-controlled.
15. Dynamic UI must never bypass application security by opening arbitrary file paths.
16. XML layout templates are declarative layout definitions, never arbitrary executable code.
17. Report/document variables must be explicitly exposed by the consuming application.
18. Universal time filters are presentation/query state only.
19. Batch actions must execute through application/provider boundaries, never by bypassing domain security.
20. Batch selection scope must always be explicit to the user.
21. XML is the canonical portable layout format; compiled render models are runtime derivatives.
22. Published layout versions must render deterministically and never silently resolve to latest.
23. Layout XML and business/data XML are separate concepts.
24. Screen preview should use validated/compiled layouts rather than invoking the designer on every view.
25. Excel export prioritizes structured data; PDF prioritizes layout fidelity.
26. Dashboard widgets are presentation/query consumers and must not become authoritative calculation engines.
27. Dashboard nodes and widgets must be metadata-driven and reusable across applications.
28. Drill-down must target registered navigation/actions; metadata must not execute arbitrary code.
29. A single widget failure should not unnecessarily invalidate the entire dashboard.
30. Dashboard time context must use the shared Universal Time Filter contract.
31. Each template is an independently maintainable module.
32. Template modules must not depend on one another directly unless explicitly approved by architecture; shared behavior belongs in Core/Shared/Extensions.
33. New template types must be addable through registration without modifying existing templates.
34. PayCalc24 and other applications are consumers only and must never become dependencies of Dynamic UI modules.
35. Module-level documentation, tests, and compatibility declarations are required deliverables.
36. Ribbon tabs, groups, and commands are metadata-driven and configurable through Setup.
37. Contextual Ribbon behavior must resolve through metadata and application-supplied state/capabilities.
38. Ribbon definitions must never embed arbitrary executable business logic.
39. Application-specific Ribbon definitions must not be hard-coded into the generic host.
40. Application Menu is a shared shell capability separate from business Ribbon and file/document modules.
41. Standard Language/Appearance/About/Exit behavior belongs to the shell.
42. App-specific settings are registered through contributor contracts rather than shell-core changes.
43. Application Menu presentation must never replace authoritative authentication, authorization, or entitlement enforcement.
44. Every template may expose metadata-driven Top and Bottom Action Bars.
45. Action Bar metadata is presentation/dispatch-only and never contains authoritative business logic.
46. DynamicSigningTemplate is UI-only; all signing and approval execution remains below the UI boundary.
47. Dynamic UI must never hold private keys, perform PKCS#11/HSM operations, or create authoritative digital signatures.
48. Signing/approval buttons never replace application/API authorization and workflow validation.
49. Permissions/capabilities are Company-scoped when the consuming application supports Company Context.
50. Switching Company must refresh/re-resolve authorization presentation state.
51. Dynamic UI may declare required permission/capability codes but must never assign user roles/permissions.
52. HIDE/DISABLE/READ_ONLY are presentation behaviors only; authoritative enforcement remains below the UI.
53. Unresolved authorization/capability state must fail closed for privileged actions.
54. DynamicDataEntryTemplate must support a provider-based 100K+ row mode.
55. Large-grid operation must not require materializing the full dataset as UI row objects.
56. Large sort/filter/search/export operations must be able to execute through the data-provider boundary.
57. Candidate edits must be sparse; full-dataset cloning is prohibited as the default edit model.
58. Large paste/import/validation/commit operations must support chunked/asynchronous processing.
59. Final large-grid release acceptance requires real native GUI verification on macOS ARM64 and Windows x64.
60. Large grids must use viewport-driven windowed loading with a bounded prefetch buffer.
61. Direct navigation to distant row positions must not require sequential loading of intermediate rows.
62. Row identity, pending edits, validation diagnostics, and logical selection must survive visual row unload/reload.
63. Viewport/window caches must remain bounded and must never become accidental full-dataset caches.
64. Stale asynchronous viewport responses must not overwrite newer requested viewport state.
65. Font size/UI scale are global appearance preferences by default; per-grid font divergence is discouraged.
66. Grid column width, row height/density, order, and visibility are presentation preferences only.
67. User grid preferences must never alter published metadata, VariableCodes, business rules, or permissions.
68. Appearance/layout changes must not trigger full-dataset materialization.
69. Grid preference persistence must be scoped by stable user/company/workspace identities where Company Context applies.
70. Reusable templates must use semantic design tokens rather than app-specific raw colors.
71. Reusable templates must reference semantic `IconKey` values rather than direct SVG file paths.
72. Consumer branding is an overlay and must not require editing framework template code.
73. Theme/icon presentation must never alter authoritative application/business semantics.
74. Chart/status colors are presentation aids only and must not be the sole carrier of business meaning.
75. DynamicUI24 official Tier-1 desktop targets are Windows x64, Ubuntu LTS x64, and macOS Apple Silicon.
76. Required publish validation targets from v0.7 are `win-x64`, `win-arm64`, `osx-arm64`, `osx-x64`, and `linux-x64`.
77. Publish success must never be reported as native GUI certification.
78. Platform-sensitive dependencies must not silently reduce the supported platform matrix.
79. Official Linux desktop support means Ubuntu LTS x64 unless expanded by a later specification.
80. Consumer applications inherit the DynamicUI24 platform matrix by default and must document explicit compatibility exceptions.
81. Tasks 0–4 remain closed; cross-platform retrofit is performed through Maintenance M1.










## 60. Supported Desktop Platform Matrix

DynamicUI24 is a reusable cross-platform desktop UI framework with an explicit, versioned platform policy.

### 60.1 Tier 1 / P0 — Official Desktop Targets

| Platform | RID | Status | Strategic Role |
|---|---|---|---|
| Windows x64 | `win-x64` | Official / P0 | Largest current enterprise desktop base |
| Ubuntu LTS x64 | `linux-x64` | Official / P0 | Strategic enterprise Linux target; allows supported desktop deployment without requiring Windows desktop licensing |
| macOS Apple Silicon | `osx-arm64` | Official / P0 | Primary modern macOS target |

Tier-1 targets are required for milestone/release native GUI certification.

### 60.2 Tier 2 / P1 — Compatibility Targets

| Platform | RID | Status |
|---|---|---|
| Windows ARM64 | `win-arm64` | Compatibility / P1 |
| macOS Intel | `osx-x64` | Compatibility / P1 |

P1 targets are included in build/publish validation. Native GUI smoke is performed at milestone/release when suitable runtime/hardware is available.

### 60.3 Tier 3 / P2 — Future / Best Effort

| Platform | RID | Status |
|---|---|---|
| Linux ARM64 | `linux-arm64` | Future / Best Effort / P2 |

Other Linux distributions may work, but official desktop Linux support is scoped to **Ubuntu LTS x64** unless expanded later.

### 60.4 Required Publish Matrix From v0.7 Forward

Every UI task that reaches publish validation must publish:

```text
win-x64
win-arm64
osx-arm64
osx-x64
linux-x64
```

Typical commands:

```bash
dotnet publish <Project> -c Release -r win-x64    --self-contained true
dotnet publish <Project> -c Release -r win-arm64  --self-contained true
dotnet publish <Project> -c Release -r osx-arm64  --self-contained true
dotnet publish <Project> -c Release -r osx-x64    --self-contained true
dotnet publish <Project> -c Release -r linux-x64  --self-contained true
```

`linux-arm64` may be added to future release pipelines.

### 60.5 Per-Task Validation vs Native GUI Certification

**Per UI task:**
- restore;
- Release build;
- tests;
- five-RID publish validation;
- real native GUI smoke on the designated development verification platform, initially `osx-arm64`.

**Milestone/release certification:**
- Windows x64 native GUI smoke;
- Ubuntu LTS x64 native GUI smoke;
- macOS Apple Silicon native GUI smoke.

P1 native smoke (`win-arm64`, `osx-x64`) is required when suitable runtime/hardware is available for release certification.

A successful publish does **not** equal native GUI certification.

### 60.6 Ubuntu LTS Policy

Official Linux desktop support means **Ubuntu LTS x64**.

This keeps the support baseline predictable while providing an enterprise deployment path that does not require Windows desktop licensing.

Do not claim generic "all Linux" support unless additional distributions are formally qualified.

### 60.7 Cross-Platform Dependency Rule

Before introducing a platform-sensitive package/native dependency:

1. verify its support against the platform matrix;
2. document the compatibility result;
3. add build/publish tests where appropriate;
4. avoid silently reducing supported targets.

Breaking a Tier-1 target requires an explicit architecture decision and release/specification note.

### 60.8 GUI Library / Native Dependency Verification

Avalonia, Actipro, document viewers, renderers, and other native/platform-sensitive libraries must be runtime-verified separately from RID publish success.

Example:

```text
linux-x64 publish PASS
```

does not prove:

```text
Ubuntu LTS x64 GUI runtime PASS
```

### 60.9 CI Policy

CI should build/test on Ubuntu, Windows, and macOS, and validate the required RID publish matrix where practical.

### 60.10 Platform-Specific Code

Platform-specific implementations are allowed only behind explicit abstractions.

- keep Core/contracts platform-neutral;
- avoid OS branches in reusable template logic;
- isolate native implementations;
- provide fallback/error behavior;
- test platform-specific adapters.

### 60.11 Consumer Application Inheritance

Consuming applications should inherit the DynamicUI24 supported platform matrix by default.

Recommended policy:

> This application consumes DynamicUI24 and targets the DynamicUI24 supported desktop platform matrix. Application-specific dependencies must not reduce the supported platform set without an explicit compatibility exception.

PayCalc24 is the first reference consumer expected to adopt this policy.

### 60.12 Application-Specific Compatibility Exception

If a consuming application cannot support one target because of an application-specific dependency:

- document the affected platform;
- document the blocking dependency;
- state whether the limitation is temporary/permanent;
- keep the exception visible in release documentation.

### 60.13 Retrofit of Closed Tasks

Tasks 0–4 remain CLOSED.

Do not reopen them individually.

Create one task:

```text
Maintenance M1 — Cross-Platform Baseline Upgrade
```

M1 must:
- preserve Task 0–4 behavior;
- add/verify the five required publish RIDs;
- review existing dependencies against the matrix;
- add platform compatibility documentation/regression guards;
- keep architecture/tests green;
- use Spec v0.7 as source of truth.

Task 5 and later use the v0.7 platform matrix directly.

### 60.14 PayCalc24 Adoption

PayCalc24 should receive a separate maintenance/adoption task rather than reopening its historical UI tasks:

```text
PayCalc24 Maintenance — Adopt DynamicUI24 Platform Matrix v0.7
```

Expected PayCalc24 desktop publish targets:

```text
win-x64
win-arm64
osx-arm64
osx-x64
linux-x64
```

PayCalc24-specific dependencies must be verified separately.


## 61. Design Tokens and Semantic SVG Icon System

The framework uses a shared design-token system for colors, typography roles, spacing,
state presentation, and reusable surfaces.

Templates must not hard-code visual values that belong to the shared design system.

### 62.1 Design Token Principle

Template code should reference semantic resources such as:

```text
SurfaceBackground
SurfaceSecondary
TextPrimary
TextSecondary
BorderDefault
Accent
AccentForeground
SelectionBackground
SelectionForeground
HoverBackground
DisabledForeground
Success
Warning
Error
Info
GridHeaderBackground
GridCellBackground
GridReadOnlyBackground
GridCalculatedBackground
ValidationErrorBackground
ValidationWarningBackground
```

Avoid direct values such as:

```text
#1677FF
#FFFFFF
#202020
```

inside reusable template implementations except inside the canonical theme resource
definitions themselves.

### 62.2 Theme Layers

Recommended resource layering:

```text
Framework Semantic Tokens
        ↓
Light Theme Values
Dark Theme Values
System Theme Resolver
        ↓
Avalonia / Actipro Resource Mapping
        ↓
Template Rendering
        ↓
Optional App Brand Overrides
```

`System` resolves to the operating-system/application appearance policy.

### 62.3 App Brand Override

A consuming app may provide a brand overlay without modifying framework template code.

Typical brand overrides:

```text
ApplicationLogo
Accent
AccentForeground
BrandPrimary
BrandSecondary
App-specific semantic IconKeys
Optional typography token overrides
```

Examples:

```text
PayCalc24 Brand Override
DigiDokument Brand Override
ContractSigning Brand Override
```

The framework must continue to provide accessible fallback tokens when an app does not
override them.

### 62.4 Token Categories

Recommended categories:

#### Color

```text
Surface.*
Text.*
Border.*
Accent.*
State.Success
State.Warning
State.Error
State.Info
Grid.*
Chart.*
Document.*
```

#### Typography

```text
FontFamily.Default
FontSize.Small
FontSize.Normal
FontSize.Large
FontWeight.Normal
FontWeight.Medium
FontWeight.Bold
```

#### Spacing

```text
Spacing.XS
Spacing.SM
Spacing.MD
Spacing.LG
Spacing.XL
```

#### Geometry

```text
CornerRadius.Small
CornerRadius.Medium
BorderThickness.Default
ControlHeight.Compact
ControlHeight.Normal
ControlHeight.Large
```

Exact token names may evolve, but templates must consume semantic tokens rather than
duplicating raw visual values.

### 62.5 Semantic IconKey

Icons are referenced using semantic keys.

Examples:

```text
IconKey.Search
IconKey.Filter
IconKey.Refresh
IconKey.Add
IconKey.Edit
IconKey.Delete
IconKey.Import
IconKey.Export
IconKey.Preview
IconKey.Validate
IconKey.Commit
IconKey.Approve
IconKey.Reject
IconKey.Sign
IconKey.History
IconKey.Company
IconKey.Report
IconKey.Warning
IconKey.Error
IconKey.Formula
IconKey.Settings
```

Template code must not directly reference:

```text
Assets/search.svg
Assets/export.svg
```

### 62.6 Icon Registry

Conceptual resolution:

```text
IconKey
   ↓
IIconRegistry
   ↓
Framework Default Icon Catalog
   +
App Icon Overrides / Additions
   ↓
SVG Resource
```

A consuming application may replace a default semantic icon or register additional
app-specific keys.

### 62.7 SVG Requirements

Recommended SVG guidelines:

- vector only where practical;
- consistent viewBox, e.g. 24×24;
- consistent visual weight;
- theme-aware monochrome/icon-color binding where practical;
- no embedded business semantics in file names/paths;
- accessible contrast in Light/Dark themes.

### 62.8 Template Rule

Reusable template modules must consume:

```text
ThemeToken
IconKey
```

not:

```text
RawColor
DirectSvgPath
```

This applies to:

- Application Menu;
- Ribbon;
- Tree;
- Action Bars;
- Data Grid;
- Dashboard widgets;
- Report/Document designer;
- History/Document viewer;
- Signing template;
- dialogs and empty/loading/error states.

### 62.9 Chart Tokens

Dashboard/chart components should use semantic chart tokens or a theme-aware palette.

Example:

```text
Chart.Series.1
Chart.Series.2
Chart.Series.3
Chart.Positive
Chart.Negative
Chart.Warning
Chart.Neutral
```

Do not hard-code app-specific chart colors in reusable widget code.

### 62.10 State Tokens

Status/validation presentation should use semantic tokens:

```text
Success
Warning
Error
Info
ReadOnly
Calculated
Selected
Disabled
```

Business logic must never be inferred from color.

### 62.11 Runtime Theme Switching

Switching:

```text
System
Light
Dark
```

must re-resolve tokens and icons safely at runtime.

Templates should not require recreation solely to adopt a theme change unless a control
library specifically requires it.

### 62.12 Brand Change Scope

Brand overrides affect presentation only.

Changing logo/accent/icons must not alter:

- metadata identity;
- VariableCode;
- permissions;
- workflow;
- calculations;
- report data;
- document identity.

### 62.13 Resource Ownership

Recommended ownership:

```text
TS24.DynamicUI.Shared
  └─ token/icon contracts

TS24.DynamicUI.Avalonia
  ├─ Light resources
  ├─ Dark resources
  ├─ System resolver
  ├─ default SVG catalog
  └─ Actipro/Avalonia mapping

Consumer App
  └─ optional brand/resource overlay
```

### 60.14 No Font Files in Distribution Contract

The framework may reference installed/system font families or app-approved font-family
names.

Font files themselves are not part of the framework's public artifact contract unless
separately licensed and managed by the consuming application.


## 62. Appearance and Grid User Preferences

The framework provides two levels of visual sizing configuration:

1. **Global Application Appearance**
2. **Per-Grid Layout Preferences**

The goal is to keep application typography visually consistent while still allowing users
to adjust dense tabular workspaces to their preferred working style.

### 62.1 Global Appearance Settings

The Application Menu is the standard location for global appearance preferences.

Recommended structure:

```text
Application Menu
└─ Appearance
   ├─ Theme
   │  ├─ System
   │  ├─ Light
   │  └─ Dark
   ├─ UI Scale
   │  ├─ 90%
   │  ├─ 100%
   │  ├─ 110%
   │  └─ 125%
   ├─ Font Size
   │  ├─ Small
   │  ├─ Normal
   │  └─ Large
   └─ Grid Density
      ├─ Compact
      ├─ Comfortable
      └─ Large
```

Exact presets may evolve, but the framework must expose a consistent global appearance model.

### 62.2 Global Font Policy

Font size should normally be controlled globally rather than independently per grid.

This prevents inconsistent typography between workspaces.

Global font/display settings may affect:

- shell;
- Ribbon;
- Application Menu;
- Tree;
- grid headers;
- grid cells;
- action bars;
- dialogs;
- Dashboard;
- Report/History/Signing templates.

A consuming application may override specific typography only where its design explicitly requires it.

### 62.3 Per-Grid Layout Preferences

Each grid should allow the user to adjust:

- column width;
- row height;
- column order;
- column visibility where permitted;
- frozen/pinned columns where supported;
- auto-fit current column;
- auto-fit all columns;
- reset grid layout.

These are presentation preferences only.

They must not change:

- data definition;
- VariableCode;
- business rules;
- permissions;
- report/data semantics.

### 62.4 Column Width

Users may resize columns by dragging headers.

The grid may support:

```text
Auto Fit Column
Auto Fit All Columns
Reset Width
```

Column width should respect configured minimum/maximum constraints where defined.

### 62.5 Row Height

Users may adjust row height through:

- Grid Density preset;
- optional explicit row-height preference where enabled.

Changing row height must recalculate viewport capacity.

It must not trigger full-dataset materialization.

### 62.6 Grid Density

Recommended density presets:

```text
COMPACT
COMFORTABLE
LARGE
```

A density preset may control:

- row height;
- cell padding;
- header height;
- icon size/padding within grid surfaces.

Density is a presentation preference and should not change application data.

### 62.7 Preference Persistence

Grid layout preferences may be persisted using a stable identity such as:

```text
UserId
CompanyId
WorkspaceDefinitionId
GridDefinitionId
PreferenceVersion
```

Suggested persisted preferences:

```text
ColumnWidths
ColumnOrder
ColumnVisibility
FrozenColumns
RowHeight / Density
LastUsedLayout
```

A user may therefore use different grid layouts in different Companies/workspaces when appropriate.

### 62.8 Layout Definition vs User Preference

The framework must distinguish:

```text
Published Grid Definition
```

from:

```text
User Grid Preference
```

Example:

```text
Published definition:
NEW_CR width = 120

User preference:
NEW_CR width = 180
```

The user preference overrides presentation only.

If the published definition removes the column, the stale user preference must not recreate it.

### 62.9 Reset UI Layout

The Application Menu should expose a standard action such as:

```text
Reset UI Layout
```

The action may offer scopes such as:

```text
Current Grid
Current Workspace
Current Application
```

The exact options may be provided by the consuming application/framework host.

Resetting layout returns presentation preferences to the current published defaults.

### 62.10 Appearance Change and Viewport Recalculation

Changing:

- font size;
- UI scale;
- grid density;
- row height;

changes the number of rows that fit in the viewport.

Required flow:

```text
Appearance / Density Change
        ↓
Recalculate Viewport Dimensions
        ↓
Recalculate ViewportRowCount
        ↓
Recalculate Bounded Prefetch Window
        ↓
Request/Reuse required data window
        ↓
Render
```

The implementation must not reload or materialize all 100K+ rows simply because appearance changed.

### 62.11 Horizontal / Vertical Scrolling

When configured columns exceed available width:

- horizontal scrolling remains available;
- frozen identity columns stay visible where enabled;
- virtualized behavior remains stable.

Vertical scrolling continues to use viewport-driven windowed loading.

### 62.12 Accessibility and Readability

Appearance settings should not make critical UI unreadable.

At minimum verify:

- Small / Normal / Large font modes;
- Compact / Comfortable / Large density;
- Light / Dark / System themes;
- grid headers;
- selected cells;
- read-only/calculated cells;
- validation markers;
- action bars.

### 62.13 Performance Rule

Visual preference changes must remain independent of authoritative data size.

The rule is:

> Change presentation geometry, not dataset materialization.


## 63. Large Data Grid Performance — 100K+ Rows

`DynamicDataEntryTemplate` must be designed for practical datasets of **100,000 rows or more**.

This is a non-negotiable framework requirement.

The template must not rely on materializing the entire dataset as UI objects.

Forbidden default architecture:

```text
API / Database
   ↓
List<100000 rows>
   ↓
ObservableCollection<100000 view-model rows>
   ↓
DataGrid renders/manages all rows
```

Preferred architecture:

```text
Authoritative Data Source
        ↓
IDataGridDataProvider
        ↓
Query / Search / Sort / Filter
        ↓
Windowed / Paged Result
        ↓
Virtualized Grid View
        ↓
Only visible / nearby rows are materialized
```

### 62.1 Data Provider Contract

Conceptual contract:

```text
IDataGridDataProvider
  GetCountAsync(...)
  QueryAsync(offset, limit, sort, filter, search, timeFilter, ...)
  ValidateBatchAsync(...)
  CommitBatchAsync(...)
  ExportAsync(...)
```

The exact API may evolve, but the provider must allow the UI to operate without owning the full
100K+ dataset in memory.

### 62.2 Virtualization

Required:

- row virtualization;
- column virtualization where supported/practical;
- incremental/windowed loading;
- bounded row materialization;
- stable selection by row identity rather than UI-object identity.

Scrolling from top to middle to bottom must not require constructing all intermediate rows.


### 62.3 Viewport-Driven Windowed Loading

The grid must load data according to the visible viewport plus a configurable buffer.

Conceptual example:

```text
Total dataset:
100,000 rows

Visible viewport:
Rows 5,000 → 5,040

Loaded window:
Rows 4,940 → 5,140
```

When the user scrolls forward, the loaded window moves accordingly:

```text
New viewport:
Rows 5,100 → 5,140

New loaded window:
Rows 5,040 → 5,240
```

Rows far outside the active window may be released from UI/view-model memory.

#### 60.3.1 Prefetch

The provider/viewport coordinator should prefetch rows ahead of the user's current scroll
direction to reduce visible loading pauses.

Prefetch behavior should be configurable using metadata/runtime settings such as:

```text
ViewportRowCount
BufferBefore
BufferAfter
PrefetchChunkSize
PrefetchDirection
```

Exact defaults are implementation-defined and should be tuned using benchmarks.

#### 60.3.2 Direct Scroll / Jump

The grid must support direct navigation to distant data positions without materializing all
intermediate rows.

Example:

```text
Current position: row 1,000
User drags scrollbar to approximately row 90,000
```

Expected:

```text
Resolve target offset
→ Query provider for the target window
→ Materialize visible/buffer rows
→ Render
```

The grid must not load rows 1,001 through 89,999 sequentially.

#### 60.3.3 Total Count and Scrollbar

The viewport layer must know or estimate the total row count through the data provider.

The scrollbar should represent the logical dataset size, even if only a small window of rows
is currently materialized.

#### 60.3.4 Stable Row Identity

Row identity must remain stable independently of visual row lifetime.

A row moving outside the viewport may be unloaded, but the framework must preserve:

- row identity;
- selection state where applicable;
- pending candidate edits;
- validation diagnostics;
- relevant row-level UI state.

#### 60.3.5 Candidate Edit Persistence Across Viewport Unload

Candidate edits are stored outside visual row objects.

Example:

```text
RowId = 5000
VariableCode = NEW_CR
OldValue = 80000000
CandidateValue = 120000000
```

If row 5000 leaves the viewport, the candidate edit remains in the sparse edit buffer.

When row 5000 returns to the viewport, the grid hydrates the row using the pending candidate
value.

#### 60.3.6 Selection Across Unloaded Rows

Selection must not depend on keeping visual rows alive.

Examples:

```text
Selected RowIds
Selected Range
All Matching
```

must remain logically valid when rows are unloaded/reloaded by viewport movement.

#### 60.3.7 Loading Presentation

If a requested viewport window is not immediately available:

- preserve shell responsiveness;
- show lightweight loading placeholders/state;
- avoid flashing/clearing the entire grid when practical;
- cancel obsolete viewport requests when the user scrolls rapidly elsewhere.

#### 60.3.8 Request Coalescing / Cancellation

Rapid scrolling may produce obsolete window requests.

The implementation should:

- cancel obsolete requests where provider/API supports cancellation;
- coalesce nearby requests;
- prevent stale responses from replacing a newer viewport window.

#### 60.3.9 Window Cache

An optional small window cache may retain recently visited windows.

The cache must be bounded.

It must never silently grow toward full dataset materialization.

#### 60.3.10 Viewport Virtualization Rule

The required principle is:

> Materialize what the user can see, plus a bounded prefetch buffer — not the entire dataset.


### 62.4 Sort / Filter / Search

For large datasets, sort/filter/search should be pushed through the data-provider/query boundary.

The UI must not default to enumerating and sorting/filtering all 100K+ rows locally.

The framework may support local-mode datasets for small data, but the large-data path must remain available.

### 62.5 Selection Model

Selection must scale independently from the number of UI row objects.

Examples:

```text
Select All
100,000 rows selected
```

must not require creating 100,000 visual selection objects.

The selection model should support:

- row identity sets;
- ranges;
- filtered-selection scopes;
- explicit `Selected / Visible / Matching` semantics.

### 62.6 Edit Buffer

Candidate edits must be sparse.

The framework must track changed cells/rows only.

It must not clone the entire large dataset for an edit session.

Conceptual model:

```text
Source Query Result
   ↓
Sparse Candidate Edit Buffer
   ├─ RowId
   ├─ VariableCode
   ├─ OldValue
   └─ CandidateValue
```

### 62.7 Large Multi-Cell Paste

Large paste operations must be chunked/batched.

Example:

```text
10,000 clipboard cells
   ↓
Parse
   ↓
Chunk
   ↓
Type conversion / validation
   ↓
Candidate buffer
   ↓
Progress / errors
   ↓
Batch commit
```

Requirements:

- do not block the UI thread for the full operation;
- show progress for large operations;
- allow cancellation where safe;
- preserve protected `FORMULA` / `SYSTEM` columns;
- report valid/invalid/ignored counts.

### 62.8 Validation

Validation for large datasets must support asynchronous/chunked execution.

Do not require validating every row on the UI thread.

Validation results should be returned as structured diagnostics keyed by row identity and `VariableCode`.

### 62.9 Import

Excel import of large datasets must use streaming/chunked reading where supported by the chosen library.

Importing 100K rows must not require rendering those rows during import.

Pipeline remains:

```text
Read Stream
→ Map
→ Validate in chunks
→ Preview summary / sampled errors
→ Commit batch
```

### 62.10 Export

Export must operate from the provider/data source, not from currently materialized visual rows.

Required:

```text
Export Current View
Export All Matching
Export Template
```

For a 100K-row export, the framework must not require scrolling/materializing rows first.

### 62.11 Calculated Columns

Calculated/formula results remain read-only.

For large datasets, calculated results should be returned by the consuming application's calculation/query boundary.

The UI must not calculate formula columns row-by-row as authoritative business logic.

### 62.12 Asynchronous UI

Long-running operations must not freeze the shell.

Use asynchronous patterns for:

- large query;
- sort/filter/search;
- paste validation;
- import;
- export;
- batch commit;
- refresh.

UI should present:

```text
Loading
Progress
Cancel where supported
Completed
Partial
Failed
```

### 62.13 Memory

The implementation must demonstrate bounded memory growth under normal large-grid use.

The exact memory ceiling is platform/runtime-dependent and must be benchmarked.

The acceptance requirement is behavioral:

- no runaway row-object accumulation;
- no unbounded selection-object growth;
- no repeated full-dataset clones;
- no crash due to routine 100K-row usage.

### 62.14 Performance Instrumentation

The Demo/benchmark harness should be able to capture:

- first meaningful paint;
- query duration;
- scroll responsiveness;
- sort/filter/search duration;
- paste duration;
- import/export throughput;
- allocated memory / working set observations;
- exceptions/crashes.

### 62.15 Platform Verification

Important milestones must be tested on:

```text
macOS ARM64 — real GUI smoke
Windows x64 — real native GUI smoke
```

CI publish alone is not sufficient for final large-grid performance acceptance.

### 62.16 Initial Performance Targets

Specification v0.6 establishes the following test profile:

```text
Dataset:
100,000 rows minimum

Dynamic columns:
30 or more

Operations:
Open
First paint
Scroll top → middle → bottom
Sort
Filter
Search
Edit
Multi-cell copy
Multi-cell paste
Clear filter
Refresh
Export
```

Success criteria:

```text
No crash
No unbounded memory growth
No requirement to materialize all rows
No unacceptable long UI-thread freeze
Correct values/selection after virtualization
```

Exact millisecond targets should be established from benchmark results during implementation rather
than guessed in the specification.


## 64. Company-Scoped Permission / Capability Presentation

Dynamic UI uses a shared permission/capability presentation layer.

The Dynamic UI Framework does **not** own user-role assignment or authoritative
authorization.

Canonical flow:

```text
User Login
   ↓
Company Context
   ↓
Application Authorization API
   ↓
Effective Permission / Capability Set
   ↓
Dynamic UI Resolver
   ↓
HIDE / DISABLE / READ_ONLY / ENABLED
```

### 62.1 Responsibility Boundary

Setup answers:

```text
"What permission/capability does this UI element require?"
```

The consuming application answers:

```text
"Does this user have that permission/capability for the current Company?"
```

Dynamic UI answers:

```text
"How should the element be presented?"
```

The framework must never treat UI state as the source of truth for authorization.

### 62.2 Company Scope

Permissions/capabilities may differ by Company Context.

Example:

```text
User A
├─ Company 01
│  ├─ PAYROLL.VIEW
│  └─ PAYROLL.EDIT
└─ Company 02
   └─ PAYROLL.VIEW
```

When the active Company changes, the framework must request/receive a refreshed
effective capability context and re-resolve UI presentation.

### 62.3 Shared Contracts

Conceptual contracts:

```text
ICompanyContextProvider
  CurrentCompany
  AvailableCompanies
  SwitchCompany(...)

IAuthorizationPresentationProvider
  GetEffectiveCapabilities(companyId, userContext)
  Refresh(...)

IUserPermissionContext
  CompanyId
  UserId
  PermissionCodes
  CapabilityCodes
  Version / Revision
```

The exact API may evolve, but Company-scoped resolution is mandatory.

### 62.4 UI Metadata

Any configurable UI element may declare:

```text
PermissionCode
CapabilityCode
UnauthorizedBehavior
```

Supported unauthorized behaviors:

```text
HIDE
DISABLE
READ_ONLY
```

Default behavior should be explicitly defined by each template/workspace.

### 62.5 Applicable UI Elements

Permission/capability metadata may be attached to:

```text
Application Menu item
Ribbon Tab
Ribbon Group
Ribbon Command
Tree Node
Workspace
Action Bar
Action
Grid
Grid Column
Grid Edit capability
Dashboard Node
Dashboard Widget
Report Definition
Report Action
History/Document action
Signing action
Template-specific command
```

### 62.6 Runtime Re-Resolution on Company Switch

Canonical flow:

```text
Switch Company
   ↓
Update Company Context
   ↓
Refresh Effective Capabilities
   ↓
Invalidate Presentation Capability Cache
   ↓
Re-resolve:
  Application Menu
  Ribbon
  Tree
  Workspace
  Grid / Columns
  Action Bars
  Dashboard
  Reports
  History/Documents
  Signing UI
   ↓
Reload Company-scoped data
```

No application restart should be required.

### 62.7 Grid / Column Rules

Example:

```text
Column: SalaryAdjustment
PermissionCode = PAYROLL.ADJUSTMENT.EDIT
UnauthorizedBehavior = READ_ONLY
```

Another example:

```text
Column: ConfidentialCost
PermissionCode = COST.VIEW
UnauthorizedBehavior = HIDE
```

Calculated/System columns remain read-only independently of user edit permission.

### 62.8 Action Rules

Example:

```text
Action: Approve
PermissionCode = APPROVAL.APPROVE
UnauthorizedBehavior = HIDE
```

Example:

```text
Action: Export
PermissionCode = REPORT.EXPORT
UnauthorizedBehavior = DISABLE
```

The backend/application API must perform the same authoritative check again when the
command executes.

### 62.9 Permission vs Capability

`PermissionCode` typically represents user authorization.

`CapabilityCode` may represent runtime/system/application availability such as:

```text
REPORT.EXPORT_PDF_AVAILABLE
SIGNING.PROVIDER_AVAILABLE
LICENSE.FEATURE_ENABLED
WORKFLOW.ACTION_AVAILABLE
```

An element may require both permission and capability.

Example:

```text
PermissionCode = SIGNING.SIGN
CapabilityCode = SIGNING.PROVIDER_AVAILABLE
```

### 62.10 Caching

A consuming application may cache permission/capability sets.

Any cache must be scoped at least by:

```text
User
Company
Capability/Permission Revision
```

Company switch must never reuse a permission set from another Company.

### 62.11 Fail-Closed Presentation

If the authorization/capability provider is unavailable or unresolved:

- privileged edit/execute actions should default to unavailable;
- the UI may still show safe read-only content if allowed by application policy;
- the shell must remain stable;
- no optimistic "allow" default is permitted.

### 62.12 Security Rules

1. Hiding a button is not authorization.
2. Disabling a grid cell is not authorization.
3. Backend/Application/API must enforce permissions on every authoritative command.
4. Company Context is part of the authorization context.
5. Permission refresh is mandatory after Company switch.
6. UI metadata may reference permission/capability codes but never assign them to users.
7. Template modules must not implement their own independent user-role systems.
8. Unknown permissions/capabilities must fail closed.


## 65. Dynamic Action Bars

Every template may expose a metadata-driven **Top Action Bar** and **Bottom Action Bar**.

Standard layout:

```text
┌───────────────────────────────────────────────────────────────┐
│ TOP ACTION BAR                                                │
│ [Action] [Action] [Import] [Export] [Validate] [...]         │
├───────────────────────────────────────────────────────────────┤
│ TEMPLATE CONTENT                                              │
│                                                               │
├───────────────────────────────────────────────────────────────┤
│ BOTTOM ACTION BAR                                             │
│ 125 rows | 18 selected | 2 errors     [Action] [Commit]      │
└───────────────────────────────────────────────────────────────┘
```

Action Bars are generic shell/template capabilities.

### 62.1 Action Bar Definition

Suggested metadata:

```text
ActionBarId
TemplateCode
WorkspaceDefinitionId
Position
DisplayOrder
IsVisible
Actions[]
```

Position:

```text
TOP
BOTTOM
```

### 62.2 Action Definition

Suggested metadata:

```text
ActionId
ActionCode
DisplayNameKey
IconKey
CommandType
DisplayOrder
PermissionCode
RequiresSelection
MinSelection
MaxSelection
EnableWhen
IsVisible
ConfirmationMode
TargetWorkspaceId
RegisteredCommandCode
BatchActionCode
```

### 62.3 Supported Generic Command Types

Recommended:

```text
NAVIGATE
REFRESH
SEARCH
FILTER
ADD
EDIT
DELETE
IMPORT
EXPORT
PREVIEW
VALIDATE
COMMIT
APPLICATION_COMMAND
BATCH_ACTION
CUSTOM_REGISTERED
```

Metadata must never contain arbitrary executable code.

### 62.4 Top Action Bar

The Top Action Bar is intended for high-frequency workspace actions such as:

- Add/Edit;
- Import/Export;
- Search/Filter;
- Validate;
- Refresh;
- Preview;
- Generate;
- contextual application commands.

### 62.5 Bottom Action Bar

The Bottom Action Bar may combine action commands with status/selection presentation.

Suggested standard status metadata:

```text
TotalRows
VisibleRows
SelectedRows
ErrorCount
WarningCount
PendingChangeCount
ReadOnlyState
```

Example:

```text
125 rows | 18 selected | 2 errors | 3 pending changes
```

### 62.6 Selection-Aware Actions

Action enablement may depend on:

- zero selection;
- single selection;
- multi-selection;
- exact selected item type;
- registered capability state.

The UI must always make selection scope explicit.

### 62.7 Action Bar Safety

1. Action Bars render and dispatch only.
2. No business rule is executed inside Action Bar metadata.
3. Permissions/capabilities are supplied by the consuming application.
4. Unknown registered commands fail safely.
5. Hidden/disabled UI does not replace server/application authorization.
6. Template modules may define default actions, but applications may override/extend through metadata and registered contributors.

## 66. DynamicSigningTemplate

`DynamicSigningTemplate` is a reusable **UI-only** presentation template for signing and approval workflows.

Typical uses include:

- tax declarations;
- invoices;
- contracts;
- accounting documents;
- company-profile change requests;
- internal approval documents;
- reports requiring confirmation/signing.

### 62.1 Standard Layout

```text
┌──────────────────────────────┬─────────────────────────────────────────────┐
│ WORK QUEUE / DOCUMENT LIST   │ DOCUMENT DETAIL / PREVIEW                   │
│                              │                                             │
│ Search / Filter / Status     │ PDF / XML / DOCX / other supported viewer  │
│                              │                                             │
│ Date | Name | Status | Actor │ Metadata / Signer / Approval Information    │
│ ...                          │                                             │
├──────────────────────────────┴─────────────────────────────────────────────┤
│ TIMELINE / COMMENTS / HISTORY                                              │
├────────────────────────────────────────────────────────────────────────────┤
│ BOTTOM ACTION BAR                                                          │
│ [Submit] [Approve] [Reject] [Sign] [History] [Other Registered Actions]   │
└────────────────────────────────────────────────────────────────────────────┘
```

### 62.2 Signing Presentation Metadata

Suggested metadata:

```text
SigningItemId
DocumentId
DocumentVersion
DocumentType
Title
Status
CreatedAt
CreatedBy
CurrentActor
CurrentStep
CanPreview
CommentRequired
ReasonRequired
TimelineId
PermissionCode
```

### 62.3 Signer / Approver Presentation

The template may display application-provided information such as:

```text
ActorId
DisplayName
Role
Step
Status
SignedAt
ApprovedAt
CertificateSummary
SignatureReference
TimestampReference
```

The framework treats these values as presentation data.

### 62.4 Timeline / History

Reusable event presentation:

```text
Created
Submitted
Reviewed
Approved
Rejected
Signed
Completed
```

Actual event names/states are application-provided.

Suggested event metadata:

```text
EventId
Timestamp
Actor
Action
Status
Comment
Reason
SignatureReference
CorrelationId
```

History is read-only.

### 62.5 Signing / Approval Actions

Actions are supplied through Dynamic Action Bars and registered command/provider boundaries.

Example semantic actions:

```text
SUBMIT
APPROVE
REJECT
SIGN
CANCEL
VIEW_HISTORY
CUSTOM_REGISTERED
```

The framework does not assume every application supports every action.

### 62.6 Execution Boundary

Canonical flow:

```text
DynamicSigningTemplate
        ↓
Registered Command / Provider
        ↓
Application API
        ↓
Signing / Approval Service
        ↓
Token / HSM / Remote Signing / Workflow Engine
```

The UI never executes the cryptographic/business operation itself.

### 62.7 Explicitly Forbidden in Dynamic UI

The Signing template must not:

- store private keys;
- read private keys;
- access PKCS#11 directly;
- manage HSM sessions;
- construct authoritative digital signatures;
- verify authorization locally as the source of truth;
- bypass entitlement/license/security guards;
- directly mutate signed document state;
- directly update authoritative approval state.

### 62.8 Provider Contracts

Conceptual provider boundaries:

```text
ISigningPresentationProvider
ISigningCommandProvider
IApprovalWorkflowPresentationProvider
IApprovalWorkflowCommandProvider
```

The exact API may evolve, but command and presentation boundaries remain separate.

### 62.9 Document Preview

Document preview should reuse `TS24.DynamicUI.Documents` and `DynamicHistoryDocumentTemplate`
viewer primitives where appropriate.

The Signing template should not implement duplicate PDF/XML/DOCX viewers.

### 62.10 Signing Template States

Standard presentation states:

```text
EMPTY
LOADING
READY
READ_ONLY
ERROR
PERMISSION_DENIED
ACTION_IN_PROGRESS
PARTIAL
COMPLETED
```

### 62.11 Signing Template Security Principle

A visible or enabled button is never authoritative security.

Every Submit/Approve/Reject/Sign action must be revalidated by the consuming application/API.


## 67. Application Menu / App Shell Menu

The Application Menu is separate from business Ribbon tabs and separate from file/document
handling.

It contains **global application settings and shell actions**.

Recommended standard structure:

```text
Application
├─ Company / Context
├─ Language
│  ├─ Tiếng Việt
│  └─ English
├─ Appearance
│  ├─ System
│  ├─ Light
│  └─ Dark
├─ General Settings
├─ User / Account
├─ License / Entitlement
├─ About
└─ Exit
```

The exact availability of Company/Context, Account, and License depends on the consuming
application and registered services.

### 62.1 Standard vs App-Specific Items

The shell owns standard capabilities such as:

```text
LANGUAGE
APPEARANCE
ABOUT
EXIT
```

Shared optional capabilities may include:

```text
COMPANY_CONTEXT
ACCOUNT
LICENSE
GENERAL_SETTINGS
```

Applications may contribute additional settings sections through a registered extension
contract.

Conceptual contract:

```text
IApplicationMenuContributor
  ContributorCode
  DisplayName
  IconKey
  DisplayOrder
  PermissionCode
  CreateItems(...)
```

App-specific contributors must not modify shell internals directly.

### 62.2 Application Menu Metadata

Suggested item metadata:

```text
MenuItemId
MenuItemCode
DisplayNameKey
IconKey
DisplayOrder
ItemType
TargetSettingPage
PermissionCode
IsVisible
RequiresService
```

Recommended item types:

```text
SETTING_PAGE
ACTION
SEPARATOR
CONTRIBUTOR_GROUP
```

### 62.3 Application Menu Rendering

Conceptual flow:

```text
Shell Standard Items
       +
Registered Shared Services
       +
IApplicationMenuContributor[]
       ↓
Application Menu Resolver
       ↓
Application Menu / Backstage
```

The shell remains stable even if an optional contributor/service is absent.

### 62.4 Language

The Application Menu is the standard location for user-facing runtime language selection.

Initial standard languages may include:

```text
vi-VN
en-US
```

Actual supported languages are supplied by the consuming application/framework resources.

Changing language must update the shell and dynamic templates according to the existing
localization architecture.

### 62.5 Appearance

The Application Menu is the standard location for runtime appearance selection:

```text
System
Light
Dark
```

Theme selection must update the shell and loaded templates consistently.

### 62.6 Company / Context

If the consuming application supports company/tenant/workspace context, the Application
Menu may expose the current context and a registered context-switch action.

The Dynamic UI Framework does not implement company security or data isolation.

It consumes the application-provided context service.

### 62.7 User / Account

If an account/profile service is registered, the menu may expose:

- current user display;
- account/profile;
- sign-out/change-account actions where supported.

Authentication semantics remain outside the Dynamic UI Framework.

### 62.8 License / Entitlement

If the consuming application provides license/entitlement information, the Application
Menu may present:

- license state;
- expiration information;
- edition/entitlement;
- manage/renew action supplied by the application.

The UI never treats menu state as the authoritative license guard.

### 62.9 About

Standard About content may include:

```text
Application Name
Application Version
Framework Version
Build/Commit
Copyright
Runtime / Platform
License/Evaluation marker where applicable
```

Apps may contribute additional About metadata.

### 62.10 Exit

Exit is a standard shell action.

It should invoke the registered clean-shutdown application command.

The framework must not terminate the process in a way that bypasses application cleanup.

### 62.11 App Menu Safety Rules

1. Application Menu is for global app/shell settings and actions.
2. It is not a replacement for business Ribbon tabs.
3. It is not the file/document history module.
4. App-specific settings are added through registered contributors, not shell-core edits.
5. Permission and license enforcement remain authoritative below the UI.
6. Unknown/failed contributors must not crash the shell.
7. Theme/language changes must remain runtime-safe across loaded templates.


## 68. Dynamic Ribbon Definition

The application Ribbon is metadata-driven and configurable through Setup.

Ribbon tabs, groups, and commands must not require hard-coded application-specific
definitions in XAML.

Conceptual configuration:

```text
Setup
├─ Navigation Tree Definition
├─ Ribbon Definition
├─ Workspace Definition
├─ Dashboard Definition
└─ Permission / Capability Mapping
```

### 62.1 Ribbon Tab Definition

Suggested metadata:

```text
RibbonTabId
TabCode
DisplayNameKey
DisplayOrder
IconKey
IsVisible
PermissionCode
ContextRule
Groups[]
```

Display names must support localization through resource keys or localized metadata.

### 62.2 Ribbon Group Definition

Suggested metadata:

```text
RibbonGroupId
GroupCode
DisplayNameKey
DisplayOrder
IconKey
IsVisible
PermissionCode
Commands[]
```

### 62.3 Ribbon Command Definition

Suggested metadata:

```text
CommandId
CommandCode
DisplayNameKey
IconKey
CommandType
TargetWorkspaceId
TargetTemplateCode
PermissionCode
RequiresSelection
EnableWhen
ConfirmationMode
DisplayOrder
```

The exact schema may evolve, but commands must resolve through registered navigation,
capability, or application-command boundaries.

### 62.4 Dynamic Ribbon Rendering

Conceptual flow:

```text
Ribbon Definition
       +
Current Workspace / Template Context
       +
Permissions / Capabilities
       ↓
Dynamic Ribbon Resolver
       ↓
Tabs → Groups → Commands
       ↓
Avalonia / Actipro Ribbon Host
```

The host renders the resolved metadata.

It must not contain application-specific branches such as:

```text
if workspace == "KPI" then ...
if workspace == "Approval" then ...
```

### 62.5 Contextual Ribbon

Ribbon groups/commands may be contextual.

Examples:

```text
KPI workspace selected
   ↓
KPI contextual group
[Validate] [Commit] [Import]

Approval workspace selected
   ↓
Approval contextual group
[Submit] [Approve] [Reject]
```

Context is resolved from metadata plus state/capabilities supplied by the consuming
application.

The Dynamic UI layer must not independently infer authoritative business eligibility.

### 62.6 Ribbon Command Types

Recommended generic command types:

```text
NAVIGATE
REFRESH
SEARCH
FILTER
IMPORT
EXPORT
PREVIEW
APPLICATION_COMMAND
BATCH_ACTION
CUSTOM_REGISTERED
```

`APPLICATION_COMMAND` and `CUSTOM_REGISTERED` must resolve through registered providers.
Metadata must never contain arbitrary executable code.

### 62.7 Permission and Enablement

Visibility and enablement may depend on:

- permission/capability supplied by the application;
- selected workspace/template;
- current selection count;
- read-only state;
- registered command availability.

The application remains authoritative for authorization and business eligibility.

### 62.8 Ribbon Setup UX

`DynamicSetupTemplate` should expose a Ribbon Designer capable of:

- create/delete/retire tabs;
- reorder tabs;
- create/reorder groups;
- add/remove/reorder commands;
- choose IconKey/SVG;
- choose target workspace/template;
- map permission/capability;
- define contextual visibility;
- preview Ribbon;
- validate definition before publish;
- version/publish Ribbon definitions.

Published Ribbon definitions should be versioned and deterministic.

### 62.9 Ribbon Localization and Theme

Ribbon labels use localization resources/metadata.

Icons use semantic `IconKey` so SVG/icon assets can be replaced without changing
command semantics.

Ribbon rendering must remain compatible with System/Light/Dark themes.

### 62.10 Ribbon Safety Rules

1. Ribbon metadata is presentation/navigation metadata.
2. No arbitrary C#, script, SQL, or executable expression is stored in Ribbon definitions.
3. Business logic remains in the consuming application's Application/Domain boundary.
4. Permission checks are not bypassed by hiding/showing UI controls.
5. Unknown command codes fail safely and visibly.
6. A malformed contextual definition must not crash the shell.


## 69. Documentation Architecture

Documentation is part of the framework deliverable.

Recommended structure:

```text
docs/
├─ architecture/
│  ├─ FRAMEWORK-OVERVIEW.md
│  ├─ TEMPLATE-CONTRACT.md
│  ├─ MODULE-DEPENDENCIES.md
│  └─ VERSION-COMPATIBILITY.md
│
├─ templates/
│  ├─ setup/
│  ├─ data-entry/
│  ├─ report/
│  ├─ history-document/
│  └─ dashboard/
│
├─ extensions/
│  ├─ excel/
│  ├─ reporting/
│  ├─ documents/
│  └─ batch/
│
└─ adoption/
   ├─ NEW-APP-QUICKSTART.md
   ├─ APP-ADOPTION-CHECKLIST.md
   └─ MIGRATION-GUIDE.md
```

Each template module must provide at least:

```text
OVERVIEW.md
METADATA.md
API.md
EXAMPLES.md
TESTING.md
CHANGE-GUIDE.md
```

The objective is that another application team can adopt a template without needing
knowledge of PayCalc24 or the original implementation history.

## 70. App Adoption Documentation

Each consuming app should maintain:

```text
docs/dynamic-ui/
├─ ADOPTION.md
├─ WORKSPACE-MAP.md
├─ VARIABLE-CATALOG.md
├─ PERMISSION-MAP.md
├─ REPORT-MAP.md
└─ DASHBOARD-MAP.md
```

These documents explain only how that application maps its domain/contracts into the
generic framework.

The framework documentation explains generic usage; application documentation explains
application-specific mapping.



## 71. Shared UX and Design-System Standards Formalized in v0.8

This section makes the shared UX behaviors verified through Tasks 8–9 part of the
normative DynamicUI24 contract.

### 71.1 Resizable Split Navigation Layout

A reusable split-navigation layout consists of:

```text
LEFT NAVIGATION PANE
        │
DRAGGABLE SPLITTER
        │
RIGHT MAIN WORKSPACE
```

It is a shared primitive and is not owned by `DynamicSetupTemplate`.

Required behavior:

- configurable minimum left-pane width;
- configurable default width;
- bounded maximum where appropriate;
- runtime horizontal resize with clear splitter affordance;
- right workspace reflows without destructive recreation;
- no layout jump during pointer hover or splitter drag;
- selection, candidate edits, navigation context, current workspace, theme, and
  language remain intact;
- controls hosted inside either pane remain responsible for their own scrolling.

Persisted user width preference is a separate presentation-preference concern and must
not be conflated with the published layout definition.

### 71.2 Tree Row Interaction States

Reusable Tree/navigation rows expose semantic interaction states:

```text
NORMAL
HOVER
SELECTED
SELECTED_HOVER
DISABLED
KEYBOARD_FOCUS
```

Rules:

1. Hover/selection apply to the whole visible row, not only the label or icon.
2. Hover must not alter row dimensions or cause layout movement.
3. Selected state remains distinguishable after the pointer leaves.
4. Disabled rows must not appear actionable.
5. Keyboard focus must have an explicit focus visual independent from hover.
6. Expand/collapse interaction remains distinct from row navigation.
7. Styling uses semantic tokens only and supports System/Light/Dark.
8. Meaning must never depend on color alone.

These rules apply to the global navigation Tree, Setup internal Tree, and reusable Tree
surfaces in future templates.

### 71.3 Tree Overflow — See More / Show Less

A Tree node with many children may limit initial child presentation.

Generic metadata/runtime policy may expose:

```text
InitialVisibleChildCount
ShowMorePageSize
SupportsShowMore
```

Required behavior:

- show a bounded initial child set;
- `See more / Xem thêm` reveals additional children incrementally;
- `Show less / Thu gọn` may restore the compact state;
- no fixed application-wide maximum number of children;
- hierarchy, permission/capability filtering, Company Context, selection, expanded
  ancestors, localization, theme, and scroll stability are preserved;
- large child collections should not require rendering every child by default.

`See more` is a shared Tree capability, not Setup-specific business logic.

### 71.4 Dynamic Action Control Variants

Reusable Action Bars and other metadata-driven action surfaces support:

```text
BUTTON
DROPDOWN_BUTTON
SPLIT_BUTTON
ICON_BUTTON
TOGGLE_BUTTON
```

Semantics:

- `BUTTON`: executes one registered action;
- `DROPDOWN_BUTTON`: opens a metadata-driven action menu;
- `SPLIT_BUTTON`: primary segment executes a configured default action while the
  chevron segment opens the menu;
- `ICON_BUTTON`: compact icon-focused action with accessible label/tooltip semantics;
- `TOGGLE_BUTTON`: presents a generic toggled state where appropriate.

Reusable controls dispatch through registered commands/navigation providers and never
embed application business logic.

### 71.5 Action Menu Metadata

Conceptual menu-item metadata:

```text
MenuItemCode
DisplayNameKey
IconKey?
RegisteredCommandCode?
DisplayOrder
PermissionRequirement?
CapabilityRequirement?
ShortcutDisplay?
GroupCode?
IsSeparator?
Children?
```

Rules:

- deterministic ordering;
- localization through resource keys;
- semantic `IconKey`;
- permission/capability resolution;
- separators/groups permitted;
- unknown icon/command fails safely;
- submenu hierarchy is limited to a practical maximum of two menu levels unless a
  later specification explicitly extends it.

### 71.6 Action Geometry and Typography

Shared actions support semantic size presets:

```text
XS
SMALL
MEDIUM
LARGE
XL
```

Optional bounded overrides may include:

```text
Width
MinWidth
MaxWidth
Height
TypographyToken
IconSize
IconPosition
Padding
Gap
```

Published metadata defines default presentation only.

Resolved geometry follows:

```text
Global UI Scale
      +
Global Font Preference
      +
Component Size / Typography Tokens
      +
Bounded Metadata Override
      ↓
Resolved Presentation Geometry
```

A metadata override must not defeat global accessibility/user scaling.

Changing UI scale, font size, component size, language, or theme must preserve semantic
state such as selection, candidate edits, navigation, and current workspace.

### 71.7 Icon Source Abstraction

`IconKey` remains the public metadata contract.

Physical icon representation is hidden behind the icon registry.

At minimum the registry supports:

```text
SVG_RESOURCE
FONT_GLYPH
```

Conceptual definition:

```text
IconDefinition
├─ IconKey
├─ SourceKind
├─ ResourceKey?
├─ Glyph?
├─ FontFamilyToken?
└─ DefaultSize?
```

Rules:

- reusable metadata must not contain direct SVG filesystem paths;
- metadata must not embed raw font files;
- consumer applications may register or override semantic IconKeys through supported
  extension points;
- unknown IconKeys use a deterministic fallback;
- icon rendering must remain System/Light/Dark compatible.

### 71.8 Consumer Customization Contract

Consumer applications should normally customize DynamicUI24 through:

```text
Metadata
Semantic Design Tokens
Registries
Providers
Extension Points
```

rather than modifying shared framework controls.

Shared controls remain application-neutral. Application branding and domain behavior
belong above the reusable framework boundary.

### 71.9 Setup Metadata Contracts Retained from Task 9

v0.8 formally retains the metadata model proven by Task 9:

- Master Catalog definitions and hierarchy;
- Workspace definitions whose `TemplateCode` choices are discovered through
  `TemplateRegistry`;
- Column definitions and published geometry metadata;
- Variable definitions and stable `VariableCode`;
- Formula-definition metadata and referenced VariableCodes;
- `INPUT`, `FORMULA`, and `SYSTEM` column modes;
- version/status/effective-state concepts;
- candidate/validation/publish lifecycle boundaries;
- published presentation metadata remains separate from per-user preferences.

Formula metadata is declarative only. DynamicUI24 does not execute arbitrary C#, SQL,
JavaScript, shell commands, assemblies, or other executable scripts stored in UI
metadata.

### 71.10 VariableCode

`VariableCode` is a stable semantic identifier.

Required properties:

- independent from localized display names;
- deterministic normalization;
- non-empty;
- unique within its defined scope;
- suitable for declarative references;
- protected after publication except through explicit versioning;
- usable by future Calculation Engine, API, report, AI, and integration boundaries
  without coupling to UI labels.

Application-specific VariableCodes do not belong in the DynamicUI24 framework
specification.


## 72. Dynamic Notification and Guidance System

### 72.1 Purpose

DynamicUI24 provides a shared Notification & Guidance capability whose preferred UX
flow is:

```text
DETECTED STATE
      ↓
EXPLANATION
      ↓
ACTIONABLE RESOLUTION
      ↓
CORRECT WORKSPACE / CONTEXT
```

The framework should help applications guide users toward resolution rather than limit
them to passive error messages.

Notification detection and source state belong to application/runtime providers.
DynamicUI24 owns generic contracts, normalization, resolution, presentation, and
navigation/command integration.

The capability belongs to shared Shell/Foundation and is not owned by Setup,
DataEntry, Report, Dashboard, Signing, or any other individual template.

### 72.2 Presentation Kinds

Supported semantic presentation kinds:

```text
TOAST
BANNER
ALERT_CARD
BLOCKING_NOTICE
NOTIFICATION_CENTER_ITEM
```

Definitions:

- `TOAST`: short transient information with minimal interruption;
- `BANNER`: persistent contextual notice in Shell or workspace;
- `ALERT_CARD`: prominent non-modal card supporting title, details, progress, and
  actions;
- `BLOCKING_NOTICE`: reserved for conditions where continuation is genuinely
  impossible or unsafe;
- `NOTIFICATION_CENTER_ITEM`: persistent/reviewable item surfaced through the Shell
  Notification Center.

Use the least intrusive presentation appropriate to the condition.

### 72.3 Notification Definition

Conceptual contract:

```text
NotificationDefinition
├─ NotificationId
├─ NotificationCode
├─ Severity
├─ PresentationKind
├─ TitleKey
├─ MessageKey
├─ IconKey?
├─ Timestamp?
├─ AutoShow
├─ Dismissible
├─ Progress?
├─ PrimaryAction?
├─ SecondaryActions[]
├─ PermissionRequirement?
├─ CapabilityRequirement?
├─ CompanyScope?
├─ WorkspaceScope?
├─ DeduplicationKey?
├─ Priority?
├─ Expiration?
└─ SourceCode?
```

Metadata remains application-neutral.

### 72.4 Severity

Semantic severity levels:

```text
INFO
SUCCESS
WARNING
ERROR
CRITICAL
```

Severity and presentation kind are separate concepts.

For example, a Warning may render as a Banner or Alert Card. Critical severity does
not automatically imply a modal/blocking surface.

Severity uses semantic design tokens and must never communicate meaning through color
alone.

### 72.5 Progress

Notifications may optionally carry progress:

```text
CurrentValue
MaximumValue
DisplayTextKey?
IsIndeterminate?
```

The framework renders supplied progress but does not calculate application metrics.

Malformed ranges must fail safely.

### 72.6 GuidanceAction

Conceptual:

```text
GuidanceAction
├─ ActionCode
├─ DisplayNameKey
├─ IconKey?
├─ ActionType
├─ WorkspaceId?
├─ RegisteredCommandCode?
├─ NavigationTarget?
├─ FocusTarget?
├─ PermissionRequirement?
└─ CapabilityRequirement?
```

Initial generic action types:

```text
NAVIGATE
COMMAND
OPEN_MENU
DISMISS
```

Guidance reuses the existing workspace navigation and registered command
infrastructure. It does not create a parallel business-command engine.

### 72.7 NavigationTarget

Navigation uses the shared navigation boundary:

```text
Notification
   ↓
GuidanceAction
   ↓
WorkspaceNavigationService
   ↓
WorkspaceDefinition
   ↓
TemplateRegistry
   ↓
Workspace Host
```

Tree selection, Ribbon context, Action Bars, and workspace presentation should
synchronize through the existing navigation context.

Notification UI must not instantiate template implementations directly.

### 72.8 Semantic FocusTarget

After successful navigation, an optional semantic `FocusTarget` may direct attention
to a specific registered field, section, or control.

Examples:

```text
FieldCode
SectionCode
ControlKey
```

Metadata must never hold runtime UI object references.

If the FocusTarget is unknown, navigation may still succeed and the focus request
fails safely.

### 72.9 Notification Center

The Shell supports a reusable Notification Center entry point, conceptually represented
by a notification/bell indicator with attention count.

A Notification Center item may show:

- severity;
- timestamp;
- title/message;
- unread/attention state;
- progress;
- primary/secondary action;
- dismiss action where permitted.

Ordering is deterministic and should consider priority and recency.

The specification does not mandate a vendor-specific visual appearance.

### 72.10 Auto-Show, Deduplication, Cooldown, and Throttling

Automatic presentation must not become notification spam.

Auto-show resolution considers:

- priority;
- presentation kind;
- deduplication;
- dismissal state;
- cooldown/throttling policy;
- current Company/workspace context;
- expiration.

A provider repeatedly emitting the same unresolved condition should normally update
one logical notification rather than create continuous duplicate popups.

### 72.11 Deduplication

`DeduplicationKey` represents one logical unresolved condition across repeated provider
emissions.

The framework may update an existing active notification when the same deduplication
key is emitted again.

Duplicate IDs or malformed deduplication data must be handled deterministically.

### 72.12 Dismissal vs Resolution

Dismissal means presentation was acknowledged/hidden according to policy.

Dismissal does not necessarily resolve the underlying source condition.

A provider may re-emit a condition after meaningful state change or applicable
cooldown.

### 72.13 Lifecycle

Conceptual lifecycle:

```text
NEW
ACTIVE
ACKNOWLEDGED
DISMISSED
RESOLVED
EXPIRED
```

Exact implementation names may vary while preserving the distinction between user
acknowledgement and actual condition resolution.

### 72.14 Provider Boundary

Conceptually:

```text
INotificationProvider
```

Application/runtime providers own detection and source state.

DynamicUI24 owns generic:

- normalization;
- permission/capability filtering;
- Company/workspace scoping;
- deduplication;
- lifecycle/presentation state;
- Shell/workspace rendering;
- registered navigation/command dispatch.

The framework must not contain application-specific monitoring or business rules.

### 72.15 Permission, Capability, and Information Disclosure

Notifications and GuidanceActions may use existing:

```text
PermissionCode
CapabilityCode
UnauthorizedBehavior
```

Privileged actions fail closed.

A notification may remain visible while an action is disabled/unavailable where
appropriate, but privileged information must not be leaked in title/message content to
users who are not authorized to see it.

### 72.16 Company Context

Notifications may be:

```text
GLOBAL
COMPANY_SCOPED
```

On Company switch:

- Company-scoped notifications re-resolve;
- stale notifications/actions from the previous Company become non-actionable or are
  removed according to policy;
- valid global notifications remain;
- unresolved permission/context states fail closed.

A notification originating from Company A must never remain actionable using Company B
authorization/context accidentally.

### 72.17 Workspace Context

Notifications may optionally be application- or workspace-scoped using semantic keys.

A workspace Banner may disappear/re-resolve after navigation while a persistent
Notification Center item may remain available according to lifecycle policy.

Notification definitions must not directly depend on a specific template
implementation.

### 72.18 Background Operations

The contract can represent future background-operation states such as:

```text
Started
Progress
Completed
Failed
```

Job scheduling/execution does not belong to the Notification system.

### 72.19 Relationship to Existing Error and Action Presentation

These concepts remain distinct:

```text
Inline Field Validation
Workspace Error State
Toast
Banner
Alert Card
Notification Center Item
Blocking Notice
Dynamic Action Bar
```

Dynamic Action Bar represents user-available actions in the current context.

Notification & Guidance represents provider/system-driven attention and resolution
flows.

They may reuse the same command/navigation infrastructure without becoming one
component.

### 72.20 Localization

Notification title/message/action labels use localization keys.

Initial framework cultures remain:

```text
vi-VN
en-US
```

Runtime language changes update active presentation without duplicating notifications
or losing lifecycle/deduplication state.

Technical identifiers remain untranslated.

### 72.21 Theme and Design Tokens

Notification UI uses semantic tokens for:

- surface;
- border;
- primary/secondary text;
- severity;
- hover/pressed/focus;
- progress;
- disabled/unavailable.

Support:

```text
System
Light
Dark
```

No application-specific hard-coded colors belong in shared notification controls.

### 72.22 Accessibility

Notification surfaces must be keyboard reachable where interactive.

Requirements include:

- logical focus order;
- keyboard-accessible actions/dismissal;
- screen-reader-friendly title/message/action semantics;
- severity not represented by color alone;
- progress has semantic/textual representation;
- reduced-motion preference respected where applicable;
- blocking notices used sparingly.

### 72.23 Security and Privacy

Shell-level notifications should expose only the minimum information required to
understand and act.

Do not surface sensitive application/customer information merely because an event
exists.

Permission resolution occurs before privileged details/actions are presented.

### 72.24 Failure Safety

The Shell must remain stable for:

- unknown NotificationCode;
- unknown PresentationKind;
- unknown IconKey;
- unknown RegisteredCommandCode;
- unknown WorkspaceId;
- unknown FocusTarget;
- unavailable provider;
- authorization resolver failure;
- stale Company context;
- malformed progress;
- duplicate IDs.

Unknown optional targets should degrade safely rather than crash the Shell.

### 72.25 Extensibility

A consumer application should normally add notification behavior by:

1. registering/providing notification state;
2. supplying localization resources;
3. optionally registering a GuidanceAction command/navigation/focus target;
4. relying on the shared Shell/workspace renderer.

Adding a notification should normally require no modification to shared Shell XAML.


## 73. AI-Assisted Maintainability and Focused Change Context

DynamicUI24 public contracts and documentation should support focused maintenance by a
developer or approved AI tool.

A future maintainer should be able to change a focused capability using:

```text
Relevant Specification Section
Capability Architecture / Design-System Document
Public Contracts
Focused Tests
```

without requiring a repository-wide redesign.

Module documentation should make explicit:

- module responsibility;
- allowed dependencies;
- public metadata contracts;
- supported extension points;
- files/projects normally in scope;
- tests required for focused changes;
- cross-platform constraints.

The goal is to make framework maintenance modular, auditable, and suitable for
least-context development workflows while preserving architecture and security
boundaries.


## 74. Definition of Done for Specification v0.8

Specification v0.8 is ready for implementation/adoption when these are accepted:
- six independently maintainable template modules confirmed;
- `IDynamicTemplate` and `TemplateRegistry` confirmed;
- future template registration model confirmed;
- module dependency rules confirmed;
- independent template versioning confirmed;
- module documentation structure confirmed;
- Dynamic Ribbon metadata model confirmed;
- Ribbon Setup Designer confirmed;
- contextual Ribbon resolution confirmed;
- Ribbon permission/capability boundary confirmed;
- Application Menu shell contract confirmed;
- standard Language/Appearance/About/Exit behavior confirmed;
- optional Context/Account/License integration confirmed;
- `IApplicationMenuContributor` extension model confirmed;
- Dynamic Action Bars confirmed;
- Top/Bottom Action Bar metadata model confirmed;
- selection/status-aware action presentation confirmed;
- `DynamicSigningTemplate` module confirmed;
- UI-only signing/approval boundary confirmed;
- no cryptographic/private-key/PKCS#11/HSM implementation in Dynamic UI confirmed;
- Company-scoped authorization presentation contract confirmed;
- `HIDE` / `DISABLE` / `READ_ONLY` behaviors confirmed;
- permission refresh on Company switch confirmed;
- fail-closed unresolved capability behavior confirmed;
- backend/application authoritative authorization boundary confirmed;
- 100K+ row large-grid architecture confirmed;
- provider/query abstraction confirmed;
- virtualization/windowed loading confirmed;
- sparse candidate edit model confirmed;
- large paste/import/export chunking confirmed;
- real-platform large-grid benchmark/smoke requirement confirmed;
- viewport-driven window loading confirmed;
- bounded prefetch/window cache confirmed;
- direct distant-scroll query behavior confirmed;
- edit/selection persistence across viewport unload confirmed;
- stale viewport response protection confirmed;
- global font/UI scale settings confirmed;
- grid density presets confirmed;
- resizable column/row presentation confirmed;
- per-grid layout persistence confirmed;
- Reset UI Layout confirmed;
- viewport recalculation after appearance changes confirmed;
- shared design token contract confirmed;
- Light/Dark/System resource mappings confirmed;
- semantic `IconKey` registry confirmed;
- consumer brand override confirmed;
- no direct SVG paths/raw app-specific colors in reusable template code confirmed;
- supported platform matrix confirmed;
- five-RID publish gate confirmed;
- Tier-1 native GUI certification policy confirmed;
- Ubuntu LTS x64 official support policy confirmed;
- consumer inheritance/compatibility-exception policy confirmed;
- Maintenance M1 retrofit path for Tasks 0–4 confirmed;
- DynamicDashboardTemplate confirmed;
- standard Tree/View Selector + Dashboard Canvas layout confirmed;
- reusable chart/KPI/grid widget contracts confirmed;
- dashboard shared time context and drill-down confirmed;
- DynamicHistoryDocumentTemplate confirmed;
- document viewer adapter boundary confirmed;
- visual XML report/document designer confirmed;
- scalar/collection variable binding confirmed;
- canonical XML layout persistence confirmed;
- compiled immutable render model confirmed;
- Screen/PDF/Excel/XML render targets confirmed;
- Layout XML/Data XML distinction confirmed;
- universal time filter confirmed;
- batch export/share/execute framework confirmed;
- project boundaries confirmed;
- dynamic tree model confirmed;
- dynamic column model confirmed;
- VariableCode model confirmed;
- Formula column behavior confirmed;
- Excel-like interaction scope confirmed;
- import/export behavior confirmed;
- application-engine boundary confirmed;
- PayCalc24 adoption model confirmed.

- reusable split-navigation layout contract confirmed;
- Tree Normal/Hover/Selected/Selected+Hover/Disabled/KeyboardFocus states confirmed;
- Tree `See more / Show less` overflow behavior confirmed;
- Button/Dropdown/Split/Icon/Toggle action variants confirmed;
- metadata-driven action menu contract confirmed;
- scalable action geometry and global UI/font-scale composition confirmed;
- SVG/font-glyph icon-source abstraction behind `IconKey` confirmed;
- consumer customization through metadata/tokens/registries/providers/extensions confirmed;
- Task 9 Master Catalog/Workspace/Column/Variable/Formula metadata contracts confirmed;
- `VariableCode` stability and publication/versioning rules confirmed;
- Formula metadata remains declarative/non-executable confirmed;
- Dynamic Notification & Guidance shared capability confirmed;
- Toast/Banner/Alert Card/Blocking Notice/Notification Center presentation kinds confirmed;
- notification severity/progress/priority/deduplication/dismissal/lifecycle confirmed;
- `GuidanceAction`, navigation target, and semantic focus target confirmed;
- provider/application detection boundary confirmed;
- Notification Center Shell contract confirmed;
- Company/workspace/permission/capability notification resolution confirmed;
- notification anti-spam cooldown/throttling policy confirmed;
- notification localization/theme/accessibility/security boundaries confirmed;
- safe unknown notification/action/navigation/focus behavior confirmed;
- AI-assisted focused-maintenance principle confirmed;

Future versions may add:
- richer Column Designer UX;
- drag/drop tree editing;
- saved grid layouts;
- advanced formula assistance;
- advanced Report Designer;
- performance benchmarks;
- internal package/version strategy;
- advanced accessibility certification and assistive-technology validation;
- notification analytics/telemetry governance;
- richer user-preference synchronization across applications.

**End of Specification v0.8**
