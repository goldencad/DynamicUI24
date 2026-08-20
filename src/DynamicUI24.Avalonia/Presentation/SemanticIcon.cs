using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

public sealed class SemanticIcon : ContentControl
{
    private static readonly SemanticIconRegistry DefaultRegistry = new();
    public static readonly StyledProperty<string?> SemanticKeyProperty =
        AvaloniaProperty.Register<SemanticIcon, string?>(nameof(SemanticKey));

    public IconSource? ResolvedSource { get; private set; }
    public string? SemanticKey { get => GetValue(SemanticKeyProperty); set => SetValue(SemanticKeyProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SemanticKeyProperty && change.NewValue is string value && !string.IsNullOrWhiteSpace(value))
            SetIcon(DefaultRegistry, new IconKey(value));
    }

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

    private Viewbox Svg(SvgIconSource source)
    {
        var size = EffectiveSize();
        var path = new global::Avalonia.Controls.Shapes.Path { Stretch = Stretch.None };
        path.AttachedToVisualTree += (_, _) => path.Data ??= Geometry.Parse(source.PathData);
        if (source.PaintMode == SvgPaintMode.Stroke)
        {
            path.StrokeThickness = source.StrokeWidth;
            path.StrokeLineCap = source.RoundLineCap ? PenLineCap.Round : PenLineCap.Flat;
            path.StrokeJoin = source.RoundLineJoin ? PenLineJoin.Round : PenLineJoin.Miter;
            path.Bind(Shape.StrokeProperty, this.GetObservable(ForegroundProperty));
        }
        else
        {
            path.Bind(Shape.FillProperty, this.GetObservable(ForegroundProperty));
        }

        var canvas = new Canvas { Width = 24, Height = 24, ClipToBounds = false };
        canvas.Children.Add(path);
        var icon = new Viewbox
        {
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both,
            Child = canvas,
        };
        icon.Bind(WidthProperty, this.GetObservable(WidthProperty));
        icon.Bind(HeightProperty, this.GetObservable(HeightProperty));
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
