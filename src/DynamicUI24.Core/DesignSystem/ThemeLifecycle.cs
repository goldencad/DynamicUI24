using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.DesignSystem;

public readonly record struct ThemeCode
{
    public ThemeCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct ThemeVersion
{
    public ThemeVersion(long value)
    {
        if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }

    public long Value { get; }
    public override string ToString() => $"v{Value}";
}

public readonly record struct ThemeGeneration
{
    public ThemeGeneration(long value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }

    public long Value { get; }
}

public readonly record struct ThemeDraftId
{
    public ThemeDraftId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public sealed record FontMapping(string PrimaryFamily, ImmutableArray<string> FallbackFamilies, int? Weight = null)
{
    public FontMapping Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(PrimaryFamily);
        if (Weight is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(Weight));
        return this;
    }
}

public sealed record TypographyMapping(ImmutableDictionary<DesignTokenKey, FontMapping> Values);
public sealed record ColorTokenMapping(ImmutableDictionary<ThemeMode, ImmutableDictionary<DesignTokenKey, string>> Values);
public sealed record SpacingProfile(ImmutableDictionary<DesignTokenKey, double> Values);
public sealed record SizingProfile(ImmutableDictionary<DesignTokenKey, double> Values);
public sealed record RadiusProfile(ImmutableDictionary<DesignTokenKey, double> Values);
public sealed record StrokeProfile(ImmutableDictionary<DesignTokenKey, double> Values);
public sealed record ElevationProfile(ImmutableDictionary<DesignTokenKey, string> Values);
public sealed record OpacityProfile(ImmutableDictionary<DesignTokenKey, double> Values);
public sealed record IconTreatmentProfile(ImmutableDictionary<DesignTokenKey, string> Values);
public sealed record MotionRecipe(double DurationMilliseconds, string Easing, bool HasReducedMotionMapping);
public sealed record MotionProfile(ImmutableDictionary<DesignTokenKey, MotionRecipe> Values);
public sealed record DensityProfile(DensityRole DefaultDensity,
    ImmutableDictionary<DensityRole, ImmutableDictionary<DesignTokenKey, double>> Values);

/// <summary>Approved visual-expression mappings. It contains no anatomy, commands, or authorization.</summary>
public sealed record ThemeMappings(
    TypographyMapping Typography,
    ColorTokenMapping Colors,
    SpacingProfile Spacing,
    SizingProfile Sizing,
    RadiusProfile Radius,
    StrokeProfile Stroke,
    ElevationProfile Elevation,
    OpacityProfile Opacity,
    IconTreatmentProfile Icons,
    MotionProfile Motion,
    DensityProfile Density,
    ImmutableDictionary<string, ImmutableHashSet<DesignTokenKey>> ComponentRecipeDependencies)
{
    public bool Contains(DesignTokenKey token) =>
        Typography.Values.ContainsKey(token) ||
        Colors.Values.Values.Any(values => values.ContainsKey(token)) ||
        Spacing.Values.ContainsKey(token) || Sizing.Values.ContainsKey(token) ||
        Radius.Values.ContainsKey(token) || Stroke.Values.ContainsKey(token) ||
        Elevation.Values.ContainsKey(token) || Opacity.Values.ContainsKey(token) ||
        Icons.Values.ContainsKey(token) || Motion.Values.ContainsKey(token) ||
        Density.Values.Values.Any(values => values.ContainsKey(token));
}

/// <summary>Immutable published Theme version. DisplayName is presentation, never identity.</summary>
public sealed record ThemeVersionDefinition(
    ThemeCode Code,
    ThemeVersion Version,
    string StandardVersion,
    string DisplayName,
    ThemeMappings Mappings,
    DateTimeOffset PublishedAt,
    string SafeChangeSummary);

public sealed class ThemeDraft
{
    public ThemeDraft(ThemeDraftId id, ThemeCode code, ThemeVersion basedOnVersion,
        ThemeGeneration generation, string standardVersion, string displayName, ThemeMappings mappings)
    {
        Id = id;
        Code = code;
        BasedOnVersion = basedOnVersion;
        Generation = generation;
        StandardVersion = string.IsNullOrWhiteSpace(standardVersion)
            ? throw new ArgumentException("Standard version is required.", nameof(standardVersion))
            : standardVersion;
        DisplayName = displayName;
        Mappings = mappings ?? throw new ArgumentNullException(nameof(mappings));
    }

    public ThemeDraftId Id { get; }
    public ThemeCode Code { get; }
    public ThemeVersion BasedOnVersion { get; }
    public ThemeGeneration Generation { get; private set; }
    public string StandardVersion { get; }
    public string DisplayName { get; private set; }
    public ThemeMappings Mappings { get; private set; }

    public void Update(string displayName, ThemeMappings mappings)
    {
        DisplayName = displayName;
        Mappings = mappings ?? throw new ArgumentNullException(nameof(mappings));
        Generation = new ThemeGeneration(checked(Generation.Value + 1));
    }

    public ThemeDraftSnapshot Snapshot() => new(Id, Code, BasedOnVersion, Generation,
        StandardVersion, DisplayName, Mappings);
}

public sealed record ThemeDraftSnapshot(ThemeDraftId Id, ThemeCode Code, ThemeVersion BasedOnVersion,
    ThemeGeneration Generation, string StandardVersion, string DisplayName, ThemeMappings Mappings);

public enum ThemeValidationSeverity { Info, Warning, Error }
public sealed record ThemeValidationDiagnostic(ThemeValidationSeverity Severity, string Code,
    DesignTokenKey? Token = null, string? SafeMessage = null);
public sealed record ThemeValidationResult(ImmutableArray<ThemeValidationDiagnostic> Diagnostics)
{
    public bool CanPublish => !Diagnostics.Any(item => item.Severity == ThemeValidationSeverity.Error);
}

public interface IThemeValidationPolicy
{
    string StandardVersion { get; }
    IReadOnlySet<DesignTokenKey> RequiredColorTokens { get; }
    IReadOnlySet<DesignTokenKey> RequiredTypographyTokens { get; }
    double MinimumHitTarget { get; }
    bool IsFontMappingSupported(FontMapping mapping);
    IEnumerable<ThemeValidationDiagnostic> ValidateAccessibility(ThemeDraftSnapshot draft);
}

public sealed class ThemeValidator(IThemeValidationPolicy policy)
{
    public ThemeValidationResult Validate(ThemeDraftSnapshot draft)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ThemeValidationDiagnostic>();
        if (!string.Equals(draft.StandardVersion, policy.StandardVersion, StringComparison.Ordinal))
            diagnostics.Add(Error("THEME_STANDARD_INCOMPATIBLE"));

        foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark })
        {
            if (!draft.Mappings.Colors.Values.TryGetValue(mode, out var colors))
            {
                diagnostics.Add(Error("THEME_APPEARANCE_MAPPING_MISSING"));
                continue;
            }

            foreach (var token in policy.RequiredColorTokens.Where(token => !colors.ContainsKey(token)))
                diagnostics.Add(Error("THEME_COLOR_TOKEN_MISSING", token));
        }

        foreach (var token in policy.RequiredTypographyTokens)
        {
            if (!draft.Mappings.Typography.Values.TryGetValue(token, out var mapping))
                diagnostics.Add(Error("THEME_TYPOGRAPHY_TOKEN_MISSING", token));
            else if (!policy.IsFontMappingSupported(mapping))
                diagnostics.Add(Error("THEME_FONT_MAPPING_UNSUPPORTED", token));
        }

        ValidateNonNegative(draft.Mappings.Spacing.Values, "THEME_SPACING_INVALID", diagnostics);
        ValidateNonNegative(draft.Mappings.Sizing.Values, "THEME_SIZING_INVALID", diagnostics);
        ValidateNonNegative(draft.Mappings.Radius.Values, "THEME_RADIUS_INVALID", diagnostics);
        ValidateNonNegative(draft.Mappings.Stroke.Values, "THEME_STROKE_INVALID", diagnostics);
        foreach (var (token, value) in draft.Mappings.Opacity.Values.Where(pair => pair.Value is < 0 or > 1))
            diagnostics.Add(Error("THEME_OPACITY_INVALID", token));
        foreach (var colors in draft.Mappings.Colors.Values.Values)
        foreach (var token in colors.Where(pair => string.IsNullOrWhiteSpace(pair.Value)).Select(pair => pair.Key))
            diagnostics.Add(Error("THEME_COLOR_RECIPE_INVALID", token));
        foreach (var token in draft.Mappings.Elevation.Values
                     .Where(pair => string.IsNullOrWhiteSpace(pair.Value)).Select(pair => pair.Key))
            diagnostics.Add(Error("THEME_ELEVATION_RECIPE_INVALID", token));
        foreach (var token in draft.Mappings.Icons.Values
                     .Where(pair => string.IsNullOrWhiteSpace(pair.Value)).Select(pair => pair.Key))
            diagnostics.Add(Error("THEME_ICON_RECIPE_INVALID", token));
        foreach (var values in draft.Mappings.Density.Values.Values)
            ValidateNonNegative(values, "THEME_DENSITY_INVALID", diagnostics);
        foreach (var (token, recipe) in draft.Mappings.Motion.Values)
        {
            if (recipe.DurationMilliseconds < 0 || string.IsNullOrWhiteSpace(recipe.Easing))
                diagnostics.Add(Error("THEME_MOTION_INVALID", token));
            if (!recipe.HasReducedMotionMapping)
                diagnostics.Add(Error("THEME_REDUCED_MOTION_MISSING", token));
        }

        if (draft.Mappings.Sizing.Values.TryGetValue(DesignTokens.Size.HitTargetMinimum, out var hitTarget) &&
            hitTarget < policy.MinimumHitTarget)
            diagnostics.Add(Error("THEME_HIT_TARGET_BELOW_MINIMUM", DesignTokens.Size.HitTargetMinimum));

        ValidateEditorGeometry(draft.Mappings, policy.MinimumHitTarget, diagnostics);

        foreach (var (recipe, dependencies) in draft.Mappings.ComponentRecipeDependencies)
        foreach (var token in dependencies.Where(token => !draft.Mappings.Contains(token)))
            diagnostics.Add(new(ThemeValidationSeverity.Error, "THEME_RECIPE_DEPENDENCY_MISSING", token, recipe));

        diagnostics.AddRange(policy.ValidateAccessibility(draft));
        return new(diagnostics.ToImmutable());
    }

    private static ThemeValidationDiagnostic Error(string code, DesignTokenKey? token = null) =>
        new(ThemeValidationSeverity.Error, code, token);

    private static void ValidateNonNegative(ImmutableDictionary<DesignTokenKey, double> values, string code,
        ImmutableArray<ThemeValidationDiagnostic>.Builder diagnostics)
    {
        foreach (var token in values.Where(pair => pair.Value < 0).Select(pair => pair.Key))
            diagnostics.Add(Error(code, token));
    }

    private static void ValidateEditorGeometry(ThemeMappings mappings, double minimumHitTarget,
        ImmutableArray<ThemeValidationDiagnostic>.Builder diagnostics)
    {
        const double standardMinimumContentPadding = 8;
        var sizing = mappings.Sizing.Values;
        var spacing = mappings.Spacing.Values;
        var radius = mappings.Radius.Values;
        var stroke = mappings.Stroke.Values;
        if (sizing.TryGetValue(DesignTokens.Size.EditorControlHeight, out var height) && height < minimumHitTarget)
            diagnostics.Add(Error("THEME_EDITOR_HEIGHT_BELOW_MINIMUM", DesignTokens.Size.EditorControlHeight));
        if (sizing.TryGetValue(DesignTokens.Size.EditorIconSize, out var icon) && icon <= 0)
            diagnostics.Add(Error("THEME_EDITOR_ICON_INVALID", DesignTokens.Size.EditorIconSize));
        if (sizing.TryGetValue(DesignTokens.Size.EditorTrailingSlotWidth, out var slot) && sizing.TryGetValue(DesignTokens.Size.EditorIconSize, out var iconSize) && slot < iconSize)
            diagnostics.Add(Error("THEME_EDITOR_TRAILING_SLOT_INVALID", DesignTokens.Size.EditorTrailingSlotWidth));
        if (sizing.TryGetValue(DesignTokens.Size.EditorLeadingSlotWidth, out var leadingSlot) &&
            sizing.TryGetValue(DesignTokens.Size.MultiChoiceCheckSize, out var checkSize) &&
            spacing.TryGetValue(DesignTokens.Editor.MultiChoiceOptionGap, out var requiredPadding) &&
            leadingSlot < checkSize + requiredPadding)
            diagnostics.Add(Error("THEME_EDITOR_LEADING_CHECK_SLOT_INVALID", DesignTokens.Size.EditorLeadingSlotWidth));
        if (spacing.TryGetValue(DesignTokens.Editor.ContentPadding, out var contentPadding) &&
            contentPadding < standardMinimumContentPadding)
            diagnostics.Add(Error("THEME_EDITOR_CONTENT_PADDING_BELOW_MINIMUM", DesignTokens.Editor.ContentPadding));
        if (sizing.TryGetValue(DesignTokens.Size.PopupMaxHeight, out var maxHeight) && sizing.TryGetValue(DesignTokens.Size.PopupOptionHeight, out var optionHeight) && maxHeight <= optionHeight)
            diagnostics.Add(Error("THEME_POPUP_HEIGHT_INVALID", DesignTokens.Size.PopupMaxHeight));
        foreach (var token in new[] { DesignTokens.Size.EditorShort, DesignTokens.Size.EditorCompact, DesignTokens.Size.EditorMedium, DesignTokens.Size.EditorLong, DesignTokens.Size.EditorFill }.Where(token => sizing.TryGetValue(token, out var value) && value <= 0))
            diagnostics.Add(Error("THEME_EDITOR_WIDTH_INVALID", token));
        foreach (var token in new[] { DesignTokens.Editor.Radius, DesignTokens.Editor.PopupRadius }.Where(token => radius.TryGetValue(token, out var value) && value < 0))
            diagnostics.Add(Error("THEME_EDITOR_RADIUS_INVALID", token));
        foreach (var token in new[] { DesignTokens.Editor.BorderThickness, DesignTokens.Editor.PopupBorderThickness }.Where(token => stroke.TryGetValue(token, out var value) && value < 0))
            diagnostics.Add(Error("THEME_EDITOR_BORDER_INVALID", token));
        foreach (var token in new[] { DesignTokens.Editor.ContentPadding, DesignTokens.Editor.InlineGap, DesignTokens.Editor.PopupPadding, DesignTokens.Editor.MultiChoiceOptionGap }.Where(token => spacing.TryGetValue(token, out var value) && value < 0))
            diagnostics.Add(Error("THEME_EDITOR_SPACING_INVALID", token));
    }
}

