using System.Text.RegularExpressions;

namespace DynamicUI24.Core.Templates;

/// <summary>Names a capability declared by a template without implementing it.</summary>
public sealed partial record TemplateCapability
{
    public TemplateCapability(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        if (!ValidCapability().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Capability names must contain only A-Z, 0-9, and underscores.",
                nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }
    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]+(?:_[A-Z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidCapability();
}
