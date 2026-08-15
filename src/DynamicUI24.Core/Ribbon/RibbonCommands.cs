using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Workspaces;

namespace DynamicUI24.Core.Ribbon;

public enum RibbonCommandResultStatus { Success, Unavailable, Denied, Failed }

public sealed record RibbonCommandResult(
    RibbonCommandResultStatus Status,
    string? DiagnosticCode = null,
    string? Message = null)
{
    public static RibbonCommandResult Success(string? message = null) => new(RibbonCommandResultStatus.Success, Message: message);
    public static RibbonCommandResult Unavailable(string code, string? message = null) => new(RibbonCommandResultStatus.Unavailable, code, message);
    public static RibbonCommandResult Denied(string code = "RIBBON_COMMAND_DENIED", string? message = null) => new(RibbonCommandResultStatus.Denied, code, message);
    public static RibbonCommandResult Failed(string code, string? message = null) => new(RibbonCommandResultStatus.Failed, code, message);
}

public sealed record RibbonCommandExecutionContext(
    RibbonResolutionContext ResolutionContext,
    WorkspaceDefinition? CurrentWorkspace = null);

public interface IRibbonNavigationService
{
    Task<RibbonCommandResult> NavigateAsync(string? workspaceId, Templates.TemplateCode? templateCode,
        CancellationToken cancellationToken = default);
}

public interface IRibbonRefreshService
{
    Task<RibbonCommandResult> RefreshAsync(RibbonCommandExecutionContext context,
        CancellationToken cancellationToken = default);
}

public interface IUiCommandRegistry
{
    bool Register(string commandCode,
        Func<RibbonCommandExecutionContext, CancellationToken, Task<RibbonCommandResult>> handler);
    Task<RibbonCommandResult> ExecuteAsync(string commandCode, RibbonCommandExecutionContext context,
        CancellationToken cancellationToken = default);
}

public sealed class UiCommandRegistry : IUiCommandRegistry
{
    private readonly Dictionary<string, Func<RibbonCommandExecutionContext, CancellationToken, Task<RibbonCommandResult>>> handlers =
        new(StringComparer.OrdinalIgnoreCase);

    public bool Register(string commandCode,
        Func<RibbonCommandExecutionContext, CancellationToken, Task<RibbonCommandResult>> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandCode);
        ArgumentNullException.ThrowIfNull(handler);
        return handlers.TryAdd(commandCode.Trim(), handler);
    }

    public async Task<RibbonCommandResult> ExecuteAsync(string commandCode, RibbonCommandExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandCode);
        ArgumentNullException.ThrowIfNull(context);
        if (!handlers.TryGetValue(commandCode.Trim(), out var handler))
            return RibbonCommandResult.Unavailable("RIBBON_COMMAND_UNKNOWN", $"Command '{commandCode}' is not registered.");
        try { return await handler(context, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) { return RibbonCommandResult.Failed("RIBBON_COMMAND_FAILED", ex.Message); }
    }
}

public sealed class RibbonCommandDispatcher(
    IRibbonNavigationService navigation,
    IRibbonRefreshService refresh,
    IUiCommandRegistry commands)
{
    public Task<RibbonCommandResult> DispatchAsync(ResolvedRibbonCommand command,
        RibbonCommandExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        if (command.State is AuthorizationPresentationState.Hidden or AuthorizationPresentationState.VisibleDisabled or AuthorizationPresentationState.VisibleReadOnly)
            return Task.FromResult(RibbonCommandResult.Denied());
        var definition = command.Definition;
        return definition.CommandType switch
        {
            RibbonCommandType.Navigate => navigation.NavigateAsync(definition.TargetWorkspaceId, definition.TargetTemplateCode, cancellationToken),
            RibbonCommandType.Refresh => refresh.RefreshAsync(context, cancellationToken),
            RibbonCommandType.CustomRegistered or RibbonCommandType.ApplicationCommand =>
                commands.ExecuteAsync(definition.RegisteredCommandCode!, context, cancellationToken),
            _ => Task.FromResult(RibbonCommandResult.Unavailable("RIBBON_COMMAND_NOT_IMPLEMENTED")),
        };
    }
}
