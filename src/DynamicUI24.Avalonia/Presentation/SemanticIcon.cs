using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

public sealed class SemanticIcon : ContentControl
{
    public IconSource? ResolvedSource { get; private set; }

    public void SetIcon(IIconRegistry registry, IconKey key)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ResolvedSource = registry.Resolve(key).Source;
        Content = ResolvedSource switch
        {
            SvgIconSource svg => Svg(svg),
            FontGlyphIconSource glyph => Glyph(glyph),
            _ => null,
        };
    }

    private PathIcon Svg(SvgIconSource source)
    {
        var size = EffectiveSize();
        var icon = new PathIcon { Data = Geometry.Parse(source.PathData), Width = size, Height = size };
        icon.Bind(PathIcon.ForegroundProperty, this.GetObservable(ForegroundProperty));
        return icon;
    }

    private TextBlock Glyph(FontGlyphIconSource source)
    {
        var icon = new TextBlock { Text = source.Glyph, FontFamily = new FontFamily(source.FontFamily),
            FontSize = EffectiveSize(),
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center };
        icon.Bind(TextBlock.ForegroundProperty, this.GetObservable(ForegroundProperty));
        return icon;
    }

    private double EffectiveSize()
    {
        if (!double.IsNaN(Height) && Height > 0) return Height;
        if (!double.IsNaN(Width) && Width > 0) return Width;
        return 16;
    }
}
