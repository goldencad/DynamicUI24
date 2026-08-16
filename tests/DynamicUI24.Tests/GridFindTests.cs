using System.Collections.Immutable;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Setup;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class GridFindTests
{
    private static readonly VariableCode Code = new("CODE");
    private static readonly VariableCode Name = new("NAME");
    private static readonly VariableCode Secret = new("SECRET");
    private static readonly GridProviderContext A = new(new(new("a"), "A", "A"), "workspace");
    private static readonly GridProviderContext B = new(new(new("b"), "B", "B"), "workspace");

    [Fact]
    public async Task CurrentColumnAndAllVisibleNavigateBySemanticIdentity()
    {
        var provider = new FindProvider(); var runtime = Runtime(provider); await runtime.LoadAsync(A, null);
        var current = await runtime.FindAsync("CODE-000042", GridFindScope.CurrentColumn, Code);
        Assert.True(current.IsMatch); Assert.Equal(new RowKey("a:42"), current.RowKey); Assert.Equal(Code, current.VariableCode);
        Assert.Equal(new GridCellAddress(new("a:42"), Code), runtime.ActiveCell);
        var all = await runtime.FindAsync("Name 000043", GridFindScope.AllVisibleColumns);
        Assert.True(all.IsMatch); Assert.Equal(Name, all.VariableCode);
        var previous = await runtime.FindAsync("Name", GridFindScope.AllVisibleColumns,
            direction: GridFindDirection.Previous);
        Assert.True(previous.IsMatch); Assert.True(previous.LogicalPosition < all.LogicalPosition);
    }

    [Fact]
    public async Task FarFindRemainsBoundedAndHonorsCurrentFilters()
    {
        var provider = new FindProvider(); var runtime = Runtime(provider); await runtime.LoadAsync(A, null);
        await runtime.SetFiltersAsync([new(Name, GridFilterOperator.Contains, "90000")], null);
        var result = await runtime.FindAsync("90000", GridFindScope.AllVisibleColumns);
        Assert.True(result.IsMatch); Assert.Equal(new RowKey("a:90000"), result.RowKey);
        Assert.True(runtime.Rows.Length <= runtime.ViewportOptions.MaximumMaterializedRows);
        Assert.True(runtime.CachedRowCount <= runtime.ViewportOptions.MaximumMaterializedRows *
            runtime.ViewportOptions.MaximumCachedWindows);
        Assert.Equal(0, provider.VisualCellIterations);
    }

    [Fact]
    public async Task NoMatchPrivacyAndStaleContextFailClosed()
    {
        var provider = new FindProvider(); var runtime = Runtime(provider); await runtime.LoadAsync(A, null);
        Assert.Equal("GRID_FIND_COLUMN_RESTRICTED",
            (await runtime.FindAsync("secret", GridFindScope.CurrentColumn, Secret)).DiagnosticCode);
        Assert.DoesNotContain(Secret, provider.LastEligibleColumns);
        Assert.Equal("GRID_FIND_NO_MATCH",
            (await runtime.FindAsync("does-not-exist", GridFindScope.AllVisibleColumns)).DiagnosticCode);
        provider.Delay = true;
        var pending = runtime.FindAsync("CODE-000042", GridFindScope.CurrentColumn, Code);
        await provider.Started.Task; await runtime.LoadAsync(B, null); provider.Release.SetResult();
        Assert.Equal("GRID_STALE_FIND_RESULT", (await pending).DiagnosticCode);
    }

    [Fact]
    public async Task CurrentRowUsesSameEngineAndRememberedScopeFallsBackSafely()
    {
        var provider = new FindProvider(); var runtime = Runtime(provider); await runtime.LoadAsync(A, null);
        var row = runtime.Rows[4].RowKey; runtime.SelectCell(new(row, Code), 4);
        var result = await runtime.FindAsync("Name 000005", GridFindScope.CurrentRow, Code, row);
        Assert.True(result.IsMatch); Assert.Equal(row, result.RowKey); Assert.Equal(Name, result.VariableCode);
        var rows = runtime.Rows.Length; var cached = runtime.CachedRowCount;
        runtime.RememberFindScope(GridFindScope.CurrentRow);
        Assert.Equal(GridFindScope.CurrentRow, runtime.ResolveFindScope(GridFindScope.AllVisibleColumns, row, Code));
        Assert.Equal(GridFindScope.AllVisibleColumns,
            runtime.ResolveFindScope(GridFindScope.AllVisibleColumns, new("missing"), Code));
        Assert.Equal(rows, runtime.Rows.Length); Assert.Equal(cached, runtime.CachedRowCount);
        Assert.Null(runtime.CurrentViewPreference.ViewName);
    }

    [Fact]
    public async Task CopyRowAndClearEditableValuesReusePrivacyAndTransactionPaths()
    {
        var provider = new FindProvider(); var runtime = Runtime(provider); await runtime.LoadAsync(A, null);
        var row = runtime.Rows[0].RowKey; var clipboard = new MemoryClipboard();
        var copy = await runtime.CopyRowAsync(row, clipboard);
        Assert.Null(copy.DiagnosticCode); Assert.DoesNotContain("secret", clipboard.Text, StringComparison.OrdinalIgnoreCase);
        var clear = await runtime.ClearRowEditableValuesAsync(row);
        Assert.Equal(2, clear.AppliedCellCount); Assert.Equal(0, runtime.PendingChangeCount);
        Assert.Null(runtime.GetValue(row, Code, out _)); Assert.Null(runtime.GetValue(row, Name, out _));
        Assert.Equal("secret", runtime.GetValue(row, Secret, out _));
    }

    private static DataEntryGridRuntime Runtime(FindProvider provider) => new(new("grid", "GRID",
    [
        Column(Code, 0), Column(Name, 1),
        Column(Secret, 2, new(Sensitivity.Restricted, PrivacyPresentation.Mask)),
    ], allowEdit: true), provider, new(visibleRowCount: 20, overscanBefore: 5, overscanAfter: 5,
        maximumCachedWindows: 2, maximumMaterializedRows: 40));

    private static ColumnDefinition Column(VariableCode code, int order, SensitiveContentDefinition? sensitive = null) =>
        new(code.Value, code.Value, code, code.Value, null, ColumnDataType.Text, ColumnEditorKind.TextBox,
            sensitive is null ? ColumnMode.Input : ColumnMode.System, order, 120, 60, 300, true, false, null,
            null, null, null, null, 1, SetupDefinitionStatus.Published, sensitive);

    private sealed class FindProvider : IVirtualizedGridDataProvider, IGridFindProvider
    {
        public bool Delay { get; set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ImmutableArray<VariableCode> LastEligibleColumns { get; private set; } = [];
        public int VisualCellIterations => 0;
        public async Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await LoadViewportAsync(context, new(0, 20, 0, 0, request.Sorts, request.Filters,
                request.Generation), cancellationToken);
            return new(result.State, result.Rows, result.TotalRowCount, result.TotalRowCount);
        }
        public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit,
            CancellationToken cancellationToken = default) => Task.FromResult(GridCommitResult.Success(edit.CandidateValue));
        public Task<GridViewportResult> LoadViewportAsync(GridProviderContext context, GridViewportRequest request,
            CancellationToken cancellationToken = default)
        {
            var matching = Enumerable.Range(1, 100_000).Where(i => Matches(i, request.FilterDefinitions)).ToArray();
            var rows = matching.Skip(request.MaterializedStartIndex).Take(request.MaterializedRowCount)
                .Select(i => Row(context, i)).ToImmutableArray();
            return Task.FromResult(new GridViewportResult(rows.Length == 0 ? GridProviderState.Empty : GridProviderState.Ready,
                request.MaterializedStartIndex, rows, matching.Length, request.RequestGeneration,
                request.MaterializedStartIndex > 0, request.MaterializedStartIndex + rows.Length < matching.Length));
        }
        public async Task<GridFindResult> FindAsync(GridProviderContext context, GridFindRequest request,
            CancellationToken cancellationToken = default)
        {
            LastEligibleColumns = request.EligibleVariableCodes;
            if (Delay) { Started.SetResult(); await Release.Task.WaitAsync(cancellationToken); }
            var eligible = Enumerable.Range(1, 100_000).Where(i => Matches(i, request.Filters)).ToArray();
            var columns = request.Scope == GridFindScope.CurrentColumn ? [request.VariableCode!.Value] : request.EligibleVariableCodes;
            if (request.Scope == GridFindScope.CurrentRow && request.RowKey is { } rowKey)
            {
                var value = int.Parse(rowKey.Value[(rowKey.Value.LastIndexOf(':') + 1)..]);
                var match = columns.FirstOrDefault(column => Value(value, column).Contains(request.Query,
                    StringComparison.OrdinalIgnoreCase));
                return match == default ? GridFindResult.NoMatch(request.RequestGeneration) :
                    GridFindResult.Match(rowKey, match, request.StartPosition, request.RequestGeneration);
            }
            var candidates = request.Direction == GridFindDirection.Next
                ? eligible.Select((value, position) => (value, position)).Where(x => x.position > request.StartPosition)
                    .Concat(eligible.Select((value, position) => (value, position)).Where(x => x.position < request.StartPosition))
                : eligible.Select((value, position) => (value, position)).Where(x => x.position < request.StartPosition).Reverse()
                    .Concat(eligible.Select((value, position) => (value, position)).Where(x => x.position > request.StartPosition).Reverse());
            foreach (var item in candidates)
                foreach (var column in columns)
                    if (Value(item.value, column).Contains(request.Query, StringComparison.OrdinalIgnoreCase))
                        return GridFindResult.Match(new($"{context.Company.CompanyId.Value}:{item.value}"), column,
                            item.position, request.RequestGeneration);
            return GridFindResult.NoMatch(request.RequestGeneration);
        }
        private static GridRow Row(GridProviderContext context, int i) => new(new($"{context.Company.CompanyId.Value}:{i}"),
            new Dictionary<VariableCode, object?> { [Code] = Value(i, Code), [Name] = Value(i, Name), [Secret] = "secret" });
        private static string Value(int i, VariableCode code) => code == Code ? $"CODE-{i:000000}" : $"Name {i:000000}";
        private static bool Matches(int i, ImmutableArray<GridFilterDefinition> filters) => filters.All(x =>
            x.Operator != GridFilterOperator.Contains || Value(i, x.VariableCode).Contains(x.Value?.ToString() ?? "",
                StringComparison.OrdinalIgnoreCase));
    }

    private sealed class MemoryClipboard : IGridClipboardService
    {
        public string Text { get; private set; } = string.Empty;
        public Task<string?> ReadTextAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(Text);
        public Task WriteTextAsync(string text, CancellationToken cancellationToken = default)
        { Text = text; return Task.CompletedTask; }
    }
}
