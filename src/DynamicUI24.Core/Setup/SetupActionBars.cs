using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Setup;

public static class SetupActionCodes
{
    public const string New = "SETUP.NEW";
    public const string Edit = "SETUP.EDIT";
    public const string Clone = "SETUP.CLONE";
    public const string Validate = "SETUP.VALIDATE";
    public const string Publish = "SETUP.PUBLISH";
    public const string Refresh = "SETUP.REFRESH";
    public const string Retire = "SETUP.RETIRE";
    public const string Cancel = "SETUP.CANCEL";
    public const string Save = "SETUP.SAVE";
    public const string ToggleDetails = "SETUP.TOGGLE_DETAILS";
}

/// <summary>Shared metadata only; execution remains registered by the consuming Setup host.</summary>
public static class SetupActionBarDefinitions
{
    private static ActionDefinition Command(string code, string key, IconKey icon, int order,
        PermissionCode? permission = null, bool selection = false,
        ActionButtonVariant variant = ActionButtonVariant.Button,
        IEnumerable<ActionMenuItemDefinition>? menuItems = null, bool isChecked = false,
        ActionControlGeometry? geometry = null) => new(code, code, new(key), icon,
            ActionType.CustomRegistered, order,
            permission is null ? null : new(permission, UnauthorizedBehavior: UnauthorizedBehavior.Disable),
            selection, registeredCommandCode: code, buttonVariant: variant, menuItems: menuItems, isChecked: isChecked,
            geometry: geometry);

    public static ActionBarDefinition Top { get; } = new("setup-top", "SETUP_TOP", ActionBarPosition.Top,
    [
        Command(SetupActionCodes.New, "Setup.Action.New", StandardIconKeys.Add, 0, new("SETUP.CREATE"),
            variant: ActionButtonVariant.SplitButton, menuItems:
            [
                new("new-standard", "NEW_STANDARD", new("Setup.Menu.NewStandard"), StandardIconKeys.Add,
                    SetupActionCodes.New, shortcutDisplay: "⌘N"),
                new("separator", "CREATE_SEPARATOR", new("Setup.Menu.NewStandard"), displayOrder: 10,
                    kind: ActionMenuItemKind.Separator),
                new("advanced", "ADVANCED", new("Setup.Menu.Advanced"), StandardIconKeys.Settings,
                    displayOrder: 20, children:
                    [
                        new("unknown", "UNKNOWN_SAFE", new("Setup.Menu.Unknown"), new IconKey("UNKNOWN_MENU_ICON"),
                            "SETUP.UNKNOWN", groupCode: "ADVANCED"),
                        new("disabled", "ADMIN_ONLY", new("Setup.Menu.AdminOnly"), StandardIconKeys.Warning,
                            "SETUP.ADMIN", 10, new(new PermissionCode("SETUP.ADMIN"), UnauthorizedBehavior: UnauthorizedBehavior.Disable),
                            groupCode: "ADVANCED"),
                        new("hidden", "HIDDEN_ITEM", new("Setup.Menu.Hidden"), StandardIconKeys.Warning,
                            "SETUP.HIDDEN", 20, new(new PermissionCode("SETUP.HIDDEN"), UnauthorizedBehavior: UnauthorizedBehavior.Hide),
                            groupCode: "ADVANCED"),
                    ]),
            ], geometry: new(ActionControlSizePreset.Large, minWidth: 128, maxWidth: 240,
                typographyToken: ActionTypographyToken.Body, iconSize: 20, padding: new(14, 7), gap: 8)),
        Command(SetupActionCodes.Edit, "Setup.Action.Edit", StandardIconKeys.Edit, 10, new("SETUP.EDIT"), true),
        Command(SetupActionCodes.Clone, "Setup.Action.Clone", StandardIconKeys.Clone, 20, new("SETUP.CREATE"), true,
            ActionButtonVariant.DropdownButton,
            [new("clone", "CLONE_SELECTED", new("Setup.Menu.CloneSelected"), StandardIconKeys.Clone, SetupActionCodes.Clone)],
            geometry: new(ActionControlSizePreset.Small, iconPosition: ActionIconPosition.Right)),
        Command(SetupActionCodes.Validate, "Setup.Action.Validate", StandardIconKeys.Validate, 30, new("SETUP.VALIDATE"), true,
            ActionButtonVariant.IconButton, geometry: new(ActionControlSizePreset.Medium, width: 36, iconSize: 18,
                iconPosition: ActionIconPosition.IconOnly)),
        Command(SetupActionCodes.Publish, "Setup.Action.Publish", StandardIconKeys.Publish, 40, new("SETUP.PUBLISH"), true),
        new("refresh", SetupActionCodes.Refresh, new("Setup.Action.Refresh"), StandardIconKeys.Refresh, ActionType.Refresh, 50,
            buttonVariant: ActionButtonVariant.IconButton),
    ]);

    public static ActionBarDefinition Bottom { get; } = new("setup-bottom", "SETUP_BOTTOM", ActionBarPosition.Bottom,
    [
        Command(SetupActionCodes.Retire, "Setup.Action.Retire", StandardIconKeys.Retire, 0, new("SETUP.RETIRE"), true),
        Command(SetupActionCodes.Cancel, "Setup.Action.Cancel", StandardIconKeys.Refresh, 10, selection: true),
        Command(SetupActionCodes.Save, "Setup.Action.Save", StandardIconKeys.Commit, 20, new("SETUP.EDIT"), true),
        Command(SetupActionCodes.ToggleDetails, "Setup.Action.ToggleDetails", StandardIconKeys.More, 30,
            variant: ActionButtonVariant.ToggleButton, geometry: new(ActionControlSizePreset.Xs,
                typographyToken: ActionTypographyToken.Caption, iconSize: 14, gap: 4)),
    ]);
}
