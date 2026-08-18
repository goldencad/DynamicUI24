using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation.Editors;

/// <summary>
/// Lightweight native-control presenter. Actipro 25.2 Pro has no general data-editor suite;
/// native Avalonia editors preserve OS Unicode, caret, selection, clipboard and IME behavior.
/// </summary>
public sealed class AvaloniaEditorPresenter : UserControl
{
    private readonly StackPanel root = new() { Spacing = 4 };
    private readonly TextBlock label = new();
    private readonly TextBlock message = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock actionFeedback = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap, IsVisible = false };
    private readonly EditorValidator validator;
    private readonly Dictionary<EditorActionDefinition, Button> actionButtons = new();
    private readonly List<TextBox> nativeTextInputs = [];
    private Control? editor;
    private CancellationTokenSource? lookupCancellation;
    private readonly EffectiveAuthorizationContext? authorization;

    public AvaloniaEditorPresenter(EditorDefinition definition, EditorRuntimeState state,
        EditorResolution resolution, CultureInfo? culture = null, EditorValidator? validator = null,
        IEditorLookupProvider? lookupProvider = null, EffectiveAuthorizationContext? authorization = null)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Resolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
        Culture = culture ?? CultureInfo.CurrentCulture;
        this.validator = validator ?? new EditorValidator();
        this.authorization = authorization;
        if (resolution.InteractionState == EditorInteractionState.Hidden) { IsVisible = false; Content = root; return; }
        BuildStableVisualTree(lookupProvider);
        Content = root;
    }

    public EditorDefinition Definition { get; }
    public EditorRuntimeState State { get; }
    public EditorResolution Resolution { get; }
    public CultureInfo Culture { get; private set; }
    public Control? NativeEditor => editor;
    public IReadOnlyList<TextBox> NativeTextInputs => nativeTextInputs;
    public string ActionFeedbackText => actionFeedback.Text ?? string.Empty;
    public event EventHandler? CandidateChanged;
    public event EventHandler? Committed;
    public event EventHandler<EditorActionInvokedEventArgs>? ActionInvoked;

    /// <summary>Shows a safe result supplied by the semantic action owner beside the invoking editor.</summary>
    public void ShowActionFeedback(string safeMessage)
    {
        actionFeedback.Text = safeMessage ?? string.Empty;
        actionFeedback.IsVisible = !string.IsNullOrWhiteSpace(safeMessage);
        AutomationProperties.SetLiveSetting(actionFeedback, AutomationLiveSetting.Polite);
    }

    public async ValueTask<bool> CommitAsync(CancellationToken cancellationToken = default)
    {
        CaptureCandidate();
        var result = await validator.ValidateAsync(new(Definition, State.CandidateValue,
            new Dictionary<EditorSemanticId, object?> { [Definition.ConsumerSemanticId] = State.CandidateValue }), cancellationToken);
        State.SetValidation(result); UpdateMessage();
        if (!State.Commit(result)) return false;
        Committed?.Invoke(this, EventArgs.Empty); return true;
    }

    public void Cancel()
    {
        State.Cancel(); ApplyStateToControl(); UpdateMessage();
    }

    public void ChangeCulture(CultureInfo culture)
    {
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
        // Never rewrite a focused TextBox: its Unicode text, composition, caret and selection belong to native input.
        if (editor is NumericUpDown number) number.NumberFormat = Culture.NumberFormat;
    }

    public void RefreshLocalizedPresentation(CultureInfo culture, Func<LocalizationKey, string> localize)
    {
        ArgumentNullException.ThrowIfNull(localize);
        ChangeCulture(culture);
        label.Text = localize(Definition.Chrome.LabelKey ?? new(Definition.EditorCode.Value));
        if (Definition.Chrome.ShowRequiredIndicator && Definition.Validation.IsRequired) label.Text += " *";
        AutomationProperties.SetName(this, label.Text);
        if (editor is not null) AutomationProperties.SetName(editor, label.Text);
        if (editor is TextBox text) text.Watermark = Definition.Chrome.PlaceholderKey is { } placeholder ? localize(placeholder) : null;
        foreach (var (action, button) in actionButtons) button.Content = localize(action.LabelKey);
        UpdateMessage(localize);
    }

    private void BuildStableVisualTree(IEditorLookupProvider? lookupProvider)
    {
        label.Text = Definition.Chrome.LabelKey?.Value ?? Definition.EditorCode.Value;
        if (Definition.Chrome.ShowRequiredIndicator && Definition.Validation.IsRequired) label.Text += " *";
        AutomationProperties.SetName(this, label.Text);
        root.Children.Add(label);
        editor = CreateNativeEditor(lookupProvider);
        editor.IsEnabled = Resolution.InteractionState != EditorInteractionState.Disabled;
        AutomationProperties.SetName(editor, label.Text);
        AutomationProperties.SetHelpText(editor, Definition.Chrome.HelperTextKey?.Value ?? string.Empty);
        ToolTip.SetTip(editor, Definition.SensitiveContent.AllowTooltipRawValue ? Definition.Chrome.Tooltip : null);
        root.Children.Add(BuildEditorWithActions(editor));
        message.Text = Definition.Chrome.HelperTextKey?.Value ?? string.Empty;
        root.Children.Add(message);
        root.Children.Add(actionFeedback);
    }

    private Control BuildEditorWithActions(Control native)
    {
        var embeddedActions = Resolution.Kind == EditorKind.Hyperlink
            ? Definition.Actions.Where(x => x.Kind != EditorActionKind.Open).ToArray()
            : Definition.Actions.ToArray();
        if (embeddedActions.Length == 0 && Definition.HelpContextCode is null) return native;
        var panel = new Grid { ColumnDefinitions = new("*,Auto") };
        panel.Children.Add(native);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        Grid.SetColumn(actions, 1);
        foreach (var action in embeddedActions)
        {
            var button = new Button { Content = action.LabelKey.Value, Tag = action.ActionCode,
                IsEnabled = CanExecuteAction(action) };
            button.Click += (_, _) => ActionInvoked?.Invoke(this, new(action));
            actions.Children.Add(button);
            actionButtons.Add(action, button);
        }
        if (Definition.HelpContextCode is not null)
        {
            var help = new Button { Content = "?" };
            help.Click += (_, _) => ActionInvoked?.Invoke(this, new(new("HELP", EditorActionKind.Help,
                new("Editor.Help"))));
            actions.Children.Add(help);
        }
        panel.Children.Add(actions); return panel;
    }

    private Control CreateNativeEditor(IEditorLookupProvider? lookupProvider) => Resolution.Kind switch
    {
        EditorKind.Boolean => CreateBoolean(),
        EditorKind.Date => CreateDate(),
        EditorKind.Time => CreateTime(),
        EditorKind.DateTime => CreateDateTime(),
        EditorKind.DateRange => CreateDateRange(),
        EditorKind.Choice => CreateChoice(),
        EditorKind.MultiChoice => CreateMultiChoiceSeam(),
        EditorKind.Lookup or EditorKind.SearchLookup => CreateLookup(lookupProvider),
        EditorKind.TreeLookup => CreateDeferred("Tree lookup presentation is deferred."),
        EditorKind.Hyperlink => CreateHyperlink(),
        EditorKind.Integer or EditorKind.Decimal or EditorKind.Currency or EditorKind.Percentage => CreateNumeric(),
        _ => CreateText(),
    };

    private TextBox CreateText()
    {
        var text = new TextBox
        {
            Text = EditorValueFormatter.Format(State.CandidateValue, Definition, Culture),
            Watermark = Definition.Chrome.PlaceholderKey?.Value,
            AcceptsReturn = Resolution.Kind == EditorKind.MultilineText,
            TextWrapping = Definition.WrapText ? TextWrapping.Wrap : TextWrapping.NoWrap,
            MaxLength = Definition.MaxLength ?? 0,
            IsReadOnly = Resolution.InteractionState == EditorInteractionState.ReadOnly,
            Focusable = true,
            MinHeight = Resolution.Kind == EditorKind.MultilineText ? 84 : 0,
        };
        if (Resolution.Kind == EditorKind.Password) text.PasswordChar = '●';
        NativeEditorInputOwnership.Enable(text);
        nativeTextInputs.Add(text);
        // TextChanged can represent native IME pre-edit. Promote only at an explicit commit boundary.
        if (Resolution.Kind == EditorKind.Password) text.KeyDown += (_, e) =>
        {
            if ((e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta)) &&
                e.Key is Key.C or Key.X)
                e.Handled = true;
        };
        if (Definition.CommitPolicy == EditorCommitPolicy.OnEnter && !text.AcceptsReturn)
            text.KeyDown += async (_, e) =>
            {
                if (e.Key != Key.Enter) return;
                await CommitAsync(); e.Handled = true;
            };
        if (Resolution.Kind == EditorKind.Password) text.ContextMenu = new ContextMenu { ItemsSource = Array.Empty<object>() };
        return text;
    }

    private NumericUpDown CreateNumeric()
    {
        var numeric = new NumericUpDown { Minimum = Definition.Minimum ?? decimal.MinValue, Maximum = Definition.Maximum ?? decimal.MaxValue,
            Increment = Definition.Increment ?? 1, IsReadOnly = Resolution.InteractionState == EditorInteractionState.ReadOnly,
            NumberFormat = Culture.NumberFormat, FormatString = Definition.Formatting.Format ?? string.Empty,
            Value = State.CandidateValue is null ? null : Convert.ToDecimal(State.CandidateValue, CultureInfo.InvariantCulture) };
        InputMethod.SetIsInputMethodEnabled(numeric, true);
        numeric.ValueChanged += (_, _) => { State.SetCandidate(numeric.Value); CandidateChanged?.Invoke(this, EventArgs.Empty); };
        return numeric;
    }

    private CheckBox CreateBoolean()
    {
        var check = new CheckBox { IsChecked = State.CandidateValue as bool?,
            IsThreeState = Definition.AllowsNull, IsEnabled = Resolution.InteractionState == EditorInteractionState.Editable };
        check.IsCheckedChanged += (_, _) => { State.SetCandidate(check.IsChecked); CandidateChanged?.Invoke(this, EventArgs.Empty); };
        return check;
    }

    private DatePicker CreateDate()
    {
        var date = new DatePicker { SelectedDate = State.CandidateValue is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : State.CandidateValue as DateTime? };
        date.SelectedDateChanged += (_, _) => { State.SetCandidate(date.SelectedDate is { } value ? DateOnly.FromDateTime(value.DateTime) : null); CandidateChanged?.Invoke(this, EventArgs.Empty); };
        return date;
    }

    private TimePicker CreateTime()
    {
        var time = new TimePicker { SelectedTime = State.CandidateValue is TimeOnly t ? t.ToTimeSpan() : State.CandidateValue as TimeSpan? };
        time.SelectedTimeChanged += (_, _) => { State.SetCandidate(time.SelectedTime is { } value ? TimeOnly.FromTimeSpan(value) : null); CandidateChanged?.Invoke(this, EventArgs.Empty); };
        return time;
    }

    private Control CreateDateTime()
    {
        var value = State.CandidateValue as DateTime?;
        var panel = new Grid { ColumnDefinitions = new("*,*") };
        var date = new DatePicker { SelectedDate = value };
        var time = new TimePicker { SelectedTime = value?.TimeOfDay }; Grid.SetColumn(time, 1);
        void Changed() { if (date.SelectedDate is { } d && time.SelectedTime is { } t) State.SetCandidate(d.Date + t); CandidateChanged?.Invoke(this, EventArgs.Empty); }
        date.SelectedDateChanged += (_, _) => Changed(); time.SelectedTimeChanged += (_, _) => Changed();
        panel.Children.Add(date); panel.Children.Add(time); return panel;
    }

    private Control CreateDateRange()
    {
        var range = State.CandidateValue is DateRangeValue r ? r : default;
        var panel = new Grid { ColumnDefinitions = new("*,*") };
        var start = new DatePicker { SelectedDate = range.Start?.ToDateTime(TimeOnly.MinValue) };
        var end = new DatePicker { SelectedDate = range.End?.ToDateTime(TimeOnly.MinValue) }; Grid.SetColumn(end, 1);
        void Changed() { State.SetCandidate(new DateRangeValue(start.SelectedDate is { } s ? DateOnly.FromDateTime(s.DateTime) : null,
            end.SelectedDate is { } e ? DateOnly.FromDateTime(e.DateTime) : null)); CandidateChanged?.Invoke(this, EventArgs.Empty); }
        start.SelectedDateChanged += (_, _) => Changed(); end.SelectedDateChanged += (_, _) => Changed();
        panel.Children.Add(start); panel.Children.Add(end); return panel;
    }

    private ComboBox CreateChoice()
    {
        var combo = new ComboBox { ItemsSource = Definition.Choices, SelectedItem = Definition.Choices.FirstOrDefault(x => x.SemanticOptionId == State.CandidateValue?.ToString()),
            IsEnabled = Resolution.InteractionState == EditorInteractionState.Editable };
        combo.SelectionChanged += (_, _) => { State.SetCandidate((combo.SelectedItem as EditorChoiceOption)?.SemanticOptionId); CandidateChanged?.Invoke(this, EventArgs.Empty); };
        return combo;
    }

    private Control CreateLookup(IEditorLookupProvider? provider)
    {
        if (provider is null) return CreateDeferred("Lookup provider is not registered.");
        var panel = new StackPanel { Spacing = 2 };
        var query = new TextBox { Watermark = Definition.Chrome.PlaceholderKey?.Value ?? "Search" };
        NativeEditorInputOwnership.Enable(query);
        nativeTextInputs.Add(query);
        var selectedDisplay = new TextBlock { Text = State.CandidateValue is EditorLookupSelection selected
            ? selected.SafeDisplayText : "No selection" };
        var results = new ListBox { MaxHeight = 180, IsVisible = false };
        var coordinator = new EditorLookupCoordinator();
        var selection = new EditorLookupSelectionState();
        void CommitSelection()
        {
            if (selection.CommitActive() is not { } committed) return;
            State.SetCandidate(committed);
            selectedDisplay.Text = committed.SafeDisplayText;
            results.IsVisible = false;
            CandidateChanged?.Invoke(this, EventArgs.Empty);
        }
        query.TextChanged += async (_, _) =>
        {
            lookupCancellation?.Cancel(); lookupCancellation?.Dispose(); lookupCancellation = new();
            var token = lookupCancellation.Token;
            try
            {
                if (await coordinator.QueryAsync(provider, Definition.EditorCode, Definition.ConsumerSemanticId,
                    query.Text ?? string.Empty, 50, cancellationToken: token))
                {
                    selection.SetItems(coordinator.Items);
                    if (State.CandidateValue is EditorLookupSelection current)
                        selection.RestoreSemanticSelection(current.SemanticOptionId);
                    results.ItemsSource = selection.Items;
                    results.SelectedIndex = selection.ActiveIndex;
                    results.IsVisible = selection.Items.Count > 0;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        };
        query.GotFocus += (_, _) => results.IsVisible = selection.Items.Count > 0;
        query.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Down || selection.Items.Count == 0) return;
            results.IsVisible = true; results.SelectedIndex = Math.Max(0, selection.ActiveIndex); results.Focus(); e.Handled = true;
        };
        results.SelectionChanged += (_, _) => selection.SetActive(results.SelectedItem as EditorLookupOption);
        results.PointerReleased += (_, _) => CommitSelection();
        results.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { CommitSelection(); e.Handled = true; }
            else if (e.Key == Key.Escape) { results.IsVisible = false; query.Focus(); e.Handled = true; }
        };
        panel.Children.Add(selectedDisplay); panel.Children.Add(query); panel.Children.Add(results); return panel;
    }

    private Control CreateMultiChoiceSeam()
    {
        var selected = (State.CandidateValue as IEnumerable<string> ?? []).ToHashSet(StringComparer.Ordinal);
        var panel = new StackPanel { Spacing = 2 };
        foreach (var option in Definition.Choices)
        {
            var check = new CheckBox { Content = option.SafeDisplayText ?? option.DisplayLabelKey.Value,
                IsChecked = selected.Contains(option.SemanticOptionId) };
            check.IsCheckedChanged += (_, _) => { if (check.IsChecked == true) selected.Add(option.SemanticOptionId); else selected.Remove(option.SemanticOptionId);
                State.SetCandidate(selected.ToArray()); CandidateChanged?.Invoke(this, EventArgs.Empty); };
            panel.Children.Add(check);
        }
        return panel;
    }

    private Control CreateHyperlink()
    {
        var openAction = Definition.Actions.FirstOrDefault(x => x.Kind == EditorActionKind.Open);
        var button = new Button { Content = State.CandidateValue?.ToString() ?? Definition.Chrome.PlaceholderKey?.Value,
            IsEnabled = openAction is not null && CanExecuteAction(openAction) &&
                Definition.Capabilities.HasFlag(EditorCapability.ExternalNavigation) };
        if (openAction is not null) button.Click += (_, _) => ActionInvoked?.Invoke(this, new(openAction));
        return button;
    }

    private static Control CreateDeferred(string message) => new TextBlock { Text = message, FontStyle = FontStyle.Italic };

    private bool CanExecuteAction(EditorActionDefinition action) =>
        Resolution.InteractionState == EditorInteractionState.Editable &&
        (action.Requirement is null || AuthorizationPresentationResolver.Resolve(action.Requirement, authorization) ==
            AuthorizationPresentationState.VisibleEnabled);

    private void CaptureCandidate()
    {
        if (editor is TextBox text)
        {
            var parsed = EditorValueParser.Parse(text.Text, Definition, Culture);
            if (parsed.IsSuccess) { State.SetCandidate(parsed.Candidate, text.Text); CandidateChanged?.Invoke(this, EventArgs.Empty); }
            else State.SetValidation(EditorValidationResult.Error(parsed.DiagnosticCode ?? "EDITOR_PARSE_INVALID", Definition.ConsumerSemanticId));
        }
    }

    private void ApplyStateToControl()
    {
        if (editor is TextBox text) text.Text = EditorValueFormatter.Format(State.CandidateValue, Definition, Culture);
        else if (editor is NumericUpDown number) number.Value = State.CandidateValue is null ? null : Convert.ToDecimal(State.CandidateValue, CultureInfo.InvariantCulture);
    }

    private void UpdateMessage(Func<LocalizationKey, string>? localize = null)
    {
        message.Text = State.Validation.IsValid ? Definition.Chrome.HelperTextKey is { } helper
            ? localize?.Invoke(helper) ?? helper.Value : string.Empty :
            State.Validation.SafeLocalizedMessage ?? State.Validation.MessageCode;
        message.Foreground = State.Validation.IsValid ? null : Brushes.IndianRed;
        if (editor is not null) AutomationProperties.SetHelpText(editor, message.Text);
    }
}

public sealed class EditorActionInvokedEventArgs(EditorActionDefinition action) : EventArgs
{ public EditorActionDefinition Action { get; } = action; }
