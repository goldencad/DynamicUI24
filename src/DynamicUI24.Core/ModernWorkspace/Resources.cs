using System.Collections.Immutable;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.Authorization;

namespace DynamicUI24.Core.ModernWorkspace;

public enum ResourceKind { File, Document, Person, Company, Record, Date, Tag, Filter, LookupItem, Workspace, ApplicationDefined }
[Flags] public enum ResourceCapabilities { None = 0, Focus = 1, Open = 2, Preview = 4, Remove = 8, Copy = 16, Status = 32, ContextMenu = 64 }
public sealed record ResourceChip(ResourceKind ResourceKind, string SemanticResourceId, string SafeDisplayLabel,
    string? OptionalIconCode = null, ResourceCapabilities Capabilities = ResourceCapabilities.None,
    UiAuthorizationBinding? AuthorizationRequirement = null);
public enum AttachmentState { Pending, Uploading, Ready, Failed, Rejected }
public sealed record AttachmentChip(ResourceChip Resource, string SafeFileName, string? MediaType = null,
    long? SizeBytes = null, AttachmentState UploadState = AttachmentState.Pending, string? SafeStatusCode = null);

[Flags] public enum DragOperation { None = 0, Copy = 1, Move = 2, Link = 4, Import = 8, Attach = 16, Reorder = 32 }
public sealed record SemanticDragPayload(ResourceKind ResourceKind, ImmutableArray<string> SemanticIds,
    ImmutableDictionary<string, string> SafeDisplayMetadata, DragOperation AllowedOperations)
{
    public bool IsValid => SemanticIds.Length > 0 && SemanticIds.All(x => !string.IsNullOrWhiteSpace(x)) && AllowedOperations != DragOperation.None;
}
public sealed record DropTargetDefinition(string TargetCode, ImmutableHashSet<ResourceKind> AcceptedKinds,
    DragOperation AllowedOperations, CapabilityCode? Capability = null);
public sealed record DropNegotiation(bool Accepted, DragOperation Operation = DragOperation.None, string SafeReasonCode = "DROP_DENIED");
public static class DropNegotiator
{
    public static DropNegotiation Negotiate(SemanticDragPayload payload, DropTargetDefinition target,
        UiAuthorizationState authorization, bool capabilityAvailable, bool privacyAllows)
    {
        if (!payload.IsValid || authorization != UiAuthorizationState.Enabled || !capabilityAvailable || !privacyAllows || !target.AcceptedKinds.Contains(payload.ResourceKind)) return new(false);
        var common = payload.AllowedOperations & target.AllowedOperations;
        if (common == DragOperation.None) return new(false, SafeReasonCode: "DROP_OPERATION_UNAVAILABLE");
        var operation = Enum.GetValues<DragOperation>().Where(x => x != DragOperation.None && common.HasFlag(x)).OrderBy(x => (int)x).First();
        return new(true, operation, "DROP_ACCEPTED");
    }
}
public interface IOsFileDropAdapter { ValueTask<IReadOnlyList<ResourceChip>> ImportAsync(IReadOnlyList<string> platformFileTokens, string targetCode, CancellationToken cancellationToken = default); }
