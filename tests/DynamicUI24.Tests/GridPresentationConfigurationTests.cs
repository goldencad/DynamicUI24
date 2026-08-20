using DynamicUI24.Core.DataEntry;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class GridPresentationConfigurationTests
{
    [Fact]
    public void MinimalPresetUsesIndependentSemanticEdges()
    {
        var configuration = new GridPresentationConfiguration(
            Preset: GridGeometryRole.Minimal,
            OuterInset: new(GridGeometryRole.None, GridGeometryRole.Compact,
                GridGeometryRole.Standard, GridGeometryRole.Comfortable));

        configuration.Validate();

        Assert.Equal(GridGeometryRole.None, configuration.EffectiveOuterInset.Left);
        Assert.Equal(GridGeometryRole.Compact, configuration.EffectiveOuterInset.Top);
        Assert.Equal(GridGeometryRole.Standard, configuration.EffectiveOuterInset.Right);
        Assert.Equal(GridGeometryRole.Comfortable, configuration.EffectiveOuterInset.Bottom);
    }

    [Fact]
    public void InvalidSemanticConfigurationFailsValidation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GridPresentationConfiguration(Preset: (GridGeometryRole)99).Validate());
        Assert.Throws<ArgumentException>(() =>
            new GridPresentationConfiguration(RowNumbersCanBeShown: false,
                RowNumbersShownByDefault: true).Validate());
        Assert.Throws<ArgumentException>(() =>
            new GridPresentationConfiguration(RowHeaderWidth: GridGeometryRole.None,
                RowNumbersCanBeShown: true).Validate());
    }

    [Fact]
    public void BarPlacementAndDensityRemainApplicationMetadata()
    {
        var configuration = new GridPresentationConfiguration(
            GridActionsAlignment: GridActionsAlignment.End,
            HeightMode: GridHeightMode.FitWorkspace,
            ViewportProfile: GridViewportProfile.Large,
            ActionsPlacement: GridActionsPlacement.Overflow,
            NavigationPlacement: GridNavigationPlacement.Bottom,
            Density: GridGeometryRole.Compact);

        configuration.Validate();

        Assert.Equal(GridActionsAlignment.End, configuration.GridActionsAlignment);
        Assert.Equal(GridActionsPlacement.Overflow, configuration.ActionsPlacement);
        Assert.Equal(GridNavigationPlacement.Bottom, configuration.NavigationPlacement);
        Assert.Equal(GridHeightMode.FitWorkspace, configuration.HeightMode);
        Assert.Equal(GridViewportProfile.Large, configuration.ViewportProfile);
        Assert.Equal(GridGeometryRole.Compact, configuration.Density);
    }

    [Fact]
    public void FixedHeightAndRowHeaderOverridesAreValidated()
    {
        Assert.Throws<ArgumentException>(() => new GridPresentationConfiguration(
            AllowFixedHeightBeyondWorkspace: true).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridPresentationConfiguration(
            RowHeaderWidthOverride: 48).Validate());
        new GridPresentationConfiguration(HeightMode: GridHeightMode.FixedSemanticHeight,
            FixedHeightRole: GridGeometryRole.Compact, AllowFixedHeightBeyondWorkspace: true,
            RowHeaderWidthOverride: 84).Validate();
    }

    [Theory]
    [InlineData(GridGeometryRole.None)]
    [InlineData(GridGeometryRole.Minimal)]
    [InlineData(GridGeometryRole.Compact)]
    [InlineData(GridGeometryRole.Standard)]
    [InlineData(GridGeometryRole.Comfortable)]
    public void ApprovedPresetsResolveToValidConfigurations(GridGeometryRole role)
    {
        var configuration = GridPresentationConfiguration.ForPreset(role);
        configuration.Validate();
        Assert.Equal(role, configuration.Preset);
    }
}
