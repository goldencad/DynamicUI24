using DynamicUI24.Core.Companies;

namespace DynamicUI24.Core.Privacy;

public enum PrivacyMode { Off, On, Auto }
public enum Sensitivity { Normal, Confidential, Restricted }
public enum PrivacyPresentation { None, Mask, PartialMask, Hide, CaptureProtect }
public enum CaptureProtectionCapability { Supported, Partial, Unsupported, Unknown }
public enum CaptureProtectionScope { Window, Region, ContentSurface }
public enum RevealScope { Field, Selection, Workspace }
public enum ProtectedValueDisposition { Raw, Masked, Omit, Block }

public sealed record PartialMaskDefinition(int PreservePrefix = 0, int PreserveSuffix = 4,
    string MaskBody = "••••")
{
    public bool IsValid => PreservePrefix >= 0 && PreserveSuffix >= 0 && !string.IsNullOrWhiteSpace(MaskBody);
}

/// <summary>Composable, application-neutral metadata. Missing metadata is NORMAL for v0.9 compatibility.</summary>
public sealed record SensitiveContentDefinition(
    Sensitivity Sensitivity = Sensitivity.Normal,
    PrivacyPresentation Presentation = PrivacyPresentation.None,
    PrivacyPresentation CaptureProtectionFallback = PrivacyPresentation.Mask,
    bool AllowTemporaryReveal = false,
    TimeSpan? TemporaryRevealDuration = null,
    bool AllowCopyWhenRevealed = false,
    bool AllowExportWhenRevealed = false,
    bool AllowSearchRawValue = false,
    bool AllowNotificationRawValue = false,
    bool AllowTooltipRawValue = false,
    bool AllowAccessibilityRawValue = false,
    string? PolicyCode = null,
    PartialMaskDefinition? PartialMask = null)
{
    public static SensitiveContentDefinition Normal { get; } = new();
}

public sealed record MandatoryPrivacyPolicy(
    bool ProtectConfidential = false,
    bool ProtectRestricted = true,
    PrivacyPresentation ConfidentialMinimum = PrivacyPresentation.Mask,
    PrivacyPresentation RestrictedMinimum = PrivacyPresentation.Mask,
    bool AllowCopyProtected = false,
    bool AllowExportProtected = false,
    PrivacyMode? ForcedMode = null,
    string PolicyReasonCode = "PRIVACY_POLICY");

public sealed record PrivacyResolutionContext(
    bool IsAuthorized,
    SensitiveContentDefinition? Metadata,
    PrivacyMode RequestedPrivacyMode,
    MandatoryPrivacyPolicy? MandatoryPolicy = null,
    CompanyId? CompanyId = null,
    string? WorkspaceId = null,
    bool IsTemporarilyRevealed = false,
    CaptureProtectionCapability CaptureCapability = CaptureProtectionCapability.Unknown,
    long Generation = 0);

public sealed record ResolvedPrivacyPresentation(
    bool IsAuthorized,
    PrivacyMode RequestedPrivacyMode,
    PrivacyMode EffectivePrivacyMode,
    Sensitivity EffectiveSensitivity,
    PrivacyPresentation Presentation,
    bool CanReveal,
    bool CanCopy,
    bool CanExport,
    bool CanSearchRaw,
    bool CanNotifyRaw,
    bool CanExposeToAccessibility,
    bool CaptureProtectionRequested,
    bool CaptureProtectionAvailable,
    bool FallbackApplied,
    string PolicyReasonCode);

public interface IPrivacyPolicyResolver
{
    ResolvedPrivacyPresentation Resolve(PrivacyResolutionContext context);
}

public sealed record CaptureProtectionResult(CaptureProtectionCapability Capability, bool IsProtected,
    CaptureProtectionScope Scope, string ReasonCode);

public interface ICaptureProtectionService
{
    CaptureProtectionCapability GetCapability(CaptureProtectionScope scope);
    ValueTask<CaptureProtectionResult> RequestProtectionAsync(string surfaceId, CaptureProtectionScope scope,
        CancellationToken cancellationToken = default);
    ValueTask ReleaseProtectionAsync(string surfaceId, CancellationToken cancellationToken = default);
}

public sealed class SafeFallbackCaptureProtectionService : ICaptureProtectionService
{
    public CaptureProtectionCapability GetCapability(CaptureProtectionScope scope) => CaptureProtectionCapability.Unsupported;
    public ValueTask<CaptureProtectionResult> RequestProtectionAsync(string surfaceId, CaptureProtectionScope scope,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(
            new CaptureProtectionResult(CaptureProtectionCapability.Unsupported, false, scope, "CAPTURE_UNSUPPORTED"));
    public ValueTask ReleaseProtectionAsync(string surfaceId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
