using Xunit;

namespace DynamicUI24.Tests;

public sealed class FoundationAssemblyTests
{
    [Fact]
    public void FoundationalAssembliesLoad()
    {
        var assemblies = new[]
        {
            typeof(Core.AssemblyMarker).Assembly,
            typeof(Shared.AssemblyMarker).Assembly,
            typeof(Avalonia.AssemblyMarker).Assembly,
        };

        Assert.All(assemblies, assembly =>
            Assert.StartsWith("DynamicUI24.", assembly.GetName().Name, StringComparison.Ordinal));
    }
}
