# Dynamic Setup designers

Specialized designers:

- [Master Catalog Designer](MASTER-CATALOG-DESIGNER.md)
- [Workspace Designer](WORKSPACE-DESIGNER.md)
- [Column Designer](COLUMN-DESIGNER.md)
- [Variable Definitions](VARIABLES.md)
- [Formula Metadata](FORMULA-METADATA.md)

Setup conforms to the reusable [DynamicUI24 design system](../../design-system/OVERVIEW.md); it does not own separate tree, action, icon, token, or split-layout rules.

`DynamicSetupTemplate` is the reusable configuration workspace for metadata-defined applications. Its internal category tree is separate from the global application tree. A category selects a management list; a definition selection resolves an editor through a registry and opens an isolated candidate buffer.

Task 9 registers production-shaped Master Catalog, Workspace, Column, Variable and Formula metadata editors. Navigation Tree, Ribbon, Action Bars, Dashboard, and Reports remain safely unavailable until later modules register their editors.

The Avalonia host uses semantic design-token brushes, `IconKey`, the shared authorization resolver, and shared top/bottom Dynamic Action Bars. Runtime culture or theme changes rebuild presentation only and do not replace the selected definition or candidate.

The internal category pane consumes the shared `DynamicTreeHost`, including localized See more / Xem thêm and Show less / Thu gọn behavior. Catalog definitions are dynamically provided and have no fixed framework count limit.

Setup places that tree and the definition/editor workspace inside the reusable `DynamicSplitNavigationHost`. Its draggable splitter adjusts the navigation width at runtime without replacing either pane. Task 8 intentionally does not persist the selected width.
