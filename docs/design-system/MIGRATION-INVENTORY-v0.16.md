# v0.16 presentation migration inventory

Snapshot basis: Task 11A baseline `87ae7666e6629af08280b05f1ce843c89f63ea0f`. Counts are line matches across `src/**/*.cs|axaml` and `samples/**/*.cs|axaml`; they are discovery signals, not automatic violations.

| Area | Matches | Initial classification |
|---|---:|---|
| FontFamily | 5 | REVIEW; keep system/icon mappings, migrate application UI authority |
| FontSize | 39 | MIGRATE TO TOKEN during owning retrofit |
| FontWeight | 16 | REVIEW; map permitted hierarchy in theme |
| Raw 6/8-digit colors | 75 | KEEP AS THEME IMPLEMENTATION in `DesignTokens.axaml`; migrate/review other UI use |
| Margin/Padding | 67 | MIGRATE TO TOKEN where semantic alias exists; REVIEW structural zeroes |
| CornerRadius | 7 | KEEP in theme recipe or MIGRATE TO TOKEN |
| Height/Width | 386 | REVIEW; preserve layout constraints/virtualization mechanics, migrate control geometry |
| RowHeight | 46 | REVIEW; preserve data/runtime measurements, migrate presentation defaults |
| IconSize | 10 | MIGRATE TO TOKEN except registry geometry mechanics |
| Style/Styles | 49 | REVIEW; keep framework-owned shared styles, remove application-local equivalents |

Concentration: 20 matching files in `DynamicUI24.Avalonia`, 8 in the Demo, 7 in Core, 2 in Shared, and 1 extension file for the combined presentation query. Core occurrences require ownership review because many are semantic grid/layout mechanics rather than theme values.

Task 11A performs no mass rewrite. Owners must classify each occurrence as **KEEP AS THEME IMPLEMENTATION**, **MIGRATE TO TOKEN**, **REVIEW**, or **REMOVE** during 11B–11G, with physical regression evidence.

Phase ownership: 11B owns Shell/Dashboard/Overview/Navigation Tree; 11C owns Universal Editor/Forms; 11D owns DataEntry/Grid; 11E owns Report Runtime; 11F owns Developer Authoring/Modern Workspace and physical Theme configuration; 11G owns the full compliance audit.

## Task 11D physical debt captured during 11B Round 2

- **Row Height interaction:** open DataEntry, activate the `Row Height … ⌄` sizing button, then attempt to select a percentage/custom value. `BuildSizingBar` creates a submenu `MenuItem` through `BuildRowHeightMenu` and reuses its child `Items` as an independent `ContextMenu.ItemsSource`. Those child menu controls retain submenu ownership/lifecycle assumptions, so pointer selection is unreliable in the standalone popup. The path predates 11B in commit `f7b2b2d`; no 11B Shell overlay or hit-testing code covers the control while settings/search overlays are closed. 11D should construct one popup-owned item collection directly and add a physical pointer/keyboard regression.
- Review sheet-action crowding, inter-button spacing, sheet-tab hierarchy, Row Height placement, viewport actions, Grid/header density, and command overflow opportunities as one DataEntry presentation pass.

### Mandatory Task 11D activation-performance gate

Measure DataEntry and Report in the same release build, Mac, company, authorization context, and dataset over at least five cold workspace activations. Capture navigation command received, workspace constructed, first provider request, first visible frame, first materialized rows, logical Ready, and spinner dismissed as separate timestamps. Task 11D is not physically acceptable if it reports logical Ready alone.

Provisional acceptance targets, fixed before 11D implementation:

- median navigation-to-first-visible-frame at most 500 ms and no more than 250 ms slower than Report;
- first visible/materialized rows at most 750 ms;
- logical Ready and spinner dismissal at most 1,000 ms, with dismissal no more than one frame after Ready;
- one bounded initial provider request unless a measured generation/company transition requires another;
- no repeated synthetic 100K scan or full authorization/context rematerialization on the initial frame;
- p95 across the measured runs no greater than twice the median.

Optimizations must retain generation safety, Company safety, authorization/P1, bounded virtualization, and lazy construction. Instrument provider requests, refresh reasons, materialization counts, and compositor first-frame time independently.
