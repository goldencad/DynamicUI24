using Avalonia.Controls;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class ThemeFoundationTests
{
    [Theory]
    [InlineData(ThemeMode.System)]
    [InlineData(ThemeMode.Light)]
    [InlineData(ThemeMode.Dark)]
    public void ShellAcceptsEverySupportedTheme(ThemeMode theme)
    {
        var shell = new ShellPresentation(ApplicationBrand.Default) { Theme = theme };

        Assert.Equal(theme, shell.Theme);
    }

    [Fact]
    public void RuntimeThemeSwitchPreservesWorkspaceAndState()
    {
        var shell = CreateSelectedShell();
        shell.State = PresentationState.For(PresentationStateKind.ReadOnly);

        shell.Theme = ThemeMode.Dark;

        Assert.Equal("report-workspace", shell.CurrentWorkspaceId);
        Assert.Equal("REPORT", shell.CurrentWorkspaceTitle);
        Assert.Equal(PresentationStateKind.ReadOnly, shell.State.Kind);
    }

    [Fact]
    public void AvaloniaThemeServiceSwitchesAllModesWithoutRestart()
    {
        var application = new global::Avalonia.Application();
        var service = new AvaloniaThemeService(application);

        foreach (var theme in Enum.GetValues<ThemeMode>())
        {
            service.SetTheme(theme);
            Assert.Equal(theme, service.Current);
        }
    }

    private static ShellPresentation CreateSelectedShell() => new(ApplicationBrand.Default)
    {
        CurrentWorkspaceId = "report-workspace",
        CurrentWorkspaceTitle = "REPORT",
    };
}

public sealed class LocalizationFoundationTests
{
    [Theory]
    [InlineData("vi-VN", "Không gian làm việc")]
    [InlineData("en-US", "Workspace")]
    public void SupportedCulturesResolveAtRuntime(string culture, string expected)
    {
        var localization = new DictionaryLocalizationService();

        Assert.True(localization.TrySetCulture(culture));

        Assert.Equal(expected, localization.Get(new("Shell.Workspace")));
    }

    [Fact]
    public void MissingKeyUsesDeterministicFallback()
    {
        var localization = new DictionaryLocalizationService("en-US");

        Assert.Equal("[Missing.Key]", localization.Get(new("Missing.Key")));
    }

    [Fact]
    public void TechnicalTemplateCodeIsNotTranslated()
    {
        var localization = new DictionaryLocalizationService();
        const string templateCode = "DATA_ENTRY";

        localization.TrySetCulture("en-US");

        Assert.Equal("DATA_ENTRY", templateCode);
        Assert.Equal("[DATA_ENTRY]", localization.Get(new(templateCode)));
    }

    [Fact]
    public void CultureSwitchPreservesWorkspace()
    {
        var shell = new ShellPresentation(ApplicationBrand.Default)
        {
            CurrentWorkspaceId = "setup-workspace",
            CurrentWorkspaceTitle = "SETUP",
        };

        shell.CultureName = "en-US";

        Assert.Equal("setup-workspace", shell.CurrentWorkspaceId);
        Assert.Equal("SETUP", shell.CurrentWorkspaceTitle);
    }
}

public sealed class IconRegistryTests
{
    [Fact]
    public void KnownKeyResolves()
    {
        var icon = new SemanticIconRegistry().Resolve(StandardIconKeys.Search);

        Assert.False(icon.IsFallback);
        Assert.NotEmpty(icon.SvgPathData);
    }

    [Fact]
    public void ConsumerCanOverrideStandardKey()
    {
        var registry = new SemanticIconRegistry();
        var replacement = new IconDefinition(StandardIconKeys.Export, "M1,1 L9,9");

        registry.Register(replacement, replace: true);

        Assert.Same(replacement, registry.Resolve(StandardIconKeys.Export));
    }

    [Fact]
    public void ConsumerCanRegisterCustomKey()
    {
        var registry = new SemanticIconRegistry();
        var custom = new IconDefinition(new("CONSUMER_CUSTOM"), "M2,2 L8,8");

        registry.Register(custom);

        Assert.Same(custom, registry.Resolve(new("consumer_custom")));
    }

    [Fact]
    public void UnknownKeyUsesSafeDeterministicFallback()
    {
        var registry = new SemanticIconRegistry();

        var first = registry.Resolve(new("UNKNOWN_ONE"));
        var second = registry.Resolve(new("UNKNOWN_TWO"));

        Assert.True(first.IsFallback);
        Assert.Same(first, second);
    }

    [Fact]
    public void RegistrySupportsSvgResourcesAndFontGlyphSourcesBehindIconKey()
    {
        var registry = new SemanticIconRegistry();
        var svg = new IconDefinition(new("SVG_RESOURCE"), new SvgIconSource("M1,1 L9,9", "icons/sample.svg"));
        var glyph = new IconDefinition(new("FONT_GLYPH"), new FontGlyphIconSource("★", ".AppleSystemUIFont"));
        registry.Register(svg);
        registry.Register(glyph);
        Assert.Equal("icons/sample.svg", Assert.IsType<SvgIconSource>(registry.Resolve(new("SVG_RESOURCE")).Source).ResourceName);
        Assert.Equal("★", Assert.IsType<FontGlyphIconSource>(registry.Resolve(new("FONT_GLYPH")).Source).Glyph);
        Assert.Empty(registry.Resolve(new("FONT_GLYPH")).SvgPathData);
    }

