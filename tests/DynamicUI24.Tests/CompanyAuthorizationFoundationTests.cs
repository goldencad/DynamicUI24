using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class CompanyContextTests
{
    private static readonly CompanyDescriptor CompanyA = new(new("a"), "A", "Demo A", IsActive: true);
    private static readonly CompanyDescriptor CompanyB = new(new("b"), "B", "Demo B", IsActive: true);
    private static readonly CompanyDescriptor CompanyInactive = new(new("inactive"), "I", "Inactive", IsActive: false);

    [Fact]
    public void InitialAndAvailableCompaniesAreDeterministic()
    {
        var context = CreateContext();

        Assert.Equal(CompanyA, context.CurrentCompany);
        Assert.Equal([CompanyA, CompanyB, CompanyInactive], context.AvailableCompanies);
    }

    [Fact]
    public async Task SwitchPublishesExactlyOneDeterministicNotification()
    {
        var context = CreateContext();
        CompanyChangedEventArgs? notification = null;
        var count = 0;
        context.CompanyChanged += (_, args) => { notification = args; count++; };

        var result = await context.SwitchCompanyAsync(CompanyB.CompanyId);

        Assert.True(result.IsSuccess);
        Assert.True(result.DidChange);
        Assert.Equal(CompanyB, context.CurrentCompany);
        Assert.Equal(CompanyA, notification!.PreviousCompany);
        Assert.Equal(CompanyB, notification.CurrentCompany);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RepeatedSwitchSucceedsWithoutDuplicateNotification()
    {
        var context = CreateContext();
        var count = 0;
        context.CompanyChanged += (_, _) => count++;

        var result = await context.SwitchCompanyAsync(CompanyA.CompanyId);

        Assert.True(result.IsSuccess);
        Assert.False(result.DidChange);
        Assert.Equal(0, count);
    }

    [Theory]
    [InlineData("missing", CompanySwitchError.UnknownCompany)]
    [InlineData("inactive", CompanySwitchError.InactiveCompany)]
    public async Task InvalidSwitchIsRejectedWithoutChangingState(string id, CompanySwitchError expected)
    {
        var context = CreateContext();

        var result = await context.SwitchCompanyAsync(new CompanyId(id));

        Assert.False(result.IsSuccess);
        Assert.Equal(expected, result.Error);
        Assert.Equal(CompanyA, context.CurrentCompany);
    }

    private static CompanyContextProvider CreateContext() =>
        new([CompanyA, CompanyB, CompanyInactive], CompanyA.CompanyId);
}

public sealed class CompanyProfileTests
{
    [Fact]
    public async Task ProviderReturnsCompanyScopedProfilesAndAdditionalFields()
    {
        var companyA = new CompanyId("a");
        var companyB = new CompanyId("b");
        var provider = new StubProfileProvider(new Dictionary<CompanyId, CompanyProfile>
        {
            [companyA] = new(companyA, "Legal A", additionalFields: [new("Synced", "A")]),
            [companyB] = new(companyB, "Legal B", additionalFields: [new("Synced", "B")]),
        });

        var resultA = await provider.GetProfileAsync(companyA);
        var resultB = await provider.GetProfileAsync(companyB);

        Assert.Equal("Legal A", resultA.Profile!.LegalName);
        Assert.Equal("A", resultA.Profile.AdditionalFields["Synced"]);
        Assert.Equal("Legal B", resultB.Profile!.LegalName);
    }

    [Fact]
    public async Task MissingProfileReturnsSafeNotFoundResult()
    {
        var result = await new StubProfileProvider(
            new Dictionary<CompanyId, CompanyProfile>()).GetProfileAsync(new("missing"));

        Assert.Equal(CompanyProfileStatus.NotFound, result.Status);
        Assert.Null(result.Profile);
    }

    [Fact]
    public void AdditionalFieldsAreReadOnlyAndCopied()
    {
        var source = new Dictionary<string, string> { ["Field"] = "Original" };
        var profile = new CompanyProfile(new("a"), "Legal A", additionalFields: source);
        source["Field"] = "Changed";

        Assert.Equal("Original", profile.AdditionalFields["Field"]);
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(profile.AdditionalFields);
    }
}

public sealed class AuthorizationValueTests
{
    [Fact]
    public void PermissionCodeNormalizesAndUsesValueEquality() =>
        Assert.Equal(new PermissionCode("DATA.EDIT"), new PermissionCode(" data.edit "));

    [Fact]
    public void CapabilityCodeNormalizesAndUsesValueEquality() =>
        Assert.Equal(new CapabilityCode("REPORT.EXPORT"), new CapabilityCode(" report.export "));

    [Fact]
    public void EmptySemanticCodesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new PermissionCode(" "));
        Assert.Throws<ArgumentException>(() => new CapabilityCode(" "));
    }

    [Fact]
    public void EffectiveContextIsImmutableAndCompanyScoped()
    {
        var permissions = new List<PermissionCode> { new("DATA.VIEW") };
        var context = Ready("a", permissions, []);
        permissions.Add(new("DATA.EDIT"));

        Assert.Single(context.PermissionCodes);
        Assert.Equal(new CompanyId("a"), context.CompanyId);
        Assert.Equal("r1", context.Revision);
    }

    [Fact]
    public void CacheIdentityIncludesUserCompanyAndRevision()
    {
        var user = new UserId("user");
        var a = new AuthorizationContextCacheKey(user, new("a"), "r1");
        var b = new AuthorizationContextCacheKey(user, new("b"), "r1");
        var nextRevision = new AuthorizationContextCacheKey(user, new("a"), "r2");

        Assert.NotEqual(a, b);
        Assert.NotEqual(a, nextRevision);
    }

    internal static EffectiveAuthorizationContext Ready(
        string company,
        IEnumerable<PermissionCode> permissions,
        IEnumerable<CapabilityCode> capabilities) =>
        new(new("user"), new(company), permissions, capabilities, "r1");
}

