using System.Collections.Immutable;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Notifications;

public enum NotificationSeverity { Info, Success, Warning, Error, Critical }
public enum NotificationPresentationKind { Toast, Banner, AlertCard, BlockingNotice, NotificationCenterItem }
public enum NotificationSurface { NotificationCenter, TopActionBar, BottomActionBar, Banner, AlertCard, Toast, BlockingNotice }
public enum NotificationDisplayMode { IconOnly, Compact, Standard, Detailed }
public enum NotificationLifecycleState { New, Active, Acknowledged, Dismissed, Resolved, Expired }
public enum GuidanceActionType { Navigate, Command, OpenMenu, Dismiss }
public enum NotificationCompanyScope { Global, CompanyScoped }
public enum NotificationWorkspaceScope { Application, Workspace }

public sealed record NotificationProgress
{
    public NotificationProgress(double currentValue, double maximumValue, LocalizationKey? displayTextKey = null,
        bool isIndeterminate = false)
    {
        IsIndeterminate = isIndeterminate;
        DisplayTextKey = displayTextKey;
        MaximumValue = double.IsFinite(maximumValue) && maximumValue > 0 ? maximumValue : 1;
        CurrentValue = double.IsFinite(currentValue) ? Math.Clamp(currentValue, 0, MaximumValue) : 0;
        WasNormalized = !double.IsFinite(currentValue) || !double.IsFinite(maximumValue) || maximumValue <= 0 ||
            currentValue < 0 || currentValue > maximumValue;
    }

    public double CurrentValue { get; }
    public double MaximumValue { get; }
    public LocalizationKey? DisplayTextKey { get; }
    public bool IsIndeterminate { get; }
    public bool WasNormalized { get; }
    public double Percentage => IsIndeterminate ? 0 : CurrentValue / MaximumValue * 100;
}

public sealed record NotificationSurfaceDefinition
{
    public NotificationSurfaceDefinition(NotificationSurface surface,
        NotificationDisplayMode displayMode = NotificationDisplayMode.Standard, bool showIcon = true,
        bool showTitle = true, bool showMessage = true, bool showProgress = true,
        bool showPrimaryAction = true, bool showSecondaryActions = true, int displayOrder = 0)
    {
        Surface = surface;
        DisplayMode = displayMode;
        ShowIcon = showIcon;
        ShowTitle = showTitle;
        ShowMessage = showMessage;
        ShowProgress = showProgress;
        ShowPrimaryAction = showPrimaryAction;
        ShowSecondaryActions = showSecondaryActions;
        DisplayOrder = displayOrder;
    }

    public NotificationSurface Surface { get; }
    public NotificationDisplayMode DisplayMode { get; }
    public bool ShowIcon { get; }
    public bool ShowTitle { get; }
    public bool ShowMessage { get; }
    public bool ShowProgress { get; }
    public bool ShowPrimaryAction { get; }
    public bool ShowSecondaryActions { get; }
    public int DisplayOrder { get; }
}

