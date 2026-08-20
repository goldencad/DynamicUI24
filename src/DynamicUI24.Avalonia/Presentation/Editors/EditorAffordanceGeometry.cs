using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation.Editors;

public enum EditorAffordanceKind { Calendar, Clock, Dropdown, Search, Clear, Reveal, Help, OpenBrowse }

/// <summary>One geometry law and one canonical vector renderer for editor affordances.</summary>
public sealed class EditorAffordanceSlot : Grid
{
    private static readonly SemanticIconRegistry Icons = new();

    public EditorAffordanceSlot(EditorAffordanceKind kind, string accessibleName)
    {
        Kind = kind;
        EditorThemeResources.Bind(this, WidthProperty, EditorThemeResources.TrailingSlotWidth);
        EditorThemeResources.Bind(this, HeightProperty, EditorThemeResources.ControlHeight);
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Center;
        IsHitTestVisible = false;
        var icon = new SemanticIcon
        {
            Width = EditorPresentationTokens.IconSize,
            Height = EditorPresentationTokens.IconSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        icon.SetIcon(Icons, SemanticIconKey);
        EditorThemeResources.Bind(icon, SemanticIcon.WidthProperty, EditorThemeResources.IconSize);
        EditorThemeResources.Bind(icon, SemanticIcon.HeightProperty, EditorThemeResources.IconSize);
        EditorThemeResources.Bind(icon, SemanticIcon.ForegroundProperty, EditorThemeResources.IconBrush);
        Children.Add(icon);
        AutomationProperties.SetName(this, accessibleName);
    }

    public EditorAffordanceKind Kind { get; }
    public IconKey SemanticIconKey => Kind switch
    {
        EditorAffordanceKind.Calendar => StandardIconKeys.Calendar,
        EditorAffordanceKind.Clock => StandardIconKeys.Clock,
        EditorAffordanceKind.Search => StandardIconKeys.Search,
        EditorAffordanceKind.Clear => StandardIconKeys.Clear,
        EditorAffordanceKind.Reveal => StandardIconKeys.Reveal,
        EditorAffordanceKind.Help => StandardIconKeys.Help,
        EditorAffordanceKind.OpenBrowse => StandardIconKeys.OpenBrowse,
        _ => StandardIconKeys.ChevronDown,
    };
}

public static class EditorSurfaceGeometry
{
    /// <summary>
    /// Builds the one and only physical editor surface.  The child is deliberately
    /// confined to the content column: it never paints beneath the trailing slot.
    /// </summary>
    public static EditorSurface WithTrailingAffordance(Control value, EditorAffordanceKind kind, string accessibleName)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new EditorSurface(value, kind, accessibleName);
    }
}

/// <summary>
/// Owns the complete editor outline, corner radius, background and trailing slot.
/// Native inputs live inside it with their own conflicting chrome neutralized.
/// </summary>
public sealed class EditorSurface : Border
{
    public EditorSurface(Control content, EditorAffordanceKind kind, string accessibleName)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Classes.Add("dui-editor-surface");
        Height = EditorPresentationTokens.ControlHeight;
        BorderThickness = new Thickness(EditorPresentationTokens.BorderThickness);
        CornerRadius = new CornerRadius(EditorPresentationTokens.CornerRadius);
        ClipToBounds = true;
        EditorThemeResources.Bind(this, HeightProperty, EditorThemeResources.ControlHeight);
        EditorThemeResources.Bind(this, BorderThicknessProperty, EditorThemeResources.BorderThicknessValue);
        EditorThemeResources.Bind(this, CornerRadiusProperty, EditorThemeResources.RadiusValue);
        EditorThemeResources.Bind(this, BackgroundProperty, EditorThemeResources.SurfaceBackground);
        EditorThemeResources.Bind(this, BorderBrushProperty, EditorThemeResources.SurfaceBorderBrush);

        var layout = new Grid
        {
            ColumnDefinitions = new("*,Auto"),
        };
        ContentHost = new Border { Child = Content };
        EditorThemeResources.Bind(ContentHost, PaddingProperty, EditorThemeResources.ContentPaddingValue);
        Grid.SetColumn(ContentHost, 0);
        layout.Children.Add(ContentHost);
        TrailingAffordance = new EditorAffordanceSlot(kind, accessibleName);
        Grid.SetColumn(TrailingAffordance, 1);
        layout.Children.Add(TrailingAffordance);
        Child = layout;
    }

    public Control Content { get; }
    public Border ContentHost { get; }
    public EditorAffordanceSlot TrailingAffordance { get; }
    public bool OwnsBorder => true;
}
