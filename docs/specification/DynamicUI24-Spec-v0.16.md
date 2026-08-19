# DynamicUI24 Specification v0.16
## Unified Design System, Theme & Presentation Standard

**Document type:** Versioned authoritative specification amendment

**Status:** Task 11A review candidate

**Revision basis:** v0.15 + unified presentation governance

**Architecture authority:** `DynamicUI24-ARCHITECTURE-CHARTER.md` v0.2

**Charter SHA-256:** `415d53271b6681cdd9d617e4ab751e7316e03816f736df97b5425c37620420cc`

**Previous specification:** `docs/specification/DynamicUI24-Spec-v0.15.md`

**Previous v0.15 SHA-256:** `423fc2270b377ddf6730c0166c36654306f600fdccb68510a56e251dd5b50cc5`

> v0.16 is additive. Earlier requirements remain authoritative unless this document explicitly extends them. DynamicUI24 is the authoritative owner of TS24 application UI presentation standards. Task 11A establishes contracts and governance; it does not perform the 11B–11G control retrofit.

## 1. Authority and required flow

```text
Application Definition
        ↓
DynamicUI24 Semantic Component / Token
        ↓
DynamicUI24 Presentation Standard
        ↓
Theme Resolution
        ↓
Rendered UI
```

`STANDARD != THEME != APPLICATION METADATA`.

- Application metadata owns **what** is presented and semantic identity.
- The Standard owns **how** semantic UI is structured and behaves.
- A Theme owns **how** that Standard is visually expressed.

Applications MUST consume DynamicUI24 semantic contracts and MUST NOT create an independent application UI styling system.

## 2. Normative Standard

The Standard owns semantic component roles, anatomy, layout structure, typography hierarchy, sizing, spacing, density, responsive behavior, interaction and component-state contracts, focus and keyboard behavior, accessibility, semantic color roles, and semantic motion roles.

The Standard is stable across operating systems, theme generations, Light/Dark/System appearance, localization, and rematerialization. It MUST NOT contain product-specific business meaning or vendor control types.

## 3. Theme contract

A versioned Theme maps Standard semantics to replaceable visual recipes: concrete palette, platform font family, permitted weight mappings, radii, strokes, elevation, opacity, icon treatment, motion values, interaction visuals, and component recipes. A Theme declares its identity, version, compatible Standard version, supported appearance modes, and token values.

Light and Dark are concrete modes. System delegates appearance selection to the platform. A future theme generation can replace the current generation without changing application metadata or code. Branding names such as `TS24.Default.2026` are examples, not frozen identifiers.

Applications MUST NOT bind directly to concrete theme values where a semantic token exists. Raw colors belong only in theme implementation or explicitly reviewed document/content rendering.

## 4. Semantic token foundation

Required foundation categories are Typography, Spacing, Sizing, Radius, Stroke, Elevation, Motion, Opacity, and Icon Geometry.

Required semantic color roles are:

```text
Surface.Window       Surface.Workspace    Surface.Panel
Surface.Editor       Surface.Selected     Surface.Hover
Text.Primary         Text.Secondary       Text.Muted       Text.Disabled
Border.Default       Border.Subtle        Border.Focus
Accent.Primary       Accent.Secondary
Status.Success       Status.Warning       Status.Critical  Status.Info
```

Token keys express meaning, never a physical value. Renaming or changing a published token's meaning is a contract change.

## 5. Typography authority

DynamicUI24 owns application-UI typography through `Typography.Display`, `Typography.PageTitle`, `Typography.SectionTitle`, `Typography.Subtitle`, `Typography.Body`, `Typography.BodySmall`, `Typography.Caption`, `Typography.Label`, `Typography.Button`, `Typography.Input`, `Typography.Grid`, `Typography.GridHeader`, `Typography.Menu`, `Typography.Navigation`, and `Typography.Code`.

Applications MUST NOT hard-code product UI font families, create local typography scales, or establish app-local font-size conventions. Themes SHOULD map UI roles to the platform system UI family and Code to a platform monospace family. Mapping MUST be Unicode-first and include safe fallbacks. Text layout MUST tolerate Vietnamese and other supported scripts without assuming glyph width.

