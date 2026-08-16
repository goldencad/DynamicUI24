using System.Globalization;

namespace DynamicUI24.Core.Privacy;

public sealed record SensitiveValuePresentation(string DisplayValue, bool IsVisible, string AccessibleValue,
    string? TooltipValue, bool IsRevealed, bool CaptureProtectionRequested);

public interface ISensitiveValuePresenter
{
    SensitiveValuePresentation Present(object? value, SensitiveContentDefinition? metadata,
        ResolvedPrivacyPresentation resolved, CultureInfo? culture = null);
}

public sealed class SensitiveValuePresenter : ISensitiveValuePresenter
{
    public const string Mask = "••••••••";
    public SensitiveValuePresentation Present(object? value, SensitiveContentDefinition? metadata,
        ResolvedPrivacyPresentation resolved, CultureInfo? culture = null)
    {
        metadata ??= SensitiveContentDefinition.Normal;
        culture ??= CultureInfo.CurrentCulture;
        var raw = value is IFormattable formattable ? formattable.ToString(null, culture) ?? string.Empty : value?.ToString() ?? string.Empty;
        var display = resolved.Presentation switch
        {
            PrivacyPresentation.None => raw,
            PrivacyPresentation.PartialMask => Partial(raw, metadata.PartialMask),
            PrivacyPresentation.Hide => LocalizedHidden(culture),
            _ => Mask,
        };
        var visible = resolved.Presentation != PrivacyPresentation.Hide;
        var safeAccessible = resolved.CanExposeToAccessibility ? raw : LocalizedSensitiveHidden(culture);
        var tooltip = metadata.AllowTooltipRawValue && resolved.Presentation == PrivacyPresentation.None ? raw : null;
        return new(display, visible, safeAccessible, tooltip, resolved.Presentation == PrivacyPresentation.None &&
            metadata.Sensitivity != Sensitivity.Normal, resolved.CaptureProtectionRequested);
    }

    private static string Partial(string raw, PartialMaskDefinition? definition)
    {
        if (definition?.IsValid != true) return Mask;
        var prefix = raw[..Math.Min(definition.PreservePrefix, raw.Length)];
        var suffixStart = Math.Max(prefix.Length, raw.Length - definition.PreserveSuffix);
        return prefix + definition.MaskBody + raw[suffixStart..];
    }
    private static string LocalizedHidden(CultureInfo culture) => culture.Name.Equals("vi-VN", StringComparison.OrdinalIgnoreCase) ? "Đã ẩn" : "Hidden";
    private static string LocalizedSensitiveHidden(CultureInfo culture) => culture.Name.Equals("vi-VN", StringComparison.OrdinalIgnoreCase) ? "Giá trị nhạy cảm đã ẩn" : "Sensitive value hidden";
}
