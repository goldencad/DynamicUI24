using Avalonia.Controls;
using DynamicUI24.Avalonia;
using DynamicUI24.Demo;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Avalonia.Presentation.Editors;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using Xunit;

namespace DynamicUI24.Tests;

[Collection("Avalonia native UI")]
public sealed class EditorLocalizationCrashTests
{
    [Fact]
    public void EditorDemoCultureSwitchDoesNotReparentControls()
    {
        var localization = new DictionaryLocalizationService("en-US");
        var workspace = new DemoEditorWorkspace(localization);
        var controls = workspace.Presenters.Select(x => x.NativeEditor).ToArray();
        var text = workspace.Presenters.Single(x => x.Definition.EditorCode.Value == "TEXT");
        var choice = workspace.Presenters.Single(x => x.Definition.EditorCode.Value == "CHOICE");
        var textBox = Assert.IsType<TextBox>(text.NativeEditor);
        textBox.Text = "Tiếng Việt 🌏";
        text.State.SetCandidate(textBox.Text, textBox.Text);
        textBox.CaretIndex = 5;
        textBox.SelectionStart = 2;
        textBox.SelectionEnd = 7;
        text.State.SetValidation(EditorValidationResult.Error("VISIBLE", text.Definition.ConsumerSemanticId, "Safe error"));
        var comboBox = Assert.IsType<ComboBox>(choice.NativeEditor);
        comboBox.SelectedItem = choice.Definition.Choices.Single(x => x.SemanticOptionId == "BETA");
        var selectedOption = comboBox.SelectedItem;

        foreach (var culture in new[] { "vi-VN", "en-US", "vi-VN", "en-US", "vi-VN", "en-US" })
            workspace.CultureSelector.SelectedItem = culture;

        Assert.Equal(controls, workspace.Presenters.Select(x => x.NativeEditor));
        Assert.Equal("Tiếng Việt 🌏", textBox.Text);
        Assert.Equal(5, textBox.CaretIndex);
        Assert.Equal(2, textBox.SelectionStart);
        Assert.Equal(7, textBox.SelectionEnd);
        Assert.Equal("Tiếng Việt 🌏", text.State.CandidateValue);
        Assert.Equal("VISIBLE", text.State.Validation.MessageCode);
        Assert.Equal("BETA", choice.State.CandidateValue);
        Assert.Same(selectedOption, comboBox.SelectedItem);
        Assert.Equal(0, workspace.LookupRequestCount);
    }

    [Fact]
    public void GlobalLocalizationCallbackRetainsLocalizationAwareWorkspaceInstance()
    {
        var localization = new DictionaryLocalizationService("en-US");
        var registry = new TemplateRegistry();
        Assert.True(registry.Register(new EditorTestTemplate()).IsSuccess);
        var host = new DynamicWorkspaceHost(registry, localization);
        var workspace = new DemoEditorWorkspace(localization);
        var factoryCalls = 0;
        host.RegisterViewFactory(EditorTestTemplate.Code, _ => { factoryCalls++; return workspace; });
        host.ShowWorkspace(new("editor-test", "Editor Test", EditorTestTemplate.Code));

        Assert.True(localization.TrySetCulture("vi-VN"));
        Assert.Same(workspace, host.Content);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public void EditorWorkspaceReactivationReusesItsNativeControlTreeAndRuntimeState()
    {
        var localization = new DictionaryLocalizationService("en-US");
        var registry = new TemplateRegistry();
        Assert.True(registry.Register(new EditorTestTemplate()).IsSuccess);
        var host = new DynamicWorkspaceHost(registry, localization);
        var workspace = new DemoEditorWorkspace(localization);
        var factoryCalls = 0;
        host.RegisterViewFactory(EditorTestTemplate.Code, _ => { factoryCalls++; return workspace; });
        var definition = new WorkspaceDefinition("editor-test", "Editor Test", EditorTestTemplate.Code);
        host.ShowWorkspace(definition);
        var text = workspace.Presenters.Single(x => x.Definition.EditorCode.Value == "TEXT");
        var textBox = Assert.IsType<TextBox>(text.NativeEditor);
        var coldActivation = NativeEditorInputOwnership.Snapshot(textBox);
        textBox.Text = "First activation candidate: Tiếng Việt 🌏";
        text.State.SetCandidate(textBox.Text, textBox.Text);

        host.Clear();
        host.ShowWorkspace(definition);

        Assert.Same(workspace, host.Content);
        Assert.Same(textBox, text.NativeEditor);
        Assert.Equal("First activation candidate: Tiếng Việt 🌏", textBox.Text);
        Assert.Equal("First activation candidate: Tiếng Việt 🌏", text.State.CandidateValue);
        Assert.Equal(0, workspace.LookupRequestCount);
        Assert.Equal(1, factoryCalls);
        Assert.Equal(2, workspace.WorkspaceActivationCount);
        Assert.Equal(1, workspace.WorkspaceDeactivationCount);
        Assert.Equal(1, coldActivation.WorkspaceActivationCount);
        var warmActivation = NativeEditorInputOwnership.Snapshot(textBox);
        Assert.Equal(2, warmActivation.WorkspaceActivationCount);
        Assert.Equal(coldActivation.TextBoxIdentity, warmActivation.TextBoxIdentity);
        Assert.Equal(coldActivation.VisualRootIdentity, warmActivation.VisualRootIdentity);
        Assert.Equal(coldActivation.TopLevelIdentity, warmActivation.TopLevelIdentity);
    }

    [Fact]
    public void FirstWorkspaceActivationCallbackRunsAfterContentAssignmentWithoutRematerialization()
    {
        var localization = new DictionaryLocalizationService("en-US");
        var registry = new TemplateRegistry();
        Assert.True(registry.Register(new EditorTestTemplate()).IsSuccess);
        var host = new DynamicWorkspaceHost(registry, localization);
        var view = new ActivationOrderView(() => host.Content);
        host.RegisterViewFactory(EditorTestTemplate.Code, _ => view);

        host.ShowWorkspace(new("editor-test", "Editor Test", EditorTestTemplate.Code));

        Assert.Equal(1, view.ActivationCount);
        Assert.True(view.WasAssignedWhenActivated);
        Assert.Same(view, host.Content);
    }

    private sealed class EditorTestTemplate : DynamicTemplateBase
    {
        public static TemplateCode Code { get; } = new("EDITOR_TEST");
        public override TemplateCode TemplateCode => Code;
        public override string ModuleName => "TEST";
        public override IReadOnlyCollection<TemplateCapability> SupportedCapabilities { get; } = [];
    }

    private sealed class ActivationOrderView(Func<object?> currentContent) : Control, IRuntimeWorkspaceActivationAware
    {
        public int ActivationCount { get; private set; }
        public bool WasAssignedWhenActivated { get; private set; }
        public void WorkspaceActivated()
        {
            ActivationCount++;
            WasAssignedWhenActivated = ReferenceEquals(this, currentContent());
        }
        public void WorkspaceDeactivated() { }
    }
}
