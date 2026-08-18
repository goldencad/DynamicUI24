using System.Collections.Immutable;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class UiAuthoringFoundationTests
{
    private static readonly UiDefinitionCode Code = new("DEMO.APP");

    [Fact]
    public void PublishedDefinitionIsImmutableAndDraftHasBoundedUndoRedo()
    {
        var published = Definition();
        var draft = new UiDefinitionDraft(published);
        var added = Element("FIELD.NAME", UiElementKind.Field, "FORM.MAIN", Editor());
        draft.Upsert(added);
        Assert.True(draft.IsDirty); Assert.Equal(3, draft.Elements.Length);
        Assert.True(draft.Undo()); Assert.Equal(2, draft.Elements.Length);
        Assert.True(draft.Redo()); Assert.Equal(3, draft.Elements.Length);
        Assert.Equal(2, published.Elements.Length);
    }

    [Fact]
    public void ValidationBlocksDuplicateMissingParentInvalidLayoutAndEditor()
    {
        var draft = new UiDefinitionDraft(Definition());
        draft.Upsert(new(new("BROKEN"), UiElementKind.Field, new("Broken"), new("MISSING"),
            editor: new(new("BROKEN"), new("BROKEN"), EditorValueType.Integer, EditorKind.Password),
            layout: new(100, 300, 100)));
        var result = new UiDefinitionValidator().Validate(draft);
        Assert.False(result.CanPublish);
        Assert.Contains(result.Diagnostics, x => x.Code == "UI_MISSING_PARENT");
        Assert.Contains(result.Diagnostics, x => x.Code == "UI_INVALID_LAYOUT_RANGE");
        Assert.Contains(result.Diagnostics, x => x.Code == "UI_EDITOR_VALUE_TYPE_INCOMPATIBLE");
    }

    [Fact]
    public async Task PublishCreatesCoherentVersionAndRollbackOnlyChangesActivation()
    {
        var v1 = Definition(); var repository = new InMemoryUiDefinitionRepository([v1]);
        var service = new UiDefinitionLifecycleService(repository, new());
        var draft = await service.CreateDraftAsync(Code); draft.Upsert(Element("FORM.EXTRA", UiElementKind.Form));
        var preview = await service.PreviewAsync(draft);
        Assert.Equal(v1.Version, preview.Version);
        Assert.Equal(v1.Version, (await repository.GetActiveAsync(Code))!.Version);
        var v2 = await service.PublishAsync(draft, "Form added");
        Assert.Equal(2, v2.Version.Value); Assert.Equal(v2.Version, (await repository.GetActiveAsync(Code))!.Version);
        await service.RollbackAsync(Code, v1.Version);
        Assert.Equal(v1.Version, (await repository.GetActiveAsync(Code))!.Version);
        Assert.Equal(2, (await repository.GetVersionsAsync(Code)).Count);
    }

    [Fact]
    public async Task RepositoryAllocatesAppendsAndActivatesAtomicallyAndRetryIsIdempotent()
    {
        var v1 = Definition(); var repository = new InMemoryUiDefinitionRepository([v1]);
        var request = new UiDefinitionPublishRequest(Code, new(1), 1, DateTimeOffset.UtcNow,
            v1.Elements, "Atomic publish", "REQUEST-1");
        var first = await repository.PublishAndActivateAsync(request);
        var retry = await repository.PublishAndActivateAsync(request);
        Assert.Equal(2, first.Definition.Version.Value);
        Assert.True(retry.WasAlreadyCommitted);
        Assert.Equal(first.Definition, retry.Definition);
        Assert.Equal(2, (await repository.GetActiveAsync(Code))!.Version.Value);
        var versions = await repository.GetVersionsAsync(Code);
        Assert.Equal(2, versions.Count);
        Assert.Single(versions, x => x.IsActive && x.Version.Value == 2);
    }

    [Fact]
    public async Task VersionConflictIsRejectedBeforeMutationAndDraftRemainsModified()
    {
        var v1 = Definition(); var repository = new InMemoryUiDefinitionRepository([v1]);
        var draft = new UiDefinitionDraft(v1); draft.Upsert(Element("FORM.EXTRA", UiElementKind.Form));
        var invalidExpectation = new UiDefinitionPublishRequest(Code, new(2), 1, DateTimeOffset.UtcNow,
            draft.Elements, "Must fail", "REQUEST-CONFLICT");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.PublishAndActivateAsync(invalidExpectation).AsTask());
        Assert.Equal("UI_DEFINITION_VERSION_CONFLICT", error.Message);
        Assert.True(draft.IsDirty); Assert.Equal(1, draft.BasedOnVersion.Value);
        Assert.Single(await repository.GetVersionsAsync(Code));
        Assert.Equal(1, (await repository.GetActiveAsync(Code))!.Version.Value);
    }

    [Fact]
    public async Task AuthoritativeAllocationSkipsRetainedNewerHistoryAfterRollback()
    {
        var v1 = Definition(); var repository = new InMemoryUiDefinitionRepository([v1]);
        var first = await repository.PublishAndActivateAsync(new(Code, new(1), 1, DateTimeOffset.UtcNow,
            v1.Elements, "v2", "REQUEST-V2"));
        Assert.Equal(2, first.Definition.Version.Value);
        await repository.ActivateAsync(Code, new(1));
        var next = await repository.PublishAndActivateAsync(new(Code, new(1), 1, DateTimeOffset.UtcNow,
            v1.Elements, "v3", "REQUEST-V3"));
        Assert.Equal(3, next.Definition.Version.Value);
        var versions = await repository.GetVersionsAsync(Code);
        Assert.Equal(3, versions.Count);
        Assert.Single(versions, x => x.IsActive && x.Version.Value == 3);
    }

    [Fact]
    public async Task ReusingCommittedRequestAfterRollbackReturnsConflictWithoutReactivation()
    {
        var v1 = Definition(); var repository = new InMemoryUiDefinitionRepository([v1]);
        var request = new UiDefinitionPublishRequest(Code, new(1), 1, DateTimeOffset.UtcNow,
            v1.Elements, "v2", "REQUEST-RETRY");
        await repository.PublishAndActivateAsync(request);
        await repository.ActivateAsync(Code, new(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.PublishAndActivateAsync(request).AsTask());
        Assert.Equal(1, (await repository.GetActiveAsync(Code))!.Version.Value);
        Assert.Equal(2, (await repository.GetVersionsAsync(Code)).Count);
    }

    [Fact]
    public async Task RetryAfterCompletionResultWasLostReturnsSameActiveVersionWithoutDuplicate()
    {
        var v1 = Definition(); var repository = new InMemoryUiDefinitionRepository([v1]);
        var audit = new ThrowOncePublishedAudit();
        var lifecycle = new UiDefinitionLifecycleService(repository, new(), audit);
        var draft = new UiDefinitionDraft(v1); draft.Upsert(Element("FORM.EXTRA", UiElementKind.Form));
        await Assert.ThrowsAsync<InvalidOperationException>(() => lifecycle.PublishAsync(draft, "retry", publishRequestId: "REQUEST-LOST").AsTask());
        Assert.True(draft.IsDirty);
        Assert.Equal(2, (await repository.GetActiveAsync(Code))!.Version.Value);
        var retry = await lifecycle.PublishAsync(draft, "retry", publishRequestId: "REQUEST-LOST");
        Assert.Equal(2, retry.Version.Value);
        Assert.Equal(2, (await repository.GetVersionsAsync(Code)).Count);
        Assert.Single(await repository.GetVersionsAsync(Code), x => x.IsActive && x.Version.Value == 2);
    }

    [Theory]
    [InlineData(UnauthorizedBehavior.Hide, UiAuthorizationState.Hidden)]
    [InlineData(UnauthorizedBehavior.Disable, UiAuthorizationState.Disabled)]
    [InlineData(UnauthorizedBehavior.ReadOnly, UiAuthorizationState.ReadOnly)]
    public async Task PermissionDenialPreservesCanonicalStates(UnauthorizedBehavior behavior, UiAuthorizationState expected)
    {
        var request = Request(new(new("FEATURE"), new("DENIED"), null, null, behavior));
        var result = await new DefaultUiAuthorizationResolver().ResolveAsync(request);
        Assert.Equal(expected, result.State);
    }

    [Fact]
    public async Task CapabilityGrantEnablesAndReportsSemanticCapability()
    {
        var capability = StandardUiCapabilities.CanExport;
        var request = Request(new(Capability: capability), capability, capabilities: ImmutableHashSet.Create(capability));
        var result = await new DefaultUiAuthorizationResolver().ResolveAsync(request);
        Assert.Equal(UiAuthorizationState.Enabled, result.State); Assert.True(result.Grants(capability));
    }

    [Fact]
    public async Task FailureAndStaleGenerationFailClosed()
    {
        var service = new GenerationSafeUiAuthorizationService(new ThrowingResolver());
        var result = await service.ResolveAsync(Request(new(Permission: new("PROTECTED"))));
        Assert.Equal(UiAuthorizationState.Hidden, result.State);
        Assert.Equal("UI_AUTHORIZATION_FAILED", result.SafeDiagnosticCode);
    }

    [Fact]
    public void PreferenceCannotResurrectDeniedElementAndCanReturnLater()
    {
        var element = new UiElementDefinition(new("GRID.SALARY"), UiElementKind.GridColumn, new("Salary"),
            layout: new(125, 64, 640), personalization: new());
        var preference = new UiElementPreference(element.Code, true, 222, 1, true);
        var denied = UiPreferenceResolver.Resolve(element, preference, UiAuthorizationState.Hidden);
        Assert.False(denied.IsVisible); Assert.False(denied.IsPinned); Assert.Equal(222, denied.Width);
        var restored = UiPreferenceResolver.Resolve(element, preference, UiAuthorizationState.Enabled);
        Assert.True(restored.IsVisible); Assert.True(restored.IsPinned); Assert.Equal(222, restored.Width);
    }

    [Fact]
    public void RemovedAndInvalidPreferencesRepairDeterministically()
    {
        var definition = Definition();
        var repaired = UiPreferenceResolver.Repair([new(new("FORM.MAIN")), new(new("REMOVED"))], definition);
        Assert.Single(repaired);
        var element = definition.Elements[0] with { };
        var resolved = UiPreferenceResolver.Resolve(element, new(element.Code, Width: double.NaN), UiAuthorizationState.Enabled);
        Assert.Contains("UI_PREFERENCE_WIDTH_RESET", resolved.RepairCodes);
    }

    private static UiAuthorizationRequest Request(UiAuthorizationBinding? binding, CapabilityCode? requested = null,
        IReadOnlySet<CapabilityCode>? capabilities = null)
    {
        var security = new UserSecurityContext("DEMO", 1, ImmutableHashSet<PermissionCode>.Empty,
            capabilities ?? ImmutableHashSet<CapabilityCode>.Empty);
        var context = new UiAuthorizationContext(security, new CompanyId("COMPANY"), "WORKSPACE", Code,
            new(1), 1, 1, 7, PrivacyMode.On);
        return new(new("TARGET"), binding, requested, context);
    }

    private static UiDefinition Definition() => new(Code, new(1), 1, DateTimeOffset.UnixEpoch,
        [Element("WORKSPACE.MAIN", UiElementKind.Workspace), Element("FORM.MAIN", UiElementKind.Form, "WORKSPACE.MAIN")], "Initial");
    private static UiElementDefinition Element(string code, UiElementKind kind, string? parent = null, EditorDefinition? editor = null) =>
        new(new(code), kind, new($"TITLE.{code}"), parent is null ? null : new(parent), editor: editor);
    private static EditorDefinition Editor() => new(new("TEXT"), new("FIELD.NAME"), EditorValueType.String, EditorKind.Text);
    private sealed class ThrowingResolver : IUiAuthorizationResolver
    { public ValueTask<UiAuthorizationResult> ResolveAsync(UiAuthorizationRequest request, CancellationToken cancellationToken = default) => throw new InvalidOperationException(); }
    private sealed class ThrowOncePublishedAudit : IUiAuthoringAuditSink
    {
        private bool thrown;
        public ValueTask WriteAsync(UiAuthoringAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (!thrown && auditEvent.Kind == UiAuthoringEventKind.Published)
            { thrown = true; throw new InvalidOperationException("SIMULATED_COMPLETION_LOST"); }
            return ValueTask.CompletedTask;
        }
    }
}
