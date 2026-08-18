using System.Collections.Concurrent;
using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Privacy;

namespace DynamicUI24.Core.Authoring;

public enum UiAuthorizationState { Hidden, Disabled, ReadOnly, Enabled }

public static class StandardUiCapabilities
{
    public static readonly CapabilityCode CanOpen = new("CAN_OPEN");
    public static readonly CapabilityCode CanExecute = new("CAN_EXECUTE");
    public static readonly CapabilityCode CanEdit = new("CAN_EDIT");
    public static readonly CapabilityCode CanCopy = new("CAN_COPY");
    public static readonly CapabilityCode CanPaste = new("CAN_PASTE");
    public static readonly CapabilityCode CanClear = new("CAN_CLEAR");
    public static readonly CapabilityCode CanFind = new("CAN_FIND");
    public static readonly CapabilityCode CanFilter = new("CAN_FILTER");
    public static readonly CapabilityCode CanSort = new("CAN_SORT");
    public static readonly CapabilityCode CanGroup = new("CAN_GROUP");
    public static readonly CapabilityCode CanExport = new("CAN_EXPORT");
    public static readonly CapabilityCode CanPrint = new("CAN_PRINT");
    public static readonly CapabilityCode CanReveal = new("CAN_REVEAL");
    public static readonly CapabilityCode CanConfigure = new("CAN_CONFIGURE");
    public static readonly CapabilityCode CanPersonalize = new("CAN_PERSONALIZE");
    public static readonly CapabilityCode CanDrillDown = new("CAN_DRILL_DOWN");
    public static readonly CapabilityCode CanOpenUiAuthoring = new("CAN_OPEN_UI_AUTHORING");
    public static readonly CapabilityCode CanEditUiDefinition = new("CAN_EDIT_UI_DEFINITION");
    public static readonly CapabilityCode CanPreviewUiDefinition = new("CAN_PREVIEW_UI_DEFINITION");
    public static readonly CapabilityCode CanPublishUiDefinition = new("CAN_PUBLISH_UI_DEFINITION");
    public static readonly CapabilityCode CanRollbackUiDefinition = new("CAN_ROLLBACK_UI_DEFINITION");
    public static readonly CapabilityCode CanEditAuthorizationBindings = new("CAN_EDIT_AUTHORIZATION_BINDINGS");
}

public sealed record UserSecurityContext(string SubjectCode, long Generation,
    IReadOnlySet<PermissionCode> Permissions, IReadOnlySet<CapabilityCode> Capabilities);

public sealed record UiAuthorizationContext(UserSecurityContext Security, CompanyId CompanyId,
    string? WorkspaceCode, UiDefinitionCode DefinitionCode, UiDefinitionVersion DefinitionVersion,
    long CompanyGeneration, long PolicyGeneration, long RequestGeneration, PrivacyMode PrivacyMode);

public sealed record UiAuthorizationRequest(UiElementCode ElementCode, UiAuthorizationBinding? Binding,
    CapabilityCode? RequestedCapability, UiAuthorizationContext Context, bool Protected = true);

public sealed record UiAuthorizationResult(UiElementCode ElementCode, UiAuthorizationState State,
    ImmutableHashSet<CapabilityCode> GrantedCapabilities, long RequestGeneration,
    UiDefinitionVersion DefinitionVersion, string? SafeDiagnosticCode = null)
{
    public bool IsCurrent(UiAuthorizationContext context) => RequestGeneration == context.RequestGeneration &&
        DefinitionVersion == context.DefinitionVersion;
    public bool Grants(CapabilityCode capability) => State == UiAuthorizationState.Enabled && GrantedCapabilities.Contains(capability);
}

/// <summary>Consumes application/security decisions for presentation. It is never a backend security boundary.</summary>
public interface IUiAuthorizationResolver
{
    ValueTask<UiAuthorizationResult> ResolveAsync(UiAuthorizationRequest request, CancellationToken cancellationToken = default);
}

