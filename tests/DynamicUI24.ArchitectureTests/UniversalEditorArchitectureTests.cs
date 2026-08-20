using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class UniversalEditorArchitectureTests
{
    private static readonly string Root = FindRoot();
    private static readonly string EditorRoot = Path.Combine(Root, "src", "DynamicUI24.Core", "Editors");

    [Fact]
    public void CoreEditorSurfaceIsVendorAndUiNeutral()
    {
        var source = ReadEditorSources();
        Assert.DoesNotContain("using Avalonia", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Actipro", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DevExpress", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Windows.Forms", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExactlyOneEditorResolverExists()
    {
        var count = Directory.GetFiles(Path.Combine(Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText).Count(text => text.Contains("class EditorResolver", StringComparison.Ordinal));
        Assert.Equal(1, count);
    }

    [Fact]
    public void EditorValueTypesAreGenericAndContainNoBusinessCatalog()
    {
        var source = ReadEditorSources();
        Assert.DoesNotContain("Salary", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TaxPeriod", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sql", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Formula", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LookupContractIsBoundedAndReturnsSemanticModels()
    {
        var source = File.ReadAllText(Path.Combine(EditorRoot, "EditorLookup.cs"));
        Assert.Contains("MaximumWindowSize = 200", source, StringComparison.Ordinal);
        Assert.Contains("ValueTask<EditorLookupResult>", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Control", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedEditorGeometryAndAffordanceAuthorityIsPresentationOwned()
    {
        var geometry = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation",
            "Editors", "EditorAffordanceGeometry.cs"));
        var tokens = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation",
            "Editors", "EditorPresentationTokens.cs"));
        var presenter = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation",
            "Editors", "AvaloniaEditorPresenter.cs"));

        Assert.Contains("class EditorAffordanceSlot", geometry, StringComparison.Ordinal);
        Assert.Contains("SemanticIcon", geometry, StringComparison.Ordinal);
        Assert.DoesNotContain("GlyphFor", geometry, StringComparison.Ordinal);
        Assert.Contains("TrailingSlotWidth", geometry, StringComparison.Ordinal);
        Assert.Contains("ControlHeight = 32", tokens, StringComparison.Ordinal);
        Assert.Contains("IconSize = 16", tokens, StringComparison.Ordinal);
        Assert.Contains("PopupMaxHeight = 240", tokens, StringComparison.Ordinal);
        Assert.Contains("CloseTransientSurfaces", presenter, StringComparison.Ordinal);
        Assert.Contains("EditorKind.Time => EditorThemeResources.WidthTime", presenter, StringComparison.Ordinal);
        Assert.Contains("PART_Button", presenter, StringComparison.Ordinal);
        Assert.Contains("StandardIconKeys.Calendar", presenter, StringComparison.Ordinal);
        Assert.Contains("DynamicCheckBoxPresentation.Apply", presenter, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportComboBox", presenter, StringComparison.Ordinal);
        Assert.DoesNotContain("DataEntryGrid", presenter, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorGeometryUsesThemeResourcesAndKeepsLifecycleOutsideTheme()
    {
        var adapter = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation",
            "Editors", "EditorThemeResources.cs"));
        var tokens = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation",
            "DesignTokens.axaml"));
        var lifecycle = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Core", "DesignSystem",
            "ThemeLifecycle.cs"));

        Assert.Contains("GetResourceObservable", adapter, StringComparison.Ordinal);
        Assert.Contains("DuiEditorControlHeight", tokens, StringComparison.Ordinal);
        Assert.Contains("DuiPopupMaxHeight", tokens, StringComparison.Ordinal);
        Assert.Contains("THEME_EDITOR_TRAILING_SLOT_INVALID", lifecycle, StringComparison.Ordinal);
        Assert.Contains("THEME_POPUP_HEIGHT_INVALID", lifecycle, StringComparison.Ordinal);
        Assert.DoesNotContain("CloseTransientSurfaces", lifecycle, StringComparison.Ordinal);
        Assert.DoesNotContain("Avalonia", lifecycle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultiChoiceUsesOneSharedOptionRowAndTimeUsesOneCanonicalClockIdentity()
    {
        var presenter = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation",
            "Editors", "AvaloniaEditorPresenter.cs"));
        var affordance = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation",
            "Editors", "EditorAffordanceGeometry.cs"));

        Assert.Contains("MULTICHOICE_OPTION_ROW", presenter, StringComparison.Ordinal);
        Assert.Contains("DynamicCheckBoxPresentation.Apply", presenter, StringComparison.Ordinal);
        Assert.Contains("LeadingSlotWidth", presenter, StringComparison.Ordinal);
        Assert.Contains("StandardIconKeys.Clock", affordance, StringComparison.Ordinal);
        Assert.DoesNotContain("new TextBlock { Text = \"◷\"", presenter, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedCheckBoxControlThemeOwnsOneBoxAndCatalogStateIcons()
    {
        var tokens = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation",
            "DesignTokens.axaml"));
        var presenter = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation",
            "Editors", "AvaloniaEditorPresenter.cs"));

        Assert.Contains("x:Key=\"DuiCheckBoxTheme\"", tokens, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(tokens, "x:Name=\"BoxSurface\"")
            .Cast<System.Text.RegularExpressions.Match>());
        Assert.Contains("SemanticKey=\"CHECK\"", tokens, StringComparison.Ordinal);
        Assert.Contains("SemanticKey=\"INDETERMINATE\"", tokens, StringComparison.Ordinal);
        Assert.Contains("DynamicCheckBoxPresentation.Apply(check)", presenter, StringComparison.Ordinal);
        Assert.DoesNotContain("dui-multichoice-native-check", presenter, StringComparison.Ordinal);
        Assert.DoesNotContain("MeasureAdjuster", tokens, StringComparison.Ordinal);
    }

    private static string ReadEditorSources() => string.Join('\n', Directory.GetFiles(EditorRoot, "*.cs").Select(File.ReadAllText));
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DynamicUI24.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
