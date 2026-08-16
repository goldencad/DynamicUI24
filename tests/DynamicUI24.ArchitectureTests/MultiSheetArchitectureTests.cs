using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class MultiSheetArchitectureTests
{
    private static readonly string Root = FindRoot();
    private static readonly string Sheets = Path.Combine(Root, "src", "DynamicUI24.Core", "Sheets");

    [Fact]
    public void CoreSheetFoundationIsPlatformAndVendorNeutral()
    {
        var text = string.Join('\n', Directory.GetFiles(Sheets, "*.cs").Select(File.ReadAllText));
        Assert.DoesNotContain("Avalonia", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DevExpress", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Spreadsheet", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayCalc24", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Period", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SheetHostDoesNotParseEvaluateOrRewriteFormulas()
    {
        var host = File.ReadAllText(Path.Combine(Sheets, "SheetHostRuntime.cs"));
        Assert.DoesNotContain("string.Replace", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Regex", host, StringComparison.Ordinal);
        Assert.DoesNotContain("EvaluateFormula", host, StringComparison.Ordinal);
        Assert.Contains("ISheetCalculationCompatibility", host, StringComparison.Ordinal);
    }

    [Fact]
    public void DemoSmokeSurfaceUsesRealHostLifecycleAndExistingLargeDataProvider()
    {
        var demo = File.ReadAllText(Path.Combine(Root, "samples", "DynamicUI24.Demo", "DemoMultiSheetWorkspace.cs"));
        Assert.Contains("SheetHostRuntime", demo, StringComparison.Ordinal);
        Assert.Contains("DemoDataEntryProvider", demo, StringComparison.Ordinal);
        Assert.Contains("Host.CreateAsync", demo, StringComparison.Ordinal);
        Assert.Contains("Host.DuplicateAsync", demo, StringComparison.Ordinal);
        Assert.Contains("Host.SaveAsAsync", demo, StringComparison.Ordinal);
        Assert.Contains("Host.DeleteAsync", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("string.Replace", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("EvaluateFormula", demo, StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DynamicUI24.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
