using System.Globalization;

namespace DynamicUI24.Shared.Presentation;

public readonly record struct LocalizationKey
{
    public LocalizationKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public interface ILocalizationService
{
    CultureInfo CurrentCulture { get; }
    event EventHandler? CultureChanged;
    string Get(LocalizationKey key);
    bool TrySetCulture(string cultureName);
}
