namespace DynamicUI24.Core.ActionBars;

public enum ActionControlSizePreset { Xs, Small, Medium, Large, Xl }
public enum ActionTypographyToken { Caption, Label, Body, Title }
public enum ActionIconPosition { Left, Right, Top, Bottom, IconOnly }

public sealed record ActionControlPresetToken(double Height, double IconSize, double Gap,
    double PaddingHorizontal, double PaddingVertical);

public static class ActionControlTokenCatalog
{
    public static ActionControlPresetToken Resolve(ActionControlSizePreset preset) => preset switch
    {
        ActionControlSizePreset.Xs => new(24, 12, 4, 6, 2),
        ActionControlSizePreset.Small => new(28, 14, 5, 8, 4),
        ActionControlSizePreset.Large => new(38, 20, 8, 14, 8),
        ActionControlSizePreset.Xl => new(44, 24, 10, 18, 10),
        _ => new(32, 16, 6, 11, 6),
    };

    public static double Resolve(ActionTypographyToken token) => token switch
    {
        ActionTypographyToken.Caption => 11,
        ActionTypographyToken.Body => 14,
        ActionTypographyToken.Title => 16,
        _ => 13,
    };
}

/// <summary>UI-framework-neutral thickness metadata.</summary>
public sealed record ActionThickness
{
    public ActionThickness(double left, double top, double right, double bottom)
    {
        if (new[] { left, top, right, bottom }.Any(value =>
                double.IsNaN(value) || double.IsInfinity(value) || value is < 0 or > 48))
            throw new ArgumentOutOfRangeException(nameof(left), "Action padding values must be between 0 and 48.");
        Left = left; Top = top; Right = right; Bottom = bottom;
    }

    public ActionThickness(double horizontal, double vertical) : this(horizontal, vertical, horizontal, vertical) { }
    public double Left { get; }
    public double Top { get; }
    public double Right { get; }
    public double Bottom { get; }
}

/// <summary>Validated action geometry. Global UI/font scaling is deliberately applied later by the host.</summary>
public sealed record ActionControlGeometry
{
    public ActionControlGeometry(ActionControlSizePreset sizePreset = ActionControlSizePreset.Medium,
        double? width = null, double? minWidth = null, double? maxWidth = null, double? height = null,
        ActionTypographyToken typographyToken = ActionTypographyToken.Label, double? iconSize = null,
        ActionIconPosition iconPosition = ActionIconPosition.Left, ActionThickness? padding = null, double? gap = null)
    {
        Validate(width, 16, 640, nameof(width));
        Validate(minWidth, 16, 640, nameof(minWidth));
        Validate(maxWidth, 16, 640, nameof(maxWidth));
        Validate(height, 20, 96, nameof(height));
        Validate(iconSize, 8, 64, nameof(iconSize));
        Validate(gap, 0, 32, nameof(gap));
        if (minWidth > maxWidth) throw new ArgumentException("Minimum width cannot exceed maximum width.");
        if (width is { } explicitWidth && (explicitWidth < minWidth || explicitWidth > maxWidth))
            throw new ArgumentException("Width must remain within MinWidth and MaxWidth.");
        SizePreset = sizePreset; Width = width; MinWidth = minWidth; MaxWidth = maxWidth; Height = height;
        TypographyToken = typographyToken; IconSize = iconSize; IconPosition = iconPosition;
        Padding = padding; Gap = gap;
    }

    public ActionControlSizePreset SizePreset { get; }
    public double? Width { get; }
    public double? MinWidth { get; }
    public double? MaxWidth { get; }
    public double? Height { get; }
    public ActionTypographyToken TypographyToken { get; }
    public double? IconSize { get; }
    public ActionIconPosition IconPosition { get; }
    public ActionThickness? Padding { get; }
    public double? Gap { get; }

    private static void Validate(double? value, double minimum, double maximum, string parameter)
    {
        if (value is not null && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value < minimum || value > maximum))
            throw new ArgumentOutOfRangeException(parameter, $"Value must be between {minimum} and {maximum}.");
    }
}
