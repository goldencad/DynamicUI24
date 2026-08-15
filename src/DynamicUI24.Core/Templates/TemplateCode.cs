using System.Text.RegularExpressions;

namespace DynamicUI24.Core.Templates;

/// <summary>Identifies a template using a normalized, extensible code.</summary>
public sealed partial record TemplateCode
{
    public TemplateCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToUpperInvariant();
        if (!ValidCode().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Template codes must contain only A-Z, 0-9, and underscores.",
                nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]+(?:_[A-Z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidCode();
}
