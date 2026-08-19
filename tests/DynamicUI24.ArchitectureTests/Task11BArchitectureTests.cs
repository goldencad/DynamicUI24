using System.Text.RegularExpressions;
using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class Task11BArchitectureTests
{
    private static readonly string Root = FindRepositoryRoot();

    [Fact]
    public void ShellAndTreeConsumeSemanticDesignResources()
    {
        var shell = Read("src/DynamicUI24.Avalonia/Presentation/ShellHost.axaml");
        var tree = Read("src/DynamicUI24.Avalonia/Presentation/DynamicTreeHost.axaml");
        Assert.Contains("DuiSurfaceWindowBrush", shell, StringComparison.Ordinal);
        Assert.Contains("DuiShellRegionPaddingThickness", shell, StringComparison.Ordinal);
        Assert.Contains("DuiNavigationRowHeight", tree, StringComparison.Ordinal);
        Assert.Contains("DuiNavigationIconSize", tree, StringComparison.Ordinal);
        Assert.Contains("DuiNavigationChevronSize", tree, StringComparison.Ordinal);
        Assert.DoesNotMatch("#[0-9A-Fa-f]{6,8}", shell + tree);
    }

    [Fact]
    public void DashboardAndOverviewUseOneFrameworkOwnedComponentTaxonomy()
    {
        var components = Read("src/DynamicUI24.Avalonia/Presentation/DashboardOverviewControls.cs");
        var demo = Read("samples/DynamicUI24.Demo/MainWindow.axaml.cs");
        Assert.Contains("class DashboardPage", components, StringComparison.Ordinal);
        Assert.Contains("class MetricCard", components, StringComparison.Ordinal);
        Assert.Contains("class OverviewSection", components, StringComparison.Ordinal);
        Assert.Contains("new DashboardPage", demo, StringComparison.Ordinal);
        Assert.Contains("new OverviewSection", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("OverviewEngine", components + demo, StringComparison.Ordinal);
    }

    [Fact]
    public void TreeKeepsSemanticIdentityAndHasNoApplicationLocalStyleEngine()
    {
        var tree = Read("src/DynamicUI24.Avalonia/Presentation/DynamicTreeHost.axaml.cs");
        var sampleSources = Directory.EnumerateFiles(Path.Combine(Root, "samples"), "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".axaml")
            .Select(File.ReadAllText);
        Assert.Contains("Definition.NodeId", tree, StringComparison.Ordinal);
        Assert.Contains("Definition.WorkspaceId", tree, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedNodeId = selected.Label", tree, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex("class\\s+.*Tree.*Style|TreeView\\.Styles"), string.Join('\n', sampleSources));
    }

    [Fact]
    public void SettingsUseFrameworkSemanticNavigationReadableWidthsAndCompactEmptyShellState()
    {
        var settings = Read("src/DynamicUI24.Avalonia/Presentation/ApplicationMenuView.cs");
        var state = Read("src/DynamicUI24.Shared/Presentation/SettingsNavigationState.cs");
        var tokens = Read("src/DynamicUI24.Avalonia/Presentation/DesignTokens.axaml");
        var notifications = Read("src/DynamicUI24.Avalonia/Presentation/NotificationHost.cs");
        Assert.Contains("ListBox navigation", settings, StringComparison.Ordinal);
        Assert.Contains("DuiFormReadableWidth", settings, StringComparison.Ordinal);
        Assert.Contains("DuiEditorWidthCompact", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("HorizontalAlignment.Stretch, Tag = culture", settings, StringComparison.Ordinal);
        Assert.Contains("SettingsNavigationState", settings, StringComparison.Ordinal);
        Assert.Contains("CurrentPageCode", state, StringComparison.Ordinal);
        Assert.Contains("DuiEditorWidthShort", tokens, StringComparison.Ordinal);
        Assert.Contains("IsVisible = model.Notifications.Length > 0", notifications, StringComparison.Ordinal);
    }

    [Fact]
    public void AllShellTypographyUsesOnePlatformResolvedSharedAdapter()
    {
        var adapter = Read("src/DynamicUI24.Avalonia/Presentation/AvaloniaTypography.cs");
        Assert.Contains("AvaloniaPlatformFontMapping.UiFallbackStack", adapter, StringComparison.Ordinal);
        Assert.Contains("TextElement.SetFontFamily", adapter, StringComparison.Ordinal);
        foreach (var file in new[] { "ShellHost.axaml.cs", "ApplicationMenuView.cs", "DynamicTreeHost.axaml.cs",
                     "NotificationHost.cs", "SearchPaletteView.axaml.cs", "BreadcrumbHost.cs", "ContextPanelHost.cs",
                     "DashboardOverviewControls.cs" })
            Assert.Contains("AvaloniaTypography.ApplyUiFont", Read($"src/DynamicUI24.Avalonia/Presentation/{file}"),
                StringComparison.Ordinal);
        Assert.Contains("FontFamily = AvaloniaTypography.UiFontFamily",
            Read("src/DynamicUI24.Avalonia/Presentation/DynamicRibbonHost.cs"), StringComparison.Ordinal);
    }

    private static string Read(string relative) => File.ReadAllText(Path.Combine(Root, relative));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "DynamicUI24.slnx")))
            current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
