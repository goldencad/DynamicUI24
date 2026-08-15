using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.ApplicationMenu;

public enum ApplicationMenuItemType { SettingPage, Action, Separator, ContributorGroup }

public static class StandardApplicationMenuCodes
{
    public const string CompanyContext = "COMPANY_CONTEXT";
    public const string Language = "LANGUAGE";
    public const string Appearance = "APPEARANCE";
    public const string GeneralSettings = "GENERAL_SETTINGS";
    public const string Account = "ACCOUNT";
    public const string License = "LICENSE";
    public const string About = "ABOUT";
    public const string Exit = "EXIT";
}

public sealed record ApplicationMenuItem(
    string Code,
    LocalizationKey DisplayNameKey,
    IconKey IconKey,
    int DisplayOrder,
    ApplicationMenuItemType ItemType = ApplicationMenuItemType.SettingPage,
    string? TargetPage = null,
    PresentationRequirement? Requirement = null,
    bool IsStandard = false);

public interface IApplicationMenuContributor
{
    string ContributorCode { get; }
    IEnumerable<ApplicationMenuItem> CreateItems();
}

public sealed record ResolvedApplicationMenuItem(
    ApplicationMenuItem Item,
    AuthorizationPresentationState PresentationState,
    string? DiagnosticCode = null);

public sealed class ApplicationMenuComposer
{
    private readonly List<IApplicationMenuContributor> contributors = [];

    public bool Register(IApplicationMenuContributor contributor)
    {
        ArgumentNullException.ThrowIfNull(contributor);
        if (contributors.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.ContributorCode, contributor.ContributorCode)))
            return false;
        contributors.Add(contributor);
        return true;
    }

    public IReadOnlyList<ResolvedApplicationMenuItem> Compose(EffectiveAuthorizationContext? authorization = null)
    {
        var result = StandardItems().Select(Resolve).ToList();
        foreach (var contributor in contributors.OrderBy(x => x.ContributorCode, StringComparer.Ordinal))
        {
            try
            {
                var items = contributor.CreateItems() ?? [];
                result.AddRange(items.Select(Resolve));
            }
            catch
            {
                // One optional extension must never take down the shell.
            }
        }
        return result
            .Where(x => x.PresentationState != AuthorizationPresentationState.Hidden)
            .OrderBy(x => x.Item.DisplayOrder)
            .ThenBy(x => x.Item.Code, StringComparer.Ordinal)
            .ToImmutableArray();

        ResolvedApplicationMenuItem Resolve(ApplicationMenuItem item) => new(
            item,
            item.Requirement is null
                ? AuthorizationPresentationState.VisibleEnabled
                : AuthorizationPresentationResolver.Resolve(item.Requirement, authorization));
    }

    private static IEnumerable<ApplicationMenuItem> StandardItems() =>
    [
        Standard(StandardApplicationMenuCodes.CompanyContext, "AppMenu.Company", StandardIconKeys.Company, 100),
        Standard(StandardApplicationMenuCodes.Language, "AppMenu.Language", StandardIconKeys.Language, 200),
        Standard(StandardApplicationMenuCodes.Appearance, "AppMenu.Appearance", StandardIconKeys.Appearance, 300),
        Standard(StandardApplicationMenuCodes.GeneralSettings, "AppMenu.GeneralSettings", StandardIconKeys.Settings, 400),
        Standard(StandardApplicationMenuCodes.Account, "AppMenu.Account", StandardIconKeys.Account, 500),
        Standard(StandardApplicationMenuCodes.License, "AppMenu.License", StandardIconKeys.License, 600),
        Standard(StandardApplicationMenuCodes.About, "AppMenu.About", StandardIconKeys.About, 900),
        Standard(StandardApplicationMenuCodes.Exit, "AppMenu.Exit", StandardIconKeys.Exit, 1000, ApplicationMenuItemType.Action),
    ];

    private static ApplicationMenuItem Standard(string code, string key, IconKey icon, int order,
        ApplicationMenuItemType type = ApplicationMenuItemType.SettingPage) =>
        new(code, new LocalizationKey(key), icon, order, type, code, IsStandard: true);
}

public sealed record AccountPresentation(string DisplayName, string? Detail = null);
public interface IAccountPresentationProvider { Task<AccountPresentation?> GetAsync(CancellationToken cancellationToken = default); }

public sealed record LicensePresentation(string Edition, string State, DateOnly? Expiration = null, string? EntitlementSummary = null);
public interface ILicensePresentationProvider { Task<LicensePresentation?> GetAsync(CancellationToken cancellationToken = default); }

public enum OptionalPresentationStatus { Ready, Unavailable, Error }
public sealed record OptionalPresentationResult<T>(OptionalPresentationStatus Status, T? Value = default);

public static class OptionalPresentationLoader
{
    public static async Task<OptionalPresentationResult<T>> LoadAsync<T>(
        Func<CancellationToken, Task<T?>>? load,
        CancellationToken cancellationToken = default)
    {
        if (load is null) return new(OptionalPresentationStatus.Unavailable);
        try
        {
            var value = await load(cancellationToken).ConfigureAwait(false);
            return value is null
                ? new(OptionalPresentationStatus.Unavailable)
                : new(OptionalPresentationStatus.Ready, value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(OptionalPresentationStatus.Error); }
    }
}
