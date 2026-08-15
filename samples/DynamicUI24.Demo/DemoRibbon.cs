using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Ribbon;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

internal static class DemoRibbon
{
    public static RibbonDefinition Create() => new("demo-ribbon", "DEMO", 1,
    [
        new RibbonTabDefinition("home", "HOME", new("Ribbon.Home"),
        [
            new RibbonGroupDefinition("workspace", "WORKSPACE", new("Ribbon.Workspace"),
            [
                new("open-report", "OPEN_REPORT", new("Ribbon.OpenReport"), StandardIconKeys.Preview,
                    RibbonCommandType.Navigate, targetWorkspaceId: "report-demo"),
                new("refresh", "REFRESH", new("Ribbon.Refresh"), StandardIconKeys.Refresh,
                    RibbonCommandType.Refresh, displayOrder: 10),
            ]),
            new RibbonGroupDefinition("actions", "ACTIONS", new("Ribbon.Actions"),
            [
                new("hello", "HELLO", new("Ribbon.Hello"), StandardIconKeys.Info,
                    RibbonCommandType.CustomRegistered, registeredCommandCode: "DEMO.HELLO"),
                new("selection", "SELECTION_ACTION", new("Ribbon.SelectionAction"), StandardIconKeys.Edit,
                    RibbonCommandType.ApplicationCommand, displayOrder: 10,
                    registeredCommandCode: "DEMO.SELECTION", requiresSelection: true,
                    permissionRequirement: new(new PermissionCode("DATA.EDIT"),
                        UnauthorizedBehavior: UnauthorizedBehavior.Disable)),
            ], displayOrder: 10),
        ]),
        new RibbonTabDefinition("data", "DATA", new("Ribbon.Data"),
        [
            new RibbonGroupDefinition("find", "FIND", new("Ribbon.Find"),
            [
                new("search", "SEARCH", new("Ribbon.Search"), StandardIconKeys.Search, RibbonCommandType.Search),
                new("filter", "FILTER", new("Ribbon.Filter"), StandardIconKeys.Filter,
                    RibbonCommandType.Filter, displayOrder: 10),
            ]),
        ], displayOrder: 10),
        new RibbonTabDefinition("reports", "REPORTS", new("Ribbon.Reports"),
        [
            new RibbonGroupDefinition("report-tools", "REPORT_TOOLS", new("Ribbon.ReportTools"),
            [
                new("preview", "PREVIEW", new("Ribbon.Preview"), StandardIconKeys.Preview,
                    RibbonCommandType.Preview),
                new("export", "EXPORT", new("Ribbon.Export"), StandardIconKeys.Export,
                    RibbonCommandType.Export, displayOrder: 10,
                    permissionRequirement: new(new PermissionCode("REPORT.VIEW"),
                        new CapabilityCode("REPORT.EXPORT_PDF_AVAILABLE"), UnauthorizedBehavior.Hide,
                        UnauthorizedBehavior.Disable)),
            ],
                permissionRequirement: new(CapabilityCode: new CapabilityCode("REPORT.EXPORT_PDF_AVAILABLE"),
                    UnauthorizedBehavior: UnauthorizedBehavior.Disable),
                contextRule: new(TemplateCode: StandardTemplateCodes.Report)),
        ], displayOrder: 20),
        new RibbonTabDefinition("tools", "TOOLS", new("Ribbon.Tools"),
        [
            new RibbonGroupDefinition("diagnostics", "DIAGNOSTICS", new("Ribbon.Diagnostics"),
            [
                new("unknown", "UNKNOWN_SAFE", new("Ribbon.Unknown"), new IconKey("UNKNOWN_RIBBON_ICON"),
                    RibbonCommandType.CustomRegistered, registeredCommandCode: "DEMO.UNKNOWN"),
            ]),
        ], displayOrder: 30),
    ]);
}

internal sealed class DemoRibbonNavigationService(
    IReadOnlyList<WorkspaceDefinition> workspaces,
    Action<WorkspaceDefinition> navigate) : IRibbonNavigationService
{
    public Task<RibbonCommandResult> NavigateAsync(string? workspaceId, TemplateCode? templateCode,
        CancellationToken cancellationToken = default)
    {
        var target = workspaceId is not null
            ? workspaces.FirstOrDefault(x => x.WorkspaceId.Equals(workspaceId, StringComparison.OrdinalIgnoreCase))
            : workspaces.FirstOrDefault(x => x.TemplateCode == templateCode);
        if (target is null)
            return Task.FromResult(RibbonCommandResult.Unavailable("RIBBON_NAVIGATION_TARGET_UNKNOWN"));
        navigate(target);
        return Task.FromResult(RibbonCommandResult.Success());
    }
}

internal sealed class DemoRibbonRefreshService(Action refresh) : IRibbonRefreshService
{
    public Task<RibbonCommandResult> RefreshAsync(RibbonCommandExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        refresh();
        return Task.FromResult(RibbonCommandResult.Success("Demo workspace refreshed."));
    }
}
