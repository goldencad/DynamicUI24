namespace DynamicUI24.Core.Templates;

/// <summary>A small immutable version identity for a template contract.</summary>
public readonly record struct TemplateVersion : IComparable<TemplateVersion>
{
    public TemplateVersion(int major, int minor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(major);
        ArgumentOutOfRangeException.ThrowIfNegative(minor);
        Major = major;
        Minor = minor;
    }

    public int Major { get; }
    public int Minor { get; }

    public int CompareTo(TemplateVersion other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        return majorComparison != 0 ? majorComparison : Minor.CompareTo(other.Minor);
    }

    public override string ToString() => $"{Major}.{Minor}";
}
