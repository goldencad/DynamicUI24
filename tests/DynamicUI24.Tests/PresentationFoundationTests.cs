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
