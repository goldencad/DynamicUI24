using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using DynamicUI24.Core.ModernWorkspace;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Small native-Avalonia adapter for semantic panes; it deliberately is not a docking environment.</summary>
public sealed class WorkspacePaneHost : Grid
{
    private readonly Dictionary<PaneCode, Control> controls = [];
    public void SetPane(PaneDefinition definition, PaneRuntimeState state, Func<Control> contentFactory)
    {
        ArgumentNullException.ThrowIfNull(contentFactory);
        if (!state.Visible) { RemovePane(definition.PaneCode); return; }
        if (!controls.TryGetValue(definition.PaneCode, out var control))
        {
            control = contentFactory(); controls[definition.PaneCode] = control; Children.Add(control);
        }
        control.IsVisible = !state.Collapsed;
        if (definition.Role is PaneRole.LeftNavigation or PaneRole.RightContext or PaneRole.SecondaryContent)
            control.Width = state.CurrentSize;
        else if (definition.Role == PaneRole.BottomActivity) control.Height = state.CurrentSize;
    }
    public bool RemovePane(PaneCode code)
    {
        if (!controls.Remove(code, out var control)) return false;
        Children.Remove(control); return true;
    }
}

public sealed class ContextualToolbarHost : StackPanel
{
    public ContextualToolbarHost() { Orientation = Orientation.Horizontal; Spacing = 6; }
    public void Show(IEnumerable<ResolvedContextualAction> actions, Func<string, Task> dispatch)
    {
        Children.Clear();
        foreach (var action in actions)
        {
            var button = new Button { Content = action.Definition.ActionCode,
                IsEnabled = action.State == Core.Authoring.UiAuthorizationState.Enabled };
            button.Click += async (_, _) => await dispatch(action.Definition.CommandCode);
            Children.Add(button);
        }
        IsVisible = Children.Count > 0;
    }
}

public sealed class ResourceChipControl : Button
{
    public ResourceChipControl(ResourceChip resource)
    {
        Resource = resource; Content = resource.SafeDisplayLabel;
        Classes.Add("resource-chip");
        AutomationProperties.SetName(this, resource.SafeDisplayLabel);
    }
    public ResourceChip Resource { get; }
}

public sealed class ContentStateView : StackPanel
{
    public ContentStateView(ContentStatePresentation presentation, Func<string, Task>? dispatch = null)
    {
        Spacing = 8; HorizontalAlignment = HorizontalAlignment.Center;
        AutomationProperties.SetName(this, presentation.State.ToString());
        Children.Add(new TextBlock { Text = presentation.SafeMessage, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        if (presentation.PrimaryCommandCode is { } command)
        {
            var button = new Button { Content = command };
            if (dispatch is not null) button.Click += async (_, _) => await dispatch(command);
            Children.Add(button);
        }
    }
}

public sealed class LightweightComposerControl : StackPanel
{
    private readonly TextBox input = new() { AcceptsReturn = true, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap, MinHeight = 80 };
    public LightweightComposerControl(ComposerDefinition definition, Func<string, Task> submit)
    {
        Spacing = 8; Definition = definition;
        AutomationProperties.SetName(input, definition.ComposerCode);
        Children.Add(input);
        var button = new Button { Content = definition.SubmitMeaning.ToString() };
        button.Click += async (_, _) => await submit(input.Text ?? string.Empty);
        Children.Add(button);
    }
    public ComposerDefinition Definition { get; }
    public string DraftText { get => input.Text ?? string.Empty; set => input.Text = value; }
}

public sealed class StructuredCompareControl : StackPanel
{
    public StructuredCompareControl(ComparePresentation presentation)
    {
        AutomationProperties.SetName(this, $"Compare {presentation.Identity.CompareSessionId}");
        foreach (var difference in presentation.Fields)
            Children.Add(new TextBlock { Text = $"{difference.FieldCode}: {difference.SafeBefore ?? "∅"} → {difference.SafeAfter ?? "∅"} ({difference.Kind})" });
    }
}
