using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Search;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Sheets;

public sealed record SheetPresentation(SheetCode SheetCode, string Title, string? Subtitle,
    AuthorizationPresentationState AuthorizationState, ResolvedPrivacyPresentation Privacy,
    bool IsHidden, bool IsActive);

/// <summary>One P1 projection shared by header, tabs, overflow, search and accessibility.</summary>
public sealed class SheetPresentationResolver(IPrivacyPolicyResolver privacy, ISensitiveValuePresenter presenter,
    ILocalizationService localization)
{
    public SheetPresentation Resolve(SheetDefinition sheet, bool active, EffectiveAuthorizationContext? authorization,
        PrivacyMode privacyMode, CompanyId? companyId, string workspaceId)
    {
        var auth = sheet.PresentationRequirement is null ? AuthorizationPresentationState.VisibleEnabled :
            AuthorizationPresentationResolver.Resolve(sheet.PresentationRequirement, authorization);
        var authorized = auth != AuthorizationPresentationState.Hidden;
        var resolved = privacy.Resolve(new(authorized, sheet.PrivacyMetadata, privacyMode,
            CompanyId: companyId, WorkspaceId: workspaceId));
        var rawTitle = localization.Get(sheet.GridHeader?.TitleKey ?? sheet.TitleKey);
        var subtitleKey = sheet.GridHeader?.SubtitleKey ?? sheet.SubtitleKey;
        var rawSubtitle = subtitleKey is null ? null : localization.Get(subtitleKey.Value);
        var title = presenter.Present(rawTitle, sheet.GridHeader?.TitlePrivacy ?? sheet.PrivacyMetadata, resolved).DisplayValue;
        var subtitle = rawSubtitle is null ? null : presenter.Present(rawSubtitle,
            sheet.GridHeader?.SubtitlePrivacy ?? sheet.PrivacyMetadata, resolved).DisplayValue;
        return new(sheet.SheetCode, title, subtitle, auth, resolved, sheet.IsHidden, active);
    }
}

public interface ISheetNavigationService
{
    Task<bool> ActivateAsync(string workspaceCode, SheetCode sheetCode, CancellationToken cancellationToken = default);
}

public sealed record SheetSearchTarget(string WorkspaceCode, SheetCode SheetCode)
{
    public string SemanticIdentity => $"{WorkspaceCode.Trim().ToUpperInvariant()}:{SheetCode.Value}";
}

public sealed class SheetSearchProvider(string workspaceCode, Func<IReadOnlyList<SheetPresentation>> presentations) : ISearchProvider
{
    public string ProviderCode => "SHEETS";
    public IReadOnlySet<SearchResultKind> SupportedKinds { get; } = new HashSet<SearchResultKind> { SearchResultKind.Workspace };
    public Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var results = presentations().Where(x => !x.IsHidden && x.AuthorizationState != AuthorizationPresentationState.Hidden &&
            x.Privacy.CanSearchRaw && (query.NormalizedText.Length == 0 || SearchText.Normalize(x.Title).Contains(query.NormalizedText, StringComparison.Ordinal)))
            .Select(x => (SearchResult)new($"SHEET:{x.SheetCode.Value}", SearchResultKind.Workspace, ProviderCode,
                x.Title, x.Subtitle, workspaceId: workspaceCode, navigationTarget: x.SheetCode.Value,
                deduplicationKey: new SheetSearchTarget(workspaceCode, x.SheetCode).SemanticIdentity,
                isActionable: true, canFavorite: true, canPin: true, canRecordRecent: true)).ToArray();
        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }
}

/// <summary>Invalidates S2 immediately on semantic sheet changes so stale prior-sheet results cannot publish.</summary>
public sealed class SheetContextBridge(ContextPanelCoordinator context)
{
    public void OnActiveSheetChanged() => context.Invalidate();
    public static ContextSelection Selection(SheetCode sheetCode, string? rowKey = null, string? variableCode = null) =>
        new(EntityKey: $"SHEET:{sheetCode.Value}", RowKey: rowKey, VariableCode: variableCode);
}
