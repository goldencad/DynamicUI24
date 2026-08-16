using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Privacy;

namespace DynamicUI24.Core.Context;

public enum ContextPanelScope { Global, Company, Workspace, Selection }
public enum ContextLoadingState { Empty, Loading, Ready, Error }
public enum ContextItemKind { Field, Status, Action, Navigation, Text }

public readonly record struct HelpContextCode
{
    public HelpContextCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }
    public string Value { get; }
    public override string ToString() => Value;
}

public sealed record ContextPanelSectionDefinition(
    string SectionCode, string DisplayNameKey, int DisplayOrder = 0, string? IconKey = null,
    string? ProviderCode = null, PermissionCode? PermissionCode = null,
    CapabilityCode? CapabilityCode = null, bool IsCollapsible = false,
    bool DefaultExpanded = true, HelpContextCode? HelpContextCode = null)
{
    public string NormalizedCode { get; } = RequiredCode(SectionCode, nameof(SectionCode));
    private static string RequiredCode(string value, string name)
    { ArgumentException.ThrowIfNullOrWhiteSpace(value, name); return value.Trim().ToUpperInvariant(); }
}

public sealed record ContextPanelDefinition
{
    public ContextPanelDefinition(string panelCode, IEnumerable<ContextPanelSectionDefinition> sections,
        string? displayNameKey = null, string? iconKey = null, bool enabled = true, bool defaultOpen = false,
        double defaultWidth = 320, double minWidth = 240, double maxWidth = 560, bool collapsible = true,
        string? contentProviderCode = null, PermissionCode? permissionCode = null,
        CapabilityCode? capabilityCode = null, string? privacyPolicyCode = null, int displayOrder = 0,
        ContextPanelScope scope = ContextPanelScope.Workspace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(panelCode);
        if (!double.IsFinite(minWidth) || minWidth <= 0 || !double.IsFinite(maxWidth) || maxWidth < minWidth)
            throw new ArgumentOutOfRangeException(nameof(minWidth), "Context panel width bounds are invalid.");
        if (!double.IsFinite(defaultWidth)) throw new ArgumentOutOfRangeException(nameof(defaultWidth));
        PanelCode = panelCode.Trim().ToUpperInvariant(); DisplayNameKey = displayNameKey; IconKey = iconKey;
        Enabled = enabled; DefaultOpen = defaultOpen; MinWidth = minWidth; MaxWidth = maxWidth;
        DefaultWidth = Math.Clamp(defaultWidth, minWidth, maxWidth); Collapsible = collapsible;
        ContentProviderCode = contentProviderCode?.Trim().ToUpperInvariant(); PermissionCode = permissionCode;
        CapabilityCode = capabilityCode; PrivacyPolicyCode = privacyPolicyCode; DisplayOrder = displayOrder; Scope = scope;
        Sections = (sections ?? throw new ArgumentNullException(nameof(sections))).OrderBy(x => x.DisplayOrder).ToImmutableArray();
        if (Sections.GroupBy(x => x.NormalizedCode).Any(x => x.Count() > 1))
            throw new ArgumentException("Duplicate context panel section code.", nameof(sections));
    }
    public string PanelCode { get; } public string? DisplayNameKey { get; } public string? IconKey { get; }
    public bool Enabled { get; } public bool DefaultOpen { get; } public double DefaultWidth { get; }
    public double MinWidth { get; } public double MaxWidth { get; } public bool Collapsible { get; }
    public string? ContentProviderCode { get; } public PermissionCode? PermissionCode { get; }
    public CapabilityCode? CapabilityCode { get; } public string? PrivacyPolicyCode { get; }
    public int DisplayOrder { get; } public ContextPanelScope Scope { get; }
    public ImmutableArray<ContextPanelSectionDefinition> Sections { get; }
}

public sealed class ContextPanelState(ContextPanelDefinition definition)
{
    public string PanelCode { get; } = definition.PanelCode;
    public bool IsOpen { get; private set; } = definition.DefaultOpen;
    public double Width { get; private set; } = definition.DefaultWidth;
    public string? CurrentContextKey { get; internal set; }
    public ContextLoadingState LoadingState { get; internal set; } = ContextLoadingState.Empty;
    public string? SelectedSection { get; private set; } = definition.Sections.FirstOrDefault()?.NormalizedCode;
    public long Generation { get; internal set; }
    public string? DiagnosticCode { get; internal set; }
    public void Open() => IsOpen = true;
    public void Close() { if (definition.Collapsible) IsOpen = false; }
    public void Toggle() { if (IsOpen) Close(); else Open(); }
    public double Resize(double width) => Width = Math.Clamp(double.IsFinite(width) ? width : definition.DefaultWidth, definition.MinWidth, definition.MaxWidth);
    public bool SelectSection(string sectionCode)
    {
        var code = sectionCode?.Trim().ToUpperInvariant();
        if (!definition.Sections.Any(x => x.NormalizedCode == code)) return false;
        SelectedSection = code; return true;
    }
}

public sealed record ContextSelection(string? EntityKey = null, string? RowKey = null,
    string? VariableCode = null, string? DocumentKey = null);

public sealed record ContextPanelRequest(CompanyId? CompanyId, string? WorkspaceId, string? TemplateCode,
    string? NavigationTarget, ContextSelection Selection, HelpContextCode? HelpContextCode,
    CultureInfo Culture, PrivacyMode PrivacyMode, EffectiveAuthorizationContext? PermissionContext,
    long Generation, CancellationToken CancellationToken)
{
    public string SemanticKey => string.Join('|', CompanyId?.Value, WorkspaceId, Selection.EntityKey,
        Selection.RowKey, Selection.VariableCode, Selection.DocumentKey, HelpContextCode?.Value);
}

public sealed record ContextItem(string FieldCode, string DisplayNameKey, object? Value,
    ContextItemKind Kind = ContextItemKind.Field, string? IconKey = null,
    SensitiveContentDefinition? SensitiveContent = null, PermissionCode? PermissionCode = null,
    CapabilityCode? CapabilityCode = null, bool IsReadOnly = true, string? NavigationTarget = null,
    string? RegisteredCommandCode = null);
public sealed record ContextSectionResult(string SectionCode, string? Title,
    ImmutableArray<ContextItem> Items, HelpContextCode? HelpContextCode = null);
public sealed record ContextPanelResult(string ProviderCode, string ContextKey,
    ImmutableArray<ContextSectionResult> Sections, ContextLoadingState State, long Generation,
    string? DiagnosticCode = null)
{
    public static ContextPanelResult Empty(string provider, string key, long generation) =>
        new(provider, key, [], ContextLoadingState.Empty, generation);
}
public interface IContextPanelProvider
{
    string ProviderCode { get; }
    ValueTask<ContextPanelResult> GetContextAsync(ContextPanelRequest request);
}

public interface IContextPanelPreferenceStore
{
    ContextPanelPreference? Load(string panelCode);
    void Save(string panelCode, ContextPanelPreference preference);
}
public sealed record ContextPanelPreference(bool IsOpen, double Width, string? SelectedSection);
public sealed class InMemoryContextPanelPreferenceStore : IContextPanelPreferenceStore
{
    private readonly Dictionary<string, ContextPanelPreference> values = new(StringComparer.OrdinalIgnoreCase);
    public ContextPanelPreference? Load(string panelCode) => values.GetValueOrDefault(panelCode);
    public void Save(string panelCode, ContextPanelPreference preference) => values[panelCode] = preference;
}
