using System.Reflection;
using System.Xml.Linq;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>
/// Canonical mapping from stable semantic icon identities to replaceable DynamicUI24 SVG assets.
/// Controls consume <see cref="IIconRegistry"/> and never know these physical asset names.
/// </summary>
public sealed class DynamicUI24IconCatalog
{
    public const string CanonicalViewBox = "0 0 24 24";
    private const string ResourcePrefix = "DynamicUI24.Icons.";

    private static readonly IReadOnlyDictionary<IconKey, string> Assets = new Dictionary<IconKey, string>
    {
        [StandardIconKeys.Clock] = "clock.svg",
        [StandardIconKeys.Calendar] = "calendar.svg",
        [StandardIconKeys.ChevronDown] = "chevron-down.svg",
        [StandardIconKeys.Search] = "search.svg",
        [StandardIconKeys.Info] = "help.svg",
        [StandardIconKeys.Help] = "help.svg",
        [StandardIconKeys.Clear] = "clear.svg",
        [StandardIconKeys.Reveal] = "reveal.svg",
        [StandardIconKeys.OpenBrowse] = "open-browse.svg",
        [StandardIconKeys.More] = "overflow.svg",
        [StandardIconKeys.Check] = "check.svg",
        [StandardIconKeys.Indeterminate] = "indeterminate.svg",
    };

    private readonly Func<string, Stream> openAsset;

    public DynamicUI24IconCatalog(Func<string, Stream>? openAsset = null) =>
        this.openAsset = openAsset ?? OpenEmbeddedAsset;

    public IEnumerable<IconDefinition> LoadDefinitions() => Assets.Select(pair => Load(pair.Key, pair.Value));

    public string AssetPath(IconKey key) => Assets.TryGetValue(key, out var file)
        ? $"Assets/Icons/{file}"
        : throw new KeyNotFoundException($"No DynamicUI24 SVG asset is mapped for semantic icon '{key}'.");

    private IconDefinition Load(IconKey key, string file)
    {
        using var stream = openAsset(file);
        var document = XDocument.Load(stream, LoadOptions.None);
        var root = document.Root ?? throw new InvalidDataException($"SVG asset '{file}' has no root element.");
        if (!string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(root.Attribute("viewBox")?.Value, CanonicalViewBox, StringComparison.Ordinal))
            throw new InvalidDataException($"SVG asset '{file}' must use viewBox '{CanonicalViewBox}'.");

        ValidateTint(root, file);
        var paths = root.Descendants().Where(x => x.Name.LocalName == "path")
            .Select(x => x.Attribute("d")?.Value).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (paths.Length == 0) throw new InvalidDataException($"SVG asset '{file}' contains no path geometry.");
        var stroke = root.Attribute("stroke")?.Value == "currentColor";
        var strokeWidth = stroke && double.TryParse(root.Attribute("stroke-width")?.Value,
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var width)
            ? width : 0;
        return new IconDefinition(key, new SvgIconSource(string.Join(' ', paths!), $"Assets/Icons/{file}",
            stroke ? SvgPaintMode.Stroke : SvgPaintMode.Fill, strokeWidth,
            root.Attribute("stroke-linecap")?.Value == "round",
            root.Attribute("stroke-linejoin")?.Value == "round"));
    }

    private static void ValidateTint(XElement root, string file)
    {
        foreach (var attribute in root.DescendantsAndSelf().Attributes()
                     .Where(x => x.Name.LocalName is "fill" or "stroke"))
        {
            if (attribute.Value is "none" or "currentColor") continue;
            throw new InvalidDataException($"SVG asset '{file}' hard-codes {attribute.Name.LocalName}; use currentColor or none.");
        }
    }

    private static Stream OpenEmbeddedAsset(string file)
    {
        var assembly = typeof(DynamicUI24IconCatalog).Assembly;
        return assembly.GetManifestResourceStream(ResourcePrefix + file)
            ?? throw new FileNotFoundException($"Required DynamicUI24 icon asset '{file}' is missing.", file);
    }
}
