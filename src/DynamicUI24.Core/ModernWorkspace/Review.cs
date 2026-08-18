using System.Collections.Immutable;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.Authorization;

namespace DynamicUI24.Core.ModernWorkspace;

public enum DifferenceKind { Added, Removed, Changed, Moved, Unchanged, Conflict }
public sealed record CompareIdentity(string CompareSessionId, string LeftRevisionId, string RightRevisionId, string TargetSemanticId);
public sealed record StructuredDifference(string FieldCode, string? SafeBefore, string? SafeAfter, DifferenceKind Kind);
public sealed record TextDifferenceSpan(int Start, int Length, DifferenceKind Kind, string? Classification = null);
public sealed record ComparePresentation(CompareIdentity Identity, ImmutableArray<StructuredDifference> Fields,
    ImmutableArray<TextDifferenceSpan> TextSpans, bool IsPrivacyProtected = false);
public enum ReviewAction { Accept, Reject, Apply, Restore, OpenSource, Comment }
public interface IReviewCommandAdapter { ValueTask ExecuteAsync(CompareIdentity identity, ReviewAction action, string commandCode, CancellationToken cancellationToken = default); }
public sealed record ReviewActionDefinition(ReviewAction Action, string CommandCode, CapabilityCode? Capability = null);
