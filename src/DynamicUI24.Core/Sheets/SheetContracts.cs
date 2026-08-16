using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Sheets;

/// <summary>Stable, non-localized semantic sheet identity. Titles and tab positions are never identity.</summary>
public readonly record struct SheetCode
{
    public SheetCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }
    public string Value { get; }
    public override string ToString() => Value;
}

public enum SheetContentType { DataEntryGrid, Report, History, Document, Dashboard, Custom }
public enum SheetOverflowPolicy { Menu, Scroll }
public enum SheetTabPlacement { Top, Bottom }
public enum SheetLifecycleAction { Create, Duplicate, SaveAs, Rename, Reorder, Hide, Show, Delete }

public sealed record GridHeaderDefinition(LocalizationKey TitleKey, LocalizationKey? SubtitleKey = null,
    IconKey? IconKey = null, bool ShowRowCount = true, bool ShowSelectionCount = true,
    bool ShowFilteredCount = true, bool ShowStatus = true, HelpContextCode? HelpContextCode = null,
    SensitiveContentDefinition? TitlePrivacy = null, SensitiveContentDefinition? SubtitlePrivacy = null);

public sealed record SheetDefinition
{
    public SheetDefinition(SheetCode sheetCode, LocalizationKey titleKey, int displayOrder,
        SheetContentType contentType, string contentDefinitionCode, LocalizationKey? subtitleKey = null,
        GridDefinition? gridDefinition = null, GridHeaderDefinition? gridHeader = null, bool isHidden = false,
        bool isClosable = true, bool isReorderable = true, bool isHideable = true, bool isDuplicable = true,
        bool isSaveAsEnabled = true, PresentationRequirement? presentationRequirement = null,
        SensitiveContentDefinition? privacyMetadata = null, HelpContextCode? helpContextCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentDefinitionCode);
        if (contentType == SheetContentType.DataEntryGrid && gridDefinition is null)
            throw new ArgumentException("A DataEntry sheet requires an existing GridDefinition.", nameof(gridDefinition));
        SheetCode = sheetCode; TitleKey = titleKey; SubtitleKey = subtitleKey; DisplayOrder = displayOrder;
        ContentType = contentType; ContentDefinitionCode = contentDefinitionCode.Trim().ToUpperInvariant();
        GridDefinition = gridDefinition; GridHeader = gridHeader; IsHidden = isHidden; IsClosable = isClosable;
        IsReorderable = isReorderable; IsHideable = isHideable; IsDuplicable = isDuplicable;
        IsSaveAsEnabled = isSaveAsEnabled; PresentationRequirement = presentationRequirement;
        PrivacyMetadata = privacyMetadata; HelpContextCode = helpContextCode;
    }
    public SheetCode SheetCode { get; }
    public LocalizationKey TitleKey { get; init; }
    public LocalizationKey? SubtitleKey { get; init; }
    public int DisplayOrder { get; init; }
    public SheetContentType ContentType { get; }
    public string ContentDefinitionCode { get; }
    public GridDefinition? GridDefinition { get; }
    public GridHeaderDefinition? GridHeader { get; init; }
    public bool IsHidden { get; init; }
    public bool IsClosable { get; }
    public bool IsReorderable { get; }
    public bool IsHideable { get; }
    public bool IsDuplicable { get; }
    public bool IsSaveAsEnabled { get; }
    public PresentationRequirement? PresentationRequirement { get; }
    public SensitiveContentDefinition? PrivacyMetadata { get; }
    public HelpContextCode? HelpContextCode { get; }
}

public sealed record SheetHostCapabilities(bool AllowCreate = false, bool AllowDuplicate = false,
    bool AllowSaveAs = false, bool AllowRename = false, bool AllowReorder = false,
    bool AllowHide = false, bool AllowDelete = false);

public sealed record SheetHostDefinition
{
    public SheetHostDefinition(string hostCode, IEnumerable<SheetDefinition> sheets,
        SheetHostCapabilities? capabilities = null, SheetOverflowPolicy overflowPolicy = SheetOverflowPolicy.Menu,
        SheetTabPlacement tabPlacement = SheetTabPlacement.Top, int maximumMaterializedSheets = 2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostCode);
        if (maximumMaterializedSheets <= 0) throw new ArgumentOutOfRangeException(nameof(maximumMaterializedSheets));
        HostCode = hostCode.Trim().ToUpperInvariant();
        Sheets = (sheets ?? throw new ArgumentNullException(nameof(sheets))).OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.SheetCode.Value, StringComparer.Ordinal).ToImmutableArray();
        if (Sheets.GroupBy(x => x.SheetCode).Any(x => x.Count() > 1))
            throw new ArgumentException("Duplicate SheetCode.", nameof(sheets));
        Capabilities = capabilities ?? new(); OverflowPolicy = overflowPolicy; TabPlacement = tabPlacement;
        MaximumMaterializedSheets = maximumMaterializedSheets;
    }
    public string HostCode { get; }
    public ImmutableArray<SheetDefinition> Sheets { get; }
    public SheetHostCapabilities Capabilities { get; }
    public SheetOverflowPolicy OverflowPolicy { get; }
    public SheetTabPlacement TabPlacement { get; }
    public int MaximumMaterializedSheets { get; }
}

public sealed record DataWorkspaceDefinition(string WorkspaceCode, LocalizationKey TitleKey,
    SheetHostDefinition SheetHost, LocalizationKey? SubtitleKey = null,
    HelpContextCode? HelpContextCode = null, PresentationRequirement? PresentationRequirement = null);
