using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Notifications;

public enum GuidanceActionResultStatus { Success, Unavailable, Denied, PartialSuccess, Failed }
public sealed record FocusRequestResult(bool IsSuccess, string? DiagnosticCode = null)
{
    public static FocusRequestResult NotFound() => new(false, "FOCUS_TARGET_NOT_FOUND");
}
public sealed record GuidanceActionResult(GuidanceActionResultStatus Status, string? DiagnosticCode = null,
    FocusRequestResult? FocusResult = null);

public interface IFocusTargetService
{
    Task<FocusRequestResult> RequestFocusAsync(FocusTarget target, CancellationToken cancellationToken = default);
}
public interface INotificationMenuService
{
    Task<bool> OpenAsync(GuidanceAction action, CancellationToken cancellationToken = default);
}

public sealed class NotificationActionDispatcher(IWorkspaceNavigationService navigation, IActionCommandRegistry commands,
    Func<ActionCommandExecutionContext> commandContext, IFocusTargetService? focus = null,
    INotificationMenuService? menus = null)
{
    public async Task<GuidanceActionResult> DispatchAsync(ResolvedGuidanceAction action,
        Func<bool>? dismiss = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!action.IsEnabled) return new(GuidanceActionResultStatus.Denied, "NOTIFICATION_ACTION_DENIED");
        try
        {
            return action.Definition.ActionType switch
            {
                GuidanceActionType.Navigate => await NavigateAsync(action.Definition, cancellationToken).ConfigureAwait(false),
                GuidanceActionType.Command => FromCommand(await commands.ExecuteAsync(action.Definition.RegisteredCommandCode!, commandContext(), cancellationToken).ConfigureAwait(false)),
                GuidanceActionType.OpenMenu => menus is not null && await menus.OpenAsync(action.Definition, cancellationToken).ConfigureAwait(false)
                    ? new(GuidanceActionResultStatus.Success) : new(GuidanceActionResultStatus.Unavailable, "NOTIFICATION_MENU_UNAVAILABLE"),
                GuidanceActionType.Dismiss => dismiss?.Invoke() == true ? new(GuidanceActionResultStatus.Success)
                    : new(GuidanceActionResultStatus.Unavailable, "NOTIFICATION_DISMISS_UNAVAILABLE"),
                _ => new(GuidanceActionResultStatus.Unavailable, "NOTIFICATION_ACTION_UNKNOWN"),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(GuidanceActionResultStatus.Failed, "NOTIFICATION_ACTION_FAILED"); }
    }

    private async Task<GuidanceActionResult> NavigateAsync(GuidanceAction action, CancellationToken cancellationToken)
    {
        var result = await navigation.NavigateAsync(action.WorkspaceId!, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return new(GuidanceActionResultStatus.Unavailable, result.DiagnosticCode ?? "WORKSPACE_UNAVAILABLE");
        if (action.FocusTarget is null || focus is null) return new(GuidanceActionResultStatus.Success);
        var focusResult = await focus.RequestFocusAsync(action.FocusTarget, cancellationToken).ConfigureAwait(false);
        return focusResult.IsSuccess ? new(GuidanceActionResultStatus.Success, FocusResult: focusResult)
            : new(GuidanceActionResultStatus.PartialSuccess, focusResult.DiagnosticCode, focusResult);
    }
    private static GuidanceActionResult FromCommand(ActionCommandResult result) => result.Status switch
    {
        ActionCommandResultStatus.Success => new(GuidanceActionResultStatus.Success),
        ActionCommandResultStatus.Denied => new(GuidanceActionResultStatus.Denied, result.DiagnosticCode),
        ActionCommandResultStatus.Failed => new(GuidanceActionResultStatus.Failed, result.DiagnosticCode),
        _ => new(GuidanceActionResultStatus.Unavailable, result.DiagnosticCode),
    };
}

public sealed class NotificationActionBarAdapter
{
    public ActionBarDefinition Create(NotificationSurface surface, IEnumerable<ResolvedNotification> notifications)
    {
        if (surface is not (NotificationSurface.TopActionBar or NotificationSurface.BottomActionBar))
            throw new ArgumentOutOfRangeException(nameof(surface));
        var position = surface == NotificationSurface.TopActionBar ? ActionBarPosition.Top : ActionBarPosition.Bottom;
        var actions = notifications.SelectMany(notification => ToActions(notification, surface));
        return new($"notification-{position.ToString().ToLowerInvariant()}", $"NOTIFICATION_{position.ToString().ToUpperInvariant()}",
            position, actions);
    }

    private static IEnumerable<ActionDefinition> ToActions(ResolvedNotification notification, NotificationSurface surface)
    {
        var surfaceDefinition = notification.Surfaces.First(x => x.Surface == surface);
        if (notification.PrimaryAction is { } primary)
            yield return ToAction(notification, primary, surface, surfaceDefinition.ShowTitle
                ? notification.Instance.Definition.TitleKey : primary.Definition.DisplayNameKey);
        if (!surfaceDefinition.ShowSecondaryActions) yield break;
        foreach (var secondary in notification.SecondaryActions)
            yield return ToAction(notification, secondary, surface, secondary.Definition.DisplayNameKey);
    }

    private static ActionDefinition ToAction(ResolvedNotification notification, ResolvedGuidanceAction resolved,
        NotificationSurface surface, LocalizationKey displayNameKey)
    {
        var guidance = resolved.Definition;
        var actionType = guidance.ActionType switch
        {
            GuidanceActionType.Navigate => ActionType.Navigate,
            GuidanceActionType.Command => ActionType.CustomRegistered,
            _ => ActionType.CustomRegistered,
        };
        var definition = notification.Instance.Definition;
        var requirement = resolved.IsEnabled ? guidance.Requirement :
            new PresentationRequirement(new PermissionCode("NOTIFICATION.ACTION.ENABLED"),
                UnauthorizedBehavior: UnauthorizedBehavior.Disable);
        return new ActionDefinition($"notification-{notification.Instance.InstanceId}-{surface}-{guidance.ActionCode}", guidance.ActionCode,
            displayNameKey, definition.IconKey ?? SeverityIcon(definition.Severity), actionType,
            definition.Priority, requirement, targetWorkspaceId: guidance.WorkspaceId,
            registeredCommandCode: guidance.RegisteredCommandCode ?? $"NOTIFICATION.{guidance.ActionCode}");
    }
    public static IconKey SeverityIcon(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Success => StandardIconKeys.Success,
        NotificationSeverity.Warning or NotificationSeverity.Critical => StandardIconKeys.Warning,
        NotificationSeverity.Error => StandardIconKeys.Error,
        _ => StandardIconKeys.Info,
    };
}
