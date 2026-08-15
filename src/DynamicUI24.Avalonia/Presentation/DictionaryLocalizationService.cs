using System.Globalization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

public sealed class DictionaryLocalizationService : ILocalizationService
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Catalogs =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en-US"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Shell.Workspace"] = "Workspace",
                ["Shell.Template"] = "Template code",
                ["Shell.Module"] = "Resolved module",
                ["Shell.Version"] = "Template version",
                ["Shell.Capabilities"] = "Capabilities",
                ["Shell.ResolutionError"] = "Resolution error",
                ["Shell.Exit"] = "Exit",
                ["Demo.Workspace"] = "Workspace",
                ["Demo.Theme"] = "Theme",
                ["Demo.Language"] = "Language",
                ["Demo.State"] = "State",
                ["Demo.IconSamples"] = "Semantic icon samples",
                ["State.Empty"] = "Nothing to show yet.",
                ["State.Loading"] = "Loading…",
                ["State.Ready"] = "Ready",
                ["State.Error"] = "Something went wrong.",
                ["State.ReadOnly"] = "This content is read-only.",
                ["State.PermissionDenied"] = "Permission denied.",
                ["State.Unavailable"] = "This content is currently unavailable.",
            },
            ["vi-VN"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Shell.Workspace"] = "Không gian làm việc",
                ["Shell.Template"] = "Mã template",
                ["Shell.Module"] = "Module đã phân giải",
                ["Shell.Version"] = "Phiên bản template",
                ["Shell.Capabilities"] = "Khả năng",
                ["Shell.ResolutionError"] = "Lỗi phân giải",
                ["Shell.Exit"] = "Thoát",
                ["Demo.Workspace"] = "Không gian làm việc",
                ["Demo.Theme"] = "Giao diện",
                ["Demo.Language"] = "Ngôn ngữ",
                ["Demo.State"] = "Trạng thái",
                ["Demo.IconSamples"] = "Biểu tượng ngữ nghĩa",
                ["State.Empty"] = "Chưa có nội dung để hiển thị.",
                ["State.Loading"] = "Đang tải…",
                ["State.Ready"] = "Sẵn sàng",
                ["State.Error"] = "Đã xảy ra lỗi.",
                ["State.ReadOnly"] = "Nội dung này chỉ được đọc.",
                ["State.PermissionDenied"] = "Không có quyền truy cập.",
                ["State.Unavailable"] = "Nội dung này hiện không khả dụng.",
            },
        };

    public DictionaryLocalizationService(string initialCulture = "vi-VN")
    {
        if (!TrySetCulture(initialCulture))
        {
            throw new ArgumentException("Unsupported culture.", nameof(initialCulture));
        }
    }

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("vi-VN");
    public event EventHandler? CultureChanged;

    public string Get(LocalizationKey key) =>
        Catalogs[CurrentCulture.Name].TryGetValue(key.Value, out var value)
            ? value
            : $"[{key.Value}]";

    public bool TrySetCulture(string cultureName)
    {
        if (!Catalogs.ContainsKey(cultureName))
        {
            return false;
        }

        var culture = CultureInfo.GetCultureInfo(cultureName);
        if (CurrentCulture.Equals(culture))
        {
            return true;
        }

        CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
