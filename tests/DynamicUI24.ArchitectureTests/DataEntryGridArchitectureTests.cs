namespace DynamicUI24.ArchitectureTests;

using Xunit;

public sealed class DataEntryGridArchitectureTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void CoreGridContractsArePlatformAndBusinessNeutral()
    {
        var source = ReadDirectory("src/DynamicUI24.Core/DataEntry");
        Assert.DoesNotContain("Avalonia", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayCalc24", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Odoo", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TreeHost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RibbonHost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OperatingSystem", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GridReusesColumnDefinitionAndVariableCodeContracts()
    {
        var definitions = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridDefinitions.cs"));
        var provider = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridProvider.cs"));
        Assert.Contains("IEnumerable<ColumnDefinition>", definitions, StringComparison.Ordinal);
        Assert.Contains("VariableCode", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("class GridColumnDefinition", definitions, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderIsAsyncReadyAndContainsVirtualizationSeamWithoutImplementation()
    {
        var source = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridProvider.cs"));
        Assert.Contains("Task<GridDataResult> LoadAsync", source, StringComparison.Ordinal);
        Assert.Contains("CancellationToken", source, StringComparison.Ordinal);
        Assert.Contains("10B can add a viewport", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ViewportRequest", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RendererUsesExistingSharedFoundationsAndNoDirectSvgPaths()
    {
        var source = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("AppearancePreferenceService", source, StringComparison.Ordinal);
        Assert.Contains("ILocalizationService", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".svg", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#", source, StringComparison.Ordinal);
    }

    [Fact]
    public void NoFormulaExecutionOrImportExportEngineWasAdded()
    {
        var source = ReadDirectory("src/DynamicUI24.Core/DataEntry");
        Assert.DoesNotContain("ExpressionText", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CSharpScript", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Csv", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Excel", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadDirectory(string relative) => string.Join('\n',
        Directory.EnumerateFiles(Path(relative), "*.cs", SearchOption.AllDirectories).OrderBy(x => x).Select(File.ReadAllText));
    private static string Path(string relative) => System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(System.IO.Path.Combine(directory.FullName, "DynamicUI24.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