Document-native typography remains owned by document content and DocsView24 rendering; it is outside this application-UI authority.

## 6. Spacing, sizing, and density

The base spacing scale is `Space.2XS`, `XS`, `S`, `M`, `L`, `XL`, `2XL`. Standard aliases include `Form.RowGap`, `GroupGap`, `SectionGap`, `ColumnGap`, `LabelGap`, `Pane.Padding`, `Pane.HeaderGap`, `Toolbar.ItemGap`, `Grid.CellPadding`, `Navigation.RowGap`, and `Dialog.SectionGap`.

Sizing roles include `Control.Height.Compact|Standard|Large`, `Editor.Width.Short|Compact|Medium|Long|Fill`, `Icon.Size.Small|Standard|Large`, `HitTarget.Minimum`, and `Form.ReadableWidth`. Compact values MUST NOT be forced to fill a workspace.

Density roles are Compact, Standard, and Comfortable. Density may alter row/control height, spacing, grid padding, navigation rows, and toolbar geometry. It MUST NOT alter identity, authorization, business values, or application logic.

## 7. Motion, effects, and accessibility

Motion roles are `Motion.None`, `Fast`, `Standard`, and `Emphasized`. Themes choose duration and easing. Motion may clarify hover, selection, pane reveal, flyout, notification, and state transitions; it MUST respect reduced-motion preferences and MUST NOT reduce enterprise usability. Radius, stroke, elevation, transparency, and effects are theme recipes selected by semantic role.

## 8. Standard component state model

Shared controls use consistent semantics for Normal, Hover, Pressed, Focused, Selected, Disabled, ReadOnly, Error, Warning, and Loading. Only applicable states are required. Theme switching MUST preserve semantic/business state, values, selections, pane/grid state, authorization, Draft state, and report state; it requires no application-specific rebuild.

## 9. Buttons and actions

Button roles are Primary, Secondary, Tertiary, Danger, Icon, Split, and Overflow. The Standard owns anatomy, hit target, icon/text arrangement, loading/disabled behavior, focus, keyboard activation, and accessibility. Themes own recipes. Applications MUST NOT invent equivalent local roles.

Action Bars and Toolbars use the existing semantic command infrastructure. The Standard owns hierarchy, spacing, icon geometry, focus/hover/disabled behavior, contextual placement, overflow, and responsive collapse. v0.16 creates no command system.

## 10. Universal Editors and forms

Editor roles are Text, Multiline, Integer, Decimal, Currency, Percentage, Boolean, Date, Time, DateTime, DateRange, Choice, Lookup, SearchLookup, MultiChoice, Password, Hyperlink, and ButtonEdit.

Common anatomy covers height, border/focus, ReadOnly, Disabled, Error, help, validation, labels, spacing, width class, and accessible name. Raw implementation decomposition MUST NOT become user-facing semantics. Date/Time is an accepted reference pattern.

Forms group related fields, preserve compact editors, use readable width, and derive layout from field relationships. DynamicUI24 owns responsive reflow; application metadata MUST NOT use absolute X/Y positions as layout authority. Labels remain associated with editors. DateRange remains one semantic group, never six independent day/month/year fields.

## 11. Grid

Standard roles are Grid.Header, Cell, InputCell, FormulaCell, SystemCell, GroupHeader, Footer, Selection, ActiveCell, Validation, Empty, and Loading. The Standard owns row/header geometry, typography, padding, borders, selection/focus, validation, sort/filter indication, hierarchy, and density. Report Grid and DataEntry Grid share a design language while retaining distinct semantics.

## 12. Navigation Tree

The shared Standard owns row height, indentation, chevron geometry/placement, semantic icon geometry, baseline alignment, parent/leaf anatomy, selected/hover/focused/disabled states, unauthorized hidden behavior, badge/count and context-action placement, supported drag/drop affordance, density, keyboard navigation, and accessibility.

`NodeCode` and the semantic target remain identity; tree position is presentation. Applications MUST NOT independently style nodes.

## 13. Shell, dashboard, and overview

