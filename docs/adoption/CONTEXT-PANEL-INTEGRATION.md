# Context Panel Integration

1. Publish one validated `ContextPanelDefinition` and keep its `ContextPanelState` separate.
2. Register application-neutral `IContextPanelProvider` instances by code.
3. Translate workspace selection into `ContextSelection` stable keys; do not pass controls or row objects.
4. On selection/company/workspace changes call `ContextPanelCoordinator.ResolveAsync` and render only its published result.
5. Project every item with `ContextItemPresenter` and the current P1 privacy/reveal and authorization context.
6. Put one `ContextPanelHost` in `ShellHost.ContextPanelContent`; bind open/width/section to an `IContextPanelPreferenceStore` if persistence is desired.

For 100K grids, resolve one RowKey directly. Never enumerate all rows, retain virtualized containers or materialize offscreen data.
