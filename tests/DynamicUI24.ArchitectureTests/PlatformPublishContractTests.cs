using System.Xml.Linq;
using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class PlatformPublishContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string[] RequiredRids = ["win-x64", "win-arm64", "osx-arm64", "osx-x64", "linux-x64"];

    [Fact]
    public void RequiredPublishRidsAreCentralizedAndAssignedToDemo()
    {
        var props = XDocument.Load(Path.Combine(RepositoryRoot, "Directory.Build.props"));
        var supportedRids = props.Descendants("SupportedPublishRids").Single().Value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(RequiredRids, supportedRids);

        var demoProject = XDocument.Load(Path.Combine(RepositoryRoot,
            "samples", "DynamicUI24.Demo", "DynamicUI24.Demo.csproj"));
        Assert.Equal("$(SupportedPublishRids)", demoProject.Descendants("RuntimeIdentifiers").Single().Value);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DynamicUI24.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
