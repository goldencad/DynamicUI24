# DynamicUI24 Application Development Policy

**Document Type:** Governance / Application Development Policy\
**Project:** DynamicUI24\
**Status:** Proposed authoritative application-development governance\
**Applies To:** All developers, Codex/local agents, reviewers,
maintainers, and consuming applications that configure, compose, extend,
or integrate DynamicUI24\
**Authoritative Location:**
`docs/governance/DYNAMICUI24-APPLICATION-DEVELOPMENT-POLICY.md`\
**Related Governance:** `DynamicUI24-ARCHITECTURE-CHARTER.md`,
`docs/governance/AI-DEVELOPMENT-POLICY.md`, and the applicable versioned
DynamicUI24 specification.

------------------------------------------------------------------------

## 1. Purpose

DynamicUI24 is the reusable presentation foundation for TS24
applications. This policy governs how application developers use it.

It prevents the pattern:

``` text
Developer inspects a screen
→ guesses the architecture
→ creates controls manually
→ wires local events
→ embeds business/security/state logic in presentation
→ creates a second pattern
```

The required path is:

``` text
Charter / Authoritative Spec
→ Application Development Policy
→ Developer Guide / Golden Pattern
→ Application metadata + providers + commands + policies
→ DynamicUI24 Runtime
→ Application business/calculation/integration services
```

**Absence of an understood implementation pattern is not permission to
invent one.**

------------------------------------------------------------------------

## 2. Core Rule

> **Application developers MUST configure and compose existing
> DynamicUI24 semantic capabilities before writing application-specific
> UI code.**

A mature consuming application should primarily provide metadata,
providers, commands, business/application services, adapters, and
application-specific policies. DynamicUI24 owns reusable presentation
mechanics.

------------------------------------------------------------------------

## 3. Authority Order

Use this order:

1.  DynamicUI24 Architecture Charter.
2.  Applicable authoritative DynamicUI24 specification.
3.  This policy.
4.  Security/privacy policies.
5.  Approved Developer Guide / Golden Pattern.
6.  Public framework contracts.
7.  Application specification.
8.  Existing implementation as supporting evidence only.

Existing code is not automatically authoritative architecture. If it
conflicts with a higher authority, **STOP and escalate**.

------------------------------------------------------------------------

## 4. Relationship to AI Governance

This policy defines what application implementation is architecturally
allowed. `AI-DEVELOPMENT-POLICY.md` defines how Cloud AI and Local
AI/Codex divide architecture, implementation, security review,
repository work, testing, and escalation.

Both apply simultaneously. Developers/Local AI may implement repetitive
application modules under approved patterns, but MUST stop when a new
framework primitive, architecture change, security-boundary change, or
invariant-breaking workaround is required.

------------------------------------------------------------------------

## 5. Required Application Architecture

``` text
Application Definition / Metadata
        ↓
DynamicUI24 semantic contracts
        ↓
DynamicUI24 Runtime
        ↓
Provider / Adapter boundary
        ↓
Application / Domain / Business services
        ↓
Authoritative engines and integrations
```

Never make a visual control the owner of business calculation,
persistence, permission decisions, or durable business state.

------------------------------------------------------------------------

## 6. Semantics Before Visuals

Developers MUST use semantic identity such as `WorkspaceCode`,
`SheetCode`, `VariableCode`, `RowKey`, `CommandCode`, `HelpContextCode`,
`PolicyCode`, `ProviderCode`, and other officially defined semantic
codes.

Localized labels, tab captions, visual indexes, screen coordinates,
theme, font, current order, and Avalonia control instances are
presentation only.

Renaming, reordering, localization, theme changes, hide/show,
virtualization, and rematerialization MUST NOT break application
meaning.

------------------------------------------------------------------------

## 7. Separate Definition, Runtime, Business State, and Controls

``` text
Definition / Metadata
!= Draft
!= Runtime UI State
!= Business State
!= Rendered Control
```

Published definitions are stable/read-focused. Drafts belong to the
authoring lifecycle. Runtime state is transient. Rendered controls are
disposable presentation.

Presentation changes such as theme, panel size, column reorder, tab
reorder, or privacy presentation MUST NOT mark business data dirty.

------------------------------------------------------------------------

## 8. Mandatory Decision Ladder

