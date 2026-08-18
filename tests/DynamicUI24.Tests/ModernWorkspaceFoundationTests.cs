using System.Collections.Immutable;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.ModernWorkspace;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class ModernWorkspaceFoundationTests
{
    [Fact] public void PanePreferenceIsRepairedAndCannotResurrectDeniedPane()
    {
        var definition = new PaneDefinition(new("DETAILS"), PaneRole.SecondaryContent, defaultSize: 300, minSize: 200, maxSize: 500);
        var preference = new PanePreference(definition.PaneCode, false, 900, SelectedSecondaryContent: new("REMOVED"));
        var denied = PaneStateResolver.Resolve(definition, preference, UiAuthorizationState.Hidden, true, new HashSet<PaneCode>());
        Assert.False(denied.Visible); Assert.Equal(300, denied.CurrentSize); Assert.Null(denied.SelectedSecondaryContent);
    }

    [Fact] public void LazyPaneDoesNotConstructUntilRequested()
    {
        var count = 0; var pane = new LazyPaneContent<object>(() => { count++; return new(); });
        Assert.False(pane.IsCreated); Assert.Equal(0, count); Assert.Same(pane.GetOrCreate(), pane.GetOrCreate()); Assert.Equal(1, count);
    }

    [Fact] public void CollapsedPaneSurvivesWorkspaceRematerializationBySemanticKey()
    {
        var store = new WorkspacePaneSessionStateStore();
        var workspace = new WorkspaceCode("workspace-a");
        var definition = SecondaryPane();
        var firstRenderedControl = new object();

        var collapsed = store.SetCollapsed(workspace, definition, true, UiAuthorizationState.Enabled, true);
        var rematerializedControl = new object();
        var reactivated = store.Resolve(new WorkspaceCode("WORKSPACE-A"),
            new PaneDefinition(new("secondary"), PaneRole.SecondaryContent, defaultSize: 300),
            UiAuthorizationState.Enabled, true);

        Assert.True(collapsed.Collapsed);
        Assert.True(reactivated.Collapsed);
        Assert.NotSame(firstRenderedControl, rematerializedControl);
    }

    [Fact] public void ExpandedPaneSurvivesWorkspaceRematerialization()
    {
        var store = new WorkspacePaneSessionStateStore();
        var workspace = new WorkspaceCode("workspace-a");
        var definition = SecondaryPane();
        store.SetCollapsed(workspace, definition, true, UiAuthorizationState.Enabled, true);
        store.SetCollapsed(workspace, definition, false, UiAuthorizationState.Enabled, true);

        Assert.False(store.Resolve(workspace, definition, UiAuthorizationState.Enabled, true).Collapsed);
    }

    [Fact] public void StalePreferenceRepairsAndAuthorizationRemainsTheCeiling()
    {
        var store = new WorkspacePaneSessionStateStore();
        var workspace = new WorkspaceCode("workspace-a");
        var definition = SecondaryPane();
        store.SetPreference(workspace, new(definition.PaneCode, true, 9_999,
            SelectedSecondaryContent: new("REMOVED")));

        var denied = store.Resolve(workspace, definition, UiAuthorizationState.Hidden, true, new HashSet<PaneCode>());
        Assert.False(denied.Visible);
        Assert.False(denied.Collapsed);
        Assert.Equal(definition.DefaultSize, denied.CurrentSize);

        var allowedAgain = store.Resolve(workspace, definition, UiAuthorizationState.Enabled, true, new HashSet<PaneCode>());
        Assert.True(allowedAgain.Visible);
        Assert.True(allowedAgain.Collapsed);
        Assert.Equal(definition.MaxSize, allowedAgain.CurrentSize);
        Assert.Null(allowedAgain.SelectedSecondaryContent);
    }

    [Fact] public void ContextualActionsDisappearForInvalidSelectionAndReuseCommandCodes()
    {
        var definition = new ContextualActionDefinition("OPEN", "DOCUMENT.OPEN", ContextualActionPlacement.ContextualToolbar);
        Assert.Empty(ContextualActionResolver.Resolve(null, [definition], _ => UiAuthorizationState.Enabled));
        var result = ContextualActionResolver.Resolve(new("DOCUMENT", ["doc-1"], 1), [definition], _ => UiAuthorizationState.Enabled);
        Assert.Equal("DOCUMENT.OPEN", Assert.Single(result).Definition.CommandCode);
    }

    [Fact] public async Task OperationsAreGenerationSafeCapabilityDrivenAndReattachable()
    {
        var coordinator = new OperationCoordinator(maximumRetained: 5);
        var running = new OperationSnapshot("op-1", "IMPORT", "DEMO", OperationState.Running, "Import", Capabilities: new(CanCancel: true), Generation: 2);
        Assert.True(await coordinator.PublishAsync(running));
        Assert.False(await coordinator.PublishAsync(running with { State = OperationState.Pending, Generation = 1 }));
        Assert.Equal(OperationState.Running, coordinator.Reattach("op-1")!.State);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await coordinator.RetryAsync("op-1", new FakeOperationProvider()));
    }

    [Fact] public void DropNegotiationFailsClosedAndDoesNotMutatePayload()
    {
        var payload = new SemanticDragPayload(ResourceKind.Document, ["doc-1"], ImmutableDictionary<string, string>.Empty, DragOperation.Copy);
        var target = new DropTargetDefinition("ATTACH", [ResourceKind.Document], DragOperation.Copy);
        Assert.False(DropNegotiator.Negotiate(payload, target, UiAuthorizationState.Hidden, true, true).Accepted);
        Assert.True(DropNegotiator.Negotiate(payload, target, UiAuthorizationState.Enabled, true, true).Accepted);
        Assert.Equal("doc-1", Assert.Single(payload.SemanticIds));
    }

    [Fact] public void ReviewAndComposerKeepSemanticIdentitySeparateFromDisplayText()
    {
        var identity = new CompareIdentity("compare-1", "rev-a", "rev-b", "record-1");
        var diff = new StructuredDifference("FIELD_CODE", "Old", "New", DifferenceKind.Changed);
        Assert.Equal("FIELD_CODE", diff.FieldCode); Assert.Equal("record-1", identity.TargetSemanticId);
        var state = new ComposerRuntimeState("Tiếng Việt 😀", [new(ResourceKind.Document, "doc-1", "Safe document")], []);
        Assert.Equal("doc-1", Assert.Single(state.AttachedResources).SemanticResourceId);
    }

    [Theory]
    [InlineData(ContentPresentationState.Loading)] [InlineData(ContentPresentationState.Empty)]
    [InlineData(ContentPresentationState.FilteredEmpty)] [InlineData(ContentPresentationState.Unavailable)]
    [InlineData(ContentPresentationState.Offline)] [InlineData(ContentPresentationState.Unauthorized)]
    [InlineData(ContentPresentationState.Error)] [InlineData(ContentPresentationState.Ready)]
    public void StandardContentStatesArePresentationOnly(ContentPresentationState state) =>
        Assert.Equal(state, new ContentStatePresentation(state, "Safe message").State);

    private sealed class FakeOperationProvider : IOperationProvider
    {
        public ValueTask<OperationSnapshot?> GetAsync(string operationId, CancellationToken cancellationToken = default) => ValueTask.FromResult<OperationSnapshot?>(null);
        public ValueTask<OperationSnapshot> CancelAsync(string operationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<OperationSnapshot> RetryAsync(string operationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static PaneDefinition SecondaryPane() => new(new("SECONDARY"), PaneRole.SecondaryContent,
        defaultSize: 300, minSize: 200, maxSize: 500);
}
