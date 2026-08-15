# Action Bar Metadata Example

```csharp
var top = new ActionBarDefinition("orders-top", "ORDERS_TOP", ActionBarPosition.Top,
[
    new ActionDefinition("refresh", "REFRESH", new("Action.Refresh"),
        StandardIconKeys.Refresh, ActionType.Refresh),
    new ActionDefinition("edit", "EDIT", new("Action.Edit"),
        StandardIconKeys.Edit, ActionType.Edit, displayOrder: 10,
        permissionRequirement: new(new PermissionCode("ORDER.EDIT"),
            UnauthorizedBehavior: UnauthorizedBehavior.Disable),
        requiresSelection: true, minSelection: 1, maxSelection: 1),
]);

var bottom = new ActionBarDefinition("orders-bottom", "ORDERS_BOTTOM", ActionBarPosition.Bottom,
[
    new ActionDefinition("custom", "CUSTOM", new("Action.Custom"),
        StandardIconKeys.Info, ActionType.CustomRegistered,
        registeredCommandCode: "ORDERS.CUSTOM",
        buttonVariant: ActionButtonVariant.IconButton,
        geometry: new(ActionControlSizePreset.Small, width: 32, iconSize: 14,
            iconPosition: ActionIconPosition.IconOnly)),
]);
```

No XAML changes are required. Resolve these definitions against the current context and show them in the shared top/bottom hosts. The metadata describes presentation and dispatch only; it must not contain business logic.

See the shared [button standard](../design-system/BUTTONS.md) for dropdown/split menus, all five presets, bounded overrides, typography, padding/gap, and global scaling behavior.
