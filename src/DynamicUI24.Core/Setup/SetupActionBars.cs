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
}

/// <summary>Shared metadata only; execution remains registered by the consuming Setup host.</summary>
public static class SetupActionBarDefinitions
{
    private static ActionDefinition Command(string code, string key, IconKey icon, int order,
        PermissionCode? permission = null, bool selection = false) => new(code, code, new(key), icon,
            ActionType.CustomRegistered, order,
            permission is null ? null : new(permission, UnauthorizedBehavior: UnauthorizedBehavior.Disable),
            selection, registeredCommandCode: code);

    public static ActionBarDefinition Top { get; } = new("setup-top", "SETUP_TOP", ActionBarPosition.Top,
    [
        Command(SetupActionCodes.New, "Setup.Action.New", StandardIconKeys.Add, 0, new("SETUP.CREATE")),
        Command(SetupActionCodes.Edit, "Setup.Action.Edit", StandardIconKeys.Edit, 10, new("SETUP.EDIT"), true),
        Command(SetupActionCodes.Clone, "Setup.Action.Clone", StandardIconKeys.Clone, 20, new("SETUP.CREATE"), true),
        Command(SetupActionCodes.Validate, "Setup.Action.Validate", StandardIconKeys.Validate, 30, new("SETUP.VALIDATE"), true),
        Command(SetupActionCodes.Publish, "Setup.Action.Publish", StandardIconKeys.Publish, 40, new("SETUP.PUBLISH"), true),
        new("refresh", SetupActionCodes.Refresh, new("Setup.Action.Refresh"), StandardIconKeys.Refresh, ActionType.Refresh, 50),
    ]);

    public static ActionBarDefinition Bottom { get; } = new("setup-bottom", "SETUP_BOTTOM", ActionBarPosition.Bottom,
    [
        Command(SetupActionCodes.Retire, "Setup.Action.Retire", StandardIconKeys.Retire, 0, new("SETUP.RETIRE"), true),
        Command(SetupActionCodes.Cancel, "Setup.Action.Cancel", StandardIconKeys.Refresh, 10, selection: true),
        Command(SetupActionCodes.Save, "Setup.Action.Save", StandardIconKeys.Commit, 20, new("SETUP.EDIT"), true),
    ]);
}
