using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace DynamicUI24.Core.ModernWorkspace;

public enum OperationState { Pending, Running, Succeeded, Failed, Cancelled, NeedsAttention }
public sealed record OperationCapabilities(bool CanCancel = false, bool CanRetry = false, bool CanDismiss = false,
    bool CanOpenResult = false, bool CanOpenDetails = false);
public sealed record OperationProgress(double? Fraction = null, string? SafeCurrentStep = null)
{
    public bool IsIndeterminate => Fraction is null;
    public double? NormalizedFraction => Fraction is { } value && double.IsFinite(value) ? Math.Clamp(value, 0, 1) : null;
}
public sealed record OperationSnapshot(string OperationId, string OperationKind, string SourceFeatureCode,
    OperationState State, string SafeTitle, string? SafeDescription = null, WorkspaceCode? WorkspaceCode = null,
    string? TargetSemanticId = null, OperationCapabilities? Capabilities = null, OperationProgress? Progress = null,
    long Generation = 0, string? SafeDiagnosticCode = null)
{
    public OperationCapabilities EffectiveCapabilities => Capabilities ?? new();
}
public interface IOperationProvider
{
    ValueTask<OperationSnapshot?> GetAsync(string operationId, CancellationToken cancellationToken = default);
    ValueTask<OperationSnapshot> CancelAsync(string operationId, CancellationToken cancellationToken = default);
    ValueTask<OperationSnapshot> RetryAsync(string operationId, CancellationToken cancellationToken = default);
}
public interface IOperationNotificationProjection { ValueTask ProjectAsync(OperationSnapshot snapshot, CancellationToken cancellationToken = default); }

public sealed class OperationCoordinator(IOperationNotificationProjection? notifications = null, int maximumRetained = 100)
{
    private readonly ConcurrentDictionary<string, OperationSnapshot> snapshots = new(StringComparer.OrdinalIgnoreCase);
    public ImmutableArray<OperationSnapshot> Current => snapshots.Values.OrderByDescending(x => x.Generation).Take(MaximumRetained).ToImmutableArray();
    public int MaximumRetained { get; } = Math.Clamp(maximumRetained, 1, 1000);
    public async ValueTask<bool> PublishAsync(OperationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Validate(snapshot);
        if (snapshots.TryGetValue(snapshot.OperationId, out var current) && snapshot.Generation < current.Generation) return false;
        snapshots[snapshot.OperationId] = snapshot;
        Trim();
        if (notifications is not null && snapshot.State is OperationState.Succeeded or OperationState.Failed or OperationState.NeedsAttention)
            await notifications.ProjectAsync(snapshot, cancellationToken).ConfigureAwait(false);
        return true;
    }
    public OperationSnapshot? Reattach(string operationId) => snapshots.GetValueOrDefault(operationId);
    public async ValueTask<OperationSnapshot> CancelAsync(string id, IOperationProvider provider, CancellationToken token = default)
    {
        var current = Require(id); if (!current.EffectiveCapabilities.CanCancel || current.State is not (OperationState.Pending or OperationState.Running)) throw new InvalidOperationException("OPERATION_CANCEL_UNAVAILABLE");
        var result = await provider.CancelAsync(id, token).ConfigureAwait(false); await PublishAsync(result, token); return result;
    }
    public async ValueTask<OperationSnapshot> RetryAsync(string id, IOperationProvider provider, CancellationToken token = default)
    {
        var current = Require(id); if (!current.EffectiveCapabilities.CanRetry || current.State is not (OperationState.Failed or OperationState.Cancelled or OperationState.NeedsAttention)) throw new InvalidOperationException("OPERATION_RETRY_UNAVAILABLE");
        var result = await provider.RetryAsync(id, token).ConfigureAwait(false); await PublishAsync(result, token); return result;
    }
    private OperationSnapshot Require(string id) => snapshots.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException("OPERATION_UNKNOWN");
    private void Trim() { foreach (var item in snapshots.Values.OrderByDescending(x => x.Generation).Skip(MaximumRetained)) snapshots.TryRemove(item.OperationId, out _); }
    private static void Validate(OperationSnapshot x) { ArgumentException.ThrowIfNullOrWhiteSpace(x.OperationId); ArgumentException.ThrowIfNullOrWhiteSpace(x.OperationKind); ArgumentException.ThrowIfNullOrWhiteSpace(x.SourceFeatureCode); ArgumentException.ThrowIfNullOrWhiteSpace(x.SafeTitle); }
}
