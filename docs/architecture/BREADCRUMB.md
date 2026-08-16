# Breadcrumb Architecture

Breadcrumb answers “Where am I?” from the same navigation/workspace state used by Tree, Ribbon, Search and Quick Access. `BreadcrumbItem` contains stable metadata identity and an optional existing workspace navigation target. `BreadcrumbNavigator` activates ancestors only through `IWorkspaceNavigationService`; it never instantiates views.

The final item is the sole current item. Narrow layouts keep root and current visible and place middle ancestors in the existing flyout/menu surface. Search filtering never changes the active path. Company/culture changes re-resolve path labels. Dynamic labels must use the shared privacy pipeline; metadata labels are preferred.
