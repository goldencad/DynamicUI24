using System.Collections.Concurrent;
using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

public enum GridColumnPin { None, Left }
public enum GridDensity { Comfortable, Compact }
public enum GridPreferenceScopeKind { GlobalUser, UserGrid, UserCompanyGrid }

/// <summary>A storage key with no business-tenancy assumption.</summary>
public sealed record GridPreferenceScope(GridPreferenceScopeKind Kind, string UserId, string GridCode,
    string? CompanyId = null)
{
    public string StorageKey => $"{Kind}:{UserId.Trim()}:{CompanyId?.Trim() ?? "-"}:{GridCode.Trim().ToUpperInvariant()}";
}

/// <summary>Presentation-only state for a semantic column. It never contains row or cell values.</summary>
public sealed record GridColumnPreference(VariableCode VariableCode, int Order, decimal? Width = null,
    bool? IsVisible = null, GridColumnPin Pin = GridColumnPin.None, decimal? WidthScalePercent = null);

public sealed record GridViewPreference(string GridCode, int SchemaVersion,
    ImmutableArray<GridColumnPreference> Columns, ImmutableArray<GridSortDefinition> Sorts,
    ImmutableArray<GridFilterDescriptor> Filters, GridDensity Density = GridDensity.Comfortable,
    string? ViewName = null, decimal RowHeightScalePercent = 100m, GridFindScope? FindScope = null,
    bool ShowRowNumbers = true)
{
    public const int CurrentSchemaVersion = 1;
    public static GridViewPreference Empty(string gridCode) =>
        new(gridCode.Trim().ToUpperInvariant(), CurrentSchemaVersion, [], [], []);
}

