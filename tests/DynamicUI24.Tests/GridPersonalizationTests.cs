using System.Collections.Immutable;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class GridPersonalizationTests
{
    [Fact]
    public void PreferenceOverlayUsesSemanticIdentityClampsRepairsAndFailsClosed()
    {
        var resolved = Resolve([Column("A", 0, 100, 60, 200), Column("B", 1, 120, 80, 180), Column("C", 2, 140, 90, 240)]);
        var preference = Preference(resolved.Definition.GridCode,
        [
            new(new("B"), 0, 999, true, GridColumnPin.Left),
            new(new("A"), 1, -4, false),
            new(new("REMOVED"), 2, 100, true),
        ]);

        var view = GridViewPreferenceResolver.Resolve(resolved, preference);

        Assert.Equal(["B", "A", "C"], view.Columns.Select(x => x.VariableCode.Value));
        Assert.Equal(180, view.Columns[0].Width);
        Assert.False(view.Columns[1].IsVisible);
        Assert.True(view.Columns[2].IsVisible); // new metadata column uses its default
        Assert.DoesNotContain(view.Columns, x => x.VariableCode == new VariableCode("REMOVED"));
        Assert.Contains(view.Diagnostics, x => x.Code == "GRID_PREFERENCE_WIDTH_INVALID");
    }

    [Fact]
    public void DuplicateAndVersionMismatchAreRepairedWithoutCrashing()
    {
        var resolved = Resolve([Column("A", 0), Column("B", 1)]);
        var duplicate = Preference("GRID", [new(new("A"), 1), new(new("A"), 0)]);
        Assert.Contains(GridViewPreferenceResolver.Resolve(resolved, duplicate).Diagnostics,
            x => x.Code == "GRID_PREFERENCE_DUPLICATE");

        var future = duplicate with { SchemaVersion = 99 };
        var repaired = GridViewPreferenceResolver.Resolve(resolved, future);
        Assert.Equal(["A", "B"], repaired.Columns.Select(x => x.VariableCode.Value));
        Assert.Contains(repaired.Diagnostics, x => x.Code == "GRID_PREFERENCE_VERSION_UNSUPPORTED");
    }

    [Fact]
    public void PinBudgetUnpinsOverflowDeterministically()
    {
        var resolved = Resolve([Column("A", 0, 120), Column("B", 1, 120), Column("C", 2, 120)]);
        var preference = Preference("GRID", [
            new(new("A"), 0, 120, true, GridColumnPin.Left),
            new(new("B"), 1, 120, true, GridColumnPin.Left),
            new(new("C"), 2, 120, true, GridColumnPin.Left)]);
        var view = GridViewPreferenceResolver.Resolve(resolved, preference, 250);
        Assert.Equal(GridColumnPin.Left, view.Columns[0].Pin);
        Assert.Equal(GridColumnPin.Left, view.Columns[1].Pin);
        Assert.Equal(GridColumnPin.None, view.Columns[2].Pin);
        Assert.Contains(view.Diagnostics, x => x.Code == "GRID_PIN_WIDTH_BUDGET");
    }

    [Theory]
    [MemberData(nameof(ValidFilters))]
    public void TypedFiltersAcceptOnlyTypeAppropriateOperators(GridFilterDescriptor filter) => Assert.True(filter.IsValid);

    public static IEnumerable<object[]> ValidFilters() =>
    [
        [new GridFilterDescriptor(new("TEXT"), GridFilterOperatorKind.Contains, GridFilterDataType.Text, "a")],
        [new GridFilterDescriptor(new("NUMBER"), GridFilterOperatorKind.Between, GridFilterDataType.Number, 1, 2)],
        [new GridFilterDescriptor(new("DATE"), GridFilterOperatorKind.After, GridFilterDataType.Date, new DateOnly(2026, 1, 1))],
        [new GridFilterDescriptor(new("FLAG"), GridFilterOperatorKind.Any, GridFilterDataType.Boolean)],
    ];

    [Fact]
    public void InvalidTypedFilterAndRawPreferenceValuesAreRejectedByContract()
    {
        Assert.False(new GridFilterDescriptor(new("N"), GridFilterOperatorKind.Contains, GridFilterDataType.Number, "secret").IsValid);
        Assert.False(new GridFilterDescriptor(new("D"), GridFilterOperatorKind.Between, GridFilterDataType.Date, DateOnly.FromDateTime(DateTime.Today)).IsValid);
        Assert.DoesNotContain(typeof(GridViewPreference).GetProperties(), x => x.Name.Contains("Row", StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains("Cell", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("Value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task InMemoryStoreIsScopedAndResettable()
    {
        var store = new InMemoryGridViewPreferenceStore();
        var a = new GridPreferenceScope(GridPreferenceScopeKind.UserGrid, "u1", "grid");
        var b = new GridPreferenceScope(GridPreferenceScopeKind.UserGrid, "u2", "grid");
        await store.SaveAsync(a, GridViewPreference.Empty("grid"));
        Assert.NotNull(await store.LoadAsync(a)); Assert.Null(await store.LoadAsync(b));
        await store.RemoveAsync(a); Assert.Null(await store.LoadAsync(a));
    }

    private static GridViewPreference Preference(string code, IEnumerable<GridColumnPreference> columns) =>
        new(code, GridViewPreference.CurrentSchemaVersion, columns.ToImmutableArray(), [], []);

    private static ResolvedGridDefinition Resolve(IEnumerable<ColumnDefinition> columns) => GridMetadataResolver.Resolve(
        new GridDefinition("grid", "GRID", columns), null);

    private static ColumnDefinition Column(string code, int order, decimal width = 100, decimal min = 60, decimal max = 300) =>
        new(code, code, new(code), $"Grid.{code}", null, ColumnDataType.Text, ColumnEditorKind.TextBox,
            ColumnMode.Input, order, width, min, max, true, false, null, null, null, null, null, 1, SetupDefinitionStatus.Published);
}