public sealed record FocusTarget
{
    public FocusTarget(string focusTargetCode, string? fieldCode = null, string? sectionCode = null,
        string? controlKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(focusTargetCode);
        FocusTargetCode = focusTargetCode.Trim().ToUpperInvariant();
        FieldCode = Normalize(fieldCode); SectionCode = Normalize(sectionCode); ControlKey = Normalize(controlKey);
    }
    public string FocusTargetCode { get; }
    public string? FieldCode { get; }
    public string? SectionCode { get; }
    public string? ControlKey { get; }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record GuidanceAction
{
    public GuidanceAction(string actionCode, LocalizationKey displayNameKey, GuidanceActionType actionType,
        IconKey? iconKey = null, string? workspaceId = null, string? registeredCommandCode = null,
        FocusTarget? focusTarget = null, PresentationRequirement? requirement = null,
        IEnumerable<ActionMenuItemDefinition>? menuItems = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionCode);
        ActionCode = actionCode.Trim().ToUpperInvariant();
        DisplayNameKey = displayNameKey;
        ActionType = actionType;
        IconKey = iconKey;
        WorkspaceId = Clean(workspaceId);
        RegisteredCommandCode = Clean(registeredCommandCode)?.ToUpperInvariant();
        FocusTarget = focusTarget;
        Requirement = requirement;
        MenuItems = (menuItems ?? []).ToImmutableArray();
    }
    public string ActionCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public GuidanceActionType ActionType { get; }
    public IconKey? IconKey { get; }
    public string? WorkspaceId { get; }
    public string? RegisteredCommandCode { get; }
    public FocusTarget? FocusTarget { get; }
    public PresentationRequirement? Requirement { get; }
    public ImmutableArray<ActionMenuItemDefinition> MenuItems { get; }
    public bool IsWellFormed => ActionType switch
    {
        GuidanceActionType.Navigate => WorkspaceId is not null,
        GuidanceActionType.Command => RegisteredCommandCode is not null,
        GuidanceActionType.OpenMenu => MenuItems.Length > 0,
        GuidanceActionType.Dismiss => true,
        _ => false,
    };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record NotificationDefinition
{
    public NotificationDefinition(string notificationCode, NotificationSeverity severity,
        NotificationPresentationKind presentationKind, LocalizationKey titleKey, LocalizationKey messageKey,
        IconKey? iconKey = null, bool autoShow = false, bool dismissible = true, int priority = 0,
        string? deduplicationKey = null, DateTimeOffset? expiration = null,
        NotificationCompanyScope companyScope = NotificationCompanyScope.Global,
        NotificationWorkspaceScope workspaceScope = NotificationWorkspaceScope.Application,
        string? workspaceId = null, PresentationRequirement? requirement = null,
        GuidanceAction? primaryAction = null, IEnumerable<GuidanceAction>? secondaryActions = null,
        NotificationProgress? progress = null, IEnumerable<NotificationSurfaceDefinition>? surfaces = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationCode);
        NotificationCode = notificationCode.Trim().ToUpperInvariant();
        Severity = severity; PresentationKind = presentationKind; TitleKey = titleKey; MessageKey = messageKey;
        IconKey = iconKey; AutoShow = autoShow; Dismissible = dismissible; Priority = priority;
        DeduplicationKey = string.IsNullOrWhiteSpace(deduplicationKey) ? null : deduplicationKey.Trim().ToUpperInvariant();
        Expiration = expiration; CompanyScope = companyScope; WorkspaceScope = workspaceScope;
        WorkspaceId = string.IsNullOrWhiteSpace(workspaceId) ? null : workspaceId.Trim(); Requirement = requirement;
        PrimaryAction = primaryAction; SecondaryActions = (secondaryActions ?? []).ToImmutableArray(); Progress = progress;
        Surfaces = (surfaces ?? [DefaultSurface(presentationKind),
            new(NotificationSurface.NotificationCenter, NotificationDisplayMode.Detailed, displayOrder: 100)])
            .GroupBy(x => x.Surface).Select(x => x.OrderBy(y => y.DisplayOrder).First())
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Surface).ToImmutableArray();
    }

    public string NotificationCode { get; }
    public NotificationSeverity Severity { get; }
    public NotificationPresentationKind PresentationKind { get; }
    public LocalizationKey TitleKey { get; }
    public LocalizationKey MessageKey { get; }
    public IconKey? IconKey { get; }
    public bool AutoShow { get; }
    public bool Dismissible { get; }
    public int Priority { get; }
    public string? DeduplicationKey { get; }
    public DateTimeOffset? Expiration { get; }
    public NotificationCompanyScope CompanyScope { get; }
    public NotificationWorkspaceScope WorkspaceScope { get; }
    public string? WorkspaceId { get; }
    public PresentationRequirement? Requirement { get; }
    public GuidanceAction? PrimaryAction { get; }
    public ImmutableArray<GuidanceAction> SecondaryActions { get; }
    public NotificationProgress? Progress { get; }
    public ImmutableArray<NotificationSurfaceDefinition> Surfaces { get; }

    private static NotificationSurfaceDefinition DefaultSurface(NotificationPresentationKind kind) => new(kind switch
    {
        NotificationPresentationKind.Toast => NotificationSurface.Toast,
        NotificationPresentationKind.Banner => NotificationSurface.Banner,
        NotificationPresentationKind.AlertCard => NotificationSurface.AlertCard,
        NotificationPresentationKind.BlockingNotice => NotificationSurface.BlockingNotice,
        _ => NotificationSurface.NotificationCenter,
    });
}

public sealed record NotificationInstance
{
    public NotificationInstance(string instanceId, NotificationDefinition definition, DateTimeOffset createdAt,
        DateTimeOffset? updatedAt = null, NotificationLifecycleState lifecycleState = NotificationLifecycleState.New,
        bool isUnread = true, bool requiresAttention = false, NotificationProgress? currentProgress = null,
        CompanyId? companyContext = null, string? workspaceContext = null,
        DateTimeOffset? dismissedAt = null, DateTimeOffset? resolvedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        InstanceId = instanceId.Trim(); Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CreatedAt = createdAt; UpdatedAt = updatedAt ?? createdAt; LifecycleState = lifecycleState;
        IsUnread = isUnread; RequiresAttention = requiresAttention; CurrentProgress = currentProgress ?? definition.Progress;
        CompanyContext = companyContext; WorkspaceContext = Clean(workspaceContext);
        DismissedAt = dismissedAt; ResolvedAt = resolvedAt;
    }
    public string InstanceId { get; init; }
    public NotificationDefinition Definition { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public NotificationLifecycleState LifecycleState { get; init; }
    public bool IsUnread { get; init; }
    public bool RequiresAttention { get; init; }
    public NotificationProgress? CurrentProgress { get; init; }
    public CompanyId? CompanyContext { get; init; }
    public string? WorkspaceContext { get; init; }
    public DateTimeOffset? DismissedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
