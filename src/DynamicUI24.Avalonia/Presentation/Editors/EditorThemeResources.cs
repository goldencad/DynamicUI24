using Avalonia;
using Avalonia.Controls;

namespace DynamicUI24.Avalonia.Presentation.Editors;

/// <summary>Presentation adapter from stable editor roles to mutable Avalonia theme resources.</summary>
public static class EditorThemeResources
{
    public const string ControlHeight = "DuiEditorControlHeight";
    public const string ContentPadding = "DuiEditorContentPadding";
    public const string ContentPaddingValue = "DuiEditorContentPaddingValue";
    public const string InlineGap = "DuiEditorInlineGap";
    public const string HelpGap = "DuiEditorHelpGap";
    public const string IconSize = "DuiEditorIconSize";
    public const string LeadingSlotWidth = "DuiEditorLeadingSlotWidth";
    public const string TrailingSlotWidth = "DuiEditorTrailingSlotWidth";
    public const string BorderThickness = "DuiEditorBorderThickness";
    public const string Radius = "DuiEditorRadius";
    public const string BorderThicknessValue = "DuiEditorBorderThicknessValue";
    public const string RadiusValue = "DuiEditorRadiusValue";
    public const string SurfaceBackground = "DuiSurfaceRaisedBrush";
    public const string SurfaceBorderBrush = "DuiBorderBrush";
    public const string FocusBorderBrush = "DuiFocusBrush";
    public const string IconBrush = "DuiEditorIconBrush";
    public const string PopupMaxHeight = "DuiPopupMaxHeight";
    public const string PopupPadding = "DuiPopupPadding";
    public const string PopupOptionHeight = "DuiPopupOptionHeight";
    public const string PopupBorderThickness = "DuiPopupBorderThickness";
    public const string PopupRadius = "DuiPopupRadius";
    public const string PopupElevation = "DuiPopupElevation";
    public const string MultiChoiceCheckSize = "DuiMultiChoiceCheckSize";
    public const string MultiChoiceOptionGap = "DuiMultiChoiceOptionGap";
    public const string WidthShort = "DuiEditorWidthShort";
    public const string WidthCompact = "DuiEditorWidthCompact";
    public const string WidthMedium = "DuiEditorWidthMedium";
    public const string WidthLong = "DuiEditorWidthLong";
    public const string WidthFill = "DuiFormReadableWidth";
    public const string WidthTime = "DuiEditorWidthTime";
    public const string WidthDateTime = "DuiEditorWidthDateTime";
    public const string WidthDateRange = "DuiEditorWidthDateRange";

    public static void Bind(Control control, AvaloniaProperty property, string key) =>
        control.Bind(property, control.GetResourceObservable(key));

    public static double Number(string key, double fallback) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is double number ? number : fallback;
}
