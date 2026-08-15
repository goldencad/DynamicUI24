using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

public sealed class SemanticIconRegistry : IIconRegistry
{
    private static readonly IconKey MissingKey = new("MISSING");
    private static readonly IconDefinition Missing = new(
        MissingKey,
        "M3,3 L21,3 L21,21 L3,21 Z M7,7 L17,17 M17,7 L7,17",
        true);

    private readonly Dictionary<IconKey, IconDefinition> icons = new();

    public SemanticIconRegistry() => RegisterDefaults();

    public IconDefinition Resolve(IconKey key) => icons.TryGetValue(key, out var icon) ? icon : Missing;

    public void Register(IconDefinition icon, bool replace = false)
    {
        ArgumentNullException.ThrowIfNull(icon);
        if (!replace && icons.ContainsKey(icon.Key))
        {
            throw new InvalidOperationException($"Icon key '{icon.Key}' is already registered.");
        }

        icons[icon.Key] = icon;
    }

    private void RegisterDefaults()
    {
        Register(new(StandardIconKeys.Search, "M10,4 A6,6 0 1 0 10,16 A6,6 0 1 0 10,4 M14.5,14.5 L21,21"));
        Register(new(StandardIconKeys.Filter, "M3,5 L21,5 L14,13 L14,20 L10,18 L10,13 Z"));
        Register(new(StandardIconKeys.Refresh, "M20,7 L20,3 L16,3 M20,3 L16,7 M19,11 A8,8 0 1 1 16,6"));
        Register(new(StandardIconKeys.Add, "M12,4 L12,20 M4,12 L20,12"));
        Register(new(StandardIconKeys.Edit, "M4,20 L8,19 L19,8 L16,5 L5,16 Z"));
        Register(new(StandardIconKeys.Delete, "M6,7 L18,7 M9,7 L9,20 M15,7 L15,20 M8,4 L16,4 L18,7 L6,7 Z"));
        Register(new(StandardIconKeys.Import, "M12,3 L12,15 M7,10 L12,15 L17,10 M4,19 L20,19"));
        Register(new(StandardIconKeys.Export, "M12,15 L12,3 M7,8 L12,3 L17,8 M4,19 L20,19"));
        Register(new(StandardIconKeys.Preview, "M2,12 C6,5 18,5 22,12 C18,19 6,19 2,12 M12,9 A3,3 0 1 0 12,15 A3,3 0 1 0 12,9"));
        Register(new(StandardIconKeys.Settings, "M12,3 A2,2 0 1 0 12,7 A2,2 0 1 0 12,3 M12,9 A3,3 0 1 0 12,15 A3,3 0 1 0 12,9 M12,17 A2,2 0 1 0 12,21 A2,2 0 1 0 12,17"));
        Register(new(StandardIconKeys.Warning, "M12,3 L22,21 L2,21 Z M12,9 L12,15 M12,18 L12,18.1"));
        Register(new(StandardIconKeys.Error, "M4,4 L20,20 M20,4 L4,20"));
        Register(new(StandardIconKeys.Info, "M12,3 A9,9 0 1 0 12,21 A9,9 0 1 0 12,3 M12,10 L12,17 M12,7 L12,7.1"));
        Register(new(StandardIconKeys.Success, "M3,12 L9,18 L21,5"));
        Register(new(StandardIconKeys.Formula, "M18,5 L9,5 L6,12 L3,19 M6,12 L14,12 M15,16 L21,16 M18,13 L18,19"));
        Register(new(StandardIconKeys.Application, "M4,4 L20,4 L20,20 L4,20 Z"));
        Register(new(StandardIconKeys.Company, "M4,20 L4,7 L12,3 L20,7 L20,20 M8,20 L8,15 L16,15 L16,20"));
        Register(new(StandardIconKeys.SwitchCompany, "M4,8 L18,8 M15,5 L18,8 L15,11 M20,16 L6,16 M9,13 L6,16 L9,19"));
        Register(new(StandardIconKeys.Profile, "M12,3 A4,4 0 1 0 12,11 A4,4 0 1 0 12,3 M4,21 C4,15 20,15 20,21"));
        Register(new(StandardIconKeys.Language, "M12,3 A9,9 0 1 0 12,21 A9,9 0 1 0 12,3 M3,12 L21,12 M12,3 C8,8 8,16 12,21 M12,3 C16,8 16,16 12,21"));
        Register(new(StandardIconKeys.Appearance, "M12,3 A9,9 0 1 0 12,21 C14,21 15,19 14,17 C13,15 15,14 18,14 C20,14 21,12 21,10 C20,6 16,3 12,3"));
        Register(new(StandardIconKeys.Account, "M12,4 A4,4 0 1 0 12,12 A4,4 0 1 0 12,4 M4,21 C4,15 20,15 20,21"));
        Register(new(StandardIconKeys.License, "M5,4 L19,4 L19,20 L12,17 L5,20 Z M8,9 L16,9 M8,13 L14,13"));
        Register(new(StandardIconKeys.About, "M12,3 A9,9 0 1 0 12,21 A9,9 0 1 0 12,3 M12,10 L12,17 M12,7 L12,7.1"));
        Register(new(StandardIconKeys.Exit, "M10,4 L4,4 L4,20 L10,20 M14,8 L20,12 L14,16 M20,12 L8,12"));
    }
}
