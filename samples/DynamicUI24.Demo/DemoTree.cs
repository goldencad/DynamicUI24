using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

internal static class DemoTree
{
    public static TreeDefinition Create() => new("demo-tree", "DEMO", 1,
    [
        new("dashboard", "DASHBOARD", new("Tree.Dashboard"), iconKey: StandardIconKeys.Application),
        new("overview", "OVERVIEW", new("Tree.Overview"), "dashboard", StandardIconKeys.Preview, workspaceId: "dashboard-demo"),
        new("editors", "EDITORS", new("Editor Demo"), "dashboard", StandardIconKeys.Edit, 5, "editor-demo"),
        new("ui-authoring", "UI_AUTHORING", new("Developer UI Authoring"), "dashboard", StandardIconKeys.Settings, 7, "ui-authoring-demo",
            permissionRequirement: new(CapabilityCode: StandardUiCapabilities.CanOpenUiAuthoring,
                UnauthorizedBehavior: UnauthorizedBehavior.Hide)),
        new("data", "DATA", new("Tree.Data"), "dashboard", StandardIconKeys.Edit, 10),
        new("entry", "ENTRY", new("Tree.Entry"), "data", StandardIconKeys.Edit, workspaceId: "data-entry-demo"),
        new("review", "REVIEW", new("Tree.Review"), "data", StandardIconKeys.Preview, 10, "history-demo"),
        new("reports", "REPORTS", new("Tree.Reports"), "dashboard", StandardIconKeys.Preview, 20),
        new("standard-report", "STANDARD_REPORT", new("Tree.StandardReport"), "reports", StandardIconKeys.Preview, workspaceId: "report-demo"),
        new("history", "HISTORY", new("Tree.History"), "reports", StandardIconKeys.Application, 10, "history-demo"),
        new("tools", "TOOLS", new("Tree.Tools"), "dashboard", StandardIconKeys.Settings, 30),
        new("signing", "SIGNING", new("Tree.Signing"), "tools", StandardIconKeys.Edit, workspaceId: "signing-demo"),
        new("company-only", "COMPANY_ONLY", new("Tree.CompanyOnly"), "tools", StandardIconKeys.Company, 10, "calendar-demo",
            permissionRequirement: new(new PermissionCode("COMPANY.CALENDAR"), UnauthorizedBehavior: UnauthorizedBehavior.Hide)),
        new("disabled", "DISABLED", new("Tree.Disabled"), "tools", StandardIconKeys.Warning, 20, "setup-demo",
            permissionRequirement: new(new PermissionCode("SETUP.ACCESS"), UnauthorizedBehavior: UnauthorizedBehavior.Disable)),
        new("safe-unknown", "SAFE_UNKNOWN", new("Tree.SafeUnknown"), "tools", StandardIconKeys.Warning, 30, "missing-workspace"),
    ]);
}
