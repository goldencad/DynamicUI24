using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using DynamicUI24.Avalonia;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Avalonia.Presentation.Editors;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Demo;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

[Collection("Avalonia native UI")]
public sealed class EditorInteractionRegressionTests
{
    [Fact]
    public async Task ActualDemoRegistryContainsBothEmbeddedSemanticCommands()
    {
        var registry = new ActionCommandRegistry();
        DemoEditorActions.Register(registry);

        Assert.Equal("Hyperlink action invoked", (await registry.ExecuteAsync(
            DemoEditorActions.HyperlinkOpen, Context())).Message);
        Assert.Equal("Browse action invoked", (await registry.ExecuteAsync(
            DemoEditorActions.ButtonEditBrowse, Context())).Message);
    }

    [Theory]
    [InlineData("HYPERLINK", DemoEditorActions.HyperlinkOpen, "Hyperlink action invoked")]
    [InlineData("BUTTON_EDIT", DemoEditorActions.ButtonEditBrowse, "Browse action invoked")]
    public void DemoPointerClickDispatchesThroughSharedRegistryAndShowsLocalFeedback(
        string editorCode, string actionCode, string expected)
    {
        var registry = new ActionCommandRegistry();
        DemoEditorActions.Register(registry);
        var workspace = new DemoEditorWorkspace(new DictionaryLocalizationService("en-US"), registry, Context);
        var presenter = workspace.Presenters.Single(x => x.Definition.EditorCode.Value == editorCode);
        var button = presenter.GetLogicalDescendants().OfType<Button>().Single(x => Equals(x.Tag, actionCode) ||
            editorCode == "HYPERLINK" && ReferenceEquals(x, presenter.NativeEditor));

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal(expected, workspace.ActionStatusText);
        Assert.Equal(expected, presenter.ActionFeedbackText);
    }

    [Fact]
    public void DeniedBrowseActionDoesNotDisableEditableButtonEditText()
    {
        var definition = new EditorDefinition(new("BUTTON"), new("FIELD"), EditorValueType.String,
            EditorKind.ButtonEdit, actions: [new("BROWSE", EditorActionKind.Browse, new("Browse"),
                Requirement: new(new PermissionCode("BROWSE")))]);
        var resolution = new EditorResolver().Resolve(definition, EditorPlatformCapabilities.AllNative);
        var presenter = new AvaloniaEditorPresenter(definition, new(definition), resolution, CultureInfo.InvariantCulture);
        var text = Assert.IsType<TextBox>(presenter.NativeEditor);

        Assert.Equal(EditorInteractionState.Editable, resolution.InteractionState);
        Assert.True(text.IsEnabled);
        Assert.False(text.IsReadOnly);
        Assert.True(text.Focusable);
        text.Text = "still editable";
        Assert.Equal("still editable", text.Text);
    }

    [Fact]
    public void DeniedBrowsePointerActionDoesNotExecuteAndKeepsButtonEditTextEditable()
    {
        var calls = 0;
        var registry = new ActionCommandRegistry();
        registry.Register("BROWSE", (_, _) => { calls++; return Task.FromResult(ActionCommandResult.Success()); });
        var action = new EditorActionDefinition("BROWSE", EditorActionKind.Browse, new("Browse"),
            Requirement: new(new PermissionCode("BROWSE")));
        var definition = new EditorDefinition(new("BUTTON"), new("FIELD"), EditorValueType.String,
            EditorKind.ButtonEdit, actions: [action]);
        var resolution = new EditorResolver().Resolve(definition, EditorPlatformCapabilities.AllNative);
        var presenter = new AvaloniaEditorPresenter(definition, new(definition), resolution, CultureInfo.InvariantCulture);
        var dispatcher = new EditorActionDispatcher(registry);
        presenter.ActionInvoked += async (_, e) => await dispatcher.DispatchAsync(e.Action, resolution, Context());
        var button = presenter.GetLogicalDescendants().OfType<Button>().Single(x => Equals(x.Tag, "BROWSE"));
        var text = Assert.IsType<TextBox>(presenter.NativeEditor);

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        text.Text = "still editable after denied action";

        Assert.Equal(0, calls);
        Assert.True(text.IsEnabled);
        Assert.False(text.IsReadOnly);
        Assert.Equal("still editable after denied action", text.Text);
    }

    [Fact]
    public void EditorReadOnlyAndDisabledStatesRemainDefinitionScoped()
    {
        var readOnly = Definition(isReadOnly: true);
        var disabled = Definition(isDisabled: true);
        var readOnlyPresenter = Present(readOnly);
        var disabledPresenter = Present(disabled);
        Assert.True(Assert.IsType<TextBox>(readOnlyPresenter.NativeEditor).IsReadOnly);
        Assert.False(Assert.IsType<TextBox>(disabledPresenter.NativeEditor).IsEnabled);
    }

    [Fact]
    public void LookupPointerSelectionCommitsSemanticIdentityNotVisualIndex()
    {
        var state = LookupState(out var record44, out _);
        Assert.True(state.SetActive(record44));
        var selected = state.CommitActive();
        Assert.Equal("REC-000044", selected!.SemanticOptionId);
        Assert.Equal("Record 000044", selected.SafeDisplayText);
    }

    [Fact]
    public void LookupKeyboardSelectionCommitsActiveSemanticIdentity()
    {
        var state = LookupState(out _, out _);
        Assert.True(state.MoveActive(1));
        var selected = state.CommitActive();
        Assert.Equal("REC-000045", selected!.SemanticOptionId);
    }

    [Fact]
    public void LookupLocalizationChangesDisplayOnlyAndKeepsSemanticSelection()
    {
        var state = LookupState(out var record44, out _);
        state.SetActive(record44); state.CommitActive();
        state.SetItems([new("REC-000044", "Bản ghi 000044"), new("REC-000045", "Bản ghi 000045")]);
        Assert.True(state.RestoreSemanticSelection("REC-000044"));
        Assert.Equal("REC-000044", state.Selected!.SemanticOptionId);
        Assert.Equal("Bản ghi 000044", state.Selected.SafeDisplayText);
    }

    [Fact]
    public void LookupSelectionRemainsBounded()
    {
        var state = new EditorLookupSelectionState();
        state.SetItems(Enumerable.Range(0, 100_000).Select(x => new EditorLookupOption($"ID-{x}", $"Item {x}")));
        Assert.Equal(EditorLookupRequest.MaximumWindowSize, state.Items.Count);
    }

    private static EditorLookupSelectionState LookupState(out EditorLookupOption record44, out EditorLookupOption record45)
    {
        record44 = new("REC-000044", "Record 000044"); record45 = new("REC-000045", "Record 000045");
        var state = new EditorLookupSelectionState(); state.SetItems([record44, record45]); return state;
    }

    private static EditorDefinition Definition(bool isReadOnly = false, bool isDisabled = false) =>
        new(new("TEXT"), new("FIELD"), EditorValueType.String, isReadOnly: isReadOnly, isDisabled: isDisabled);
    private static AvaloniaEditorPresenter Present(EditorDefinition definition) => new(definition, new(definition),
        new EditorResolver().Resolve(definition, EditorPlatformCapabilities.AllNative), CultureInfo.InvariantCulture);

    private static ActionCommandExecutionContext Context()
    {
        var company = new CompanyDescriptor(new("demo"), "DEMO", "Demo");
        var workspace = new WorkspaceDefinition("editor-demo", "Editor Demo", new TemplateCode("DASHBOARD"));
        return new(new(company, workspace, workspace.TemplateCode,
            new(new("user"), company.CompanyId, [], [], "r1"), new(0), PresentationState.Ready));
    }
}
