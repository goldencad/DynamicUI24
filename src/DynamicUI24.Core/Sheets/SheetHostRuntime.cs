using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Sheets;

public sealed record SheetRuntimeState(SheetCode SheetCode, GridSelectionState Selection,
    int ViewportStartIndex, ImmutableArray<GridFilterDefinition> Filters,
    ImmutableArray<GridSortDefinition> Sorts, string? PersonalizationKey = null,
    long Generation = 0, bool IsDirty = false, GridViewPreference? ViewPreference = null,
    ImmutableDictionary<RowKey, decimal>? RowHeightOverrides = null)
{
    public static SheetRuntimeState Empty(SheetCode code) => new(code, GridSelectionState.Empty, 0, [], []);
}

public interface ISheetRuntimeMaterializer
{
    object Materialize(SheetDefinition definition, SheetRuntimeState retainedState);
    SheetRuntimeState Capture(SheetDefinition definition, object runtime, SheetRuntimeState retainedState);
    void Release(SheetDefinition definition, object runtime);
}

public sealed class SheetHostChangedEventArgs(string reason, SheetCode? sheetCode = null) : EventArgs
{
    public string Reason { get; } = reason;
    public SheetCode? SheetCode { get; } = sheetCode;
}

/// <summary>UI-free coordinator. It retains compact semantic state and bounds materialized content with LRU eviction.</summary>
public sealed class SheetHostRuntime
{
    private readonly ISheetRuntimeMaterializer materializer;
    private readonly ISheetLifecycleProvider lifecycle;
    private readonly ISheetCalculationCompatibility calculation;
    private readonly Dictionary<SheetCode, SheetDefinition> sheets;
    private readonly Dictionary<SheetCode, SheetRuntimeState> states = [];
    private readonly Dictionary<SheetCode, object> materialized = [];
    private readonly LinkedList<SheetCode> recency = [];
    private EffectiveAuthorizationContext? authorization;

