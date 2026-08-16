using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class SearchArchitectureTests
{
    private static readonly string Root = FindRoot();
    private static readonly string SearchRoot = Path.Combine(Root, "src", "DynamicUI24.Core", "Search");
    private static string SearchText => string.Join('\n', Directory.GetFiles(SearchRoot, "*.cs").Select(File.ReadAllText));

    [Fact]
    public void CoreSearchIsPlatformAndApplicationNeutral()
    {
        Assert.DoesNotContain("Avalonia", SearchText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DynamicUI24.Demo", SearchText, StringComparison.Ordinal);
        Assert.DoesNotContain("PayCalc24", SearchText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Odoo", SearchText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DllImport", SearchText, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchHasNoDatabaseAiOrArbitraryExecutionDependency()
    {
        Assert.DoesNotContain("EntityFramework", SearchText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SqlConnection", SearchText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Embedding", SearchText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Assembly.Load", SearchText, StringComparison.Ordinal);
        Assert.DoesNotContain("Process.Start", SearchText, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedSecurityAndActivationSeamsAreReused()
    {
        Assert.Contains("AuthorizationPresentationResolver.Resolve", SearchText, StringComparison.Ordinal);
        Assert.Contains("PrivacySearchPresentation.Resolve", SearchText, StringComparison.Ordinal);
        Assert.Contains("IWorkspaceNavigationService", SearchText, StringComparison.Ordinal);
        Assert.Contains("IUiCommandRegistry", SearchText, StringComparison.Ordinal);
        Assert.Contains("Generation", SearchText, StringComparison.Ordinal);
        Assert.Contains("CancellationToken", SearchText, StringComparison.Ordinal);
    }

    [Fact]
    public void QuickAccessStoresSemanticIdentityNotLocalizedLabel()
    {
        var quick = File.ReadAllText(Path.Combine(SearchRoot, "QuickAccess.cs"));
        Assert.Contains("TargetCode", quick, StringComparison.Ordinal);
        Assert.DoesNotContain("DisplayText", quick, StringComparison.Ordinal);
        Assert.DoesNotContain("TreeDefinition", quick, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationSearchContainsNoRecordSearchOrRuntimePanelFeatures()
    {
        var navigation = File.ReadAllText(Path.Combine(SearchRoot, "NavigationSearch.cs"));
        Assert.DoesNotContain("SearchResultKind.Record", navigation, StringComparison.Ordinal);
        Assert.DoesNotContain("ContextPanel", SearchText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Breadcrumb", SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "DynamicUI24.slnx"))) current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