### Level 1 --- Configure First

If DynamicUI24 already supports the requirement, developers MUST
configure/compose it.

Examples: Workspace, navigation, Ribbon/action placement, menus, fields,
Universal Editor, Grid columns, Sheets, layout, localization, Help,
Search registration, Quick Access semantics, authorization requirements,
privacy presentation, Context Panel, and supported Pane/Composer/Report
metadata.

### Level 2 --- Approved Provider / Adapter / Command

If DynamicUI24 owns the UX but application-specific data or behavior is
needed, implement the approved boundary.

Examples:

``` text
Lookup UI       → application lookup provider
Search UI       → application search provider
Grid UI         → application data provider
Command surface → registered semantic command
Context Panel   → context provider
Documents       → approved vendor-neutral adapter
```

### Level 3 --- Approved Application Extension

Application-specific UI code is allowed only when the requirement is
genuinely application-specific, no existing shared capability owns it,
an approved extension seam exists, no shared subsystem is duplicated,
and business/security authority remains outside presentation.

### Level 4 --- Framework Capability Gap

If the requirement is reusable or needs a new shared interaction
primitive:

``` text
STOP
→ record DynamicUI24 Capability Gap
→ architecture review
→ specification decision
→ Golden Pattern/framework implementation
→ application adoption
```

Do not hide a framework gap inside one application.

------------------------------------------------------------------------

## 9. Configuration-First, Not Metadata-Everything

Prefer:

``` text
Configuration > custom UI code
Composition   > duplication
Provider      > control coupling
Semantic ID   > visual identity
Shared command > local event wiring
```

But do not encode arbitrary scripts, SQL, hidden business logic, opaque
imperative workflows, or a second calculation language into UI metadata.

------------------------------------------------------------------------

## 10. Developer UI Authoring

Where supported, production UI definitions SHOULD use the governed
lifecycle:

``` text
Published
→ Create Draft
→ Edit
→ Validate
→ Preview
→ Publish
→ Immutable Version
→ Activate
```

Rollback activates a prior valid version without deleting newer history.
Preview MUST NOT publish or activate. Published metadata MUST NOT be
edited in place.

------------------------------------------------------------------------

## 11. Universal Editor Policy

For supported generic values, developers MUST use the DynamicUI24
Universal Editor rather than create a second generic editor framework.

Preserve native OS input, Unicode/IME, caret/selection, validation,
ReadOnly/Disabled semantics, semantic actions, localization, and
accessibility.

Do not implement language-specific keyboard mapping, focus forcing, text
rewriting, or startup timers as IME workarounds.

Missing reusable editor kinds are capability-gap candidates.

------------------------------------------------------------------------

## 12. DataEntry / Grid Policy

Authoritative identity:

``` text
RowKey + VariableCode
```

Business meaning MUST NOT depend on visible column position.

Grid behavior must preserve bounded virtualization, selection, editing,
clipboard, validation, undo/redo, import/export, personalization,
sort/filter, privacy, accessibility, and context integration.

``` text
100,000 logical rows != 100,000 visual controls
```

Never materialize the full logical dataset merely to implement
presentation behavior.

------------------------------------------------------------------------

## 13. Multi-Sheet Policy

Authoritative identity:

``` text
WorkspaceCode + SheetCode
```

Tab title/order are presentation. Duplicate and Save As create a new
`SheetCode`. UI MUST NOT clone business data by enumerating visual rows;
providers/business/data layers own physical cloning.

------------------------------------------------------------------------

## 14. Business and Calculation Boundary

``` text
DynamicUI24 UI
→ semantic values/commands
→ Application layer
→ Authoritative business/calculation engine
→ result/diagnostics
→ DynamicUI24 presentation
```

Where TS24 Calculation Engine is authoritative, developers MUST NOT
create another formula/calculation engine in DynamicUI24 or application
UI.

Cross-sheet formulas use stable semantic identity. Never rewrite
formulas using localized titles, tab indexes, or blind string
replacement.

------------------------------------------------------------------------

## 15. Database / Storage Boundary

Presentation MUST NOT become the database layer. Do not put SQL, ORM
persistence, connection management, or database-specific business
behavior into generic DynamicUI24 controls/definitions.

Use approved application/domain/repository/provider boundaries.

