using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

internal static class DemoActionBars
{
    public static WorkspaceActionBarDefinitions Create() => new(
    [
        Pair("dashboard-demo",
            Top("overview-top",
                Action("refresh", "REFRESH", "ActionBar.Refresh", StandardIconKeys.Refresh, ActionType.Refresh),
                Action("preview", "PREVIEW", "ActionBar.Preview", StandardIconKeys.Preview, ActionType.Preview, 10)),
            Bottom("overview-bottom",
                Action("custom", "CUSTOM", "ActionBar.Custom", StandardIconKeys.Info,
                    ActionType.CustomRegistered, registeredCommandCode: "DEMO.ACTION.CUSTOM"),
                Action("data", "OPEN_DATA", "ActionBar.OpenData", StandardIconKeys.Edit,
                    ActionType.Navigate, 10, targetWorkspaceId: "data-entry-demo"))),
        Pair("data-entry-demo",
            Top("data-top",
                Action("add", "ADD", "ActionBar.Add", StandardIconKeys.Add, ActionType.Add),
                Action("edit", "EDIT", "ActionBar.Edit", StandardIconKeys.Edit, ActionType.Edit, 10,
                    requirement: new(new PermissionCode("DATA.EDIT"), UnauthorizedBehavior: UnauthorizedBehavior.Disable),
                    requiresSelection: true, minSelection: 1, maxSelection: 1),
                Action("refresh", "REFRESH", "ActionBar.Refresh", StandardIconKeys.Refresh, ActionType.Refresh, 20),
                Action("filter", "FILTER", "ActionBar.Filter", StandardIconKeys.Filter, ActionType.Filter, 30),
                Action("search", "SEARCH", "ActionBar.Search", StandardIconKeys.Search, ActionType.Search, 40),
                Action("custom", "CUSTOM", "ActionBar.Custom", StandardIconKeys.Info, ActionType.CustomRegistered, 50,
                    registeredCommandCode: "DEMO.ACTION.CUSTOM")),
            Bottom("data-bottom",
                Action("export", "EXPORT", "ActionBar.Export", StandardIconKeys.Export, ActionType.Export))),
        Pair("signing-demo",
            Top("signing-top",
                Action("preview", "PREVIEW", "ActionBar.Preview", StandardIconKeys.Preview, ActionType.Preview),
                Action("custom", "CUSTOM_REGISTERED", "ActionBar.Custom", StandardIconKeys.Info,
                    ActionType.CustomRegistered, 10, registeredCommandCode: "DEMO.ACTION.CUSTOM"),
                Action("gated", "PERMISSION_GATED", "ActionBar.PermissionGated", StandardIconKeys.Success,
                    ActionType.CustomRegistered, 20,
                    requirement: new(new PermissionCode("DATA.EDIT"), UnauthorizedBehavior: UnauthorizedBehavior.Disable),
                    registeredCommandCode: "DEMO.ACTION.GATED")),
            Bottom("signing-bottom",
                Action("unknown", "UNKNOWN_COMMAND", "ActionBar.Unknown", new IconKey("UNKNOWN_ACTION_ICON"),
                    ActionType.CustomRegistered, registeredCommandCode: "DEMO.ACTION.UNKNOWN"))),
        Pair("report-demo",
            Top("report-top",
                Action("overview", "OPEN_OVERVIEW", "ActionBar.OpenOverview", StandardIconKeys.Application,
                    ActionType.Navigate, targetWorkspaceId: "dashboard-demo"),
                Action("refresh", "REFRESH", "ActionBar.Refresh", StandardIconKeys.Refresh, ActionType.Refresh, 10)),
            Bottom("report-bottom", Action("custom", "CUSTOM", "ActionBar.Custom", StandardIconKeys.Info,
                ActionType.CustomRegistered, registeredCommandCode: "DEMO.ACTION.CUSTOM"))),
    ]);

    private static KeyValuePair<string, IEnumerable<ActionBarDefinition>> Pair(string workspaceId,
        params ActionBarDefinition[] bars) => new(workspaceId, bars);

    private static ActionBarDefinition Top(string id, params ActionDefinition[] actions) =>
        new(id, id, ActionBarPosition.Top, actions);

    private static ActionBarDefinition Bottom(string id, params ActionDefinition[] actions) =>
        new(id, id, ActionBarPosition.Bottom, actions);

    private static ActionDefinition Action(string id, string code, string label, IconKey icon, ActionType type,
        int order = 0, PresentationRequirement? requirement = null, bool requiresSelection = false,
        int? minSelection = null, int? maxSelection = null, string? targetWorkspaceId = null,
        string? registeredCommandCode = null) => new(id, code, new(label), icon, type, order, requirement,
            requiresSelection, minSelection, maxSelection, targetWorkspaceId: targetWorkspaceId,
            registeredCommandCode: registeredCommandCode);
}
