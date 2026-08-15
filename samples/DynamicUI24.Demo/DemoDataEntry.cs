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

internal sealed class DemoDataEntryProvider : IDataEntryGridProvider
{
    private readonly object sync = new();
    private readonly Dictionary<CompanyId, ImmutableArray<GridRow>> source = new();
    public bool SimulateFailure { get; set; }

    public async Task<GridDataResult> LoadAsync(GridProviderContext context, GridDataRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(context.Company.CompanyId == DemoCompanyData.CompanyAId ? 45 : 5, cancellationToken);
        if (SimulateFailure) throw new InvalidOperationException("Demo provider failure details must remain isolated.");
        ImmutableArray<GridRow> rows;
        lock (sync)
        {
            if (!source.TryGetValue(context.Company.CompanyId, out rows))
                source[context.Company.CompanyId] = rows = BuildRows(context.Company);
        }
        var query = rows.AsEnumerable();
        foreach (var filter in request.Filters) query = query.Where(row => Matches(row, filter));
        IOrderedEnumerable<GridRow>? ordered = null;
        foreach (var sort in request.Sorts.OrderBy(x => x.Priority))
        {
            Func<GridRow, object?> key = row => row.Values.GetValueOrDefault(sort.VariableCode);
            ordered = ordered is null
                ? sort.Direction == GridSortDirection.Ascending ? query.OrderBy(key, GridObjectComparer.Instance) : query.OrderByDescending(key, GridObjectComparer.Instance)
                : sort.Direction == GridSortDirection.Ascending ? ordered.ThenBy(key, GridObjectComparer.Instance) : ordered.ThenByDescending(key, GridObjectComparer.Instance);
        }
        var visible = (ordered ?? query).ToImmutableArray();
        return new(visible.Length == 0 ? GridProviderState.Empty : GridProviderState.Ready, visible, rows.Length, visible.Length);
    }

    public Task<GridCommitResult> CommitAsync(GridProviderContext context, GridCellEdit edit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            if (!source.TryGetValue(context.Company.CompanyId, out var rows))
                return Task.FromResult(GridCommitResult.Rejected("GRID_ROW_UNAVAILABLE"));
            var index = -1;
            for (var candidateIndex = 0; candidateIndex < rows.Length; candidateIndex++)
                if (rows[candidateIndex].RowKey == edit.RowKey) { index = candidateIndex; break; }
            if (index < 0) return Task.FromResult(GridCommitResult.Rejected("GRID_ROW_UNAVAILABLE"));
            var current = rows[index].Values.GetValueOrDefault(edit.VariableCode);
            var value = ConvertCandidate(edit.CandidateValue, current);
            source[context.Company.CompanyId] = rows.SetItem(index, rows[index].WithValue(edit.VariableCode, value));
            return Task.FromResult(GridCommitResult.Success(value));
        }
    }

    private static ImmutableArray<GridRow> BuildRows(CompanyDescriptor company) => Enumerable.Range(1, 30)
        .Select(index =>
        {
            var quantity = index * 2;
            var rate = 7.25m + index / 4m;
            var prefix = company.Code;
            IReadOnlyDictionary<VariableCode, object?> values = new Dictionary<VariableCode, object?>
            {
                [new("ITEM_CODE")] = $"{prefix}-{index:000}", [new("ITEM_NAME")] = $"Sample item {index:00}",
                [new("CATEGORY")] = index % 3 == 0 ? "EXTENDED" : "STANDARD", [new("QUANTITY")] = quantity,
                [new("UNIT_RATE")] = rate, [new("IS_ACTIVE")] = index % 4 != 0,
                [new("START_DATE")] = new DateOnly(2026, 1, 1).AddDays(index), [new("NOTES")] = index % 5 == 0 ? "Review this neutral sample" : null,
                [new("TOTAL")] = quantity * rate, [new("UPDATED_AT")] = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Local).AddMinutes(index),
                [new("REFERENCE")] = $"REF-{1000 + index}", [new("PRIVILEGED_NOTE")] = "hidden value",
            };
            return new GridRow(new($"{company.CompanyId.Value}:ROW:{index:000}"), values, warningCount: index == 7 ? 1 : 0);
        }).ToImmutableArray();

    private static bool Matches(GridRow row, GridFilterDefinition filter)
    {
        row.Values.TryGetValue(filter.VariableCode, out var cell);
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