------------------------------------------------------------------------

## 16. Shared Systems Must Be Reused

Applications MUST reuse the approved semantic infrastructure for:

-   commands/actions;
-   Global Search;
-   Quick Access;
-   Context Panel;
-   notifications;
-   navigation;
-   Ribbon/menu/action bars;
-   localization;
-   theme;
-   preferences;
-   Help routing;
-   authorization;
-   privacy presentation;
-   UI-definition lifecycle.

Do not create parallel global systems inside an application.

------------------------------------------------------------------------

## 17. Search and Quick Access

Search providers return semantic activation targets and MUST respect
authorization, privacy, stale context, and bounded behavior. Search does
not instantiate arbitrary screens merely to search them.

Quick Access is preference, not authority. Pinned/Favorite/Recent
entries store semantic references and re-resolve current
metadata/security. They MUST NOT grant permission or resurrect
unauthorized content.

------------------------------------------------------------------------

## 18. Authorization

Use the approved centralized semantic authorization resolver.

Keep distinct:

``` text
Hidden
Disabled
ReadOnly
Enabled
```

Do not scatter hard-coded role checks through views. Application
roles/profiles map to permissions/capabilities through approved
application boundaries; product role names do not belong in DynamicUI24
Core.

------------------------------------------------------------------------

## 19. Permission, Capability, Policy, Privacy

``` text
Permission
!= Capability
!= Policy
!= Privacy Presentation
```

Privacy does not grant authorization. Privacy OFF cannot bypass
mandatory policy.

If protected authorization/privacy resolution is stale, unavailable,
ambiguous, or invalid, **fail closed**.

------------------------------------------------------------------------

## 20. Preferences

Preferences overlay metadata; they do not mutate published metadata and
cannot override authorization or mandatory privacy.

Stale/invalid preferences repair deterministically. A preference MUST
NOT resurrect a denied field, column, action, sheet, or workspace.

------------------------------------------------------------------------

## 21. P1 Privacy

Protect sensitive data across primary and secondary surfaces, including
Grid/Form, Search, Quick Access, notifications, Context Panel, tooltips,
clipboard, import preview, export, accessibility, diagnostics,
tabs/subtitles, authoring inspector, Draft preview, validation, version
history, and inactive caches.

Masking the visible field is insufficient if raw data leaks elsewhere.

------------------------------------------------------------------------

## 22. Company/Tenant Context and Stale Work

Late results from an old context MUST NOT appear after context changes.

**Cancellation is optimization. Generation/context validation is
correctness.**

Apply this to Grid, Lookup, Search, Context, Preview, Sheets, Privacy,
documents, authorization, and application providers.

------------------------------------------------------------------------

## 23. Localization, Theme, Accessibility

Localization and theme change presentation, not semantics, business
data, authorization, calculation meaning, Draft contents, or version
identity.

Do not use localized display text as a business key.

Application extensions must preserve accessibility names, roles, states,
keyboard behavior, and virtualization while respecting privacy.

------------------------------------------------------------------------

## 24. Native Input

OS/native text input remains authoritative.

MUST NOT introduce language-specific key remapping, IME repair timers,
unjustified focus forcing, text rewriting as an input workaround, or a
second text-input system.

Native defects require correct lifecycle/platform isolation and
real-platform evidence.

------------------------------------------------------------------------

## 25. Lazy Construction

Inactive heavy workspaces SHOULD remain lazy. Do not eagerly construct
large Grid/Sheet/Editor/Report/Document/Authoring/history surfaces at
cold start unless an authoritative requirement demands it.

------------------------------------------------------------------------

## 26. Provider Failure Isolation

One provider failure MUST NOT crash the Shell. Use safe error states,
bounded diagnostics, stale-result rejection, cancellation where useful,
and retry where meaningful.

------------------------------------------------------------------------

## 27. Vendor Boundary

DynamicUI24 Core is vendor-neutral. Do not leak DevExpress, Actipro,
Avalonia-control, native-platform, or other vendor-specific types into
generic Core contracts unless explicitly authorized.

Vendor-specific implementations belong behind approved
adapters/extensions.

------------------------------------------------------------------------

## 28. Documents and Signing

Prefer semantic document references, streams, bounded buffers, lazy
preview, cancellation, and explicit capabilities.

