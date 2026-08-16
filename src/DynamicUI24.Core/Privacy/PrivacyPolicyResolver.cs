namespace DynamicUI24.Core.Privacy;

/// <summary>Single fail-closed policy decision point. Authorization is an input and is never granted here.</summary>
public sealed class PrivacyPolicyResolver : IPrivacyPolicyResolver
{
    public ResolvedPrivacyPresentation Resolve(PrivacyResolutionContext context)
    {
        var metadata = Normalize(context.Metadata, out var malformed);
        var policy = context.MandatoryPolicy ?? new();
        var sensitivity = metadata.Sensitivity;
        var effectiveMode = policy.ForcedMode ?? context.RequestedPrivacyMode;
        var mandatory = sensitivity == Sensitivity.Restricted && policy.ProtectRestricted ||
            sensitivity == Sensitivity.Confidential && policy.ProtectConfidential;
        if (mandatory && effectiveMode == PrivacyMode.Off) effectiveMode = PrivacyMode.On;

        var protectedByMode = effectiveMode is PrivacyMode.On or PrivacyMode.Auto && sensitivity != Sensitivity.Normal;
        var presentation = protectedByMode || mandatory ? metadata.Presentation : PrivacyPresentation.None;
        if (mandatory)
            presentation = Stricter(presentation, sensitivity == Sensitivity.Restricted
                ? policy.RestrictedMinimum : policy.ConfidentialMinimum);
        if (!context.IsAuthorized)
            presentation = sensitivity == Sensitivity.Restricted ? PrivacyPresentation.Hide : PrivacyPresentation.Mask;
        if (malformed && sensitivity != Sensitivity.Normal)
            presentation = PrivacyPresentation.Mask;

        var captureRequested = presentation == PrivacyPresentation.CaptureProtect;
        var captureAvailable = context.CaptureCapability == CaptureProtectionCapability.Supported;
        var fallback = false;
        if (captureRequested && !captureAvailable)
        {
            presentation = SafeFallback(metadata.CaptureProtectionFallback);
            fallback = true;
        }

        var canReveal = context.IsAuthorized && metadata.AllowTemporaryReveal &&
            metadata.TemporaryRevealDuration is { } duration && duration > TimeSpan.Zero;
        var revealed = canReveal && context.IsTemporarilyRevealed;
        if (revealed) presentation = PrivacyPresentation.None;
        var rawWithoutReveal = context.IsAuthorized && !revealed && presentation == PrivacyPresentation.None;
        return new(context.IsAuthorized, context.RequestedPrivacyMode, effectiveMode, sensitivity, presentation,
            canReveal, rawWithoutReveal || revealed && metadata.AllowCopyWhenRevealed || policy.AllowCopyProtected,
            rawWithoutReveal || revealed && metadata.AllowExportWhenRevealed || policy.AllowExportProtected,
            (rawWithoutReveal || revealed) && metadata.AllowSearchRawValue,
            (rawWithoutReveal || revealed) && metadata.AllowNotificationRawValue,
            (rawWithoutReveal || revealed) && metadata.AllowAccessibilityRawValue, captureRequested, captureAvailable, fallback,
            !context.IsAuthorized ? "PRIVACY_UNAUTHORIZED" : malformed ? "PRIVACY_METADATA_INVALID" :
            fallback ? "CAPTURE_SAFE_FALLBACK" : mandatory ? policy.PolicyReasonCode : "PRIVACY_RESOLVED");
    }

    private static SensitiveContentDefinition Normalize(SensitiveContentDefinition? source, out bool malformed)
    {
        source ??= SensitiveContentDefinition.Normal;
        malformed = !Enum.IsDefined(source.Sensitivity) || !Enum.IsDefined(source.Presentation) ||
            !Enum.IsDefined(source.CaptureProtectionFallback) ||
            source.TemporaryRevealDuration is { } duration && duration <= TimeSpan.Zero ||
            source.Presentation == PrivacyPresentation.PartialMask && source.PartialMask?.IsValid != true;
        if (!Enum.IsDefined(source.Sensitivity)) source = source with { Sensitivity = Sensitivity.Restricted };
        if (!Enum.IsDefined(source.Presentation)) source = source with { Presentation = PrivacyPresentation.Mask };
        return source;
    }

    private static PrivacyPresentation SafeFallback(PrivacyPresentation fallback) => fallback switch
    {
        PrivacyPresentation.Mask or PrivacyPresentation.PartialMask or PrivacyPresentation.Hide => fallback,
        _ => PrivacyPresentation.Mask,
    };

    private static PrivacyPresentation Stricter(PrivacyPresentation left, PrivacyPresentation right) =>
        Rank(left) >= Rank(right) ? left : right;
    private static int Rank(PrivacyPresentation value) => value switch
    {
        PrivacyPresentation.None => 0, PrivacyPresentation.PartialMask => 1, PrivacyPresentation.Mask => 2,
        PrivacyPresentation.CaptureProtect => 3, PrivacyPresentation.Hide => 4, _ => 4,
    };
}
