using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicUI24.Core.ActionBars;
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

    private void Render()
    {
        if (resolved is null)
        {
            Clear();
            return;
        }

        IsVisible = resolved.Definition.IsVisible;
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        foreach (var action in resolved.Actions)
        {
            var definition = action.Definition;
            var icon = new SemanticIcon { Width = 16, Height = 16 };
            icon.SetIcon(icons, definition.IconKey);
            var iconBrush = executionContext!.ResolutionContext.PresentationState.Kind switch
            {
                PresentationStateKind.Error => "DuiErrorBrush",
                PresentationStateKind.Unavailable => "DuiWarningBrush",
                PresentationStateKind.ReadOnly => "DuiTextMutedBrush",
                _ when !action.IsEnabled => "DuiDisabledBrush",
                _ => "DuiAccentBrush",
            };
            icon.Bind(SemanticIcon.ForegroundProperty, icon.GetResourceObservable(iconBrush));
            var label = new TextBlock
            {
                Text = localization.Get(definition.DisplayNameKey),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var button = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children = { icon, label },
                },
                IsEnabled = action.IsEnabled,
                Tag = definition.ActionCode,
            };
            if (action.IsReadOnly) ToolTip.SetTip(button, localization.Get(new("State.ReadOnly")));
            button.Click += async (_, _) => await ExecuteActionAsync((string)button.Tag!);
            actions.Children.Add(button);
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
}