Document processing is not digital signing. Signing remains a separate
authoritative boundary.

------------------------------------------------------------------------

## 29. No Arbitrary Execution

UI metadata MUST NOT become an arbitrary execution platform.

Prohibited by default: arbitrary scripts, arbitrary SQL execution,
VBA/macros runtime, untrusted reflection/plugin scanning, embedded
executable execution, automatic untrusted external-link execution, and
hidden imperative business workflows encoded as metadata.

------------------------------------------------------------------------

## 30. Prohibited Duplicate Systems

Without an explicit architecture change, application teams MUST NOT
create a second:

-   generic editor system;
-   Grid interaction system;
-   SheetHost system;
-   Global Search system;
-   Quick Access system;
-   authorization resolver;
-   privacy presentation system;
-   Context Panel framework;
-   notification framework;
-   global navigation framework;
-   Ribbon/action-bar framework;
-   localization framework;
-   theme framework;
-   preference framework;
-   Help-routing framework;
-   formula/calculation engine;
-   UI-definition lifecycle.

If the existing capability is insufficient, file a Capability Gap.

------------------------------------------------------------------------

## 31. Prohibited Application Patterns

MUST NOT:

-   put business calculation inside UI controls;
-   access the database directly from generic presentation controls;
-   scatter hard-coded role checks through views;
-   use visual indexes as business identity;
-   persist Control instances as durable identity/state;
-   let preferences override authorization;
-   treat masking as authorization;
-   expose raw sensitive values through
    diagnostics/accessibility/secondary surfaces;
-   eagerly materialize huge datasets;
-   eagerly construct inactive heavy workspaces;
-   replace shared Search/Editor/Command systems locally;
-   add language-specific IME hacks;
-   bypass privacy in clipboard/export;
-   edit published definitions directly;
-   delete history to implement rollback;
-   fork generic framework code per application/customer.

------------------------------------------------------------------------

## 32. Capability Gap Procedure

When DynamicUI24 appears insufficient:

1.  Confirm against Charter, applicable Spec, Developer Guide, Golden
    Pattern, public contracts, and current supported
    metadata/editor/provider capability.
2.  Do not implement a private workaround.
3.  Record a Capability Gap containing:
    -   application need;
    -   current capability;
    -   why it is insufficient;
    -   required semantic behavior;
    -   security/privacy impact;
    -   expected cross-application reuse;
    -   bounded/performance impact;
    -   proposed extension seam;
    -   minimal acceptance scenario.
4.  Classify it as configuration/documentation gap, provider/adapter
    gap, application-specific extension, reusable DynamicUI24
    capability, or security/privacy architecture change.
5.  New shared primitives/cross-module contracts require
    specification-first approval.

------------------------------------------------------------------------

## 33. Golden Pattern Rule

Once a Golden Pattern exists, structurally equivalent application
modules MUST follow it.

``` text
One approved Golden Pattern
→ many application definitions/providers
```

A Golden Pattern includes ownership, contracts, identity, state,
security/privacy, extension points, tests, and common failure
modes---not merely sample code.

------------------------------------------------------------------------

## 34. Developer Documentation Rule

This policy defines mandatory governance. Developer Guides explain
implementation.

Every reusable capability SHOULD document:

``` text
WHAT IT OWNS
WHAT IT DOES NOT OWN
CORE CONTRACTS
IDENTITY RULES
STATE RULES
SECURITY RULES
PRIVACY RULES
CONFIGURATION MODEL
PROVIDER / ADAPTER EXTENSION POINTS
EXAMPLE
FOCUSED TESTS
COMMON FAILURE MODES
```

Developers MUST consult the applicable guide before implementing the
capability.

------------------------------------------------------------------------

## 35. Application Project Bootstrap

Every consuming project SHOULD declare:

-   DynamicUI24 version/spec baseline;
-   authoritative business/calculation engines;
-   application providers/adapters;
-   application commands;
-   security/privacy mapping;
-   approved framework extensions;
-   known capability gaps;
-   required physical acceptance platforms.

Recommended project docs:

``` text
docs/governance/DYNAMICUI24-ADOPTION.md
docs/architecture/UI-CAPABILITY-MAP.md
docs/adoption/PROJECT-UI-GOLDEN-PATTERNS.md
```

