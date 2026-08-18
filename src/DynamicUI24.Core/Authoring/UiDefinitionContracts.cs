using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.Editors;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Authoring;

public readonly record struct UiDefinitionCode
{
    public UiDefinitionCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct UiDefinitionVersion
{
    public UiDefinitionVersion(long value) { if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); Value = value; }
    public long Value { get; }
    public UiDefinitionVersion Next() => new(checked(Value + 1));
    public override string ToString() => $"v{Value}";
}

public readonly record struct FeatureCode
{
    public FeatureCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct PolicyCode
{
    public PolicyCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct UiElementCode
{
    public UiElementCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
    public override string ToString() => Value;
}

public enum UiElementKind { Workspace, RibbonTab, RibbonGroup, Command, Menu, ActionBar, Form, Field, Grid, GridColumn, Report, ReportParameter, ReportColumn, Pane, Composer }
public enum UiSurface { Ribbon, Menu, ActionBar, Inline, ContextualToolbar, CommandPalette }

public sealed record UiAuthorizationBinding(FeatureCode? Feature = null, PermissionCode? Permission = null,
    CapabilityCode? Capability = null, PolicyCode? Policy = null,
    UnauthorizedBehavior DeniedBehavior = UnauthorizedBehavior.Hide);

public sealed record UserPersonalizationPolicy(bool UserCanHide = true, bool UserCanReorder = true,
    bool UserCanResize = true, bool UserCanPin = true, bool UserCanSaveView = true,
    bool UserCanCollapse = true);

public sealed record UiLayoutDefinition(double? DefaultWidth = null, double? MinimumWidth = null,
    double? MaximumWidth = null, int Priority = 0, bool DefaultVisible = true,
    bool Collapsible = true, bool UserResizable = true);

public sealed record UiElementDefinition
{
    public UiElementDefinition(UiElementCode code, UiElementKind kind, LocalizationKey titleKey,
        UiElementCode? parentCode = null, string? semanticReference = null,
        EditorDefinition? editor = null, HelpContextCode? helpContextCode = null,
        UiAuthorizationBinding? authorization = null, UiLayoutDefinition? layout = null,
        UserPersonalizationPolicy? personalization = null, IEnumerable<UiSurface>? eligibleSurfaces = null,
        bool isSensitive = false)
    {
        Code = code; Kind = kind; TitleKey = titleKey; ParentCode = parentCode;
        SemanticReference = semanticReference?.Trim(); Editor = editor; HelpContextCode = helpContextCode;
        Authorization = authorization; Layout = layout ?? new(); Personalization = personalization ?? new();
        EligibleSurfaces = (eligibleSurfaces ?? []).Distinct().ToImmutableArray(); IsSensitive = isSensitive;
    }
    public UiElementCode Code { get; }
    public UiElementKind Kind { get; }
    public LocalizationKey TitleKey { get; }
    public UiElementCode? ParentCode { get; }
    public string? SemanticReference { get; }
    public EditorDefinition? Editor { get; }
    public HelpContextCode? HelpContextCode { get; }
    public UiAuthorizationBinding? Authorization { get; }
    public UiLayoutDefinition Layout { get; }
    public UserPersonalizationPolicy Personalization { get; }
    public ImmutableArray<UiSurface> EligibleSurfaces { get; }
    public bool IsSensitive { get; }
}

public sealed record UiDefinition
{
    public UiDefinition(UiDefinitionCode code, UiDefinitionVersion version, int schemaVersion,
        DateTimeOffset publishedAt, IEnumerable<UiElementDefinition> elements, string safeChangeSummary)
    {
        if (schemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        Code = code; Version = version; SchemaVersion = schemaVersion; PublishedAt = publishedAt;
        Elements = (elements ?? throw new ArgumentNullException(nameof(elements))).ToImmutableArray();
        SafeChangeSummary = string.IsNullOrWhiteSpace(safeChangeSummary) ? "Definition published" : safeChangeSummary.Trim();
    }
    public UiDefinitionCode Code { get; }
    public UiDefinitionVersion Version { get; }
    public int SchemaVersion { get; }
    public DateTimeOffset PublishedAt { get; }
    public ImmutableArray<UiElementDefinition> Elements { get; }
    public string SafeChangeSummary { get; }
}

public sealed record UiDefinitionVersionInfo(UiDefinitionCode Code, UiDefinitionVersion Version,
    int SchemaVersion, DateTimeOffset PublishedAt, string SafeChangeSummary, bool IsActive);

public sealed record UiDefinitionPublishRequest(UiDefinitionCode Code, UiDefinitionVersion ExpectedActiveVersion,
    int SchemaVersion, DateTimeOffset PublishedAt, ImmutableArray<UiElementDefinition> Elements,
    string SafeChangeSummary, string PublishRequestId)
{
    public UiDefinitionPublishRequest Validate()
    {
        if (SchemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(SchemaVersion));
        ArgumentException.ThrowIfNullOrWhiteSpace(SafeChangeSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(PublishRequestId);
        return this;
    }
}

public sealed record UiDefinitionPublishResult(UiDefinition Definition, bool WasAlreadyCommitted);

public interface IUiDefinitionRepository
{
    ValueTask<UiDefinition?> GetActiveAsync(UiDefinitionCode code, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyList<UiDefinitionVersionInfo>> GetVersionsAsync(UiDefinitionCode code, CancellationToken cancellationToken = default);
    /// <summary>Validates the expected active version, allocates the next version, appends and activates atomically.</summary>
    ValueTask<UiDefinitionPublishResult> PublishAndActivateAsync(UiDefinitionPublishRequest request,
        CancellationToken cancellationToken = default);
    ValueTask ActivateAsync(UiDefinitionCode code, UiDefinitionVersion version, CancellationToken cancellationToken = default);
}

public interface IUiDefinitionMigrator
{
    int CurrentSchemaVersion { get; }
    UiDefinitionMigrationResult Migrate(UiDefinition source);
}

public sealed record UiDefinitionMigrationResult(UiDefinition? Definition, ImmutableArray<UiDefinitionDiagnostic> Diagnostics)
{
    public bool IsSuccess => Definition is not null && Diagnostics.All(x => x.Severity != UiDefinitionDiagnosticSeverity.Error);
}
