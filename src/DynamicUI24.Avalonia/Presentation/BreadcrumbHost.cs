using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using DynamicUI24.Core.Context;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Responsive semantic breadcrumb; activation is delegated to the shared navigation service.</summary>
public sealed class BreadcrumbHost : StackPanel
{
    private readonly ILocalizationService localization;
    private BreadcrumbPath? path;
    public BreadcrumbHost(ILocalizationService localization)
    {
        this.localization = localization; Orientation = Orientation.Horizontal; Spacing = 6;
        AvaloniaTypography.ApplyUiFont(this);
        AutomationProperties.SetName(this, "Breadcrumb");
        localization.CultureChanged += (_, _) => Rebuild(); SizeChanged += (_, _) => Rebuild();
    }
    public event EventHandler<BreadcrumbItem>? ItemActivated;
    public BreadcrumbPath? Path { get => path; set { path = value; Rebuild(); } }
    private void Rebuild()
    {
        Children.Clear(); if (path is null) return;
        var maximum = Bounds.Width > 0 ? Math.Max(2, (int)(Bounds.Width / 145)) : 4;
        var layout = BreadcrumbOverflowResolver.Resolve(path, maximum);
        for (var index = 0; index < layout.Visible.Length; index++)
        {
            if (index > 0) Children.Add(BodyText(">"));
            if (layout.HasOverflow && index == 1)
            {
                var overflow = new Button { Content = "…" };
                var flyout = new MenuFlyout();
                foreach (var hidden in layout.Overflow)
                {
                    var item = new MenuItem { Header = Localize(hidden.DisplayNameKey), IsEnabled = hidden.NavigationTarget is not null };
                    item.Click += (_, _) => ItemActivated?.Invoke(this, hidden); flyout.Items.Add(item);
                }
                overflow.Flyout = flyout; Children.Add(overflow);
            }
            var crumb = layout.Visible[index];
            if (crumb.IsCurrent) Children.Add(BodyText(Localize(crumb.DisplayNameKey), true));
            else
            {
                var button = new Button { Content = Localize(crumb.DisplayNameKey), IsEnabled = crumb.NavigationTarget is not null };
                button.Click += (_, _) => ItemActivated?.Invoke(this, crumb); Children.Add(button);
            }
        }
    }
    private static TextBlock BodyText(string text, bool strong = false)
    {
        var block = new TextBlock { Text = text, FontWeight = strong ? global::Avalonia.Media.FontWeight.SemiBold :
            global::Avalonia.Media.FontWeight.Normal, VerticalAlignment = VerticalAlignment.Center };
        block.Bind(TextBlock.FontSizeProperty, block.GetResourceObservable("DuiTypographyBody"));
        return block;
    }
    private string Localize(string key) { var value = localization.Get(new LocalizationKey(key)); return value == key ? key : value; }
}