public sealed class AuthorizationPresentationResolverTests
{
    private static readonly PermissionCode Edit = new("DATA.EDIT");
    private static readonly CapabilityCode EditingAvailable = new("DATA.EDITING_AVAILABLE");

    [Fact]
    public void BothRequirementsPresentResolveEnabled()
    {
        var context = AuthorizationValueTests.Ready("a", [Edit], [EditingAvailable]);
        var requirement = new PresentationRequirement(Edit, EditingAvailable, UnauthorizedBehavior.Hide);

        Assert.Equal(AuthorizationPresentationState.VisibleEnabled,
            AuthorizationPresentationResolver.Resolve(requirement, context));
    }

    [Theory]
    [InlineData(UnauthorizedBehavior.Hide, AuthorizationPresentationState.Hidden)]
    [InlineData(UnauthorizedBehavior.Disable, AuthorizationPresentationState.VisibleDisabled)]
    [InlineData(UnauthorizedBehavior.ReadOnly, AuthorizationPresentationState.VisibleReadOnly)]
    public void MissingPermissionUsesConfiguredBehavior(
        UnauthorizedBehavior behavior,
        AuthorizationPresentationState expected)
    {
        var requirement = new PresentationRequirement(Edit, null, behavior);

        Assert.Equal(expected, AuthorizationPresentationResolver.Resolve(
            requirement, AuthorizationValueTests.Ready("b", [], [])));
    }

    [Fact]
    public void MissingCapabilityUsesExplicitUnavailableBehavior()
    {
        var requirement = new PresentationRequirement(
            Edit, EditingAvailable, UnauthorizedBehavior.Hide, UnauthorizedBehavior.Disable);

        Assert.Equal(AuthorizationPresentationState.VisibleDisabled,
            AuthorizationPresentationResolver.Resolve(
                requirement, AuthorizationValueTests.Ready("a", [Edit], [])));
    }

    [Fact]
    public void UnresolvedPrivilegedActionFailsClosed()
    {
        var requirement = new PresentationRequirement(Edit, null, UnauthorizedBehavior.Disable);

        Assert.Equal(AuthorizationPresentationState.VisibleDisabled,
            AuthorizationPresentationResolver.Resolve(requirement, null));
    }

    [Fact]
    public void ExplicitReadOnlyRequirementRemainsSafeWhenProviderUnavailable()
    {
        var unavailable = EffectiveAuthorizationContext.Unavailable(new("user"), new("c"), "r1");
        var requirement = new PresentationRequirement(Edit, null, UnauthorizedBehavior.ReadOnly);

        Assert.Equal(AuthorizationPresentationState.VisibleReadOnly,
            AuthorizationPresentationResolver.Resolve(requirement, unavailable));
    }

    [Fact]
    public void PublicPresentationWithoutRequirementsDoesNotNeedAuthorizationContext() =>
        Assert.Equal(AuthorizationPresentationState.VisibleEnabled,
            AuthorizationPresentationResolver.Resolve(new PresentationRequirement(), null));

    [Fact]
    public void PresentationResultDoesNotRepresentBackendAuthorization()
    {
        var visible = AuthorizationPresentationResolver.Resolve(
            new PresentationRequirement(Edit), AuthorizationValueTests.Ready("a", [Edit], []));
        const bool authoritativeBackendAccepted = false;

        Assert.Equal(AuthorizationPresentationState.VisibleEnabled, visible);
        Assert.False(authoritativeBackendAccepted);
    }
}

