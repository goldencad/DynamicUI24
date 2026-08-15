using System.Collections.Immutable;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class GridViewportTests
{
    private static readonly CompanyDescriptor CompanyA = new(new("a"), "A", "Company A");
    private static readonly CompanyDescriptor CompanyB = new(new("b"), "B", "Company B");
    private static readonly GridProviderContext ContextA = new(CompanyA, "workspace");

    [Fact]
    public void RequestValidatesRangesAndComputesBoundedOverscan()
    {
        var request = new GridViewportRequest(10, 20, 5, 7, requestGeneration: 4);
        Assert.Equal(5, request.MaterializedStartIndex);
        Assert.Equal(32, request.MaterializedRowCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridViewportRequest(-1, 20, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridViewportRequest(0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridViewportRequest(0, 20, -1, 0));
    }

    [Fact]
    public async Task HundredThousandLogicalRowsMaterializeOnlyRequestedWindowAndFarJump()
    {
        var generated = 0;
        var provider = Provider((context, request, _) =>
        {
            var result = Window(context, request, 100_000);
            generated += result.Rows.Length;
            return Task.FromResult(result);
        });
        var runtime = Runtime(provider);
        await runtime.LoadAsync(ContextA, null);
        Assert.Equal(100_000, runtime.TotalRows);
        Assert.InRange(runtime.Rows.Length, 1, 100);
        await runtime.RequestViewportAsync(90_000, 60);
        Assert.InRange(runtime.Rows.Length, 1, 100);
        Assert.Equal(89_980, runtime.ViewportStartIndex);
        Assert.StartsWith("a:89981", runtime.Rows[0].RowKey.Value, StringComparison.Ordinal);
        Assert.True(generated < 250);
    }

    [Fact]
    public async Task LateViewportCannotReplaceNewerViewportAndObsoleteRequestIsCancelled()
    {
        var releaseA = new TaskCompletionSource<GridViewportResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken tokenA = default;
        var provider = Provider((context, request, token) =>
        {
            if (request.StartIndex == 100) { tokenA = token; return releaseA.Task; }
            return Task.FromResult(Window(context, request, 100_000));
        });
        var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        var loadA = runtime.RequestViewportAsync(100, 60);
        var loadB = runtime.RequestViewportAsync(500, 60); await loadB;
        Assert.True(tokenA.IsCancellationRequested);
        releaseA.SetResult(Window(ContextA, new(100, 60, 20, 20, requestGeneration: runtime.Generation - 1), 100_000));
        await loadA;
        Assert.Equal(500, runtime.RequestedViewportStartIndex);
        Assert.Contains("501", runtime.Rows[20].RowKey.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompanySwitchRejectsLateOldCompanyWindowAndClearsIdentityState()
    {
        var releaseA = new TaskCompletionSource<GridViewportResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = Provider((context, request, _) => context.Company.CompanyId == CompanyA.CompanyId
            ? releaseA.Task : Task.FromResult(Window(context, request, 100_000)));
        var runtime = Runtime(provider);
        var oldLoad = runtime.LoadAsync(ContextA, null);
        await runtime.LoadAsync(new(CompanyB, "workspace"), null);
        releaseA.SetResult(Window(ContextA, new(0, 60, 20, 20, requestGeneration: runtime.Generation - 1), 100_000));
        await oldLoad;
        Assert.All(runtime.Rows, row => Assert.StartsWith("b:", row.RowKey.Value, StringComparison.Ordinal));
        Assert.Empty(runtime.SelectedRowKeys);
    }

    [Fact]
    public async Task SelectionAndEditCandidateSurviveDistantWindowsWithoutHoldingRowViewModels()
    {
        var runtime = Runtime(Provider((context, request, _) => Task.FromResult(Window(context, request, 100_000))));
        await runtime.LoadAsync(ContextA, null);
        var selected = runtime.Rows[20].RowKey;
        runtime.ToggleSelection(selected);
        Assert.True(runtime.BeginEdit(selected, new("VALUE")));
        runtime.SetCandidate("draft");
        await runtime.RequestViewportAsync(50_000, 60);
        Assert.Contains(selected, runtime.SelectedRowKeys);
        Assert.Equal("draft", runtime.EditBuffer?.CandidateValue);
        Assert.DoesNotContain(runtime.Rows, x => x.RowKey == selected);
        Assert.True((await runtime.CommitEditAsync()).IsSuccess);
        await runtime.RequestViewportAsync(0, 60);
        Assert.Contains(selected, runtime.SelectedRowKeys);
        Assert.Equal("draft", runtime.GetValue(selected, new("VALUE"), out _));
        Assert.Null(runtime.EditBuffer);
    }

    [Fact]
    public async Task SortFilterAndRefreshInvalidateMappingAndAdvanceGeneration()
    {
        var calls = 0;
        var runtime = Runtime(Provider((context, request, _) =>
        {
            calls++;
            var total = request.FilterDefinitions.Length == 0 ? 100_000 : 25_000;
            return Task.FromResult(Window(context, request, total));
        }));
        await runtime.LoadAsync(ContextA, null);
        await runtime.RequestViewportAsync(500, 60);
        var beforeSort = runtime.Generation;
        await runtime.SetSortAsync([new(new("VALUE"), GridSortDirection.Descending)], null);
        Assert.True(runtime.Generation > beforeSort); Assert.Equal(0, runtime.RequestedViewportStartIndex);
        await runtime.SetFiltersAsync([new(new("VALUE"), GridFilterOperator.Contains, "2")], null);
        Assert.Equal(25_000, runtime.TotalRows);
        var beforeRefresh = runtime.Generation;
        await runtime.RefreshAsync();
        Assert.True(runtime.Generation > beforeRefresh); Assert.True(calls >= 5);
    }

    [Fact]
    public async Task CacheEvictsAfterManyDistantWindowsAndNeverRetainsLogicalDataset()
    {
        var runtime = Runtime(Provider((context, request, _) => Task.FromResult(Window(context, request, 100_000))));
        await runtime.LoadAsync(ContextA, null);
        for (var index = 1; index <= 12; index++) await runtime.RequestViewportAsync(index * 5_000, 60);
        Assert.InRange(runtime.CachedWindowCount, 1, 3);
        Assert.InRange(runtime.CachedRowCount, 1, 300);
        Assert.True(runtime.CachedRowCount < runtime.TotalRows);
    }

    [Fact]
    public async Task FewerRowsEmptyLastWindowMalformedResultFailureAndRetryAreSafe()
    {
        var malformed = true;
        var provider = Provider((context, request, _) =>
        {
            if (malformed)
                return Task.FromResult(new GridViewportResult(GridProviderState.Ready, request.MaterializedStartIndex + 1,
                    [], 100_000, request.RequestGeneration, false, true));
            return Task.FromResult(Window(context, request, 12));
        });
        var runtime = Runtime(provider);
        await runtime.LoadAsync(ContextA, null);
        Assert.Equal(GridProviderState.Error, runtime.State);
        Assert.Equal("GRID_VIEWPORT_RESULT_MALFORMED", runtime.DiagnosticCode);
        malformed = false; await runtime.RetryViewportAsync();
        Assert.Equal(12, runtime.Rows.Length);
        Assert.Equal(12, runtime.TotalRows);
        Assert.False(runtime.HasNextViewport);
    }

    [Fact]
    public async Task DeactivateCancelsWorkspaceRequestAndRejectsLateResult()
    {
        var release = new TaskCompletionSource<GridViewportResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken requestToken = default;
        var provider = Provider((context, request, token) => { requestToken = token; return release.Task; });
        var runtime = Runtime(provider);
        var load = runtime.LoadAsync(ContextA, null);
        runtime.Deactivate();
        Assert.True(requestToken.IsCancellationRequested);
        release.SetResult(Window(ContextA, new(0, 60, 20, 20, requestGeneration: runtime.Generation - 1), 100_000));
        await load;
        Assert.Empty(runtime.Rows); Assert.Equal(GridProviderState.Unavailable, runtime.State);
    }

    private static DataEntryGridRuntime Runtime(TestVirtualProvider provider) =>
        new(Definition(), provider, new(60, 20, 20, 3, 120));

    private static TestVirtualProvider Provider(
        Func<GridProviderContext, GridViewportRequest, CancellationToken, Task<GridViewportResult>> load) => new(load);

    private static GridViewportResult Window(GridProviderContext context, GridViewportRequest request, int total)
    {
        var start = request.MaterializedStartIndex;
        var count = Math.Max(0, Math.Min(request.MaterializedRowCount, total - start));
        var rows = Enumerable.Range(start + 1, count).Select(index => new GridRow(
            new($"{context.Company.CompanyId.Value}:{index}"),
            new Dictionary<VariableCode, object?> { [new("VALUE")] = $"value-{index}" })).ToImmutableArray();
        return new(rows.Length == 0 ? GridProviderState.Empty : GridProviderState.Ready, start, rows, total,
            request.RequestGeneration, start > 0, start + rows.Length < total);
    }

    private static GridDefinition Definition() => new("virtual", "VIRTUAL",
        [new ColumnDefinition("value", "VALUE", new("VALUE"), "Grid.Value", null, ColumnDataType.Text,
            ColumnEditorKind.TextBox, ColumnMode.Input, 0, 120, 60, 300, true, false, null, null, null, null,
            null, 1, SetupDefinitionStatus.Published)], selectionMode: GridSelectionMode.Multiple, allowEdit: true);

    private sealed class TestVirtualProvider(
        Func<GridProviderContext, GridViewportRequest, CancellationToken, Task<GridViewportResult>> load)
        : IVirtualizedGridDataProvider
    {
        public Task<GridViewportResult> LoadViewportAsync(GridProviderContext context, GridViewportRequest request,
            CancellationToken cancellationToken = default) => load(context, request, cancellationToken);
        public Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit,
            CancellationToken cancellationToken = default) => Task.FromResult(GridCommitResult.Success(edit.CandidateValue));
    }
}
