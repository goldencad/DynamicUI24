using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.ImportExport;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

internal static class DemoDataEntry
{
    public static GridDefinition CreateDefinition()
    {
        var columns = new[]
        {
            Column("code", "CODE", "ITEM_CODE", "Grid.Column.Code", ColumnDataType.Text, ColumnEditorKind.TextBox, ColumnMode.Input, 0, 118, required: true),
            Column("name", "NAME", "ITEM_NAME", "Grid.Column.Name", ColumnDataType.Text, ColumnEditorKind.TextBox, ColumnMode.Input, 10, 190, required: true),
            Column("category", "CATEGORY", "CATEGORY", "Grid.Column.Category", ColumnDataType.Choice, ColumnEditorKind.ComboBox, ColumnMode.Input, 20, 130),
            Column("quantity", "QUANTITY", "QUANTITY", "Grid.Column.Quantity", ColumnDataType.Integer, ColumnEditorKind.Number, ColumnMode.Input, 30, 100, required: true),
            Column("rate", "RATE", "UNIT_RATE", "Grid.Column.Rate", ColumnDataType.Decimal, ColumnEditorKind.Number, ColumnMode.Input, 40, 118, format: "N2"),
            Column("active", "ACTIVE", "IS_ACTIVE", "Grid.Column.Active", ColumnDataType.Boolean, ColumnEditorKind.Checkbox, ColumnMode.Input, 50, 90),
            Column("date", "START_DATE", "START_DATE", "Grid.Column.StartDate", ColumnDataType.Date, ColumnEditorKind.DatePicker, ColumnMode.Input, 60, 120, format: "d"),
            Column("notes", "PUBLIC_NOTE", "PUBLIC_NOTE", "Grid.Column.Notes", ColumnDataType.MultilineText, ColumnEditorKind.TextBox, ColumnMode.Input, 70, 180),
            Column("total", "TOTAL", "TOTAL", "Grid.Column.Total", ColumnDataType.Formula, ColumnEditorKind.Formula, ColumnMode.Formula, 80, 126, format: "N2"),
            Column("updated", "UPDATED_AT", "UPDATED_AT", "Grid.Column.UpdatedAt", ColumnDataType.System, ColumnEditorKind.ReadOnly, ColumnMode.System, 90, 155, format: "g"),
            Column("reference", "CONTACT_REFERENCE", "CONTACT_REFERENCE", "Grid.Column.Reference", ColumnDataType.Reference, ColumnEditorKind.ReadOnly, ColumnMode.Input, 100, 150,
                sensitive: new(Sensitivity.Confidential, PrivacyPresentation.PartialMask, AllowTemporaryReveal: true,
                    TemporaryRevealDuration: TimeSpan.FromSeconds(8), PartialMask: new(0, 4, "•••• "))),
            Column("secret", "PRIVATE_REFERENCE", "PRIVATE_REFERENCE", "Grid.Column.Privileged", ColumnDataType.Text, ColumnEditorKind.ReadOnly, ColumnMode.System, 110, 160,
                sensitive: new(Sensitivity.Restricted, PrivacyPresentation.CaptureProtect, PrivacyPresentation.Mask)),
        };
        return new("demo-data-grid", "DEMO_DATA_GRID", columns, new("Grid.Title"),
            [new(new("ITEM_CODE"), GridSortDirection.Ascending)], selectionMode: GridSelectionMode.Multiple,
            allowEdit: true, allowAdd: true, allowDelete: true, showRowNumbers: true, showStatusBar: true);
    }

    private static ColumnDefinition Column(string id, string code, string variable, string label, ColumnDataType type,
        ColumnEditorKind editor, ColumnMode mode, int order, decimal width, bool required = false,
        bool visible = true, string? permission = null, string? format = null, SensitiveContentDefinition? sensitive = null) =>
        new(id, code, new(variable), label, null, type, editor, mode, order, width, 64, 360,
            visible, required, permission, format, null, null, null, 1, SetupDefinitionStatus.Published, sensitive);
}

