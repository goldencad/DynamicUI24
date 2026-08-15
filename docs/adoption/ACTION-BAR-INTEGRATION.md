# Action Bar Integration

Create immutable bar metadata per workspace, resolve it whenever workspace, Company, authorization, selection, or shared presentation state changes, and pass the result plus an execution context to `DynamicActionBarHost`. Place one host above and one below workspace content.

Register application-owned handlers through `IActionCommandRegistry`, implement `IActionRefreshService`, and provide the shared `IWorkspaceNavigationService`. Handlers contain application behavior; the bar host only dispatches. Subscribe to `CommandCompleted` to publish success, unavailable, denied, or failed state through the application's shared presentation/status surface.

Use `DisplayNameKey` catalogs for every supported culture and semantic `IconKey` values registered in `IIconRegistry`. Runtime culture/theme changes require no metadata or XAML changes. Add a new action by changing metadata and, only for a registered operation, registering its external handler.
