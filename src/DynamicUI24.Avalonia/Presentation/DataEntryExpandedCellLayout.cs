using Avalonia;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Bounds the presentation-only expanded cell surface to the current viewport.</summary>
public static class DataEntryExpandedCellLayout
{
    public static Size Resolve(string? text, Size source, Size viewport)
    {
        var value = text ?? string.Empty;
        var lines = Math.Max(1, value.Count(x => x == '\n') + 1);
        var longest = value.Split('\n').Select(x => x.Length).DefaultIfEmpty().Max();
        var maximumWidth = Math.Max(source.Width, viewport.Width * .82);
        var maximumHeight = Math.Max(source.Height, viewport.Height * .68);
        var desiredWidth = Math.Max(source.Width, Math.Max(320, longest * 8d + 32));
        var wrappedLines = Math.Max(lines, (int)Math.Ceiling(longest * 8d / Math.Max(1, desiredWidth - 32)));
        var desiredHeight = Math.Max(source.Height, Math.Max(96, 92 + wrappedLines * 22d));
        return new(Math.Min(desiredWidth, maximumWidth), Math.Min(desiredHeight, maximumHeight));
    }
}
