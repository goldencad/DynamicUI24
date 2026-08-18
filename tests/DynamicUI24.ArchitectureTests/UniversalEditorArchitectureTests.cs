using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class UniversalEditorArchitectureTests
{
    private static readonly string Root = FindRoot();
    private static readonly string EditorRoot = Path.Combine(Root, "src", "DynamicUI24.Core", "Editors");

    [Fact]
    public void CoreEditorSurfaceIsVendorAndUiNeutral()
    {
        var source = ReadEditorSources();
        Assert.DoesNotContain("using Avalonia", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Actipro", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DevExpress", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Windows.Forms", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExactlyOneEditorResolverExists()
    {
        var count = Directory.GetFiles(Path.Combine(Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText).Count(text => text.Contains("class EditorResolver", StringComparison.Ordinal));
        Assert.Equal(1, count);
    }

    [Fact]
    public void EditorValueTypesAreGenericAndContainNoBusinessCatalog()
    {
        var source = ReadEditorSources();
        Assert.DoesNotContain("Salary", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TaxPeriod", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sql", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Formula", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LookupContractIsBoundedAndReturnsSemanticModels()
    {
        var source = File.ReadAllText(Path.Combine(EditorRoot, "EditorLookup.cs"));
        Assert.Contains("MaximumWindowSize = 200", source, StringComparison.Ordinal);
        Assert.Contains("ValueTask<EditorLookupResult>", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Control", source, StringComparison.Ordinal);
    }

    private static string ReadEditorSources() => string.Join('\n', Directory.GetFiles(EditorRoot, "*.cs").Select(File.ReadAllText));
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DynamicUI24.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
