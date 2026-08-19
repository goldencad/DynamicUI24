# Shell, Dashboard, and Overview

Task 11B applies the v0.16 Standard/Theme boundary to the shared physical surfaces. `ShellHost` owns region anatomy and responsive priority; it consumes semantic surface, border, typography, icon, control-height, and spacing resources. Below 840 device-independent pixels the secondary navigation region collapses before the primary workspace is reduced. Search labels and shortcut hints compact below 720 while the search action remains available.

`DashboardPage`, `MetricCard`, and `OverviewSection` are framework-owned presentation components. A metric has stable label, primary value, optional context, and optional action anatomy. An overview is a summary/list composition using the same page, heading, panel, spacing, and typography language; it is not a separate dashboard engine. Applications own all displayed values and localized copy.

## Before / after

- **Shell before:** local 18-pixel margins, legacy surface aliases, and secondary chrome that remained wide as the window narrowed. **After:** semantic region padding/surfaces, quieter subtle boundaries, compact text hierarchy, semantic icon sizing, and navigation-first responsive collapse.
- **Dashboard before:** the Demo rendered a single locally sized title. **After:** a bounded enterprise page with shared section hierarchy and compact KPI cards.
- **Overview before:** no reusable overview composition was physically demonstrated. **After:** a shared summary/list section sits in the same page language as Dashboard.

This retrofit changes presentation only. Workspace activation, navigation dispatch, authorization, privacy, search, Quick Access, breadcrumbs, and application calculations remain outside these components.

## Physical product-UX rules

- Settings use a compact semantic navigation rail keyed by stable page code and a content region bounded by `Form.ReadableWidth`.
- A compact semantic value uses Short, Compact, Medium, or Long width. Fill is reserved for values and compositions that genuinely benefit from the available width.
- Settings pages follow page title, optional description, section, setting group, control, and optional support-text anatomy. Empty pages use the shared semantic empty-state presentation.
- Navigation rows communicate selection with surface and typography. A visible border is reserved for a meaningful region boundary or keyboard focus, not every row or option.
- Shell secondary regions compact or disappear when empty. Required blocking state remains visible, but ordinary secondary chrome must not permanently displace the workspace.
- Small option sets use compact selection controls with an immediately visible selected state; they are not rendered as full-width generic buttons.
- Automated correctness is necessary but physical product-UX acceptance on the supported desktop platform remains a release gate.
