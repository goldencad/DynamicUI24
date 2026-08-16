using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Privacy;

namespace DynamicUI24.Core.Context;

public sealed record ContextualHelpRequest(HelpContextCode HelpContextCode, CultureInfo Culture,
    CompanyId? CompanyId, string? WorkspaceId, EffectiveAuthorizationContext? PermissionContext,
    PrivacyMode PrivacyMode, long Generation, CancellationToken CancellationToken);
public sealed record ContextualHelpResult(HelpContextCode HelpContextCode, string Title, string Content,
    ImmutableArray<string> RelatedActions, ImmutableArray<string> RelatedNavigation,
    string ProviderCode, long Generation, string? DiagnosticCode = null);
public interface IContextualHelpProvider
{
    string ProviderCode { get; }
    ValueTask<ContextualHelpResult?> GetHelpAsync(ContextualHelpRequest request);
}
public static class HelpContextResolver
{
    /// <summary>Precedence: field, section, workspace, template.</summary>
    public static HelpContextCode? Resolve(HelpContextCode? field, HelpContextCode? section,
        HelpContextCode? workspace, HelpContextCode? template) => field ?? section ?? workspace ?? template;
}
