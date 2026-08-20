namespace DynamicUI24.Core.DataEntry;

public enum GridGeometryRole { None, Minimal, Compact, Standard, Comfortable }
public enum GridHeightMode { FitWorkspace, Compact, Standard, Expanded, FixedSemanticHeight }
public enum GridViewportProfile { Compact, Standard, Large, MaximumWorkspace }
public enum GridActionsPlacement { TopInline, TopToolbar, Overflow }
public enum GridNavigationPlacement { TopInline, Bottom, Auto }
public enum GridActionsAlignment { Start, End }

/// <summary>Vendor-neutral application metadata. Themes map roles to physical values.</summary>
public sealed record GridOuterInset(
    GridGeometryRole Left = GridGeometryRole.Minimal,
    GridGeometryRole Top = GridGeometryRole.Minimal,
    GridGeometryRole Right = GridGeometryRole.Minimal,
    GridGeometryRole Bottom = GridGeometryRole.Minimal);

public sealed record GridPresentationConfiguration(
    GridGeometryRole Preset = GridGeometryRole.Minimal,
    GridHeightMode HeightMode = GridHeightMode.FitWorkspace,
    GridViewportProfile ViewportProfile = GridViewportProfile.Standard,
    GridOuterInset? OuterInset = null,
    GridGeometryRole RowHeaderWidth = GridGeometryRole.Standard,
    double? RowHeaderWidthOverride = null,
    GridGeometryRole HeaderHeight = GridGeometryRole.Standard,
    GridGeometryRole ScrollbarClearance = GridGeometryRole.Minimal,
    GridActionsAlignment GridActionsAlignment = GridActionsAlignment.Start,
    GridActionsPlacement ActionsPlacement = GridActionsPlacement.TopInline,
    GridNavigationPlacement NavigationPlacement = GridNavigationPlacement.Auto,
    GridGeometryRole FixedHeightRole = GridGeometryRole.Standard,
    bool AllowFixedHeightBeyondWorkspace = false,
    bool RowNumbersCanBeShown = true,
    bool RowNumbersShownByDefault = true,
    GridGeometryRole Density = GridGeometryRole.Standard)
{
    public GridOuterInset EffectiveOuterInset => OuterInset ?? new(Preset, Preset, Preset, Preset);

    public static GridPresentationConfiguration ForPreset(GridGeometryRole preset) => preset switch
    {
        GridGeometryRole.None => new(preset, RowHeaderWidth: GridGeometryRole.Minimal,
            HeaderHeight: GridGeometryRole.Minimal, ScrollbarClearance: GridGeometryRole.None,
            Density: GridGeometryRole.Minimal),
        GridGeometryRole.Minimal => new(preset, RowHeaderWidth: GridGeometryRole.Standard,
            HeaderHeight: GridGeometryRole.Compact, ScrollbarClearance: GridGeometryRole.Minimal,
            Density: GridGeometryRole.Compact),
        GridGeometryRole.Compact => new(preset, RowHeaderWidth: GridGeometryRole.Compact,
            HeaderHeight: GridGeometryRole.Compact, ScrollbarClearance: GridGeometryRole.Compact,
            Density: GridGeometryRole.Compact),
        GridGeometryRole.Standard => new(preset),
        GridGeometryRole.Comfortable => new(preset, RowHeaderWidth: GridGeometryRole.Comfortable,
            HeaderHeight: GridGeometryRole.Comfortable, ScrollbarClearance: GridGeometryRole.Comfortable,
            Density: GridGeometryRole.Comfortable),
        _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown semantic Grid preset."),
    };

    public void Validate()
    {
        ValidateEnum(Preset, nameof(Preset));
        ValidateEnum(HeightMode, nameof(HeightMode));
        ValidateEnum(ViewportProfile, nameof(ViewportProfile));
        ValidateEnum(RowHeaderWidth, nameof(RowHeaderWidth));
        ValidateEnum(HeaderHeight, nameof(HeaderHeight));
        ValidateEnum(ScrollbarClearance, nameof(ScrollbarClearance));
        ValidateEnum(GridActionsAlignment, nameof(GridActionsAlignment));
        ValidateEnum(ActionsPlacement, nameof(ActionsPlacement));
        ValidateEnum(NavigationPlacement, nameof(NavigationPlacement));
        ValidateEnum(FixedHeightRole, nameof(FixedHeightRole));
        ValidateEnum(Density, nameof(Density));
        var inset = EffectiveOuterInset;
        ValidateEnum(inset.Left, "Grid.OuterInset.Left");
        ValidateEnum(inset.Top, "Grid.OuterInset.Top");
        ValidateEnum(inset.Right, "Grid.OuterInset.Right");
        ValidateEnum(inset.Bottom, "Grid.OuterInset.Bottom");
        if (!RowNumbersCanBeShown && RowNumbersShownByDefault)
            throw new ArgumentException("Row numbers cannot default to visible when the capability is disabled.", nameof(RowNumbersShownByDefault));
        if (RowHeaderWidth == GridGeometryRole.None && RowNumbersCanBeShown)
            throw new ArgumentException("A visible row-header capability requires a readable row-header width.", nameof(RowHeaderWidth));
        if (RowHeaderWidthOverride is { } width && (double.IsNaN(width) || double.IsInfinity(width) || width < 72 || width > 240))
            throw new ArgumentOutOfRangeException(nameof(RowHeaderWidthOverride), "RowHeader override must preserve content and hit-target bounds.");
        if (HeightMode != GridHeightMode.FixedSemanticHeight && AllowFixedHeightBeyondWorkspace)
            throw new ArgumentException("Workspace overflow permission applies only to FixedSemanticHeight.", nameof(AllowFixedHeightBeyondWorkspace));
    }

    private static void ValidateEnum<T>(T value, string name) where T : struct, Enum
    {
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(name, value, "Unknown semantic Grid role.");
    }
}
