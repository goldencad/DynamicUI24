using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;

namespace DynamicUI24.Core.Notifications;

public interface INotificationClock { DateTimeOffset UtcNow { get; } }
public sealed class SystemNotificationClock : INotificationClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

public sealed record NotificationProviderContext(CompanyDescriptor CurrentCompany, string? CurrentWorkspaceId,
    EffectiveAuthorizationContext? Authorization, DateTimeOffset Now);

public interface INotificationProvider
{
    string ProviderCode { get; }
    Task<IReadOnlyList<NotificationInstance>> GetNotificationsAsync(NotificationProviderContext context,
        CancellationToken cancellationToken = default);
}

public sealed record NotificationDiagnostic(string Code, string? ProviderCode = null, string? NotificationCode = null);
public sealed record ResolvedGuidanceAction(GuidanceAction Definition, AuthorizationPresentationState State)
{
    public bool IsEnabled => State == AuthorizationPresentationState.VisibleEnabled;
}
public sealed record ResolvedNotification(NotificationInstance Instance,
    ImmutableArray<NotificationSurfaceDefinition> Surfaces, ResolvedGuidanceAction? PrimaryAction,
    ImmutableArray<ResolvedGuidanceAction> SecondaryActions, bool ShouldAutoShow)
{
    public string LogicalId => Instance.InstanceId;
}
public sealed record NotificationCenterGroup(string Code, ImmutableArray<ResolvedNotification> Items);
public sealed record NotificationPresentationModel(ImmutableArray<ResolvedNotification> Notifications,
    ImmutableArray<NotificationCenterGroup> CenterGroups, int AttentionCount,
    ImmutableArray<NotificationDiagnostic> Diagnostics)
{
    public ImmutableArray<ResolvedNotification> ForSurface(NotificationSurface surface) => Notifications
        .Where(x => x.Surfaces.Any(y => y.Surface == surface)).ToImmutableArray();
}

public sealed class NotificationCoordinator
{
    private const int MaximumRetained = 200;
    private readonly ImmutableArray<INotificationProvider> providers;
    private readonly INotificationClock clock;
    private readonly TimeSpan autoShowCooldown;
    private readonly Dictionary<string, NotificationInstance> logicalState = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> lastAutoShow = new(StringComparer.OrdinalIgnoreCase);

    public NotificationCoordinator(IEnumerable<INotificationProvider> providers, INotificationClock? clock = null,
        TimeSpan? autoShowCooldown = null)
    {
        this.providers = (providers ?? throw new ArgumentNullException(nameof(providers)))
            .GroupBy(x => x.ProviderCode, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToImmutableArray();
        this.clock = clock ?? new SystemNotificationClock();
        this.autoShowCooldown = autoShowCooldown ?? TimeSpan.FromMinutes(5);
        Current = new([], [], 0, []);
    }

    public NotificationPresentationModel Current { get; private set; }
    public event EventHandler<NotificationPresentationModel>? Changed;

    public NotificationInstance? FindInstance(string instanceId) => logicalState.Values.FirstOrDefault(x =>
        x.InstanceId.Equals(instanceId, StringComparison.OrdinalIgnoreCase));

    public async Task<NotificationPresentationModel> RefreshAsync(CompanyDescriptor company, string? workspaceId,
        EffectiveAuthorizationContext? authorization, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var context = new NotificationProviderContext(company, workspaceId, authorization, now);
        var diagnostics = ImmutableArray.CreateBuilder<NotificationDiagnostic>();
        var emissions = new List<(string Provider, NotificationInstance Instance)>();
        foreach (var provider in providers.OrderBy(x => x.ProviderCode, StringComparer.Ordinal))
        {
            try
            {
                var values = await provider.GetNotificationsAsync(context, cancellationToken).ConfigureAwait(false) ?? [];
                var duplicateIds = values.GroupBy(x => x.InstanceId, StringComparer.OrdinalIgnoreCase)
                    .Where(x => x.Count() > 1).Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var duplicate in duplicateIds) diagnostics.Add(new("NOTIFICATION_DUPLICATE_INSTANCE_ID", provider.ProviderCode, duplicate));
                foreach (var duplicateCode in values.GroupBy(x => x.Definition.NotificationCode, StringComparer.OrdinalIgnoreCase)
                             .Where(x => x.Select(y => y.Definition.DeduplicationKey ?? y.InstanceId).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1))
                    diagnostics.Add(new("NOTIFICATION_DUPLICATE_CODE", provider.ProviderCode, duplicateCode.Key));
                foreach (var value in values) Validate(value, provider.ProviderCode, diagnostics);
                emissions.AddRange(values.Where(x => !duplicateIds.Contains(x.InstanceId)).Select(x => (provider.ProviderCode, x)));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { diagnostics.Add(new("NOTIFICATION_PROVIDER_FAILED", provider.ProviderCode)); }
        }

        var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in emissions.GroupBy(x => LogicalKey(x.Provider, x.Instance), StringComparer.OrdinalIgnoreCase)
                     .OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var emitted = group.OrderByDescending(x => x.Instance.UpdatedAt).ThenBy(x => x.Instance.InstanceId, StringComparer.Ordinal).First().Instance;
            if (group.Count() > 1) diagnostics.Add(new("NOTIFICATION_DEDUPLICATED", group.First().Provider, emitted.Definition.NotificationCode));
            activeKeys.Add(group.Key);
            if (emitted.Definition.Expiration is { } expiration && expiration <= now)
            {
                logicalState[group.Key] = emitted with { LifecycleState = NotificationLifecycleState.Expired, UpdatedAt = now };
                continue;
            }
            if (emitted.LifecycleState is NotificationLifecycleState.Resolved or NotificationLifecycleState.Expired)
            {
                logicalState[group.Key] = emitted with { UpdatedAt = now };
                continue;
            }
            if (logicalState.TryGetValue(group.Key, out var previous))
            {
                if (previous.LifecycleState == NotificationLifecycleState.Dismissed)
                    emitted = emitted with { LifecycleState = NotificationLifecycleState.Dismissed,
                        DismissedAt = previous.DismissedAt, IsUnread = previous.IsUnread };
                emitted = emitted with { InstanceId = previous.InstanceId, CreatedAt = previous.CreatedAt,
                    UpdatedAt = now, CurrentProgress = emitted.CurrentProgress ?? emitted.Definition.Progress };
            }
            else emitted = emitted with { LifecycleState = NotificationLifecycleState.Active, UpdatedAt = now };
            logicalState[group.Key] = emitted;
        }

        foreach (var missing in logicalState.Keys.Where(x => !activeKeys.Contains(x)).ToArray())
        {
            var state = logicalState[missing];
            if (state.LifecycleState is NotificationLifecycleState.Active or NotificationLifecycleState.New or NotificationLifecycleState.Acknowledged)
                logicalState[missing] = state with { LifecycleState = NotificationLifecycleState.Resolved, ResolvedAt = now, UpdatedAt = now };
        }
        TrimState();
        Current = Resolve(context, diagnostics.ToImmutable());
        Changed?.Invoke(this, Current);
        return Current;
    }

