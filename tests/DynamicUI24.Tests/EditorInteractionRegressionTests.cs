using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Automation;
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
    public void HelpIsLightweightLabelAffordanceAndDoesNotMutateValue()
    {
        var definition = new EditorDefinition(new("TEXT"), new("FIELD"), EditorValueType.String,
            helpContextCode: new("EDITOR.TEXT"));
        var state = new EditorRuntimeState(definition, "semantic value");
        var presenter = new AvaloniaEditorPresenter(definition, state,
            new EditorResolver().Resolve(definition, EditorPlatformCapabilities.AllNative));
        var help = presenter.GetLogicalDescendants().OfType<Button>().Single(x => Equals(x.Tag, "HELP"));

        Assert.IsType<EditorAffordanceSlot>(help.Content);
        Assert.IsType<EditorAffordanceSlot>(help.Content);
        Assert.Equal("Help", AutomationProperties.GetName(help));
        help.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal("semantic value", state.CandidateValue);
    }

    [Fact]
    public void SearchLookupAndMultiChoiceDropdownsStayInTheirFieldTrees()
    {
        var lookup = new EditorDefinition(new("SEARCH"), new("FIELD.SEARCH"), EditorValueType.LookupKey,
            EditorKind.SearchLookup);
        var multi = new EditorDefinition(new("MULTI"), new("FIELD.MULTI"), EditorValueType.MultiChoice,
            choices: [new("A", new("A"), "A")]);
        var lookupPresenter = new AvaloniaEditorPresenter(lookup, new(lookup),
            new EditorResolver().Resolve(lookup, EditorPlatformCapabilities.AllNative),
            lookupProvider: new EmptyLookupProvider());
        var multiPresenter = Present(multi);

        var lookupDropDown = lookupPresenter.GetLogicalDescendants().OfType<Border>()
            .Single(x => Equals(x.Tag, "LOOKUP_DROPDOWN"));
        Assert.NotNull(lookupDropDown.Child);
        Assert.Equal("LOOKUP_DROPDOWN", lookupDropDown.Tag);
        var scroller = Assert.Single(multiPresenter.GetLogicalDescendants().OfType<ScrollViewer>());
        Assert.Equal("MULTICHOICE_SCROLL", scroller.Tag);
        Assert.Equal(ScrollBarVisibility.Disabled, scroller.HorizontalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Auto, scroller.VerticalScrollBarVisibility);
        var optionList = Assert.IsType<StackPanel>(scroller.Content);
        Assert.True(double.IsNaN(optionList.Width));
        Assert.Equal(HorizontalAlignment.Stretch, optionList.HorizontalAlignment);
    }

    [Fact]
    public void MultiChoiceKeepsVerticalOverflowAvailableWithoutCreatingHorizontalExtentPolicy()
    {
        var choices = Enumerable.Range(1, 10)
            .Select(index => new EditorChoiceOption($"OPTION_{index}", new($"Option.{index}"),
                index == 1 ? new string('L', 200) : $"Option {index}"))
            .ToArray();
        var definition = new EditorDefinition(new("MULTI_LONG"), new("FIELD.MULTI_LONG"),
            EditorValueType.MultiChoice, choices: choices);
        var presenter = Present(definition);
        var scroller = Assert.Single(presenter.GetLogicalDescendants().OfType<ScrollViewer>());
        var optionList = Assert.IsType<StackPanel>(scroller.Content);

        Assert.Equal(ScrollBarVisibility.Disabled, scroller.HorizontalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Auto, scroller.VerticalScrollBarVisibility);
        Assert.True(choices.Length * EditorPresentationTokens.OptionRowHeight >
            EditorPresentationTokens.PopupMaxHeight);
        Assert.All(optionList.Children.OfType<Grid>(), row =>
            Assert.Equal(HorizontalAlignment.Stretch, row.HorizontalAlignment));
        Assert.All(optionList.GetLogicalDescendants().OfType<TextBlock>(), label =>
            Assert.Equal(TextTrimming.CharacterEllipsis, label.TextTrimming));
    }

    [Fact]
    public void DateTimeHasCompleteCompactTimeSurfaceAndClockAffordance()
    {
        var definition = new EditorDefinition(new("DATETIME"), new("FIELD.DATETIME"), EditorValueType.DateTime);
        var presenter = Present(definition);
        var time = presenter.NativeTextInputs.Single();

        Assert.Equal(double.NaN, time.Width);
        Assert.IsType<Grid>(presenter.NativeEditor);
        var surface = Assert.Single(presenter.GetLogicalDescendants().OfType<EditorSurface>());
        Assert.Same(time, surface.Content);
        Assert.Same(time, surface.ContentHost.Child);
        Assert.Equal(new Thickness(0), time.Padding);
        Assert.Same(surface.TrailingAffordance, surface.Child!.GetLogicalDescendants().OfType<EditorAffordanceSlot>().Single());
        Assert.True(surface.ClipToBounds);
        Assert.True(surface.OwnsBorder);
        var slot = Assert.Single(presenter.GetLogicalDescendants().OfType<EditorAffordanceSlot>(),
            x => x.Kind == EditorAffordanceKind.Clock);
        Assert.IsType<SemanticIcon>(Assert.Single(slot.Children));
        Assert.Equal(VerticalAlignment.Center, time.VerticalContentAlignment);
    }

    [Fact]
    public void SharedAffordanceGeometryCoversTimeMultiChoiceLookupAndHelp()
    {
        var multi = new EditorDefinition(new("MULTI"), new("FIELD.MULTI"), EditorValueType.MultiChoice,
            choices: [new("A", new("A"), "A")]);
        var lookup = new EditorDefinition(new("LOOKUP"), new("FIELD.LOOKUP"), EditorValueType.LookupKey,
            EditorKind.SearchLookup);
        var help = new EditorDefinition(new("TEXT"), new("FIELD.TEXT"), EditorValueType.String,
            helpContextCode: new("HELP.TEXT"));

        Assert.Contains(Present(multi).GetLogicalDescendants().OfType<EditorAffordanceSlot>(),
            x => x.Kind == EditorAffordanceKind.Dropdown);
        var lookupPresenter = new AvaloniaEditorPresenter(lookup, new(lookup),
            new EditorResolver().Resolve(lookup, EditorPlatformCapabilities.AllNative),
            lookupProvider: new EmptyLookupProvider());
        Assert.Contains(lookupPresenter.GetLogicalDescendants().OfType<EditorAffordanceSlot>(),
            x => x.Kind == EditorAffordanceKind.Search);
        Assert.Contains(Present(help).GetLogicalDescendants().OfType<EditorAffordanceSlot>(),
            x => x.Kind == EditorAffordanceKind.Help);
    }

    [Fact]
    public void MultiChoiceOptionRowSeparatesSharedCheckSlotFromLabel()
    {
        var definition = new EditorDefinition(new("MULTI"), new("FIELD.MULTI"), EditorValueType.MultiChoice,
            choices: [new("ONE", new("One"), "One")]);
        var presenter = Present(definition);
        var row = presenter.GetLogicalDescendants().OfType<Grid>()
            .Single(x => Equals(x.Tag, "MULTICHOICE_OPTION_ROW"));

        Assert.Equal(2, row.Children.Count);
        var slot = Assert.IsType<Grid>(row.Children[0]);
        var check = Assert.IsType<CheckBox>(Assert.Single(slot.Children));
        Assert.IsType<TextBlock>(row.Children[1]);
        Assert.Equal(1, Grid.GetColumn(row.Children[1]));
        Assert.Equal(HorizontalAlignment.Stretch, row.HorizontalAlignment);
        Assert.Equal(TextTrimming.CharacterEllipsis, Assert.IsType<TextBlock>(row.Children[1]).TextTrimming);
        Assert.Empty(slot.GetLogicalDescendants().OfType<EditorAffordanceSlot>());
        Assert.Contains(DynamicCheckBoxPresentation.PresentationClass, check.Classes);
        Assert.True(EditorPresentationTokens.NativeCheckSize >= 20);
        Assert.True(EditorPresentationTokens.LeadingSlotWidth >=
            EditorPresentationTokens.NativeCheckSize + EditorPresentationTokens.InlineGap);
        Assert.True(check.Focusable);
        Assert.Equal("One", AutomationProperties.GetName(check));
    }

    [Fact]
    public void BooleanAndMultiChoiceReuseNativeCheckBoxSemanticsWithOneSharedVisualTheme()
    {
        var boolean = Present(new EditorDefinition(new("BOOL"), new("FIELD.BOOL"), EditorValueType.Boolean));
        var multi = Present(new EditorDefinition(new("MULTI"), new("FIELD.MULTI"), EditorValueType.MultiChoice,
            choices: [new("A", new("A"), "A")]));
        var booleanCheck = Assert.IsType<CheckBox>(boolean.NativeEditor);
        var multiCheck = Assert.Single(multi.GetLogicalDescendants().OfType<CheckBox>());

        Assert.All(new[] { booleanCheck, multiCheck }, check =>
        {
            Assert.Contains(DynamicCheckBoxPresentation.PresentationClass, check.Classes);
            Assert.True(check.Focusable);
            Assert.IsAssignableFrom<CheckBox>(check);
        });
        multiCheck.IsChecked = true;
        Assert.True(multiCheck.IsChecked);
    }

    [Fact]
    public void StandaloneTimeAndDateTimeUseTheSameCanonicalClockAffordance()
    {
        var time = Present(new EditorDefinition(new("TIME"), new("TIME.FIELD"), EditorValueType.Time));
        var dateTime = Present(new EditorDefinition(new("DATETIME"), new("DATETIME.FIELD"), EditorValueType.DateTime));
        var standaloneSlot = Assert.Single(time.GetLogicalDescendants().OfType<EditorAffordanceSlot>(),
            x => x.Kind == EditorAffordanceKind.Clock);
        var dateTimeSlot = Assert.Single(dateTime.GetLogicalDescendants().OfType<EditorAffordanceSlot>(),
            x => x.Kind == EditorAffordanceKind.Clock);

        Assert.Equal(StandardIconKeys.Clock, standaloneSlot.SemanticIconKey);
        Assert.Equal(standaloneSlot.SemanticIconKey, dateTimeSlot.SemanticIconKey);
        Assert.Equal(standaloneSlot.GetType(), dateTimeSlot.GetType());
        Assert.IsType<EditorSurface>(standaloneSlot.Parent?.Parent);
        Assert.IsType<EditorSurface>(dateTimeSlot.Parent?.Parent);
        Assert.All(new[] { standaloneSlot, dateTimeSlot }, slot =>
            Assert.IsType<SemanticIcon>(Assert.Single(slot.Children)));
    }

    [Fact]
    public void LookupAndMultiChoiceKeepTheirTrailingSlotsInsideOneBorderOwningSurface()
    {
        var multi = Present(new EditorDefinition(new("MULTI"), new("FIELD.MULTI"), EditorValueType.MultiChoice,
            choices: [new("A", new("A"), "A")]));
        var lookupDefinition = new EditorDefinition(new("LOOKUP"), new("FIELD.LOOKUP"), EditorValueType.LookupKey,
            EditorKind.SearchLookup);
        var lookup = new AvaloniaEditorPresenter(lookupDefinition, new EditorRuntimeState(lookupDefinition),
            new EditorResolver().Resolve(lookupDefinition, EditorPlatformCapabilities.AllNative), lookupProvider: new EmptyLookupProvider());

        foreach (var presenter in new[] { multi, lookup })
        {
            var surface = Assert.Single(presenter.GetLogicalDescendants().OfType<EditorSurface>());
            Assert.Same(surface, surface.TrailingAffordance.Parent?.Parent);
            Assert.True(surface.ClipToBounds);
            Assert.True(surface.OwnsBorder);
        }
    }

    [Fact]
    public void MultiChoiceCanCloseReopenAndOwnerLifecycleClosesItWithoutChangingSelection()
    {
        var definition = new EditorDefinition(new("MULTI"), new("FIELD.MULTI"), EditorValueType.MultiChoice,
            choices: [new("A", new("A"), "Alpha"), new("B", new("B"), "Beta")]);
        var state = new EditorRuntimeState(definition, new[] { "A" });
        var presenter = new AvaloniaEditorPresenter(definition, state,
            new EditorResolver().Resolve(definition, EditorPlatformCapabilities.AllNative));
        var trigger = presenter.GetLogicalDescendants().OfType<Button>()
            .Single(x => Equals(x.Tag, "MULTICHOICE_TRIGGER"));

        trigger.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(presenter.IsMultiChoiceOpen);
        presenter.CloseTransientSurfaces();
        Assert.False(presenter.IsMultiChoiceOpen);
        trigger.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(presenter.IsMultiChoiceOpen);
        presenter.RefreshLocalizedPresentation(CultureInfo.GetCultureInfo("vi-VN"), key => key.Value);
        Assert.Equal(new[] { "A" }, Assert.IsType<string[]>(state.CandidateValue));
        presenter.CloseTransientSurfaces();
        Assert.False(presenter.IsMultiChoiceOpen);
    }

    [Fact]
    public void SharedFormLayoutEnforcesReadableWidthAndResponsiveFieldGrouping()
    {
        var form = new UniversalFormPanel();
        var section = new UniversalFormSection("Dates");
        section.AddField(new TextBox());
        section.AddField(new TextBox());
        form.Children.Add(section);

        Assert.Equal(EditorPresentationTokens.FormMaxReadableWidth, form.MaxWidth);
        Assert.Equal(2, section.Fields.Children.Count);
        Assert.IsType<WrapPanel>(section.Fields);
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

    private sealed class EmptyLookupProvider : IEditorLookupProvider
    {
        public string ProviderCode => "EMPTY";
        public ValueTask<EditorLookupResult> QueryAsync(EditorLookupRequest request) => ValueTask.FromResult(
            new EditorLookupResult([], null, 0, request.Generation, request.CompanyId, request.ContextRevision,
                EditorLookupStatus.Empty));
    }
}
