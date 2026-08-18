namespace DynamicUI24.ArchitectureTests;

using Xunit;

public sealed class ModernWorkspaceArchitectureTests
{
    [Fact] public void CoreModernWorkspaceRemainsVendorNeutral()
    {
        var root = FindRepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root, "src", "DynamicUI24.Core", "ModernWorkspace"), "*.cs");
        var text = string.Join('\n', files.Select(File.ReadAllText));
        Assert.DoesNotContain("Avalonia", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Actipro", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DevExpress", text, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Data", text, StringComparison.Ordinal);
    }

    [Fact] public void FoundationDoesNotDeclareParallelInfrastructure()
    {
        var root = FindRepositoryRoot();
        var text = string.Join('\n', Directory.GetFiles(Path.Combine(root, "src", "DynamicUI24.Core", "ModernWorkspace"), "*.cs").Select(File.ReadAllText));
        Assert.DoesNotContain("class ActionCommandRegistry", text, StringComparison.Ordinal);
        Assert.DoesNotContain("class NotificationCoordinator", text, StringComparison.Ordinal);
        Assert.DoesNotContain("class SearchCoordinator", text, StringComparison.Ordinal);
        Assert.DoesNotContain("class ContextPanelCoordinator", text, StringComparison.Ordinal);
    }

    [Fact] public void PaneSessionStateDoesNotDependOnAvaloniaControlsOrVisualPosition()
    {
        var root = FindRepositoryRoot();
        var text = File.ReadAllText(Path.Combine(root, "src", "DynamicUI24.Core", "ModernWorkspace", "WorkspacePanes.cs"));
        Assert.Contains("WorkspacePaneKey(WorkspaceCode WorkspaceCode, PaneCode PaneCode)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Control", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Visual", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ChildIndex", text, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DynamicUI24.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
