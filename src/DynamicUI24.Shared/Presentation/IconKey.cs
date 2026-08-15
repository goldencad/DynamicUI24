namespace DynamicUI24.Shared.Presentation;

/// <summary>Semantic icon identity. Its value is deliberately unrelated to an asset path.</summary>
public readonly record struct IconKey
{
    public IconKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public static class StandardIconKeys
{
    public static IconKey Search { get; } = new("SEARCH");
    public static IconKey Filter { get; } = new("FILTER");
    public static IconKey Refresh { get; } = new("REFRESH");
    public static IconKey Add { get; } = new("ADD");
    public static IconKey Edit { get; } = new("EDIT");
    public static IconKey Delete { get; } = new("DELETE");
    public static IconKey Import { get; } = new("IMPORT");
    public static IconKey Export { get; } = new("EXPORT");
    public static IconKey Preview { get; } = new("PREVIEW");
    public static IconKey Settings { get; } = new("SETTINGS");
    public static IconKey Warning { get; } = new("WARNING");
    public static IconKey Error { get; } = new("ERROR");
    public static IconKey Info { get; } = new("INFO");
    public static IconKey Success { get; } = new("SUCCESS");
    public static IconKey Formula { get; } = new("FORMULA");
    public static IconKey Application { get; } = new("APPLICATION");
    public static IconKey Company { get; } = new("COMPANY");
    public static IconKey SwitchCompany { get; } = new("SWITCH_COMPANY");
    public static IconKey Profile { get; } = new("PROFILE");
    public static IconKey Language { get; } = new("LANGUAGE");
    public static IconKey Appearance { get; } = new("APPEARANCE");
    public static IconKey Account { get; } = new("ACCOUNT");
    public static IconKey License { get; } = new("LICENSE");
    public static IconKey About { get; } = new("ABOUT");
    public static IconKey Exit { get; } = new("EXIT");
}

/// <summary>Portable SVG path payload resolved from a semantic key.</summary>
public sealed record IconDefinition(IconKey Key, string SvgPathData, bool IsFallback = false);

public interface IIconRegistry
{
    IconDefinition Resolve(IconKey key);
}
