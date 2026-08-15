# Dynamic Ribbon

DynamicUI24 renders a real Actipro `Ribbon` from metadata. The reusable host contains no application tab names and does not construct workspaces or execute domain operations. Its responsibilities are presentation, navigation, and dispatch only.

The flow is `RibbonDefinition → validation → DynamicRibbonResolver → ResolvedRibbon → DynamicRibbonHost`. The host creates Actipro tab, group, and button controls at runtime. The Application Menu remains a separate top-left shell region.

Actipro Avalonia Pro `25.2.0` is referenced because it is compatible with Avalonia `11.3.x`. It is commercial software and requires an appropriate paid or evaluation license. This repository embeds no Actipro license key, so its current repository status must be treated as evaluation/unlicensed until a consuming organization supplies its own valid license.

Language changes rebuild labels while preserving the selected tab code where it remains visible. Theme changes use the existing Avalonia theme state and do not replace definitions or workspace state.
