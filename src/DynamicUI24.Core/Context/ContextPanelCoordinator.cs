using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Privacy;

namespace DynamicUI24.Core.Context;

/// <summary>Isolates provider failures and publishes only the latest semantic request generation.</summary>
public sealed class ContextPanelCoordinator : IDisposable
{
    private readonly IReadOnlyDictionary<string, IContextPanelProvider> providers;
    private readonly object sync = new();
    private CancellationTokenSource? cancellation;
    private long generation;
    public ContextPanelCoordinator(IEnumerable<IContextPanelProvider> providers) => this.providers = providers
        .GroupBy(x => x.ProviderCode, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    public ContextPanelResult? Current { get; private set; }
    public event EventHandler<ContextPanelResult>? Changed;

    public async Task<ContextPanelResult> ResolveAsync(string providerCode,
        Func<long, CancellationToken, ContextPanelRequest> requestFactory, CancellationToken cancellationToken = default)
    {
        CancellationTokenSource local;
        long currentGeneration;
        lock (sync)
        {
            cancellation?.Cancel(); cancellation?.Dispose();
            cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            local = cancellation; currentGeneration = ++generation;
        }
        if (!providers.TryGetValue(providerCode, out var provider))
            return PublishIfCurrent(new(providerCode, string.Empty, [], ContextLoadingState.Error,
                currentGeneration, "CONTEXT_PROVIDER_UNKNOWN"), currentGeneration);
        var request = requestFactory(currentGeneration, local.Token);
        PublishIfCurrent(new(providerCode, request.SemanticKey, [], ContextLoadingState.Loading, currentGeneration), currentGeneration);
        try
        {
            var result = await provider.GetContextAsync(request).ConfigureAwait(false);
            if (result.Generation != currentGeneration)
                return Current ?? ContextPanelResult.Empty(providerCode, request.SemanticKey, currentGeneration);
            return PublishIfCurrent(Validate(result), currentGeneration);
        }
        catch (OperationCanceledException) when (local.IsCancellationRequested)
        { return Current ?? ContextPanelResult.Empty(providerCode, request.SemanticKey, currentGeneration); }
        catch
        {
            return PublishIfCurrent(new(providerCode, request.SemanticKey, [], ContextLoadingState.Error,
                currentGeneration, "CONTEXT_PROVIDER_FAILED"), currentGeneration);
        }
    }
    public void Invalidate()
    {
        lock (sync) { cancellation?.Cancel(); generation++; Current = null; }
    }
    private ContextPanelResult Validate(ContextPanelResult result)
    {
        if (result.Sections.GroupBy(x => x.SectionCode, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1) ||
            result.Sections.SelectMany(x => x.Items).GroupBy(x => x.FieldCode, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            return result with { Sections = [], State = ContextLoadingState.Error, DiagnosticCode = "CONTEXT_RESULT_INVALID" };
        return result;
    }
    private ContextPanelResult PublishIfCurrent(ContextPanelResult result, long expected)
    {
        lock (sync) { if (expected != generation) return Current ?? result; Current = result; }
        Changed?.Invoke(this, result); return result;
    }
    public void Dispose() { lock (sync) { cancellation?.Cancel(); cancellation?.Dispose(); cancellation = null; } }
}

public sealed record ContextItemPresentation(ContextItem Item, string DisplayValue,
    AuthorizationPresentationState AuthorizationState, ResolvedPrivacyPresentation Privacy);

/// <summary>One privacy/permission projection shared by panel text, copy and accessibility surfaces.</summary>
public sealed class ContextItemPresenter(IPrivacyPolicyResolver privacyResolver, ISensitiveValuePresenter valuePresenter)
{
    public ContextItemPresentation Present(ContextItem item, ContextPanelRequest request,
        MandatoryPrivacyPolicy? mandatoryPolicy = null, bool isRevealed = false)
    {
        var auth = AuthorizationPresentationResolver.Resolve(new(item.PermissionCode, item.CapabilityCode,
            UnauthorizedBehavior.Hide), request.PermissionContext);
        var authorized = auth != AuthorizationPresentationState.Hidden;
        ResolvedPrivacyPresentation privacy;
        try { privacy = privacyResolver.Resolve(new(authorized, item.SensitiveContent, request.PrivacyMode,
            mandatoryPolicy, request.CompanyId, request.WorkspaceId, isRevealed, Generation: request.Generation)); }
        catch { privacy = privacyResolver.Resolve(new(false, new(Sensitivity.Restricted, PrivacyPresentation.Hide),
            PrivacyMode.On, new MandatoryPrivacyPolicy(), request.CompanyId, request.WorkspaceId, Generation: request.Generation)); }
        return new(item, valuePresenter.Present(item.Value, item.SensitiveContent, privacy, request.Culture).DisplayValue, auth, privacy);
    }
}
