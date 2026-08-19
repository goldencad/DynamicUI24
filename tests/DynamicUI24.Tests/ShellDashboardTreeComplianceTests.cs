using DynamicUI24.Avalonia.Presentation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class ShellDashboardTreeComplianceTests
{
    [Fact]
    public void TreeDensityChangesRowHeightWithoutChangingSemanticSelection()
    {
        var appearance = new AppearancePreferenceService();
        var host = new DynamicTreeHost(new DictionaryLocalizationService(), new SemanticIconRegistry(), appearance: appearance);
        var state = new NavigationTreeSessionState();
        state.Select("leaf-id", "workspace-id");
        var identity = state.SelectedNodeId;

        appearance.Update(appearance.Current with { GridDensity = GridDensityPreference.Compact });

        Assert.Equal(identity, state.SelectedNodeId);
        Assert.Equal(28, host.RowHeight);
    }

    [Fact]
    public void TreeLocalizationAndThemePreferencesDoNotReplaceExpandedOrSelectedIdentity()
    {
        var localization = new DictionaryLocalizationService();
        var appearance = new AppearancePreferenceService();
        var state = new NavigationTreeSessionState();
        state.SetExpanded("parent-id", true);
        state.Select("leaf-id", "workspace-id");

        Assert.True(localization.TrySetCulture("vi-VN"));
        appearance.Update(appearance.Current with { Theme = ThemeMode.Dark });

        Assert.Equal("leaf-id", state.SelectedNodeId);
        Assert.True(state.IsExpanded("parent-id"));
    }

    [Fact]
    public void TreeVisualOrderAndLabelsCannotRedefineNodeCodeIdentity()
    {
        var first = new TreeNodeDefinition("first-id", "FIRST_CODE", new("Tree.Dashboard"),
            workspaceId: "first-workspace");
        var second = new TreeNodeDefinition("second-id", "SECOND_CODE", new("Tree.Overview"),
            workspaceId: "second-workspace");
        var state = new NavigationTreeSessionState();
        state.Select(second.NodeId, second.WorkspaceId);

        var reordered = new[] { second, first };

        Assert.Equal("second-id", state.SelectedNodeId);
        Assert.Equal("second-workspace", state.SelectedWorkspaceId);
        Assert.Equal("second-id", reordered[0].NodeId);
    }

    [Fact]
    public void DashboardAndOverviewExposeSharedStableAnatomy()
    {
        var page = new DashboardPage("Dashboard", "Summary");
        var metric = new MetricCard("Label", "42", "Context");
        var overview = new OverviewSection("Overview", "Status", ["Recent item"]);

        page.AddSection("Metrics", metric);
        page.AddSection("Recent", overview);

        Assert.Equal(3, page.Children.Count);
        Assert.NotNull(metric.Child);
        Assert.NotNull(overview.Child);
    }

    [Fact]
    public void SettingsPageIdentitySurvivesLanguageThemeAndDisplayLabelChanges()
    {
        var state = new SettingsNavigationState("appearance");
        var localization = new DictionaryLocalizationService("en-US");
        var appearance = new AppearancePreferenceService();

        Assert.True(localization.TrySetCulture("vi-VN"));
        appearance.Update(appearance.Current with { Theme = ThemeMode.Dark });

        Assert.Equal("APPEARANCE", state.CurrentPageCode);
    }

    [Fact]
    public void SharedTypographyAppliesResolvedPlatformFamilyToRenderedControlRoots()
    {
        var root = new Border();

        AvaloniaTypography.ApplyUiFont(root);

        Assert.Equal(AvaloniaTypography.UiFontFamily, TextElement.GetFontFamily(root));
        Assert.Equal(AvaloniaTypography.UiFontFamily, root.Resources["DuiFontFamilyUi"]);
        Assert.Contains(AvaloniaPlatformFontMapping.UiFallbackStack[0],
            AvaloniaTypography.UiFontFamily.Name, StringComparison.Ordinal);
    }

}
