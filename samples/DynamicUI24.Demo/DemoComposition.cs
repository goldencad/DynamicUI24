using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Template.Dashboard;
using DynamicUI24.Template.DataEntry;
using DynamicUI24.Template.HistoryDocument;
using DynamicUI24.Template.Report;
using DynamicUI24.Template.Setup;
using DynamicUI24.Template.Signing;

namespace DynamicUI24.Demo;

internal sealed record DemoComposition(
    TemplateRegistry Registry,
    IReadOnlyList<WorkspaceDefinition> Workspaces,
    ICompanyContextProvider CompanyContext,
    CompanyScopeCoordinator CompanyScope)
{
    public static DemoComposition Create()
    {
        var registry = new TemplateRegistry();
        EnsureRegistered(SetupTemplateRegistration.Register(registry));
        EnsureRegistered(DataEntryTemplateRegistration.Register(registry));
        EnsureRegistered(ReportTemplateRegistration.Register(registry));
        EnsureRegistered(HistoryDocumentTemplateRegistration.Register(registry));
        EnsureRegistered(DashboardTemplateRegistration.Register(registry));
        EnsureRegistered(SigningTemplateRegistration.Register(registry));
        EnsureRegistered(registry.Register(new CalendarTemplate()));

        WorkspaceDefinition[] workspaces =
        [
            new("setup-demo", "Setup Demo", StandardTemplateCodes.Setup),
            new("data-entry-demo", "Data Entry Demo", StandardTemplateCodes.DataEntry),
            new("report-demo", "Report Demo", StandardTemplateCodes.Report),
            new("history-demo", "History Document Demo", StandardTemplateCodes.HistoryDocument),
            new("dashboard-demo", "Dashboard Demo", StandardTemplateCodes.Dashboard),
            new("editor-demo", "Editor Demo", StandardTemplateCodes.Dashboard),
            new("ui-authoring-demo", "Developer UI Authoring", StandardTemplateCodes.Dashboard),
            new("modern-workspace-demo", "Modern Workspace Demo", StandardTemplateCodes.Dashboard),
            new("signing-demo", "Signing Demo", StandardTemplateCodes.Signing),
            new("calendar-demo", "Calendar Extension Demo", CalendarTemplate.Code),
            new("unknown-demo", "Unknown Template (safe failure)", new TemplateCode("UNKNOWN")),
        ];

        var companyContext = new CompanyContextProvider(DemoCompanyData.Companies, DemoCompanyData.CompanyAId);
        var companyScope = new CompanyScopeCoordinator(
            companyContext,
            new DemoCompanyProfileProvider(),
            new DemoAuthorizationPresentationProvider(),
            DemoCompanyData.User);
        return new(registry, workspaces, companyContext, companyScope);
    }

    private static void EnsureRegistered(TemplateRegistrationResult result)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Message);
        }
    }
}

internal sealed class CalendarTemplate : DynamicTemplateBase
{
    public static TemplateCode Code { get; } = new("CALENDAR");
    public override TemplateCode TemplateCode => Code;
    public override string ModuleName => typeof(CalendarTemplate).Assembly.GetName().Name!;
    public override IReadOnlyCollection<TemplateCapability> SupportedCapabilities { get; } =
        [new("TIME_FILTER")];
}
