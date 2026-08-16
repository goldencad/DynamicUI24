using Avalonia;
using System.Text;

namespace DynamicUI24.Demo;

internal static class Program
{
    public static bool IsSmokeRun { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        IsSmokeRun = args.Contains("--smoke", StringComparer.Ordinal);
        if (IsSmokeRun)
        {
            var log = new StreamWriter(Path.Combine(Path.GetTempPath(), "dynamicui24-10d-smoke.log"), false, Encoding.UTF8) { AutoFlush = true };
            Console.SetOut(new TeeWriter(Console.Out, log));
            Console.SetError(new TeeWriter(Console.Error, log));
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();

    private sealed class TeeWriter(TextWriter first, TextWriter second) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public override void Write(char value) { first.Write(value); second.Write(value); }
        public override void Write(string? value) { first.Write(value); second.Write(value); }
        public override void WriteLine(string? value) { first.WriteLine(value); second.WriteLine(value); }
        public override void Flush() { first.Flush(); second.Flush(); }
    }
}
