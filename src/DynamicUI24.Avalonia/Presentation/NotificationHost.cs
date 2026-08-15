using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using DynamicUI24.Core.Notifications;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Shared renderer for notification center, toast, banner, alert-card and blocking-notice surfaces.</summary>
public sealed class NotificationHost : Border
{
    private readonly NotificationCoordinator coordinator;
    private readonly NotificationActionDispatcher dispatcher;
    private readonly ILocalizationService localization;
    private readonly IIconRegistry icons;
    private readonly StackPanel root = new() { Spacing = 8 };
    private readonly StackPanel centerItems = new() { Spacing = 8 };
    private readonly Border centerPanel = new() { IsVisible = false, Padding = new Thickness(10), BorderThickness = new Thickness(1) };

    public NotificationHost(NotificationCoordinator coordinator, NotificationActionDispatcher dispatcher,
        ILocalizationService localization, IIconRegistry icons)
    {
        this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.icons = icons ?? throw new ArgumentNullException(nameof(icons));
        Padding = new Thickness(0);
        Child = root;
        centerPanel.Child = new ScrollViewer { Content = centerItems, MaxHeight = 360 };
        centerPanel.Bind(BackgroundProperty, centerPanel.GetResourceObservable("DuiSurfaceRaisedBrush"));
        centerPanel.Bind(BorderBrushProperty, centerPanel.GetResourceObservable("DuiBorderBrush"));
        coordinator.Changed += (_, model) => Render(model);
        localization.CultureChanged += (_, _) => Render(coordinator.Current);
        Render(coordinator.Current);
    }

    public int AttentionCount => coordinator.Current.AttentionCount;
    public bool IsCenterOpen { get => centerPanel.IsVisible; set { centerPanel.IsVisible = value; if (value) centerItems.Focus(); } }
    public event EventHandler<GuidanceActionResult>? ActionCompleted;

    private void Render(NotificationPresentationModel model)
    {
        root.Children.Clear();
        var centerHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var centerButton = new Button();
        centerButton.Click += (_, _) =>
        {
            centerPanel.IsVisible = !centerPanel.IsVisible;
            if (centerPanel.IsVisible) centerItems.Focus();
        };
        var title = new TextBlock { Text = localization.Get(new("Notification.Center")), FontWeight = global::Avalonia.Media.FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center };
        centerButton.Content = $"🔔 {model.AttentionCount}";
        AutomationProperties.SetName(centerButton, $"{localization.Get(new("Notification.Center"))}: {model.AttentionCount}");
        centerHeader.Children.Add(title); centerHeader.Children.Add(centerButton); Grid.SetColumn(centerButton, 1);
        root.Children.Add(centerHeader);

        centerItems.Children.Clear();
        foreach (var group in model.CenterGroups)
        {
            centerItems.Children.Add(new TextBlock { Text = localization.Get(new($"Notification.Group.{group.Code}")),
                FontWeight = global::Avalonia.Media.FontWeight.SemiBold, Margin = new Thickness(0, 4) });
            foreach (var notification in group.Items.Where(x => x.Surfaces.Any(y => y.Surface == NotificationSurface.NotificationCenter)))
                centerItems.Children.Add(CreateCard(notification, NotificationSurface.NotificationCenter));
        }
        root.Children.Add(centerPanel);

        foreach (var surface in new[] { NotificationSurface.BlockingNotice, NotificationSurface.Banner,
                     NotificationSurface.AlertCard, NotificationSurface.Toast })
        {
            var items = model.ForSurface(surface).Where(x => surface != NotificationSurface.Toast || x.ShouldAutoShow).ToArray();
            if (items.Length == 0) continue;
            var panel = new StackPanel { Spacing = 8 };
            foreach (var item in items) panel.Children.Add(CreateCard(item, surface));
            root.Children.Add(panel);
        }
        IsVisible = model.Notifications.Length > 0;
    }

