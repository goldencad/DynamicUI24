# Grid header

The active grid header presents a metadata/localization-driven title and optional subtitle above the reused DataEntry grid. Workspace and grid headers remain separate. Title, subtitle, tooltip and automation text use the same P1 resolution; restricted raw text must not be cached or exposed.

Column width uses the compact header dropdown and semantic percentage commands keyed by `VariableCode`. Grid-wide row height uses the corresponding percentage menu. These deterministic menus are the supported cross-platform sizing interaction; DataEntry does not expose column splitters, per-row grips, resize cursors, or physical drag-resize gestures.
