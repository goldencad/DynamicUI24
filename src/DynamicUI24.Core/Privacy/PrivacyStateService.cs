using System.Collections.Immutable;

namespace DynamicUI24.Core.Privacy;

public enum PrivacyStateChangeKind { ModeChanged, TemporaryRevealStarted, TemporaryRevealEnded, PolicyInvalidated, ContextInvalidated }
public sealed class PrivacyStateChangedEventArgs(PrivacyStateChangeKind kind, long generation, string? scopeKey = null) : EventArgs
{
    public PrivacyStateChangeKind Kind { get; } = kind;
    public long Generation { get; } = generation;
    public string? ScopeKey { get; } = scopeKey;
}
public sealed record TemporaryRevealRequest(string FieldKey, RevealScope Scope, TimeSpan Duration, long Generation);

public interface IPrivacyStateService
{
    PrivacyMode RequestedMode { get; }
    PrivacyMode EffectiveMode { get; }
    long Generation { get; }
    event EventHandler<PrivacyStateChangedEventArgs>? StateChanged;
    void SetRequestedMode(PrivacyMode mode, PrivacyMode? mandatoryMode = null);
    bool BeginReveal(TemporaryRevealRequest request);
    bool IsRevealed(string fieldKey, long generation);
    void RevokeReveal(string? fieldKey = null);
    void InvalidateContext(string? companyId = null, string? workspaceId = null);
    void InvalidatePolicy(PrivacyMode? mandatoryMode = null);
}

public sealed class PrivacyStateService(TimeProvider? timeProvider = null) : IPrivacyStateService
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private ImmutableDictionary<string, RevealEntry> reveals = ImmutableDictionary<string, RevealEntry>.Empty.WithComparers(StringComparer.Ordinal);
    public PrivacyMode RequestedMode { get; private set; } = PrivacyMode.Auto;
    public PrivacyMode EffectiveMode { get; private set; } = PrivacyMode.Auto;
    public long Generation { get; private set; }
    public event EventHandler<PrivacyStateChangedEventArgs>? StateChanged;

    public void SetRequestedMode(PrivacyMode mode, PrivacyMode? mandatoryMode = null)
    {
        if (!Enum.IsDefined(mode)) mode = PrivacyMode.On;
        RequestedMode = mode; EffectiveMode = mandatoryMode ?? mode; Generation++; RevokeExpired();
        StateChanged?.Invoke(this, new(PrivacyStateChangeKind.ModeChanged, Generation));
    }

    public bool BeginReveal(TemporaryRevealRequest request)
    {
        if (request.Generation != Generation || request.Duration <= TimeSpan.Zero || string.IsNullOrWhiteSpace(request.FieldKey)) return false;
        reveals = reveals.SetItem(request.FieldKey, new(clock.GetUtcNow().Add(request.Duration), request.Generation));
        StateChanged?.Invoke(this, new(PrivacyStateChangeKind.TemporaryRevealStarted, Generation, request.FieldKey));
        return true;
    }

    public bool IsRevealed(string fieldKey, long generation)
    {
        RevokeExpired();
        return generation == Generation && reveals.TryGetValue(fieldKey, out var reveal) && reveal.Generation == generation;
    }

    public void RevokeReveal(string? fieldKey = null)
    {
        var changed = fieldKey is null ? reveals.Count > 0 : reveals.ContainsKey(fieldKey);
        reveals = fieldKey is null ? reveals.Clear() : reveals.Remove(fieldKey);
        if (changed) StateChanged?.Invoke(this, new(PrivacyStateChangeKind.TemporaryRevealEnded, Generation, fieldKey));
    }

    public void InvalidateContext(string? companyId = null, string? workspaceId = null)
    {
        reveals = reveals.Clear(); Generation++;
        StateChanged?.Invoke(this, new(PrivacyStateChangeKind.ContextInvalidated, Generation, workspaceId ?? companyId));
    }

    public void InvalidatePolicy(PrivacyMode? mandatoryMode = null)
    {
        reveals = reveals.Clear(); EffectiveMode = mandatoryMode ?? RequestedMode; Generation++;
        StateChanged?.Invoke(this, new(PrivacyStateChangeKind.PolicyInvalidated, Generation));
    }

    private void RevokeExpired()
    {
        var now = clock.GetUtcNow();
        foreach (var entry in reveals.Where(x => x.Value.ExpiresAt <= now).ToArray())
        {
            reveals = reveals.Remove(entry.Key);
            StateChanged?.Invoke(this, new(PrivacyStateChangeKind.TemporaryRevealEnded, Generation, entry.Key));
        }
    }
    private sealed record RevealEntry(DateTimeOffset ExpiresAt, long Generation);
}
