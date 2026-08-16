using System.Globalization;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Setup;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class PrivacyFoundationTests
{
    private readonly PrivacyPolicyResolver resolver = new();
    private readonly SensitiveValuePresenter presenter = new();

    [Theory]
    [InlineData(PrivacyMode.Off, PrivacyMode.Off, PrivacyPresentation.None)]
    [InlineData(PrivacyMode.On, PrivacyMode.On, PrivacyPresentation.Mask)]
    [InlineData(PrivacyMode.Auto, PrivacyMode.Auto, PrivacyPresentation.Mask)]
    public void RequestedModesResolveOptionalConfidentialPresentation(PrivacyMode requested,
        PrivacyMode effective, PrivacyPresentation presentation)
    {
        var result = Resolve(new(Sensitivity.Confidential, PrivacyPresentation.Mask), requested,
            new MandatoryPrivacyPolicy(ProtectRestricted: true));
        Assert.Equal(requested, result.RequestedPrivacyMode);
        Assert.Equal(effective, result.EffectivePrivacyMode);
        Assert.Equal(presentation, result.Presentation);
    }

    [Fact]
    public void OffCannotBypassMandatoryRestrictedProtection()
    {
        var result = Resolve(new(Sensitivity.Restricted, PrivacyPresentation.CaptureProtect,
            PrivacyPresentation.Mask), PrivacyMode.Off, capability: CaptureProtectionCapability.Unsupported);
        Assert.Equal(PrivacyMode.Off, result.RequestedPrivacyMode);
        Assert.Equal(PrivacyMode.On, result.EffectivePrivacyMode);
        Assert.Equal(PrivacyPresentation.Mask, result.Presentation);
        Assert.True(result.FallbackApplied);
    }

    [Theory]
    [InlineData(CaptureProtectionCapability.Supported, PrivacyPresentation.CaptureProtect, false)]
    [InlineData(CaptureProtectionCapability.Partial, PrivacyPresentation.Hide, true)]
    [InlineData(CaptureProtectionCapability.Unsupported, PrivacyPresentation.Hide, true)]
    [InlineData(CaptureProtectionCapability.Unknown, PrivacyPresentation.Hide, true)]
    public void CaptureCapabilityIsHonestAndFallsBack(CaptureProtectionCapability capability,
        PrivacyPresentation expected, bool fallback)
    {
        var result = Resolve(new(Sensitivity.Restricted, PrivacyPresentation.CaptureProtect,
            PrivacyPresentation.Hide), PrivacyMode.On, capability: capability);
        Assert.Equal(expected, result.Presentation); Assert.Equal(fallback, result.FallbackApplied);
        Assert.Equal(capability == CaptureProtectionCapability.Supported, result.CaptureProtectionAvailable);
    }

    [Fact]
    public void UnauthorizedOrMalformedSensitiveMetadataFailsClosed()
    {
        var unauthorized = Resolve(new(Sensitivity.Restricted, PrivacyPresentation.None), PrivacyMode.Off, authorized: false);
        Assert.Equal(PrivacyPresentation.Hide, unauthorized.Presentation);
        var malformed = Resolve(new((Sensitivity)999, (PrivacyPresentation)999), PrivacyMode.Off);
        Assert.Equal(Sensitivity.Restricted, malformed.EffectiveSensitivity);
        Assert.Equal(PrivacyPresentation.Mask, malformed.Presentation);
    }

    [Fact]
    public void MissingMetadataRemainsNormalAndVisibleForBackwardCompatibility()
    {
        var result = resolver.Resolve(new(true, null, PrivacyMode.On));
        Assert.Equal(Sensitivity.Normal, result.EffectiveSensitivity);
        Assert.Equal(PrivacyPresentation.None, result.Presentation);
    }

    [Fact]
    public void PresenterMasksWithoutLengthLeakAndPartialMaskIsGeneric()
    {
        var full = presenter.Present("123456789012345", null,
            Resolve(new(Sensitivity.Confidential, PrivacyPresentation.Mask), PrivacyMode.On));
        Assert.Equal(SensitiveValuePresenter.Mask, full.DisplayValue);
        var metadata = new SensitiveContentDefinition(Sensitivity.Confidential, PrivacyPresentation.PartialMask,
            PartialMask: new(0, 4, "•••• "));
        var partial = presenter.Present("12345678", metadata, Resolve(metadata, PrivacyMode.On));
        Assert.Equal("•••• 5678", partial.DisplayValue);
    }

    [Fact]
    public void HiddenAndMaskedValuesNeverLeakToTooltipOrAccessibility()
    {
        var metadata = new SensitiveContentDefinition(Sensitivity.Restricted, PrivacyPresentation.Hide);
        var value = presenter.Present("raw-secret", metadata, Resolve(metadata, PrivacyMode.On), CultureInfo.GetCultureInfo("en-US"));
        Assert.Equal("Hidden", value.DisplayValue); Assert.Null(value.TooltipValue);
        Assert.Equal("Sensitive value hidden", value.AccessibleValue);
        Assert.DoesNotContain("raw-secret", $"{value.DisplayValue}{value.AccessibleValue}{value.TooltipValue}");
    }

    [Fact]
    public void RevealDoesNotImplyCopyExportOrAccessibility()
    {
        var metadata = new SensitiveContentDefinition(Sensitivity.Confidential, PrivacyPresentation.Mask,
            AllowTemporaryReveal: true, TemporaryRevealDuration: TimeSpan.FromSeconds(5));
        var result = Resolve(metadata, PrivacyMode.On, revealed: true);
        Assert.Equal(PrivacyPresentation.None, result.Presentation);
        Assert.False(result.CanCopy); Assert.False(result.CanExport); Assert.False(result.CanExposeToAccessibility);
    }

    [Fact]
    public void RevealTimeoutManualAndContextInvalidationAreGenerationSafe()
    {
        var clock = new ManualTimeProvider(); var state = new PrivacyStateService(clock);
        var generation = state.Generation;
        Assert.True(state.BeginReveal(new("FIELD", RevealScope.Field, TimeSpan.FromSeconds(2), generation)));
        Assert.True(state.IsRevealed("FIELD", generation));
        clock.Advance(TimeSpan.FromSeconds(3)); Assert.False(state.IsRevealed("FIELD", generation));
        Assert.True(state.BeginReveal(new("FIELD", RevealScope.Field, TimeSpan.FromSeconds(2), state.Generation)));
        state.InvalidateContext("COMPANY_B", "WORKSPACE_B");
        Assert.False(state.IsRevealed("FIELD", generation));
        Assert.False(state.BeginReveal(new("FIELD", RevealScope.Field, TimeSpan.FromSeconds(2), generation)));
    }

    [Fact]
    public void MixedClipboardCopyMasksProtectedCells()
    {
        var normal = new ColumnDefinition("n", "N", new("N"), "N", null, ColumnDataType.Text,
            ColumnEditorKind.TextBox, ColumnMode.Input, 0, null, null, null, true, false, null, null, null, null, null, 1, SetupDefinitionStatus.Published);
        var secretMetadata = new SensitiveContentDefinition(Sensitivity.Restricted, PrivacyPresentation.Mask);
        var secret = normal with { ColumnId = "s", ColumnCode = "S", VariableCode = new("S"), SensitiveContent = secretMetadata };
        var text = PrivacyClipboardPolicy.Serialize([[new("public", normal, Resolve(null, PrivacyMode.On)),
            new("raw-secret", secret, Resolve(secretMetadata, PrivacyMode.On))]], presenter);
        Assert.Equal($"public\t{SensitiveValuePresenter.Mask}", text);
        Assert.DoesNotContain("raw-secret", text);
    }

    [Fact]
    public void SearchNavigationAndNotificationIdentityRemainStableWhileValuesMask()
    {
        var metadata = new SensitiveContentDefinition(Sensitivity.Restricted, PrivacyPresentation.Mask);
        var context = new PrivacyResolutionContext(true, metadata, PrivacyMode.On);
        var search = PrivacySearchPresentation.Resolve("RESULT-1", "Safe", "raw-secret", "workspace/1",
            metadata, context, resolver, presenter);
        var notification = PrivacyNotificationPresentation.Resolve("PRIVATE_REFERENCE", "raw-secret", metadata,
            context, resolver, presenter);
        Assert.Equal("RESULT-1", search.StableId); Assert.Equal("workspace/1", search.NavigationTarget);
        Assert.Equal(SensitiveValuePresenter.Mask, search.SafeSubtitle);
        Assert.Equal(search.SafeSubtitle, notification.Value.DisplayValue);
    }

    [Fact]
    public void ImportPreviewAndExportUseIndependentSharedPolicy()
    {
        var metadata = new SensitiveContentDefinition(Sensitivity.Restricted, PrivacyPresentation.Mask);
        var context = new PrivacyResolutionContext(true, metadata, PrivacyMode.On);
        Assert.Equal(SensitiveValuePresenter.Mask,
            PrivacyImportExportPolicy.PresentImportPreview("raw-secret", metadata, context, resolver, presenter).DisplayValue);
        var denied = PrivacyImportExportPolicy.ResolveExport("raw-secret", metadata, context, resolver, presenter);
        Assert.Equal(ProtectedValueDisposition.Omit, denied.Disposition); Assert.Null(denied.Value);
        var masked = PrivacyImportExportPolicy.ResolveExport("raw-secret", metadata, context, resolver, presenter,
            ProtectedValueDisposition.Masked);
        Assert.Equal(SensitiveValuePresenter.Mask, masked.Value);
    }

    [Fact]
    public void FormDetailUsesSameResolverAsOtherSurfaces()
    {
        var metadata = new SensitiveContentDefinition(Sensitivity.Restricted, PrivacyPresentation.Hide);
        var detail = new PrivacyDetailPresenter(resolver, presenter).Present(
            [new("PUBLIC_NOTE", "visible"), new("PRIVATE_REFERENCE", "raw-secret", metadata)],
            field => new(true, field.Metadata, PrivacyMode.On));
        Assert.Equal("visible", detail[0].Value.DisplayValue);
        Assert.Equal("Hidden", detail[1].Value.DisplayValue);
    }

    private ResolvedPrivacyPresentation Resolve(SensitiveContentDefinition? metadata, PrivacyMode mode,
        MandatoryPrivacyPolicy? policy = null, bool authorized = true, bool revealed = false,
        CaptureProtectionCapability capability = CaptureProtectionCapability.Unknown) => resolver.Resolve(
            new(authorized, metadata, mode, policy ?? new(), IsTemporarilyRevealed: revealed, CaptureCapability: capability));

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }
}
