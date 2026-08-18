using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.ModernWorkspace;

namespace DynamicUI24.Demo;

internal sealed class DemoModernWorkspace : Grid
{
    private static readonly WorkspaceCode Workspace = new("MODERN_WORKSPACE_DEMO");
    private static readonly PaneDefinition SecondaryPane = new(new("SECONDARY_CONTENT"), PaneRole.SecondaryContent,
        defaultSize: 280, minSize: 180, maxSize: 520, canCollapse: true, canResize: true, canRememberState: true);
    private readonly WorkspacePaneSessionStateStore paneState;
    private readonly StackPanel secondary = new() { Width = 280, Spacing = 8 };
    private readonly TextBlock status = new() { Text = "Ready" };
    private readonly GridSplitter splitter = new() { Width = 6, ResizeDirection = GridResizeDirection.Columns };
    public DemoModernWorkspace(WorkspacePaneSessionStateStore paneState)
    {
        this.paneState = paneState ?? throw new ArgumentNullException(nameof(paneState));
        Margin = new Thickness(18); ColumnDefinitions = new("*,Auto,280"); RowDefinitions = new("Auto,*,180");
        var toolbar = new ContextualToolbarHost();
        toolbar.Show(ContextualActionResolver.Resolve(new("DOCUMENT", ["demo-document"], 1),
            [new("OPEN", "DEMO.MODERN.OPEN", ContextualActionPlacement.ContextualToolbar), new("MORE", "DEMO.MODERN.MORE", ContextualActionPlacement.Overflow)],
            _ => UiAuthorizationState.Enabled), command => { status.Text = $"Command: {command}"; return Task.CompletedTask; });
        Grid.SetColumnSpan(toolbar, 3); Children.Add(toolbar);

        var primary = new StackPanel { Spacing = 10, Margin = new Thickness(0, 12, 12, 12) };
        primary.Children.Add(new TextBlock { Text = "Modern Workspace Demo", FontSize = 24 });
        primary.Children.Add(new ContentStateView(new(ContentPresentationState.Ready, "Primary content is ready.")));
        primary.Children.Add(new WrapPanel { Children =
        {
            new ResourceChipControl(new(ResourceKind.Document, "doc-1", "Document chip", Capabilities: ResourceCapabilities.Open | ResourceCapabilities.Remove)),
            new ResourceChipControl(new(ResourceKind.Person, "person-1", "Person chip", Capabilities: ResourceCapabilities.Open)),
            new ResourceChipControl(new(ResourceKind.File, "file-1", "Attachment.pdf", Capabilities: ResourceCapabilities.Preview))
        }});
        primary.Children.Add(status); Grid.SetRow(primary, 1); Children.Add(primary);

        Grid.SetColumn(splitter, 1); Grid.SetRow(splitter, 1); Children.Add(splitter);
        var collapse = new Button { Content = "Collapse secondary" };
        collapse.Click += (_, _) => Apply(paneState.SetCollapsed(Workspace, SecondaryPane, true,
            UiAuthorizationState.Enabled, capabilityAvailable: true));
        secondary.Children.Add(collapse);
        var expand = new Button { Content = "Expand secondary" };
        expand.Click += (_, _) => Apply(paneState.SetCollapsed(Workspace, SecondaryPane, false,
            UiAuthorizationState.Enabled, capabilityAvailable: true));
        primary.Children.Add(expand);
        secondary.Children.Add(new StructuredCompareControl(new(new("compare-1", "before", "after", "record-1"),
            [new("FIELD_A", "Same", "Same", DifferenceKind.Unchanged), new("FIELD_B", "Before", "After", DifferenceKind.Changed),
             new("FIELD_C", null, "Added", DifferenceKind.Added), new("FIELD_D", "Removed", null, DifferenceKind.Removed)], [])));
        Grid.SetColumn(secondary, 2); Grid.SetRow(secondary, 1); Children.Add(secondary);

        var bottom = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        bottom.Children.Add(new TextBlock { Text = "Activity · Running · Failed · Needs attention" });
        bottom.Children.Add(new LightweightComposerControl(new("DEMO_COMPOSER", "DEMO.MODERN.SUBMIT", ComposerSubmitMeaning.Send,
            AllowAttachments: true, AllowMentions: true, AllowActionPicker: true), text => { status.Text = $"Submitted {text.Length} characters"; return Task.CompletedTask; }));
        Grid.SetRow(bottom, 2); Grid.SetColumnSpan(bottom, 3); Children.Add(bottom);
        Apply(paneState.Resolve(Workspace, SecondaryPane, UiAuthorizationState.Enabled, capabilityAvailable: true));
    }

    private void Apply(PaneRuntimeState state)
    {
        secondary.IsVisible = state.Visible && !state.Collapsed;
        splitter.IsVisible = secondary.IsVisible;
        ColumnDefinitions[2].Width = secondary.IsVisible ? new GridLength(state.CurrentSize) : new GridLength(0);
        status.Text = state.Collapsed ? "Secondary pane collapsed; semantic session state retained." : "Secondary pane expanded.";
    }
}
