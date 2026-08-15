using Avalonia;

namespace DynamicUI24.Demo;

internal static class Program
{
    public static bool IsSmokeRun { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        IsSmokeRun = args.Contains("--smoke", StringComparer.Ordinal);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();
}
