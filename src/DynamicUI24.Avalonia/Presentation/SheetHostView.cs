using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using DynamicUI24.Core.Sheets;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Avalonia adapter for Core sheet state. All activation remains semantic SheetCode-based.</summary>
public sealed class SheetHostView : UserControl
{
    private readonly SheetHostRuntime runtime;
    private readonly Func<SheetDefinition, SheetPresentation> present;
    private readonly Func<SheetDefinition, object, Control> contentFactory;
    private readonly int maximumVisibleTabs;

    public SheetHostView(SheetHostRuntime runtime, Func<SheetDefinition, SheetPresentation> present,
        Func<SheetDefinition, object, Control> contentFactory, int maximumVisibleTabs = 4)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.present = present ?? throw new ArgumentNullException(nameof(present));
        this.contentFactory = contentFactory ?? throw new ArgumentNullException(nameof(contentFactory));
        if (maximumVisibleTabs <= 0) throw new ArgumentOutOfRangeException(nameof(maximumVisibleTabs));
        this.maximumVisibleTabs = maximumVisibleTabs;
        runtime.Changed += (_, _) => Rebuild(); Rebuild();
    }

    public void Rebuild()
    {
        // Detach the previous tree before reusing the active adapter control in the replacement tree.
        Content = null;
        var root = new DockPanel();
        var tabs = BuildTabs(); DockPanel.SetDock(tabs, runtime.Definition.TabPlacement == SheetTabPlacement.Top ? Dock.Top : Dock.Bottom);
        root.Children.Add(tabs);
        if (runtime.ActiveSheetCode is { } active)
        {
            var definition = runtime.Sheets.First(x => x.SheetCode == active);
            var model = present(definition);
            var header = new StackPanel { Spacing = 2, Margin = new(12, 10, 12, 8), Children =
                { new TextBlock { Text = model.Title, FontSize = 20, FontWeight = global::Avalonia.Media.FontWeight.SemiBold } } };
            if (!string.IsNullOrWhiteSpace(model.Subtitle)) header.Children.Add(new TextBlock { Text = model.Subtitle, Opacity = .72 });
            AutomationProperties.SetName(header, string.Join(". ", new[] { model.Title, model.Subtitle }.Where(x => !string.IsNullOrWhiteSpace(x))));
            var body = new DockPanel(); DockPanel.SetDock(header, Dock.Top); body.Children.Add(header);
            if (runtime.GetActiveRuntime() is { } value)
            {
                var activeContent = contentFactory(definition, value);
                if (activeContent.Parent is Panel previousPanel) previousPanel.Children.Remove(activeContent);
                else if (activeContent.Parent is ContentControl previousContent) previousContent.Content = null;
                body.Children.Add(activeContent);
            }
            root.Children.Add(body);
        }
        Content = root;
    }

    private Control BuildTabs()
    {
        var visible = runtime.VisibleSheets;
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new(8, 6) };
        foreach (var sheet in visible.Take(maximumVisibleTabs)) row.Children.Add(Tab(sheet));
        var overflowSheets = visible.Skip(maximumVisibleTabs).Concat(runtime.HiddenSheets).ToArray();
        if (overflowSheets.Length > 0)
        {
            var overflow = new MenuItem { Header = $"… {overflowSheets.Length}" };
            foreach (var sheet in overflowSheets.Where(x => !x.IsHidden)) overflow.Items.Add(TabMenu(sheet));
            row.Children.Add(new Menu { Items = { overflow } });
        }
        return row;
    }
    private Button Tab(SheetDefinition sheet)
    {
        var model = present(sheet); var button = new Button { Content = model.Title, Tag = sheet.SheetCode,
            FontWeight = model.IsActive ? global::Avalonia.Media.FontWeight.Bold : global::Avalonia.Media.FontWeight.Normal };
        AutomationProperties.SetName(button, model.Title); button.Click += (_, _) => runtime.TryActivate(sheet.SheetCode); return button;
    }
    private MenuItem TabMenu(SheetDefinition sheet)
    {
        var model = present(sheet); var item = new MenuItem { Header = model.Title, Tag = sheet.SheetCode };
        item.Click += (_, _) => runtime.TryActivate(sheet.SheetCode); return item;
    }
}
