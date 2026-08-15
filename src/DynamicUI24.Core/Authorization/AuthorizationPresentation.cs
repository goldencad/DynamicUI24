using System.Collections.Immutable;
using DynamicUI24.Core.Companies;

namespace DynamicUI24.Core.Authorization;

public readonly record struct UserId
{
    public UserId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public sealed record UserContext(UserId UserId);

public readonly record struct AuthorizationContextCacheKey
{
    public AuthorizationContextCacheKey(UserId userId, CompanyId companyId, string revision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        UserId = userId;
        CompanyId = companyId;
        Revision = revision.Trim();
    }

    public UserId UserId { get; }
    public CompanyId CompanyId { get; }
    public string Revision { get; }
}

public readonly record struct PermissionCode
{
    public PermissionCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct CapabilityCode
{
    public CapabilityCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public enum EffectiveAuthorizationStatus
{
    Ready,
    Error,
    Unavailable,
}

public sealed record EffectiveAuthorizationContext
{
    public EffectiveAuthorizationContext(
        UserId userId,
        CompanyId companyId,
        IEnumerable<PermissionCode>? permissionCodes,
        IEnumerable<CapabilityCode>? capabilityCodes,
        string revision,
        EffectiveAuthorizationStatus status = EffectiveAuthorizationStatus.Ready,
        string? diagnosticCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        UserId = userId;
        CompanyId = companyId;
        PermissionCodes = (permissionCodes ?? []).ToImmutableHashSet();
        CapabilityCodes = (capabilityCodes ?? []).ToImmutableHashSet();
        Revision = revision.Trim();
        Status = status;
        DiagnosticCode = diagnosticCode;
    }

    public UserId UserId { get; }
    public CompanyId CompanyId { get; }
    public IReadOnlySet<PermissionCode> PermissionCodes { get; }
    public IReadOnlySet<CapabilityCode> CapabilityCodes { get; }
    public string Revision { get; }
    public EffectiveAuthorizationStatus Status { get; }
    public string? DiagnosticCode { get; }

    public static EffectiveAuthorizationContext Unavailable(
        UserId userId,
        CompanyId companyId,
        string revision,
        string? diagnosticCode = null) =>
        new(userId, companyId, [], [], revision, EffectiveAuthorizationStatus.Unavailable, diagnosticCode);
}

public interface IAuthorizationPresentationProvider
{
    Task<EffectiveAuthorizationContext> GetEffectiveContextAsync(
        UserContext userContext,
        CompanyId companyId,
        CancellationToken cancellationToken = default);

    Task<EffectiveAuthorizationContext> RefreshAsync(
        UserContext userContext,
        CompanyId companyId,
        CancellationToken cancellationToken = default);
}

public enum UnauthorizedBehavior
{
    Hide,
    Disable,
    ReadOnly,
}

public sealed record PresentationRequirement(
    PermissionCode? PermissionCode = null,
    CapabilityCode? CapabilityCode = null,
    UnauthorizedBehavior UnauthorizedBehavior = UnauthorizedBehavior.Disable,
    UnauthorizedBehavior? CapabilityUnavailableBehavior = null);

public enum AuthorizationPresentationState
{
    VisibleEnabled,
    VisibleDisabled,
    VisibleReadOnly,
    Hidden,
}

/// <summary>Resolves presentation only; it never authorizes or executes an operation.</summary>
public static class AuthorizationPresentationResolver
{
    public static AuthorizationPresentationState Resolve(
        PresentationRequirement requirement,
        EffectiveAuthorizationContext? context)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        var isPrivileged = requirement.PermissionCode is not null || requirement.CapabilityCode is not null;
        if (!isPrivileged)
        {
            return AuthorizationPresentationState.VisibleEnabled;
        }

        if (context is null || context.Status != EffectiveAuthorizationStatus.Ready)
        {
            return Apply(requirement.CapabilityUnavailableBehavior ?? requirement.UnauthorizedBehavior);
        }

        if (requirement.PermissionCode is { } permission && !context.PermissionCodes.Contains(permission))
        {
            return Apply(requirement.UnauthorizedBehavior);
        }

        if (requirement.CapabilityCode is { } capability && !context.CapabilityCodes.Contains(capability))
        {
            return Apply(requirement.CapabilityUnavailableBehavior ?? requirement.UnauthorizedBehavior);
        }

        return AuthorizationPresentationState.VisibleEnabled;
    }

    private static AuthorizationPresentationState Apply(UnauthorizedBehavior behavior) => behavior switch
    {
        UnauthorizedBehavior.Hide => AuthorizationPresentationState.Hidden,
        UnauthorizedBehavior.Disable => AuthorizationPresentationState.VisibleDisabled,
        UnauthorizedBehavior.ReadOnly => AuthorizationPresentationState.VisibleReadOnly,
        _ => throw new ArgumentOutOfRangeException(nameof(behavior)),
    };
}