    public SheetHostRuntime(SheetHostDefinition definition, ISheetRuntimeMaterializer materializer,
        ISheetLifecycleProvider lifecycle, ISheetCalculationCompatibility calculation,
        EffectiveAuthorizationContext? authorization = null, SheetCode? preferredActiveSheet = null)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));
        this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        this.calculation = calculation ?? throw new ArgumentNullException(nameof(calculation));
        this.authorization = authorization;
        sheets = definition.Sheets.ToDictionary(x => x.SheetCode);
        foreach (var sheet in sheets.Values) states[sheet.SheetCode] = SheetRuntimeState.Empty(sheet.SheetCode);
        ActiveSheetCode = ResolveEligible(preferredActiveSheet);
    }

    public SheetHostDefinition Definition { get; }
    public SheetCode? ActiveSheetCode { get; private set; }
    public ImmutableArray<SheetDefinition> Sheets => sheets.Values.OrderBy(x => x.DisplayOrder)
        .ThenBy(x => x.SheetCode.Value, StringComparer.Ordinal).ToImmutableArray();
    public ImmutableArray<SheetDefinition> VisibleSheets => Sheets.Where(x => !x.IsHidden && IsAuthorized(x)).ToImmutableArray();
    public ImmutableArray<SheetDefinition> HiddenSheets => Sheets.Where(x => x.IsHidden && IsAuthorized(x)).ToImmutableArray();
    public int MaterializedSheetCount => materialized.Count;
    public IReadOnlyCollection<SheetCode> MaterializedSheetCodes => materialized.Keys;
    public event EventHandler<SheetHostChangedEventArgs>? Changed;

    public bool TryActivate(SheetCode sheetCode)
    {
        if (!sheets.TryGetValue(sheetCode, out var sheet) || sheet.IsHidden || !IsAuthorized(sheet)) return false;
        if (ActiveSheetCode is { } previous && previous != sheetCode) Capture(previous);
        ActiveSheetCode = sheetCode; EnsureMaterialized(sheet); Touch(sheetCode);
        Changed?.Invoke(this, new("ACTIVATED", sheetCode)); return true;
    }

    public object? GetActiveRuntime() => ActiveSheetCode is { } code && materialized.TryGetValue(code, out var value)
        ? value : ActiveSheetCode is { } active && sheets.TryGetValue(active, out var sheet) ? EnsureMaterialized(sheet) : null;
    public SheetRuntimeState GetRetainedState(SheetCode code) => states.TryGetValue(code, out var state)
        ? state : throw new KeyNotFoundException(code.Value);
    public void RetainState(SheetRuntimeState state)
    {
        if (!sheets.ContainsKey(state.SheetCode)) throw new KeyNotFoundException(state.SheetCode.Value);
        states[state.SheetCode] = state;
    }

    public bool Rename(SheetCode code, LocalizationKey title, LocalizationKey? subtitle = null)
    {
        if (!Definition.Capabilities.AllowRename || !sheets.TryGetValue(code, out var sheet)) return false;
        sheets[code] = sheet with { TitleKey = title, SubtitleKey = subtitle };
        Changed?.Invoke(this, new("RENAMED", code)); return true;
    }

    public bool Reorder(SheetCode code, int displayOrder)
    {
        if (!Definition.Capabilities.AllowReorder || !sheets.TryGetValue(code, out var sheet) || !sheet.IsReorderable) return false;
        sheets[code] = sheet with { DisplayOrder = displayOrder };
        Changed?.Invoke(this, new("REORDERED", code)); return true;
    }

    public bool SetHidden(SheetCode code, bool hidden)
    {
        if (!Definition.Capabilities.AllowHide || !sheets.TryGetValue(code, out var sheet) || !sheet.IsHideable) return false;
        sheets[code] = sheet with { IsHidden = hidden };
        if (hidden && ActiveSheetCode == code) ResolveActiveAfterLoss(code);
        Changed?.Invoke(this, new(hidden ? "HIDDEN" : "SHOWN", code)); return true;
    }

    public async Task<SheetLifecycleResult> CreateAsync(CancellationToken token = default)
    {
        if (!Definition.Capabilities.AllowCreate) return SheetLifecycleResult.Rejected("SHEET_CREATE_NOT_ALLOWED");
        var result = await lifecycle.CreateAsync(token).ConfigureAwait(false);
        if (!result.IsSuccess || result.Sheet is null) return result;
        if (sheets.ContainsKey(result.Sheet.SheetCode)) return SheetLifecycleResult.Rejected("SHEET_PROVIDER_IDENTITY_INVALID");
        sheets.Add(result.Sheet.SheetCode, result.Sheet);
        states.Add(result.Sheet.SheetCode, SheetRuntimeState.Empty(result.Sheet.SheetCode));
        Changed?.Invoke(this, new("CREATED", result.Sheet.SheetCode)); return result;
    }

    public async Task<SheetLifecycleResult> DuplicateAsync(SheetCloneRequest request, CancellationToken token = default) =>
        await CloneAsync(request, SheetLifecycleAction.Duplicate, token).ConfigureAwait(false);
    public async Task<SheetLifecycleResult> SaveAsAsync(SheetCloneRequest request, CancellationToken token = default) =>
        await CloneAsync(request, SheetLifecycleAction.SaveAs, token).ConfigureAwait(false);

    public async Task<SheetLifecycleResult> DeleteAsync(SheetCode code, bool confirmed = false,
        CancellationToken token = default)
    {
        if (!Definition.Capabilities.AllowDelete || !sheets.TryGetValue(code, out var sheet) || !sheet.IsClosable)
            return SheetLifecycleResult.Rejected("SHEET_DELETE_NOT_ALLOWED");
        var validation = await calculation.ValidateDeleteAsync(code, token).ConfigureAwait(false);
        if (!validation.IsSuccess || validation.RequiresConfirmation && !confirmed)
            return SheetLifecycleResult.Rejected(validation.Diagnostics.FirstOrDefault()?.Code ?? "SHEET_DELETE_BLOCKED",
                validation.RequiresConfirmation);
        var result = await lifecycle.DeleteAsync(code, token).ConfigureAwait(false);
        if (!result.IsSuccess) return result;
        Evict(code); sheets.Remove(code); states.Remove(code);
        if (ActiveSheetCode == code) ResolveActiveAfterLoss(code);
        Changed?.Invoke(this, new("DELETED", code)); return result;
    }

    public async Task<SheetCalculationResult> RequestRecalculationAsync(IEnumerable<SheetCode> changed,
        CancellationToken token = default) => await calculation.RequestRecalculationAsync(changed.Distinct(), token).ConfigureAwait(false);

    public void UpdateAuthorization(EffectiveAuthorizationContext? value)
    {
        authorization = value;
        if (ActiveSheetCode is { } active && (!sheets.TryGetValue(active, out var sheet) || !IsAuthorized(sheet)))
            ResolveActiveAfterLoss(active);
        Changed?.Invoke(this, new("AUTHORIZATION_CHANGED", ActiveSheetCode));
    }

    private async Task<SheetLifecycleResult> CloneAsync(SheetCloneRequest request, SheetLifecycleAction action,
        CancellationToken token)
    {
        var allowed = action == SheetLifecycleAction.Duplicate ? Definition.Capabilities.AllowDuplicate : Definition.Capabilities.AllowSaveAs;
        if (!allowed || !sheets.TryGetValue(request.SourceSheetCode, out var source) ||
            action == SheetLifecycleAction.Duplicate && !source.IsDuplicable ||
            action == SheetLifecycleAction.SaveAs && !source.IsSaveAsEnabled)
            return SheetLifecycleResult.Rejected("SHEET_CLONE_NOT_ALLOWED");
        if (request.SourceSheetCode == request.TargetSheetCode || sheets.ContainsKey(request.TargetSheetCode))
            return SheetLifecycleResult.Rejected("SHEET_CLONE_IDENTITY_INVALID");
        var validation = await calculation.ValidateCloneAsync(request, token).ConfigureAwait(false);
        if (!validation.IsSuccess) return SheetLifecycleResult.Rejected(validation.Diagnostics.FirstOrDefault()?.Code ?? "SHEET_CLONE_VALIDATION_FAILED");
        var result = await lifecycle.CloneAsync(request, token).ConfigureAwait(false);
        if (!result.IsSuccess || result.Sheet is null) return result;
        if (result.Sheet.SheetCode == request.SourceSheetCode || result.Sheet.SheetCode != request.TargetSheetCode || sheets.ContainsKey(result.Sheet.SheetCode))
            return SheetLifecycleResult.Rejected("SHEET_PROVIDER_IDENTITY_INVALID");
        sheets.Add(result.Sheet.SheetCode, result.Sheet); states.Add(result.Sheet.SheetCode, SheetRuntimeState.Empty(result.Sheet.SheetCode));
        Changed?.Invoke(this, new(action == SheetLifecycleAction.Duplicate ? "DUPLICATED" : "SAVED_AS", result.Sheet.SheetCode));
        return result;
    }

    private object EnsureMaterialized(SheetDefinition sheet)
    {
        if (materialized.TryGetValue(sheet.SheetCode, out var runtime)) { Touch(sheet.SheetCode); return runtime; }
        while (materialized.Count >= Definition.MaximumMaterializedSheets)
        {
            var victim = recency.Last?.Value;
            if (victim is null) break;
            Evict(victim.Value);
        }
        runtime = materializer.Materialize(sheet, states[sheet.SheetCode]); materialized.Add(sheet.SheetCode, runtime);
        Touch(sheet.SheetCode); return runtime;
    }
    private void Capture(SheetCode code)
    {
        if (sheets.TryGetValue(code, out var sheet) && materialized.TryGetValue(code, out var runtime))
            states[code] = materializer.Capture(sheet, runtime, states[code]);
    }
    private void Evict(SheetCode code)
    {
        Capture(code);
        if (sheets.TryGetValue(code, out var sheet) && materialized.Remove(code, out var runtime)) materializer.Release(sheet, runtime);
        var node = recency.Find(code); if (node is not null) recency.Remove(node);
    }
    private void Touch(SheetCode code) { var node = recency.Find(code); if (node is not null) recency.Remove(node); recency.AddFirst(code); }
    private bool IsAuthorized(SheetDefinition sheet) => sheet.PresentationRequirement is null ||
        AuthorizationPresentationResolver.Resolve(sheet.PresentationRequirement, authorization) != AuthorizationPresentationState.Hidden;
    private SheetCode? ResolveEligible(SheetCode? preferred) => preferred is { } code && sheets.TryGetValue(code, out var candidate) &&
        !candidate.IsHidden && IsAuthorized(candidate) ? code : VisibleSheets.FirstOrDefault()?.SheetCode;
    private void ResolveActiveAfterLoss(SheetCode lost)
    {
        Capture(lost); ActiveSheetCode = ResolveEligible(null);
        if (ActiveSheetCode is { } next && sheets.TryGetValue(next, out var sheet)) EnsureMaterialized(sheet);
    }
}
