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
                ["Demo.Company"] = "Company",
                ["Demo.CurrentCompany"] = "Current Company",
                ["Demo.CompanyProfile"] = "Read-only Company profile",
                ["Demo.PermissionCodes"] = "PermissionCode values",
                ["Demo.CapabilityCodes"] = "CapabilityCode values",
                ["Demo.Requirement"] = "Requirement",
                ["Demo.UnauthorizedBehavior"] = "Unauthorized behavior",
                ["Demo.ResolvedPresentation"] = "Resolved presentation",
                ["State.Empty"] = "Nothing to show yet.",
                ["State.Loading"] = "Loading…",
                ["State.Ready"] = "Ready",
                ["State.Error"] = "Something went wrong.",
                ["State.ReadOnly"] = "This content is read-only.",
                ["State.PermissionDenied"] = "Permission denied.",
                ["State.Unavailable"] = "This content is currently unavailable.",
                ["AppMenu.Company"] = "Current Company",
                ["AppMenu.Open"] = "Open application menu",
                ["AppMenu.Language"] = "Language",
                ["AppMenu.Appearance"] = "Appearance",
                ["AppMenu.GeneralSettings"] = "General Settings",
                ["AppMenu.Account"] = "User / Account",
                ["AppMenu.License"] = "License / Entitlement",
                ["AppMenu.About"] = "About",
                ["AppMenu.Exit"] = "Exit",
                ["AppMenu.SwitchCompany"] = "Switch Company",
                ["AppMenu.CompanyProfile"] = "Company Profile (read-only)",
                ["AppMenu.LegalName"] = "Legal Name",
                ["AppMenu.ShortName"] = "Short Name",
                ["AppMenu.TaxCode"] = "Tax Code",
                ["AppMenu.Address"] = "Address",
                ["AppMenu.Phone"] = "Phone",
                ["AppMenu.Email"] = "Email",
                ["AppMenu.Website"] = "Website",
                ["AppMenu.Representative"] = "Representative",
                ["AppMenu.Status"] = "Status",
                ["AppMenu.Theme"] = "Theme",
                ["AppMenu.Theme.System"] = "System",
                ["AppMenu.Theme.Light"] = "Light",
                ["AppMenu.Theme.Dark"] = "Dark",
                ["AppMenu.FontSize"] = "Font Size",
                ["AppMenu.GridDensity"] = "Grid Density",
                ["AppMenu.UiScaleFoundation"] = "UI Scale: 100% (shared preference foundation)",
                ["AppMenu.ResetLayout"] = "Reset UI Layout",
                ["AppMenu.NoSettings"] = "No additional general settings are registered.",
                ["AppMenu.Contributed"] = "Application Extension",
                ["AppMenu.ContributedDescription"] = "This page is supplied by the application.",
                ["AppMenu.Edition"] = "Edition",
                ["AppMenu.LicenseState"] = "License State",
                ["AppMenu.Expiration"] = "Expiration",
                ["AppMenu.Entitlements"] = "Entitlements",
                ["AppMenu.ApplicationName"] = "Application Name",
                ["AppMenu.ApplicationVersion"] = "Application Version",
                ["AppMenu.FrameworkVersion"] = "DynamicUI24 Framework Version",
                ["AppMenu.Runtime"] = "Runtime",
                ["AppMenu.Platform"] = "Operating System / Platform",
                ["Demo.Preferences"] = "Demo Preferences",
                ["Demo.SelectionCount"] = "Selection count",
                ["Ribbon.Home"] = "Home",
                ["Ribbon.Data"] = "Data",
                ["Ribbon.Reports"] = "Reports",
                ["Ribbon.Tools"] = "Tools",
                ["Ribbon.Workspace"] = "Workspace",
                ["Ribbon.Actions"] = "Actions",
                ["Ribbon.Find"] = "Find",
                ["Ribbon.ReportTools"] = "Report tools",
                ["Ribbon.Diagnostics"] = "Diagnostics",
                ["Ribbon.OpenReport"] = "Open report demo",
                ["Ribbon.Refresh"] = "Refresh",
                ["Ribbon.Hello"] = "Say hello",
                ["Ribbon.SelectionAction"] = "Selection action",
                ["Ribbon.Search"] = "Search",
                ["Ribbon.Filter"] = "Filter",
                ["Ribbon.Preview"] = "Preview",
                ["Ribbon.Export"] = "Export",
                ["Ribbon.Unknown"] = "Unknown command proof",
                ["Ribbon.RefreshComplete"] = "Demo workspace refreshed.",
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
                ["Demo.Company"] = "Công ty",
                ["Demo.CurrentCompany"] = "Công ty hiện tại",
                ["Demo.CompanyProfile"] = "Hồ sơ công ty chỉ đọc",
                ["Demo.PermissionCodes"] = "Các giá trị PermissionCode",
                ["Demo.CapabilityCodes"] = "Các giá trị CapabilityCode",
                ["Demo.Requirement"] = "Yêu cầu",
                ["Demo.UnauthorizedBehavior"] = "Ứng xử khi không có quyền",
                ["Demo.ResolvedPresentation"] = "Kết quả trình bày",
                ["State.Empty"] = "Chưa có nội dung để hiển thị.",
                ["State.Loading"] = "Đang tải…",
                ["State.Ready"] = "Sẵn sàng",
                ["State.Error"] = "Đã xảy ra lỗi.",
                ["State.ReadOnly"] = "Nội dung này chỉ được đọc.",
                ["State.PermissionDenied"] = "Không có quyền truy cập.",
                ["State.Unavailable"] = "Nội dung này hiện không khả dụng.",
                ["AppMenu.Company"] = "Công ty hiện tại",
                ["AppMenu.Open"] = "Mở menu ứng dụng",
                ["AppMenu.Language"] = "Ngôn ngữ",
                ["AppMenu.Appearance"] = "Giao diện",
                ["AppMenu.GeneralSettings"] = "Cài đặt chung",
                ["AppMenu.Account"] = "Người dùng / Tài khoản",
                ["AppMenu.License"] = "Giấy phép / Quyền lợi",
                ["AppMenu.About"] = "Giới thiệu",
                ["AppMenu.Exit"] = "Thoát",
                ["AppMenu.SwitchCompany"] = "Chuyển công ty",
                ["AppMenu.CompanyProfile"] = "Hồ sơ công ty (chỉ đọc)",
                ["AppMenu.LegalName"] = "Tên pháp lý",
                ["AppMenu.ShortName"] = "Tên ngắn",
                ["AppMenu.TaxCode"] = "Mã số thuế",
                ["AppMenu.Address"] = "Địa chỉ",
                ["AppMenu.Phone"] = "Điện thoại",
                ["AppMenu.Email"] = "Email",
                ["AppMenu.Website"] = "Website",
                ["AppMenu.Representative"] = "Người đại diện",
                ["AppMenu.Status"] = "Trạng thái",
                ["AppMenu.Theme"] = "Chủ đề",
                ["AppMenu.Theme.System"] = "Hệ thống",
                ["AppMenu.Theme.Light"] = "Sáng",
                ["AppMenu.Theme.Dark"] = "Tối",
                ["AppMenu.FontSize"] = "Cỡ chữ",
                ["AppMenu.GridDensity"] = "Mật độ lưới",
                ["AppMenu.UiScaleFoundation"] = "Tỷ lệ UI: 100% (nền tảng tùy chọn dùng chung)",
                ["AppMenu.ResetLayout"] = "Đặt lại bố cục UI",
                ["AppMenu.NoSettings"] = "Không có cài đặt chung bổ sung nào được đăng ký.",
                ["AppMenu.Contributed"] = "Tiện ích ứng dụng",
                ["AppMenu.ContributedDescription"] = "Trang này do ứng dụng cung cấp.",
                ["AppMenu.Edition"] = "Phiên bản",
                ["AppMenu.LicenseState"] = "Trạng thái giấy phép",
                ["AppMenu.Expiration"] = "Hết hạn",
                ["AppMenu.Entitlements"] = "Quyền lợi",
                ["AppMenu.ApplicationName"] = "Tên ứng dụng",
                ["AppMenu.ApplicationVersion"] = "Phiên bản ứng dụng",
                ["AppMenu.FrameworkVersion"] = "Phiên bản framework DynamicUI24",
                ["AppMenu.Runtime"] = "Runtime",
                ["AppMenu.Platform"] = "Hệ điều hành / Nền tảng",
                ["Demo.Preferences"] = "Tùy chọn Demo",
                ["Demo.SelectionCount"] = "Số mục được chọn",
                ["Ribbon.Home"] = "Trang chủ",
                ["Ribbon.Data"] = "Dữ liệu",
                ["Ribbon.Reports"] = "Báo cáo",
                ["Ribbon.Tools"] = "Công cụ",
                ["Ribbon.Workspace"] = "Không gian làm việc",
                ["Ribbon.Actions"] = "Thao tác",
                ["Ribbon.Find"] = "Tìm kiếm",
                ["Ribbon.ReportTools"] = "Công cụ báo cáo",
                ["Ribbon.Diagnostics"] = "Chẩn đoán",
                ["Ribbon.OpenReport"] = "Mở demo báo cáo",
                ["Ribbon.Refresh"] = "Làm mới",
                ["Ribbon.Hello"] = "Gửi lời chào",
                ["Ribbon.SelectionAction"] = "Thao tác vùng chọn",
                ["Ribbon.Search"] = "Tìm kiếm",
                ["Ribbon.Filter"] = "Bộ lọc",
                ["Ribbon.Preview"] = "Xem trước",
                ["Ribbon.Export"] = "Xuất",
                ["Ribbon.Unknown"] = "Kiểm chứng lệnh không xác định",
                ["Ribbon.RefreshComplete"] = "Đã làm mới workspace demo.",
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
