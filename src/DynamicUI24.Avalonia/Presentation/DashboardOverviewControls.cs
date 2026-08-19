using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Reusable v0.16 page composition for dashboard and overview surfaces.</summary>
public sealed class DashboardPage : StackPanel
{
    public DashboardPage(string title, string? subtitle = null)
    {
        AvaloniaTypography.ApplyUiFont(this);
        Spacing = ResourceNumber("DuiSectionGap", 24);
        MaxWidth = ResourceNumber("DuiDashboardReadableWidth", 1180);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Children.Add(new StackPanel
        {
            Spacing = ResourceNumber("DuiSpaceXs", 4),
            Children =
            {
                SemanticText(title, "DuiTypographyPageTitle", FontWeight.SemiBold, "DuiTextPrimaryBrush"),
                SemanticText(subtitle ?? string.Empty, "DuiTypographySubtitle", FontWeight.Normal,
                    "DuiTextSecondaryBrush", !string.IsNullOrWhiteSpace(subtitle)),
            },
        });
    }

    public void AddSection(string title, Control content, string? supportingText = null)
    {
        var heading = new StackPanel
        {
            Spacing = ResourceNumber("DuiSpaceXs", 4),
            Children =
            {
                SemanticText(title, "DuiTypographySectionTitle", FontWeight.SemiBold, "DuiTextPrimaryBrush"),
                SemanticText(supportingText ?? string.Empty, "DuiTypographyBodySmall", FontWeight.Normal,
                    "DuiTextSecondaryBrush", !string.IsNullOrWhiteSpace(supportingText)),
            },
        };
        Children.Add(new StackPanel
        {
            Spacing = ResourceNumber("DuiCardGap", 12),
            Children = { heading, content },
        });
    }

    internal static TextBlock SemanticText(string text, string sizeToken, FontWeight weight,
        string brushToken, bool visible = true)
    {
        var block = new TextBlock { Text = text, FontWeight = weight, IsVisible = visible, TextWrapping = TextWrapping.Wrap };
        block.Bind(TextBlock.FontSizeProperty, block.GetResourceObservable(sizeToken));
        block.Bind(TextBlock.ForegroundProperty, block.GetResourceObservable(brushToken));
        return block;
    }

    public static double ResourceNumber(string key, double fallback) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is double number ? number : fallback;
}

/// <summary>Stable label/value/context/action anatomy; metric values remain application-owned.</summary>
public sealed class MetricCard : Border
{
    public MetricCard(string label, string value, string? context = null, Control? action = null)
    {
        CornerRadius = new CornerRadius(DashboardPage.ResourceNumber("DuiRadiusMedium", 7));
        Padding = new Thickness(DashboardPage.ResourceNumber("DuiCardPadding", 16));
        BorderThickness = new Thickness(DashboardPage.ResourceNumber("DuiStrokeSubtle", 1));
        MinWidth = DashboardPage.ResourceNumber("DuiDashboardMetricMinWidth", 180);
        Bind(BackgroundProperty, this.GetResourceObservable("DuiSurfacePanelBrush"));
        Bind(BorderBrushProperty, this.GetResourceObservable("DuiBorderSubtleBrush"));
        var body = new StackPanel
        {
            Spacing = DashboardPage.ResourceNumber("DuiSpaceS", 8),
            Children =
            {
                DashboardPage.SemanticText(label, "DuiTypographyBodySmall", FontWeight.Medium, "DuiTextSecondaryBrush"),
                DashboardPage.SemanticText(value, "DuiTypographyPageTitle", FontWeight.SemiBold, "DuiTextPrimaryBrush"),
                DashboardPage.SemanticText(context ?? string.Empty, "DuiTypographyBodySmall", FontWeight.Normal,
                    "DuiTextSecondaryBrush", !string.IsNullOrWhiteSpace(context)),
            },
        };
        if (action is not null) body.Children.Add(action);
        Child = body;
    }
}

/// <summary>Shared summary/list composition used by Overview without creating another dashboard engine.</summary>
public sealed class OverviewSection : Border
{
    public OverviewSection(string title, string? summary, IEnumerable<string> items)
    {
        CornerRadius = new CornerRadius(DashboardPage.ResourceNumber("DuiRadiusMedium", 7));
        Padding = new Thickness(DashboardPage.ResourceNumber("DuiCardPadding", 16));
        BorderThickness = new Thickness(DashboardPage.ResourceNumber("DuiStrokeSubtle", 1));
        Bind(BackgroundProperty, this.GetResourceObservable("DuiSurfacePanelBrush"));
        Bind(BorderBrushProperty, this.GetResourceObservable("DuiBorderSubtleBrush"));
        var panel = new StackPanel { Spacing = DashboardPage.ResourceNumber("DuiSpaceS", 8) };
        panel.Children.Add(DashboardPage.SemanticText(title, "DuiTypographySectionTitle", FontWeight.SemiBold,
            "DuiTextPrimaryBrush"));
        panel.Children.Add(DashboardPage.SemanticText(summary ?? string.Empty, "DuiTypographyBodySmall", FontWeight.Normal,
            "DuiTextSecondaryBrush", !string.IsNullOrWhiteSpace(summary)));
        foreach (var item in items)
        {
            var row = DashboardPage.SemanticText(item, "DuiTypographyBody", FontWeight.Normal, "DuiTextPrimaryBrush");
            row.Margin = new Thickness(0, DashboardPage.ResourceNumber("DuiSpaceXs", 4), 0, 0);
            panel.Children.Add(row);
        }
        Child = panel;
    }
}
