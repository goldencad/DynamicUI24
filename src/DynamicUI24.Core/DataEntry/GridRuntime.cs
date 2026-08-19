using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Setup;
using DynamicUI24.Core.Privacy;

namespace DynamicUI24.Core.DataEntry;

public sealed record GridValidationDiagnostic(string Code, string MessageKey);
public sealed record GridEditBuffer(RowKey RowKey, VariableCode VariableCode, object? SourceValue,
    object? CandidateValue, GridValidationDiagnostic? Diagnostic = null)
{
    public bool IsDirty => !Equals(SourceValue, CandidateValue);
}

public sealed class GridRuntimeChangedEventArgs(string reason, GridCellAddress? cell = null) : EventArgs
{
    public string Reason { get; } = reason;
    public GridCellAddress? Cell { get; } = cell;
}

/// <summary>UI-platform-free state machine for loading, selection, sorting, filtering and one-cell editing.</summary>
public sealed partial class DataEntryGridRuntime
{
    private readonly IDataEntryGridProvider provider;
    private readonly IVirtualizedGridDataProvider? viewportProvider;
    private readonly GridWindowCache windowCache;
    private CancellationTokenSource? activeRequest;
    private long generation;
    private GridProviderContext? context;
    private EffectiveAuthorizationContext? authorization;
    private readonly GridEditHistory editHistory;
    private readonly IPrivacyPolicyResolver privacyResolver;
    private readonly IPrivacyStateService privacyState;
    private readonly ISensitiveValuePresenter sensitiveValuePresenter;
    private readonly Dictionary<(RowKey RowKey, VariableCode VariableCode), GridCellChange> pendingChanges = [];

