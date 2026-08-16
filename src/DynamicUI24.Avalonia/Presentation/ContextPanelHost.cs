using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using DynamicUI24.Core.Context;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Safe generic renderer. Core providers never return Avalonia controls.</summary>
public sealed class ContextPanelHost : Grid
{
    private readonly ILocalizationService localization;
    private readonly TextBlock title = new() { FontSize = 17, FontWeight = global::Avalonia.Media.FontWeight.SemiBold };
    private readonly Button close = new() { Content = "×", Width = 36 };
    private readonly TabControl sections = new();
    public ContextPanelHost(ILocalizationService localization)
    {
        this.localization = localization; RowDefinitions.Add(new(GridLength.Auto)); RowDefinitions.Add(new(GridLength.Star));
        var header = new Grid { ColumnDefinitions = new("*,Auto"), Margin = new global::Avalonia.Thickness(14, 10) };
        header.Children.Add(title); Grid.SetColumn(close, 1); header.Children.Add(close); Children.Add(header);
        Grid.SetRow(sections, 1); Children.Add(sections); close.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        AutomationProperties.SetName(this, "Context panel"); AutomationProperties.SetName(close, "Close context panel");
    }
    public event EventHandler? CloseRequested;
    public void ShowResult(ContextPanelResult result, Func<ContextItem, string>? value = null)
    {
        title.Text = result.State switch { ContextLoadingState.Loading => Local("Context.Loading", "Loading…"),
            ContextLoadingState.Error => Local("Context.Error", "Context unavailable"), _ => Local("Context.Title", "Context") };
        if (result.State != ContextLoadingState.Ready)
        {
            sections.ItemsSource = new[] { new TabItem { Header = "", Content = new TextBlock
            {
                Text = result.State == ContextLoadingState.Empty ? Local("Context.Empty", "No item selected") : title.Text,
                Margin = new global::Avalonia.Thickness(14), TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            } } };
            return;
        }
        sections.ItemsSource = result.Sections.Select(section =>
        {
            var content = new StackPanel { Margin = new global::Avalonia.Thickness(14), Spacing = 10 };
            foreach (var item in section.Items)
            {
                content.Children.Add(new StackPanel { Spacing = 2, Children =
                {
                    new TextBlock { Text = Local(item.DisplayNameKey, item.DisplayNameKey), Opacity = .7 },
                    new TextBlock { Text = value?.Invoke(item) ?? item.Value?.ToString() ?? string.Empty,
                        TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
                } });
            }
            return new TabItem { Header = Local(section.Title ?? section.SectionCode,
                section.Title ?? section.SectionCode), Content = content };
        }).ToArray();
    }
    private string Local(string key, string fallback) { var value = localization.Get(new(key)); return value == key ? fallback : value; }
}
