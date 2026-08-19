namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Platform-owned font stacks used by the current Avalonia theme.</summary>
public static class AvaloniaPlatformFontMapping
{
    public static IReadOnlyList<string> UiFallbackStack => OperatingSystem.IsMacOS()
        ? [".AppleSystemUIFont", "Arial Unicode MS", "sans-serif"]
        : OperatingSystem.IsWindows()
            ? ["Segoe UI", "Arial", "sans-serif"]
            : ["Noto Sans", "DejaVu Sans", "sans-serif"];

    public static IReadOnlyList<string> CodeFallbackStack => OperatingSystem.IsMacOS()
        ? ["SFMono-Regular", "Menlo", "monospace"]
        : OperatingSystem.IsWindows()
            ? ["Cascadia Mono", "Consolas", "monospace"]
            : ["Noto Sans Mono", "DejaVu Sans Mono", "monospace"];
}
