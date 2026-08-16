using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;

namespace DynamicUI24.Core.Search;

public sealed record SearchCoordinatorOptions(int GlobalLimit = 50, int PerKindLimit = 12)
{
    public SearchCoordinatorOptions Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(GlobalLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(PerKindLimit);
        return this;
    }
}

public sealed class SearchCoordinator
{
    private readonly ImmutableArray<ISearchProvider> providers;
    private readonly SearchCoordinatorOptions options;
    private readonly object gate = new();
    private CancellationTokenSource? active;
    private long generation;

    public SearchCoordinator(IEnumerable<ISearchProvider> providers, SearchCoordinatorOptions? options = null)
    {
        this.providers = (providers ?? throw new ArgumentNullException(nameof(providers))).ToImmutableArray();
        this.options = (options ?? new()).Validate();
    }

    public long CurrentGeneration { get { lock (gate) return generation; } }

    public async Task<SearchResponse> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        CancellationTokenSource request;
        long current;
        lock (gate)
        {
            active?.Cancel();
            active?.Dispose();
            active = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            request = active;
            current = ++generation;
        }
        query = query with { Generation = current };
        var tasks = providers.Select(p => SafeSearchAsync(p, query, request.Token)).ToArray();
        var responses = await Task.WhenAll(tasks).ConfigureAwait(false);
        lock (gate)
            if (current != generation)
                return new(current, [], [], true);

        var failed = responses.Where(x => x.Error).Select(x => x.Provider.ProviderCode)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToImmutableArray();
        var candidates = responses.Where(x => !x.Error).SelectMany(x => x.Results)
            .Where(IsStructurallySafe).Where(r => InCompanyScope(r, query.CurrentCompanyId))
            .Select(r => ApplyAuthorization(r, query.PermissionContext)).Where(r => r is not null).Cast<SearchResult>()
            .Where(r => Matches(r, query.NormalizedText))
            .GroupBy(r => r.SemanticIdentity, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(r => KindRank(r.ResultKind)).ThenByDescending(r => r.IsPinned)
                .ThenByDescending(r => r.IsFavorite).ThenBy(r => r.ProviderRank).ThenBy(r => r.ProviderCode, StringComparer.Ordinal)
                .ThenBy(r => r.ResultId, StringComparer.Ordinal).First())
            .OrderByDescending(r => MatchRank(r, query.NormalizedText))
            .ThenByDescending(r => r.IsPinned).ThenByDescending(r => r.IsFavorite)
            .ThenByDescending(r => r.LastUsedAt)
            .ThenBy(r => KindRank(r.ResultKind)).ThenBy(r => r.ProviderRank)
            .ThenBy(r => r.ResultId, StringComparer.Ordinal)
            .GroupBy(r => r.ResultKind).SelectMany(g => g.Take(options.PerKindLimit))
            .Take(options.GlobalLimit).ToImmutableArray();
        return new(current, candidates, failed);
    }

    public void Invalidate()
    {
        lock (gate) { generation++; active?.Cancel(); }
    }

    private static async Task<ProviderResponse> SafeSearchAsync(ISearchProvider provider, SearchQuery query, CancellationToken token)
    {
        try { return new(provider, await provider.SearchAsync(query, token).ConfigureAwait(false) ?? [], false); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return new(provider, [], false); }
        catch { return new(provider, [], true); }
    }

    private static bool IsStructurallySafe(SearchResult? result) => result is not null &&
        Enum.IsDefined(result.ResultKind) && result.DisplayText.Length > 0;
    private static bool InCompanyScope(SearchResult result, string? companyId) => result.CompanyScope switch
    {
        CompanyScopeKind.Global => true,
        CompanyScopeKind.CompanyScoped => companyId is not null && result.CompanyId is not null &&
            result.CompanyId.Equals(companyId, StringComparison.OrdinalIgnoreCase),
        _ => false,
    };
    private static SearchResult? ApplyAuthorization(SearchResult result, EffectiveAuthorizationContext? context)
    {
        if (result.PresentationRequirement is null) return result;
        return AuthorizationPresentationResolver.Resolve(result.PresentationRequirement, context) switch
        {
            AuthorizationPresentationState.Hidden => null,
            AuthorizationPresentationState.VisibleEnabled => result,
            _ => result with { IsActionable = false },
        };
    }
    private static bool Matches(SearchResult r, string query) => query.Length == 0 ||
        SearchText.Normalize(r.DisplayText).Contains(query, StringComparison.Ordinal) ||
        SearchText.Normalize(r.SecondaryText).Contains(query, StringComparison.Ordinal) ||
        SearchText.Normalize(r.ResultId).Contains(query, StringComparison.Ordinal);
    private static int MatchRank(SearchResult r, string query)
    {
        if (query.Length == 0) return 0;
        var name = SearchText.Normalize(r.DisplayText); var id = SearchText.Normalize(r.ResultId);
        if (name == query) return 4;
        if (id == query) return 3;
        if (name.StartsWith(query, StringComparison.Ordinal)) return 2;
        return name.Contains(query, StringComparison.Ordinal) || id.Contains(query, StringComparison.Ordinal) ? 1 : 0;
    }
    private static int KindRank(SearchResultKind kind) => kind switch
    {
        SearchResultKind.Pinned => 0, SearchResultKind.Favorite => 1, SearchResultKind.Recent => 2,
        SearchResultKind.Workspace => 3, SearchResultKind.TreeNode => 4, SearchResultKind.Command => 5,
        SearchResultKind.Setting => 6, SearchResultKind.Record => 7, SearchResultKind.Document => 8,
        SearchResultKind.Report => 9, _ => 99,
    };
    private sealed record ProviderResponse(ISearchProvider Provider, IReadOnlyList<SearchResult> Results, bool Error);
}
