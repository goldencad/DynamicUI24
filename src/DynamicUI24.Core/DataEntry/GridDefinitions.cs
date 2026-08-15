using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.DataEntry;

public enum GridSelectionMode { None, Single, Multiple }
public enum GridSortDirection { Ascending, Descending }
public enum GridFilterOperator { Equals, NotEquals, Contains, StartsWith, GreaterThan, LessThan, IsEmpty, IsNotEmpty }

public sealed record GridSortDefinition(VariableCode VariableCode, GridSortDirection Direction, int Priority = 0);
public sealed record GridFilterDefinition(VariableCode VariableCode, GridFilterOperator Operator, object? Value = null);

/// <summary>Generic grid metadata. Column metadata is the Task 9 contract and is not duplicated here.</summary>
public sealed record GridDefinition
{
    public GridDefinition(string gridId, string gridCode, IEnumerable<ColumnDefinition> columns,
        LocalizationKey? displayNameKey = null, IEnumerable<GridSortDefinition>? defaultSort = null,
        IEnumerable<GridFilterDefinition>? defaultFilter = null, GridSelectionMode selectionMode = GridSelectionMode.Single,
        bool allowEdit = false, bool allowAdd = false, bool allowDelete = false, bool showRowNumbers = true,
        bool showStatusBar = true, LocalizationKey? emptyStateKey = null,
        PresentationRequirement? permissionRequirement = null, PresentationRequirement? capabilityRequirement = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gridId);
        ArgumentException.ThrowIfNullOrWhiteSpace(gridCode);
        ArgumentNullException.ThrowIfNull(columns);
        GridId = gridId.Trim();
        GridCode = gridCode.Trim().ToUpperInvariant();
        DisplayNameKey = displayNameKey;
        Columns = columns.ToImmutableArray();
        DefaultSort = (defaultSort ?? []).OrderBy(x => x.Priority).ToImmutableArray();
        DefaultFilter = (defaultFilter ?? []).ToImmutableArray();
        SelectionMode = selectionMode;
        AllowEdit = allowEdit; AllowAdd = allowAdd; AllowDelete = allowDelete;
        ShowRowNumbers = showRowNumbers; ShowStatusBar = showStatusBar;
        EmptyStateKey = emptyStateKey ?? new("Grid.State.Empty");
        PermissionRequirement = permissionRequirement; CapabilityRequirement = capabilityRequirement;
    }

    public string GridId { get; }
    public string GridCode { get; }
    public LocalizationKey? DisplayNameKey { get; }
    public ImmutableArray<ColumnDefinition> Columns { get; }
    public ImmutableArray<GridSortDefinition> DefaultSort { get; }
    public ImmutableArray<GridFilterDefinition> DefaultFilter { get; }
    public GridSelectionMode SelectionMode { get; }
    public bool AllowEdit { get; }
    public bool AllowAdd { get; }
    public bool AllowDelete { get; }
    public bool ShowRowNumbers { get; }
    public bool ShowStatusBar { get; }
    public LocalizationKey EmptyStateKey { get; }
    public PresentationRequirement? PermissionRequirement { get; }
    public PresentationRequirement? CapabilityRequirement { get; }
}

public sealed record GridDiagnostic(string Code, string? MetadataId = null);

public sealed record ResolvedGridColumn(ColumnDefinition Definition, AuthorizationPresentationState State,
    decimal Width, decimal MinWidth, decimal MaxWidth)
{
    public bool IsVisible => State != AuthorizationPresentationState.Hidden && Definition.IsVisible;
    public bool CanEdit => State == AuthorizationPresentationState.VisibleEnabled &&
        Definition.Mode == ColumnMode.Input && Definition.EditorKind is not ColumnEditorKind.ReadOnly and not ColumnEditorKind.Formula;
}

public sealed record ResolvedGridDefinition(GridDefinition Definition, AuthorizationPresentationState State,
    ImmutableArray<ResolvedGridColumn> Columns, ImmutableArray<GridDiagnostic> Diagnostics)
{
    public bool CanEdit => Definition.AllowEdit && State == AuthorizationPresentationState.VisibleEnabled;
}

