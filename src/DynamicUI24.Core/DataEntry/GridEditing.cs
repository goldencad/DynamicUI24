using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

public enum PasteCommitMode { Atomic, PartialValid }
public enum GridEditSourceAction { SingleCell, Paste, Cut, Clear, FillDown, FillRight, Undo, Redo }
public enum GridEditCommitState { Pending, Committed, Rejected }
public enum GridCellValidationState { Valid, Invalid }

public sealed record GridPasteOptions
{
    public GridPasteOptions(PasteCommitMode commitMode = PasteCommitMode.Atomic, int largeTargetThreshold = 10_000,
        int maximumClipboardCharacters = 4_000_000, int historyDepth = 100, bool allowExactTiling = true)
    {
        if (largeTargetThreshold <= 0) throw new ArgumentOutOfRangeException(nameof(largeTargetThreshold));
        if (maximumClipboardCharacters <= 0) throw new ArgumentOutOfRangeException(nameof(maximumClipboardCharacters));
        if (historyDepth <= 0) throw new ArgumentOutOfRangeException(nameof(historyDepth));
        CommitMode = commitMode;
        LargeTargetThreshold = largeTargetThreshold;
        MaximumClipboardCharacters = maximumClipboardCharacters;
        HistoryDepth = historyDepth;
        AllowExactTiling = allowExactTiling;
    }

    public PasteCommitMode CommitMode { get; }
    public int LargeTargetThreshold { get; }
    public int MaximumClipboardCharacters { get; }
    public int HistoryDepth { get; }
    public bool AllowExactTiling { get; }
}

public sealed record GridCellChange(RowKey RowKey, VariableCode VariableCode, object? OriginalValue,
    object? CandidateValue, GridCellValidationState ValidationState = GridCellValidationState.Valid,
    GridValidationDiagnostic? Diagnostic = null);

public sealed record GridEditTransaction(Guid TransactionId, ImmutableArray<GridCellChange> CellChanges,
    DateTimeOffset CreatedAt, GridEditSourceAction SourceAction, ImmutableArray<GridValidationDiagnostic> ValidationResult,
    GridEditCommitState CommitState = GridEditCommitState.Pending)
{
    public static GridEditTransaction Create(IEnumerable<GridCellChange> changes, GridEditSourceAction source,
        IEnumerable<GridValidationDiagnostic>? diagnostics = null) => new(Guid.NewGuid(), changes.ToImmutableArray(),
            DateTimeOffset.UtcNow, source, (diagnostics ?? []).ToImmutableArray());
}

public sealed record GridBatchCommitResult(bool IsSuccess, string? DiagnosticCode = null)
{
    public static GridBatchCommitResult Success { get; } = new(true);
    public static GridBatchCommitResult Rejected(string code) => new(false, code);
}

/// <summary>Optional provider capability for one logical multi-cell mutation.</summary>
public interface IGridBatchEditProvider
{
    Task<GridBatchCommitResult> CommitBatchAsync(GridProviderContext context, GridEditTransaction transaction,
        CancellationToken cancellationToken = default);
}

/// <summary>Optional logical-position resolver used without changing the active viewport.</summary>
public interface IGridLogicalRowProvider
{
    Task<ImmutableArray<GridRow>> ResolveRowsAsync(GridProviderContext context, int startPosition, int rowCount,
        ImmutableArray<GridSortDefinition> sorts, ImmutableArray<GridFilterDefinition> filters,
        long requestGeneration, CancellationToken cancellationToken = default);
}

public sealed record GridPasteResult(int AppliedCellCount, int RejectedCellCount,
    ImmutableArray<GridValidationDiagnostic> ValidationErrors, ImmutableArray<GridValidationDiagnostic> Warnings,
    bool WasAtomic, bool WasPartial, bool RequiresConfirmation = false, string? DiagnosticCode = null)
{
    public static GridPasteResult Rejected(string code, int rejected = 0,
        IEnumerable<GridValidationDiagnostic>? errors = null, bool requiresConfirmation = false) =>
        new(0, rejected, (errors ?? []).ToImmutableArray(), [], true, false, requiresConfirmation, code);
}

public static class GridPasteConverter
{
    public static (object? Value, GridValidationDiagnostic? Diagnostic) Convert(
        ColumnDefinition column, string text, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (string.IsNullOrEmpty(text)) return (null, GridValueValidator.Validate(column, null));
        object? value = text;
        var valid = column.DataType switch
        {
            ColumnDataType.Integer => long.TryParse(text, NumberStyles.Integer, culture, out var integer) ? (value = integer) is not null : false,
            ColumnDataType.Decimal => decimal.TryParse(text, NumberStyles.Number, culture, out var number) ? (value = number) is not null : false,
            ColumnDataType.Boolean => TryBoolean(text, out value),
            ColumnDataType.Date => DateOnly.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out var date) ? (value = date) is not null : false,
            ColumnDataType.DateTime => DateTime.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out var dateTime) ? (value = dateTime) is not null : false,
            ColumnDataType.Text or ColumnDataType.MultilineText or ColumnDataType.Choice or ColumnDataType.Reference => true,
            _ => false,
        };
        if (!valid) return (null, new("GRID_VALUE_TYPE_INVALID", "Grid.Validation.Type"));
        return (value, GridValueValidator.Validate(column, value));
    }

    private static bool TryBoolean(string text, out object? value)
    {
        if (bool.TryParse(text, out var boolean)) { value = boolean; return true; }
        if (text == "1") { value = true; return true; }
        if (text == "0") { value = false; return true; }
        value = null; return false;
    }
}

internal sealed class GridEditHistory
{
    private readonly int depth;
    private readonly List<GridEditTransaction> undo = [];
    private readonly List<GridEditTransaction> redo = [];
    public GridEditHistory(int depth) => this.depth = depth;
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public int UndoCount => undo.Count;
    public void Record(GridEditTransaction transaction)
    {
        undo.Add(transaction); redo.Clear();
        if (undo.Count > depth) undo.RemoveRange(0, undo.Count - depth);
    }
    public GridEditTransaction? TakeUndo()
    {
        if (!CanUndo) return null;
        var value = undo[^1]; undo.RemoveAt(undo.Count - 1); redo.Add(value); return value;
    }
    public GridEditTransaction? TakeRedo()
    {
        if (!CanRedo) return null;
        var value = redo[^1]; redo.RemoveAt(redo.Count - 1); undo.Add(value); return value;
    }
    public void RestoreUndoFailure(GridEditTransaction transaction) { redo.Remove(transaction); undo.Add(transaction); }
    public void RestoreRedoFailure(GridEditTransaction transaction) { undo.Remove(transaction); redo.Add(transaction); }
    public void Clear() { undo.Clear(); redo.Clear(); }
}
