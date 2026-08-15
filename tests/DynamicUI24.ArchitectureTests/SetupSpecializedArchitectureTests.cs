namespace DynamicUI24.ArchitectureTests;

using Xunit;

public sealed class SetupSpecializedArchitectureTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void SpecializedMetadataStaysInCoreSetupAndSetupTemplate()
    {
        Assert.True(File.Exists(Path("src/DynamicUI24.Core/Setup/SpecializedSetupDefinitions.cs")));
        Assert.True(File.Exists(Path("src/Templates/DynamicUI24.Template.Setup/SpecializedSetupEditors.cs")));
    }

    [Fact]
    public void CoreSpecializedMetadataHasNoUiPlatformOrConsumerDependency()
    {
        var source = File.ReadAllText(Path("src/DynamicUI24.Core/Setup/SpecializedSetupDefinitions.cs"));
        Assert.DoesNotContain("Avalonia", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayCalc24", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Odoo", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OperatingSystem", source, StringComparison.Ordinal);
    }

    [Fact]
    public void FormulaFoundationContainsNoExecutionEngineOrScriptRuntime()
    {
        var source = File.ReadAllText(Path("src/DynamicUI24.Core/Setup/SpecializedSetupDefinitions.cs"));
        Assert.DoesNotContain("CSharpScript", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Process.Start", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DbConnection", source, StringComparison.Ordinal);
        Assert.Contains("never executed", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SpecializedEditorsRemainRegistryBasedAndTemplateDriven()
    {
        var source = File.ReadAllText(Path("src/Templates/DynamicUI24.Template.Setup/SpecializedSetupEditors.cs"));
        Assert.Contains("editors.Register", source, StringComparison.Ordinal);
        Assert.Contains("templates.GetRegisteredTemplates()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("switch", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".svg", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoRuntimeDataGridWasAddedForSetupMetadata()
    {
        var sources = string.Join('\n', Directory.EnumerateFiles(Path("src"), "*.cs", SearchOption.AllDirectories)
            .Where(x => x.Contains("Setup", StringComparison.OrdinalIgnoreCase)).Select(File.ReadAllText));
        Assert.DoesNotContain("DataGrid", sources, StringComparison.Ordinal);
        Assert.DoesNotContain("Virtualization", sources, StringComparison.Ordinal);
    }

    private static string Path(string relative) => System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(System.IO.Path.Combine(directory.FullName, "DynamicUI24.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
