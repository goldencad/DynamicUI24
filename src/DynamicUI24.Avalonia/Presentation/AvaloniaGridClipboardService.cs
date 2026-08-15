using Avalonia.Controls;
using DynamicUI24.Core.DataEntry;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Only platform clipboard bridge used by the grid.</summary>
public sealed class AvaloniaGridClipboardService(Control owner) : IGridClipboardService
{
    public async Task<string?> ReadTextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var clipboard = TopLevel.GetTopLevel(owner)?.Clipboard ?? throw new InvalidOperationException("GRID_CLIPBOARD_UNAVAILABLE");
        var value = await clipboard.GetTextAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return value;
    }

    public async Task WriteTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var clipboard = TopLevel.GetTopLevel(owner)?.Clipboard ?? throw new InvalidOperationException("GRID_CLIPBOARD_UNAVAILABLE");
        await clipboard.SetTextAsync(text);
        cancellationToken.ThrowIfCancellationRequested();
    }
}
