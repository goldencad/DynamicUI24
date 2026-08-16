using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class ContextArchitectureTests
{
    private static readonly string Root = FindRoot();
    private static readonly string ContextText = string.Join('\n', Directory.GetFiles(Path.Combine(Root, "src/DynamicUI24.Core/Context"), "*.cs").Select(File.ReadAllText));
    [Fact] public void CoreContextContractsRemainAvaloniaAndAiFree()
    {
        Assert.DoesNotContain("using Avalonia", ContextText, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAI", ContextText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpClient", ContextText, StringComparison.Ordinal);
    }
    [Fact] public void ContextUsesSemanticSelectionAndSharedPrivacyAuthorization()
    {
        Assert.Contains("ContextSelection", ContextText, StringComparison.Ordinal);
        Assert.Contains("IPrivacyPolicyResolver", ContextText, StringComparison.Ordinal);
        Assert.Contains("AuthorizationPresentationResolver", ContextText, StringComparison.Ordinal);
        Assert.DoesNotContain("DataEntryGridHost", ContextText, StringComparison.Ordinal);
        Assert.DoesNotContain("DynamicTreeHost", ContextText, StringComparison.Ordinal);
    }
    [Fact] public void BreadcrumbActivationUsesWorkspaceNavigationService()
    {
        Assert.Contains("IWorkspaceNavigationService", ContextText, StringComparison.Ordinal);
        Assert.Contains("navigation.NavigateAsync", ContextText, StringComparison.Ordinal);
    }
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
