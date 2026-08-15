# Dynamic Setup foundation

Setup conforms to the reusable [DynamicUI24 design system](../../design-system/OVERVIEW.md); it does not own separate tree, action, icon, token, or split-layout rules.

`DynamicSetupTemplate` is the reusable configuration workspace for metadata-defined applications. Its internal category tree is separate from the global application tree. A category selects a management list; a definition selection resolves an editor through a registry and opens an isolated candidate buffer.

Task 8 is a foundation, not a collection of complete designers. Columns/Variables, Navigation Tree, Ribbon, Action Bars, Dashboard, and Reports deliberately resolve to a localized unavailable state until later modules register their editors.

The Avalonia host uses semantic design-token brushes, `IconKey`, the shared authorization resolver, and shared top/bottom Dynamic Action Bars. Runtime culture or theme changes rebuild presentation only and do not replace the selected definition or candidate.

The internal category pane consumes the shared `DynamicTreeHost`. Catalog children initially show five items in the Demo, expand five at a time through localized See more / Xem thêm, and can return to the initial window with Show less / Thu gọn. This limit is presentation paging, not a metadata or catalog-count limit.

Setup places that tree and the definition/editor workspace inside the reusable `DynamicSplitNavigationHost`. Its draggable splitter adjusts the navigation width at runtime without replacing either pane. Task 8 intentionally does not persist the selected width.
