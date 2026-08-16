using DynamicUI24.Core.Privacy;

namespace DynamicUI24.Core.Search;

public sealed record PresentedSearchResult(SearchResult Result, string Title, string Subtitle, bool IsHidden,
    bool IsActionable);

public sealed class SearchResultPresenter(IPrivacyPolicyResolver privacyResolver, ISensitiveValuePresenter valuePresenter)
{
    public PresentedSearchResult Present(SearchResult result, PrivacyResolutionContext? context)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.PrivacyMetadata is null || context is null)
            return new(result, result.DisplayText, result.SecondaryText ?? string.Empty, false, result.IsActionable);
        var presentation = PrivacySearchPresentation.Resolve(result.SemanticIdentity, result.DisplayText,
            result.SecondaryText, result.NavigationTarget ?? result.WorkspaceId ?? string.Empty,
            result.PrivacyMetadata, context, privacyResolver, valuePresenter);
        return new(result, presentation.SafeTitle, presentation.SafeSubtitle,
            presentation.Resolution.Presentation == PrivacyPresentation.Hide,
            result.IsActionable && presentation.Resolution.IsAuthorized);
    }
}
