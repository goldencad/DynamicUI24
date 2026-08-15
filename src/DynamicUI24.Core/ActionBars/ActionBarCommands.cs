using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Navigation;

namespace DynamicUI24.Core.ActionBars;

public enum ActionCommandResultStatus { Success, Unavailable, Denied, Failed }

public sealed record ActionCommandResult(ActionCommandResultStatus Status, string? DiagnosticCode = null, string? Message = null)
{
    public static ActionCommandResult Success(string? message = null) => new(ActionCommandResultStatus.Success, Message: message);
    public static ActionCommandResult Unavailable(string code, string? message = null) => new(ActionCommandResultStatus.Unavailable, code, message);
    public static ActionCommandResult Denied(string code = "ACTION_COMMAND_DENIED", string? message = null) => new(ActionCommandResultStatus.Denied, code, message);
    public static ActionCommandResult Failed(string code = "ACTION_COMMAND_FAILED", string? message = null) => new(ActionCommandResultStatus.Failed, code, message);
}

public sealed record ActionCommandExecutionContext(ActionBarResolutionContext ResolutionContext);

public interface IActionRefreshService
{
    Task<ActionCommandResult> RefreshAsync(ActionCommandExecutionContext context, CancellationToken cancellationToken = default);
}

public interface IActionCommandRegistry
{
    bool Register(string commandCode, Func<ActionCommandExecutionContext, CancellationToken, Task<ActionCommandResult>> handler);
    Task<ActionCommandResult> ExecuteAsync(string commandCode, ActionCommandExecutionContext context, CancellationToken cancellationToken = default);
}

public sealed class ActionCommandRegistry : IActionCommandRegistry
{
    private readonly Dictionary<string, Func<ActionCommandExecutionContext, CancellationToken, Task<ActionCommandResult>>> handlers =
        new(StringComparer.OrdinalIgnoreCase);

    public bool Register(string commandCode, Func<ActionCommandExecutionContext, CancellationToken, Task<ActionCommandResult>> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandCode);
        ArgumentNullException.ThrowIfNull(handler);
        return handlers.TryAdd(commandCode.Trim(), handler);
    }

    public async Task<ActionCommandResult> ExecuteAsync(string commandCode, ActionCommandExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandCode) || !handlers.TryGetValue(commandCode.Trim(), out var handler))
            return ActionCommandResult.Unavailable("ACTION_COMMAND_UNKNOWN", $"Command '{commandCode}' is not registered.");
        try { return await handler(context, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) { return ActionCommandResult.Failed(message: ex.Message); }
    }
}

public sealed class ActionBarCommandDispatcher(
    IWorkspaceNavigationService navigation,
    IActionRefreshService refresh,
    IActionCommandRegistry commands)
{
    public async Task<ActionCommandResult> DispatchAsync(ResolvedAction action, ActionCommandExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(context);
        if (action.State != AuthorizationPresentationState.VisibleEnabled) return ActionCommandResult.Denied();
        var definition = action.Definition;
        return definition.CommandType switch
        {
            ActionType.Navigate => FromNavigation(await navigation.NavigateAsync(definition.TargetWorkspaceId!, cancellationToken).ConfigureAwait(false)),
            ActionType.Refresh => await refresh.RefreshAsync(context, cancellationToken).ConfigureAwait(false),
            ActionType.ApplicationCommand or ActionType.CustomRegistered =>
                await commands.ExecuteAsync(definition.RegisteredCommandCode!, context, cancellationToken).ConfigureAwait(false),
            _ => ActionCommandResult.Unavailable("ACTION_COMMAND_NOT_IMPLEMENTED"),
        };
    }

    public Task<ActionCommandResult> DispatchMenuItemAsync(ResolvedActionMenuItem item,
        ActionCommandExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        if (!item.IsEnabled) return Task.FromResult(ActionCommandResult.Denied());
        if (item.Definition.Kind == ActionMenuItemKind.Separator || item.Definition.Children.Length > 0)
            return Task.FromResult(ActionCommandResult.Unavailable("ACTION_MENU_ITEM_NOT_EXECUTABLE"));
        return commands.ExecuteAsync(item.Definition.RegisteredCommandCode!, context, cancellationToken);
    }

    private static ActionCommandResult FromNavigation(WorkspaceNavigationResult result) => result.IsSuccess
        ? ActionCommandResult.Success()
        : ActionCommandResult.Unavailable(result.DiagnosticCode ?? "WORKSPACE_UNAVAILABLE");
}
