namespace DynamicUI24.Shared.Presentation;

/// <summary>Stable semantic identity for a design-system value.</summary>
public readonly record struct DesignTokenKey
{
    public DesignTokenKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("A design token key must be non-empty and contain no whitespace.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

/// <summary>Categories owned by the stable presentation Standard.</summary>
public enum FoundationTokenCategory
{
    Typography,
    Spacing,
    Sizing,
    Radius,
    Stroke,
    Elevation,
    Motion,
    Opacity,
    IconGeometry,
}

public enum DensityRole { Compact, Standard, Comfortable }
public enum ButtonRole { Primary, Secondary, Tertiary, Danger, Icon, Split, Overflow }
public enum ComponentState { Normal, Hover, Pressed, Focused, Selected, Disabled, ReadOnly, Error, Warning, Loading }
public enum ContentState { Initial, Loading, Empty, FilteredEmpty, Unavailable, Offline, Unauthorized, Error, Partial, Ready }

/// <summary>Shared presentation primitives whose standards are owned by DynamicUI24.</summary>
public enum ComponentRole
{
    Button,
    UniversalEditor,
    Form,
    Grid,
    NavigationTree,
    Shell,
    Dashboard,
    Overview,
    ActionBar,
    Menu,
    Flyout,
    Pane,
    Notification,
    HelpValidation,
    Icon,
    ContentState,
}

/// <summary>Anatomy required for the Task 11B Navigation Tree standard.</summary>
public enum NavigationTreePart
{
    Row,
    Indentation,
    Chevron,
    Icon,
    Typography,
    ParentNode,
    LeafNode,
    Badge,
    ContextAction,
}

public enum EditorRole
{
    Text, Multiline, Integer, Decimal, Currency, Percentage, Boolean, Date, Time,
    DateTime, DateRange, Choice, Lookup, SearchLookup, MultiChoice, Password, Hyperlink, ButtonEdit,
}

public enum GridRole
{
    Header, Cell, InputCell, FormulaCell, SystemCell, GroupHeader, Footer,
    Selection, ActiveCell, Validation, Empty, Loading,
}

/// <summary>
/// Framework-owned semantic catalog. These identities express meaning and remain stable when
/// a theme generation, platform mapping, density, or appearance mode changes.
/// </summary>
public static class DesignTokens
{
    public static class Typography
    {
        public static readonly DesignTokenKey Display = new("Typography.Display");
        public static readonly DesignTokenKey PageTitle = new("Typography.PageTitle");
        public static readonly DesignTokenKey SectionTitle = new("Typography.SectionTitle");
        public static readonly DesignTokenKey Subtitle = new("Typography.Subtitle");
        public static readonly DesignTokenKey Body = new("Typography.Body");
        public static readonly DesignTokenKey BodySmall = new("Typography.BodySmall");
        public static readonly DesignTokenKey Caption = new("Typography.Caption");
        public static readonly DesignTokenKey Label = new("Typography.Label");
        public static readonly DesignTokenKey Button = new("Typography.Button");
        public static readonly DesignTokenKey Input = new("Typography.Input");
        public static readonly DesignTokenKey Grid = new("Typography.Grid");
        public static readonly DesignTokenKey GridHeader = new("Typography.GridHeader");
        public static readonly DesignTokenKey Menu = new("Typography.Menu");
        public static readonly DesignTokenKey Navigation = new("Typography.Navigation");
        public static readonly DesignTokenKey Code = new("Typography.Code");
    }

    public static class Space
    {
        public static readonly DesignTokenKey TwoExtraSmall = new("Space.2XS");
        public static readonly DesignTokenKey ExtraSmall = new("Space.XS");
        public static readonly DesignTokenKey Small = new("Space.S");
        public static readonly DesignTokenKey Medium = new("Space.M");
        public static readonly DesignTokenKey Large = new("Space.L");
        public static readonly DesignTokenKey ExtraLarge = new("Space.XL");
        public static readonly DesignTokenKey TwoExtraLarge = new("Space.2XL");
    }

    public static class Layout
    {
        public static readonly DesignTokenKey FormRowGap = new("Form.RowGap");
        public static readonly DesignTokenKey FormGroupGap = new("Form.GroupGap");
        public static readonly DesignTokenKey FormSectionGap = new("Form.SectionGap");
        public static readonly DesignTokenKey FormColumnGap = new("Form.ColumnGap");
        public static readonly DesignTokenKey FormLabelGap = new("Form.LabelGap");
        public static readonly DesignTokenKey PanePadding = new("Pane.Padding");
        public static readonly DesignTokenKey PaneHeaderGap = new("Pane.HeaderGap");
        public static readonly DesignTokenKey ToolbarItemGap = new("Toolbar.ItemGap");
        public static readonly DesignTokenKey GridCellPadding = new("Grid.CellPadding");
        public static readonly DesignTokenKey NavigationRowGap = new("Navigation.RowGap");
        public static readonly DesignTokenKey DialogSectionGap = new("Dialog.SectionGap");
    }

    public static class Size
    {
        public static readonly DesignTokenKey ControlCompact = new("Control.Height.Compact");
        public static readonly DesignTokenKey ControlStandard = new("Control.Height.Standard");
        public static readonly DesignTokenKey ControlLarge = new("Control.Height.Large");
        public static readonly DesignTokenKey EditorShort = new("Editor.Width.Short");
        public static readonly DesignTokenKey EditorCompact = new("Editor.Width.Compact");
        public static readonly DesignTokenKey EditorMedium = new("Editor.Width.Medium");
        public static readonly DesignTokenKey EditorLong = new("Editor.Width.Long");
        public static readonly DesignTokenKey EditorFill = new("Editor.Width.Fill");
        public static readonly DesignTokenKey IconSmall = new("Icon.Size.Small");
        public static readonly DesignTokenKey IconStandard = new("Icon.Size.Standard");
        public static readonly DesignTokenKey IconLarge = new("Icon.Size.Large");
        public static readonly DesignTokenKey HitTargetMinimum = new("HitTarget.Minimum");
        public static readonly DesignTokenKey FormReadableWidth = new("Form.ReadableWidth");
        public static readonly DesignTokenKey EditorControlHeight = new("Editor.ControlHeight.Standard");
        public static readonly DesignTokenKey EditorLeadingSlotWidth = new("Editor.LeadingSlot.Width");
        public static readonly DesignTokenKey EditorTrailingSlotWidth = new("Editor.TrailingSlot.Width");
        public static readonly DesignTokenKey EditorIconSize = new("Editor.Icon.Size");
        public static readonly DesignTokenKey EditorHelpIconSize = new("Editor.Help.IconSize");
        public static readonly DesignTokenKey PopupMaxHeight = new("Popup.MaxHeight");
        public static readonly DesignTokenKey PopupOptionHeight = new("Popup.OptionHeight");
        public static readonly DesignTokenKey MultiChoiceCheckSize = new("MultiChoice.Check.Size");
    }

    public static class Color
    {
        public static readonly DesignTokenKey SurfaceWindow = new("Surface.Window");
        public static readonly DesignTokenKey SurfaceWorkspace = new("Surface.Workspace");
        public static readonly DesignTokenKey SurfacePanel = new("Surface.Panel");
        public static readonly DesignTokenKey SurfaceEditor = new("Surface.Editor");
        public static readonly DesignTokenKey SurfaceSelected = new("Surface.Selected");
        public static readonly DesignTokenKey SurfaceHover = new("Surface.Hover");
        public static readonly DesignTokenKey TextPrimary = new("Text.Primary");
        public static readonly DesignTokenKey TextSecondary = new("Text.Secondary");
        public static readonly DesignTokenKey TextMuted = new("Text.Muted");
        public static readonly DesignTokenKey TextDisabled = new("Text.Disabled");
        public static readonly DesignTokenKey BorderDefault = new("Border.Default");
        public static readonly DesignTokenKey BorderSubtle = new("Border.Subtle");
        public static readonly DesignTokenKey BorderFocus = new("Border.Focus");
        public static readonly DesignTokenKey AccentPrimary = new("Accent.Primary");
        public static readonly DesignTokenKey AccentSecondary = new("Accent.Secondary");
        public static readonly DesignTokenKey StatusSuccess = new("Status.Success");
        public static readonly DesignTokenKey StatusWarning = new("Status.Warning");
        public static readonly DesignTokenKey StatusCritical = new("Status.Critical");
        public static readonly DesignTokenKey StatusInfo = new("Status.Info");
    }

    public static class Motion
    {
        public static readonly DesignTokenKey None = new("Motion.None");
        public static readonly DesignTokenKey Fast = new("Motion.Fast");
        public static readonly DesignTokenKey Standard = new("Motion.Standard");
        public static readonly DesignTokenKey Emphasized = new("Motion.Emphasized");
    }

    public static class Editor
    {
        public static readonly DesignTokenKey ContentPadding = new("Editor.ContentPadding");
        public static readonly DesignTokenKey InlineGap = new("Editor.InlineGap");
        public static readonly DesignTokenKey HelpGap = new("Editor.Help.Gap");
        public static readonly DesignTokenKey Radius = new("Editor.Radius");
        public static readonly DesignTokenKey BorderThickness = new("Editor.Border.Thickness");
        public static readonly DesignTokenKey PopupPadding = new("Popup.Padding");
        public static readonly DesignTokenKey PopupRadius = new("Popup.Radius");
        public static readonly DesignTokenKey PopupBorderThickness = new("Popup.Border.Thickness");
        public static readonly DesignTokenKey PopupElevation = new("Popup.Elevation");
        public static readonly DesignTokenKey MultiChoiceOptionGap = new("MultiChoice.OptionGap");
    }
}

/// <summary>
/// The framework Standard owns semantic anatomy and behavior, never concrete theme values.
/// </summary>
public interface IPresentationStandard
{
    string StandardVersion { get; }
    IReadOnlySet<FoundationTokenCategory> FoundationCategories { get; }
    IReadOnlySet<DensityRole> Densities { get; }
    IReadOnlySet<ComponentRole> ComponentRoles { get; }
    IReadOnlySet<ButtonRole> ButtonRoles { get; }
    IReadOnlySet<EditorRole> EditorRoles { get; }
    IReadOnlySet<GridRole> GridRoles { get; }
    IReadOnlySet<NavigationTreePart> NavigationTreeParts { get; }
}

/// <summary>Versioned, replaceable visual expression of the presentation Standard.</summary>
public interface IThemeDefinition
{
    string ThemeId { get; }
    string ThemeVersion { get; }
    string StandardVersion { get; }
    IReadOnlySet<ThemeMode> SupportedModes { get; }
    IReadOnlyDictionary<DesignTokenKey, ThemeTokenValue> Values { get; }
}

/// <summary>Vendor-neutral physical value supplied only by a theme definition.</summary>
public sealed record ThemeTokenValue(string Value, string? Platform = null);

/// <summary>Resolves a versioned theme without coupling application metadata to its values.</summary>
public interface IThemeResolver
{
    IThemeDefinition Resolve(string themeId, ThemeMode mode);
}

/// <summary>Framework registry for versioned themes; it owns no application or business state.</summary>
public sealed class ThemeResolver : IThemeResolver
{
    private readonly IReadOnlyDictionary<string, IThemeDefinition> themes;

    public ThemeResolver(IEnumerable<IThemeDefinition> themes)
    {
        ArgumentNullException.ThrowIfNull(themes);
        this.themes = themes.ToDictionary(theme => theme.ThemeId, StringComparer.OrdinalIgnoreCase);
        if (this.themes.Count == 0)
        {
            throw new ArgumentException("At least one theme definition is required.", nameof(themes));
        }
    }

    public IThemeDefinition Resolve(string themeId, ThemeMode mode)
    {
        if (string.IsNullOrWhiteSpace(themeId) || !themes.TryGetValue(themeId, out var theme))
        {
            throw new KeyNotFoundException($"Theme '{themeId}' is not registered.");
        }

        if (!theme.SupportedModes.Contains(mode))
        {
            throw new NotSupportedException($"Theme '{theme.ThemeId}' does not support {mode} mode.");
        }

        return theme;
    }
}

/// <summary>Immutable vendor-neutral theme registration.</summary>
public sealed record ThemeDefinition : IThemeDefinition
{
    public ThemeDefinition(
        string themeId,
        string themeVersion,
        string standardVersion,
        IEnumerable<ThemeMode> supportedModes,
        IReadOnlyDictionary<DesignTokenKey, ThemeTokenValue> values)
    {
        if (string.IsNullOrWhiteSpace(themeId)) throw new ArgumentException("Theme ID is required.", nameof(themeId));
        if (string.IsNullOrWhiteSpace(themeVersion)) throw new ArgumentException("Theme version is required.", nameof(themeVersion));
        if (string.IsNullOrWhiteSpace(standardVersion)) throw new ArgumentException("Standard version is required.", nameof(standardVersion));
        ArgumentNullException.ThrowIfNull(supportedModes);
        ArgumentNullException.ThrowIfNull(values);

        ThemeId = themeId;
        ThemeVersion = themeVersion;
        StandardVersion = standardVersion;
        SupportedModes = supportedModes.ToHashSet();
        Values = values;
    }

    public string ThemeId { get; }
    public string ThemeVersion { get; }
    public string StandardVersion { get; }
    public IReadOnlySet<ThemeMode> SupportedModes { get; }
    public IReadOnlyDictionary<DesignTokenKey, ThemeTokenValue> Values { get; }
}
