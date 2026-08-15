using Avalonia.Controls;
using Avalonia.Layout;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Minimal shell-level message implementation; domain workflows supply their own dialogs later.</summary>
public sealed class AvaloniaMessageService(Func<Window?> ownerProvider) : IMessageService
{
    public async Task<MessageResult> ShowAsync(
        MessageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var completion = new TaskCompletionSource<MessageResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var window = new Window
        {
            Title = request.Title,
            Width = 440,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new global::Avalonia.Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = request.Message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
                    buttons,
                },
            },
        };

        void AddButton(string text, MessageResult result)
        {
            var button = new Button { Content = text };
            button.Click += (_, _) =>
            {
                completion.TrySetResult(result);
                window.Close();
            };
            buttons.Children.Add(button);
        }

        if (request.Kind == MessageKind.Confirmation)
        {
            AddButton("Cancel", MessageResult.Cancelled);
            AddButton("Confirm", MessageResult.Confirmed);
        }
        else
        {
            AddButton("OK", MessageResult.Acknowledged);
        }

        window.Closed += (_, _) => completion.TrySetResult(
            request.Kind == MessageKind.Confirmation ? MessageResult.Cancelled : MessageResult.Acknowledged);

        var owner = ownerProvider();
        if (owner is null)
        {
            window.Show();
        }
        else
        {
            _ = window.ShowDialog(owner);
        }

        using var registration = cancellationToken.Register(() =>
        {
            completion.TrySetCanceled(cancellationToken);
            window.Close();
        });
        return await completion.Task.ConfigureAwait(true);
    }
}
