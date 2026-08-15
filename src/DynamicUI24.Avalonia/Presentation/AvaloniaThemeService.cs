using Avalonia.Styling;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

public sealed class AvaloniaThemeService : IThemeService
{
    private readonly global::Avalonia.Application application;

    public AvaloniaThemeService(global::Avalonia.Application application)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public ThemeMode Current { get; private set; } = ThemeMode.System;
    public event EventHandler? ThemeChanged;

    public void SetTheme(ThemeMode theme)
    {
        application.RequestedThemeVariant = theme switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

        if (Current == theme)
        {
            return;
        }

        Current = theme;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
