using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;

namespace DynamicUI24.Core.Editors;

/// <summary>Adapts editor metadata to the existing shared semantic command registry.</summary>
public sealed class EditorActionDispatcher(IActionCommandRegistry commands)
{
    public Task<ActionCommandResult> DispatchAsync(EditorActionDefinition action, EditorResolution resolution,
        ActionCommandExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(context);
        if (resolution.Status != EditorResolutionStatus.Resolved ||
            resolution.InteractionState != EditorInteractionState.Editable)
            return Task.FromResult(ActionCommandResult.Denied("EDITOR_ACTION_SUPPRESSED"));
        if (action.Requirement is { } requirement &&
            AuthorizationPresentationResolver.Resolve(requirement, context.ResolutionContext.Authorization) !=
                AuthorizationPresentationState.VisibleEnabled)
            return Task.FromResult(ActionCommandResult.Denied("EDITOR_ACTION_PERMISSION_DENIED"));
        return commands.ExecuteAsync(action.ActionCode, context, cancellationToken);
    }
}
