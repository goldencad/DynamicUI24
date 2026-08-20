using System.Collections.Immutable;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

public sealed partial class DataEntryGridRuntime
{
    private GridViewPreference? viewPreference;
    private IGridViewPreferenceStore? preferenceStore;
    private GridPreferenceScope? preferenceScope;
    private Task pendingPreferenceSave = Task.CompletedTask;

    public GridViewResolution View => GridViewPreferenceResolver.Resolve(ResolvedDefinition, viewPreference);
    public ImmutableArray<GridPresentedColumn> PresentedColumns => View.VisibleColumns;
    public GridViewPreference CurrentViewPreference => View.RepairedPreference;
    public GridFindScope? RememberedFindScope => CurrentViewPreference.FindScope;

    public void ConfigurePreferencePersistence(IGridViewPreferenceStore store, GridPreferenceScope scope)
    {
        preferenceStore = store ?? throw new ArgumentNullException(nameof(store));
        preferenceScope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public Task FlushPreferencePersistenceAsync() => pendingPreferenceSave;

    internal void MutateViewPreference(Func<GridViewPreference, GridViewPreference> mutation, string reason)
    {
        viewPreference = GridViewPreferenceResolver.Resolve(ResolvedDefinition,
            mutation(CurrentViewPreference)).RepairedPreference;
        OnChanged(reason);
        if (preferenceStore is { } store && preferenceScope is { } scope)
        {
            var snapshot = CurrentViewPreference with { Sorts = Sorts };
            pendingPreferenceSave = pendingPreferenceSave.ContinueWith(async _ =>
                await store.SaveAsync(scope, snapshot).ConfigureAwait(false), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
        }
    }

    public void RememberFindScope(GridFindScope scope)
    {
        if (!Enum.IsDefined(scope)) return;
        viewPreference = CurrentViewPreference with { FindScope = scope };
        OnChanged("FIND_SCOPE_PREFERENCE");
    }

    public void ApplyViewPreference(GridViewPreference? preference)
    {
        viewPreference = GridViewPreferenceResolver.Resolve(ResolvedDefinition, preference).RepairedPreference;
        ReconcileActiveColumn();
        OnChanged("VIEW_PREFERENCE");
    }

    public bool ResizeColumn(VariableCode variableCode, decimal width)
    {
        var item = View.Columns.FirstOrDefault(x => x.VariableCode == variableCode);
        if (item is null || width <= 0) return false;
        var bounded = Math.Clamp(width, item.Column.MinWidth, item.Column.MaxWidth);
        return UpdateColumn(variableCode, x => x with { Width = bounded,
            WidthScalePercent = Math.Clamp(bounded / x.Column.Width * 100m, 50m, 300m) }, "COLUMN_RESIZE");
    }

    public bool ResetColumnWidth(VariableCode variableCode)
    {
        var metadata = ResolvedDefinition.Columns.FirstOrDefault(x => x.Definition.VariableCode == variableCode);
        return metadata is not null && UpdateColumn(variableCode, x => x with
            { Width = metadata.Width, WidthScalePercent = 100m }, "COLUMN_WIDTH_RESET");
    }

    public bool SetColumnWidthPercentage(VariableCode variableCode, decimal percentage)
    {
        var item = View.Columns.FirstOrDefault(x => x.VariableCode == variableCode);
        if (item is null) return false;
        var scale = Math.Clamp(percentage, 50m, 300m);
        return UpdateColumn(variableCode, x => x with
        {
            Width = Math.Clamp(x.Column.Width * scale / 100m, x.Column.MinWidth, x.Column.MaxWidth),
            WidthScalePercent = scale,
        }, "COLUMN_WIDTH_PERCENTAGE");
    }

    public bool IncreaseColumnWidth(VariableCode variableCode) =>
        SetColumnWidthPercentage(variableCode, GetColumnWidthPercentage(variableCode) + 10m);

    public bool DecreaseColumnWidth(VariableCode variableCode) =>
        SetColumnWidthPercentage(variableCode, GetColumnWidthPercentage(variableCode) - 10m);

    public decimal GetColumnWidthPercentage(VariableCode variableCode) =>
        View.Columns.FirstOrDefault(x => x.VariableCode == variableCode)?.WidthScalePercent ?? 100m;

    public bool ReorderColumn(VariableCode variableCode, int targetVisibleIndex)
    {
        var visible = View.VisibleColumns.ToList();
        var item = visible.FirstOrDefault(x => x.VariableCode == variableCode);
        if (item is null || targetVisibleIndex < 0 || targetVisibleIndex >= visible.Count) return false;
        visible.Remove(item); visible.Insert(targetVisibleIndex, item);
        var orderedCodes = visible.Select(x => x.VariableCode).Concat(View.Columns.Where(x => !x.IsVisible).Select(x => x.VariableCode)).ToArray();
        var map = orderedCodes.Select((code, index) => (code, index)).ToDictionary(x => x.code, x => x.index);
        return UpdateColumns(x => x with { Order = map[x.VariableCode] }, "COLUMN_REORDER");
    }

    public bool SetColumnVisible(VariableCode variableCode, bool visible)
    {
        var item = View.Columns.FirstOrDefault(x => x.VariableCode == variableCode);
        if (item is null || visible && !item.Column.IsVisible) return false;
        if (!visible && View.VisibleColumns.Length <= 1) return false;
        var changed = UpdateColumn(variableCode, x => x with { IsVisible = visible, Pin = visible ? x.Pin : GridColumnPin.None },
            visible ? "COLUMN_SHOW" : "COLUMN_HIDE");
        if (changed) ReconcileActiveColumn();
        return changed;
    }

    public bool SetColumnPin(VariableCode variableCode, GridColumnPin pin, decimal budget = GridViewPreferenceResolver.DefaultPinnedWidthBudget)
    {
        if (!Enum.IsDefined(pin)) return false;
        var item = View.Columns.FirstOrDefault(x => x.VariableCode == variableCode && x.IsVisible);
        if (item is null) return false;
        var pinnedWidth = View.Columns.Where(x => x.VariableCode != variableCode && x.Pin == GridColumnPin.Left).Sum(x => x.Width);
        if (pin == GridColumnPin.Left && pinnedWidth + item.Width > budget) return false;
        return UpdateColumn(variableCode, x => x with { Pin = pin }, pin == GridColumnPin.Left ? "COLUMN_PIN" : "COLUMN_UNPIN");
    }

    public void ResetView()
    {
        viewPreference = null;
        Sorts = Definition.DefaultSort;
        Filters = Definition.DefaultFilter;
        ReconcileActiveColumn();
        OnChanged("VIEW_RESET");
    }

    public async ValueTask RestoreViewAsync(IGridViewPreferenceStore store, GridPreferenceScope scope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ApplyViewPreference(await store.LoadAsync(scope, cancellationToken).ConfigureAwait(false));
    }

    public ValueTask SaveViewAsync(IGridViewPreferenceStore store, GridPreferenceScope scope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        return store.SaveAsync(scope, CurrentViewPreference with { Sorts = Sorts }, cancellationToken);
    }

    public async Task<GridPasteResult> FillDownAsync(CancellationToken cancellationToken = default) =>
        await FillAsync(down: true, cancellationToken).ConfigureAwait(false);

    public async Task<GridPasteResult> FillRightAsync(CancellationToken cancellationToken = default) =>
        await FillAsync(down: false, cancellationToken).ConfigureAwait(false);

    private async Task<GridPasteResult> FillAsync(bool down, CancellationToken cancellationToken)
    {
        var range = EffectivePrimaryRange();
        if (range is null || range.RowCount < (down ? 2 : 1)) return SetLastPaste(GridPasteResult.Rejected("GRID_FILL_RANGE_REQUIRED"));
        var columns = VisibleColumnsIn(range);
        if (!down && columns.Length < 2) return SetLastPaste(GridPasteResult.Rejected("GRID_FILL_RANGE_REQUIRED"));
        var rows = await ResolveRowsAsync(range.MinimumRowPosition, range.RowCount, cancellationToken).ConfigureAwait(false);
        if (rows.Length != range.RowCount) return SetLastPaste(GridPasteResult.Rejected("GRID_RANGE_ROWS_UNAVAILABLE"));
        var changes = ImmutableArray.CreateBuilder<GridCellChange>();
        var errors = ImmutableArray.CreateBuilder<GridValidationDiagnostic>();
        for (var r = 0; r < rows.Length; r++)
        for (var c = 0; c < columns.Length; c++)
        {
            if (down && r == 0 || !down && c == 0) continue;
            var target = columns[c];
            var code = target.Definition.VariableCode;
            var candidate = down ? rows[0].Values.GetValueOrDefault(code) : rows[r].Values.GetValueOrDefault(columns[0].Definition.VariableCode);
            var diagnostic = !CanEditColumn(code) ? new GridValidationDiagnostic("GRID_CELL_READ_ONLY", "Grid.Validation.ReadOnly") :
                GridValueValidator.Validate(target.Definition, candidate);
            if (diagnostic is not null) errors.Add(diagnostic);
            changes.Add(new(rows[r].RowKey, code, rows[r].Values.GetValueOrDefault(code), candidate,
                diagnostic is null ? GridCellValidationState.Valid : GridCellValidationState.Invalid, diagnostic));
        }
        if (errors.Count > 0 && PasteOptions.CommitMode == PasteCommitMode.Atomic)
            return SetLastPaste(GridPasteResult.Rejected("GRID_FILL_ATOMIC_REJECTED", changes.Count, errors));
        var valid = changes.Where(x => x.ValidationState == GridCellValidationState.Valid).ToImmutableArray();
        if (valid.Length == 0) return SetLastPaste(GridPasteResult.Rejected("GRID_FILL_NO_VALID_CELLS", changes.Count, errors));
        var applied = await ApplyChangesAsync(valid, down ? GridEditSourceAction.FillDown : GridEditSourceAction.FillRight, true, cancellationToken).ConfigureAwait(false);
        return SetLastPaste(new(applied.Success ? valid.Length : 0, changes.Count - (applied.Success ? valid.Length : 0), errors.ToImmutable(), [],
            PasteOptions.CommitMode == PasteCommitMode.Atomic && applied.WasAtomic, errors.Count > 0 && applied.Success, DiagnosticCode: applied.DiagnosticCode));
    }

    private bool UpdateColumn(VariableCode code, Func<GridPresentedColumn, GridPresentedColumn> update, string reason) =>
        UpdateColumns(x => x.VariableCode == code ? update(x) : x, reason);

    private bool UpdateColumns(Func<GridPresentedColumn, GridPresentedColumn> update, string reason)
    {
        var columns = View.Columns.Select(update).OrderBy(x => x.Order).Select((x, index) =>
            new GridColumnPreference(x.VariableCode, index, x.Width, x.IsVisible, x.Pin,
                x.WidthScalePercent)).ToImmutableArray();
        viewPreference = CurrentViewPreference with { Columns = columns };
        OnChanged(reason); return true;
    }

    private void ReconcileActiveColumn()
    {
        if (CellSelection.ActiveCell is not { } active || IsVisibleColumn(active.VariableCode)) return;
        var visible = VisibleVariableCodes();
        if (visible.Length == 0) { ClearCellSelection(); return; }
        var original = ResolvedDefinition.Columns.FindIndex(x => x.Definition.VariableCode == active.VariableCode);
        var replacement = visible.OrderBy(x => Math.Abs(ResolvedDefinition.Columns.FindIndex(c => c.Definition.VariableCode == x) - original)).First();
        var local = Rows.FindIndex(x => x.RowKey == active.RowKey);
        if (local >= 0) SelectCell(new(active.RowKey, replacement), ViewportStartIndex + local);
    }
}