    [Fact]
    public void CanonicalCatalogMapsRequiredSemanticKeysToSvgAssets()
    {
        var registry = new SemanticIconRegistry();
        var required = new[] { StandardIconKeys.Clock, StandardIconKeys.Calendar, StandardIconKeys.ChevronDown,
            StandardIconKeys.Search, StandardIconKeys.Help, StandardIconKeys.Clear, StandardIconKeys.Reveal,
            StandardIconKeys.OpenBrowse, StandardIconKeys.More, StandardIconKeys.Check };

        Assert.All(required, key =>
        {
            var source = Assert.IsType<SvgIconSource>(registry.Resolve(key).Source);
            Assert.StartsWith("Assets/Icons/", source.ResourceName, StringComparison.Ordinal);
            Assert.NotEmpty(source.PathData);
        });

        var check = Assert.IsType<SvgIconSource>(registry.Resolve(StandardIconKeys.Check).Source);
        Assert.Equal(SvgPaintMode.Stroke, check.PaintMode);
        Assert.Equal(1.75, check.StrokeWidth);
        Assert.True(check.RoundLineCap);
        Assert.True(check.RoundLineJoin);
        Assert.Equal(SvgPaintMode.Fill,
            Assert.IsType<SvgIconSource>(registry.Resolve(StandardIconKeys.More).Source).PaintMode);
    }

    [Fact]
    public void ReplacingCatalogClockAssetChangesEverySemanticConsumerWithoutPresenterChanges()
    {
        static Stream Assets(string clockPath) => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor'><path d='{clockPath}'/></svg>"));
        var first = new SemanticIconRegistry(new DynamicUI24IconCatalog(_ => Assets("M2 2 L22 22")));
        var replacement = new SemanticIconRegistry(new DynamicUI24IconCatalog(_ => Assets("M2 22 L22 2")));
        var firstConsumer = new SemanticIcon();
        var secondConsumer = new SemanticIcon();

        firstConsumer.SetIcon(first, StandardIconKeys.Clock);
        secondConsumer.SetIcon(replacement, StandardIconKeys.Clock);

        Assert.IsType<Viewbox>(firstConsumer.Content);
        Assert.Equal(SvgPaintMode.Stroke, Assert.IsType<SvgIconSource>(firstConsumer.ResolvedSource).PaintMode);
        Assert.NotEqual(Assert.IsType<SvgIconSource>(firstConsumer.ResolvedSource).PathData,
            Assert.IsType<SvgIconSource>(secondConsumer.ResolvedSource).PathData);
    }

    [Fact]
    public void MissingRequiredCatalogAssetFailsInsteadOfFallingBackToFontGlyph()
    {
        var catalog = new DynamicUI24IconCatalog(file => throw new FileNotFoundException(file));
        Assert.Throws<FileNotFoundException>(() => new SemanticIconRegistry(catalog));
    }
}

public sealed class PresentationStateTests
{
    [Theory]
    [InlineData(PresentationStateKind.Empty)]
    [InlineData(PresentationStateKind.Loading)]
    [InlineData(PresentationStateKind.Error)]
    [InlineData(PresentationStateKind.ReadOnly)]
    [InlineData(PresentationStateKind.Unavailable)]
    public void RequiredStateIsExplicit(PresentationStateKind kind) =>
        Assert.Equal(kind, PresentationState.For(kind).Kind);

    [Fact]
    public void UnavailableIsDistinctFromEmptyAndAZeroValue()
    {
        var unavailable = PresentationState.For(PresentationStateKind.Unavailable);
        var empty = PresentationState.For(PresentationStateKind.Empty);
        const int legitimateValue = 0;

        Assert.NotEqual(empty, unavailable);
        Assert.Equal(0, legitimateValue);
        Assert.Equal(PresentationStateKind.Unavailable, unavailable.Kind);
    }

    [Fact]
    public void SafeErrorControlsDetailsAndRetryPresentation()
    {
        var error = new ErrorPresentation("Try again later.", "DUI-42", "developer detail", true);

        Assert.True(error.HasDetails);
        Assert.True(error.CanRetry);
        Assert.DoesNotContain("stack", error.FriendlyMessage, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class BrandingFoundationTests
{
    [Fact]
    public void FrameworkDefaultBrandIsAlwaysAvailable() =>
        Assert.Equal("DynamicUI24", ApplicationBrand.Default.ApplicationName);

    [Fact]
    public void ConsumerBrandOverridesNameLogoAndAccent()
    {
        var brand = new ApplicationBrand("Consumer App", new("CONSUMER_LOGO"), "#123456");
        var shell = new ShellPresentation(brand);

        Assert.Same(brand, shell.Brand);
        Assert.Equal("CONSUMER_LOGO", shell.Brand.ApplicationLogoKey.Value);
        Assert.Equal("#123456", shell.Brand.AccentColor);
    }
}
