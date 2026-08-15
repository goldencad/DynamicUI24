using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<BootstrapBenchmarks>();

/// <summary>Confirms the benchmark harness; feature benchmarks belong to later tasks.</summary>
public class BootstrapBenchmarks
{
    [Benchmark]
    public Type LoadCoreAssemblyMarker() => typeof(DynamicUI24.Core.AssemblyMarker);
}