public readonly record struct ThemePreviewSessionId(string Value);
public interface IThemePreviewSession
{
    ThemePreviewSessionId SessionId { get; }
    ThemeDraftSnapshot Draft { get; }
    ThemeTokenValue? Resolve(DesignTokenKey token, ThemeMode mode);
}

/// <summary>Draft-local token resolution; it never changes active/global resources.</summary>
public sealed class ThemePreviewSession(ThemePreviewSessionId sessionId, ThemeDraftSnapshot draft) : IThemePreviewSession
{
    public ThemePreviewSessionId SessionId { get; } = sessionId;
    public ThemeDraftSnapshot Draft { get; } = draft;

    public ThemeTokenValue? Resolve(DesignTokenKey token, ThemeMode mode)
    {
        if (Draft.Mappings.Colors.Values.TryGetValue(mode, out var colors) && colors.TryGetValue(token, out var color))
            return new(color);
        if (Draft.Mappings.Typography.Values.TryGetValue(token, out var font))
            return new(string.Join(',', new[] { font.PrimaryFamily }.Concat(font.FallbackFamilies)));
        if (Draft.Mappings.Spacing.Values.TryGetValue(token, out var spacing)) return new(spacing.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (Draft.Mappings.Sizing.Values.TryGetValue(token, out var sizing)) return new(sizing.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (Draft.Mappings.Radius.Values.TryGetValue(token, out var radius)) return new(radius.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (Draft.Mappings.Stroke.Values.TryGetValue(token, out var stroke)) return new(stroke.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (Draft.Mappings.Elevation.Values.TryGetValue(token, out var elevation)) return new(elevation);
        return null;
    }
}

public sealed record ThemeVersionInfo(ThemeCode Code, ThemeVersion Version, DateTimeOffset PublishedAt,
    string SafeChangeSummary, bool IsActive);
public sealed record ThemePublishRequest(ThemeDraftSnapshot Draft, ThemeVersion ExpectedActiveVersion,
    ThemeGeneration ExpectedRepositoryGeneration, string PublishRequestId, DateTimeOffset PublishedAt,
    string SafeChangeSummary, bool Activate);
public sealed record ThemePublishResult(ThemeVersionDefinition Definition, ThemeGeneration RepositoryGeneration,
    bool IsIdempotentReplay, bool IsActive);
public sealed record ThemeActivationRequest(ThemeCode Code, ThemeVersion Version,
    ThemeGeneration ExpectedRepositoryGeneration, string RequestId, bool IsRollback);
public sealed record ThemeActivationResult(ThemeCode Code, ThemeVersion Version,
    ThemeGeneration RepositoryGeneration, bool IsIdempotentReplay);

public interface IThemeLifecycleRepository
{
    ValueTask SaveDraftAsync(ThemeDraftSnapshot draft, ThemeGeneration expectedGeneration,
        CancellationToken cancellationToken = default);
    ValueTask<ThemeDraftSnapshot?> GetDraftAsync(ThemeDraftId id, CancellationToken cancellationToken = default);
    ValueTask<ThemeVersionDefinition?> GetActiveAsync(ThemeCode code, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyList<ThemeVersionInfo>> GetVersionHistoryAsync(ThemeCode code,
        CancellationToken cancellationToken = default);
    /// <summary>Validates expectations, allocates from retained history, appends immutably, and optionally activates atomically.</summary>
    ValueTask<ThemePublishResult> PublishAsync(ThemePublishRequest request, CancellationToken cancellationToken = default);
    /// <summary>Atomically activates a retained version; rollback never deletes newer history.</summary>
    ValueTask<ThemeActivationResult> ActivateAsync(ThemeActivationRequest request, CancellationToken cancellationToken = default);
}

public static class DesignSystemCapabilities
{
    public static CapabilityCode View => StandardUiCapabilities.CanViewDesignSystem;
    public static CapabilityCode EditDraft => StandardUiCapabilities.CanEditThemeDraft;
    public static CapabilityCode Preview => StandardUiCapabilities.CanPreviewTheme;
    public static CapabilityCode Publish => StandardUiCapabilities.CanPublishTheme;
    public static CapabilityCode Activate => StandardUiCapabilities.CanActivateTheme;
    public static CapabilityCode Rollback => StandardUiCapabilities.CanRollbackTheme;
    public static CapabilityCode MutateStandard => StandardUiCapabilities.CanMutateDesignSystemStandard;
}

public enum ThemeAuditEventKind { DraftCreated, Validated, PreviewRequested, Published, Activated, RolledBack }
public sealed record ThemeAuditEvent(ThemeAuditEventKind Kind, ThemeCode Code, ThemeVersion Version,
    DateTimeOffset Timestamp, string CorrelationId, string SafeSummary, string? SafeActorContext);
public interface IThemeAuditSink
{
    ValueTask WriteAsync(ThemeAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
public sealed class NullThemeAuditSink : IThemeAuditSink
{
    public ValueTask WriteAsync(ThemeAuditEvent auditEvent, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}

public sealed record ThemeLifecycleContext(UserSecurityContext Security, string? SafeActorContext = null);

public sealed class ThemeLifecycleService(IThemeLifecycleRepository repository, ThemeValidator validator,
    IThemeAuditSink? audit = null, TimeProvider? timeProvider = null)
{
    private readonly IThemeAuditSink audit = audit ?? new NullThemeAuditSink();
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async ValueTask<ThemeDraft> CreateDraftAsync(ThemeCode code, ThemeLifecycleContext context,
        CancellationToken cancellationToken = default)
    {
        Ensure(context, DesignSystemCapabilities.EditDraft);
        var active = await repository.GetActiveAsync(code, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("THEME_NOT_FOUND");
        var draft = new ThemeDraft(new(Guid.NewGuid().ToString("N")), active.Code, active.Version,
            new(0), active.StandardVersion, active.DisplayName, active.Mappings);
        await Write(ThemeAuditEventKind.DraftCreated, code, active.Version, draft.Id.Value,
            "Theme draft created", context, cancellationToken);
        return draft;
    }

    public async ValueTask<ThemeValidationResult> ValidateAsync(ThemeDraftSnapshot draft,
        ThemeLifecycleContext context, CancellationToken cancellationToken = default)
    {
        Ensure(context, DesignSystemCapabilities.EditDraft);
        var result = validator.Validate(draft);
        await Write(ThemeAuditEventKind.Validated, draft.Code, draft.BasedOnVersion, draft.Id.Value,
            result.CanPublish ? "Theme draft valid" : "Theme draft has blocking diagnostics", context, cancellationToken);
        return result;
    }

    public async ValueTask<IThemePreviewSession> PreviewAsync(ThemeDraftSnapshot draft,
        ThemeLifecycleContext context, CancellationToken cancellationToken = default)
    {
        Ensure(context, DesignSystemCapabilities.Preview);
        var result = validator.Validate(draft);
        if (!result.CanPublish) throw new InvalidOperationException("THEME_VALIDATION_FAILED");
        var session = new ThemePreviewSession(new(Guid.NewGuid().ToString("N")), draft);
        await Write(ThemeAuditEventKind.PreviewRequested, draft.Code, draft.BasedOnVersion,
            session.SessionId.Value, "Theme preview requested", context, cancellationToken);
        return session;
    }

    public async ValueTask<ThemePublishResult> PublishAsync(ThemeDraftSnapshot draft,
        ThemeGeneration expectedRepositoryGeneration, string requestId, string safeSummary,
        bool activate, ThemeLifecycleContext context, CancellationToken cancellationToken = default)
    {
        Ensure(context, DesignSystemCapabilities.Publish);
        if (activate) Ensure(context, DesignSystemCapabilities.Activate);
        var result = validator.Validate(draft);
        if (!result.CanPublish) throw new InvalidOperationException("THEME_VALIDATION_FAILED");
        var request = new ThemePublishRequest(draft, draft.BasedOnVersion, expectedRepositoryGeneration,
            RequiredRequestId(requestId), clock.GetUtcNow(), safeSummary, activate);
        var published = await repository.PublishAsync(request, cancellationToken).ConfigureAwait(false);
        await Write(ThemeAuditEventKind.Published, draft.Code, published.Definition.Version, request.PublishRequestId,
            safeSummary, context, cancellationToken);
        if (published.IsActive)
            await Write(ThemeAuditEventKind.Activated, draft.Code, published.Definition.Version,
                request.PublishRequestId, "Published Theme activated", context, cancellationToken);
        return published;
    }

    public async ValueTask<ThemeActivationResult> RollbackAsync(ThemeCode code, ThemeVersion version,
        ThemeGeneration expectedRepositoryGeneration, string requestId, ThemeLifecycleContext context,
        CancellationToken cancellationToken = default)
    {
        Ensure(context, DesignSystemCapabilities.Rollback);
        var result = await repository.ActivateAsync(new(code, version, expectedRepositoryGeneration,
            RequiredRequestId(requestId), IsRollback: true), cancellationToken).ConfigureAwait(false);
        await Write(ThemeAuditEventKind.RolledBack, code, version, requestId,
            "Previous Theme version activated", context, cancellationToken);
        return result;
    }

    public async ValueTask<ThemeActivationResult> ActivateAsync(ThemeCode code, ThemeVersion version,
        ThemeGeneration expectedRepositoryGeneration, string requestId, ThemeLifecycleContext context,
        CancellationToken cancellationToken = default)
    {
        Ensure(context, DesignSystemCapabilities.Activate);
        var correlationId = RequiredRequestId(requestId);
        var result = await repository.ActivateAsync(new(code, version, expectedRepositoryGeneration,
            correlationId, IsRollback: false), cancellationToken).ConfigureAwait(false);
        await Write(ThemeAuditEventKind.Activated, code, version, correlationId,
            "Published Theme activated", context, cancellationToken);
        return result;
    }

    private static void Ensure(ThemeLifecycleContext context, CapabilityCode capability)
    {
        if (!context.Security.Capabilities.Contains(capability))
            throw new UnauthorizedAccessException("DESIGN_SYSTEM_CAPABILITY_DENIED");
    }

    private static string RequiredRequestId(string value) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Request ID is required.", nameof(value))
        : value.Trim();

    private ValueTask Write(ThemeAuditEventKind kind, ThemeCode code, ThemeVersion version,
        string correlationId, string summary, ThemeLifecycleContext context, CancellationToken token) =>
        audit.WriteAsync(new(kind, code, version, clock.GetUtcNow(), correlationId, summary,
            context.SafeActorContext), token);
}
