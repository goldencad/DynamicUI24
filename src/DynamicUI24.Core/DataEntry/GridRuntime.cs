using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

public sealed record GridValidationDiagnostic(string Code, string MessageKey);
public sealed record GridEditBuffer(RowKey RowKey, VariableCode VariableCode, object? SourceValue,
    object? CandidateValue, GridValidationDiagnostic? Diagnostic = null)
{
    public bool IsDirty => !Equals(SourceValue, CandidateValue);
}

public sealed class GridRuntimeChangedEventArgs(string reason) : EventArgs
{
    public string Reason { get; } = reason;
}

/// <summary>UI-platform-free state machine for loading, selection, sorting, filtering and one-cell editing.</summary>
public sealed class DataEntryGridRuntime
{
    private readonly IDataEntryGridProvider provider;
    private long generation;
    private GridProviderContext? context;

    public DataEntryGridRuntime(GridDefinition definition, IDataEntryGridProvider provider)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Sorts = definition.DefaultSort;
        Filters = definition.DefaultFilter;
        ResolvedDefinition = GridMetadataResolver.Resolve(definition, null);
    }

    public GridDefinition Definition { get; }
    public ResolvedGridDefinition ResolvedDefinition { get; private set; }
    public GridProviderState State { get; private set; } = GridProviderState.Loading;
    public ImmutableArray<GridRow> Rows { get; private set; } = [];
    public ImmutableHashSet<RowKey> SelectedRowKeys { get; private set; } = [];
    public ImmutableArray<GridSortDefinition> Sorts { get; private set; }
    public ImmutableArray<GridFilterDefinition> Filters { get; private set; }
    public GridEditBuffer? EditBuffer { get; private set; }
    public int TotalRows { get; private set; }
    public int VisibleRows { get; private set; }
    public string? DiagnosticCode { get; private set; }
    public int SelectionCount => SelectedRowKeys.Count;
    public int ErrorCount => Rows.Sum(x => x.ErrorCount) + (EditBuffer?.Diagnostic is null ? 0 : 1);
    public int WarningCount => Rows.Sum(x => x.WarningCount);
    public int PendingChangeCount => EditBuffer?.IsDirty == true ? 1 : 0;
    public event EventHandler<GridRuntimeChangedEventArgs>? Changed;

    public ActionBarStatus Status => new(TotalRows, VisibleRows, SelectionCount, ErrorCount, WarningCount,
        PendingChangeCount, !ResolvedDefinition.CanEdit);

    public async Task LoadAsync(GridProviderContext newContext, EffectiveAuthorizationContext? authorization,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newContext);
        var requestGeneration = Interlocked.Increment(ref generation);
        var contextChanged = context is null || context.Company.CompanyId != newContext.Company.CompanyId ||
            !context.WorkspaceId.Equals(newContext.WorkspaceId, StringComparison.OrdinalIgnoreCase);
        context = newContext;
        if (contextChanged) { SelectedRowKeys = []; EditBuffer = null; }
        ResolvedDefinition = GridMetadataResolver.Resolve(Definition, authorization);
        State = GridProviderState.Loading; DiagnosticCode = null; OnChanged("LOADING");
        try
        {
            var result = await provider.LoadAsync(newContext, new(Sorts, Filters, requestGeneration), cancellationToken)
                .ConfigureAwait(false);
            if (requestGeneration != Volatile.Read(ref generation)) return;
            if (result.Rows.GroupBy(x => x.RowKey).Any(x => x.Count() > 1))
            {
                ApplyFailure(GridProviderState.Error, "GRID_DUPLICATE_ROW_KEY"); return;
            }
            Rows = result.Rows; TotalRows = Math.Max(0, result.TotalRows); VisibleRows = Math.Max(0, result.VisibleRows);
            State = result.State; DiagnosticCode = result.DiagnosticCode;
            SelectedRowKeys = SelectedRowKeys.Intersect(Rows.Select(x => x.RowKey)).ToImmutableHashSet();
            OnChanged("LOADED");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { if (requestGeneration == Volatile.Read(ref generation)) ApplyFailure(GridProviderState.Error, "GRID_PROVIDER_FAILED"); }
    }

    public void UpdateAuthorization(EffectiveAuthorizationContext? authorization)
    {
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
        OnChanged("SELECTION");
    }

    public void ToggleSelection(RowKey rowKey)
    {
        if (Definition.SelectionMode == GridSelectionMode.None || !Rows.Any(x => x.RowKey == rowKey)) return;
        if (Definition.SelectionMode == GridSelectionMode.Single) SelectedRowKeys = [rowKey];
        else SelectedRowKeys = SelectedRowKeys.Contains(rowKey) ? SelectedRowKeys.Remove(rowKey) : SelectedRowKeys.Add(rowKey);
        OnChanged("SELECTION");
    }

    public bool BeginEdit(RowKey rowKey, VariableCode variableCode)
    {
        if (!CanEdit(rowKey, variableCode)) return false;
        var row = Rows.Single(x => x.RowKey == rowKey);
        row.TryGetValue(variableCode, out var source);
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
        GridCommitResult result;
        try { result = await provider.CommitAsync(context, new(buffer.RowKey, buffer.VariableCode, buffer.CandidateValue), cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { result = GridCommitResult.Rejected("GRID_PROVIDER_COMMIT_FAILED"); }
        if (!result.IsSuccess) return result;
        Rows = Rows.Select(x => x.RowKey == buffer.RowKey ? x.WithValue(buffer.VariableCode, result.CommittedValue) : x).ToImmutableArray();
        EditBuffer = null; OnChanged("EDIT_COMMIT"); return result;
    }

    public void CancelEdit() { if (EditBuffer is null) return; EditBuffer = null; OnChanged("EDIT_CANCEL"); }

    public async Task SetSortAsync(IEnumerable<GridSortDefinition> sorts, EffectiveAuthorizationContext? authorization,
        CancellationToken cancellationToken = default)
    {
        Sorts = sorts.OrderBy(x => x.Priority).ToImmutableArray();
        if (context is not null) await LoadAsync(context, authorization, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetFiltersAsync(IEnumerable<GridFilterDefinition> filters, EffectiveAuthorizationContext? authorization,
        CancellationToken cancellationToken = default)
    {
        Filters = filters.ToImmutableArray();
        if (context is not null) await LoadAsync(context, authorization, cancellationToken).ConfigureAwait(false);
    }

    public bool CanEdit(RowKey rowKey, VariableCode variableCode) => ResolvedDefinition.CanEdit &&
        Rows.Any(x => x.RowKey == rowKey) && ResolvedDefinition.Columns.Any(x => x.Definition.VariableCode == variableCode && x.CanEdit);

    public object? GetValue(RowKey rowKey, VariableCode variableCode, out string? diagnosticCode)
    {
        diagnosticCode = null;
        var row = Rows.FirstOrDefault(x => x.RowKey == rowKey);
        if (row is null || !row.TryGetValue(variableCode, out var value)) { diagnosticCode = "GRID_VARIABLE_UNAVAILABLE"; return null; }
        return value;
    }

    private void ApplyFailure(GridProviderState state, string code)
    {
        State = state; Rows = []; TotalRows = 0; VisibleRows = 0; SelectedRowKeys = []; EditBuffer = null;
        DiagnosticCode = code; OnChanged("FAILURE");
    }
    private void OnChanged(string reason) => Changed?.Invoke(this, new(reason));
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
