# DynamicUI24 Specification v0.16.1
## Design System Configuration & Theme Lifecycle Amendment

**Document type:** Versioned authoritative specification amendment draft

**Status:** Authoritative review candidate

**Revision basis:** v0.16 + governed Design System configuration and Theme lifecycle

**Architecture authority:** `DynamicUI24-ARCHITECTURE-CHARTER.md` v0.2

**Charter SHA-256:** `415d53271b6681cdd9d617e4ab751e7316e03816f736df97b5425c37620420cc`

**Previous authoritative specification:** `docs/specification/DynamicUI24-Spec-v0.16.md`

**Previous v0.16 SHA-256:** `b2ad142ebcd08548c3fb3deb4199ac52df3193cc9b356d56a83f22a95e8e79d0`

> v0.16.1 is additive. DynamicUI24 Specification v0.16 and all earlier non-conflicting authorities remain normative. This amendment governs configuration and lifecycle; it does not authorize a broad component retrofit or a full Theme Studio in Task 11A.

## 1. Primary law

```text
STANDARD MUTATION
!=
THEME MUTATION
!=
APPLICATION CONFIGURATION
```

The DynamicUI24 Standard is platform-governed. A Theme is configuration-governed within the Standard ceiling. Application or customer configuration MUST NOT silently redefine Standard rules.

The required presentation flow remains:

```text
Application Definition (what)
        ↓
DynamicUI24 Standard (structure, anatomy, behavior)
        ↓
Approved active Theme version (visual expression)
        ↓
Rendered UI
```

## 2. Configuration authority and contract separation

DynamicUI24 MUST provide semantic, vendor-neutral contracts with responsibilities equivalent to:

- `DesignSystemDefinition`: identity and version of the governed Standard ceiling available for inspection.
- `ThemeDefinition`: stable Theme identity and its compatibility with a Design System/Standard version.
- `ThemeVersion`: immutable published version identity and historical mappings.
- `ThemeDraft`: mutable, unpublished candidate based on an explicit generation/version.
- `ThemeActivation`: atomic selection of a published Theme version for an approved scope.
- `ThemeValidationResult`: errors, warnings, and diagnostics produced before preview, publication, or activation as policy requires.

Approved physical configuration is expressed through distinct semantic profiles or mappings equivalent to `TypographyMapping`, `ColorTokenMapping`, `SpacingProfile`, `SizingProfile`, `RadiusProfile`, `StrokeProfile`, `ElevationProfile`, `MotionProfile`, and `DensityProfile`.

These concepts MUST remain separate from application metadata, rendered controls, business state, authorization decisions, and persistence implementation. Names may evolve only if the responsibilities and boundaries remain intact.

## 3. Standard mutation boundary

The Standard owns semantic roles, component anatomy, required states, interaction and command semantics, semantic identity, responsive rules, accessibility minimums, and minimum hit-target rules.

Developer UI MAY inspect published Standard rules. Ordinary Theme editing MUST NOT mutate them. Standard mutation requires an explicitly stronger platform-governed capability and a separate governed lifecycle; possession of Theme-editing authority is insufficient.

Theme configuration MUST NOT alter:

- semantic component anatomy or identity;
- authorization outcomes;
- command semantics;
- required accessible name, focus, keyboard, contrast, or reduced-motion behavior;
- accessibility and minimum hit-target floors;
- required component/content states; or
- required responsive behavior.

## 4. Theme mutation boundary

A Theme MAY map only approved visual-expression contracts, including:

- semantic colors;
- platform font-family and permitted font-weight mappings;
- spacing, sizing, and density profile defaults where the Standard allows variation;
- radius recipes;
- strokes and borders;
- elevation and shadow recipes;
- opacity and transparency;
- semantic icon treatment; and
- semantic motion durations and easing.

A Theme MUST NOT introduce arbitrary per-control style overrides, executable behavior, application business meaning, a second component engine, or a route around the Standard.

## 5. Application and customer configuration ceiling

Application metadata MAY select an approved Theme or approved profile only where platform policy permits. It MUST NOT embed or override arbitrary token values locally.

Customer branding SHOULD be represented through governed Theme configuration. It MUST NOT be implemented as application-local control styles or an independent typography, color, spacing, component, or motion system.

Theme selection is presentation configuration only. It does not grant capability, change authorization, or mutate business data.

## 6. Theme identity and versioning

Theme identity is semantic and stable:

```text
ThemeCode + ThemeVersion
```

Display name, localized label, current visual order, and storage key are not authoritative identity.

Published Theme versions are immutable. Each version MUST preserve its complete historical mappings and compatible Standard version. A new publication creates a new version; it does not edit a published version in place.

Draft creation and publication MUST carry an expected generation/version. A generation or version conflict MUST fail before mutation. Activation MUST be atomic.

## 7. Theme lifecycle

The normative lifecycle is:

```text
Draft
  ↓
Validate
  ↓
Preview
  ↓
Publish
  ↓
Activate
  ↓
Rollback when authorized
```

- Draft is mutable and unpublished.
- Validate produces structured diagnostics without publishing or activating.
- Preview renders the candidate in isolation without changing the active Theme.
- Publish creates an immutable Theme version after required validation and authorization.
- Activate atomically selects a published version.
- Rollback atomically selects an earlier published version and MUST NOT delete or rewrite newer history.

Validation and Preview MAY repeat. Preview does not imply publication; publication does not imply activation unless an explicitly approved atomic workflow says so.

## 8. Preview isolation

Theme Preview MUST preserve this invariant:

```text
ACTIVE THEME and production/current presentation remain unchanged

while

DRAFT THEME renders inside a controlled Preview surface
```

