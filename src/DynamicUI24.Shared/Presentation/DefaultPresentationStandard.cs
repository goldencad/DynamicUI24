namespace DynamicUI24.Shared.Presentation;

/// <summary>The initial v0.16 semantic Standard contract.</summary>
public sealed class DefaultPresentationStandard : IPresentationStandard
{
    public const string Version = "0.16";
    public string StandardVersion => Version;
    public IReadOnlySet<FoundationTokenCategory> FoundationCategories { get; } =
        Enum.GetValues<FoundationTokenCategory>().ToHashSet();
    public IReadOnlySet<DensityRole> Densities { get; } = Enum.GetValues<DensityRole>().ToHashSet();
    public IReadOnlySet<ComponentRole> ComponentRoles { get; } = Enum.GetValues<ComponentRole>().ToHashSet();
    public IReadOnlySet<ButtonRole> ButtonRoles { get; } = Enum.GetValues<ButtonRole>().ToHashSet();
    public IReadOnlySet<EditorRole> EditorRoles { get; } = Enum.GetValues<EditorRole>().ToHashSet();
    public IReadOnlySet<GridRole> GridRoles { get; } = Enum.GetValues<GridRole>().ToHashSet();
    public IReadOnlySet<NavigationTreePart> NavigationTreeParts { get; } = Enum.GetValues<NavigationTreePart>().ToHashSet();
}
