namespace DynamicUI24.ArchitectureTests;

using Xunit;

public sealed class ImportExportArchitectureTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void Core_import_export_contracts_are_Avalonia_and_platform_picker_free()
    {
        var text = ReadTree("src/DynamicUI24.Core/ImportExport");
        Assert.DoesNotContain("using Avalonia", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StorageProvider", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenFilePicker", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OperatingSystem.Is", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Xlsx_is_an_extension_not_a_Core_dependency()
    {
        var project = File.ReadAllText(Path.Combine(Root, "src/DynamicUI24.Core/DynamicUI24.Core.csproj"));
        Assert.DoesNotContain("Excel", project, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(Root, "src/Extensions/DynamicUI24.Excel/XlsxImportExportProviders.cs")));
    }

    [Fact]
    public void DataEntry_renderer_does_not_parse_files()
    {
        var text = ReadTree("src/DynamicUI24.Avalonia/Presentation");
        var host = File.ReadAllText(Path.Combine(Root, "src/DynamicUI24.Avalonia/Presentation/ImportExportWorkspaceHost.cs"));
        Assert.DoesNotContain("IImportParserProvider", host, StringComparison.Ordinal);
        Assert.DoesNotContain("ZipArchive", text, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonDocument.Parse", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_mapping_identity_and_streaming_provider_seams_are_explicit()
    {
        var text = ReadTree("src/DynamicUI24.Core/ImportExport");
        Assert.Contains("TargetVariableCode", text, StringComparison.Ordinal);
        Assert.Contains("IGridBatchRowImportProvider", text, StringComparison.Ordinal);
        Assert.Contains("IGridExportProvider", text, StringComparison.Ordinal);
        Assert.Contains("IAsyncEnumerable", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Assembly.Load", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PayCalc24", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Odoo", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Xml_parser_prohibits_Dtd_and_external_resolution()
    {
        var text = File.ReadAllText(Path.Combine(Root, "src/DynamicUI24.Core/ImportExport/BuiltInFormatProviders.cs"));
        Assert.Contains("DtdProcessing = DtdProcessing.Prohibit", text, StringComparison.Ordinal);
        Assert.Contains("XmlResolver = null", text, StringComparison.Ordinal);
    }

    private static string ReadTree(string path) => string.Join('\n', Directory.GetFiles(Path.Combine(Root, path), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DynamicUI24.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
