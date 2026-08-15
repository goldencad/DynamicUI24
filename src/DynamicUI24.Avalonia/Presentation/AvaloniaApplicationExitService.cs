using Avalonia.Controls.ApplicationLifetimes;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

public sealed class AvaloniaApplicationExitService(IClassicDesktopStyleApplicationLifetime lifetime)
    : IApplicationExitService
{
    private readonly IClassicDesktopStyleApplicationLifetime lifetime =
        lifetime ?? throw new ArgumentNullException(nameof(lifetime));

    public void RequestExit() => lifetime.Shutdown();
}