public sealed class DefaultUiAuthorizationResolver : IUiAuthorizationResolver
{
    public ValueTask<UiAuthorizationResult> ResolveAsync(UiAuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var binding = request.Binding;
        if (binding is null && request.RequestedCapability is null)
            return ValueTask.FromResult(Result(request, UiAuthorizationState.Enabled));
        var security = request.Context.Security;
        if (binding?.Permission is { } permission && !security.Permissions.Contains(permission))
            return ValueTask.FromResult(Result(request, Map(binding.DeniedBehavior), "UI_PERMISSION_DENIED"));
        if (binding?.Capability is { } required && !security.Capabilities.Contains(required))
            return ValueTask.FromResult(Result(request, Map(binding.DeniedBehavior), "UI_CAPABILITY_UNAVAILABLE"));
        if (request.RequestedCapability is { } capability && !security.Capabilities.Contains(capability))
            return ValueTask.FromResult(Result(request, Map(binding?.DeniedBehavior ?? UnauthorizedBehavior.Disable), "UI_CAPABILITY_DENIED"));
        return ValueTask.FromResult(Result(request, UiAuthorizationState.Enabled,
            grants: request.RequestedCapability is { } granted ? [granted] : []));
    }

    private static UiAuthorizationResult Result(UiAuthorizationRequest request, UiAuthorizationState state,
        string? diagnostic = null, IEnumerable<CapabilityCode>? grants = null) => new(request.ElementCode, state,
            (grants ?? []).ToImmutableHashSet(), request.Context.RequestGeneration,
            request.Context.DefinitionVersion, diagnostic);
    private static UiAuthorizationState Map(UnauthorizedBehavior behavior) => behavior switch
    { UnauthorizedBehavior.Hide => UiAuthorizationState.Hidden, UnauthorizedBehavior.Disable => UiAuthorizationState.Disabled,
      UnauthorizedBehavior.ReadOnly => UiAuthorizationState.ReadOnly, _ => UiAuthorizationState.Hidden };
}

public sealed class GenerationSafeUiAuthorizationService(IUiAuthorizationResolver resolver)
{
    private readonly ConcurrentDictionary<UiAuthorizationCacheKey, UiAuthorizationResult> cache = new();
    public async ValueTask<UiAuthorizationResult> ResolveAsync(UiAuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        var key = new UiAuthorizationCacheKey(request.ElementCode, request.RequestedCapability,
            request.Context.DefinitionVersion, request.Context.Security.Generation,
            request.Context.CompanyGeneration, request.Context.PolicyGeneration);
        if (cache.TryGetValue(key, out var cached)) return cached with { RequestGeneration = request.Context.RequestGeneration };
        try
        {
            var result = await resolver.ResolveAsync(request, cancellationToken).ConfigureAwait(false);
            if (!result.IsCurrent(request.Context)) return FailClosed(request, "UI_AUTHORIZATION_STALE");
            cache[key] = result; return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return FailClosed(request, "UI_AUTHORIZATION_FAILED"); }
    }
    public void Clear() => cache.Clear();
    private static UiAuthorizationResult FailClosed(UiAuthorizationRequest request, string code) =>
        new(request.ElementCode, request.Protected ? UiAuthorizationState.Hidden : UiAuthorizationState.Disabled,
            [], request.Context.RequestGeneration, request.Context.DefinitionVersion, code);
    private readonly record struct UiAuthorizationCacheKey(UiElementCode Element, CapabilityCode? Capability,
        UiDefinitionVersion Version, long SecurityGeneration, long CompanyGeneration, long PolicyGeneration);
}

public static class UiAuthorizationPresentationAdapter
{
    public static AuthorizationPresentationState ToPresentationState(this UiAuthorizationState state) => state switch
    { UiAuthorizationState.Hidden => AuthorizationPresentationState.Hidden,
      UiAuthorizationState.Disabled => AuthorizationPresentationState.VisibleDisabled,
      UiAuthorizationState.ReadOnly => AuthorizationPresentationState.VisibleReadOnly,
      UiAuthorizationState.Enabled => AuthorizationPresentationState.VisibleEnabled,
      _ => AuthorizationPresentationState.Hidden };
}