------------------------------------------------------------------------

## 36. Required Task Governance Preamble

Application UI tasks SHOULD include:

``` text
DYNAMICUI24 APPLICATION GOVERNANCE

This task is governed by:
- DynamicUI24 Architecture Charter
- applicable DynamicUI24 specification
- DYNAMICUI24-APPLICATION-DEVELOPMENT-POLICY.md
- AI-DEVELOPMENT-POLICY.md

Do not invent application-local replacements for existing DynamicUI24 systems.
Configure first.
Use approved providers/adapters second.
STOP and escalate framework capability gaps.
Do not bypass semantic identity, authorization, privacy, lifecycle, or
authoritative business/calculation engines.
```

------------------------------------------------------------------------

## 37. Mandatory Stop Conditions

Developer/Codex/Local AI MUST STOP when:

-   a required DynamicUI24 contract is missing;
-   a new framework primitive is required;
-   framework behavior conflicts with authoritative spec;
-   a workaround duplicates a shared subsystem;
-   semantic identity would be violated;
-   permission/privacy behavior is ambiguous;
-   business logic appears to require UI ownership;
-   a new calculation/formula engine appears necessary;
-   presentation appears to require direct DB coupling;
-   vendor types would leak into Core;
-   bounded virtualization cannot be preserved;
-   stale-context correctness cannot be guaranteed;
-   native input would require language-specific handling;
-   published metadata would need in-place mutation;
-   the only solution changes shared architecture.

**Do not "make it work for this customer" by bypassing architecture.**

------------------------------------------------------------------------

## 38. Allowed Developer Autonomy

Within an approved pattern, developers/Local AI may create application
metadata, register workspaces, configure
navigation/actions/editors/Grid/Sheets, implement approved
providers/adapters, register commands, map permissions/capabilities, add
localization, tests, semantic-preserving migrations, adoption docs, and
implementation fixes that do not change shared architecture.

High implementation autonomy does not grant architecture autonomy.

------------------------------------------------------------------------

## 39. Review Checklist

Before accepting application UI work, verify:

-   DynamicUI24 remains presentation authority.
-   Existing capability was configured before custom code.
-   No shared subsystem was duplicated.
-   Business/calculation remains outside UI.
-   Semantic identity is used.
-   Behavior survives reorder/localization/theme/rematerialization.
-   Authorization is centralized and fail-closed.
-   Permission/Capability/Policy/Privacy remain distinct.
-   Secondary surfaces do not leak protected values.
-   Definition/Draft/runtime/business/control states are separated.
-   Async stale results are rejected.
-   Materialization is bounded.
-   Heavy inactive workspaces remain lazy.
-   Universal Editor and shared Search/Quick
    Access/Context/Notification/Command systems are reused.
-   Native IME is preserved.
-   Vendor coupling remains localized.

------------------------------------------------------------------------

## 40. Testing and Physical Acceptance

Use focused tests appropriate to risk, including semantic identity,
authorization, P1/privacy, preference precedence, stale context,
localization, theme, accessibility, Universal Editor, Grid/DataEntry,
boundedness, Draft lifecycle, lazy construction, provider isolation,
command/search/navigation activation, and migration compatibility.

Automated tests do not replace required real-platform acceptance for
native behavior such as IME/platform integration.

------------------------------------------------------------------------

## 41. Git and CI Safety

Before implementation verify repository, branch, baseline HEAD,
`origin/main`, worktree, and applicable spec/hash.

Before commit run focused tests, architecture tests, required physical
smoke, and `git diff --check`; authoritative specs must remain unchanged
unless the task is an approved spec change.

Push/release follows explicit project authorization and CI gates. No
force-push or silent shared-history rewrite.

------------------------------------------------------------------------

## 42. Legacy Migration Rule

Legacy UI is evidence of proven business behavior, not automatically
desired architecture.

Preserve business rules, semantic meaning, validated workflows, and
authoritative calculation behavior.

Do not blindly preserve control coupling, UI-owned calculation,
event-handler architecture, visual-index identity, vendor leakage, or
duplicated framework services.

------------------------------------------------------------------------

## 43. Customer-Specific Configuration

Customer differences SHOULD use approved metadata, policies,
permission/capability, preferences, providers, or application
configuration.

