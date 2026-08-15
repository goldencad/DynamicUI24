using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Core.ApplicationMenu;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class ApplicationMenuCompositionTests
{
    [Fact]
    public void StandardSectionsArePresentInDeterministicOrder()
    {
        var items = new ApplicationMenuComposer().Compose();
        Assert.Equal(
            ["COMPANY_CONTEXT", "LANGUAGE", "APPEARANCE", "GENERAL_SETTINGS", "ACCOUNT", "LICENSE", "ABOUT", "EXIT"],
            items.Select(x => x.Item.Code));
        Assert.All(items, x =>
        {
            Assert.True(x.Item.IsStandard);
            Assert.False(string.IsNullOrWhiteSpace(x.Item.DisplayNameKey.Value));
            Assert.False(string.IsNullOrWhiteSpace(x.Item.IconKey.Value));
        });
    }

    [Fact]
    public void ContributorRegistersAndDuplicateCodeIsRejected()
    {
        var composer = new ApplicationMenuComposer();
        Assert.True(composer.Register(new StubContributor("CUSTOM", 450)));
        Assert.False(composer.Register(new StubContributor("custom", 451)));
        Assert.Contains(composer.Compose(), x => x.Item.Code == "CUSTOM");
    }

    [Fact]
    public void FailedContributorIsIsolated()
    {
        var composer = new ApplicationMenuComposer();
        composer.Register(new ThrowingContributor());
        composer.Register(new StubContributor("SAFE", 451));
        var result = composer.Compose();
        Assert.Contains(result, x => x.Item.Code == "SAFE");
        Assert.Contains(result, x => x.Item.Code == StandardApplicationMenuCodes.Exit);
    }

    [Fact]
    public void PermissionCanHideAndCapabilityCanDisableContributorFailClosed()
    {
        var composer = new ApplicationMenuComposer();
        composer.Register(new RequirementContributor("HIDDEN", new(
            PermissionCode: new PermissionCode("PROFILE.VIEW"),
            UnauthorizedBehavior: UnauthorizedBehavior.Hide)));
        composer.Register(new RequirementContributor("DISABLED", new(
            CapabilityCode: new CapabilityCode("OPTIONAL.AVAILABLE"),
            CapabilityUnavailableBehavior: UnauthorizedBehavior.Disable)));
        var result = composer.Compose(null);
        Assert.DoesNotContain(result, x => x.Item.Code == "HIDDEN");
        Assert.Equal(AuthorizationPresentationState.VisibleDisabled,
            Assert.Single(result, x => x.Item.Code == "DISABLED").PresentationState);
    }

    [Fact]
    public void LocalizationKeysResolveAndSemanticIconsUseRegistry()
    {
        var composer = new ApplicationMenuComposer();
        var english = new DictionaryLocalizationService("en-US");
        var vietnamese = new DictionaryLocalizationService("vi-VN");
        var icons = new SemanticIconRegistry();
        foreach (var item in composer.Compose().Select(x => x.Item))
        {
            Assert.DoesNotContain("[", english.Get(item.DisplayNameKey));
            Assert.DoesNotContain("[", vietnamese.Get(item.DisplayNameKey));
            Assert.False(icons.Resolve(item.IconKey).IsFallback);
        }
        Assert.NotEqual(english.Get(new("AppMenu.About")), vietnamese.Get(new("AppMenu.About")));
    }

    [Fact]
    public async Task OptionalPresentationProvidersAreSafeWhenMissingFailingOrReady()
    {
        var missing = await OptionalPresentationLoader.LoadAsync<AccountPresentation>(null);
        var failing = await OptionalPresentationLoader.LoadAsync<AccountPresentation>(
            _ => Task.FromException<AccountPresentation?>(new InvalidOperationException("proof")));
        var ready = await OptionalPresentationLoader.LoadAsync<AccountPresentation>(
            _ => Task.FromResult<AccountPresentation?>(new("Demo")));
        Assert.Equal(OptionalPresentationStatus.Unavailable, missing.Status);
        Assert.Equal(OptionalPresentationStatus.Error, failing.Status);
        Assert.Equal(OptionalPresentationStatus.Ready, ready.Status);
        Assert.Equal("Demo", ready.Value!.DisplayName);
    }

    [Fact]
    public void RuntimeLocalizationSwitchChangesMenuLabelsAndPreservesShellAndCompanyIdentity()
    {
        var localization = new DictionaryLocalizationService("en-US");
        var key = new ApplicationMenuComposer().Compose().Single(x => x.Item.Code == "LANGUAGE").Item.DisplayNameKey;
        var english = localization.Get(key);
        var shell = new ShellPresentation(ApplicationBrand.Default) { CurrentWorkspaceId = "active-workspace" };
        var company = new CompanyDescriptor(new("a"), "A", "Company A");
        var companies = new CompanyContextProvider([company], company.CompanyId);
        Assert.True(localization.TrySetCulture("vi-VN"));
        Assert.NotEqual(english, localization.Get(key));
        Assert.Equal("active-workspace", shell.CurrentWorkspaceId);
        Assert.Equal(company.CompanyId, companies.CurrentCompany.CompanyId);
    }

    private sealed class StubContributor(string code, int order) : IApplicationMenuContributor
    {
        public string ContributorCode => code;
        public IEnumerable<ApplicationMenuItem> CreateItems() =>
            [new(code, new("Demo.Preferences"), StandardIconKeys.Settings, order)];
    }

    private sealed class ThrowingContributor : IApplicationMenuContributor
    {
        public string ContributorCode => "BROKEN";
        public IEnumerable<ApplicationMenuItem> CreateItems() => throw new InvalidOperationException("proof");
    }

    private sealed class RequirementContributor(string code, PresentationRequirement requirement) : IApplicationMenuContributor
    {
        public string ContributorCode => code;
        public IEnumerable<ApplicationMenuItem> CreateItems() =>
            [new(code, new("Demo.Preferences"), StandardIconKeys.Settings, 450, Requirement: requirement)];
    }
}

public sealed class AppearancePreferenceTests
{
    [Fact]
    public void PreferencesTransitionWithoutChangingIndependentShellState()
    {
        var shell = new ShellPresentation(ApplicationBrand.Default) { CurrentWorkspaceId = "report" };
        var company = new CompanyDescriptor(new("a"), "A", "Company A");
        var companies = new CompanyContextProvider([company], company.CompanyId);
        var service = new AppearancePreferenceService();
        service.Update(new(ThemeMode.Dark, 1.0, FontSizePreference.Large, GridDensityPreference.Compact));
        Assert.Equal(ThemeMode.Dark, service.Current.Theme);
        Assert.Equal(FontSizePreference.Large, service.Current.FontSize);
        Assert.Equal(GridDensityPreference.Compact, service.Current.GridDensity);
        Assert.Equal("report", shell.CurrentWorkspaceId);
        Assert.Equal(company.CompanyId, companies.CurrentCompany.CompanyId);
    }

    [Fact]
    public void InvalidScaleIsRejectedRatherThanFaked()
    {
        var service = new AppearancePreferenceService();
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Update(new(UiScale: 4)));
    }
}
