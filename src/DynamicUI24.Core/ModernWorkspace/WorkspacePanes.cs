using System.Collections.Immutable;
using System.Collections.Concurrent;
using DynamicUI24.Core.Authoring;

namespace DynamicUI24.Core.ModernWorkspace;

public enum PaneRole { PrimaryContent, LeftNavigation, RightContext, SecondaryContent, BottomActivity }
public enum PaneOverlayState { None, Overlay, Pinned }

public readonly record struct WorkspaceCode
{
    public WorkspaceCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct PaneCode
{
    public PaneCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
    public override string ToString() => Value;
}

public sealed record PaneDefinition
{
    public PaneDefinition(PaneCode paneCode, PaneRole role, bool defaultVisibility = true, double defaultSize = 320,
        double minSize = 160, double maxSize = 640, bool canCollapse = true, bool canResize = true,
        bool canOverlay = false, bool canRememberState = true, string? helpContextCode = null,
        UiAuthorizationBinding? authorizationRequirement = null, int presentationPriority = 0)
    {
        if (!double.IsFinite(minSize) || !double.IsFinite(maxSize) || minSize < 0 || maxSize < minSize)
            throw new ArgumentOutOfRangeException(nameof(minSize));
        PaneCode = paneCode; Role = role; DefaultVisibility = defaultVisibility;
        MinSize = minSize; MaxSize = maxSize; DefaultSize = Math.Clamp(defaultSize, minSize, maxSize);
        CanCollapse = canCollapse; CanResize = canResize; CanOverlay = canOverlay;
        CanRememberState = canRememberState; HelpContextCode = Clean(helpContextCode);
        AuthorizationRequirement = authorizationRequirement; PresentationPriority = presentationPriority;
    }
    public PaneCode PaneCode { get; }
    public PaneRole Role { get; }
    public bool DefaultVisibility { get; }
    public double DefaultSize { get; }
    public double MinSize { get; }
    public double MaxSize { get; }
    public bool CanCollapse { get; }
    public bool CanResize { get; }
    public bool CanOverlay { get; }
    public bool CanRememberState { get; }
    public string? HelpContextCode { get; }
    public UiAuthorizationBinding? AuthorizationRequirement { get; }
    public int PresentationPriority { get; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}

public sealed record PaneRuntimeState(PaneCode PaneCode, bool Visible, bool Collapsed, double CurrentSize,
    PaneOverlayState OverlayState = PaneOverlayState.None, PaneCode? SelectedSecondaryContent = null);
public sealed record PanePreference(PaneCode PaneCode, bool? Collapsed = null, double? CurrentSize = null,
    double? SplitterRatio = null, PaneCode? SelectedSecondaryContent = null);

public readonly record struct WorkspacePaneKey(WorkspaceCode WorkspaceCode, PaneCode PaneCode);

/// <summary>
/// Session-scoped presentation state. It contains no controls and is intentionally independent of workspace views.
/// Applications may hydrate the same safe preference shape from durable preference storage.
/// </summary>
public sealed class WorkspacePaneSessionStateStore
{
    private readonly ConcurrentDictionary<WorkspacePaneKey, PanePreference> preferences = new();

    public PaneRuntimeState Resolve(WorkspaceCode workspaceCode, PaneDefinition definition,
        UiAuthorizationState authorization, bool capabilityAvailable,
        IReadOnlySet<PaneCode>? availableSecondaryPanes = null) =>
        PaneStateResolver.Resolve(definition, GetPreference(workspaceCode, definition.PaneCode),
            authorization, capabilityAvailable, availableSecondaryPanes);

    public PanePreference? GetPreference(WorkspaceCode workspaceCode, PaneCode paneCode) =>
        preferences.GetValueOrDefault(new(workspaceCode, paneCode));

    public PaneRuntimeState SetCollapsed(WorkspaceCode workspaceCode, PaneDefinition definition, bool collapsed,
        UiAuthorizationState authorization, bool capabilityAvailable,
        IReadOnlySet<PaneCode>? availableSecondaryPanes = null)
    {
        var key = new WorkspacePaneKey(workspaceCode, definition.PaneCode);
        if (definition.CanRememberState && definition.CanCollapse)
            preferences.AddOrUpdate(key, new PanePreference(definition.PaneCode, collapsed),
                (_, current) => current with { Collapsed = collapsed });
        return Resolve(workspaceCode, definition, authorization, capabilityAvailable, availableSecondaryPanes);
    }

    public void SetPreference(WorkspaceCode workspaceCode, PanePreference preference) =>
        preferences[new(workspaceCode, preference.PaneCode)] = preference;
}

public static class PaneStateResolver
{
    public static PaneRuntimeState Resolve(PaneDefinition definition, PanePreference? preference,
        UiAuthorizationState authorization, bool capabilityAvailable, IReadOnlySet<PaneCode>? availableSecondaryPanes = null)
    {
        var allowed = capabilityAvailable && authorization != UiAuthorizationState.Hidden;
        var use = allowed && definition.CanRememberState ? preference : null;
        var collapsed = definition.CanCollapse && use?.Collapsed == true;
        PaneCode? selected = use?.SelectedSecondaryContent is { } candidate && availableSecondaryPanes?.Contains(candidate) == true
            ? candidate : null;
        return new(definition.PaneCode, allowed && definition.DefaultVisibility, collapsed,
            Math.Clamp(use?.CurrentSize ?? definition.DefaultSize, definition.MinSize, definition.MaxSize),
            PaneOverlayState.None, selected);
    }
}

public sealed class LazyPaneContent<T>(Func<T> factory) where T : class
{
    private readonly Lazy<T> content = new(factory ?? throw new ArgumentNullException(nameof(factory)), true);
    public bool IsCreated => content.IsValueCreated;
    public T GetOrCreate() => content.Value;
}