    public bool Dismiss(string instanceId)
    {
        var pair = logicalState.FirstOrDefault(x => x.Value.InstanceId.Equals(instanceId, StringComparison.OrdinalIgnoreCase));
        if (pair.Value is null || !pair.Value.Definition.Dismissible || !IsActive(pair.Value)) return false;
        logicalState[pair.Key] = pair.Value with { LifecycleState = NotificationLifecycleState.Dismissed,
            DismissedAt = clock.UtcNow, UpdatedAt = clock.UtcNow, IsUnread = false };
        RebuildFromCurrentContext(); return true;
    }

    public bool Resolve(string instanceId)
    {
        var pair = logicalState.FirstOrDefault(x => x.Value.InstanceId.Equals(instanceId, StringComparison.OrdinalIgnoreCase));
        if (pair.Value is null || pair.Value.LifecycleState is NotificationLifecycleState.Resolved or NotificationLifecycleState.Expired) return false;
        logicalState[pair.Key] = pair.Value with { LifecycleState = NotificationLifecycleState.Resolved,
            ResolvedAt = clock.UtcNow, UpdatedAt = clock.UtcNow, IsUnread = false };
        RebuildFromCurrentContext(); return true;
    }

    public bool Acknowledge(string instanceId)
    {
        var pair = logicalState.FirstOrDefault(x => x.Value.InstanceId.Equals(instanceId, StringComparison.OrdinalIgnoreCase));
        if (pair.Value is null || !IsActive(pair.Value)) return false;
        logicalState[pair.Key] = pair.Value with { LifecycleState = NotificationLifecycleState.Acknowledged, IsUnread = false, UpdatedAt = clock.UtcNow };
        RebuildFromCurrentContext(); return true;
    }

