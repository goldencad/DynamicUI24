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

    public SemanticIconRegistry(DynamicUI24IconCatalog? catalog = null) => RegisterDefaults(catalog ?? new());

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

    private void RegisterDefaults(DynamicUI24IconCatalog catalog)
    {
        foreach (var definition in catalog.LoadDefinitions()) Register(definition);
        Register(new(StandardIconKeys.Filter, "M3,5 L21,5 L14,13 L14,20 L10,18 L10,13 Z"));
        Register(new(StandardIconKeys.Refresh, "M20,7 L20,3 L16,3 M20,3 L16,7 M19,11 A8,8 0 1 1 16,6"));
        Register(new(StandardIconKeys.Add, "M12,4 L12,20 M4,12 L20,12"));
        Register(new(StandardIconKeys.Edit, "M4,20 L8,19 L19,8 L16,5 L5,16 Z"));
        Register(new(StandardIconKeys.Delete, "M6,7 L18,7 M9,7 L9,20 M15,7 L15,20 M8,4 L16,4 L18,7 L6,7 Z"));
        Register(new(StandardIconKeys.Import, "M12,3 L12,15 M7,10 L12,15 L17,10 M4,19 L20,19"));
        Register(new(StandardIconKeys.Export, "M12,15 L12,3 M7,8 L12,3 L17,8 M4,19 L20,19"));
        Register(new(StandardIconKeys.Preview, "M2,12 C6,5 18,5 22,12 C18,19 6,19 2,12 M12,9 A3,3 0 1 0 12,15 A3,3 0 1 0 12,9"));
        Register(new(StandardIconKeys.Validate, "M3,12 L9,18 L21,5"));
        Register(new(StandardIconKeys.Commit, "M4,12 L9,17 L20,6 M4,20 L20,20"));
        Register(new(StandardIconKeys.Settings, "M12,3 A2,2 0 1 0 12,7 A2,2 0 1 0 12,3 M12,9 A3,3 0 1 0 12,15 A3,3 0 1 0 12,9 M12,17 A2,2 0 1 0 12,21 A2,2 0 1 0 12,17"));
        Register(new(StandardIconKeys.Warning, "M12,3 L22,21 L2,21 Z M12,9 L12,15 M12,18 L12,18.1"));
        Register(new(StandardIconKeys.Error, "M4,4 L20,20 M20,4 L4,20"));
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
        Register(new(StandardIconKeys.Setup, "M4,4 L20,4 L20,20 L4,20 Z M8,8 L16,8 M8,12 L16,12 M8,16 L13,16"));
        Register(new(StandardIconKeys.Catalog, "M4,5 L10,5 L12,7 L20,7 L20,19 L4,19 Z"));
        Register(new(StandardIconKeys.Workspace, "M3,4 L21,4 L21,20 L3,20 Z M9,4 L9,20"));
        Register(new(StandardIconKeys.Columns, "M4,4 L20,4 L20,20 L4,20 Z M10,4 L10,20 M15,4 L15,20"));
        Register(new(StandardIconKeys.Variable, "M5,6 L9,6 L12,18 L15,6 L19,6"));
        Register(new(StandardIconKeys.Tree, "M12,4 L12,9 M6,9 L18,9 M6,9 L6,15 M18,9 L18,15 M3,15 L9,15 L9,20 L3,20 Z M15,15 L21,15 L21,20 L15,20 Z"));
        Register(new(StandardIconKeys.Ribbon, "M3,5 L21,5 L21,13 L3,13 Z M6,8 L10,8 M13,8 L18,8 M6,16 L18,16 M6,19 L14,19"));
        Register(new(StandardIconKeys.Action, "M4,12 L18,12 M13,7 L18,12 L13,17 M4,5 L8,5 M4,19 L8,19"));
        Register(new(StandardIconKeys.Dashboard, "M4,4 L11,4 L11,11 L4,11 Z M13,4 L20,4 L20,11 L13,11 Z M4,13 L11,13 L11,20 L4,20 Z M13,13 L20,13 L20,20 L13,20 Z"));
        Register(new(StandardIconKeys.Report, "M5,3 L16,3 L20,7 L20,21 L5,21 Z M15,3 L15,8 L20,8 M8,12 L17,12 M8,16 L17,16"));
        Register(new(StandardIconKeys.Clone, "M8,8 L20,8 L20,20 L8,20 Z M4,4 L16,4 L16,8 M4,4 L4,16 L8,16"));
        Register(new(StandardIconKeys.Publish, "M12,20 L12,5 M7,10 L12,5 L17,10 M5,20 L19,20"));
        Register(new(StandardIconKeys.Retire, "M5,7 L19,7 M8,7 L8,20 M16,7 L16,20 M7,4 L17,4 L19,7 L5,7 Z M11,11 L15,15 M15,11 L11,15"));
        Register(new(StandardIconKeys.Privacy, "M12,3 C7,3 4,7 4,12 C4,17 8,20 12,22 C16,20 20,17 20,12 C20,7 17,3 12,3 M9,12 A3,3 0 1 0 15,12 A3,3 0 1 0 9,12"));
        Register(new(StandardIconKeys.PrivacyOn, "M6,11 L6,20 L18,20 L18,11 Z M8,11 L8,8 A4,4 0 0 1 16,8 L16,11"));
        Register(new(StandardIconKeys.PrivacyOff, "M5,5 L19,19 M6,11 L6,20 L18,20 L18,11 M9,9 L9,8 A3,3 0 0 1 14,6"));
        Register(new(StandardIconKeys.PrivacyAuto, "M6,11 L6,20 L18,20 L18,11 Z M8,11 L8,8 A4,4 0 0 1 16,8 L16,11 M12,14 L12,17"));
        Register(new(StandardIconKeys.Hide, "M3,3 L21,21 M2,12 C6,5 18,5 22,12"));
        Register(new(StandardIconKeys.Restricted, "M12,3 L21,7 L20,15 C18,19 15,21 12,22 C9,21 6,19 4,15 L3,7 Z"));
    }
}