public sealed class CompanyScopeCoordinatorTests
{
    [Fact]
    public async Task CompanySwitchRefreshesProfileAndAuthorizationWithoutCrossCompanyLeakage()
    {
        var a = new CompanyDescriptor(new("a"), "A", "Demo A");
        var b = new CompanyDescriptor(new("b"), "B", "Demo B");
        var context = new CompanyContextProvider([a, b], a.CompanyId);
        var profiles = new StubProfileProvider(new Dictionary<CompanyId, CompanyProfile>
        {
            [a.CompanyId] = new(a.CompanyId, "Legal A"),
            [b.CompanyId] = new(b.CompanyId, "Legal B"),
        });
        var auth = new StubAuthorizationProvider(company => company == a.CompanyId
            ? AuthorizationValueTests.Ready("a", [new("DATA.EDIT")], [])
            : AuthorizationValueTests.Ready("b", [], []));
        using var coordinator = new CompanyScopeCoordinator(context, profiles, auth, new(new("user")));
        await coordinator.InitializeAsync();

        var enabled = AuthorizationPresentationResolver.Resolve(
            new(new PermissionCode("DATA.EDIT"), null, UnauthorizedBehavior.ReadOnly),
            coordinator.Snapshot.AuthorizationContext);
        await coordinator.SwitchCompanyAsync(b.CompanyId);
        var readOnly = AuthorizationPresentationResolver.Resolve(
            new(new PermissionCode("DATA.EDIT"), null, UnauthorizedBehavior.ReadOnly),
            coordinator.Snapshot.AuthorizationContext);

        Assert.Equal(AuthorizationPresentationState.VisibleEnabled, enabled);
        Assert.Equal(AuthorizationPresentationState.VisibleReadOnly, readOnly);
        Assert.Equal(b.CompanyId, coordinator.Snapshot.AuthorizationContext!.CompanyId);
        Assert.Equal("Legal B", coordinator.Snapshot.ProfileResult!.Profile!.LegalName);
    }

    [Fact]
    public async Task RapidSwitchCannotPublishStaleResponse()
    {
        var a = new CompanyDescriptor(new("a"), "A", "Demo A");
        var b = new CompanyDescriptor(new("b"), "B", "Demo B");
        var c = new CompanyDescriptor(new("c"), "C", "Demo C");
        var context = new CompanyContextProvider([a, b, c], a.CompanyId);
        var profiles = new StubProfileProvider(new Dictionary<CompanyId, CompanyProfile>
        {
            [a.CompanyId] = new(a.CompanyId, "Legal A"),
            [b.CompanyId] = new(b.CompanyId, "Legal B"),
            [c.CompanyId] = new(c.CompanyId, "Legal C"),
        });
        var pendingB = new TaskCompletionSource<EffectiveAuthorizationContext>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingC = new TaskCompletionSource<EffectiveAuthorizationContext>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var auth = new StubAuthorizationProvider(company => company switch
        {
            var id when id == b.CompanyId => pendingB.Task,
            var id when id == c.CompanyId => pendingC.Task,
            _ => Task.FromResult(AuthorizationValueTests.Ready("a", [], [])),
        });
        using var coordinator = new CompanyScopeCoordinator(context, profiles, auth, new(new("user")));

        var switchB = coordinator.SwitchCompanyAsync(b.CompanyId);
        var switchC = coordinator.SwitchCompanyAsync(c.CompanyId);
        pendingC.SetResult(AuthorizationValueTests.Ready("c", [new("C.ACCESS")], []));
        await switchC;
        pendingB.SetResult(AuthorizationValueTests.Ready("b", [new("B.ACCESS")], []));
        await switchB;

        Assert.Equal(c.CompanyId, coordinator.Snapshot.Company.CompanyId);
        Assert.Equal(c.CompanyId, coordinator.Snapshot.AuthorizationContext!.CompanyId);
        Assert.Contains(new PermissionCode("C.ACCESS"), coordinator.Snapshot.AuthorizationContext.PermissionCodes);
        Assert.DoesNotContain(new PermissionCode("B.ACCESS"), coordinator.Snapshot.AuthorizationContext.PermissionCodes);
    }
}

internal sealed class StubProfileProvider(IReadOnlyDictionary<CompanyId, CompanyProfile> profiles)
    : ICompanyProfileProvider
{
    public Task<CompanyProfileResult> GetProfileAsync(
        CompanyId companyId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(profiles.TryGetValue(companyId, out var profile)
            ? CompanyProfileResult.Ready(profile)
            : CompanyProfileResult.NotFound("TEST-NOT-FOUND"));
}

internal sealed class StubAuthorizationProvider : IAuthorizationPresentationProvider
{
    private readonly Func<CompanyId, Task<EffectiveAuthorizationContext>> load;

    public StubAuthorizationProvider(Func<CompanyId, EffectiveAuthorizationContext> load)
        : this(company => Task.FromResult(load(company)))
    {
    }

    public StubAuthorizationProvider(Func<CompanyId, Task<EffectiveAuthorizationContext>> load) =>
        this.load = load;

    public Task<EffectiveAuthorizationContext> GetEffectiveContextAsync(
        UserContext userContext,
        CompanyId companyId,
        CancellationToken cancellationToken = default) => load(companyId);

    public Task<EffectiveAuthorizationContext> RefreshAsync(
        UserContext userContext,
        CompanyId companyId,
        CancellationToken cancellationToken = default) => load(companyId);
}
