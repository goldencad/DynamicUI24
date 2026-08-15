using DynamicUI24.Core.ApplicationMenu;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

internal sealed class DemoPreferencesContributor : IApplicationMenuContributor
{
    public string ContributorCode => "DEMO_PREFERENCES";
    public IEnumerable<ApplicationMenuItem> CreateItems()
    {
        yield return new(
            ContributorCode,
            new LocalizationKey("Demo.Preferences"),
            StandardIconKeys.Settings,
            450,
            TargetPage: ContributorCode,
            Requirement: new PresentationRequirement(
                CapabilityCode: new CapabilityCode("DATA.EDITING_AVAILABLE"),
                CapabilityUnavailableBehavior: UnauthorizedBehavior.Disable));
    }
}

internal sealed class DemoAccountPresentationProvider : IAccountPresentationProvider
{
    public Task<AccountPresentation?> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<AccountPresentation?>(new("Demo User", "In-memory demo presentation only"));
}

internal sealed class DemoLicensePresentationProvider : ILicensePresentationProvider
{
    public Task<LicensePresentation?> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<LicensePresentation?>(new("Framework Demo", "DEMO", null, "Presentation data only; no enforcement"));
}
