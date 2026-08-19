using System.Collections.Immutable;
using System.Diagnostics;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.ModernWorkspace;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.Reports;

public sealed record ReportParameterDiagnostic(ReportParameterCode ParameterCode, string Code);
public sealed record ReportExecutionTrace(TimeSpan ParameterValidation, TimeSpan FirstWindowAcquisition,
    TimeSpan ProviderAcquisition, TimeSpan RowMapping, int MaterializedRows, int AggregateValues);

/// <summary>Generation-safe coordinator. Grid mechanics are delegated to the single DataEntry grid runtime.</summary>
public sealed class ReportRuntime
{
    private readonly ReportGridProviderAdapter adapter;
    private readonly IReportOutputProvider? outputProvider;
    private readonly IReportDrillDownProvider? drillDownProvider;
    private readonly OperationCoordinator? operations;
    private readonly IDocumentViewLauncher? documentViewer;
    private CancellationTokenSource? activeRun;
    private ReportExecutionContext? context;
    private long generation;

    public ReportRuntime(ReportDefinition definition, IReportProvider provider, IReportOutputProvider? outputProvider = null,
        IReportDrillDownProvider? drillDownProvider = null, GridViewportOptions? viewportOptions = null,
        OperationCoordinator? operations = null, IDocumentViewLauncher? documentViewer = null)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        adapter = new(definition, provider ?? throw new ArgumentNullException(nameof(provider)));
        this.outputProvider = outputProvider; this.drillDownProvider = drillDownProvider; this.operations = operations;
        this.documentViewer = documentViewer;
        Parameters = definition.Parameters.ToImmutableDictionary(x => x.ParameterCode, x => x.DefaultValue);
        Sorts = definition.DefaultSort; Filters = definition.DefaultFilter; Groups = definition.DefaultGroups;
        // Reports start dormant and use a smaller first-paint window than an editable data-entry surface.
        // Resize/scroll requests can grow or move the bounded window after first useful paint.
        Grid = new(ToGridDefinition(definition), adapter,
            viewportOptions ?? new GridViewportOptions(30, 8, 8, 3, 120));
        Grid.Deactivate();
        State = ContentPresentationState.Initial;
    }

    public ReportDefinition Definition { get; }
    public DataEntryGridRuntime Grid { get; }
    public ContentPresentationState State { get; private set; }
    public ImmutableDictionary<ReportParameterCode, object?> Parameters { get; private set; }
    public ImmutableDictionary<ReportParameterCode, object?> LastExecutedParameters { get; private set; } = ImmutableDictionary<ReportParameterCode, object?>.Empty;
    public ImmutableArray<ReportParameterDiagnostic> ParameterDiagnostics { get; private set; } = [];
    public ImmutableArray<ReportSortDescriptor> Sorts { get; private set; }
    public ImmutableArray<ReportFilterDescriptor> Filters { get; private set; }
    public ImmutableArray<ReportGroupDescriptor> Groups { get; private set; }
    public ImmutableArray<ReportAggregateValue> Aggregates { get; private set; } = [];
    public DateTimeOffset? GeneratedAt { get; private set; }
    public ReportExecutionTrace? LastExecutionTrace { get; private set; }
    public long Generation => Volatile.Read(ref generation);
    public int ResultProviderRequestCount => adapter.RequestCount;
    public ReportOutputArtifact? LastOutputArtifact { get; private set; }
    public bool IsDocumentViewingAvailable => documentViewer is not null && LastOutputArtifact is not null &&
        Definition.Exports.Any(x => x.Format == LastOutputArtifact.Format && x.Capabilities.HasFlag(ReportOutputCapability.View));
    public event EventHandler? Changed;

    public void SetParameter(ReportParameterCode code, object? value)
    {
        if (!Definition.Parameters.Any(x => x.ParameterCode == code)) throw new ArgumentException("REPORT_PARAMETER_UNKNOWN", nameof(code));
        Parameters = Parameters.SetItem(code, value); ValidateParameters(); Changed?.Invoke(this, EventArgs.Empty);
    }
    public void ResetParameters()
    {
        Parameters = Definition.Parameters.ToImmutableDictionary(x => x.ParameterCode, x => x.DefaultValue);
        ValidateParameters(); Invalidate(ContentPresentationState.Initial);
    }

    /// <summary>Restores definition-owned query/presentation state without acquiring data.</summary>
    public void ResetQueryState()
    {
        Parameters = Definition.Parameters.ToImmutableDictionary(x => x.ParameterCode, x => x.DefaultValue);
        Sorts = Definition.DefaultSort;
        Filters = Definition.DefaultFilter;
        Groups = Definition.DefaultGroups;
        ValidateParameters();
        Grid.PrepareQueryState(ToGridSorts(Sorts), ToGridFilters(Filters));
        Grid.Deactivate();
        Invalidate(ContentPresentationState.Initial);
    }

    public ImmutableArray<ReportParameterDiagnostic> ValidateParameters()
    {
        var result = ImmutableArray.CreateBuilder<ReportParameterDiagnostic>();
        foreach (var definition in Definition.Parameters)
        {
            Parameters.TryGetValue(definition.ParameterCode, out var value);
            if (definition.Editor.Validation.IsRequired && (value is null || value is string text && string.IsNullOrWhiteSpace(text)))
                result.Add(new(definition.ParameterCode, "REPORT_PARAMETER_REQUIRED"));
            else if (value is not null && !IsCompatible(definition.Editor.ValueType, value)) result.Add(new(definition.ParameterCode, "REPORT_PARAMETER_TYPE_INVALID"));
        }
        ParameterDiagnostics = result.ToImmutable();
        return ParameterDiagnostics;
    }

    public async Task RunAsync(ReportExecutionContext newContext, EffectiveAuthorizationContext? authorization = null,
        CancellationToken cancellationToken = default, ReportAuthorizationSnapshot? reportAuthorization = null)
    {
        ArgumentNullException.ThrowIfNull(newContext);
        if (reportAuthorization is not null && !reportAuthorization.CanRun)
        {
            State = ContentPresentationState.Unauthorized;
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }
        var validationClock = Stopwatch.StartNew();
        var diagnostics = ValidateParameters();
        validationClock.Stop();
        if (!diagnostics.IsEmpty) { State = ContentPresentationState.Initial; Changed?.Invoke(this, EventArgs.Empty); return; }
        // A report generation is also a cache identity. Never reuse a previous generation's viewport window.
        Grid.Deactivate();
        context = newContext; var current = BeginGeneration(cancellationToken); State = ContentPresentationState.Loading;
        LastExecutedParameters = Parameters; adapter.Configure(this, newContext, current.Generation); Changed?.Invoke(this, EventArgs.Empty);
        await PublishOperationAsync(OperationState.Running, current.Generation, current.Token).ConfigureAwait(false);
        // Preserve an observable Loading boundary even when a synthetic/provider result completes synchronously.
        await Task.Yield();
        try
        {
            var firstWindowClock = Stopwatch.StartNew();
            await Grid.LoadAsync(new(newContext.Company, $"REPORT:{Definition.ReportCode.Value}"), authorization, current.Token).ConfigureAwait(false);
            firstWindowClock.Stop();
            if (current.Generation != Generation) return;
            Aggregates = adapter.LatestAggregates; GeneratedAt = DateTimeOffset.UtcNow;
            LastExecutionTrace = new(validationClock.Elapsed, firstWindowClock.Elapsed, adapter.LastProviderAcquisition,
                adapter.LastRowMapping, Grid.Rows.Length, Aggregates.Length);
            State = Grid.State switch
            {
                GridProviderState.Ready => ContentPresentationState.Ready,
                GridProviderState.Empty when Filters.Length > 0 => ContentPresentationState.FilteredEmpty,
                GridProviderState.Empty => ContentPresentationState.Empty,
                GridProviderState.Unavailable => ContentPresentationState.Unavailable,
                _ => ContentPresentationState.Error,
            };
            await PublishOperationAsync(State == ContentPresentationState.Ready ? OperationState.Succeeded : OperationState.Failed,
                current.Generation, current.Token).ConfigureAwait(false);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (current.Generation != Generation) { }
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default) => context is null
        ? Task.CompletedTask : RunAsync(context, cancellationToken: cancellationToken);
    public async Task SetSortAsync(IEnumerable<ReportSortDescriptor> sorts, CancellationToken cancellationToken = default)
    { Sorts = ValidateSorts(sorts); Grid.PrepareQueryState(ToGridSorts(Sorts), ToGridFilters(Filters)); if (context is not null) await RunAsync(context, cancellationToken: cancellationToken); }
    public async Task SetFiltersAsync(IEnumerable<ReportFilterDescriptor> filters, CancellationToken cancellationToken = default)
    { Filters = ValidateFilters(filters); Grid.PrepareQueryState(ToGridSorts(Sorts), ToGridFilters(Filters)); if (context is not null) await RunAsync(context, cancellationToken: cancellationToken); }
    public async Task SetGroupsAsync(IEnumerable<ReportGroupDescriptor> groups, CancellationToken cancellationToken = default)
    { Groups = ValidateGroups(groups); if (context is not null) await RunAsync(context, cancellationToken: cancellationToken); }

    public ImmutableArray<ReportExportCapability> OutputCapabilities => Definition.Exports;
    public async Task<ReportOutputResult> ExportAsync(ReportOutputFormat format, ReportExportScope scope,
        IEnumerable<ReportColumnCode> columns, IEnumerable<RowKey>? selectedRows = null, CancellationToken cancellationToken = default)
    {
        if (outputProvider is null || context is null || !Definition.Exports.Any(x => x.Format == format && x.Scopes.Contains(scope) && x.Capabilities.HasFlag(ReportOutputCapability.Export)))
            return new ReportOutputResult(false, DiagnosticCode: "REPORT_EXPORT_UNAVAILABLE");
        var request = new ReportExportRequest(Definition.ReportCode, format, scope, LastExecutedParameters, Sorts, Filters, Groups,
            columns.ToImmutableArray(), (selectedRows ?? []).ToImmutableArray(), Generation, context);
        await PublishOutputOperationAsync(OperationState.Running, cancellationToken).ConfigureAwait(false);
        var result = await outputProvider.ExportAsync(request, cancellationToken).ConfigureAwait(false);
        LastOutputArtifact = result.IsSuccess && result.Artifact?.ReportCode == Definition.ReportCode ? result.Artifact : null;
        await PublishOutputOperationAsync(result.IsSuccess ? OperationState.Succeeded : OperationState.Failed, cancellationToken).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
        return result;
    }

    public async Task<DocumentViewResult> ViewOutputAsync(CancellationToken cancellationToken = default)
    {
        if (!IsDocumentViewingAvailable || LastOutputArtifact is null || documentViewer is null)
            return new(false, "REPORT_DOCUMENT_VIEW_UNAVAILABLE");
        return await documentViewer.LaunchAsync(new(LastOutputArtifact, ReportOutputCapability.View), cancellationToken).ConfigureAwait(false);
    }
    public async Task<ReportOutputResult> PrintAsync(ReportOutputFormat format, ReportExportScope scope,
        IEnumerable<ReportColumnCode> columns, CancellationToken cancellationToken = default)
    {
        if (outputProvider is null || context is null || !Definition.Exports.Any(x => x.Format == format &&
            x.Scopes.Contains(scope) && x.Capabilities.HasFlag(ReportOutputCapability.Print)))
            return new(false, DiagnosticCode: "REPORT_PRINT_UNAVAILABLE");
        var request = new ReportExportRequest(Definition.ReportCode, format, scope, LastExecutedParameters, Sorts, Filters,
            Groups, columns.ToImmutableArray(), Grid.SelectedRowKeys.ToImmutableArray(), Generation, context);
        await PublishOutputOperationAsync(OperationState.Running, cancellationToken).ConfigureAwait(false);
        var result = await outputProvider.PrintAsync(request, cancellationToken).ConfigureAwait(false);
        await PublishOutputOperationAsync(result.IsSuccess ? OperationState.Succeeded : OperationState.Failed, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<ReportNavigationTarget?> DrillDownAsync(string drillDownCode, RowKey rowKey,
        ReportColumnCode? columnCode = null, object? semanticReference = null, IReportNavigationDispatcher? dispatcher = null,
        CancellationToken cancellationToken = default)
    {
        if (drillDownProvider is null || context is null || !Grid.Rows.Any(x => x.RowKey == rowKey) ||
            !Definition.DrillDowns.Any(x => x.DrillDownCode.Equals(drillDownCode, StringComparison.OrdinalIgnoreCase))) return null;
        var captured = Generation;
        var target = await drillDownProvider.ResolveAsync(new(Definition.ReportCode, drillDownCode, rowKey, columnCode,
            semanticReference, captured, context), cancellationToken).ConfigureAwait(false);
        if (captured != Generation || target is null) return null;
        if (dispatcher is not null) await dispatcher.DispatchAsync(target, cancellationToken).ConfigureAwait(false);
        return target;
    }

    public void Deactivate() { context = null; Invalidate(ContentPresentationState.Unavailable); Grid.Deactivate(); }
    private (long Generation, CancellationToken Token) BeginGeneration(CancellationToken external)
    {
        var next = Interlocked.Increment(ref generation); activeRun?.Cancel(); activeRun?.Dispose();
        activeRun = CancellationTokenSource.CreateLinkedTokenSource(external); return (next, activeRun.Token);
    }
    private void Invalidate(ContentPresentationState state) { Interlocked.Increment(ref generation); activeRun?.Cancel(); State = state; Aggregates = []; GeneratedAt = null; Changed?.Invoke(this, EventArgs.Empty); }
    private ImmutableArray<ReportSortDescriptor> ValidateSorts(IEnumerable<ReportSortDescriptor> values) => values.Where(x => Definition.Columns.Any(c => c.ColumnCode == x.ColumnCode && c.IsSortable)).OrderBy(x => x.Priority).ToImmutableArray();
    private ImmutableArray<ReportFilterDescriptor> ValidateFilters(IEnumerable<ReportFilterDescriptor> values) => values.Where(x => Definition.Columns.Any(c => c.ColumnCode == x.ColumnCode && c.IsFilterable) && new GridFilterDescriptor(ToVariable(x.ColumnCode), x.Operator, x.DataType, x.Value, x.Value2).IsValid).ToImmutableArray();
    private ImmutableArray<ReportGroupDescriptor> ValidateGroups(IEnumerable<ReportGroupDescriptor> values) => values.Where(x => Definition.Columns.Any(c => c.ColumnCode == x.ColumnCode && c.IsGroupable && c.SensitiveContent is null)).OrderBy(x => x.Order).ToImmutableArray();
    internal void AdoptGridQuery(IEnumerable<GridSortDefinition> sorts, IEnumerable<GridFilterDefinition> filters)
    {
        Sorts = ValidateSorts(sorts.Select(x => new ReportSortDescriptor(ToColumn(x.VariableCode), x.Direction, x.Priority)));
        Filters = ValidateFilters(filters.Select(ToReportFilter).Where(x => x is not null).Select(x => x!));
    }
    private ReportFilterDescriptor? ToReportFilter(GridFilterDefinition value)
    {
        if (!TryMapOperator(value.Operator, out var operation)) return null;
        var column = ToColumn(value.VariableCode);
        var metadata = Definition.Columns.Single(x => x.ColumnCode == column);
        var dataType = value.DataType ?? metadata.DataType switch
        { ReportDataType.Integer or ReportDataType.Decimal => GridFilterDataType.Number,
          ReportDataType.Date or ReportDataType.DateTime => GridFilterDataType.Date,
          ReportDataType.Boolean => GridFilterDataType.Boolean, _ => GridFilterDataType.Text };
        return new(column, operation, dataType, value.Value, value.Value2);
    }
    private IEnumerable<GridSortDefinition> ToGridSorts(IEnumerable<ReportSortDescriptor> values) =>
        values.Select(x => new GridSortDefinition(ToVariable(x.ColumnCode), x.Direction, x.Priority));
    private IEnumerable<GridFilterDefinition> ToGridFilters(IEnumerable<ReportFilterDescriptor> values) =>
        values.Select(x => new GridFilterDefinition(ToVariable(x.ColumnCode), MapOperator(x.Operator), x.Value, x.Value2, x.DataType));
    private static bool TryMapOperator(GridFilterOperator value, out GridFilterOperatorKind result)
    {
        result = value switch
        { GridFilterOperator.Contains => GridFilterOperatorKind.Contains, GridFilterOperator.Equals => GridFilterOperatorKind.Equals,
          GridFilterOperator.StartsWith => GridFilterOperatorKind.StartsWith, GridFilterOperator.GreaterThan => GridFilterOperatorKind.GreaterThan,
          GridFilterOperator.LessThan => GridFilterOperatorKind.LessThan, GridFilterOperator.Between => GridFilterOperatorKind.Between,
          GridFilterOperator.Before => GridFilterOperatorKind.Before, GridFilterOperator.After => GridFilterOperatorKind.After,
          GridFilterOperator.IsEmpty => GridFilterOperatorKind.IsEmpty, GridFilterOperator.IsNotEmpty => GridFilterOperatorKind.IsNotEmpty,
          GridFilterOperator.True => GridFilterOperatorKind.True, GridFilterOperator.False => GridFilterOperatorKind.False,
          GridFilterOperator.Any => GridFilterOperatorKind.Any, _ => default };
        return value != GridFilterOperator.NotEquals;
    }
    private static GridFilterOperator MapOperator(GridFilterOperatorKind value) => value switch
    { GridFilterOperatorKind.Contains => GridFilterOperator.Contains, GridFilterOperatorKind.Equals => GridFilterOperator.Equals,
      GridFilterOperatorKind.StartsWith => GridFilterOperator.StartsWith, GridFilterOperatorKind.GreaterThan => GridFilterOperator.GreaterThan,
      GridFilterOperatorKind.LessThan => GridFilterOperator.LessThan, GridFilterOperatorKind.Between => GridFilterOperator.Between,
      GridFilterOperatorKind.Before => GridFilterOperator.Before, GridFilterOperatorKind.After => GridFilterOperator.After,
      GridFilterOperatorKind.IsEmpty => GridFilterOperator.IsEmpty, GridFilterOperatorKind.IsNotEmpty => GridFilterOperator.IsNotEmpty,
      GridFilterOperatorKind.True => GridFilterOperator.True, GridFilterOperatorKind.False => GridFilterOperator.False,
      _ => GridFilterOperator.Any };
    private static bool IsCompatible(EditorValueType type, object value) => type switch
    { EditorValueType.String or EditorValueType.LongString or EditorValueType.Secret or EditorValueType.Hyperlink => value is string,
      EditorValueType.Integer => value is sbyte or byte or short or ushort or int or uint or long or ulong,
      EditorValueType.Decimal or EditorValueType.Currency or EditorValueType.Percentage => value is decimal or double or float or int or long,
      EditorValueType.Boolean => value is bool, EditorValueType.Date => value is DateOnly or DateTime,
      EditorValueType.DateRange => value is DateRangeValue, EditorValueType.Choice or EditorValueType.LookupKey => value is string,
      EditorValueType.MultiChoice => value is IEnumerable<string>, _ => true };
    private ValueTask<bool> PublishOperationAsync(OperationState state, long currentGeneration, CancellationToken token) =>
        operations is null ? ValueTask.FromResult(false) : operations.PublishAsync(new(
            $"REPORT:{Definition.ReportCode.Value}", "REPORT_EXECUTION", "REPORT",
            state, Definition.ReportCode.Value, WorkspaceCode: new WorkspaceCode($"REPORT:{Definition.ReportCode.Value}"),
            TargetSemanticId: Definition.ReportCode.Value, Generation: currentGeneration), token);
    private ValueTask<bool> PublishOutputOperationAsync(OperationState state, CancellationToken token) =>
        operations is null ? ValueTask.FromResult(false) : operations.PublishAsync(new(
            $"REPORT:{Definition.ReportCode.Value}:OUTPUT", "REPORT_OUTPUT", "REPORT", state,
            Definition.ReportCode.Value, WorkspaceCode: new WorkspaceCode($"REPORT:{Definition.ReportCode.Value}"),
            TargetSemanticId: Definition.ReportCode.Value, Capabilities: new(CanOpenResult: LastOutputArtifact is not null),
            Generation: Generation), token);
    internal VariableCode ToVariable(ReportColumnCode code) => Definition.Columns.Single(x => x.ColumnCode == code).VariableCode ?? new VariableCode($"REPORT_{code.Value}");
    internal ReportColumnCode ToColumn(VariableCode code) => Definition.Columns.Single(x => (x.VariableCode ?? new VariableCode($"REPORT_{x.ColumnCode.Value}")) == code).ColumnCode;
    private static GridDefinition ToGridDefinition(ReportDefinition definition) => new($"report:{definition.ReportCode.Value}", definition.ReportCode.Value,
        definition.Columns.Select((x, i) => new ColumnDefinition($"report:{definition.ReportCode.Value}:{x.ColumnCode.Value}", x.ColumnCode.Value,
            x.VariableCode ?? new VariableCode($"REPORT_{x.ColumnCode.Value}"), x.DisplayNameKey.Value, null, ToColumnType(x.DataType), ColumnEditorKind.ReadOnly,
            ColumnMode.System, i, ResolveBaseWidth(x), 64m, 420m, x.IsVisible, false, null, x.Format, null, null, null, 1,
            SetupDefinitionStatus.Published, x.SensitiveContent)), selectionMode: GridSelectionMode.Single, allowEdit: false, allowAdd: false, allowDelete: false);
    internal static decimal ResolveBaseWidth(ReportColumnDefinition column)
    {
        if (column.DefaultWidth is > 0m and <= 420m) return Math.Max(64m, column.DefaultWidth.Value);
        return column.DataType switch
        {
            ReportDataType.Boolean => 96m,
            ReportDataType.Status => 120m,
            ReportDataType.Integer => 112m,
            ReportDataType.Decimal => 132m,
            ReportDataType.Date => 128m,
            ReportDataType.DateTime => 160m,
            ReportDataType.Reference => 150m,
            _ => 180m,
        };
    }
    private static ColumnDataType ToColumnType(ReportDataType value) => value switch
    { ReportDataType.Integer => ColumnDataType.Integer, ReportDataType.Decimal => ColumnDataType.Decimal, ReportDataType.Boolean => ColumnDataType.Boolean,
      ReportDataType.Date => ColumnDataType.Date, ReportDataType.DateTime => ColumnDataType.DateTime, _ => ColumnDataType.Text };
}

internal sealed class ReportGridProviderAdapter(ReportDefinition definition, IReportProvider provider) : IVirtualizedGridDataProvider, IGridFindProvider
{
    private ReportRuntime? runtime; private ReportExecutionContext? reportContext; private long reportGeneration;
    public ImmutableArray<ReportAggregateValue> LatestAggregates { get; private set; } = [];
    public TimeSpan LastProviderAcquisition { get; private set; }
    public TimeSpan LastRowMapping { get; private set; }
    public int RequestCount { get; private set; }
    public void Configure(ReportRuntime owner, ReportExecutionContext context, long generation) { runtime = owner; reportContext = context; reportGeneration = generation; }
    public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit, CancellationToken cancellationToken = default) => Task.FromResult(GridCommitResult.Rejected("REPORT_READ_ONLY"));
    public async Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request, CancellationToken cancellationToken = default)
    { var viewport = await LoadViewportAsync(context, new(0, 100, 0, 0, request.Sorts, request.Filters, request.Generation), cancellationToken); return new(viewport.State, viewport.Rows, viewport.TotalRowCount, viewport.TotalRowCount, viewport.DiagnosticCode); }
    public async Task<GridViewportResult> LoadViewportAsync(GridProviderContext context, GridViewportRequest request, CancellationToken cancellationToken = default)
    {
        if (runtime is null || reportContext is null) return GridViewportResult.Failure(request, GridProviderState.Unavailable, "REPORT_NOT_CONFIGURED");
        runtime.AdoptGridQuery(request.SortDefinitions, request.FilterDefinitions);
        var window = new ReportResultWindow(request.MaterializedStartIndex, request.MaterializedRowCount);
        RequestCount++;
        var providerClock = Stopwatch.StartNew();
        var result = await provider.ExecuteAsync(new(definition.ReportCode, runtime.LastExecutedParameters, runtime.Sorts, runtime.Filters,
            runtime.Groups, definition.Columns.Select(x => x.ColumnCode).ToImmutableArray(), window, reportGeneration, reportContext), cancellationToken);
        providerClock.Stop(); LastProviderAcquisition = providerClock.Elapsed;
        if (result.Generation != reportGeneration) return GridViewportResult.Failure(request, GridProviderState.Error, "REPORT_STALE_PROVIDER_RESULT");
        LatestAggregates = result.Aggregates;
        var mappingClock = Stopwatch.StartNew();
        var rows = result.Rows.Select(row => new GridRow(row.RowKey, row.Values.ToImmutableDictionary(x => runtime.ToVariable(x.Key), x => x.Value)));
        var gridRows = rows.ToImmutableArray();
        mappingClock.Stop(); LastRowMapping = mappingClock.Elapsed;
        var total = Math.Max(0, result.FilteredRowCount ?? result.TotalRowCount ?? 0);
        return new(gridRows.IsEmpty ? GridProviderState.Empty : GridProviderState.Ready, request.MaterializedStartIndex,
            gridRows, total, request.RequestGeneration, request.MaterializedStartIndex > 0,
            request.MaterializedStartIndex + gridRows.Length < total, result.ProviderState);
    }
    public Task<GridFindResult> FindAsync(GridProviderContext context, GridFindRequest request, CancellationToken cancellationToken = default)
    {
        if (provider is not IReportFindProvider finder || runtime is null || reportContext is null) return Task.FromResult(GridFindResult.Rejected("GRID_FIND_UNAVAILABLE", request.RequestGeneration));
        return FindCoreAsync(finder, request, cancellationToken);
    }
    private async Task<GridFindResult> FindCoreAsync(IReportFindProvider finder, GridFindRequest request, CancellationToken token)
    {
        var rr = await finder.FindAsync(new(definition.ReportCode, request.Query, request.Scope, request.RowKey,
            request.VariableCode is { } v ? runtime!.ToColumn(v) : null, request.EligibleVariableCodes.Select(runtime!.ToColumn).ToImmutableArray(),
            request.Direction, request.StartPosition, runtime.Sorts, runtime.Filters, reportGeneration, reportContext!), token);
        return rr.IsMatch && rr.RowKey is { } row && rr.ColumnCode is { } col && rr.LogicalPosition is { } pos
            ? GridFindResult.Match(row, runtime.ToVariable(col), pos, request.RequestGeneration)
            : GridFindResult.NoMatch(request.RequestGeneration);
    }
}

public sealed record ReportFindRequest(ReportCode ReportCode, string Query, GridFindScope Scope, RowKey? RowKey,
    ReportColumnCode? ColumnCode, ImmutableArray<ReportColumnCode> EligibleColumns, GridFindDirection Direction,
    int StartPosition, ImmutableArray<ReportSortDescriptor> Sorts, ImmutableArray<ReportFilterDescriptor> Filters,
    long Generation, ReportExecutionContext Context);
public sealed record ReportFindResult(bool IsMatch, RowKey? RowKey = null, ReportColumnCode? ColumnCode = null, int? LogicalPosition = null);
public interface IReportFindProvider { Task<ReportFindResult> FindAsync(ReportFindRequest request, CancellationToken cancellationToken = default); }