Do not fork framework code per customer. Customer configuration cannot
weaken mandatory security/privacy rules.

------------------------------------------------------------------------

## 44. Decision Table

  -----------------------------------------------------------------------
  Requirement                         Required approach
  ----------------------------------- -----------------------------------
  Add Workspace                       Configure semantic definition

  Add navigation entry                Configure shared navigation
                                      metadata

  Add field                           Configure definition

  Change generic editor               Configure Universal Editor

  Add Grid column                     Configure by `VariableCode`

  Add Sheet                           Configure by `SheetCode` +
                                      provider/lifecycle

  Add Lookup                          Configure editor + approved
                                      provider

  Add global search result            Shared Search provider

  Add contextual information          Context provider

  Add action                          Registered semantic command/action

  Hide by authorization               Requirement + shared resolver

  Reorder columns                     Preference overlay

  Customer label change               Localization/configuration

  Payroll/tax calculation             Authoritative business/calculation
                                      engine

  Query business data                 Approved provider/application
                                      service

  Unsupported reusable editor         **STOP → Capability Gap**

  Second Search/navigation system     **Prohibited → reuse/escalate**

  New shared interaction primitive    **STOP → architecture/spec review**

  Direct SQL from UI                  **Prohibited**

  Customer-specific framework fork    **Prohibited by default**

  Arbitrary script in metadata        **Prohibited**
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 45. Exceptions

Exceptions require explicit human architecture approval and must
document reason, scope, affected application/customer, bypassed rule,
security/privacy impact, duration, owner, mitigation, removal/migration
plan, and acceptance tests.

Urgency does not silently override this policy.

------------------------------------------------------------------------

## 46. Auditability

A completed application UI task should answer:

-   Which DynamicUI24 spec governed it?
-   Which Golden Pattern was followed?
-   What metadata was configured?
-   What provider/adapter was implemented?
-   Was a capability gap introduced?
-   What semantic identities are authoritative?
-   What authorization/privacy rules apply?
-   Which authoritative business/calculation engine is used?
-   Which tests and physical acceptance passed?
-   Which commit and CI run validated it?

------------------------------------------------------------------------

## 47. Recommended Application Spec Clause

``` text
DYNAMICUI24 APPLICATION DEVELOPMENT GOVERNANCE

DynamicUI24 is the authoritative reusable UI foundation for this application.

Application developers MUST configure and compose existing DynamicUI24
semantic capabilities before writing application-specific UI code.

Application-specific data and behavior MUST use approved providers, adapters,
commands and business/application services.

Developers MUST NOT create application-local replacements for existing
DynamicUI24 editor, grid, sheet, search, Quick Access, authorization, privacy,
context, notification, navigation, localization, theme, preference, help or
UI-definition lifecycle systems.

Business/calculation authority remains outside DynamicUI24.

If the required capability is absent or implementation would require changing
a shared architecture invariant, development MUST STOP and escalate a
DynamicUI24 Capability Gap for architecture/specification review.
```

------------------------------------------------------------------------

## 48. Strongest Rules

``` text
CONFIGURATION:
Configure first; custom application UI only through approved seams.

CONSISTENCY:
One DynamicUI24 capability → one authoritative pattern → many configurations.

IDENTITY:
Semantic identity is authoritative; visual identity is presentation.

BUSINESS:
UI presents and coordinates; business/calculation engines decide and calculate.

SECURITY:
Preference and privacy presentation never grant authorization; fail closed.

CAPABILITY GAP:
If DynamicUI24 does not support it, STOP; do not build a private framework.

DEVELOPER:
Do not guess architecture from code.
Follow Charter → Spec → Policy → Developer Guide → Golden Pattern.
```

------------------------------------------------------------------------

## 49. Governing Statement

> DynamicUI24 applications are built by describing application semantics
> and connecting approved providers, commands, policies, and
> authoritative business services to a shared presentation foundation.

> Application developers configure first, extend through approved seams
> second, and escalate reusable capability gaps instead of inventing
> parallel UI architecture.

> DynamicUI24 coordinates presentation. It does not steal ownership from
> business, calculation, storage, signing, security, or other
> authoritative engines.

------------------------------------------------------------------------

**End of DynamicUI24 Application Development Policy**
