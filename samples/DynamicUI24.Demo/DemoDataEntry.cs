using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;
using DynamicUI24.Core.Companies;
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
            Column("notes", "NOTES", "NOTES", "Grid.Column.Notes", ColumnDataType.MultilineText, ColumnEditorKind.TextBox, ColumnMode.Input, 70, 180),
            Column("total", "TOTAL", "TOTAL", "Grid.Column.Total", ColumnDataType.Formula, ColumnEditorKind.Formula, ColumnMode.Formula, 80, 126, format: "N2"),
            Column("updated", "UPDATED_AT", "UPDATED_AT", "Grid.Column.UpdatedAt", ColumnDataType.System, ColumnEditorKind.ReadOnly, ColumnMode.System, 90, 155, format: "g"),
            Column("reference", "REFERENCE", "REFERENCE", "Grid.Column.Reference", ColumnDataType.Reference, ColumnEditorKind.ReadOnly, ColumnMode.Input, 100, 135),
            Column("secret", "PRIVILEGED_NOTE", "PRIVILEGED_NOTE", "Grid.Column.Privileged", ColumnDataType.Text, ColumnEditorKind.ReadOnly, ColumnMode.System, 110, 140,
                visible: true, permission: "DEMO.PRIVILEGED"),
        };
        return new("demo-data-grid", "DEMO_DATA_GRID", columns, new("Grid.Title"),
            [new(new("ITEM_CODE"), GridSortDirection.Ascending)], selectionMode: GridSelectionMode.Multiple,
            allowEdit: true, allowAdd: true, showRowNumbers: true, showStatusBar: true);
    }

    private static ColumnDefinition Column(string id, string code, string variable, string label, ColumnDataType type,
        ColumnEditorKind editor, ColumnMode mode, int order, decimal width, bool required = false,
        bool visible = true, string? permission = null, string? format = null) =>
        new(id, code, new(variable), label, null, type, editor, mode, order, width, 64, 360,
            visible, required, permission, format, null, null, null, 1, SetupDefinitionStatus.Published);
}

internal sealed class DemoDataEntryProvider : IVirtualizedGridDataProvider
{
    public const int LogicalRowCount = 100_000;
    private readonly object sync = new();
    private readonly Dictionary<(CompanyId CompanyId, RowKey RowKey, VariableCode VariableCode), object?> edits = [];
    private int generatedRowCount;
    private int viewportRequestCount;
    public bool SimulateFailure { get; set; }
    public int GeneratedRowCount => Volatile.Read(ref generatedRowCount);
    public int ViewportRequestCount => Volatile.Read(ref viewportRequestCount);

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
        for (var offset = 0; offset < LogicalRowCount; offset++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var logicalIndex = descending ? LogicalRowCount - offset : offset + 1;
            if (!Matches(logicalIndex, context.Company, request.FilterDefinitions)) continue;
            if (matched >= wantedStart && rows.Count < wantedCount) rows.Add(BuildRow(context.Company, logicalIndex));
            matched++;
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

    private GridRow BuildRow(CompanyDescriptor company, int index)
    {
        var quantity = index * 2;
        var rate = 7.25m + index / 4m;
        var prefix = company.Code;
        var rowKey = new RowKey($"{company.CompanyId.Value}:ROW:{index:000000}");
        var values = new Dictionary<VariableCode, object?>
        {
            [new("ITEM_CODE")] = $"{prefix}-{index:000000}", [new("ITEM_NAME")] = $"Sample item {index:000000}",
            [new("CATEGORY")] = index % 3 == 0 ? "EXTENDED" : "STANDARD", [new("QUANTITY")] = quantity,
            [new("UNIT_RATE")] = rate, [new("IS_ACTIVE")] = index % 4 != 0,
            [new("START_DATE")] = new DateOnly(2026, 1, 1).AddDays(index % 3650),
            [new("NOTES")] = index % 5 == 0 ? "Review this neutral sample" : null,
            [new("TOTAL")] = quantity * rate,
            [new("UPDATED_AT")] = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Local).AddMinutes(index % 525600),
            [new("REFERENCE")] = $"REF-{1000000 + index}", [new("PRIVILEGED_NOTE")] = "hidden value",
        };
        lock (sync)
            foreach (var edit in edits.Where(x => x.Key.CompanyId == company.CompanyId && x.Key.RowKey == rowKey))
                values[edit.Key.VariableCode] = edit.Value;
        return new GridRow(rowKey, values, warningCount: index % 997 == 0 ? 1 : 0);
    }

    private static bool Matches(int index, CompanyDescriptor company, IEnumerable<GridFilterDefinition> filters)
    {
        foreach (var filter in filters)
        {
            object? value = filter.VariableCode.Value switch
            {
                "ITEM_CODE" => $"{company.Code}-{index:000000}", "ITEM_NAME" => $"Sample item {index:000000}",
                "CATEGORY" => index % 3 == 0 ? "EXTENDED" : "STANDARD", "QUANTITY" => index * 2,
                "UNIT_RATE" => 7.25m + index / 4m, "IS_ACTIVE" => index % 4 != 0, _ => null,
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
            GridFilterOperator.IsEmpty => string.IsNullOrEmpty(left),
            GridFilterOperator.IsNotEmpty => !string.IsNullOrEmpty(left),
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