    private NotificationProviderContext? lastContext;
    private ImmutableArray<NotificationDiagnostic> lastDiagnostics = [];
    private NotificationPresentationModel Resolve(NotificationProviderContext context, ImmutableArray<NotificationDiagnostic> diagnostics)
    {
        lastContext = context; lastDiagnostics = diagnostics;
        var resolved = ImmutableArray.CreateBuilder<ResolvedNotification>();
        foreach (var instance in logicalState.Values.Where(IsActive))
        {
            var definition = instance.Definition;
            if (definition.CompanyScope == NotificationCompanyScope.CompanyScoped && instance.CompanyContext != context.CurrentCompany.CompanyId) continue;
            var authorizationState = definition.Requirement is null ? AuthorizationPresentationState.VisibleEnabled :
                AuthorizationPresentationResolver.Resolve(definition.Requirement, context.Authorization);
            if (authorizationState == AuthorizationPresentationState.Hidden) continue;
            var surfaces = definition.Surfaces.Where(surface =>
                surface.Surface == NotificationSurface.NotificationCenter || definition.WorkspaceScope == NotificationWorkspaceScope.Application ||
                StringComparer.OrdinalIgnoreCase.Equals(instance.WorkspaceContext ?? definition.WorkspaceId, context.CurrentWorkspaceId)).ToImmutableArray();
            if (surfaces.Length == 0) continue;
            var primary = ResolveAction(definition.PrimaryAction, context.Authorization, authorizationState);
            var secondary = definition.SecondaryActions.Select(x => ResolveAction(x, context.Authorization, authorizationState))
                .Where(x => x is not null).Cast<ResolvedGuidanceAction>().ToImmutableArray();
            var shouldShow = definition.AutoShow && instance.LifecycleState != NotificationLifecycleState.Dismissed &&
                (!lastAutoShow.TryGetValue(instance.InstanceId, out var shown) || context.Now - shown >= autoShowCooldown);
            if (shouldShow) lastAutoShow[instance.InstanceId] = context.Now;
            resolved.Add(new(instance, surfaces, primary, secondary, shouldShow));
        }
        var ordered = resolved.OrderByDescending(x => x.Instance.Definition.Priority)
            .ThenByDescending(x => x.Instance.UpdatedAt).ThenBy(x => x.Instance.InstanceId, StringComparer.Ordinal).ToImmutableArray();
        var attention = ordered.Where(x => x.Instance.RequiresAttention || x.Instance.Definition.Severity >= NotificationSeverity.Warning).ToImmutableArray();
        var recent = ordered.Except(attention).ToImmutableArray();
        var groups = new[] { new NotificationCenterGroup("NEEDS_ATTENTION", attention), new NotificationCenterGroup("RECENT", recent) }
            .Where(x => x.Items.Length > 0).ToImmutableArray();
        return new(ordered, groups, ordered.Count(x => x.Instance.IsUnread || x.Instance.RequiresAttention), diagnostics);
    }

    private static ResolvedGuidanceAction? ResolveAction(GuidanceAction? action, EffectiveAuthorizationContext? authorization,
        AuthorizationPresentationState notificationState)
    {
        if (action is null || !action.IsWellFormed) return null;
        var state = action.Requirement is null ? notificationState : AuthorizationPresentationResolver.Resolve(action.Requirement, authorization);
        return state == AuthorizationPresentationState.Hidden ? null : new(action, state);
    }
    private void RebuildFromCurrentContext()
    {
        if (lastContext is null) return;
        Current = Resolve(lastContext with { Now = clock.UtcNow }, lastDiagnostics);
        Changed?.Invoke(this, Current);
    }
    private static string LogicalKey(string provider, NotificationInstance instance) => provider + ":" +
        (instance.Definition.DeduplicationKey ?? instance.Definition.NotificationCode + ":" + instance.InstanceId);
    private static bool IsActive(NotificationInstance x) => x.LifecycleState is NotificationLifecycleState.New or
        NotificationLifecycleState.Active or NotificationLifecycleState.Acknowledged;
    private void TrimState()
    {
        foreach (var key in logicalState.OrderByDescending(x => IsActive(x.Value)).ThenByDescending(x => x.Value.UpdatedAt)
                     .Skip(MaximumRetained).Select(x => x.Key).ToArray()) logicalState.Remove(key);
    }

    private static void Validate(NotificationInstance instance, string provider,
        ImmutableArray<NotificationDiagnostic>.Builder diagnostics)
    {
        var definition = instance.Definition;
        if (!Enum.IsDefined(definition.PresentationKind))
            diagnostics.Add(new("NOTIFICATION_PRESENTATION_UNKNOWN", provider, definition.NotificationCode));
        if (!Enum.IsDefined(definition.Severity))
            diagnostics.Add(new("NOTIFICATION_SEVERITY_UNKNOWN", provider, definition.NotificationCode));
        if (definition.Surfaces.Any(x => !Enum.IsDefined(x.Surface)))
            diagnostics.Add(new("NOTIFICATION_SURFACE_UNKNOWN", provider, definition.NotificationCode));
        if (definition.PrimaryAction is { IsWellFormed: false } || definition.SecondaryActions.Any(x => !x.IsWellFormed))
            diagnostics.Add(new("NOTIFICATION_ACTION_MALFORMED", provider, definition.NotificationCode));
        if (definition.WorkspaceScope == NotificationWorkspaceScope.Workspace &&
            string.IsNullOrWhiteSpace(instance.WorkspaceContext ?? definition.WorkspaceId))
            diagnostics.Add(new("NOTIFICATION_WORKSPACE_CONTEXT_MISSING", provider, definition.NotificationCode));
    }
}
