using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Controls.Primitives;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Reusable metadata renderer and dispatcher. Business operations remain outside this control.</summary>
public sealed class DynamicActionBarHost : Border
{
    private readonly ActionBarCommandDispatcher dispatcher;
    private readonly ILocalizationService localization;
    private readonly IIconRegistry icons;
    private ResolvedActionBar? resolved;
    private ActionCommandExecutionContext? executionContext;
    private readonly Dictionary<string, MenuRuntime> menus = new(StringComparer.OrdinalIgnoreCase);

    public DynamicActionBarHost(ActionBarCommandDispatcher dispatcher, ILocalizationService localization, IIconRegistry icons)
    {
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.icons = icons ?? throw new ArgumentNullException(nameof(icons));
        Padding = new Thickness(8, 5);
        BorderThickness = new Thickness(0, 1);
        Bind(BackgroundProperty, this.GetResourceObservable("DuiSurfaceRaisedBrush"));
        Bind(BorderBrushProperty, this.GetResourceObservable("DuiBorderBrush"));
        localization.CultureChanged += (_, _) => Render();
    }

    public ResolvedActionBar? ResolvedActionBar => resolved;
    public string? FocusedMenuItemCode { get; private set; }
    public bool LastMenuOpenUsedKeyboard { get; private set; }
    public event EventHandler<ActionCommandResult>? CommandCompleted;

    public void Show(ResolvedActionBar actionBar, ActionCommandExecutionContext context)
    {
        resolved = actionBar ?? throw new ArgumentNullException(nameof(actionBar));
        executionContext = context ?? throw new ArgumentNullException(nameof(context));
        Render();
    }

    public void Clear()
    {
        resolved = null;
        executionContext = null;
        IsVisible = false;
        Child = null;
    }

    public async Task<ActionCommandResult> ExecuteActionAsync(string actionCode, CancellationToken cancellationToken = default)
    {
        var action = resolved?.Actions.FirstOrDefault(x =>
            x.Definition.ActionCode.Equals(actionCode, StringComparison.OrdinalIgnoreCase));
        if (action is null || executionContext is null)
            return ActionCommandResult.Unavailable("ACTION_UNKNOWN");
        var result = await dispatcher.DispatchAsync(action, executionContext, cancellationToken);
        CommandCompleted?.Invoke(this, result);
        return result;
    }

    public bool IsMenuOpen(string actionCode) => menus.TryGetValue(actionCode, out var menu) && menu.Popup.IsOpen;
    public AuthorizationPresentationState? GetMenuItemState(string actionCode, string itemCode) =>
        menus.TryGetValue(actionCode, out var menu)
            ? Flatten(menu.Action.MenuItems).FirstOrDefault(x =>
                x.Definition.ItemCode.Equals(itemCode, StringComparison.OrdinalIgnoreCase))?.State
            : null;

    public bool OpenMenu(string actionCode, bool fromKeyboard = false)
    {
        if (!menus.TryGetValue(actionCode, out var menu) || !menu.Action.IsEnabled) return false;
        foreach (var other in menus.Values.Where(x => !ReferenceEquals(x, menu))) other.Popup.IsOpen = false;
        LastMenuOpenUsedKeyboard = fromKeyboard;
        menu.Popup.IsOpen = true;
        menu.FocusIndex = menu.Items.FindIndex(x => x.IsEnabled);
        if (fromKeyboard && menu.FocusIndex >= 0)
        {
            menu.Items[menu.FocusIndex].Focus();
            FocusedMenuItemCode = menu.ItemCodes[menu.FocusIndex];
        }
        return true;
    }

    public bool CloseMenu(string actionCode)
    {
        if (!menus.TryGetValue(actionCode, out var menu) || !menu.Popup.IsOpen) return false;
        menu.Popup.IsOpen = false;
        FocusedMenuItemCode = null;
        return true;
    }

    public bool NavigateMenuKeyboard(string actionCode, Key key)
    {
        if (!menus.TryGetValue(actionCode, out var menu)) return false;
        if (!menu.Popup.IsOpen && key is Key.Down or Key.F4) return OpenMenu(actionCode, true);
        if (key == Key.Escape) return CloseMenu(actionCode);
        if (key is not (Key.Down or Key.Up) || menu.Items.Count == 0) return false;
        var delta = key == Key.Down ? 1 : -1;
        for (var attempt = 0; attempt < menu.Items.Count; attempt++)
        {
            menu.FocusIndex = (menu.FocusIndex + delta + menu.Items.Count) % menu.Items.Count;
            if (!menu.Items[menu.FocusIndex].IsEnabled) continue;
            menu.Items[menu.FocusIndex].Focus();
            FocusedMenuItemCode = menu.ItemCodes[menu.FocusIndex];
            return true;
        }
        return false;
    }