    private Control CreateCard(ResolvedNotification notification, NotificationSurface surface)
    {
        var definition = notification.Instance.Definition;
        var surfaceDefinition = notification.Surfaces.First(x => x.Surface == surface);
        var border = new Border { Padding = new Thickness(surfaceDefinition.DisplayMode == NotificationDisplayMode.Compact ? 7 : 11),
            CornerRadius = new CornerRadius(7), BorderThickness = new Thickness(surface == NotificationSurface.BlockingNotice ? 2 : 1),
            Focusable = surface == NotificationSurface.BlockingNotice };
        border.Bind(BackgroundProperty, border.GetResourceObservable("DuiSurfaceRaisedBrush"));
        border.Bind(BorderBrushProperty, border.GetResourceObservable(SeverityBrush(definition.Severity)));
        var body = new StackPanel { Spacing = 5 };
        var heading = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
        if (surfaceDefinition.ShowIcon)
        {
            var icon = new SemanticIcon { Width = 17, Height = 17 };
            icon.SetIcon(icons, definition.IconKey ?? NotificationActionBarAdapter.SeverityIcon(definition.Severity));
            icon.Bind(SemanticIcon.ForegroundProperty, icon.GetResourceObservable(SeverityBrush(definition.Severity)));
            heading.Children.Add(icon);
        }
        if (surfaceDefinition.ShowTitle)
            heading.Children.Add(new TextBlock { Text = localization.Get(definition.TitleKey), FontWeight = global::Avalonia.Media.FontWeight.SemiBold,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        heading.Children.Add(new TextBlock { Text = SeverityText(definition.Severity),
            Foreground = null, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
        body.Children.Add(heading);
        if (surfaceDefinition.ShowMessage && surfaceDefinition.DisplayMode != NotificationDisplayMode.IconOnly)
            body.Children.Add(new TextBlock { Text = localization.Get(definition.MessageKey), TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        if (surfaceDefinition.ShowProgress && notification.Instance.CurrentProgress is { } progress)
        {
            body.Children.Add(new ProgressBar { Minimum = 0, Maximum = progress.MaximumValue, Value = progress.CurrentValue,
                IsIndeterminate = progress.IsIndeterminate, Height = 7 });
            body.Children.Add(new TextBlock { Text = progress.DisplayTextKey is { } key ? localization.Get(key) :
                $"{progress.CurrentValue:0} / {progress.MaximumValue:0} ({progress.Percentage:0}%)", FontSize = 11 });
        }
        if (surfaceDefinition.DisplayMode != NotificationDisplayMode.IconOnly)
        {
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            if (surfaceDefinition.ShowPrimaryAction && notification.PrimaryAction is { } primary)
                actions.Children.Add(ActionButton(notification, primary));
            if (surfaceDefinition.ShowSecondaryActions)
                foreach (var secondary in notification.SecondaryActions) actions.Children.Add(ActionButton(notification, secondary));
            if (definition.Dismissible)
            {
                var dismiss = new Button { Content = localization.Get(new("Notification.Dismiss")) };
                AutomationProperties.SetName(dismiss, localization.Get(new("Notification.Dismiss")));
                dismiss.Click += (_, _) => coordinator.Dismiss(notification.Instance.InstanceId);
                actions.Children.Add(dismiss);
            }
            if (actions.Children.Count > 0) body.Children.Add(actions);
        }
        AutomationProperties.SetName(border, $"{SeverityText(definition.Severity)}: {localization.Get(definition.TitleKey)}");
        border.Child = body;
        return border;
    }

    private Button ActionButton(ResolvedNotification notification, ResolvedGuidanceAction action)
    {
        var button = new Button { Content = localization.Get(action.Definition.DisplayNameKey), IsEnabled = action.IsEnabled };
        button.Click += async (_, _) =>
        {
            var result = await dispatcher.DispatchAsync(action,
                action.Definition.ActionType == GuidanceActionType.Dismiss ? () => coordinator.Dismiss(notification.Instance.InstanceId) : null);
            ActionCompleted?.Invoke(this, result);
        };
        return button;
    }
    private static string SeverityBrush(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Success => "DuiSuccessBrush",
        NotificationSeverity.Warning or NotificationSeverity.Critical => "DuiWarningBrush",
        NotificationSeverity.Error => "DuiErrorBrush",
        _ => "DuiInfoBrush",
    };
    private static string SeverityText(NotificationSeverity severity) => severity.ToString().ToUpperInvariant();
}
