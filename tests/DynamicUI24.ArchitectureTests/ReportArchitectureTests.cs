using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class ReportArchitectureTests
{
    [Fact]
    public void Report_core_is_vendor_and_UI_neutral()
    {
        var source = ReadReports();
        Assert.DoesNotContain("using Avalonia", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DevExpress", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ReportOutputArtifact", source);
        Assert.Contains("DocumentViewRequest", source);
        Assert.Contains("IDocumentViewLauncher", source);
    }

    [Fact]
    public void Report_output_uses_shared_commands_and_contains_no_document_engine()
    {
        var reports = ReadReports();
        var host = ReadFile("src", "DynamicUI24.Avalonia", "Presentation", "ReportWorkspaceHost.cs");
        Assert.Contains("ReportCommandCodes.Export", host);
        Assert.Contains("ReportCommandCodes.ViewOutput", host);
        Assert.Contains("IActionCommandRegistry", host);
        foreach (var forbidden in new[] { "PdfDocumentProcessor", "RichEditDocumentServer", "SpreadsheetDocumentServer", "System.IO.File" })
            Assert.DoesNotContain(forbidden, reports, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Report_action_placement_is_semantic_and_reuses_shared_presentation_engines()
    {
        var reports = ReadReports();
        var host = ReadFile("src", "DynamicUI24.Avalonia", "Presentation", "ReportWorkspaceHost.cs");
        Assert.Contains("ReportActionDefinition", reports);
        Assert.Contains("ReportActionPlacement", reports);
        Assert.Contains("ActionBarDefinition", reports);
        Assert.Contains("ContextualActionDefinition", reports);
        Assert.Contains("DynamicActionBarHost", host);
        Assert.Contains("ContextualToolbarHost", host);
        Assert.DoesNotContain("class ReportActionDispatcher", reports);
        Assert.DoesNotContain("class ReportCommandRegistry", reports);
        Assert.DoesNotContain("class ReportToolbarEngine", reports);
        foreach (var forbidden in new[] { "TopPixel", "LeftPixel", "ActionX", "ActionY" })
            Assert.DoesNotContain(forbidden, reports, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Children = { run, reset", host);
        Assert.DoesNotContain("Children = { outputFormat, export", host);
    }

    [Fact]
    public void Date_time_presentation_is_owned_only_by_universal_editor()
    {
        var reports = ReadReports();
        var host = ReadFile("src", "DynamicUI24.Avalonia", "Presentation", "ReportWorkspaceHost.cs");
        var editor = ReadFile("src", "DynamicUI24.Avalonia", "Presentation", "Editors", "AvaloniaEditorPresenter.cs");
        Assert.Contains("CalendarDatePicker", editor);
        Assert.Contains("EditorPresentationTokens", editor);
        Assert.Contains("AvaloniaEditorPresenter", host);
        Assert.DoesNotContain("ReportDateEditor", reports + host);
        Assert.DoesNotContain("ReportTimeEditor", reports + host);
        Assert.DoesNotContain("new DatePicker", host);
        Assert.DoesNotContain("new TimePicker", host);
    }

    [Fact]
    public void Provider_contract_contains_no_storage_formula_or_signing_technology()
    {
        var surface = ReadReports();
        foreach (var forbidden in new[] { "SqlConnection", "DbContext", "Mongo", "DevExpress", "FormulaParser", "PKCS11", "Chilkat", "PayCalc24", "Odoo" })
            Assert.DoesNotContain(forbidden, surface, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Runtime_reuses_the_single_grid_and_find_engine()
    {
        var source = ReadReports();
        Assert.Contains("DataEntryGridRuntime", source);
        Assert.DoesNotContain("class ReportFindEngine", source);
    }

    [Fact]
    public void Definitions_are_immutable_semantic_contracts()
    {
        var source = ReadReports();
        Assert.Contains("sealed record ReportDefinition", source);
        Assert.DoesNotContain("SqlConnection", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Report_reuses_current_editor_authorization_state_and_operation_foundations()
    {
        var source = ReadReports();
        Assert.Contains("EditorDefinition", source);
        Assert.Contains("UiAuthorizationBinding", source);
        Assert.Contains("ContentPresentationState", source);
        Assert.Contains("OperationCoordinator", source);
        Assert.DoesNotContain("ReportParameterType", source);
        Assert.DoesNotContain("ReportRuntimeState", source);
        Assert.DoesNotContain("ReportProgressCoordinator", source);
    }

    [Fact]
    public void Report_demo_factory_is_lazy_and_does_not_restore_shared_grid_hooks()
    {
        var main = ReadFile("samples", "DynamicUI24.Demo", "MainWindow.axaml.cs");
        var host = ReadFile("src", "DynamicUI24.Avalonia", "Presentation", "ReportWorkspaceHost.cs");
        var grid = ReadFile("src", "DynamicUI24.Avalonia", "Presentation", "DataEntryGridHost.cs");
        Assert.Contains("reportWorkspace = new Lazy<Control>", main);
        Assert.Contains("StandardTemplateCodes.Report, _ => reportWorkspace.Value", main);
        Assert.Contains("AvaloniaEditorPresenter", host);
        Assert.DoesNotContain("SuspendAutomaticViewportResize", grid);
        Assert.DoesNotContain("ReportParameterType", host);
    }

    [Fact]
    public void Report_run_and_reset_use_semantic_shared_commands_not_direct_button_runtime_calls()
    {
        var reports = ReadReports();
        var host = ReadFile("src", "DynamicUI24.Avalonia", "Presentation", "ReportWorkspaceHost.cs");
        Assert.Contains("ReportCommandCodes", reports);
        Assert.Contains("IActionCommandRegistry", host);
        Assert.Contains("commandRegistry.Register(RunCommandCode", host);
        Assert.Contains("commandRegistry.Register(ResetCommandCode", host);
        Assert.Contains("DynamicActionBarHost", host);
        Assert.Contains("ResetQueryState", reports);
        Assert.DoesNotContain("run.Click += async (_, _) => await RunAsync()", host);
        Assert.DoesNotContain("reset.Click += (_, _) => runtime.ResetParameters()", host);
    }

    [Fact]
    public void Report_geometry_is_semantic_and_shared_with_grid_header_and_rows()
    {
        var reports = ReadReports();
        var gridHost = ReadFile("src", "DynamicUI24.Avalonia", "Presentation", "DataEntryGridHost.cs");
        Assert.Contains("ReportColumnCode", reports);
        Assert.Contains("ResolveBaseWidth", reports);
        Assert.Contains("BuildHeader(columns", gridHost);
        Assert.Contains("BuildRow(runtime.Rows[index], index, columns", gridHost);
        Assert.DoesNotContain("ReportTableEngine", reports);
        Assert.DoesNotContain("visualIndex", reports, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadReports()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "DynamicUI24.Core", "Reports")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return string.Join("\n", Directory.GetFiles(Path.Combine(directory!.FullName, "src", "DynamicUI24.Core", "Reports"), "*.cs")
            .Order().Select(File.ReadAllText));
    }

    private static string ReadFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine([directory.FullName, .. parts]))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine([directory!.FullName, .. parts]));
    }
}
