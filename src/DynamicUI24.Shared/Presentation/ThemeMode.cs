namespace DynamicUI24.Shared.Presentation;

/// <summary>The runtime appearance selected by the host application.</summary>
public enum ThemeMode
{
    System,
    Light,
    Dark,
}

public interface IThemeService
{
    ThemeMode Current { get; }
    event EventHandler? ThemeChanged;
    void SetTheme(ThemeMode theme);
}
