using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
    private static readonly SemanticIconRegistry EditorIcons = new();
    private readonly StackPanel root = new() { Spacing = EditorPresentationTokens.FieldGap,
        MaxWidth = EditorPresentationTokens.FormMaxReadableWidth };
    private readonly TextBlock label = new();
    private readonly TextBlock message = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock actionFeedback = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap, IsVisible = false };
    private readonly EditorValidator validator;
    private readonly Dictionary<EditorActionDefinition, Button> actionButtons = new();
    private readonly List<TextBox> nativeTextInputs = [];
    private readonly List<CalendarDatePicker> calendarInputs = [];
    private Control? editor;
    private Action? captureCompositeCandidate;
    private TextBlock? rangeStartLabel;
    private TextBlock? rangeEndLabel;
    private Button? helpButton;
    private CancellationTokenSource? lookupCancellation;
    private Border? lookupDropDown;
    private Border? multiChoiceDropDown;
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
        DetachedFromVisualTree += (_, _) => CloseTransientSurfaces();
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
    public bool IsMultiChoiceOpen => multiChoiceDropDown?.IsVisible == true;

    public void CloseTransientSurfaces()
    {
        if (lookupDropDown is not null) lookupDropDown.IsVisible = false;
        if (multiChoiceDropDown is not null) multiChoiceDropDown.IsVisible = false;
        lookupCancellation?.Cancel();
    }

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
        foreach (var calendar in calendarInputs) calendar.CustomDateFormatString = DateFormat(culture);
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
        foreach (var (action, button) in actionButtons)
        {
            var actionName = localize(action.LabelKey);
            AutomationProperties.SetName(button, actionName);
            ToolTip.SetTip(button, actionName);
        }
        if (helpButton is not null) AutomationProperties.SetName(helpButton, localize(new("Editor.Help")));
        if (rangeStartLabel is not null) rangeStartLabel.Text = localize(new("Editor.DateRange.Start"));
        if (rangeEndLabel is not null) rangeEndLabel.Text = localize(new("Editor.DateRange.End"));
        UpdateMessage(localize);
    }

    private void BuildStableVisualTree(IEditorLookupProvider? lookupProvider)
    {
        label.Text = Definition.Chrome.LabelKey?.Value ?? Definition.EditorCode.Value;
        if (Definition.Chrome.ShowRequiredIndicator && Definition.Validation.IsRequired) label.Text += " *";
        AutomationProperties.SetName(this, label.Text);
        root.Children.Add(BuildLabelAnatomy());
        editor = CreateNativeEditor(lookupProvider);
        ApplyPreferredGeometry(editor);
        editor.IsEnabled = Resolution.InteractionState != EditorInteractionState.Disabled;
        AutomationProperties.SetName(editor, label.Text);
        AutomationProperties.SetHelpText(editor, Definition.Chrome.HelperTextKey?.Value ?? string.Empty);
        ToolTip.SetTip(editor, Definition.SensitiveContent.AllowTooltipRawValue ? Definition.Chrome.Tooltip : null);
        root.Children.Add(BuildEditorWithActions(editor));
        message.Text = Definition.Chrome.HelperTextKey?.Value ?? string.Empty;
        root.Children.Add(message);
        root.Children.Add(actionFeedback);
    }

    private Control BuildLabelAnatomy()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = EditorPresentationTokens.FieldGap };
        EditorThemeResources.Bind(row, StackPanel.SpacingProperty, EditorThemeResources.HelpGap);
        label.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(label);
        if (Definition.HelpContextCode is not { } helpCode) return row;
        var help = helpButton = new Button
        {
            Content = new EditorAffordanceSlot(EditorAffordanceKind.Help, "Help"), Tag = "HELP",
            Width = EditorPresentationTokens.MinimumHitTarget, Height = EditorPresentationTokens.MinimumHitTarget,
            Padding = new Thickness(0),
            Background = Brushes.Transparent, BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center
        };
        EditorThemeResources.Bind(help, Button.WidthProperty, EditorThemeResources.TrailingSlotWidth);
        EditorThemeResources.Bind(help, Button.HeightProperty, EditorThemeResources.ControlHeight);
        AutomationProperties.SetName(help, "Help");
        AutomationProperties.SetHelpText(help, helpCode.Value);
        ToolTip.SetTip(help, helpCode.Value);
        help.Click += (_, _) => ActionInvoked?.Invoke(this, new(new("HELP", EditorActionKind.Help,
            new("Editor.Help"))));
        row.Children.Add(help);
        return row;
    }

    private Control BuildEditorWithActions(Control native)
    {
        var embeddedActions = Resolution.Kind == EditorKind.Hyperlink
            ? Definition.Actions.Where(x => x.Kind != EditorActionKind.Open).ToArray()
            : Definition.Actions.ToArray();
        if (embeddedActions.Length == 0) return native;
        var panel = new Grid { ColumnDefinitions = new("*,Auto") };
        panel.Children.Add(native);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        Grid.SetColumn(actions, 1);
        foreach (var action in embeddedActions)
        {
            var button = new Button { Content = new EditorAffordanceSlot(ActionAffordance(action), action.LabelKey.Value), Tag = action.ActionCode,
                Width = EditorPresentationTokens.TrailingActionSize, Height = EditorPresentationTokens.TrailingActionSize,
                Padding = new Thickness(4), IsEnabled = CanExecuteAction(action) };
            AutomationProperties.SetName(button, action.LabelKey.Value);
            ToolTip.SetTip(button, action.LabelKey.Value);
            button.Click += (_, _) => ActionInvoked?.Invoke(this, new(action));
            actions.Children.Add(button);
            actionButtons.Add(action, button);
        }
        panel.Children.Add(actions); return panel;
    }

    private static EditorAffordanceKind ActionAffordance(EditorActionDefinition action) => action.Kind switch
    {
        EditorActionKind.Clear => EditorAffordanceKind.Clear,
        EditorActionKind.Reveal => EditorAffordanceKind.Reveal,
        EditorActionKind.Help => EditorAffordanceKind.Help,
        EditorActionKind.Open or EditorActionKind.Browse => EditorAffordanceKind.OpenBrowse,
        _ => EditorAffordanceKind.Dropdown,
    };

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
            IsThreeState = Definition.AllowsNull, IsEnabled = Resolution.InteractionState == EditorInteractionState.Editable,
            Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center };
        DynamicCheckBoxPresentation.Apply(check);
        check.IsCheckedChanged += (_, _) => { State.SetCandidate(check.IsChecked); CandidateChanged?.Invoke(this, EventArgs.Empty); };
        return check;
    }

    private CalendarDatePicker CreateDate()
    {
        var date = CompactDatePicker(State.CandidateValue is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : State.CandidateValue as DateTime?);
        date.SelectedDateChanged += (_, _) => { State.SetCandidate(date.SelectedDate is { } value ? DateOnly.FromDateTime(value) : null); CandidateChanged?.Invoke(this, EventArgs.Empty); };
        captureCompositeCandidate = () => State.SetCandidate(date.SelectedDate is { } value ? DateOnly.FromDateTime(value) : null);
        return date;
    }

    private Control CreateTime()
    {
        var (surface, input) = CompactTimeEditor(State.CandidateValue is TimeOnly t ? t :
            State.CandidateValue is TimeSpan span ? TimeOnly.FromTimeSpan(span) : null);
        captureCompositeCandidate = () => CaptureTime(input, value => State.SetCandidate(value));
        return surface;
    }

    private Control CreateDateTime()
    {
        var value = State.CandidateValue as DateTime?;
        var panel = new Grid { ColumnDefinitions = new("Auto,Auto"), ColumnSpacing = EditorPresentationTokens.InlineGap };
        var date = CompactDatePicker(value);
        var (timeSurface, timeInput) = CompactTimeEditor(value is { } current ? TimeOnly.FromDateTime(current) : null);
        Grid.SetColumn(timeSurface, 1);
        void Changed() { if (date.SelectedDate is { } d && TryParseTime(timeInput.Text, out var t)) State.SetCandidate(d.Date + t.ToTimeSpan()); CandidateChanged?.Invoke(this, EventArgs.Empty); }
        date.SelectedDateChanged += (_, _) => Changed();
        captureCompositeCandidate = Changed;
        panel.Children.Add(date); panel.Children.Add(timeSurface); return panel;
    }

    private Control CreateDateRange()
    {
        var range = State.CandidateValue is DateRangeValue r ? r : default;
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        var start = CompactDatePicker(range.Start?.ToDateTime(TimeOnly.MinValue));
        var end = CompactDatePicker(range.End?.ToDateTime(TimeOnly.MinValue));
        rangeStartLabel = new() { Text = "Start date", VerticalAlignment = VerticalAlignment.Center };
        rangeEndLabel = new() { Text = "End date", VerticalAlignment = VerticalAlignment.Center };
        var startGroup = new StackPanel { Spacing = EditorPresentationTokens.FieldGap,
            Margin = new Thickness(0, 0, EditorPresentationTokens.FieldGroupGap, EditorPresentationTokens.FieldGap),
            Children = { rangeStartLabel, start } };
        var endGroup = new StackPanel { Spacing = EditorPresentationTokens.FieldGap,
            Margin = new Thickness(0, 0, 0, EditorPresentationTokens.FieldGap), Children = { rangeEndLabel, end } };
        void Changed() { State.SetCandidate(new DateRangeValue(start.SelectedDate is { } s ? DateOnly.FromDateTime(s) : null,
            end.SelectedDate is { } e ? DateOnly.FromDateTime(e) : null)); CandidateChanged?.Invoke(this, EventArgs.Empty); }
        start.SelectedDateChanged += (_, _) => Changed(); end.SelectedDateChanged += (_, _) => Changed();
        panel.Children.Add(startGroup); panel.Children.Add(endGroup); return panel;
    }

    private ComboBox CreateChoice()
    {
        var combo = new ComboBox { ItemsSource = Definition.Choices, SelectedItem = Definition.Choices.FirstOrDefault(x => x.SemanticOptionId == State.CandidateValue?.ToString()),
            IsEnabled = Resolution.InteractionState == EditorInteractionState.Editable,
            MaxDropDownHeight = EditorPresentationTokens.PopupMaxHeight,
            Height = EditorPresentationTokens.ControlHeight,
            Padding = new Thickness(EditorPresentationTokens.ContentPadding, 0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center };
        combo.Classes.Add("dui-choice");
        EditorThemeResources.Bind(combo, ComboBox.HeightProperty, EditorThemeResources.ControlHeight);
        combo.SelectionChanged += (_, _) => { State.SetCandidate((combo.SelectedItem as EditorChoiceOption)?.SemanticOptionId); CandidateChanged?.Invoke(this, EventArgs.Empty); };
        return combo;
    }

    private Control CreateLookup(IEditorLookupProvider? provider)
    {
        if (provider is null) return CreateDeferred("Lookup provider is not registered.");
        var panel = new Grid { RowDefinitions = new("Auto,Auto") };
        var selectedText = new TextBlock { Text = State.CandidateValue is EditorLookupSelection initial ? initial.SafeDisplayText : "Select…",
            VerticalAlignment = VerticalAlignment.Center };
        var trigger = new Button { Height = EditorPresentationTokens.ControlHeight, Padding = new Thickness(0),
            Background = Brushes.Transparent, BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = EditorSurfaceGeometry.WithTrailingAffordance(selectedText, EditorAffordanceKind.Search, "Search") };
        EditorThemeResources.Bind(trigger, Button.HeightProperty, EditorThemeResources.ControlHeight);
        var popupContent = new Grid { RowDefinitions = new("Auto,*"), RowSpacing = EditorPresentationTokens.FieldGap,
            Width = EditorPresentationTokens.LongControlWidth };
        var query = new TextBox { Watermark = Definition.Chrome.PlaceholderKey?.Value ?? "Search", Height = EditorPresentationTokens.ControlHeight };
        NativeEditorInputOwnership.Enable(query);
        nativeTextInputs.Add(query);
        var results = new ListBox { MaxHeight = EditorPresentationTokens.PopupMaxHeight - EditorPresentationTokens.ControlHeight - EditorPresentationTokens.FieldGap };
        Grid.SetRow(results, 1);
        var coordinator = new EditorLookupCoordinator();
        var selection = new EditorLookupSelectionState();
        void CommitSelection()
        {
            if (selection.CommitActive() is not { } committed) return;
            State.SetCandidate(committed);
            selectedText.Text = committed.SafeDisplayText;
            if (lookupDropDown is not null) lookupDropDown.IsVisible = false;
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
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        };
        trigger.Click += (_, _) => { if (lookupDropDown is null) return;
            lookupDropDown.IsVisible = !lookupDropDown.IsVisible; if (lookupDropDown.IsVisible) query.Focus(); };
        query.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { if (lookupDropDown is not null) lookupDropDown.IsVisible = false; trigger.Focus(); e.Handled = true; }
            else if (e.Key == Key.Down && selection.Items.Count > 0)
            { results.SelectedIndex = Math.Max(0, selection.ActiveIndex); results.Focus(); e.Handled = true; }
        };
        results.SelectionChanged += (_, _) => selection.SetActive(results.SelectedItem as EditorLookupOption);
        results.PointerReleased += (_, _) => CommitSelection();
        results.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { CommitSelection(); e.Handled = true; }
            else if (e.Key == Key.Escape) { if (lookupDropDown is not null) lookupDropDown.IsVisible = false; trigger.Focus(); e.Handled = true; }
        };
        popupContent.Children.Add(query); popupContent.Children.Add(results);
        lookupDropDown = new Border { Tag = "LOOKUP_DROPDOWN", Child = popupContent, IsVisible = false,
            MaxHeight = EditorPresentationTokens.PopupMaxHeight, BorderThickness = new Thickness(EditorPresentationTokens.BorderThickness),
            Padding = new Thickness(EditorPresentationTokens.ContentPadding),
            CornerRadius = new CornerRadius(EditorPresentationTokens.CornerRadius), ClipToBounds = true };
        EditorThemeResources.Bind(lookupDropDown, Border.MaxHeightProperty, EditorThemeResources.PopupMaxHeight);
        EditorThemeResources.Bind(lookupDropDown, Border.BorderBrushProperty, EditorThemeResources.SurfaceBorderBrush);
        EditorThemeResources.Bind(lookupDropDown, Border.BackgroundProperty, EditorThemeResources.SurfaceBackground);
        Grid.SetRow(lookupDropDown, 1);
        panel.Children.Add(trigger); panel.Children.Add(lookupDropDown); return panel;
    }

    private Control CreateMultiChoiceSeam()
    {
        var selected = (State.CandidateValue as IEnumerable<string> ?? []).ToHashSet(StringComparer.Ordinal);
        var panel = new Grid { RowDefinitions = new("Auto,Auto") };
        var trigger = new Button { Height = EditorPresentationTokens.ControlHeight,
            Tag = "MULTICHOICE_TRIGGER",
            Padding = new Thickness(0), Background = Brushes.Transparent, BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center };
        EditorThemeResources.Bind(trigger, Button.HeightProperty, EditorThemeResources.ControlHeight);
        var choices = new StackPanel { Spacing = EditorPresentationTokens.FieldGap,
            HorizontalAlignment = HorizontalAlignment.Stretch };
        EditorThemeResources.Bind(choices, StackPanel.SpacingProperty, EditorThemeResources.MultiChoiceOptionGap);
        var summary = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        var triggerContent = EditorSurfaceGeometry.WithTrailingAffordance(summary, EditorAffordanceKind.Dropdown, "Open choices");
        trigger.Content = triggerContent;
        void UpdateSummary() => summary.Text = selected.Count switch
        {
            0 => "Select…",
            1 => Definition.Choices.FirstOrDefault(x => selected.Contains(x.SemanticOptionId))?.ToString() ?? "1 selected",
            _ => $"{selected.Count} selected"
        };
        foreach (var option in Definition.Choices)
        {
            var check = new CheckBox { IsChecked = selected.Contains(option.SemanticOptionId),
                Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            // Avalonia owns semantics/input; DynamicUI24 owns the reusable visual ControlTheme.
            DynamicCheckBoxPresentation.Apply(check);
            var leadingSlot = new Grid { Width = EditorPresentationTokens.LeadingSlotWidth,
                Height = EditorPresentationTokens.OptionRowHeight, HorizontalAlignment = HorizontalAlignment.Left };
            EditorThemeResources.Bind(leadingSlot, Grid.WidthProperty, EditorThemeResources.LeadingSlotWidth);
            EditorThemeResources.Bind(leadingSlot, Grid.HeightProperty, EditorThemeResources.PopupOptionHeight);
            leadingSlot.Children.Add(check);
            var optionLabel = new TextBlock { Text = option.SafeDisplayText ?? option.DisplayLabelKey.Value,
                VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            AutomationProperties.SetName(check, optionLabel.Text);
            var optionRow = new Grid { ColumnDefinitions = new("Auto,*"), Height = EditorPresentationTokens.OptionRowHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch, Tag = "MULTICHOICE_OPTION_ROW" };
            EditorThemeResources.Bind(optionRow, Grid.HeightProperty, EditorThemeResources.PopupOptionHeight);
            optionRow.Children.Add(leadingSlot); Grid.SetColumn(optionLabel, 1); optionRow.Children.Add(optionLabel);
            check.IsCheckedChanged += (_, _) => { if (check.IsChecked == true) selected.Add(option.SemanticOptionId); else selected.Remove(option.SemanticOptionId);
                State.SetCandidate(selected.Order(StringComparer.Ordinal).ToArray()); UpdateSummary(); CandidateChanged?.Invoke(this, EventArgs.Empty); };
            choices.Children.Add(optionRow);
        }
        var scroller = new ScrollViewer
        {
            Tag = "MULTICHOICE_SCROLL", Content = choices, MaxHeight = EditorPresentationTokens.PopupMaxHeight,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        EditorThemeResources.Bind(scroller, ScrollViewer.MaxHeightProperty, EditorThemeResources.PopupMaxHeight);
        var dropDown = multiChoiceDropDown = new Border
        {
            Tag = "MULTICHOICE_DROPDOWN",
            Child = scroller, IsVisible = false, BorderThickness = new Thickness(EditorPresentationTokens.BorderThickness),
            Padding = new Thickness(EditorPresentationTokens.ContentPadding),
            CornerRadius = new CornerRadius(EditorPresentationTokens.CornerRadius), ClipToBounds = true,
        };
        EditorThemeResources.Bind(dropDown, Border.BorderBrushProperty, EditorThemeResources.SurfaceBorderBrush);
        EditorThemeResources.Bind(dropDown, Border.BackgroundProperty, EditorThemeResources.SurfaceBackground);
        Grid.SetRow(dropDown, 1);
        void SetOpen(bool open) => dropDown.IsVisible = open;
        trigger.Click += (_, _) => SetOpen(!dropDown.IsVisible);
        trigger.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Down or Key.Space or Key.Enter) { SetOpen(true); e.Handled = true; }
            else if (e.Key == Key.Escape) { SetOpen(false); e.Handled = true; }
        };
        choices.KeyDown += (_, e) => { if (e.Key == Key.Escape) { SetOpen(false); trigger.Focus(); e.Handled = true; } };
        UpdateSummary(); panel.Children.Add(trigger); panel.Children.Add(dropDown);
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
        captureCompositeCandidate?.Invoke();
        if (editor is TextBox text)
        {
            var parsed = EditorValueParser.Parse(text.Text, Definition, Culture);
            if (parsed.IsSuccess) { State.SetCandidate(parsed.Candidate, text.Text); CandidateChanged?.Invoke(this, EventArgs.Empty); }
            else State.SetValidation(EditorValidationResult.Error(parsed.DiagnosticCode ?? "EDITOR_PARSE_INVALID", Definition.ConsumerSemanticId));
        }
    }

    private CalendarDatePicker CompactDatePicker(DateTime? value)
    {
        var picker = new CalendarDatePicker
        {
            SelectedDate = value, SelectedDateFormat = CalendarDatePickerFormat.Custom,
            CustomDateFormatString = DateFormat(Culture), Width = EditorPresentationTokens.CompactControlWidth,
            Height = EditorPresentationTokens.ControlHeight, HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(EditorPresentationTokens.ContentPadding, 0,
                EditorPresentationTokens.TrailingSlotWidth + EditorPresentationTokens.ContentPadding, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            IsEnabled = Resolution.InteractionState == EditorInteractionState.Editable,
        };
        EditorThemeResources.Bind(picker, CalendarDatePicker.HeightProperty, EditorThemeResources.ControlHeight);
        picker.Classes.Add("dui-date");
        picker.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            var button = picker.GetVisualDescendants().OfType<Button>()
                .FirstOrDefault(x => x.Name == "PART_Button");
            if (button is null || Equals(button.Tag, "DUI_CATALOG_CALENDAR")) return;
            var icon = new SemanticIcon
            {
                Width = EditorPresentationTokens.IconSize,
                Height = EditorPresentationTokens.IconSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            icon.SetIcon(EditorIcons, StandardIconKeys.Calendar);
            EditorThemeResources.Bind(icon, SemanticIcon.WidthProperty, EditorThemeResources.IconSize);
            EditorThemeResources.Bind(icon, SemanticIcon.HeightProperty, EditorThemeResources.IconSize);
            EditorThemeResources.Bind(icon, SemanticIcon.ForegroundProperty, EditorThemeResources.IconBrush);
            button.Content = icon;
            button.Tag = "DUI_CATALOG_CALENDAR";
        }, DispatcherPriority.Loaded);
        calendarInputs.Add(picker); return picker;
    }
    private static string DateFormat(CultureInfo culture) => culture.Name.Equals("vi-VN", StringComparison.OrdinalIgnoreCase)
        ? "dd/MM/yyyy" : culture.DateTimeFormat.ShortDatePattern;

    private (Control Surface, TextBox Input) CompactTimeEditor(TimeOnly? value)
    {
        var text = new TextBox { Text = value?.ToString("HH:mm", CultureInfo.InvariantCulture), Watermark = "HH:mm",
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0), Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Center,
            IsReadOnly = Resolution.InteractionState == EditorInteractionState.ReadOnly };
        NativeEditorInputOwnership.Enable(text); nativeTextInputs.Add(text);
        var surface = EditorSurfaceGeometry.WithTrailingAffordance(text, EditorAffordanceKind.Clock, "Time");
        surface.Width = EditorPresentationTokens.TimeControlWidth;
        EditorThemeResources.Bind(surface, Border.WidthProperty, EditorThemeResources.WidthTime);
        return (surface, text);
    }
    private void CaptureTime(TextBox text, Action<TimeOnly?> apply)
    {
        if (string.IsNullOrWhiteSpace(text.Text)) apply(null);
        else if (TryParseTime(text.Text, out var value)) apply(value);
        else State.SetValidation(EditorValidationResult.Error("EDITOR_TIME_PARSE_INVALID", Definition.ConsumerSemanticId));
        CandidateChanged?.Invoke(this, EventArgs.Empty);
    }
    private bool TryParseTime(string? text, out TimeOnly value) => TimeOnly.TryParse(text, Culture, DateTimeStyles.None, out value) ||
        TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    private void ApplyPreferredGeometry(Control control)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        control.HorizontalAlignment = HorizontalAlignment.Left;
        var width = Definition.Width switch
        {
            EditorWidthClass.Short => EditorPresentationTokens.ShortControlWidth,
            EditorWidthClass.Compact => EditorPresentationTokens.CompactControlWidth,
            EditorWidthClass.Medium => EditorPresentationTokens.MediumControlWidth,
            EditorWidthClass.Long => EditorPresentationTokens.LongControlWidth,
            EditorWidthClass.Fill => EditorPresentationTokens.FormMaxReadableWidth,
            _ => Resolution.Kind switch
            {
                EditorKind.Time => EditorPresentationTokens.TimeControlWidth,
                EditorKind.Date or EditorKind.Boolean => EditorPresentationTokens.CompactControlWidth,
                EditorKind.DateTime => EditorPresentationTokens.DateTimeWidth,
                EditorKind.Choice => EditorPresentationTokens.MediumControlWidth,
                EditorKind.DateRange => EditorPresentationTokens.DateRangeWidth,
                EditorKind.MultilineText or EditorKind.SearchLookup or EditorKind.MultiChoice => EditorPresentationTokens.LongControlWidth,
                _ => EditorPresentationTokens.MediumControlWidth,
            }
        };
        control.Width = width;
        control.MaxWidth = width;
        var resource = Definition.Width switch
        {
            EditorWidthClass.Short => EditorThemeResources.WidthShort,
            EditorWidthClass.Compact => EditorThemeResources.WidthCompact,
            EditorWidthClass.Medium => EditorThemeResources.WidthMedium,
            EditorWidthClass.Long => EditorThemeResources.WidthLong,
            EditorWidthClass.Fill => EditorThemeResources.WidthFill,
            _ => Resolution.Kind switch
            {
                EditorKind.Time => EditorThemeResources.WidthTime,
                EditorKind.Date or EditorKind.Boolean => EditorThemeResources.WidthCompact,
                EditorKind.DateTime => EditorThemeResources.WidthDateTime,
                EditorKind.DateRange => EditorThemeResources.WidthDateRange,
                EditorKind.MultilineText or EditorKind.SearchLookup or EditorKind.MultiChoice => EditorThemeResources.WidthLong,
                _ => EditorThemeResources.WidthMedium,
            }
        };
        EditorThemeResources.Bind(control, Control.WidthProperty, resource);
        EditorThemeResources.Bind(control, Control.MaxWidthProperty, resource);
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
