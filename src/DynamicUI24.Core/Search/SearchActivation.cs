using DynamicUI24.Core.Navigation;
using DynamicUI24.Core.Ribbon;

namespace DynamicUI24.Core.Search;

public enum SearchActivationStatus { Success, Unavailable, Denied, Failed }
public sealed record SearchActivationResult(SearchActivationStatus Status, string? DiagnosticCode = null)
{
    public static SearchActivationResult Success() => new(SearchActivationStatus.Success);
    public static SearchActivationResult Unavailable(string code) => new(SearchActivationStatus.Unavailable, code);
    public static SearchActivationResult Denied(string code = "SEARCH_RESULT_DENIED") => new(SearchActivationStatus.Denied, code);
    public static SearchActivationResult Failed(string code = "SEARCH_ACTIVATION_FAILED") => new(SearchActivationStatus.Failed, code);
}

public interface ISettingNavigationService
{
    Task<bool> NavigateAsync(string target, CancellationToken cancellationToken = default);
}

public sealed class SearchActivationService(IWorkspaceNavigationService navigation, IUiCommandRegistry commands,
    Func<RibbonCommandExecutionContext> commandContext, ISettingNavigationService? settings = null,
    IQuickAccessStore? quickAccess = null)
{
    public async Task<SearchActivationResult> ActivateAsync(SearchResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsActionable) return SearchActivationResult.Denied();
        try
        {
            SearchActivationResult activated;
            if (result.ResultKind is SearchResultKind.Workspace or SearchResultKind.TreeNode or SearchResultKind.Record or
                SearchResultKind.Document or SearchResultKind.Report or SearchResultKind.Recent or SearchResultKind.Favorite or SearchResultKind.Pinned)
            {
                var target = result.WorkspaceId ?? result.NavigationTarget;
                if (target is null) return SearchActivationResult.Unavailable("SEARCH_NAVIGATION_TARGET_UNKNOWN");
                var nav = await navigation.NavigateAsync(target, cancellationToken).ConfigureAwait(false);
                activated = nav.IsSuccess ? SearchActivationResult.Success() : SearchActivationResult.Unavailable(nav.DiagnosticCode ?? "SEARCH_NAVIGATION_UNAVAILABLE");
            }
            else if (result.ResultKind == SearchResultKind.Command)
            {
                if (result.RegisteredCommandCode is null) return SearchActivationResult.Unavailable("SEARCH_COMMAND_UNKNOWN");
                var command = await commands.ExecuteAsync(result.RegisteredCommandCode, commandContext(), cancellationToken).ConfigureAwait(false);
                activated = command.Status switch
                {
                    RibbonCommandResultStatus.Success => SearchActivationResult.Success(),
                    RibbonCommandResultStatus.Denied => SearchActivationResult.Denied(command.DiagnosticCode ?? "SEARCH_COMMAND_DENIED"),
                    RibbonCommandResultStatus.Failed => SearchActivationResult.Failed(command.DiagnosticCode ?? "SEARCH_COMMAND_FAILED"),
                    _ => SearchActivationResult.Unavailable(command.DiagnosticCode ?? "SEARCH_COMMAND_UNKNOWN"),
                };
            }
            else if (result.ResultKind == SearchResultKind.Setting && settings is not null && result.NavigationTarget is { } setting)
                activated = await settings.NavigateAsync(setting, cancellationToken).ConfigureAwait(false)
                    ? SearchActivationResult.Success() : SearchActivationResult.Unavailable("SEARCH_SETTING_UNKNOWN");
            else activated = SearchActivationResult.Unavailable("SEARCH_RESULT_KIND_UNSUPPORTED");

            if (activated.Status == SearchActivationStatus.Success && result.CanRecordRecent && quickAccess is not null)
                quickAccess.RecordRecent(new(result.SemanticIdentity, result.ResultKind,
                    result.WorkspaceId ?? result.NavigationTarget ?? result.RegisteredCommandCode ?? result.ResultId,
                    result.ProviderCode, result.CompanyScope, result.CompanyId, result.WorkspaceId));
            return activated;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return SearchActivationResult.Failed(); }
    }
}
