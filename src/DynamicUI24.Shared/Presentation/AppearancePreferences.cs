namespace DynamicUI24.Shared.Presentation;

public enum FontSizePreference { Small, Normal, Large }
public enum GridDensityPreference { Compact, Comfortable, Large }

public sealed record AppearancePreferences(
    ThemeMode Theme = ThemeMode.System,
    double UiScale = 1d,
    FontSizePreference FontSize = FontSizePreference.Normal,
    GridDensityPreference GridDensity = GridDensityPreference.Comfortable);

public interface IAppearancePreferenceService
{
    AppearancePreferences Current { get; }
    event EventHandler? PreferencesChanged;
    void Update(AppearancePreferences preferences);
}

public sealed class AppearancePreferenceService : IAppearancePreferenceService
{
    public AppearancePreferences Current { get; private set; } = new();
    public event EventHandler? PreferencesChanged;

    public void Update(AppearancePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (preferences.UiScale is < .75 or > 2)
            throw new ArgumentOutOfRangeException(nameof(preferences), "UI scale must be between 0.75 and 2.0.");
        if (Current == preferences) return;
        Current = preferences;
        PreferencesChanged?.Invoke(this, EventArgs.Empty);
    }
}

public interface ILayoutResetService
{
    Task ResetAsync(CancellationToken cancellationToken = default);
}
