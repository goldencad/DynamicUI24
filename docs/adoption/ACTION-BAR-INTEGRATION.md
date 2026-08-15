# Action Bar Integration

Configure variants and bounded geometry according to the shared [button standard](../design-system/BUTTONS.md), and register sources according to the [icon standard](../design-system/ICONS.md).

Create immutable bar metadata per workspace, resolve it whenever workspace, Company, authorization, selection, or shared presentation state changes, and pass the result plus an execution context to `DynamicActionBarHost`. Place one host above and one below workspace content.

Register application-owned handlers through `IActionCommandRegistry`, implement `IActionRefreshService`, and provide the shared `IWorkspaceNavigationService`. Handlers contain application behavior; the bar host only dispatches. Subscribe to `CommandCompleted` to publish success, unavailable, denied, or failed state through the application's shared presentation/status surface.

Use `DisplayNameKey` catalogs for every supported culture and semantic `IconKey` values registered in `IIconRegistry`. Runtime culture/theme changes require no metadata or XAML changes. Add a new action by changing metadata and, only for a registered operation, registering its external handler.

Consumer applications should normally customize DynamicUI24 through metadata, tokens, registries, providers, and extension points rather than modifying shared framework controls.