    public DataEntryGridRuntime(GridDefinition definition, IDataEntryGridProvider provider,
        GridViewportOptions? viewportOptions = null, GridPasteOptions? pasteOptions = null,
        IPrivacyPolicyResolver? privacyResolver = null, IPrivacyStateService? privacyState = null,
        ISensitiveValuePresenter? sensitiveValuePresenter = null, GridRowHeightOptions? rowHeightOptions = null)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        viewportProvider = provider as IVirtualizedGridDataProvider;
        ViewportOptions = viewportOptions ?? new();
        RowHeightOptions = rowHeightOptions ?? new();
        windowCache = new(ViewportOptions.MaximumCachedWindows);
        PasteOptions = pasteOptions ?? new();
        editHistory = new(PasteOptions.HistoryDepth);
        this.privacyResolver = privacyResolver ?? new PrivacyPolicyResolver();
        this.privacyState = privacyState ?? new PrivacyStateService();
        this.sensitiveValuePresenter = sensitiveValuePresenter ?? new SensitiveValuePresenter();
        Sorts = definition.DefaultSort;
        Filters = definition.DefaultFilter;
        ResolvedDefinition = GridMetadataResolver.Resolve(definition, null);
    }

    public GridDefinition Definition { get; }
    public GridViewportOptions ViewportOptions { get; }
    public GridPasteOptions PasteOptions { get; }
    public bool IsVirtualized => viewportProvider is not null;
    public ResolvedGridDefinition ResolvedDefinition { get; private set; }
    public GridProviderState State { get; private set; } = GridProviderState.Loading;
    public ImmutableArray<GridRow> Rows { get; private set; } = [];
    public ImmutableHashSet<RowKey> SelectedRowKeys { get; private set; } = [];
    public GridSelectionState CellSelection { get; private set; } = GridSelectionState.Empty;
    public ImmutableArray<GridSortDefinition> Sorts { get; private set; }
    public ImmutableArray<GridFilterDefinition> Filters { get; private set; }
    public GridEditBuffer? EditBuffer { get; private set; }
    public int TotalRows { get; private set; }
    public int VisibleRows { get; private set; }
    public int ViewportStartIndex { get; private set; }
    public int RequestedViewportStartIndex { get; private set; }
    public int RequestedViewportRowCount { get; private set; }
    public bool HasPreviousViewport { get; private set; }
    public bool HasNextViewport { get; private set; }
    public int CachedWindowCount => windowCache.WindowCount;
    public int CachedRowCount => windowCache.RowCount;
    public long Generation => Volatile.Read(ref generation);
    public string? DiagnosticCode { get; private set; }
    public int SelectionCount => SelectedRowKeys.Count;
    public int SelectedCellCount => GetSelectedCellCount();
    public int InteractionSelectionCount => SelectedCellCount > 0 ? SelectedCellCount : SelectionCount;
    public bool CanUndo => editHistory.CanUndo;
    public bool CanRedo => editHistory.CanRedo;
    public GridPasteResult? LastPasteResult { get; private set; }
    public int ErrorCount => Rows.Sum(x => x.ErrorCount) + (EditBuffer?.Diagnostic is null ? 0 : 1);
    public int WarningCount => Rows.Sum(x => x.WarningCount);
    public int PendingChangeCount => pendingChanges.Count + (EditBuffer?.IsDirty == true ? 1 : 0);
    public event EventHandler<GridRuntimeChangedEventArgs>? Changed;

    public ActionBarStatus Status
    {
        get
        {
            var range = CellSelection.PrimaryRange;
            return new(TotalRows, VisibleRows, SelectionCount, ErrorCount, WarningCount,
                PendingChangeCount, !ResolvedDefinition.CanEdit, SelectedCellCount,
                range?.RowCount, range is null ? null : RangeColumnCount(range));
        }
    }

    public async Task LoadAsync(GridProviderContext newContext, EffectiveAuthorizationContext? authorization,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newContext);
        var contextChanged = context is null || context.Company.CompanyId != newContext.Company.CompanyId ||
            !context.WorkspaceId.Equals(newContext.WorkspaceId, StringComparison.OrdinalIgnoreCase);
        context = newContext;
        this.authorization = authorization;
        if (contextChanged)
        {
            SelectedRowKeys = []; CellSelection = GridSelectionState.Empty; EditBuffer = null; Rows = []; TotalRows = 0; VisibleRows = 0;
            LastPasteResult = null; editHistory.Clear(); pendingChanges.Clear();
            ViewportStartIndex = 0; RequestedViewportStartIndex = 0; windowCache.Clear();
        }
        ResolvedDefinition = GridMetadataResolver.Resolve(Definition, authorization);
        if (viewportProvider is not null)
        {
            await RequestViewportAsync(contextChanged ? 0 : RequestedViewportStartIndex,
                RequestedViewportRowCount > 0 ? RequestedViewportRowCount : ViewportOptions.VisibleRowCount,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var (requestGeneration, requestToken) = BeginRequest(cancellationToken);
        State = GridProviderState.Loading; DiagnosticCode = null; OnChanged("LOADING");
        try
        {
            var result = await provider.LoadAsync(newContext, new(Sorts, Filters, requestGeneration), requestToken)
                .ConfigureAwait(false);
            if (requestGeneration != Volatile.Read(ref generation)) return;
            if (result.Rows.GroupBy(x => x.RowKey).Any(x => x.Count() > 1))
            {
                ApplyFailure(GridProviderState.Error, "GRID_DUPLICATE_ROW_KEY"); return;
            }
            Rows = result.Rows; TotalRows = Math.Max(0, result.TotalRows); VisibleRows = Math.Max(0, result.VisibleRows);
            State = result.State; DiagnosticCode = result.DiagnosticCode;
            SelectedRowKeys = SelectedRowKeys.Intersect(Rows.Select(x => x.RowKey)).ToImmutableHashSet();
            ReconcileCellSelection();
            OnChanged("LOADED");
        }
        catch (OperationCanceledException) when (requestGeneration != Volatile.Read(ref generation)) { }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { if (requestGeneration == Volatile.Read(ref generation)) ApplyFailure(GridProviderState.Error, "GRID_PROVIDER_FAILED"); }
    }

    public async Task RequestViewportAsync(int startIndex, int requestedRowCount,
        CancellationToken cancellationToken = default)
    {
        if (viewportProvider is null) throw new InvalidOperationException("GRID_PROVIDER_NOT_VIRTUALIZED");
        if (context is null) throw new InvalidOperationException("GRID_CONTEXT_UNAVAILABLE");
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        if (requestedRowCount <= 0 ||
            (long)requestedRowCount + ViewportOptions.OverscanBefore + ViewportOptions.OverscanAfter > ViewportOptions.MaximumMaterializedRows)
            throw new ArgumentOutOfRangeException(nameof(requestedRowCount));

        var boundedStart = TotalRows > 0 ? Math.Min(startIndex, Math.Max(0, TotalRows - 1)) : startIndex;
        RequestedViewportStartIndex = boundedStart;
        RequestedViewportRowCount = requestedRowCount;
        var key = new GridWindowKey(boundedStart, requestedRowCount);
        var (requestGeneration, requestToken) = BeginRequest(cancellationToken);
        if (windowCache.TryGet(key, out var cached))
        {
            ApplyViewport(cached with { RequestGeneration = requestGeneration }, key, requestGeneration);
            return;
        }

        var request = new GridViewportRequest(boundedStart, requestedRowCount,
            ViewportOptions.OverscanBefore, ViewportOptions.OverscanAfter, Sorts, Filters, requestGeneration);
        var initialLoad = Rows.Length == 0;
        if (initialLoad) State = GridProviderState.Loading;
        DiagnosticCode = null; OnChanged(initialLoad ? "VIEWPORT_LOADING" : "VIEWPORT_FETCHING");
        try
        {
            var result = await viewportProvider.LoadViewportAsync(context, request, requestToken).ConfigureAwait(false);
            if (requestGeneration != Volatile.Read(ref generation)) return;
            if (result.RequestGeneration != requestGeneration) { ApplyViewportFailure("GRID_VIEWPORT_GENERATION_INVALID"); return; }
            if (result.State is GridProviderState.Error or GridProviderState.Unavailable)
            {
                State = result.State; DiagnosticCode = result.DiagnosticCode ?? "GRID_VIEWPORT_FAILED";
                OnChanged("VIEWPORT_FAILURE"); return;
            }
            if (!IsValid(result, request)) { ApplyViewportFailure("GRID_VIEWPORT_RESULT_MALFORMED"); return; }
            windowCache.Set(key, result);
            ApplyViewport(result, key, requestGeneration);
        }
        catch (OperationCanceledException) when (requestGeneration != Volatile.Read(ref generation)) { }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { if (requestGeneration == Volatile.Read(ref generation)) ApplyViewportFailure("GRID_PROVIDER_FAILED"); }
    }

    public Task RetryViewportAsync(CancellationToken cancellationToken = default) => viewportProvider is null
        ? context is null ? Task.CompletedTask : LoadAsync(context, null, cancellationToken)
        : RequestViewportAsync(RequestedViewportStartIndex,
            RequestedViewportRowCount > 0 ? RequestedViewportRowCount : ViewportOptions.VisibleRowCount, cancellationToken);

    public Task RefreshAsync(EffectiveAuthorizationContext? authorization = null,
        CancellationToken cancellationToken = default)
    {
        windowCache.Clear();
        return context is null ? Task.CompletedTask : LoadAsync(context, authorization, cancellationToken);
    }

    public Task ResizeViewportAsync(int visibleRowCount, CancellationToken cancellationToken = default) =>
        RequestViewportAsync(RequestedViewportStartIndex, Math.Min(visibleRowCount,
            ViewportOptions.MaximumMaterializedRows - ViewportOptions.OverscanBefore - ViewportOptions.OverscanAfter), cancellationToken);

    public void Deactivate()
    {
        Interlocked.Increment(ref generation);
        activeRequest?.Cancel(); activeRequest?.Dispose(); activeRequest = null;
        windowCache.Clear(); Rows = []; TotalRows = 0; VisibleRows = 0; State = GridProviderState.Unavailable;
        SelectedRowKeys = []; CellSelection = GridSelectionState.Empty; EditBuffer = null; LastPasteResult = null;
        editHistory.Clear(); pendingChanges.Clear();
        HasPreviousViewport = false; HasNextViewport = false; OnChanged("DEACTIVATED");
    }

    public void UpdateAuthorization(EffectiveAuthorizationContext? authorization)
    {
        this.authorization = authorization;
        ResolvedDefinition = GridMetadataResolver.Resolve(Definition, authorization);
        if (EditBuffer is not null && !CanEdit(EditBuffer.RowKey, EditBuffer.VariableCode)) EditBuffer = null;
        OnChanged("AUTHORIZATION");
    }

    public void Select(IEnumerable<RowKey> rowKeys)
    {
        var available = Rows.Select(x => x.RowKey).ToHashSet();
        var requested = rowKeys.Where(available.Contains).Distinct().ToArray();
        SelectedRowKeys = Definition.SelectionMode switch
        {
            GridSelectionMode.None => [],
            GridSelectionMode.Single => requested.Take(1).ToImmutableHashSet(),
            GridSelectionMode.Multiple => requested.ToImmutableHashSet(),
            _ => [],
        };
        CellSelection = CellSelection with { SelectedRowKeys = SelectedRowKeys, SelectionMode = GridCellSelectionMode.Row };
        OnChanged("SELECTION");
    }

    public void ToggleSelection(RowKey rowKey)
    {
        if (Definition.SelectionMode == GridSelectionMode.None || !Rows.Any(x => x.RowKey == rowKey)) return;
        if (Definition.SelectionMode == GridSelectionMode.Single) SelectedRowKeys = [rowKey];
        else SelectedRowKeys = SelectedRowKeys.Contains(rowKey) ? SelectedRowKeys.Remove(rowKey) : SelectedRowKeys.Add(rowKey);
        CellSelection = CellSelection with { SelectedRowKeys = SelectedRowKeys, SelectionMode = GridCellSelectionMode.Row };
        OnChanged("SELECTION");
    }

    public bool BeginEdit(RowKey rowKey, VariableCode variableCode)
    {
        if (!CanEdit(rowKey, variableCode)) return false;
        var source = GetValue(rowKey, variableCode, out _);
        EditBuffer = new(rowKey, variableCode, source, source);
        OnChanged("EDIT_BEGIN"); return true;
    }

    public GridValidationDiagnostic? SetCandidate(object? candidate)
    {
        if (EditBuffer is null) return new("GRID_EDIT_NOT_ACTIVE", "Grid.Validation.NotEditing");
        var column = ResolvedDefinition.Columns.Single(x => x.Definition.VariableCode == EditBuffer.VariableCode);
        var diagnostic = GridValueValidator.Validate(column.Definition, candidate);
        EditBuffer = EditBuffer with { CandidateValue = candidate, Diagnostic = diagnostic };
        OnChanged("EDIT_CANDIDATE"); return diagnostic;
    }

    public async Task<GridCommitResult> CommitEditAsync(CancellationToken cancellationToken = default)
    {
        if (EditBuffer is null || context is null) return GridCommitResult.Rejected("GRID_EDIT_NOT_ACTIVE");
        var diagnostic = SetCandidate(EditBuffer.CandidateValue);
        if (diagnostic is not null) return GridCommitResult.Rejected(diagnostic.Code);
        var buffer = EditBuffer;
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask.ConfigureAwait(false);
        var change = new GridCellChange(buffer.RowKey, buffer.VariableCode, buffer.SourceValue, buffer.CandidateValue);
        StageChanges([change]);
        editHistory.Record(GridEditTransaction.Create([change], GridEditSourceAction.SingleCell));
        EditBuffer = null;
        OnChanged("EDIT_COMMIT", new(buffer.RowKey, buffer.VariableCode));
        return GridCommitResult.Success(buffer.CandidateValue);
    }

    public async Task<GridCommitResult> SavePendingChangesAsync(CancellationToken cancellationToken = default)
    {
        if (context is null) return GridCommitResult.Rejected("GRID_CONTEXT_UNAVAILABLE");
        var changes = pendingChanges.Values.ToImmutableArray();
        if (changes.IsEmpty) return GridCommitResult.Success(null);
        var persisted = changes;
        if (provider is IGridBatchEditProvider batch)
        {
            var result = await batch.CommitBatchAsync(context, GridEditTransaction.Create(changes,
                GridEditSourceAction.SingleCell), cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess) return GridCommitResult.Rejected(result.DiagnosticCode ?? "GRID_PROVIDER_BATCH_COMMIT_FAILED");
        }
        else
        {
            var committed = ImmutableArray.CreateBuilder<GridCellChange>(changes.Length);
            foreach (var change in changes)
            {
                var result = await provider.CommitAsync(context, new(change.RowKey, change.VariableCode,
                    change.CandidateValue), cancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess) return result;
                committed.Add(change with { CandidateValue = result.CommittedValue });
            }
            persisted = committed.ToImmutable();
        }
        ApplyValues(persisted);
        pendingChanges.Clear(); OnChanged("GRID_SAVE"); return GridCommitResult.Success(null);
    }

    private void StageChanges(IEnumerable<GridCellChange> changes)
    {
        foreach (var change in changes)
        {
            var key = (change.RowKey, change.VariableCode);
            var staged = pendingChanges.TryGetValue(key, out var existing)
                ? change with { OriginalValue = existing.OriginalValue } : change;
            if (Equals(staged.OriginalValue, staged.CandidateValue)) pendingChanges.Remove(key);
            else pendingChanges[key] = staged;
            Rows = Rows.Select(x => x.RowKey == change.RowKey ? x.WithValue(change.VariableCode,
                change.CandidateValue) : x).ToImmutableArray();
            windowCache.UpdateCell(change.RowKey, change.VariableCode, change.CandidateValue);
        }
    }

    public void CancelEdit() { if (EditBuffer is null) return; EditBuffer = null; OnChanged("EDIT_CANCEL"); }

    public async Task SetSortAsync(IEnumerable<GridSortDefinition> sorts, EffectiveAuthorizationContext? authorization,
        CancellationToken cancellationToken = default)
    {
        Sorts = sorts.OrderBy(x => x.Priority).ToImmutableArray();
        windowCache.Clear(); RequestedViewportStartIndex = 0;
        if (context is not null) await LoadAsync(context, authorization, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetFiltersAsync(IEnumerable<GridFilterDefinition> filters, EffectiveAuthorizationContext? authorization,
        CancellationToken cancellationToken = default)
    {
        Filters = filters.ToImmutableArray();
        windowCache.Clear(); RequestedViewportStartIndex = 0;
        if (context is not null) await LoadAsync(context, authorization, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Applies semantic query defaults without acquiring data; the owning coordinator controls the next load.</summary>
    internal void PrepareQueryState(IEnumerable<GridSortDefinition> sorts, IEnumerable<GridFilterDefinition> filters)
    {
        Sorts = sorts.OrderBy(x => x.Priority).ToImmutableArray();
        Filters = filters.ToImmutableArray();
        windowCache.Clear(); RequestedViewportStartIndex = 0;
        SelectedRowKeys = []; CellSelection = GridSelectionState.Empty; EditBuffer = null;
    }

    public bool CanEdit(RowKey rowKey, VariableCode variableCode) => ResolvedDefinition.CanEdit &&
        Rows.Any(x => x.RowKey == rowKey) && ResolvedDefinition.Columns.Any(x => x.Definition.VariableCode == variableCode && x.CanEdit);

    public object? GetValue(RowKey rowKey, VariableCode variableCode, out string? diagnosticCode)
    {
        diagnosticCode = null;
        if (pendingChanges.TryGetValue((rowKey, variableCode), out var pending)) return pending.CandidateValue;
        var row = Rows.FirstOrDefault(x => x.RowKey == rowKey);
        if (row is null || !row.TryGetValue(variableCode, out var value)) { diagnosticCode = "GRID_VARIABLE_UNAVAILABLE"; return null; }
        return value;
    }

    private void ApplyFailure(GridProviderState state, string code)
    {
        State = state; Rows = []; TotalRows = 0; VisibleRows = 0; SelectedRowKeys = []; CellSelection = GridSelectionState.Empty; EditBuffer = null;
        DiagnosticCode = code; OnChanged("FAILURE");
    }

    private (long Generation, CancellationToken Token) BeginRequest(CancellationToken cancellationToken)
    {
        var requestGeneration = Interlocked.Increment(ref generation);
        activeRequest?.Cancel(); activeRequest?.Dispose();
        activeRequest = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        return (requestGeneration, activeRequest.Token);
    }

    private static bool IsValid(GridViewportResult result, GridViewportRequest request) =>
        !result.Rows.IsDefault && result.State is GridProviderState.Ready or GridProviderState.Empty &&
        (result.State == GridProviderState.Empty) == (result.Rows.Length == 0) &&
        result.StartIndex == request.MaterializedStartIndex && result.TotalRowCount >= 0 &&
        result.StartIndex <= result.TotalRowCount && result.Rows.Length <= request.MaterializedRowCount &&
        (long)result.StartIndex + result.Rows.Length <= result.TotalRowCount &&
        result.HasPrevious == (result.StartIndex > 0) &&
        result.HasNext == (result.StartIndex + result.Rows.Length < result.TotalRowCount) &&
        !result.Rows.GroupBy(x => x.RowKey).Any(x => x.Count() > 1);

    private void ApplyViewport(GridViewportResult result, GridWindowKey key, long requestGeneration)
    {
        if (requestGeneration != Volatile.Read(ref generation)) return;
        Rows = result.Rows; ViewportStartIndex = result.StartIndex;
        TotalRows = result.TotalRowCount; VisibleRows = result.TotalRowCount;
        HasPreviousViewport = result.HasPrevious; HasNextViewport = result.HasNext;
        State = result.Rows.Length == 0 ? GridProviderState.Empty : GridProviderState.Ready;
        DiagnosticCode = result.DiagnosticCode;
        RequestedViewportStartIndex = key.StartIndex; RequestedViewportRowCount = key.RequestedRowCount;
        ReconcileCellSelection();
        OnChanged("VIEWPORT_LOADED");
    }

    private void ApplyViewportFailure(string code)
    {
        State = GridProviderState.Error; DiagnosticCode = code; OnChanged("VIEWPORT_FAILURE");
    }
    private void OnChanged(string reason, GridCellAddress? cell = null) => Changed?.Invoke(this, new(reason, cell));
}

public static class GridValueValidator
{
    public static GridValidationDiagnostic? Validate(ColumnDefinition column, object? candidate)
    {
        if (column.Mode is not ColumnMode.Input || column.EditorKind is ColumnEditorKind.ReadOnly or ColumnEditorKind.Formula)
            return new("GRID_CELL_READ_ONLY", "Grid.Validation.ReadOnly");
        if (column.IsRequired && (candidate is null || candidate is string text && string.IsNullOrWhiteSpace(text)))
            return new("GRID_VALUE_REQUIRED", "Grid.Validation.Required");
        if (candidate is null or string { Length: 0 }) return null;
        var value = candidate.ToString()!;
        var valid = column.DataType switch
        {
            ColumnDataType.Integer => candidate is int or long or short || long.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out _),
            ColumnDataType.Decimal => candidate is decimal or double or float || decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out _),
            ColumnDataType.Boolean => candidate is bool || bool.TryParse(value, out _),
            ColumnDataType.Date => candidate is DateOnly or DateTime || DateOnly.TryParse(value, CultureInfo.CurrentCulture, out _),
            ColumnDataType.DateTime => candidate is DateTime || DateTime.TryParse(value, CultureInfo.CurrentCulture, out _),
            _ => true,
        };
        return valid ? null : new("GRID_VALUE_TYPE_INVALID", "Grid.Validation.Type");
    }
}
