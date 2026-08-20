using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace DynamicUI24.Avalonia.Presentation.Editors;

/// <summary>Shared readable-width page anatomy for metadata-driven business forms.</summary>
public sealed class UniversalFormPanel : StackPanel
{
    public UniversalFormPanel()
    {
        Spacing = EditorPresentationTokens.SectionGap;
        MaxWidth = EditorPresentationTokens.FormMaxReadableWidth;
        HorizontalAlignment = HorizontalAlignment.Left;
    }
}

/// <summary>A low-chrome semantic section whose fields wrap as available width changes.</summary>
public sealed class UniversalFormSection : StackPanel
{
    public UniversalFormSection(string title, string? supportingText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Spacing = EditorPresentationTokens.FieldGroupGap;
        Title = new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeight.SemiBold };
        Fields = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        Children.Add(Title);
        if (!string.IsNullOrWhiteSpace(supportingText))
            Children.Add(new TextBlock { Text = supportingText, TextWrapping = TextWrapping.Wrap });
        Children.Add(Fields);
    }

    public TextBlock Title { get; }
    public WrapPanel Fields { get; }

    public void AddField(Control field)
    {
        ArgumentNullException.ThrowIfNull(field);
        field.Margin = new global::Avalonia.Thickness(0, 0, EditorPresentationTokens.FieldGroupGap,
            EditorPresentationTokens.FieldGroupGap);
        Fields.Children.Add(field);
    }
}