Preview MUST use an isolated resolution scope and MUST NOT replace global/current Theme resources, mutate the active-version record, reconstruct application business state, or persist preview choices as application preferences.

A Design System Preview Lab SHOULD render representative shared examples for typography, buttons, editors/form fields, Grid, Navigation Tree, Action Bar/Menu, pane, notification, and content states. Samples are semantic proof fixtures, not application data and not a second UI framework.

Preview may be physically incomplete until the owning component retrofit phase. Its presence MUST NOT be used to claim that non-retrofitted production components are v0.16-compliant.

## 9. Authorization

Theme governance reuses Task 10H Dynamic Authorization and semantic capability checks. Core MUST NOT hard-code Viewer, Editor, Administrator, or other role names.

The authorization model MUST distinguish capabilities equivalent to:

```text
CanViewDesignSystem
CanEditThemeDraft
CanPreviewTheme
CanPublishTheme
CanActivateTheme
CanRollbackTheme
```

Capabilities are independent unless platform policy explicitly composes them. In particular, edit or preview authority does not imply publish, activate, or rollback authority. Standard mutation requires a separate, stronger platform-governed capability and MUST NOT be inferred from any Theme capability.

Authorization remains separate from validation: a technically valid Theme cannot be published or activated by an unauthorized actor.

## 10. Validation and accessibility ceiling

Theme validation MUST evaluate all rules required by the compatible Standard and policy. It MUST reject invalid mappings and MAY produce policy-defined warnings for reviewable risks.

Validation includes, where applicable:

- missing required semantic tokens;
- incomplete required Light/Dark mappings;
- invalid or unsupported font/fallback mappings;
- invalid contrast against the governing accessibility policy;
- sizing or density mappings that would violate minimum hit targets or usability floors;
- invalid radius, stroke, elevation, opacity, or icon recipes;
- invalid motion duration/easing or reduced-motion behavior; and
- Standard-version incompatibility.

Themes cannot override the accessibility ceiling. System appearance resolution MUST result in an approved Light or Dark mapping. Validation results MUST identify the relevant semantic token/profile and provide safe diagnostics without leaking protected data.

## 11. Runtime activation and state preservation

Runtime activation or appearance-mode switching MUST preserve application business state, editor values, Grid state, selection, pane state, authorization, application drafts, Theme drafts, and report runtime state.

Theme resolution MUST NOT trigger unnecessary reconstruction of stateful controls. Theme activation changes the active visual mapping atomically; Light, Dark, and System remain appearance modes of the selected Theme version rather than independent Themes.

Activation failure MUST leave the previously active Theme intact. Unsupported or incompatible versions fail before resource mutation.

## 12. Persistence boundary

Core MUST define vendor-neutral repository/provider seams for Theme drafts, immutable published versions, active-version resolution, generation checks, and lifecycle history. Core MUST NOT contain MariaDB, SQL, ORM, file-format, or vendor-specific persistence implementation.

Persistence, transactions, concurrency implementation, storage security, backup, and retention belong to platform/application infrastructure behind those seams. Repository results return semantic lifecycle models, not rendered controls.

## 13. Audit seam

Theme lifecycle MUST expose semantic audit events equivalent to:

- Theme draft created;
- validation completed;
- Theme version published;
- Theme version activated; and
- rollback activated.

Events SHOULD include semantic Theme/version identity, outcome, actor/context references permitted by policy, timestamp, and correlation identity. They MUST NOT contain unnecessary raw sensitive data or physical control instances.

DynamicUI24 Core exposes event contracts; authoritative audit storage, retention, integrity, and external delivery remain platform/application infrastructure-owned.

## 14. Developer UI Authoring boundary

The canonical future physical configuration surface belongs under the existing Developer UI Authoring/Design System administration model:

```text
Developer UI Authoring
└── Design System
    ├── Standard
    ├── Themes
    ├── Typography
    ├── Colors
    ├── Spacing & Sizing
    ├── Components
    ├── Density
    ├── Motion
    └── Preview Lab
```

This surface reuses existing authoring lifecycle, Dynamic Authorization, validation, command, notification, and audit seams. It MUST NOT become a separate administration application or a second design engine.

Standard is inspection-first. Theme pages configure only approved mappings. The complete physical authoring and Preview Lab surface belongs to Task 11F unless a later authoritative task explicitly reschedules it.

## 15. Task 11A implementation impact

After v0.16.1 adoption, Task 11A MAY add only foundational contracts/seams for:

- Theme draft and published-version identity;
- validation models and validators;
- lifecycle repository/provider boundaries;
- isolated Preview resolution;
- activation resolution and atomic activation requests/results;
- semantic lifecycle audit events; and
- Dynamic Authorization capability requirements.

Task 11A MUST NOT build the full Theme Studio/Preview Lab, persistence implementation, arbitrary style editor, broad component retrofit, or application-specific configuration system.

Tasks 11B–11G remain unopened. Their existing retrofit ownership is unchanged; Task 11F remains the expected owner of the full physical Design System configuration surface.

## 16. Acceptance requirements

Before this amendment is authoritative, reviewers MUST verify its compatibility with the Constitution, Architecture Charter, v0.16, Application Development Policy, Task 10H authorization model, and existing Developer UI Authoring lifecycle.

After adoption, foundational implementation requires focused lifecycle, authorization, preview-isolation, immutability, generation-conflict, activation/rollback, vendor-neutrality, and state-preservation tests. Physical acceptance applies only to an actually exposed reference/preview surface; it does not confer compliance on components awaiting 11B–11G retrofit.
