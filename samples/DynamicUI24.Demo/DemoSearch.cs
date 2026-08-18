using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Search;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

internal sealed class DemoSearchProvider(string code, IReadOnlyList<SearchResult> results) : ISearchProvider
{
    public string ProviderCode { get; } = code;
    public IReadOnlySet<SearchResultKind> SupportedKinds { get; } = results.Select(x => x.ResultKind).ToHashSet();
    public Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(results); }
}

internal sealed class ThrowingSearchProvider : ISearchProvider
{
    public string ProviderCode => "DEMO_FAILURE";
    public IReadOnlySet<SearchResultKind> SupportedKinds { get; } = new HashSet<SearchResultKind> { SearchResultKind.Record };
    public Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Intentional isolated demo failure.");
}

internal static class DemoSearch
{
    public static IReadOnlyList<ISearchProvider> CreateProviders(IReadOnlyList<WorkspaceDefinition> workspaces,
        IQuickAccessStore quickAccess, out DemoQuickAccessResolver quickResolver)
    {
        var workspaceResults = workspaces.Select((x, i) => new SearchResult(x.WorkspaceId, SearchResultKind.Workspace,
            "WORKSPACES", x.DisplayName, x.TemplateCode.Value, iconKey: StandardIconKeys.Application, providerRank: i,
            workspaceId: x.WorkspaceId, navigationTarget: x.WorkspaceId, deduplicationKey: $"workspace:{x.WorkspaceId}",
            presentationRequirement: x.WorkspaceId.Equals("ui-authoring-demo", StringComparison.OrdinalIgnoreCase)
                ? new(CapabilityCode: StandardUiCapabilities.CanOpenUiAuthoring, UnauthorizedBehavior: UnauthorizedBehavior.Hide)
                : null,
            canFavorite: true, canPin: true, canRecordRecent: true)).ToArray();
        SearchResult[] commands =
        [
            new("hello", SearchResultKind.Command, "COMMANDS", "Say hello", "Registered safe command",
                iconKey: StandardIconKeys.Info, registeredCommandCode: "DEMO.HELLO", canFavorite: true, canPin: true,
                canRecordRecent: true),
        ];
        var treeResults = DemoTree.Create().Nodes.Select((x, i) => new SearchResult(x.NodeId,
            SearchResultKind.TreeNode, "NAVIGATION", x.DisplayNameKey.Value, x.NodeCode,
            iconKey: x.IconKey, providerRank: i, workspaceId: x.WorkspaceId,
            navigationTarget: x.WorkspaceId, presentationRequirement: x.PermissionRequirement,
            deduplicationKey: x.WorkspaceId is null ? $"tree:{x.NodeId}" : $"workspace:{x.WorkspaceId}",
            canFavorite: x.WorkspaceId is not null, canPin: x.WorkspaceId is not null,
            canRecordRecent: x.WorkspaceId is not null)).ToArray();
        SearchResult[] settings =
        [
            new("privacy-settings", SearchResultKind.Setting, "SETTINGS", "Privacy settings",
                "Application menu setting", iconKey: StandardIconKeys.Settings, navigationTarget: "PRIVACY_SETTINGS"),
        ];
        SearchResult[] records =
        [
            new("sample-record", SearchResultKind.Record, "NEUTRAL_RECORDS", "Example record", "Reference 1001",
                iconKey: StandardIconKeys.Preview, workspaceId: "data-entry-demo", canRecordRecent: true),
            new("restricted-record", SearchResultKind.Record, "NEUTRAL_RECORDS", "Restricted example",
                "PRIVATE_REFERENCE = 123456789", iconKey: StandardIconKeys.Privacy,
                workspaceId: "data-entry-demo", privacyMetadata: new(Sensitivity.Restricted, PrivacyPresentation.Mask),
                canRecordRecent: true),
            new("company-a-record", SearchResultKind.Record, "NEUTRAL_RECORDS", "Company A example",
                "Company-scoped", workspaceId: "data-entry-demo", companyScope: CompanyScopeKind.CompanyScoped,
                companyId: DemoCompanyData.CompanyAId.Value),
            new("unknown-target", SearchResultKind.Record, "NEUTRAL_RECORDS", "Unavailable example",
                "Safe unknown target", workspaceId: "missing-workspace"),
        ];
        var all = workspaceResults.Concat(treeResults).Concat(commands).Concat(settings).Concat(records).ToArray();
        quickResolver = new(all);
        return
        [
            new DemoSearchProvider("WORKSPACES", workspaceResults),
            new DemoSearchProvider("NAVIGATION", treeResults),
            new DemoSearchProvider("COMMANDS", commands),
            new DemoSearchProvider("SETTINGS", settings),
            new DemoSearchProvider("NEUTRAL_RECORDS", records),
            new QuickAccessSearchProvider(quickAccess, quickResolver),
            new ThrowingSearchProvider(),
        ];
    }
}

internal sealed class DemoQuickAccessResolver(IEnumerable<SearchResult> results) : IQuickAccessResolver
{
    private readonly IReadOnlyDictionary<string, SearchResult> results = results
        .GroupBy(x => x.SemanticIdentity, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(x => x.Key, x => x.OrderBy(r => r.ResultKind == SearchResultKind.Workspace ? 0 : 1)
            .ThenBy(r => r.ProviderRank).First(), StringComparer.OrdinalIgnoreCase);
    public SearchResult? Resolve(QuickAccessEntry entry, SearchQuery context) =>
        results.TryGetValue(entry.EntryId, out var result) ? result : null;
}

internal sealed class DemoSettingNavigationService(Action<string> show) : ISettingNavigationService
{
    public Task<bool> NavigateAsync(string target, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); show(target); return Task.FromResult(target == "PRIVACY_SETTINGS"); }
}
