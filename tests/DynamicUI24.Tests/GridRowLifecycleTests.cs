using System.Collections.Immutable;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class GridRowLifecycleTests
{
    private static readonly GridProviderContext ContextA = new(new(new("a"), "A", "A"), "workspace");
    private static readonly GridProviderContext ContextB = new(new(new("b"), "B", "B"), "workspace");

    [Fact]
    public async Task InsertAboveAndBelowReturnProviderRowKeysAndActivateInsertedRow()
    {
        var provider = new LifecycleProvider(); var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        var anchor = provider.Rows[1].RowKey;
        var above = await runtime.InsertRowAsync(anchor, GridRowInsertPlacement.Before);
        Assert.True(above.IsSuccess); Assert.NotNull(above.InsertedRowKey); Assert.NotEqual(anchor, above.InsertedRowKey);
        Assert.Equal(above.InsertedRowKey, runtime.ActiveCell?.RowKey);
        Assert.True(Array.FindIndex(runtime.Rows.ToArray(), x => x.RowKey == above.InsertedRowKey) <
            Array.FindIndex(runtime.Rows.ToArray(), x => x.RowKey == anchor));
        var below = await runtime.InsertRowAsync(anchor, GridRowInsertPlacement.After);
        Assert.True(below.IsSuccess);
        Assert.True(Array.FindIndex(runtime.Rows.ToArray(), x => x.RowKey == below.InsertedRowKey) >
            Array.FindIndex(runtime.Rows.ToArray(), x => x.RowKey == anchor));
        Assert.Equal(5, runtime.TotalRows); Assert.Equal(2, provider.CalculationInvalidations);
    }

    [Fact]
    public async Task DeleteSelectedRowsClearsSelectionActiveEditAndPendingForDeletedKeys()
    {
        var provider = new LifecycleProvider(); var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        var deleted = provider.Rows.Take(2).Select(x => x.RowKey).ToArray();
        Assert.True(runtime.BeginEdit(deleted[0], new("VALUE"))); runtime.SetCandidate("pending");
        Assert.True((await runtime.CommitEditAsync()).IsSuccess); Assert.Equal(1, runtime.PendingChangeCount);
        runtime.Select(deleted);
        var result = await runtime.DeleteSelectedRowsAsync();
        Assert.True(result.IsSuccess); Assert.Equal(deleted.ToHashSet(), result.DeletedRowKeys.ToHashSet());
        Assert.Equal(0, runtime.PendingChangeCount); Assert.DoesNotContain(runtime.Rows, x => deleted.Contains(x.RowKey));
        Assert.DoesNotContain(runtime.SelectedRowKeys, deleted.Contains);
        Assert.NotNull(runtime.ActiveCell); Assert.DoesNotContain(runtime.ActiveCell!.Value.RowKey, deleted);
        Assert.Equal(1, runtime.TotalRows); Assert.Equal(1, provider.CalculationInvalidations);
    }

    [Fact]
    public async Task LifecycleFailsClosedForMetadataCapabilityAndProviderRejection()
    {
        var provider = new LifecycleProvider { CanInsertRows = false, CanDeleteRows = false };
        var runtime = Runtime(provider); await runtime.LoadAsync(ContextA, null);
        Assert.Equal("GRID_ROW_INSERT_DENIED", (await runtime.InsertRowAsync(provider.Rows[0].RowKey,
            GridRowInsertPlacement.After)).DiagnosticCode);
        Assert.Equal("GRID_ROW_DELETE_DENIED", (await runtime.DeleteRowAsync(provider.Rows[0].RowKey)).DiagnosticCode);
        provider.CanInsertRows = true; provider.CanDeleteRows = true; provider.Reject = true;
        Assert.Equal("DEMO_REJECT", (await runtime.InsertRowAsync(provider.Rows[0].RowKey,
            GridRowInsertPlacement.After)).DiagnosticCode);
        Assert.Equal("DEMO_REJECT", (await runtime.DeleteRowAsync(provider.Rows[0].RowKey)).DiagnosticCode);
    }

    [Fact]
    public async Task LateLifecycleResultIsRejectedAfterContextGenerationChanges()
    {
        var provider = new LifecycleProvider { DelayInsert = true }; var runtime = Runtime(provider);
        await runtime.LoadAsync(ContextA, null); var anchor = provider.Rows[0].RowKey;
        var insert = runtime.InsertRowAsync(anchor, GridRowInsertPlacement.After);
        await provider.InsertStarted.Task; await runtime.LoadAsync(ContextB, null); provider.InsertRelease.SetResult();
        Assert.Equal("GRID_STALE_ROW_RESULT", (await insert).DiagnosticCode);
    }

    [Fact]
    public async Task VirtualLifecycleRefreshesBoundedWindowAndMakesInsertedRowObservable()
    {
        var provider = new VirtualLifecycleProvider();
        var runtime = new DataEntryGridRuntime(Runtime(provider.Inner).Definition, provider,
            new GridViewportOptions(2, 0, 0, 2, 4));
        await runtime.LoadAsync(ContextA, null); var anchor = runtime.Rows[1].RowKey;
        var beforeCount = runtime.TotalRows; var requests = provider.ViewportRequests;
        var result = await runtime.InsertRowAsync(anchor, GridRowInsertPlacement.After);
        Assert.True(result.IsSuccess); Assert.Equal(beforeCount + 1, runtime.TotalRows);
        Assert.True(provider.ViewportRequests > requests); Assert.Contains(runtime.Rows, x => x.RowKey == result.InsertedRowKey);
        Assert.Equal(result.InsertedRowKey, runtime.ActiveCell?.RowKey);
        Assert.True(runtime.Rows.Length <= runtime.ViewportOptions.MaximumMaterializedRows);
    }

    private static DataEntryGridRuntime Runtime(LifecycleProvider provider) => new(new GridDefinition("grid", "GRID",
        [new ColumnDefinition("value", "VALUE", new("VALUE"), "Value", null, ColumnDataType.Text,
            ColumnEditorKind.TextBox, ColumnMode.Input, 0, 120, 60, 300, true, false, null, null, null, null, null, 1,
            SetupDefinitionStatus.Published)], selectionMode: GridSelectionMode.Multiple, allowEdit: true,
        allowAdd: true, allowDelete: true), provider);

    private sealed class LifecycleProvider : IDataEntryGridProvider, IGridRowLifecycleProvider, IGridRowCalculationInvalidation
    {
        private int identity = 3;
        public List<GridRow> Rows { get; } = Enumerable.Range(1, 3).Select(x =>
            new GridRow(new($"row:{x}"), new Dictionary<VariableCode, object?> { [new("VALUE")] = $"value-{x}" })).ToList();
        public bool CanInsertRows { get; set; } = true;
        public bool CanDeleteRows { get; set; } = true;
        public bool Reject { get; set; }
        public bool DelayInsert { get; set; }
        public TaskCompletionSource InsertStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource InsertRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int CalculationInvalidations { get; private set; }
        public Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request,
            CancellationToken cancellationToken = default) => Task.FromResult(GridDataResult.Ready(Rows));
        public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit,
            CancellationToken cancellationToken = default) => Task.FromResult(GridCommitResult.Success(edit.CandidateValue));
        public async Task<GridRowInsertResult> InsertRowAsync(GridProviderContext context, GridRowInsertRequest request,
            CancellationToken cancellationToken = default)
        {
            if (Reject) return GridRowInsertResult.Rejected("DEMO_REJECT");
            if (DelayInsert) { InsertStarted.SetResult(); await InsertRelease.Task.WaitAsync(cancellationToken); }
            var index = Rows.FindIndex(x => x.RowKey == request.AnchorRowKey);
            var key = new RowKey($"insert:{Interlocked.Increment(ref identity)}");
            Rows.Insert(index + (request.Placement == GridRowInsertPlacement.After ? 1 : 0), new(key, request.InitialValues));
            return GridRowInsertResult.Success(key, Rows.Count, request.AnchorLogicalPosition +
                (request.Placement == GridRowInsertPlacement.After ? 1 : 0));
        }
        public Task<GridRowDeleteResult> DeleteRowsAsync(GridProviderContext context, GridRowDeleteRequest request,
            CancellationToken cancellationToken = default)
        {
            if (Reject) return Task.FromResult(GridRowDeleteResult.Rejected("DEMO_REJECT"));
            Rows.RemoveAll(x => request.RowKeys.Contains(x.RowKey));
            return Task.FromResult(GridRowDeleteResult.Success(request.RowKeys, Rows.Count));
        }
        public Task InvalidateRowsAsync(GridProviderContext context, IEnumerable<RowKey> changedRows,
            CancellationToken cancellationToken = default) { CalculationInvalidations++; return Task.CompletedTask; }
    }

    private sealed class VirtualLifecycleProvider : IVirtualizedGridDataProvider, IGridRowLifecycleProvider
    {
        public LifecycleProvider Inner { get; } = new();
        public int ViewportRequests { get; private set; }
        public bool CanInsertRows => true; public bool CanDeleteRows => true;
        public Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request,
            CancellationToken cancellationToken = default) => Inner.LoadAsync(context, request, cancellationToken);
        public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit,
            CancellationToken cancellationToken = default) => Inner.CommitAsync(context, edit, cancellationToken);
        public Task<GridViewportResult> LoadViewportAsync(GridProviderContext context, GridViewportRequest request,
            CancellationToken cancellationToken = default)
        {
            ViewportRequests++;
            var rows = Inner.Rows.Skip(request.MaterializedStartIndex).Take(request.MaterializedRowCount).ToImmutableArray();
            return Task.FromResult(new GridViewportResult(rows.Length == 0 ? GridProviderState.Empty : GridProviderState.Ready,
                request.MaterializedStartIndex, rows, Inner.Rows.Count, request.RequestGeneration,
                request.MaterializedStartIndex > 0, request.MaterializedStartIndex + rows.Length < Inner.Rows.Count));
        }
        public Task<GridRowInsertResult> InsertRowAsync(GridProviderContext context, GridRowInsertRequest request,
            CancellationToken cancellationToken = default) => Inner.InsertRowAsync(context, request, cancellationToken);
        public Task<GridRowDeleteResult> DeleteRowsAsync(GridProviderContext context, GridRowDeleteRequest request,
            CancellationToken cancellationToken = default) => Inner.DeleteRowsAsync(context, request, cancellationToken);
    }
}