internal sealed class DemoDataEntryProvider : IVirtualizedGridDataProvider, IGridLogicalRowProvider, IGridBatchEditProvider,
    IGridBatchRowImportProvider, IGridExportProvider, IGridRowLifecycleProvider, IGridRowCalculationInvalidation,
    IGridFindProvider
{
    public const int LogicalRowCount = 100_000;
    private readonly object sync = new();
    private readonly Dictionary<(CompanyId CompanyId, RowKey RowKey, VariableCode VariableCode), object?> edits = [];
    private readonly Dictionary<CompanyId, List<(RowKey Key, RowKey Anchor, GridRowInsertPlacement Placement,
        ImmutableDictionary<VariableCode, object?> Values)>> inserted = [];
    private readonly Dictionary<CompanyId, HashSet<RowKey>> deleted = [];
    private long insertedIdentity;
    private int generatedRowCount;
    private int viewportRequestCount;
    public bool SimulateFailure { get; set; }
    public int GeneratedRowCount => Volatile.Read(ref generatedRowCount);
    public int ViewportRequestCount => Volatile.Read(ref viewportRequestCount);
    public bool CanInsertRows => true;
    public bool CanDeleteRows => true;

    public async Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var viewportRequest = new GridViewportRequest(0, 60, 0, 0, request.Sorts, request.Filters, request.Generation);
        var result = await LoadViewportAsync(context, viewportRequest, cancellationToken);
        return new(result.State, result.Rows, result.TotalRowCount, result.TotalRowCount, result.DiagnosticCode);
    }

    public async Task<GridViewportResult> LoadViewportAsync(GridProviderContext context, GridViewportRequest request,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref viewportRequestCount);
        await Task.Delay(context.Company.CompanyId == DemoCompanyData.CompanyAId ? 35 : 5, cancellationToken);
        if (SimulateFailure) throw new InvalidOperationException("Demo provider failure details must remain isolated.");

        var descending = request.SortDefinitions.FirstOrDefault()?.Direction == GridSortDirection.Descending;
        var wantedStart = request.MaterializedStartIndex;
        var wantedCount = request.MaterializedRowCount;
        var rows = ImmutableArray.CreateBuilder<GridRow>(wantedCount);
        var matched = 0;
        void Consider(GridRow row)
        {
            if (!Matches(row, request.FilterDefinitions)) return;
            if (matched >= wantedStart && rows.Count < wantedCount) rows.Add(row);
            matched++;
        }
        for (var offset = 0; offset < LogicalRowCount; offset++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var logicalIndex = descending ? LogicalRowCount - offset : offset + 1;
            var baseKey = BaseRowKey(context.Company, logicalIndex);
            foreach (var item in InsertedAt(context.Company.CompanyId, baseKey, GridRowInsertPlacement.Before))
                if (!IsDeleted(context.Company.CompanyId, item.Key)) Consider(BuildInsertedRow(context.Company, item));
            if (!IsDeleted(context.Company.CompanyId, baseKey) && Matches(logicalIndex, context.Company, request.FilterDefinitions))
                Consider(BuildRow(context.Company, logicalIndex));
            foreach (var item in InsertedAt(context.Company.CompanyId, baseKey, GridRowInsertPlacement.After))
                if (!IsDeleted(context.Company.CompanyId, item.Key)) Consider(BuildInsertedRow(context.Company, item));
        }
        Interlocked.Add(ref generatedRowCount, rows.Count);
        return new(rows.Count == 0 ? GridProviderState.Empty : GridProviderState.Ready, wantedStart, rows.ToImmutable(),
            matched, request.RequestGeneration, wantedStart > 0, wantedStart + rows.Count < matched,
            $"generated={rows.Count}");
    }

    public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            var insertedRow = inserted.GetValueOrDefault(context.Company.CompanyId)?.FirstOrDefault(x => x.Key == edit.RowKey);
            if (insertedRow is not null)
            {
                edits[(context.Company.CompanyId, edit.RowKey, edit.VariableCode)] = edit.CandidateValue;
                return Task.FromResult(GridCommitResult.Success(edit.CandidateValue));
            }
            var separator = edit.RowKey.Value.LastIndexOf(':');
            if (separator < 0 || !int.TryParse(edit.RowKey.Value[(separator + 1)..], out var logicalIndex) ||
                logicalIndex is < 1 or > LogicalRowCount)
                return Task.FromResult(GridCommitResult.Rejected("GRID_ROW_UNAVAILABLE"));
            var current = BuildRow(context.Company, logicalIndex).Values.GetValueOrDefault(edit.VariableCode);
            var value = ConvertCandidate(edit.CandidateValue, current);
            edits[(context.Company.CompanyId, edit.RowKey, edit.VariableCode)] = value;
            return Task.FromResult(GridCommitResult.Success(value));
        }
    }

    public Task<GridRowInsertResult> InsertRowAsync(GridProviderContext context, GridRowInsertRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            var key = new RowKey($"{context.Company.CompanyId.Value}:INSERT:{Interlocked.Increment(ref insertedIdentity):000000}");
            if (!inserted.TryGetValue(context.Company.CompanyId, out var rows)) inserted[context.Company.CompanyId] = rows = [];
            rows.Add((key, request.AnchorRowKey, request.Placement, request.InitialValues));
            var position = request.AnchorLogicalPosition +
                (request.Placement == GridRowInsertPlacement.After ? 1 : 0);
            return Task.FromResult(GridRowInsertResult.Success(key, LogicalRowCount + rows.Count -
                (deleted.GetValueOrDefault(context.Company.CompanyId)?.Count ?? 0), position));
        }
    }

    public Task<GridFindResult> FindAsync(GridProviderContext context, GridFindRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var columns = request.Scope == GridFindScope.CurrentColumn && request.VariableCode is { } current
            ? [current] : request.EligibleVariableCodes;
        if (request.Scope == GridFindScope.CurrentRow)
            return Task.FromResult(FindInCurrentRow(context, request, columns));
        var descending = request.Sorts.FirstOrDefault()?.Direction == GridSortDirection.Descending;
        var position = 0;
        (GridRow Row, VariableCode Variable, int Position)? forward = null, wrap = null, same = null;
        for (var offset = 0; offset < LogicalRowCount; offset++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var logicalIndex = descending ? LogicalRowCount - offset : offset + 1;
            var baseKey = BaseRowKey(context.Company, logicalIndex);
            foreach (var item in InsertedAt(context.Company.CompanyId, baseKey, GridRowInsertPlacement.Before))
                Consider(BuildInsertedRow(context.Company, item));
            if (!IsDeleted(context.Company.CompanyId, baseKey)) Consider(BuildRow(context.Company, logicalIndex));
            foreach (var item in InsertedAt(context.Company.CompanyId, baseKey, GridRowInsertPlacement.After))
                Consider(BuildInsertedRow(context.Company, item));
        }
        var match = forward ?? wrap ?? same;
        return Task.FromResult(match is null ? GridFindResult.NoMatch(request.RequestGeneration) :
            GridFindResult.Match(match.Value.Row.RowKey, match.Value.Variable, match.Value.Position,
                request.RequestGeneration));

        void Consider(GridRow row)
        {
            if (!Matches(row, request.Filters)) return;
            var variable = columns.FirstOrDefault(code => Text(row.Values.GetValueOrDefault(code)).Contains(request.Query,
                StringComparison.CurrentCultureIgnoreCase));
            if (variable != default)
            {
                if (position == request.StartPosition) same = (row, variable, position);
                var after = position > request.StartPosition;
                var before = position < request.StartPosition;
                if (request.Direction == GridFindDirection.Next)
                {
                    if (after && forward is null) forward = (row, variable, position);
                    else if (before && wrap is null) wrap = (row, variable, position);
                }
                else
                {
                    if (before) forward = (row, variable, position);
                    else if (after) wrap = (row, variable, position);
                }
            }
            position++;
        }
        static string Text(object? value) => value?.ToString() ?? string.Empty;
    }

    private GridFindResult FindInCurrentRow(GridProviderContext context, GridFindRequest request,
        ImmutableArray<VariableCode> columns)
    {
        if (request.RowKey is not { } key) return GridFindResult.Rejected("GRID_FIND_ROW_UNAVAILABLE", request.RequestGeneration);
        GridRow? row = null;
        var separator = key.Value.LastIndexOf(':');
        if (separator >= 0 && int.TryParse(key.Value[(separator + 1)..], out var index) &&
            index is >= 1 and <= LogicalRowCount && !IsDeleted(context.Company.CompanyId, key))
            row = BuildRow(context.Company, index);
        else
        {
            lock (sync)
            {
                var insertedRow = inserted.GetValueOrDefault(context.Company.CompanyId)?.FirstOrDefault(x => x.Key == key);
                if (insertedRow is not null && !IsDeleted(context.Company.CompanyId, key))
                    row = BuildInsertedRow(context.Company, insertedRow.Value);
            }
        }
        if (row is null || !Matches(row, request.Filters))
            return GridFindResult.Rejected("GRID_FIND_ROW_UNAVAILABLE", request.RequestGeneration);
        var start = request.VariableCode is { } active ? columns.IndexOf(active) : -1;
        var ordered = request.Direction == GridFindDirection.Next
            ? columns.Skip(start + 1).Concat(columns.Take(start + 1))
            : columns.Take(Math.Max(0, start)).Reverse().Concat(columns.Skip(Math.Max(0, start)).Reverse());
        var match = ordered.FirstOrDefault(code => (row.Values.GetValueOrDefault(code)?.ToString() ?? string.Empty)
            .Contains(request.Query, StringComparison.CurrentCultureIgnoreCase));
        return match == default ? GridFindResult.NoMatch(request.RequestGeneration) :
            GridFindResult.Match(key, match, request.StartPosition, request.RequestGeneration);
    }

    public Task<GridRowDeleteResult> DeleteRowsAsync(GridProviderContext context, GridRowDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            if (!deleted.TryGetValue(context.Company.CompanyId, out var keys)) deleted[context.Company.CompanyId] = keys = [];
            foreach (var key in request.RowKeys) keys.Add(key);
            return Task.FromResult(GridRowDeleteResult.Success(request.RowKeys,
                LogicalRowCount + (inserted.GetValueOrDefault(context.Company.CompanyId)?.Count ?? 0) - keys.Count));
        }
    }

    public Task InvalidateRowsAsync(GridProviderContext context, IEnumerable<RowKey> changedRows,
        CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); return Task.CompletedTask; }

    public async Task<ImmutableArray<GridRow>> ResolveRowsAsync(GridProviderContext context, int startPosition, int rowCount,
        ImmutableArray<GridSortDefinition> sorts, ImmutableArray<GridFilterDefinition> filters,
        long requestGeneration, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startPosition);
        if (rowCount <= 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
        var result = await LoadViewportAsync(context, new(startPosition, rowCount, 0, 0, sorts, filters,
            requestGeneration), cancellationToken);
        return result.Rows;
    }

    public Task<GridBatchCommitResult> CommitBatchAsync(GridProviderContext context, GridEditTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            var resolved = new List<(GridCellChange Change, object? Value)>(transaction.CellChanges.Length);
            foreach (var change in transaction.CellChanges)
            {
                var insertedRow = inserted.GetValueOrDefault(context.Company.CompanyId)?.FirstOrDefault(x => x.Key == change.RowKey);
                if (insertedRow is not null)
                {
                    resolved.Add((change, change.CandidateValue));
                    continue;
                }
                var separator = change.RowKey.Value.LastIndexOf(':');
                if (separator < 0 || !int.TryParse(change.RowKey.Value[(separator + 1)..], out var logicalIndex) ||
                    logicalIndex is < 1 or > LogicalRowCount)
                    return Task.FromResult(GridBatchCommitResult.Rejected("GRID_ROW_UNAVAILABLE"));
                var current = BuildRow(context.Company, logicalIndex).Values.GetValueOrDefault(change.VariableCode);
                resolved.Add((change, ConvertCandidate(change.CandidateValue, current)));
            }
            foreach (var item in resolved)
                edits[(context.Company.CompanyId, item.Change.RowKey, item.Change.VariableCode)] = item.Value;
            return Task.FromResult(GridBatchCommitResult.Success);
        }
    }

    public Task<ImportBatchResult> ImportRowsAsync(GridProviderContext context, ImportBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = request.Rows.Select((row, index) =>
        {
            var rowKey = new RowKey($"{context.Company.CompanyId.Value}:IMPORT:{request.TransactionId:N}:{index:000000}");
            lock (sync) foreach (var value in row.Values) edits[(context.Company.CompanyId, rowKey, value.Key)] = value.Value;
            return new ImportRowResult(true, rowKey);
        }).ToImmutableArray();
        return Task.FromResult(new ImportBatchResult(results));
    }

    public async IAsyncEnumerable<GridRow> ExportRowsAsync(ExportProviderRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var keys = request.SelectedRowKeys.ToHashSet();
        for (var index = 1; index <= LogicalRowCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested(); var row = BuildRow(request.Context.Company, index);
            if (request.Scope == ExportScope.SelectedRows && !keys.Contains(row.RowKey)) continue;
            if (!Matches(index, request.Context.Company, request.Filters)) continue;
            yield return row;
            if ((index & 1023) == 0) await Task.Yield();
        }
    }

    public Task<long?> GetExportCountAsync(ExportProviderRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Scope == ExportScope.SelectedRows) return Task.FromResult<long?>(request.SelectedRowKeys.Length);
        if (request.Filters.Length == 0) return Task.FromResult<long?>(LogicalRowCount);
        return Task.FromResult<long?>(null);
    }

    private GridRow BuildRow(CompanyDescriptor company, int index)
    {
        var quantity = index * 2;
        var rate = 7.25m + index / 4m;
        var prefix = company.Code;
        var rowKey = BaseRowKey(company, index);
        var values = new Dictionary<VariableCode, object?>
        {
            [new("ITEM_CODE")] = $"{prefix}-{index:000000}", [new("ITEM_NAME")] = $"Sample item {index:000000}",
            [new("CATEGORY")] = index % 3 == 0 ? "EXTENDED" : "STANDARD", [new("QUANTITY")] = quantity,
            [new("UNIT_RATE")] = rate, [new("IS_ACTIVE")] = index % 4 != 0,
            [new("START_DATE")] = new DateOnly(2026, 1, 1).AddDays(index % 3650),
            [new("PUBLIC_NOTE")] = index % 5 == 0 ? "Review this neutral sample" : null,
            [new("TOTAL")] = quantity * rate,
            [new("UPDATED_AT")] = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Local).AddMinutes(index % 525600),
            [new("CONTACT_REFERENCE")] = $"CONTACT-{1000000 + index}", [new("PRIVATE_REFERENCE")] = $"PRIVATE-{9000000 + index}",
        };
        lock (sync)
            foreach (var edit in edits.Where(x => x.Key.CompanyId == company.CompanyId && x.Key.RowKey == rowKey))
                values[edit.Key.VariableCode] = edit.Value;
        return new GridRow(rowKey, values, warningCount: index % 997 == 0 ? 1 : 0);
    }

    private static RowKey BaseRowKey(CompanyDescriptor company, int index) =>
        new($"{company.CompanyId.Value}:ROW:{index:000000}");

    private IEnumerable<(RowKey Key, RowKey Anchor, GridRowInsertPlacement Placement,
        ImmutableDictionary<VariableCode, object?> Values)> InsertedAt(CompanyId company, RowKey anchor,
        GridRowInsertPlacement placement)
    { lock (sync) return inserted.GetValueOrDefault(company)?.Where(x => x.Anchor == anchor && x.Placement == placement).ToArray() ?? []; }

    private bool IsDeleted(CompanyId company, RowKey key) { lock (sync) return deleted.GetValueOrDefault(company)?.Contains(key) == true; }

    private GridRow BuildInsertedRow(CompanyDescriptor company, (RowKey Key, RowKey Anchor,
        GridRowInsertPlacement Placement, ImmutableDictionary<VariableCode, object?> Values) item)
    {
        var values = item.Values.ToDictionary(x => x.Key, x => x.Value);
        lock (sync) foreach (var edit in edits.Where(x => x.Key.CompanyId == company.CompanyId && x.Key.RowKey == item.Key))
            values[edit.Key.VariableCode] = edit.Value;
        return new(item.Key, values);
    }

    private static bool Matches(GridRow row, IEnumerable<GridFilterDefinition> filters) =>
        filters.All(filter => Matches(row.Values.GetValueOrDefault(filter.VariableCode), filter));

    private static bool Matches(int index, CompanyDescriptor company, IEnumerable<GridFilterDefinition> filters)
    {
        foreach (var filter in filters)
        {
            object? value = filter.VariableCode.Value switch
            {
                "ITEM_CODE" => $"{company.Code}-{index:000000}", "ITEM_NAME" => $"Sample item {index:000000}",
                "CATEGORY" => index % 3 == 0 ? "EXTENDED" : "STANDARD", "QUANTITY" => index * 2,
                "UNIT_RATE" => 7.25m + index / 4m, "IS_ACTIVE" => index % 4 != 0,
                "START_DATE" => new DateOnly(2026, 1, 1).AddDays(index % 3650), _ => null,
            };
            if (!Matches(value, filter)) return false;
        }
        return true;
    }

    private static bool Matches(object? cell, GridFilterDefinition filter)
    {
        var left = cell?.ToString() ?? string.Empty;
        var right = filter.Value?.ToString() ?? string.Empty;
        return filter.Operator switch
        {
            GridFilterOperator.Equals => string.Equals(left, right, StringComparison.CurrentCultureIgnoreCase),
            GridFilterOperator.NotEquals => !string.Equals(left, right, StringComparison.CurrentCultureIgnoreCase),
            GridFilterOperator.Contains => left.Contains(right, StringComparison.CurrentCultureIgnoreCase),
            GridFilterOperator.StartsWith => left.StartsWith(right, StringComparison.CurrentCultureIgnoreCase),
            GridFilterOperator.GreaterThan => GridObjectComparer.Instance.Compare(cell, filter.Value) > 0,
            GridFilterOperator.LessThan => GridObjectComparer.Instance.Compare(cell, filter.Value) < 0,
            GridFilterOperator.Before => GridObjectComparer.Instance.Compare(cell, filter.Value) < 0,
            GridFilterOperator.After => GridObjectComparer.Instance.Compare(cell, filter.Value) > 0,
            GridFilterOperator.Between => GridObjectComparer.Instance.Compare(cell, filter.Value) >= 0 &&
                GridObjectComparer.Instance.Compare(cell, filter.Value2) <= 0,
            GridFilterOperator.IsEmpty => string.IsNullOrEmpty(left),
            GridFilterOperator.IsNotEmpty => !string.IsNullOrEmpty(left),
            GridFilterOperator.True => cell is true,
            GridFilterOperator.False => cell is false,
            GridFilterOperator.Any => true,
            _ => false,
        };
    }

    private static object? ConvertCandidate(object? candidate, object? current)
    {
        var text = candidate?.ToString();
        return current switch
        {
            int when int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) => value,
            decimal when decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) => value,
            bool when bool.TryParse(text, out var value) => value,
            DateOnly when DateOnly.TryParse(text, CultureInfo.CurrentCulture, out var value) => value,
            DateTime when DateTime.TryParse(text, CultureInfo.CurrentCulture, out var value) => value,
            _ => candidate,
        };
    }

    private sealed class GridObjectComparer : IComparer<object?>
    {
        public static GridObjectComparer Instance { get; } = new();
        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1; if (y is null) return 1;
            if (x is IComparable comparable && x.GetType() == y.GetType()) return comparable.CompareTo(y);
            return string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