Shared standards govern workspace/section titles, cards, KPI tiles, overview regions, empty states, headers, action groups, spacing, readable width, and responsive reflow. Applications MUST NOT invent one-off card geometry. Dashboards remain information-dense enterprise surfaces, not marketing pages.

## 14. Menus, flyouts, panes, notifications, and help

Menus/Flyouts standardize row height, icon/check anatomy, separators, shortcuts, destructive/disabled states, hover/focus, nesting, and accessibility. Applications reuse the shared presentation.

Panes reuse the Task 10I semantic identity/runtime model. The Standard owns header, padding, divider, resize/collapse affordances, title/subtitle, contextual actions, empty and selection states.

Notification severities are Info, Success, Warning, and Critical. Content states are Initial, Loading, Empty, FilteredEmpty, Unavailable, Offline, Unauthorized, Error, Partial, and Ready. Equivalent application-local status cards are prohibited.

`HelpContextCode` remains semantic authority. DynamicUI24 owns one compact help affordance and consistent field Warning/Error/Info presentation, accessible association, and section summary where applicable.

## 15. Icons

Applications use `SemanticIcon`/icon keys. The Standard owns semantic sizing, alignment, hit geometry, and accessible meaning. Themes may evolve visual treatment without changing identity. Arbitrary application SVG geometry or per-app icon sizes are prohibited where an approved semantic icon exists.

## 16. Responsive behavior

As space decreases, implementations MUST: (1) preserve the primary workspace, (2) collapse secondary information, (3) compact actions, (4) use overflow, (5) wrap semantic groups, and (6) avoid destructive horizontal layouts. Controls MUST NOT shrink below usable/accessibility limits or rely on fixed desktop widths.

## 17. Application governance

Applications define what is presented. They MUST NOT hard-code product font families or control colors, arbitrary spacing/radius where tokens exist, independent button/editor/grid/tree systems, or bypass shared anatomy. Extension requests that introduce reusable presentation semantics are framework capability gaps and require specification review.

A shared primitive is not complete merely because it compiles, binds data, and passes automated tests. Completion also requires Design System compliance, Product UX review, and real-platform physical acceptance where applicable. Raw framework controls are not finished product UI.

## 18. Compatibility and retrofit

Task 11A preserves the accepted current Demo. Existing `Dui*` resources temporarily map to v0.16 semantic identities through a compatibility adapter. This mapping is migration infrastructure, not permission for new direct physical styling.

Retrofit proceeds by ownership: 11B Shell/Dashboard/Overview/Navigation Tree; 11C Editors/Forms; 11D DataEntry/Grid; 11E Report Runtime; 11F Authoring/Modern Workspace; 11G full compliance audit. No broad retrofit belongs to 11A.

## 19. Architecture guards and testing

Automated guards MUST establish that Standard and Theme contracts are separate; semantic typography, spacing, sizing, density, color, motion, and component roles exist; Core remains Avalonia/Actipro/DevExpress-neutral; application/sample code does not establish prohibited font or raw-color authority; semantic application layout does not use absolute pixel coordinates; and framework-owned component taxonomies cannot be replaced through application metadata.

Guards SHOULD reason about ownership and file layer rather than reject harmless constants. Raw theme values, document-native content, tests, generated output, and explicitly reviewed physical adapters are distinct from application semantic authority.

Validation includes focused Design System and architecture tests, prior editor/authorization/Modern Workspace/Report/DataEntry regressions, full solution build, diff/whitespace checks, governance hash verification, and physical Light/Dark/System review on representative platforms. Known Avalonia global-state ordering limitations remain disclosed rather than disguised.

## 20. Physical acceptance

Before v0.16 is approved, a reviewer MUST physically inspect representative system-font fallback, Vietnamese/Unicode text, Light/Dark/System switching, focus visibility, keyboard operation, density, readable forms, compact values, reduced motion where available, and preservation of runtime state. Automated success alone is insufficient.

## 21. Implementation guidance status

Files under `docs/design-system/` are non-authoritative implementation guidance. If guidance conflicts with this specification, this specification wins. Guidance should link here rather than restate normative requirements wholesale.
