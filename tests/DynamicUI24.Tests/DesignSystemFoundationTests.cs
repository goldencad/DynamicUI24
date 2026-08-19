using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class DesignSystemFoundationTests
{
    [Fact]
    public void StandardAndThemeAreSeparateContracts()
    {
        Assert.False(typeof(IPresentationStandard).IsAssignableFrom(typeof(IThemeDefinition)));
        Assert.DoesNotContain(typeof(IThemeDefinition).GetProperties(), property =>
            property.PropertyType == typeof(IReadOnlySet<ButtonRole>) ||
            property.PropertyType == typeof(IReadOnlySet<EditorRole>));
    }

    [Fact]
    public void StandardPublishesCompleteFoundationAndComponentTaxonomy()
    {
        var standard = new DefaultPresentationStandard();

        Assert.Equal(Enum.GetValues<FoundationTokenCategory>().Length, standard.FoundationCategories.Count);
        Assert.Equal(Enum.GetValues<DensityRole>().Length, standard.Densities.Count);
        Assert.Equal(Enum.GetValues<ComponentRole>().Length, standard.ComponentRoles.Count);
        Assert.Contains(ButtonRole.Danger, standard.ButtonRoles);
        Assert.Contains(EditorRole.DateRange, standard.EditorRoles);
        Assert.Contains(GridRole.ActiveCell, standard.GridRoles);
        Assert.Contains(NavigationTreePart.ContextAction, standard.NavigationTreeParts);
    }

    [Fact]
    public void RequiredSemanticIdentitiesAreStableAndVendorNeutral()
    {
        Assert.Equal("Typography.GridHeader", DesignTokens.Typography.GridHeader.Value);
        Assert.Equal("Space.2XS", DesignTokens.Space.TwoExtraSmall.Value);
        Assert.Equal("Form.SectionGap", DesignTokens.Layout.FormSectionGap.Value);
        Assert.Equal("Control.Height.Standard", DesignTokens.Size.ControlStandard.Value);
        Assert.Equal("Surface.Window", DesignTokens.Color.SurfaceWindow.Value);
        Assert.Equal("Status.Critical", DesignTokens.Color.StatusCritical.Value);
        Assert.Equal("Motion.Emphasized", DesignTokens.Motion.Emphasized.Value);
    }

    [Fact]
    public void CurrentThemeResolvesEveryAppearanceModeWithoutOwningRuntimeState()
    {
        var definition = CurrentThemeCompatibility.CreateThemeDefinition();
        var resolver = new ThemeResolver([definition]);

        foreach (var mode in Enum.GetValues<ThemeMode>())
        {
            Assert.Same(definition, resolver.Resolve(CurrentThemeCompatibility.ThemeId, mode));
        }

        Assert.Equal(DefaultPresentationStandard.Version, definition.StandardVersion);
        Assert.DoesNotContain(typeof(ThemeResolver).GetProperties(), property =>
            property.Name.Contains("State", StringComparison.Ordinal));
    }

    [Fact]
    public void PlatformTypographyMappingIsFallbackSafe()
    {
        Assert.True(AvaloniaPlatformFontMapping.UiFallbackStack.Count >= 2);
        Assert.True(AvaloniaPlatformFontMapping.CodeFallbackStack.Count >= 2);
        Assert.Equal("sans-serif", AvaloniaPlatformFontMapping.UiFallbackStack[^1]);
        Assert.Equal("monospace", AvaloniaPlatformFontMapping.CodeFallbackStack[^1]);
    }

    [Fact]
    public void CurrentThemeCompatibilityCoversEveryRequiredColorRole()
    {
        var required = typeof(DesignTokens.Color).GetFields()
            .Select(field => Assert.IsType<DesignTokenKey>(field.GetValue(null)))
            .ToArray();

        Assert.All(required, token => Assert.True(CurrentThemeCompatibility.ResourceKeys.ContainsKey(token), token.Value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Space Bad")]
    public void InvalidTokenIdentityIsRejected(string value) =>
        Assert.Throws<ArgumentException>(() => new DesignTokenKey(value));
}
