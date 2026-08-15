using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class NotificationArchitectureTests
{
    private static readonly string Root = FindRoot();
    private static readonly string CoreDirectory = Path.Combine(Root, "src", "DynamicUI24.Core", "Notifications");

    [Fact]
    public void NotificationCoreContractsRemainPlatformAndConsumerFree()
    {
        var source = ReadCore();
        Assert.DoesNotContain("Avalonia", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DynamicUI24.Demo", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PayCalc24", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Odoo", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Process.Start", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Assembly.Load", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GuidanceReusesNavigationCommandMenuAndActionBarFoundations()
    {
        var actions = File.ReadAllText(Path.Combine(CoreDirectory, "NotificationActions.cs"));
        Assert.Contains("IWorkspaceNavigationService", actions, StringComparison.Ordinal);
        Assert.Contains("IActionCommandRegistry", actions, StringComparison.Ordinal);
        Assert.Contains("ActionMenuItemDefinition", ReadCore(), StringComparison.Ordinal);
        Assert.Contains("ActionBarDefinition", actions, StringComparison.Ordinal);
        Assert.Contains("ActionBarPosition.Top", actions, StringComparison.Ordinal);
        Assert.Contains("ActionBarPosition.Bottom", actions, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellRendererDoesNotInstantiateTemplatesOrDuplicateLogicalState()
    {
        var host = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation", "NotificationHost.cs"));
        Assert.DoesNotContain("TemplateRegistry", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new NotificationInstance", host, StringComparison.Ordinal);
        Assert.Contains("NotificationPresentationModel", host, StringComparison.Ordinal);
        var shell = File.ReadAllText(Path.Combine(Root, "src", "DynamicUI24.Avalonia", "Presentation", "ShellHost.axaml"));
        Assert.Contains("NotificationPresenter", shell, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScopeDidNotIntroduceNeighborFeaturesOrPersistence()
    {
        var paths = Directory.EnumerateFiles(CoreDirectory, "*.cs").Select(Path.GetFileName).ToArray();
        Assert.DoesNotContain(paths, x => x!.Contains("Search", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, x => x!.Contains("Favorite", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, x => x!.Contains("Database", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("WebSocket", ReadCore(), StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadCore() => string.Join('\n', Directory.EnumerateFiles(CoreDirectory, "*.cs")
        .OrderBy(x => x, StringComparer.Ordinal).Select(File.ReadAllText));
    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "DynamicUI24.slnx"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
