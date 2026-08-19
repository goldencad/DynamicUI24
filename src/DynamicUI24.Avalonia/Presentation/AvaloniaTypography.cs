using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Resolves and applies the platform UI font once for native and vendor visual trees.</summary>
public static class AvaloniaTypography
{
    public static FontFamily UiFontFamily { get; } =
        new(string.Join(",", AvaloniaPlatformFontMapping.UiFallbackStack));

    public static FontFamily CodeFontFamily { get; } =
        new(string.Join(",", AvaloniaPlatformFontMapping.CodeFallbackStack));
    public static string UiFamilyName => UiFontFamily.Name;

    public static void ApplyUiFont(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);
        root.Resources["DuiFontFamilyUi"] = UiFontFamily;
        TextElement.SetFontFamily(root, UiFontFamily);
    }
}
