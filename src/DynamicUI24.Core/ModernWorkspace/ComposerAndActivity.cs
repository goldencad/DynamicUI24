using System.Collections.Immutable;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.Authorization;

namespace DynamicUI24.Core.ModernWorkspace;

public enum ComposerSubmitMeaning { Send, Run, Comment, Create, Ask, ApplicationDefined }
public sealed record ComposerDefinition(string ComposerCode, string SubmitCommandCode, ComposerSubmitMeaning SubmitMeaning,
    bool AllowAttachments = false, bool AllowMentions = false, bool AllowActionPicker = false,
    CapabilityCode? SubmitCapability = null, int MaximumAttachments = 10, int MaximumSuggestions = 20);
public enum ComposerExecutionState { Idle, Invalid, Submitting, Running, Failed }
public sealed record ComposerRuntimeState(string DraftText, ImmutableArray<ResourceChip> AttachedResources,
    ImmutableArray<string> SafeValidationMessages, ComposerExecutionState State = ComposerExecutionState.Idle,
    string? OperationId = null)
{
    public static ComposerRuntimeState Empty { get; } = new(string.Empty, [], []);
}
public sealed record ComposerSubmission(string ComposerCode, string CommandCode, string DraftText,
    ImmutableArray<(ResourceKind Kind, string SemanticId)> Resources);
public interface IComposerCommandAdapter { ValueTask<string?> SubmitAsync(ComposerSubmission submission, CancellationToken cancellationToken = default); }
public interface IMentionProvider { ValueTask<ImmutableArray<ResourceChip>> SearchAsync(string boundedQuery, int maximumResults, CancellationToken cancellationToken = default); }
public interface IComposerActionProvider { ValueTask<ImmutableArray<ContextualActionDefinition>> ResolveAsync(string boundedQuery, int maximumResults, CancellationToken cancellationToken = default); }

public enum ActivityKind { Operation, Configuration, Document, Approval, Record, ApplicationDefined }
public sealed record ActivityItem(string ActivityId, ActivityKind ActivityKind, DateTimeOffset Timestamp,
    string SafeSummary, string? ActorSemanticId = null, string? TargetSemanticId = null);
public interface IActivityProvider { ValueTask<ImmutableArray<ActivityItem>> GetPageAsync(string? cursor, int maximumItems, CancellationToken cancellationToken = default); }
public enum ContentPresentationState { Initial, Loading, Empty, FilteredEmpty, Unavailable, Offline, Unauthorized, Error, Partial, Ready }
public sealed record ContentStatePresentation(ContentPresentationState State, string SafeMessage,
    string? PrimaryCommandCode = null, string? DetailsCommandCode = null);
