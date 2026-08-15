using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.ActionBars;

public sealed record ActionSelectionContext
{
    public ActionSelectionContext(int selectionCount)
    {
        if (selectionCount < 0) throw new ArgumentOutOfRangeException(nameof(selectionCount));
        SelectionCount = selectionCount;
    }

    public int SelectionCount { get; }
    public bool HasSelection => SelectionCount > 0;
}

/// <summary>Nullable values mean unavailable; zero remains a real, renderable value.</summary>
public sealed record ActionBarStatus(
    int? TotalRows = null,
    int? VisibleRows = null,
    int? SelectedRows = null,
    int? ErrorCount = null,
    int? WarningCount = null,
    int? PendingChangeCount = null,
    bool? ReadOnlyState = null,
    int? SelectedCells = null,
    int? SelectionRows = null,
    int? SelectionColumns = null);

public sealed record ActionBarResolutionContext(
    CompanyDescriptor Company,
    WorkspaceDefinition Workspace,
    TemplateCode TemplateCode,
    EffectiveAuthorizationContext? Authorization,
    ActionSelectionContext Selection,
    PresentationState PresentationState,
    ActionBarStatus? Status = null,
    IReadOnlyDictionary<string, bool>? ActionAvailability = null);

public sealed record ActionBarDiagnostic(string Code, string? ActionCode = null);

public sealed record ResolvedActionMenuItem(ActionMenuItemDefinition Definition, AuthorizationPresentationState State,
    ImmutableArray<ResolvedActionMenuItem> Children)
{
    public bool IsEnabled => State == AuthorizationPresentationState.VisibleEnabled;
}

public sealed record ResolvedAction(ActionDefinition Definition, AuthorizationPresentationState State,
    ImmutableArray<ResolvedActionMenuItem> MenuItems = default)
{
    public bool IsEnabled => State == AuthorizationPresentationState.VisibleEnabled;
    public bool IsReadOnly => State == AuthorizationPresentationState.VisibleReadOnly;
}

public sealed record ResolvedActionBar(
    ActionBarDefinition Definition,
    ImmutableArray<ResolvedAction> Actions,
    ActionBarStatus? Status,
    ImmutableArray<ActionBarDiagnostic> Diagnostics);

public sealed class DynamicActionBarResolver
{
    public ResolvedActionBar Resolve(ActionBarDefinition definition, ActionBarResolutionContext context,
        IEnumerable<WorkspaceDefinition>? knownWorkspaces = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);
        var workspaceIds = knownWorkspaces?.Select(x => x.WorkspaceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var diagnostics = ImmutableArray.CreateBuilder<ActionBarDiagnostic>();
        var actions = ImmutableArray.CreateBuilder<ResolvedAction>();

        if (!definition.IsVisible) return new(definition, [], definition.Position == ActionBarPosition.Bottom ? context.Status : null, []);
        foreach (var action in definition.Actions.Where(x => x.IsVisible))
        {
            if (!IsWellFormed(action, workspaceIds, diagnostics)) continue;
            var state = action.PermissionRequirement is null
                ? AuthorizationPresentationState.VisibleEnabled
                : AuthorizationPresentationResolver.Resolve(action.PermissionRequirement, context.Authorization);
            var count = context.Selection.SelectionCount;
            if ((action.RequiresSelection && count == 0) ||
                (action.MinSelection is { } min && count < min) ||
                (action.MaxSelection is { } max && count > max))
                state = Combine(state, AuthorizationPresentationState.VisibleDisabled);
            if (context.ActionAvailability?.TryGetValue(action.ActionCode, out var available) == true && !available)
                state = Combine(state, AuthorizationPresentationState.VisibleDisabled);
            if (context.PresentationState.Kind is PresentationStateKind.Loading or PresentationStateKind.Error or PresentationStateKind.Unavailable)
                state = Combine(state, AuthorizationPresentationState.VisibleDisabled);
            else if (context.PresentationState.Kind == PresentationStateKind.ReadOnly)
                state = Combine(state, AuthorizationPresentationState.VisibleReadOnly);
            if (state != AuthorizationPresentationState.Hidden) actions.Add(new(action, state,
                ResolveMenuItems(action.MenuItems, context.Authorization, diagnostics, action.ActionCode)));
        }

        return new(definition, actions.ToImmutable(), definition.Position == ActionBarPosition.Bottom ? context.Status : null,
            diagnostics.ToImmutable());
    }

    private static ImmutableArray<ResolvedActionMenuItem> ResolveMenuItems(
        IEnumerable<ActionMenuItemDefinition> items, EffectiveAuthorizationContext? authorization,
        ImmutableArray<ActionBarDiagnostic>.Builder diagnostics, string actionCode)
    {
        var result = ImmutableArray.CreateBuilder<ResolvedActionMenuItem>();
        foreach (var item in items.Where(x => x.IsVisible).OrderBy(x => x.DisplayOrder).ThenBy(x => x.ItemCode, StringComparer.Ordinal))
        {
            var state = item.PermissionRequirement is null ? AuthorizationPresentationState.VisibleEnabled :
                AuthorizationPresentationResolver.Resolve(item.PermissionRequirement, authorization);
            if (state == AuthorizationPresentationState.Hidden) continue;
            if (item.Kind == ActionMenuItemKind.Command && item.Children.Length == 0 && string.IsNullOrWhiteSpace(item.RegisteredCommandCode))
            {
                diagnostics.Add(new("ACTION_MENU_COMMAND_MISSING", $"{actionCode}.{item.ItemCode}"));
                state = AuthorizationPresentationState.VisibleDisabled;
            }
            result.Add(new(item, state, ResolveMenuItems(item.Children, authorization, diagnostics, actionCode)));
        }
        return result.ToImmutable();
    }

    private static bool IsWellFormed(ActionDefinition action, IReadOnlySet<string>? workspaceIds,
        ImmutableArray<ActionBarDiagnostic>.Builder diagnostics)
    {
        if (action.CommandType == ActionType.Navigate && string.IsNullOrWhiteSpace(action.TargetWorkspaceId))
            return Invalid("ACTION_NAVIGATE_TARGET_MISSING", action, diagnostics);
        if (action.CommandType == ActionType.Navigate && workspaceIds is not null && !workspaceIds.Contains(action.TargetWorkspaceId!))
            return Invalid("ACTION_UNKNOWN_WORKSPACE", action, diagnostics);
        if (action.CommandType is ActionType.ApplicationCommand or ActionType.CustomRegistered &&
            string.IsNullOrWhiteSpace(action.RegisteredCommandCode))
            return Invalid("ACTION_REGISTERED_COMMAND_MISSING", action, diagnostics);
        if (action.CommandType == ActionType.BatchAction && string.IsNullOrWhiteSpace(action.BatchActionCode))
            return Invalid("ACTION_BATCH_CODE_MISSING", action, diagnostics);
        return true;
    }

    private static bool Invalid(string code, ActionDefinition action, ImmutableArray<ActionBarDiagnostic>.Builder diagnostics)
    {
        diagnostics.Add(new(code, action.ActionCode));
        return false;
    }

    private static AuthorizationPresentationState Combine(AuthorizationPresentationState left,
        AuthorizationPresentationState right) => (AuthorizationPresentationState)Math.Max((int)left, (int)right);
}
