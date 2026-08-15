using DynamicUI24.Core.Workspaces;

namespace DynamicUI24.Core.Navigation;

public sealed record WorkspaceNavigationResult(bool IsSuccess, WorkspaceDefinition? Workspace, string? DiagnosticCode = null)
{
    public static WorkspaceNavigationResult Unavailable(string code) => new(false, null, code);
}
public sealed class WorkspaceNavigationChangedEventArgs(WorkspaceDefinition? previous, WorkspaceDefinition? current) : EventArgs
{
    public WorkspaceDefinition? PreviousWorkspace { get; } = previous;
    public WorkspaceDefinition? CurrentWorkspace { get; } = current;
}
public interface IWorkspaceNavigationService
{
    WorkspaceDefinition? CurrentWorkspace { get; }
    event EventHandler<WorkspaceNavigationChangedEventArgs>? NavigationChanged;
    Task<WorkspaceNavigationResult> NavigateAsync(string workspaceId, CancellationToken cancellationToken = default);
}
public sealed class WorkspaceNavigationService(IEnumerable<WorkspaceDefinition> workspaces) : IWorkspaceNavigationService
{
    private readonly IReadOnlyDictionary<string, WorkspaceDefinition> workspaces = workspaces.ToDictionary(x => x.WorkspaceId, StringComparer.OrdinalIgnoreCase);
    public WorkspaceDefinition? CurrentWorkspace { get; private set; }
    public event EventHandler<WorkspaceNavigationChangedEventArgs>? NavigationChanged;
    public Task<WorkspaceNavigationResult> NavigateAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(workspaceId) || !workspaces.TryGetValue(workspaceId, out var next)) return Task.FromResult(WorkspaceNavigationResult.Unavailable("WORKSPACE_UNKNOWN"));
        var previous = CurrentWorkspace;
        CurrentWorkspace = next;
        if (previous != next) NavigationChanged?.Invoke(this, new(previous, next));
        return Task.FromResult(new WorkspaceNavigationResult(true, next));
    }
}
