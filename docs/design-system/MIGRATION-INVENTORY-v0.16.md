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
