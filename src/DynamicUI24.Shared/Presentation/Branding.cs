namespace DynamicUI24.Shared.Presentation;

public sealed record ApplicationBrand(
    string ApplicationName,
    IconKey ApplicationLogoKey,
    string? AccentColor = null)
{
    public static ApplicationBrand Default { get; } =
        new("DynamicUI24", StandardIconKeys.Settings);
}
