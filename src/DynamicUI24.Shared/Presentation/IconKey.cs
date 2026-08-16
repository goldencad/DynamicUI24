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
    public static IconKey Validate { get; } = new("VALIDATE");
    public static IconKey Commit { get; } = new("COMMIT");
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
    public static IconKey Setup { get; } = new("SETUP");
    public static IconKey Catalog { get; } = new("CATALOG");
    public static IconKey Workspace { get; } = new("WORKSPACE");
    public static IconKey Columns { get; } = new("COLUMNS");
    public static IconKey Variable { get; } = new("VARIABLE");
    public static IconKey Tree { get; } = new("TREE");
    public static IconKey Ribbon { get; } = new("RIBBON");
    public static IconKey Action { get; } = new("ACTION");
    public static IconKey Dashboard { get; } = new("DASHBOARD");
    public static IconKey Report { get; } = new("REPORT");
    public static IconKey Clone { get; } = new("CLONE");
    public static IconKey Publish { get; } = new("PUBLISH");
    public static IconKey Retire { get; } = new("RETIRE");
    public static IconKey More { get; } = new("MORE");
    public static IconKey Privacy { get; } = new("PRIVACY");
    public static IconKey PrivacyOn { get; } = new("PRIVACY_ON");
    public static IconKey PrivacyOff { get; } = new("PRIVACY_OFF");
    public static IconKey PrivacyAuto { get; } = new("PRIVACY_AUTO");
    public static IconKey Reveal { get; } = new("REVEAL");
    public static IconKey Hide { get; } = new("HIDE");
    public static IconKey Restricted { get; } = new("RESTRICTED");
}

/// <summary>Registry-owned source. Reusable metadata continues to expose only <see cref="IconKey"/>.</summary>
public abstract record IconSource;

/// <summary>Portable SVG geometry, optionally identified by a logical application resource name.</summary>
public sealed record SvgIconSource : IconSource
{
    public SvgIconSource(string pathData, string? resourceName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathData);
        PathData = pathData;
        ResourceName = string.IsNullOrWhiteSpace(resourceName) ? null : resourceName.Trim();
    }

    public string PathData { get; }
    public string? ResourceName { get; }
}

/// <summary>A glyph and installed/logical family name; never a raw font-file payload.</summary>
public sealed record FontGlyphIconSource : IconSource
{
    public FontGlyphIconSource(string glyph, string fontFamily)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(glyph);
        ArgumentException.ThrowIfNullOrWhiteSpace(fontFamily);
        Glyph = glyph;
        FontFamily = fontFamily.Trim();
    }

    public string Glyph { get; }
    public string FontFamily { get; }
}

public sealed record IconDefinition
{
    public IconDefinition(IconKey key, IconSource source, bool isFallback = false)
    {
        Key = key;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        IsFallback = isFallback;
    }

    public IconDefinition(IconKey key, string svgPathData, bool isFallback = false)
        : this(key, new SvgIconSource(svgPathData), isFallback) { }

    public IconKey Key { get; }
    public IconSource Source { get; }
    public bool IsFallback { get; }

    /// <summary>Compatibility projection for existing SVG consumers; empty for non-SVG sources.</summary>
    public string SvgPathData => (Source as SvgIconSource)?.PathData ?? string.Empty;
}

public interface IIconRegistry
{
    IconDefinition Resolve(IconKey key);
}
