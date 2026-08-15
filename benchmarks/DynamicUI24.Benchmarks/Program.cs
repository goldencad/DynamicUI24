using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

public class BootstrapBenchmarks
{
    [Benchmark]
    public Type LoadCoreAssemblyMarker() => typeof(DynamicUI24.Core.AssemblyMarker);
}

/// <summary>Focused proof that logical extent does not determine materialized object count.</summary>
[MemoryDiagnoser]
[InProcess]
public class GridViewportBenchmarks
{
    private static readonly GridProviderContext Context = new(
        new CompanyDescriptor(new CompanyId("benchmark"), "BENCH", "Benchmark"), "viewport-benchmark");
    private readonly SyntheticProvider provider = new();

    [Params(0, 90_000)]
    public int StartIndex { get; set; }

    [Benchmark]
    public Task<GridViewportResult> ResolveHundredThousandRowWindow() => provider.LoadViewportAsync(Context,
        new GridViewportRequest(StartIndex, 60, 20, 20, requestGeneration: 1));

    private sealed class SyntheticProvider : IVirtualizedGridDataProvider
    {
        public Task<GridViewportResult> LoadViewportAsync(GridProviderContext context, GridViewportRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            const int total = 100_000;
            var start = request.MaterializedStartIndex;
            var count = Math.Min(request.MaterializedRowCount, total - start);
            var rows = Enumerable.Range(start + 1, count).Select(index => new GridRow(new($"ROW:{index}"),
                new Dictionary<VariableCode, object?> { [new("VALUE")] = index })).ToImmutableArray();
            return Task.FromResult(new GridViewportResult(GridProviderState.Ready, start, rows, total,
                request.RequestGeneration, start > 0, start + count < total));
        }

        public Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
