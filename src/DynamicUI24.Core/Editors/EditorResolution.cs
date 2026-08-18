using DynamicUI24.Core.Authorization;

namespace DynamicUI24.Core.Editors;

public enum EditorPlatformCapabilityStatus { Supported, Partial, Unsupported, Unknown }
public enum EditorInteractionState { Hidden, Disabled, ReadOnly, Editable }
public enum EditorResolutionStatus { Resolved, Incompatible, Unsupported }

public sealed record EditorPlatformCapabilities(
    IReadOnlyDictionary<EditorKind, EditorPlatformCapabilityStatus> Kinds)
{
    public static EditorPlatformCapabilities AllNative { get; } = new(
        Enum.GetValues<EditorKind>().ToDictionary(x => x, _ => EditorPlatformCapabilityStatus.Supported));
    public EditorPlatformCapabilityStatus Get(EditorKind kind) =>
        Kinds.GetValueOrDefault(kind, EditorPlatformCapabilityStatus.Unknown);
}

public sealed record EditorResolution(EditorResolutionStatus Status, EditorKind? Kind,
    EditorInteractionState InteractionState, EditorPlatformCapabilityStatus PlatformStatus,
    string? DiagnosticCode = null)
{
    public bool IsUsable => Status == EditorResolutionStatus.Resolved &&
        InteractionState != EditorInteractionState.Hidden;
}

/// <summary>The single deterministic resolver shared by grids, filters, parameters and forms.</summary>
public sealed class EditorResolver
{
    private static readonly IReadOnlyDictionary<EditorValueType, EditorKind> Defaults =
        new Dictionary<EditorValueType, EditorKind>
        {
            [EditorValueType.String] = EditorKind.Text, [EditorValueType.LongString] = EditorKind.MultilineText,
            [EditorValueType.Integer] = EditorKind.Integer, [EditorValueType.Decimal] = EditorKind.Decimal,
            [EditorValueType.Currency] = EditorKind.Currency, [EditorValueType.Percentage] = EditorKind.Percentage,
            [EditorValueType.Boolean] = EditorKind.Boolean, [EditorValueType.Date] = EditorKind.Date,
            [EditorValueType.Time] = EditorKind.Time, [EditorValueType.DateTime] = EditorKind.DateTime,
            [EditorValueType.DateRange] = EditorKind.DateRange, [EditorValueType.Choice] = EditorKind.Choice,
            [EditorValueType.MultiChoice] = EditorKind.MultiChoice, [EditorValueType.LookupKey] = EditorKind.Lookup,
            [EditorValueType.Secret] = EditorKind.Password, [EditorValueType.Hyperlink] = EditorKind.Hyperlink,
        };

    public EditorResolution Resolve(EditorDefinition definition, EditorPlatformCapabilities platform,
        EffectiveAuthorizationContext? authorization = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(platform);
        var interaction = ResolveInteraction(definition, authorization);
        var kind = definition.ExplicitKind ?? Defaults[definition.ValueType];
        if (!IsCompatible(definition.ValueType, kind))
            return new(EditorResolutionStatus.Incompatible, null, interaction,
                EditorPlatformCapabilityStatus.Unsupported, "EDITOR_KIND_INCOMPATIBLE");
        var support = platform.Get(kind);
        if (support is EditorPlatformCapabilityStatus.Unsupported or EditorPlatformCapabilityStatus.Unknown)
            return new(EditorResolutionStatus.Unsupported, kind, interaction, support, "EDITOR_KIND_UNSUPPORTED");
        return new(EditorResolutionStatus.Resolved, kind, interaction, support);
    }

    private static EditorInteractionState ResolveInteraction(EditorDefinition definition,
        EffectiveAuthorizationContext? authorization)
    {
        if (definition.PresentationRequirement is { } requirement)
        {
            var state = AuthorizationPresentationResolver.Resolve(requirement, authorization);
            if (state == AuthorizationPresentationState.Hidden) return EditorInteractionState.Hidden;
            if (state == AuthorizationPresentationState.VisibleDisabled) return EditorInteractionState.Disabled;
            if (state == AuthorizationPresentationState.VisibleReadOnly) return EditorInteractionState.ReadOnly;
        }
        if (definition.IsDisabled) return EditorInteractionState.Disabled;
        return definition.IsReadOnly ? EditorInteractionState.ReadOnly : EditorInteractionState.Editable;
    }

    public static bool IsCompatible(EditorValueType type, EditorKind kind) => type switch
    {
        EditorValueType.String => kind is EditorKind.Text or EditorKind.MultilineText or EditorKind.ButtonEdit,
        EditorValueType.LongString => kind is EditorKind.MultilineText or EditorKind.Text,
        EditorValueType.Integer => kind == EditorKind.Integer,
        EditorValueType.Decimal => kind == EditorKind.Decimal,
        EditorValueType.Currency => kind == EditorKind.Currency,
        EditorValueType.Percentage => kind == EditorKind.Percentage,
        EditorValueType.Boolean => kind is EditorKind.Boolean or EditorKind.Choice,
        EditorValueType.Date => kind == EditorKind.Date,
        EditorValueType.Time => kind == EditorKind.Time,
        EditorValueType.DateTime => kind == EditorKind.DateTime,
        EditorValueType.DateRange => kind == EditorKind.DateRange,
        EditorValueType.Choice => kind == EditorKind.Choice,
        EditorValueType.MultiChoice => kind == EditorKind.MultiChoice,
        EditorValueType.LookupKey => kind is EditorKind.Lookup or EditorKind.SearchLookup or EditorKind.TreeLookup,
        EditorValueType.Secret => kind == EditorKind.Password,
        EditorValueType.Hyperlink => kind == EditorKind.Hyperlink,
        _ => false,
    };
}
