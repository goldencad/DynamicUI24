namespace DynamicUI24.Shared.Presentation;

/// <summary>Language- and theme-independent identity for the active Shell settings page.</summary>
public sealed class SettingsNavigationState(string initialPageCode)
{
    public string CurrentPageCode { get; private set; } = Validate(initialPageCode);

    public void Navigate(string pageCode) => CurrentPageCode = Validate(pageCode);

    private static string Validate(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().ToUpperInvariant();
    }
}
