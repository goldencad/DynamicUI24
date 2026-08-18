using System.Collections.Immutable;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.Authorization;

namespace DynamicUI24.Core.ModernWorkspace;

public enum ContextualActionPlacement { InlineTrailing, FocusOrHover, Overflow, ContextualToolbar }
public sealed record SemanticSelection(string SelectionKind, ImmutableArray<string> SemanticIds, long Generation)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(SelectionKind) && SemanticIds.Length > 0 &&
        SemanticIds.All(x => !string.IsNullOrWhiteSpace(x));
}
public sealed record ContextualActionDefinition(string ActionCode, string CommandCode,
    ContextualActionPlacement Placement, CapabilityCode? Capability = null, bool IsDestructive = false,
    bool HasKeyboardAlternative = true);
public sealed record ResolvedContextualAction(ContextualActionDefinition Definition, UiAuthorizationState State);

public static class ContextualActionResolver
{
    public static ImmutableArray<ResolvedContextualAction> Resolve(SemanticSelection? selection,
        IEnumerable<ContextualActionDefinition> definitions, Func<ContextualActionDefinition, UiAuthorizationState> authorize)
    {
        if (selection?.IsValid != true) return [];
        return definitions.Select(x => new ResolvedContextualAction(x, authorize(x)))
            .Where(x => x.State != UiAuthorizationState.Hidden)
            .Where(x => x.Definition.Placement != ContextualActionPlacement.FocusOrHover || x.Definition.HasKeyboardAlternative)
            .ToImmutableArray();
    }
}
