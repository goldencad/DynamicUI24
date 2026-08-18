using System.Collections.Immutable;

namespace DynamicUI24.Core.Authoring;

public enum UiAuthoringEventKind { DraftCreated, Validated, Previewed, Published, RollbackActivated }
public sealed record UiAuthoringAuditEvent(UiAuthoringEventKind Kind, UiDefinitionCode DefinitionCode,
    UiDefinitionVersion Version, DateTimeOffset Timestamp, string SafeSummary, string? SafeActorContext);
public interface IUiAuthoringAuditSink { ValueTask WriteAsync(UiAuthoringAuditEvent auditEvent, CancellationToken cancellationToken = default); }
public sealed class NullUiAuthoringAuditSink : IUiAuthoringAuditSink
{ public ValueTask WriteAsync(UiAuthoringAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask; }

public sealed class UiDefinitionLifecycleService(IUiDefinitionRepository repository, UiDefinitionValidator validator,
    IUiAuthoringAuditSink? audit = null, TimeProvider? timeProvider = null)
{
    private readonly IUiAuthoringAuditSink audit = audit ?? new NullUiAuthoringAuditSink();
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async ValueTask<UiDefinitionDraft> CreateDraftAsync(UiDefinitionCode code, string? actor = null, CancellationToken cancellationToken = default)
    {
        var published = await repository.GetActiveAsync(code, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("UI_DEFINITION_NOT_FOUND");
        var draft = new UiDefinitionDraft(published);
        await Write(UiAuthoringEventKind.DraftCreated, code, published.Version, "Draft created", actor, cancellationToken);
        return draft;
    }

    public async ValueTask<UiDefinitionValidationResult> ValidateAsync(UiDefinitionDraft draft, string? actor = null, CancellationToken cancellationToken = default)
    {
        var result = validator.Validate(draft);
        await Write(UiAuthoringEventKind.Validated, draft.Code, draft.BasedOnVersion,
            result.CanPublish ? "Definition valid" : "Definition has blocking diagnostics", actor, cancellationToken);
        return result;
    }

    public async ValueTask<UiDefinition> PreviewAsync(UiDefinitionDraft draft, string? actor = null, CancellationToken cancellationToken = default)
    {
        var preview = draft.CreatePublished(draft.BasedOnVersion, clock.GetUtcNow(), "DRAFT PREVIEW");
        await Write(UiAuthoringEventKind.Previewed, draft.Code, draft.BasedOnVersion, "Draft previewed", actor, cancellationToken);
        return preview;
    }

    public async ValueTask<UiDefinition> PublishAsync(UiDefinitionDraft draft, string safeSummary, string? actor = null,
        CancellationToken cancellationToken = default, string? publishRequestId = null)
    {
        var validation = validator.Validate(draft);
        if (!validation.CanPublish) throw new InvalidOperationException("UI_DEFINITION_VALIDATION_FAILED");
        var request = new UiDefinitionPublishRequest(draft.Code, draft.BasedOnVersion, draft.SchemaVersion,
            clock.GetUtcNow(), draft.Elements, safeSummary,
            string.IsNullOrWhiteSpace(publishRequestId) ? Guid.NewGuid().ToString("N") : publishRequestId.Trim()).Validate();
        var result = await repository.PublishAndActivateAsync(request, cancellationToken).ConfigureAwait(false);
        var published = result.Definition;
        await Write(UiAuthoringEventKind.Published, published.Code, published.Version, published.SafeChangeSummary, actor, cancellationToken);
        return published;
    }

    public async ValueTask RollbackAsync(UiDefinitionCode code, UiDefinitionVersion version, string? actor = null, CancellationToken cancellationToken = default)
    {
        var versions = await repository.GetVersionsAsync(code, cancellationToken).ConfigureAwait(false);
        if (!versions.Any(x => x.Version == version)) throw new InvalidOperationException("UI_DEFINITION_VERSION_NOT_FOUND");
        await repository.ActivateAsync(code, version, cancellationToken).ConfigureAwait(false);
        await Write(UiAuthoringEventKind.RollbackActivated, code, version, "Previous definition activated", actor, cancellationToken);
    }

    private ValueTask Write(UiAuthoringEventKind kind, UiDefinitionCode code, UiDefinitionVersion version,
        string summary, string? actor, CancellationToken token) => audit.WriteAsync(
            new(kind, code, version, clock.GetUtcNow(), summary, actor), token);
}

public sealed class InMemoryUiDefinitionRepository : IUiDefinitionRepository
{
    private readonly object gate = new();
    private readonly Dictionary<UiDefinitionCode, SortedDictionary<long, UiDefinition>> definitions = [];
    private readonly Dictionary<UiDefinitionCode, long> active = [];
    private readonly Dictionary<string, PublishReceipt> publishReceipts = new(StringComparer.Ordinal);
    public InMemoryUiDefinitionRepository(IEnumerable<UiDefinition>? seed = null)
    { foreach (var item in seed ?? []) Add(item, activate: true); }
    public ValueTask<UiDefinition?> GetActiveAsync(UiDefinitionCode code, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); lock (gate) return ValueTask.FromResult(active.TryGetValue(code, out var v) ? definitions[code][v] : null); }
    public ValueTask<IReadOnlyList<UiDefinitionVersionInfo>> GetVersionsAsync(UiDefinitionCode code, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); lock (gate) { if (!definitions.TryGetValue(code, out var versions)) return ValueTask.FromResult<IReadOnlyList<UiDefinitionVersionInfo>>([]);
        return ValueTask.FromResult<IReadOnlyList<UiDefinitionVersionInfo>>(versions.Values.Select(x => new UiDefinitionVersionInfo(x.Code, x.Version, x.SchemaVersion, x.PublishedAt, x.SafeChangeSummary, active.GetValueOrDefault(code) == x.Version.Value)).ToImmutableArray()); } }
    public ValueTask<UiDefinitionPublishResult> PublishAndActivateAsync(UiDefinitionPublishRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); request.Validate();
        lock (gate)
        {
            if (publishReceipts.TryGetValue(request.PublishRequestId, out var receipt))
            {
                if (receipt.Code != request.Code || receipt.ExpectedVersion != request.ExpectedActiveVersion ||
                    receipt.Signature != Signature(request) || active.GetValueOrDefault(request.Code) != receipt.Definition.Version.Value)
                    throw new InvalidOperationException("UI_DEFINITION_PUBLISH_REQUEST_CONFLICT");
                return ValueTask.FromResult(new UiDefinitionPublishResult(receipt.Definition, true));
            }
            if (!active.TryGetValue(request.Code, out var activeVersion) || activeVersion != request.ExpectedActiveVersion.Value)
                throw new InvalidOperationException("UI_DEFINITION_VERSION_CONFLICT");
            if (!definitions.TryGetValue(request.Code, out var versions))
                throw new InvalidOperationException("UI_DEFINITION_NOT_FOUND");
            var nextValue = checked(versions.Keys.Max() + 1);
            if (versions.ContainsKey(nextValue)) throw new InvalidOperationException("UI_DEFINITION_VERSION_CONFLICT");
            var published = new UiDefinition(request.Code, new(nextValue), request.SchemaVersion,
                request.PublishedAt, request.Elements, request.SafeChangeSummary);
            // No await or externally observable state occurs inside this critical section.
            versions.Add(nextValue, published);
            active[request.Code] = nextValue;
            publishReceipts.Add(request.PublishRequestId,
                new(request.Code, request.ExpectedActiveVersion, Signature(request), published));
            return ValueTask.FromResult(new UiDefinitionPublishResult(published, false));
        }
    }
    public ValueTask ActivateAsync(UiDefinitionCode code, UiDefinitionVersion version, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); lock (gate) { if (!definitions.TryGetValue(code, out var versions) || !versions.ContainsKey(version.Value)) throw new InvalidOperationException("UI_DEFINITION_VERSION_NOT_FOUND"); active[code] = version.Value; } return ValueTask.CompletedTask; }
    private void Add(UiDefinition definition, bool activate) { if (!definitions.TryGetValue(definition.Code, out var versions)) definitions[definition.Code] = versions = []; if (versions.ContainsKey(definition.Version.Value)) throw new InvalidOperationException("UI_DEFINITION_VERSION_EXISTS"); versions.Add(definition.Version.Value, definition); if (activate) active[definition.Code] = definition.Version.Value; }
    private static string Signature(UiDefinitionPublishRequest request) => string.Join('\n',
        request.Elements.OrderBy(x => x.Code.Value, StringComparer.Ordinal).Select(x =>
            $"{x.Code.Value}|{x.Kind}|{x.TitleKey.Value}|{x.ParentCode?.Value}|{x.SemanticReference}")) +
        $"\n{request.SchemaVersion}|{request.SafeChangeSummary}";
    private sealed record PublishReceipt(UiDefinitionCode Code, UiDefinitionVersion ExpectedVersion,
        string Signature, UiDefinition Definition);
}