public interface IGridViewPreferenceStore
{
    ValueTask<GridViewPreference?> LoadAsync(GridPreferenceScope scope, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(GridPreferenceScope scope, GridViewPreference preference, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(GridPreferenceScope scope, CancellationToken cancellationToken = default);
}

/// <summary>Demo/test seam. Production applications can replace it without changing the grid.</summary>
public sealed class InMemoryGridViewPreferenceStore : IGridViewPreferenceStore
{
    private readonly ConcurrentDictionary<string, GridViewPreference> preferences = new(StringComparer.Ordinal);
    public ValueTask<GridViewPreference?> LoadAsync(GridPreferenceScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(preferences.GetValueOrDefault(scope.StorageKey));
    }
    public ValueTask SaveAsync(GridPreferenceScope scope, GridViewPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        preferences[scope.StorageKey] = preference; return ValueTask.CompletedTask;
    }
    public ValueTask RemoveAsync(GridPreferenceScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        preferences.TryRemove(scope.StorageKey, out _); return ValueTask.CompletedTask;
    }
}

public sealed record GridPresentedColumn(ResolvedGridColumn Column, int Order, decimal Width,
    bool IsVisible, GridColumnPin Pin, decimal WidthScalePercent = 100m)
{
    public VariableCode VariableCode => Column.Definition.VariableCode;
}

public sealed record GridViewResolution(ImmutableArray<GridPresentedColumn> Columns,
    GridViewPreference RepairedPreference, ImmutableArray<GridDiagnostic> Diagnostics)
{
    public ImmutableArray<GridPresentedColumn> VisibleColumns => Columns.Where(x => x.IsVisible)
        .OrderBy(x => x.Pin == GridColumnPin.Left ? 0 : 1).ThenBy(x => x.Order).ToImmutableArray();
}

public static class GridViewPreferenceResolver
{
    public const decimal DefaultPinnedWidthBudget = 640m;

    public static GridViewResolution Resolve(ResolvedGridDefinition metadata, GridViewPreference? preference,
        decimal pinnedWidthBudget = DefaultPinnedWidthBudget)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var diagnostics = ImmutableArray.CreateBuilder<GridDiagnostic>();
        var usable = preference is not null && preference.SchemaVersion == GridViewPreference.CurrentSchemaVersion &&
            preference.GridCode.Equals(metadata.Definition.GridCode, StringComparison.OrdinalIgnoreCase);
        if (preference is not null && !usable) diagnostics.Add(new("GRID_PREFERENCE_VERSION_UNSUPPORTED"));
        var preferred = (usable ? preference!.Columns : []).GroupBy(x => x.VariableCode)
            .ToDictionary(x => x.Key, x => { if (x.Count() > 1) diagnostics.Add(new("GRID_PREFERENCE_DUPLICATE", x.Key.Value)); return x.First(); });
        var result = new List<GridPresentedColumn>(metadata.Columns.Length);
        foreach (var column in metadata.Columns)
        {
            preferred.TryGetValue(column.Definition.VariableCode, out var item);
            var order = item?.Order >= 0 ? item.Order : column.Definition.DisplayOrder;
            if (item?.Order < 0) diagnostics.Add(new("GRID_PREFERENCE_ORDER_INVALID", column.Definition.ColumnId));
            var widthScale = Math.Clamp(item?.WidthScalePercent ??
                (item?.Width is > 0 ? item.Width.Value / column.Width * 100m : 100m), 50m, 300m);
            var width = item?.WidthScalePercent is not null
                ? Math.Clamp(column.Width * widthScale / 100m, column.MinWidth, column.MaxWidth)
                : item?.Width is > 0 ? Math.Clamp(item.Width.Value, column.MinWidth, column.MaxWidth) : column.Width;
            if (item?.Width is <= 0) diagnostics.Add(new("GRID_PREFERENCE_WIDTH_INVALID", column.Definition.ColumnId));
            // Authorization/metadata visibility is authoritative and fails closed.
            var visible = column.IsVisible && (item?.IsVisible ?? true);
            var pin = visible ? item?.Pin ?? GridColumnPin.None : GridColumnPin.None;
            result.Add(new(column, order, width, visible, pin, widthScale));
        }
        var ordered = result.OrderBy(x => x.Order).ThenBy(x => x.Column.Definition.DisplayOrder)
            .ThenBy(x => x.VariableCode.Value, StringComparer.Ordinal).ToList();
        decimal pinned = 0;
        for (var index = 0; index < ordered.Count; index++)
        {
            var item = ordered[index];
            if (item.Pin != GridColumnPin.Left) continue;
            if (pinned + item.Width > Math.Max(0, pinnedWidthBudget))
            {
                diagnostics.Add(new("GRID_PIN_WIDTH_BUDGET", item.Column.Definition.ColumnId));
                ordered[index] = item with { Pin = GridColumnPin.None };
            }
            else pinned += item.Width;
        }
        var repairedColumns = ordered.Select((x, index) => new GridColumnPreference(x.VariableCode, index,
            x.Width, x.IsVisible, x.Pin, x.WidthScalePercent)).ToImmutableArray();
        var repaired = new GridViewPreference(metadata.Definition.GridCode, GridViewPreference.CurrentSchemaVersion,
            repairedColumns, usable ? preference!.Sorts : [], usable ? preference!.Filters : [],
            usable ? preference!.Density : GridDensity.Comfortable, usable ? preference!.ViewName : null,
            Math.Clamp(usable ? preference!.RowHeightScalePercent : 100m, 75m, 300m),
            usable ? preference!.FindScope : null,
            usable ? preference!.ShowRowNumbers : metadata.Definition.Presentation.RowNumbersShownByDefault);
        return new(ordered.Select((x, index) => x with { Order = index }).ToImmutableArray(), repaired, diagnostics.ToImmutable());
    }
}

public enum GridFilterDataType { Text, Number, Date, Boolean }
public enum GridFilterOperatorKind
{
    Contains, Equals, StartsWith, GreaterThan, LessThan, Between, Before, After, IsEmpty, IsNotEmpty, True, False, Any
}

/// <summary>Typed provider-facing filter; UI controls and suggested raw values are deliberately absent.</summary>
public sealed record GridFilterDescriptor(VariableCode VariableCode, GridFilterOperatorKind Operator,
    GridFilterDataType DataType, object? Value = null, object? Value2 = null)
{
    public bool RequiresValue => Operator is not (GridFilterOperatorKind.IsEmpty or GridFilterOperatorKind.IsNotEmpty or
        GridFilterOperatorKind.True or GridFilterOperatorKind.False or GridFilterOperatorKind.Any);
    public bool IsValid => GridFilterValidation.IsValid(this);
}

public static class GridFilterValidation
{
    public static bool IsValid(GridFilterDescriptor filter)
    {
        var allowed = filter.DataType switch
        {
            GridFilterDataType.Text => filter.Operator is GridFilterOperatorKind.Contains or GridFilterOperatorKind.Equals or
                GridFilterOperatorKind.StartsWith or GridFilterOperatorKind.IsEmpty or GridFilterOperatorKind.IsNotEmpty,
            GridFilterDataType.Number => filter.Operator is GridFilterOperatorKind.Equals or GridFilterOperatorKind.GreaterThan or
                GridFilterOperatorKind.LessThan or GridFilterOperatorKind.Between or GridFilterOperatorKind.IsEmpty,
            GridFilterDataType.Date => filter.Operator is GridFilterOperatorKind.Equals or GridFilterOperatorKind.Before or
                GridFilterOperatorKind.After or GridFilterOperatorKind.Between or GridFilterOperatorKind.IsEmpty,
            GridFilterDataType.Boolean => filter.Operator is GridFilterOperatorKind.True or GridFilterOperatorKind.False or GridFilterOperatorKind.Any,
            _ => false,
        };
        if (!allowed || filter.RequiresValue && filter.Value is null) return false;
        if (filter.Operator == GridFilterOperatorKind.Between && filter.Value2 is null) return false;
        return filter.DataType switch
        {
            GridFilterDataType.Number when filter.RequiresValue => IsNumber(filter.Value) &&
                (filter.Operator != GridFilterOperatorKind.Between || IsNumber(filter.Value2)),
            GridFilterDataType.Date when filter.RequiresValue => IsDate(filter.Value) &&
                (filter.Operator != GridFilterOperatorKind.Between || IsDate(filter.Value2)),
            _ => true,
        };
    }
    private static bool IsNumber(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal ||
        decimal.TryParse(value?.ToString(), out _);
    private static bool IsDate(object? value) => value is DateOnly or DateTime or DateTimeOffset || DateOnly.TryParse(value?.ToString(), out _);
}
