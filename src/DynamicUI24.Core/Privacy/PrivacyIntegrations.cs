using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.ApplicationMenu;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Privacy;

public sealed record PrivacyFieldValue(string FieldCode, object? Value, SensitiveContentDefinition? Metadata = null);
public sealed record PrivacyFieldPresentation(string FieldCode, SensitiveValuePresentation Value,
    ResolvedPrivacyPresentation Resolution);

/// <summary>Shared Form/Detail projection. It deliberately owns no policy decisions.</summary>
public sealed class PrivacyDetailPresenter(IPrivacyPolicyResolver resolver, ISensitiveValuePresenter presenter)
{
    public ImmutableArray<PrivacyFieldPresentation> Present(IEnumerable<PrivacyFieldValue> fields,
        Func<PrivacyFieldValue, PrivacyResolutionContext> contextFactory, CultureInfo? culture = null) =>
        fields.Select(field =>
        {
            var resolved = resolver.Resolve(contextFactory(field));
            return new PrivacyFieldPresentation(field.FieldCode,
                presenter.Present(field.Value, field.Metadata, resolved, culture), resolved);
        }).ToImmutableArray();
}

public sealed record PrivacySearchResultPresentation(string StableId, string SafeTitle, string SafeSubtitle,
    string NavigationTarget, ResolvedPrivacyPresentation Resolution);

public static class PrivacySearchPresentation
{
    public static PrivacySearchResultPresentation Resolve(string stableId, string safeTitle, object? subtitle,
        string navigationTarget, SensitiveContentDefinition? metadata, PrivacyResolutionContext context,
        IPrivacyPolicyResolver resolver, ISensitiveValuePresenter presenter)
    {
        var result = resolver.Resolve(context with { Metadata = metadata });
        return new(stableId, safeTitle, presenter.Present(subtitle, metadata, result).DisplayValue, navigationTarget, result);
    }
}

public sealed record PrivacyNotificationField(string FieldCode, SensitiveValuePresentation Value,
    ResolvedPrivacyPresentation Resolution);

public static class PrivacyNotificationPresentation
{
    public static PrivacyNotificationField Resolve(string fieldCode, object? value, SensitiveContentDefinition? metadata,
        PrivacyResolutionContext context, IPrivacyPolicyResolver resolver, ISensitiveValuePresenter presenter)
    {
        var result = resolver.Resolve(context with { Metadata = metadata });
        return new(fieldCode, presenter.Present(value, metadata, result), result);
    }
}

public sealed record PrivacyExportDecision(ProtectedValueDisposition Disposition, object? Value,
    ResolvedPrivacyPresentation Resolution, string ReasonCode);

public static class PrivacyImportExportPolicy
{
    public static SensitiveValuePresentation PresentImportPreview(object? value, SensitiveContentDefinition? metadata,
        PrivacyResolutionContext context, IPrivacyPolicyResolver resolver, ISensitiveValuePresenter presenter)
    {
        var result = resolver.Resolve(context with { Metadata = metadata });
        return presenter.Present(value, metadata, result);
    }

    public static PrivacyExportDecision ResolveExport(object? value, SensitiveContentDefinition? metadata,
        PrivacyResolutionContext context, IPrivacyPolicyResolver resolver, ISensitiveValuePresenter presenter,
        ProtectedValueDisposition protectedDisposition = ProtectedValueDisposition.Omit)
    {
        var result = resolver.Resolve(context with { Metadata = metadata });
        if (result.CanExport) return new(ProtectedValueDisposition.Raw, value, result, "EXPORT_RAW_ALLOWED");
        if (protectedDisposition == ProtectedValueDisposition.Masked)
            return new(protectedDisposition, presenter.Present(value, metadata, result).DisplayValue, result, "EXPORT_MASKED");
        return new(protectedDisposition, null, result, protectedDisposition == ProtectedValueDisposition.Block ? "EXPORT_BLOCKED" : "EXPORT_OMITTED");
    }
}

public sealed record PrivacyClipboardValue(object? Value, ColumnDefinition Column, ResolvedPrivacyPresentation Resolution);
public static class PrivacyClipboardPolicy
{
    public static string Serialize(IEnumerable<IEnumerable<PrivacyClipboardValue>> rows, ISensitiveValuePresenter presenter,
        ProtectedValueDisposition protectedDisposition = ProtectedValueDisposition.Masked, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        return string.Join('\n', rows.Select(row => string.Join('\t', row.Select(cell =>
        {
            if (cell.Resolution.CanCopy) return SafeCell(GridValue(cell.Value, cell.Column, culture));
            if (protectedDisposition == ProtectedValueDisposition.Block) return string.Empty;
            return SafeCell(presenter.Present(cell.Value, cell.Column.SensitiveContent, cell.Resolution, culture).DisplayValue);
        }))));
    }
    private static string GridValue(object? value, ColumnDefinition column, CultureInfo culture) => value switch
    {
        null => string.Empty, IFormattable f => f.ToString(column.Format, culture) ?? string.Empty, _ => value.ToString() ?? string.Empty,
    };
    private static string SafeCell(string value) => value.Replace('\t', ' ').Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace('\r', '\n').Replace('\n', ' ');
}

public static class PrivacyShellDefinitions
{
    public static ActionDefinition TopAction(PrivacyMode requestedMode) => new("privacy", "PRIVACY",
        new("Privacy.Title"), requestedMode switch { PrivacyMode.Off => StandardIconKeys.PrivacyOff,
            PrivacyMode.On => StandardIconKeys.PrivacyOn, _ => StandardIconKeys.PrivacyAuto }, ActionType.ApplicationCommand,
        displayOrder: 900, registeredCommandCode: "PRIVACY.SET_MODE", buttonVariant: ActionButtonVariant.DropdownButton,
        menuItems:
        [
            Item("auto", "PRIVACY_AUTO", "Privacy.Auto", "PRIVACY.SET_AUTO", 10),
            Item("on", "PRIVACY_ON", "Privacy.On", "PRIVACY.SET_ON", 20),
            Item("off", "PRIVACY_OFF", "Privacy.Off", "PRIVACY.SET_OFF", 30),
            new("separator", "PRIVACY_SEPARATOR", new("Privacy.Title"), displayOrder: 40, kind: ActionMenuItemKind.Separator),
            Item("reveal", "PRIVACY_REVEAL", "Privacy.Reveal", "PRIVACY.REVEAL", 50, StandardIconKeys.Reveal),
            Item("settings", "PRIVACY_SETTINGS", "Privacy.Settings", "PRIVACY.SETTINGS", 60, StandardIconKeys.Settings),
        ]);

    public static ApplicationMenuItem SettingsMenuItem() => new("PRIVACY_SETTINGS", new("Privacy.Settings"),
        StandardIconKeys.Privacy, 350, ApplicationMenuItemType.SettingPage, "PRIVACY_SETTINGS", IsStandard: true);
    public static string CompactState(PrivacyMode requested, PrivacyMode effective) => requested == effective
        ? $"Privacy: {requested}" : $"Privacy: {requested} (policy: {effective})";
    private static ActionMenuItemDefinition Item(string id, string code, string key, string command, int order, IconKey? icon = null) =>
        new(id, code, new(key), icon, command, order);
}
