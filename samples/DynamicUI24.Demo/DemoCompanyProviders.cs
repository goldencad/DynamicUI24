using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;

namespace DynamicUI24.Demo;

internal static class DemoCompanyData
{
    public static CompanyId CompanyAId { get; } = new("demo-company-a");
    public static CompanyId CompanyBId { get; } = new("demo-company-b");
    public static CompanyId CompanyCId { get; } = new("demo-company-c");
    public static UserContext User { get; } = new(new UserId("demo-user"));

    public static IReadOnlyList<CompanyDescriptor> Companies { get; } =
    [
        new(CompanyAId, "DEMO-A", "Northwind Demo Studio", "DEMO-TAX-A"),
        new(CompanyBId, "DEMO-B", "Contoso Demo Works", "DEMO-TAX-B"),
        new(CompanyCId, "DEMO-C", "Fabrikam Restricted Demo", "DEMO-TAX-C"),
    ];
}

internal sealed class DemoCompanyProfileProvider : ICompanyProfileProvider
{
    private static readonly IReadOnlyDictionary<CompanyId, CompanyProfile> Profiles =
        new Dictionary<CompanyId, CompanyProfile>
        {
            [DemoCompanyData.CompanyAId] = new(
                DemoCompanyData.CompanyAId,
                "Northwind Demo Studio LLC",
                "Northwind Demo",
                "DEMO-TAX-A",
                "101 Sample Avenue",
                "+84 000 100 100",
                "hello-a@example.invalid",
                additionalFields:
                [
                    new("Source", "In-memory demo provider"),
                    new("Region", "Demo North"),
                ]),
            [DemoCompanyData.CompanyBId] = new(
                DemoCompanyData.CompanyBId,
                "Contoso Demo Works Ltd.",
                "Contoso Demo",
                "DEMO-TAX-B",
                "202 Example Boulevard",
                "+84 000 200 200",
                "hello-b@example.invalid",
                additionalFields:
                [
                    new("Source", "In-memory demo provider"),
                    new("Region", "Demo South"),
                ]),
            [DemoCompanyData.CompanyCId] = new(
                DemoCompanyData.CompanyCId,
                "Fabrikam Restricted Demo Inc.",
                "Fabrikam Demo",
                "DEMO-TAX-C",
                "303 Placeholder Road",
                "+84 000 300 300",
                "hello-c@example.invalid",
                additionalFields:
                [
                    new("Source", "In-memory demo provider"),
                    new("Access", "Restricted demonstration"),
                ]),
        };

    public async Task<CompanyProfileResult> GetProfileAsync(
        CompanyId companyId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(25, cancellationToken);
        return Profiles.TryGetValue(companyId, out var profile)
            ? CompanyProfileResult.Ready(profile)
            : CompanyProfileResult.NotFound("DUI-DEMO-PROFILE-NOT-FOUND");
    }
}

internal sealed class DemoAuthorizationPresentationProvider : IAuthorizationPresentationProvider
{
    private static readonly PermissionCode DataView = new("DATA.VIEW");
    private static readonly PermissionCode DataEdit = new("DATA.EDIT");
    private static readonly PermissionCode ReportView = new("REPORT.VIEW");
    private static readonly CapabilityCode ReportExport = new("REPORT.EXPORT_PDF_AVAILABLE");
    private static readonly CapabilityCode Editing = new("DATA.EDITING_AVAILABLE");
    private long revision;

    public Task<EffectiveAuthorizationContext> GetEffectiveContextAsync(
        UserContext userContext,
        CompanyId companyId,
        CancellationToken cancellationToken = default) =>
        LoadAsync(userContext, companyId, cancellationToken);

    public Task<EffectiveAuthorizationContext> RefreshAsync(
        UserContext userContext,
        CompanyId companyId,
        CancellationToken cancellationToken = default) =>
        LoadAsync(userContext, companyId, cancellationToken);

    private async Task<EffectiveAuthorizationContext> LoadAsync(
        UserContext userContext,
        CompanyId companyId,
        CancellationToken cancellationToken)
    {
        await Task.Delay(companyId == DemoCompanyData.CompanyBId ? 80 : 35, cancellationToken);
        var currentRevision = $"demo-r{Interlocked.Increment(ref revision)}";
        if (companyId == DemoCompanyData.CompanyAId)
        {
            return new(userContext.UserId, companyId,
                [DataView, DataEdit, ReportView], [ReportExport, Editing], currentRevision);
        }

        if (companyId == DemoCompanyData.CompanyBId)
        {
            return new(userContext.UserId, companyId,
                [DataView, ReportView], [ReportExport], currentRevision);
        }

        return EffectiveAuthorizationContext.Unavailable(
            userContext.UserId, companyId, currentRevision, "DUI-DEMO-AUTH-UNAVAILABLE");
    }
}
