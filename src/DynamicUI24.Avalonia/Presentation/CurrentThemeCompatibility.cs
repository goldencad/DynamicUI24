using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>
/// Temporary v0.16 bridge from semantic identities to the accepted pre-v0.16 Avalonia
/// resources. Later retrofit phases can move consumers without changing application metadata.
/// </summary>
public static class CurrentThemeCompatibility
{
    public const string ThemeId = "DynamicUI24.Current";

    public static IReadOnlyDictionary<DesignTokenKey, string> ResourceKeys { get; } =
        new Dictionary<DesignTokenKey, string>
        {
            [DesignTokens.Color.SurfaceWindow] = "DuiSurfaceBrush",
            [DesignTokens.Color.SurfaceWorkspace] = "DuiSurfaceBrush",
            [DesignTokens.Color.SurfacePanel] = "DuiSurfaceRaisedBrush",
            [DesignTokens.Color.SurfaceEditor] = "DuiSurfaceRaisedBrush",
            [DesignTokens.Color.SurfaceSelected] = "DuiSelectionBrush",
            [DesignTokens.Color.SurfaceHover] = "DuiHoverBrush",
            [DesignTokens.Color.TextPrimary] = "DuiTextBrush",
            [DesignTokens.Color.TextSecondary] = "DuiTextMutedBrush",
            [DesignTokens.Color.TextMuted] = "DuiTextMutedBrush",
            [DesignTokens.Color.TextDisabled] = "DuiDisabledBrush",
            [DesignTokens.Color.BorderDefault] = "DuiBorderBrush",
            [DesignTokens.Color.BorderSubtle] = "DuiGridBrush",
            [DesignTokens.Color.BorderFocus] = "DuiFocusBrush",
            [DesignTokens.Color.AccentPrimary] = "DuiAccentBrush",
            [DesignTokens.Color.AccentSecondary] = "DuiAccentBrush",
            [DesignTokens.Color.StatusSuccess] = "DuiSuccessBrush",
            [DesignTokens.Color.StatusWarning] = "DuiWarningBrush",
            [DesignTokens.Color.StatusCritical] = "DuiErrorBrush",
            [DesignTokens.Color.StatusInfo] = "DuiInfoBrush",
            [DesignTokens.Typography.Caption] = "DuiTypographyCaption",
            [DesignTokens.Typography.Label] = "DuiTypographyLabel",
            [DesignTokens.Typography.Body] = "DuiTypographyBody",
            [DesignTokens.Typography.PageTitle] = "DuiTypographyTitle",
            [DesignTokens.Space.ExtraSmall] = "DuiSpacingXs",
            [DesignTokens.Space.Small] = "DuiSpacingSmall",
            [DesignTokens.Space.Medium] = "DuiSpacingMedium",
            [DesignTokens.Space.Large] = "DuiSpacingLarge",
        };

    /// <summary>Registers the accepted current visual generation with the v0.16 resolver.</summary>
    public static IThemeDefinition CreateThemeDefinition() => new ThemeDefinition(
        ThemeId,
        themeVersion: "1",
        DefaultPresentationStandard.Version,
        Enum.GetValues<ThemeMode>(),
        ResourceKeys.ToDictionary(
            pair => pair.Key,
            pair => new ThemeTokenValue(pair.Value, "AvaloniaResource")));
}
