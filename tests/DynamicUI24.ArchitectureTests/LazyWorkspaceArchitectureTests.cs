using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class LazyWorkspaceArchitectureTests
{
    private static readonly string Root = FindRoot();
    private static readonly string MainWindow = File.ReadAllText(Path.Combine(
        Root, "samples", "DynamicUI24.Demo", "MainWindow.axaml.cs"));

    [Fact]
    public void ColdDemoStartupRegistersDataEntryWithoutConstructingItsRuntimeGraph()
    {
        var constructorStart = MainWindow.IndexOf("private MainWindow(DemoComposition composition)", StringComparison.Ordinal);
        var factoryStart = MainWindow.IndexOf("private Control CreateDataEntryWorkspace", StringComparison.Ordinal);
        Assert.True(constructorStart >= 0 && factoryStart > constructorStart);
        var startup = MainWindow[constructorStart..factoryStart];

        Assert.Contains("new Lazy<Control>", startup, StringComparison.Ordinal);
        Assert.Contains("RegisterViewFactory(StandardTemplateCodes.DataEntry, _ => dataEntryWorkspace.Value)",
            startup, StringComparison.Ordinal);
        Assert.DoesNotContain("new DemoDataEntryProvider", startup, StringComparison.Ordinal);
        Assert.DoesNotContain("new DataEntryGridRuntime", startup, StringComparison.Ordinal);
        Assert.DoesNotContain("new DataEntryGridHost", startup, StringComparison.Ordinal);
        Assert.DoesNotContain("new DemoMultiSheetWorkspace", startup, StringComparison.Ordinal);
        Assert.DoesNotContain("new ImportExportWorkspaceHost", startup, StringComparison.Ordinal);
        Assert.DoesNotContain("new DemoPrivacyPanel", startup, StringComparison.Ordinal);
    }

    [Fact]
    public void FirstDataEntryActivationBuildsTheCompleteRealWorkspaceExactlyOnce()
    {
        var factoryStart = MainWindow.IndexOf("private Control CreateDataEntryWorkspace", StringComparison.Ordinal);
        var nextMethod = MainWindow.IndexOf("private Control BuildDemoSurface", factoryStart, StringComparison.Ordinal);
        var factory = MainWindow[factoryStart..nextMethod];

        Assert.Contains("new DemoDataEntryProvider", factory, StringComparison.Ordinal);
        Assert.Contains("new DataEntryGridHost(new DataEntryGridRuntime", factory, StringComparison.Ordinal);
        Assert.Contains("multiSheetWorkspace = new(", factory, StringComparison.Ordinal);
        Assert.Contains("new ImportExportWorkspaceHost", factory, StringComparison.Ordinal);
        Assert.Contains("new DemoPrivacyPanel", factory, StringComparison.Ordinal);
        Assert.Contains("new TabControl", factory, StringComparison.Ordinal);
        Assert.Contains("Multi-Sheet Data", factory, StringComparison.Ordinal);
        Assert.Contains("Import / Export", factory, StringComparison.Ordinal);
        Assert.Contains("LazyThreadSafetyMode.ExecutionAndPublication", MainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void LazyDataEntryRetainsRealCommandsDataSheetsAndImportExportBehavior()
    {
        var multiSheet = File.ReadAllText(Path.Combine(Root, "samples", "DynamicUI24.Demo",
            "DemoMultiSheetWorkspace.cs"));
        var provider = File.ReadAllText(Path.Combine(Root, "samples", "DynamicUI24.Demo",
            "DemoDataEntry.cs"));

        foreach (var command in new[] { "DEMO.GRID.COPY", "DEMO.GRID.CUT", "DEMO.GRID.PASTE",
                     "DEMO.GRID.UNDO", "DEMO.GRID.REDO", "DEMO.GRID.CLEAR" })
            Assert.Contains(command, MainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("DATAENTRY_REMOVED_FOR_BINARY_ISOLATION", MainWindow, StringComparison.Ordinal);
        foreach (var sheet in new[] { "SHEET_A", "SHEET_B", "SHEET_C", "SHEET_D", "SHEET_PRIVATE" })
            Assert.Contains(sheet, multiSheet, StringComparison.Ordinal);
        Assert.Contains("SheetHostRuntime", multiSheet, StringComparison.Ordinal);
        Assert.Contains("SheetHostView", multiSheet, StringComparison.Ordinal);
        Assert.Contains("LogicalRowCount = 100_000", provider, StringComparison.Ordinal);
        Assert.Contains("ShowProfiles(DemoImportExport.ImportProfiles, DemoImportExport.ExportProfiles)",
            MainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorAndReportRemainRegistrationOnlyAtColdStartup()
    {
        var constructorStart = MainWindow.IndexOf("private MainWindow(DemoComposition composition)", StringComparison.Ordinal);
        var factoryStart = MainWindow.IndexOf("private Control CreateDataEntryWorkspace", StringComparison.Ordinal);
        var startup = MainWindow[constructorStart..factoryStart];

        Assert.Contains("RegisterViewFactory(StandardTemplateCodes.Dashboard, definition =>", startup,
            StringComparison.Ordinal);
        Assert.Contains("? new DemoEditorWorkspace", startup, StringComparison.Ordinal);
        Assert.DoesNotContain("var editorWorkspace = new DemoEditorWorkspace", startup, StringComparison.Ordinal);
        Assert.DoesNotContain("new Report", startup, StringComparison.Ordinal);
    }

    [Fact]
    public void TemporaryImeDiagnosticArtifactsAreAbsent()
    {
        Assert.DoesNotContain("NativeInputDiagnosticWindow", MainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Pure Avalonia TextBox", MainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("STARTUP_DELTA", MainWindow, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(Root, "samples", "DynamicUI24.Demo",
            "NativeInputDiagnosticWindow.cs")));
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DynamicUI24.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
