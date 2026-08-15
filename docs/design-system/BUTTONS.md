# Buttons and Dynamic Action Bars

## Metadata contract

`ActionDefinition.ButtonVariant` selects `Button`, `DropdownButton`, `SplitButton`, `IconButton`, or `ToggleButton`. Both Top and Bottom `DynamicActionBarHost` instances use the same renderer. `Button` and `IconButton` dispatch their action; a toggle dispatches without owning business state. A dropdown opens its menu. A split button's main segment dispatches its configured default registered command while the chevron opens the menu.

Menu items use localization keys and semantic `IconKey` values. They may declare deterministic order, a permission/capability requirement, shortcut display, group, separator, and one child level. Two menu levels are the maximum. Hidden items are removed, disabled items remain visible, and missing commands fail safely. Down or F4 opens, Up/Down navigates enabled commands, Escape closes, and activation uses the registered command dispatcher.

## Geometry

`ActionControlGeometry` is optional and has safe Medium defaults. `SizePreset` accepts `Xs`, `Small`, `Medium`, `Large`, or `Xl`. Applications may provide bounded `Width`, `MinWidth`, `MaxWidth`, and `Height`, plus `TypographyToken`, `IconSize`, `IconPosition`, `Padding`, and `Gap`. Positions are Left, Right, Top, Bottom, and IconOnly. Invalid, non-finite, inverted, or out-of-range dimensions are rejected when metadata is created.

```csharp
new ActionDefinition(
    "publish", "PUBLISH", new("Action.Publish"), StandardIconKeys.Publish,
    ActionType.CustomRegistered,
    registeredCommandCode: "PUBLISH",
    buttonVariant: ActionButtonVariant.SplitButton,
    menuItems: publishMenu,
    geometry: new(ActionControlSizePreset.Large,
        minWidth: 128, maxWidth: 240,
        typographyToken: ActionTypographyToken.Body,
        iconSize: 20, iconPosition: ActionIconPosition.Left,
        padding: new ActionThickness(14, 7), gap: 8));
```

Preset values are semantic control-size tokens, not permission or business semantics. Explicit dimensions are logical, pre-scale units. `IAppearancePreferenceService.UiScale` multiplies all geometry; its font-size preference also multiplies typography. Thus global UI Scale and Font Size remain authoritative over metadata and token defaults.

## Extension rule

Add variants or geometry fields as optional metadata and implement them once in the shared host. Preserve the meaning of existing presets, `IconKey`, registered-command dispatch, accessibility behavior, and safe fallbacks. Do not create Setup-only action controls or encode domain operations in the renderer.