public static class GridMetadataResolver
{
    public static ResolvedGridDefinition Resolve(GridDefinition definition, EffectiveAuthorizationContext? authorization)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var diagnostics = ImmutableArray.CreateBuilder<GridDiagnostic>();
        var excluded = new HashSet<ColumnDefinition>();
        ExcludeDuplicates(definition.Columns, x => x.ColumnId, "GRID_DUPLICATE_COLUMN_ID", excluded, diagnostics);
        ExcludeDuplicates(definition.Columns, x => x.ColumnCode, "GRID_DUPLICATE_COLUMN_CODE", excluded, diagnostics);
        ExcludeDuplicates(definition.Columns, x => x.VariableCode.Value, "GRID_DUPLICATE_VARIABLE_CODE", excluded, diagnostics);

        var state = ResolveRequirements(definition.PermissionRequirement, definition.CapabilityRequirement, authorization);
        var columns = ImmutableArray.CreateBuilder<ResolvedGridColumn>();
        foreach (var column in definition.Columns.OrderBy(x => x.DisplayOrder).ThenBy(x => x.ColumnCode, StringComparer.Ordinal))
        {
            if (excluded.Contains(column)) continue;
            if (string.IsNullOrWhiteSpace(column.DisplayNameKey))
                diagnostics.Add(new("GRID_COLUMN_LABEL_MISSING", column.ColumnId));
            if (!Enum.IsDefined(column.DataType))
                diagnostics.Add(new("GRID_COLUMN_DATA_TYPE_UNKNOWN", column.ColumnId));
            if (!Enum.IsDefined(column.EditorKind))
                diagnostics.Add(new("GRID_COLUMN_EDITOR_UNKNOWN", column.ColumnId));
            if (column.Mode is ColumnMode.Formula or ColumnMode.System && column.EditorKind is not ColumnEditorKind.ReadOnly and not ColumnEditorKind.Formula)
                diagnostics.Add(new("GRID_COLUMN_MODE_FORCED_READ_ONLY", column.ColumnId));

            var min = column.MinWidth is > 0 ? column.MinWidth.Value : 64m;
            var max = column.MaxWidth is > 0 ? column.MaxWidth.Value : 640m;
            if (min > max) { diagnostics.Add(new("GRID_COLUMN_WIDTH_INVALID", column.ColumnId)); (min, max) = (64m, 640m); }
            var width = Math.Clamp(column.Width is > 0 ? column.Width.Value : 140m, min, max);
            var columnState = ParseColumnRequirement(column.PermissionRequirement, authorization);
            if (column.Mode is ColumnMode.Formula or ColumnMode.System)
                columnState = Combine(columnState, AuthorizationPresentationState.VisibleReadOnly);
            columns.Add(new(column, columnState, width, min, max));
        }
        return new(definition, state, columns.ToImmutable(), diagnostics.ToImmutable());
    }

    private static void ExcludeDuplicates(IEnumerable<ColumnDefinition> columns, Func<ColumnDefinition, string> key,
        string code, HashSet<ColumnDefinition> excluded, ImmutableArray<GridDiagnostic>.Builder diagnostics)
    {
        foreach (var group in columns.GroupBy(key, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
            foreach (var column in group) { excluded.Add(column); diagnostics.Add(new(code, column.ColumnId)); }
    }

    private static AuthorizationPresentationState ParseColumnRequirement(string? value, EffectiveAuthorizationContext? context)
    {
        if (string.IsNullOrWhiteSpace(value)) return AuthorizationPresentationState.VisibleEnabled;
        var text = value.Trim();
        var requirement = text.StartsWith("CAPABILITY:", StringComparison.OrdinalIgnoreCase)
            ? new PresentationRequirement(CapabilityCode: new(text[11..]), UnauthorizedBehavior: UnauthorizedBehavior.Hide)
            : new PresentationRequirement(new PermissionCode(text.StartsWith("PERMISSION:", StringComparison.OrdinalIgnoreCase) ? text[11..] : text),
                UnauthorizedBehavior: UnauthorizedBehavior.Hide);
        return AuthorizationPresentationResolver.Resolve(requirement, context);
    }

    private static AuthorizationPresentationState ResolveRequirements(PresentationRequirement? permission,
        PresentationRequirement? capability, EffectiveAuthorizationContext? context)
    {
        var state = AuthorizationPresentationState.VisibleEnabled;
        if (permission is not null) state = Combine(state, AuthorizationPresentationResolver.Resolve(permission, context));
        if (capability is not null) state = Combine(state, AuthorizationPresentationResolver.Resolve(capability, context));
        return state;
    }

    private static AuthorizationPresentationState Combine(AuthorizationPresentationState left, AuthorizationPresentationState right) =>
        (AuthorizationPresentationState)Math.Max((int)left, (int)right);
}
