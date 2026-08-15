# Dynamic Setup foundation

`DynamicSetupTemplate` is the reusable configuration workspace for metadata-defined applications. Its internal category tree is separate from the global application tree. A category selects a management list; a definition selection resolves an editor through a registry and opens an isolated candidate buffer.

Task 8 is a foundation, not a collection of complete designers. Columns/Variables, Navigation Tree, Ribbon, Action Bars, Dashboard, and Reports deliberately resolve to a localized unavailable state until later modules register their editors.

The Avalonia host uses semantic design-token brushes, `IconKey`, the shared authorization resolver, and shared top/bottom Dynamic Action Bars. Runtime culture or theme changes rebuild presentation only and do not replace the selected definition or candidate.
