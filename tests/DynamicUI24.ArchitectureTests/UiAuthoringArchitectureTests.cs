using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class UiAuthoringArchitectureTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void CoreAuthoringIsVendorNeutralAndContainsNoScriptOrSqlEngine()
    {
        var directory = Path.Combine(Root, "src", "DynamicUI24.Core", "Authoring");
        var text = string.Join('\n', Directory.EnumerateFiles(directory, "*.cs").Select(File.ReadAllText));
        Assert.DoesNotContain("Avalonia", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Actipro", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DevExpress", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.Data.Sql", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.CodeAnalysis.Scripting", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Viewer", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Administrator", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DraftPublishedRuntimeAuthorizationAndPreferenceAreDistinctTypes()
    {
        var directory = Path.Combine(Root, "src", "DynamicUI24.Core", "Authoring");
        var text = string.Join('\n', Directory.EnumerateFiles(directory, "*.cs").Select(File.ReadAllText));
        foreach (var name in new[] { "UiDefinition", "UiDefinitionDraft", "UiDefinitionVersionInfo",
                     "UiAuthoringRuntimeState", "UiAuthorizationResult", "UiElementPreference" })
            Assert.Contains($"{name}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DemoProfileSelectorUsesDiscoverableTwoRowLayout()
    {
        var mainWindow = File.ReadAllText(Path.Combine(Root, "samples", "DynamicUI24.Demo", "MainWindow.axaml.cs"));
        var start = mainWindow.IndexOf("private Control BuildDemoSurface()", StringComparison.Ordinal);
        var end = mainWindow.IndexOf("private static Control Field", start, StringComparison.Ordinal);
        var layout = mainWindow[start..end];
        Assert.Contains("ColumnDefinitions(\"2*,*,*\")", layout, StringComparison.Ordinal);
        Assert.Contains("RowDefinitions(\"Auto,Auto\")", layout, StringComparison.Ordinal);
        Assert.Contains("Field(workspaceLabel, workspaceSelector, 0)", layout, StringComparison.Ordinal);
        Assert.Contains("Field(demoProfileLabel, demoProfileSelector, 1)", layout, StringComparison.Ordinal);
        Assert.Contains("Field(themeLabel, themeSelector, 2)", layout, StringComparison.Ordinal);
        Assert.Contains("Field(languageLabel, languageSelector, 0, 1)", layout, StringComparison.Ordinal);
        Assert.Contains("Field(stateLabel, stateSelector, 1, 1)", layout, StringComparison.Ordinal);
        Assert.Contains("Field(selectionLabel, selectionSelector, 2, 1)", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("ColumnDefinitions(\"2*,*,*,*,*,*\")", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void DemoAuthoringUsesUniversalEditorRealLifecycleAndLazyFactory()
    {
        var authoring = File.ReadAllText(Path.Combine(Root, "samples", "DynamicUI24.Demo", "DemoUiAuthoring.cs"));
        var mainWindow = File.ReadAllText(Path.Combine(Root, "samples", "DynamicUI24.Demo", "MainWindow.axaml.cs"));
        Assert.Contains("AvaloniaEditorPresenter", authoring, StringComparison.Ordinal);
        Assert.Contains("UiDefinitionLifecycleService", authoring, StringComparison.Ordinal);
        Assert.Contains("InMemoryUiDefinitionRepository", authoring, StringComparison.Ordinal);
        Assert.Contains("lifecycle.PreviewAsync", authoring, StringComparison.Ordinal);
        Assert.Contains("lifecycle.PublishAsync", authoring, StringComparison.Ordinal);
        Assert.Contains("lifecycle.RollbackAsync", authoring, StringComparison.Ordinal);
        Assert.Contains("? new DemoUiAuthoringWorkspace", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("var authoringWorkspace = new DemoUiAuthoringWorkspace", mainWindow, StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "DynamicUI24.slnx"))) current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
