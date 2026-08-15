using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;

namespace DynamicUI24.Core.Ribbon;

public sealed record RibbonSelectionContext(int Count)
{
    public bool HasSelection => Count > 0;
}

public sealed record RibbonResolutionContext(
    CompanyDescriptor Company,
    WorkspaceDefinition Workspace,
    TemplateCode TemplateCode,
    EffectiveAuthorizationContext? Authorization,
    RibbonSelectionContext Selection);

public sealed record ResolvedRibbonCommand(
    RibbonCommandDefinition Definition,
    AuthorizationPresentationState State)
{
    public bool IsEnabled => State == AuthorizationPresentationState.VisibleEnabled;
    public bool IsReadOnly => State == AuthorizationPresentationState.VisibleReadOnly;
}

public sealed record ResolvedRibbonGroup(
    RibbonGroupDefinition Definition,
    AuthorizationPresentationState State,
    ImmutableArray<ResolvedRibbonCommand> Commands);

public sealed record ResolvedRibbonTab(
    RibbonTabDefinition Definition,
    AuthorizationPresentationState State,
    ImmutableArray<ResolvedRibbonGroup> Groups);

public sealed record ResolvedRibbon(
    RibbonDefinition Definition,
    ImmutableArray<ResolvedRibbonTab> Tabs,
    ImmutableArray<RibbonDiagnostic> Diagnostics);

public sealed class DynamicRibbonResolver
{
    public ResolvedRibbon Resolve(RibbonDefinition definition, RibbonResolutionContext context,
        IEnumerable<WorkspaceDefinition>? knownWorkspaces = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);
        var validation = RibbonDefinitionValidator.Validate(definition, knownWorkspaces);
        if (!validation.IsValid) return new(definition, [], validation.Diagnostics);

        var tabs = definition.Tabs
            .Where(x => x.IsVisible && Matches(x.ContextRule, context))
            .Select(tab => ResolveTab(tab, context))
            .Where(x => x.State != AuthorizationPresentationState.Hidden && !x.Groups.IsEmpty)
            .ToImmutableArray();
        return new(definition, tabs, validation.Diagnostics);
    }

    private static ResolvedRibbonTab ResolveTab(RibbonTabDefinition tab, RibbonResolutionContext context)
    {
        var state = ResolveRequirement(tab.PermissionRequirement, context.Authorization);
        var groups = tab.Groups
            .Where(x => x.IsVisible && Matches(x.ContextRule, context))
            .Select(group => ResolveGroup(group, context, state))
            .Where(x => x.State != AuthorizationPresentationState.Hidden && !x.Commands.IsEmpty)
            .ToImmutableArray();
        return new(tab, state, groups);
    }

    private static ResolvedRibbonGroup ResolveGroup(RibbonGroupDefinition group, RibbonResolutionContext context,
        AuthorizationPresentationState parentState)
    {
        var state = Combine(parentState, ResolveRequirement(group.PermissionRequirement, context.Authorization));
        var commands = group.Commands
            .Where(x => Matches(x.ContextRule, context))
            .Select(command =>
            {
                var commandState = Combine(state, ResolveRequirement(command.PermissionRequirement, context.Authorization));
                if ((command.RequiresSelection || command.ContextRule?.RequiresSelection == true) && !context.Selection.HasSelection)
                    commandState = Combine(commandState, AuthorizationPresentationState.VisibleDisabled);
                return new ResolvedRibbonCommand(command, commandState);
            })
            .Where(x => x.State != AuthorizationPresentationState.Hidden)
            .ToImmutableArray();
        return new(group, state, commands);
    }

    private static AuthorizationPresentationState ResolveRequirement(PresentationRequirement? requirement,
        EffectiveAuthorizationContext? authorization) => requirement is null
            ? AuthorizationPresentationState.VisibleEnabled
            : AuthorizationPresentationResolver.Resolve(requirement, authorization);

    private static bool Matches(RibbonContextRule? rule, RibbonResolutionContext context)
    {
        if (rule is null) return true;
        if (!rule.IsWellFormed) return false;
        if (rule.WorkspaceId is { } workspace && !workspace.Equals(context.Workspace.WorkspaceId, StringComparison.OrdinalIgnoreCase)) return false;
        if (rule.TemplateCode is { } template && template != context.TemplateCode) return false;
        if (rule.RequiresSelection && !context.Selection.HasSelection) return false;
        if (rule.CapabilityCode is { } capability &&
            (context.Authorization is null || context.Authorization.Status != EffectiveAuthorizationStatus.Ready ||
             !context.Authorization.CapabilityCodes.Contains(capability))) return false;
        return true;
    }

    private static AuthorizationPresentationState Combine(AuthorizationPresentationState left,
        AuthorizationPresentationState right) =>
        (AuthorizationPresentationState)Math.Max((int)left, (int)right);
}
