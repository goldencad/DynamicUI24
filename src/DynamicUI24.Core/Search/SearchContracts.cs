using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Search;

public enum SearchScope { GlobalSearch, NavigationSearch, WorkspaceSearch }
public enum SearchResultKind { Workspace, TreeNode, Command, Setting, Record, Document, Report, Recent, Favorite, Pinned }
public enum CompanyScopeKind { Global, CompanyScoped }

public sealed record SearchQuery(
    string QueryText,
    SearchScope Scope,
    string? CurrentCompanyId = null,
    string? CurrentWorkspaceId = null,
    string? CurrentTemplateCode = null,
    string? CurrentNavigationNode = null,
    CultureInfo? Culture = null,
    EffectiveAuthorizationContext? PermissionContext = null,
    PrivacyResolutionContext? PrivacyContext = null,
    long Generation = 0)
{
    public string NormalizedText { get; } = SearchText.Normalize(QueryText);
    public CultureInfo EffectiveCulture { get; } = Culture ?? CultureInfo.CurrentCulture;
}

public sealed record SearchResult
{
    public SearchResult(string resultId, SearchResultKind resultKind, string providerCode,
        string displayText, string? secondaryText = null, LocalizationKey? displayNameKey = null,
        LocalizationKey? secondaryTextKey = null, IconKey? iconKey = null, double? score = null,
        int providerRank = 0, string? workspaceId = null, string? navigationTarget = null,
        string? registeredCommandCode = null, string? contextKey = null,
        PresentationRequirement? presentationRequirement = null,
        CompanyScopeKind companyScope = CompanyScopeKind.Global, string? companyId = null,
        string? deduplicationKey = null, SensitiveContentDefinition? privacyMetadata = null,
        bool isActionable = true, bool canFavorite = false, bool canPin = false,
        bool canRecordRecent = false, bool isFavorite = false, bool isPinned = false,
        DateTimeOffset? lastUsedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerCode);
        ResultId = resultId.Trim();
        ResultKind = resultKind;
        ProviderCode = providerCode.Trim().ToUpperInvariant();
        DisplayText = displayText?.Trim() ?? string.Empty;
        SecondaryText = secondaryText;
        DisplayNameKey = displayNameKey;
        SecondaryTextKey = secondaryTextKey;
        IconKey = iconKey;
        Score = score is { } s && double.IsFinite(s) ? s : null;
        ProviderRank = providerRank;
        WorkspaceId = Clean(workspaceId);
        NavigationTarget = Clean(navigationTarget);
        RegisteredCommandCode = Clean(registeredCommandCode);
        ContextKey = Clean(contextKey);
        PresentationRequirement = presentationRequirement;
        CompanyScope = companyScope;
        CompanyId = Clean(companyId);
        DeduplicationKey = Clean(deduplicationKey);
        PrivacyMetadata = privacyMetadata;
        IsActionable = isActionable;
        CanFavorite = canFavorite;
        CanPin = canPin;
        CanRecordRecent = canRecordRecent;
        IsFavorite = isFavorite;
        IsPinned = isPinned;
        LastUsedAt = lastUsedAt;
    }

    public string ResultId { get; }
    public SearchResultKind ResultKind { get; init; }
    public string ProviderCode { get; }
    public string DisplayText { get; }
    public string? SecondaryText { get; }
    public LocalizationKey? DisplayNameKey { get; }
    public LocalizationKey? SecondaryTextKey { get; }
    public IconKey? IconKey { get; }
    public double? Score { get; }
    public int ProviderRank { get; }
    public string? WorkspaceId { get; init; }
    public string? NavigationTarget { get; init; }
    public string? RegisteredCommandCode { get; init; }
    public string? ContextKey { get; }
    public PresentationRequirement? PresentationRequirement { get; init; }
    public CompanyScopeKind CompanyScope { get; init; }
    public string? CompanyId { get; init; }
    public string? DeduplicationKey { get; init; }
    public SensitiveContentDefinition? PrivacyMetadata { get; }
    public bool IsActionable { get; init; }
    public bool CanFavorite { get; init; }
    public bool CanPin { get; init; }
    public bool CanRecordRecent { get; init; }
    public bool IsFavorite { get; init; }
    public bool IsPinned { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public string SemanticIdentity => DeduplicationKey ?? $"{ProviderCode}:{ResultId}";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public interface ISearchProvider
{
    string ProviderCode { get; }
    IReadOnlySet<SearchResultKind> SupportedKinds { get; }
    Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
}

public sealed record SearchGroup(SearchResultKind Kind, ImmutableArray<SearchResult> Results);
public sealed record SearchResponse(long Generation, ImmutableArray<SearchResult> Results,
    ImmutableArray<string> FailedProviderCodes, bool IsStale = false)
{
    public ImmutableArray<SearchGroup> Groups => Results.GroupBy(x => x.ResultKind)
        .Select(x => new SearchGroup(x.Key, x.ToImmutableArray())).ToImmutableArray();
}

public static class SearchText
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => c is 'đ' or 'Đ' ? 'D' : char.ToUpperInvariant(c)).ToArray()).Normalize(NormalizationForm.FormC);
    }
}