    public async Task<ActionCommandResult> ExecuteMenuItemAsync(string actionCode, string itemCode,
        CancellationToken cancellationToken = default)
    {
        if (!menus.TryGetValue(actionCode, out var menu) || executionContext is null)
            return ActionCommandResult.Unavailable("ACTION_MENU_UNKNOWN");
        var item = Flatten(menu.Action.MenuItems).FirstOrDefault(x =>
            x.Definition.ItemCode.Equals(itemCode, StringComparison.OrdinalIgnoreCase));
        if (item is null) return ActionCommandResult.Unavailable("ACTION_MENU_ITEM_UNKNOWN");
        var result = await dispatcher.DispatchMenuItemAsync(item, executionContext, cancellationToken);
        menu.Popup.IsOpen = false;
        CommandCompleted?.Invoke(this, result);
        return result;
    }

    private void Render()
    {
        if (resolved is null)
        {
            Clear();
            return;
        }

        IsVisible = resolved.Definition.IsVisible;
        foreach (var menu in menus.Values) menu.Popup.IsOpen = false;
        menus.Clear();
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        foreach (var action in resolved.Actions)
        {
            actions.Children.Add(CreateActionControl(action));
        }

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        if (resolved.Definition.Position == ActionBarPosition.Bottom && resolved.Status is { } status)
        {
            var summary = new TextBlock
            {
                Text = FormatStatus(status),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0),
            };
            summary.Bind(TextBlock.ForegroundProperty, summary.GetResourceObservable("DuiTextMutedBrush"));
            row.Children.Add(summary);
            Grid.SetColumn(actions, 1);
        }
        row.Children.Add(actions);
        Child = row;
    }

    private Control CreateActionControl(ResolvedAction action)
    {
        var definition = action.Definition;
        Control control = definition.ButtonVariant switch
        {
            ActionButtonVariant.IconButton => CreateMainButton(action, true),
            ActionButtonVariant.ToggleButton => CreateToggleButton(action),
            ActionButtonVariant.DropdownButton => CreateDropdownButton(action),
            ActionButtonVariant.SplitButton => CreateSplitButton(action),
            _ => CreateMainButton(action, false),
        };
        if (action.IsReadOnly) ToolTip.SetTip(control, localization.Get(new("State.ReadOnly")));
        return control;
    }

    private Button CreateMainButton(ResolvedAction action, bool iconOnly)
    {
        var button = new Button { Content = CreateActionContent(action, iconOnly), IsEnabled = action.IsEnabled,
            Tag = action.Definition.ActionCode };
        if (iconOnly) ToolTip.SetTip(button, localization.Get(action.Definition.DisplayNameKey));
        button.Click += async (_, _) => await ExecuteActionAsync(action.Definition.ActionCode);
        return button;
    }

    private ToggleButton CreateToggleButton(ResolvedAction action)
    {
        var button = new ToggleButton { Content = CreateActionContent(action, false), IsEnabled = action.IsEnabled,
            IsChecked = action.Definition.IsChecked };
        button.Click += async (_, _) => await ExecuteActionAsync(action.Definition.ActionCode);
        return button;
    }

    private Control CreateDropdownButton(ResolvedAction action)
    {
        var button = new Button { Content = CreateActionContent(action, false, true), IsEnabled = action.IsEnabled };
        button.Click += (_, _) => OpenMenu(action.Definition.ActionCode);
        button.KeyDown += (_, e) => { if (e.Key is Key.Down or Key.F4) { e.Handled = OpenMenu(action.Definition.ActionCode, true); } };
        var popup = RegisterMenu(action, button);
        return new Grid { Children = { button, popup } };
    }

    private Control CreateSplitButton(ResolvedAction action)
    {
        var main = CreateMainButton(action, false);
        var chevron = new Button { Content = "⌄", IsEnabled = action.IsEnabled, Padding = new Thickness(7, 0) };
        chevron.Click += (_, _) => OpenMenu(action.Definition.ActionCode);
        chevron.KeyDown += (_, e) => { if (e.Key is Key.Down or Key.F4) { e.Handled = OpenMenu(action.Definition.ActionCode, true); } };
        var popup = RegisterMenu(action, chevron);
        return new Grid { Children = { new StackPanel { Orientation = Orientation.Horizontal, Spacing = 1,
            Children = { main, chevron } }, popup } };
    }

    private Control CreateActionContent(ResolvedAction action, bool iconOnly, bool chevron = false)
    {
        var icon = CreateIcon(action.Definition.IconKey, action.IsEnabled);
        if (iconOnly) return icon;
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6,
            Children = { icon, new TextBlock { Text = localization.Get(action.Definition.DisplayNameKey), VerticalAlignment = VerticalAlignment.Center } } };
        if (chevron) panel.Children.Add(new TextBlock { Text = "⌄", VerticalAlignment = VerticalAlignment.Center });
        return panel;
    }

    private SemanticIcon CreateIcon(IconKey key, bool enabled)
    {
        var icon = new SemanticIcon { Width = 16, Height = 16 };
        icon.SetIcon(icons, key);
        var brush = executionContext!.ResolutionContext.PresentationState.Kind switch
        {
            PresentationStateKind.Error => "DuiErrorBrush",
            PresentationStateKind.Unavailable => "DuiWarningBrush",
            PresentationStateKind.ReadOnly => "DuiTextMutedBrush",
            _ when !enabled => "DuiDisabledBrush",
            _ => "DuiAccentBrush",
        };
        icon.Bind(SemanticIcon.ForegroundProperty, icon.GetResourceObservable(brush));
        return icon;
    }

    private Popup RegisterMenu(ResolvedAction action, Control trigger)
    {
        var panel = new StackPanel { Spacing = 2, MinWidth = 210 };
        var runtime = new MenuRuntime(action, new Popup
        {
            PlacementTarget = trigger,
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            IsLightDismissEnabled = true,
        });
        BuildMenuItems(panel, action.Definition.ActionCode, action.MenuItems, runtime, 0);
        var surface = new Border { Padding = new Thickness(6), CornerRadius = new CornerRadius(7),
            BorderThickness = new Thickness(1), Child = panel };
        surface.Bind(BackgroundProperty, surface.GetResourceObservable("DuiSurfaceRaisedBrush"));
        surface.Bind(BorderBrushProperty, surface.GetResourceObservable("DuiBorderBrush"));
        surface.KeyDown += (_, e) => e.Handled = NavigateMenuKeyboard(action.Definition.ActionCode, e.Key);
        runtime.Popup.Child = surface;
        menus[action.Definition.ActionCode] = runtime;
        return runtime.Popup;
    }

    private void BuildMenuItems(Panel panel, string actionCode, IEnumerable<ResolvedActionMenuItem> items,
        MenuRuntime runtime, int level)
    {
        string? previousGroup = null;
        foreach (var item in items)
        {
            var definition = item.Definition;
            if (definition.Kind == ActionMenuItemKind.Separator ||
                previousGroup is not null && definition.GroupCode is not null && definition.GroupCode != previousGroup)
            { panel.Children.Add(new Separator()); previousGroup = definition.GroupCode; if (definition.Kind == ActionMenuItemKind.Separator) continue; }
            previousGroup = definition.GroupCode ?? previousGroup;
            var content = new Grid { ColumnDefinitions = new("Auto,*,Auto"), ColumnSpacing = 8 };
            if (definition.IconKey is { } iconKey) content.Children.Add(CreateIcon(iconKey, item.IsEnabled));
            var label = new TextBlock { Text = localization.Get(definition.DisplayNameKey), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(label, 1); content.Children.Add(label);
            var suffix = new TextBlock { Text = definition.Children.Length > 0 ? "›" : definition.ShortcutDisplay ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center };
            suffix.Bind(TextBlock.ForegroundProperty, suffix.GetResourceObservable("DuiTextMutedBrush"));
            Grid.SetColumn(suffix, 2); content.Children.Add(suffix);
            var button = new Button { Content = content, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                IsEnabled = item.IsEnabled, Tag = definition.ItemCode, Padding = new Thickness(8, 5),
                Margin = new Thickness(level * 14, 0, 0, 0) };
            runtime.Items.Add(button); runtime.ItemCodes.Add(definition.ItemCode);
            if (definition.Children.Length > 0)
            {
                var childrenPanel = new StackPanel { Spacing = 2, IsVisible = false };
                button.Click += (_, _) => childrenPanel.IsVisible = !childrenPanel.IsVisible;
                panel.Children.Add(button); panel.Children.Add(childrenPanel);
                BuildMenuItems(childrenPanel, actionCode, item.Children, runtime, level + 1);
            }
            else
            {
                button.Click += async (_, _) => await ExecuteMenuItemAsync(actionCode, definition.ItemCode);
                panel.Children.Add(button);
            }
        }
    }

    private static IEnumerable<ResolvedActionMenuItem> Flatten(IEnumerable<ResolvedActionMenuItem> items) => items
        .SelectMany(item => new[] { item }.Concat(Flatten(item.Children)));

    private string FormatStatus(ActionBarStatus status)
    {
        var parts = new List<string>();
        Add(parts, status.TotalRows, "ActionBar.Status.Rows");
        Add(parts, status.VisibleRows, "ActionBar.Status.Visible");
        Add(parts, status.SelectedRows, "ActionBar.Status.Selected");
        Add(parts, status.ErrorCount, "ActionBar.Status.Errors");
        Add(parts, status.WarningCount, "ActionBar.Status.Warnings");
        Add(parts, status.PendingChangeCount, "ActionBar.Status.Pending");
        if (status.ReadOnlyState is { } readOnly)
            parts.Add(localization.Get(new(readOnly ? "ActionBar.Status.ReadOnly" : "ActionBar.Status.Editable")));
        return string.Join(" · ", parts);
    }

    private void Add(ICollection<string> parts, int? value, string key)
    {
        if (value is not null) parts.Add($"{value.Value} {localization.Get(new(key))}");
    }

    private sealed class MenuRuntime(ResolvedAction action, Popup popup)
    {
        public ResolvedAction Action { get; } = action;
        public Popup Popup { get; } = popup;
        public List<Button> Items { get; } = [];
        public List<string> ItemCodes { get; } = [];
        public int FocusIndex { get; set; } = -1;
    }
}
