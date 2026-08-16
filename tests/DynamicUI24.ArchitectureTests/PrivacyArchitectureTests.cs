using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class PrivacyArchitectureTests
{
    private static readonly string Root = FindRoot();
    private static readonly string PrivacyRoot = Path.Combine(Root, "src", "DynamicUI24.Core", "Privacy");

    [Fact]
    public void CorePrivacyHasNoPlatformOrConsumerDependencies()
    {
        var text = ReadPrivacy();
        Assert.DoesNotContain("using Avalonia", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PayCalc24", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Odoo", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DllImport", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedResolverAndSurfaceSeamsAreExplicit()
    {
        var text = ReadPrivacy();
        Assert.Contains("interface IPrivacyPolicyResolver", text, StringComparison.Ordinal);
        Assert.Contains("PrivacyClipboardPolicy", text, StringComparison.Ordinal);
        Assert.Contains("PrivacyNotificationPresentation", text, StringComparison.Ordinal);
        Assert.Contains("PrivacySearchPresentation", text, StringComparison.Ordinal);
        Assert.Contains("PrivacyImportExportPolicy", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CaptureContractIsPlatformNeutralAndFallbackIsMandatory()
    {
        var text = ReadPrivacy();
        Assert.Contains("interface ICaptureProtectionService", text, StringComparison.Ordinal);
        Assert.Contains("CaptureProtectionFallback", text, StringComparison.Ordinal);
        Assert.Contains("SafeFallback", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PrivacyContractsContainNoBusinessSpecificClassificationOrDlpSubsystem()
    {
        var contracts = File.ReadAllText(Path.Combine(PrivacyRoot, "PrivacyContracts.cs"));
        Assert.Contains("enum Sensitivity { Normal, Confidential, Restricted }", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("PAYROLL", contracts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dlp", ReadPrivacy(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RevealStateIsRuntimeOnly()
    {
        var state = File.ReadAllText(Path.Combine(PrivacyRoot, "PrivacyStateService.cs"));
        Assert.DoesNotContain("File.", state, StringComparison.Ordinal);
        Assert.DoesNotContain("Preferences", state, StringComparison.Ordinal);
        Assert.Contains("Generation", state, StringComparison.Ordinal);
    }

    private static string ReadPrivacy() => string.Join('\n', Directory.GetFiles(PrivacyRoot, "*.cs").Select(File.ReadAllText));
    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "DynamicUI24.slnx"))) current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
