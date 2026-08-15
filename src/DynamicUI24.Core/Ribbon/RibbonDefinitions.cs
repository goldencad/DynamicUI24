using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Templates;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Ribbon;

public enum RibbonCommandType
{
    Navigate,
    Refresh,
    Search,
    Filter,
    Import,
    Export,
    Preview,
    ApplicationCommand,
    BatchAction,
    CustomRegistered,
}

public enum RibbonConfirmationMode
{
    None,
    Confirm,
}

/// <summary>A deliberately small, declarative rule. It never evaluates executable expressions.</summary>
public sealed record RibbonContextRule(
    string? WorkspaceId = null,
    TemplateCode? TemplateCode = null,
    CapabilityCode? CapabilityCode = null,
    bool RequiresSelection = false)
{
    public bool IsWellFormed =>
        (WorkspaceId is null || !string.IsNullOrWhiteSpace(WorkspaceId)) &&
        (WorkspaceId is not null || TemplateCode is not null || CapabilityCode is not null || RequiresSelection);
}

public sealed record RibbonCommandDefinition
{
    public RibbonCommandDefinition(
        string commandId,
        string commandCode,
        LocalizationKey displayNameKey,
        IconKey iconKey,
        RibbonCommandType commandType,
        int displayOrder = 0,
        string? targetWorkspaceId = null,
        TemplateCode? targetTemplateCode = null,
        string? registeredCommandCode = null,
        PresentationRequirement? permissionRequirement = null,
        bool requiresSelection = false,
        RibbonConfirmationMode confirmationMode = RibbonConfirmationMode.None,
        RibbonContextRule? contextRule = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandCode);
        CommandId = commandId.Trim();
        CommandCode = commandCode.Trim().ToUpperInvariant();
        DisplayNameKey = displayNameKey;
        IconKey = iconKey;
        CommandType = commandType;
        DisplayOrder = displayOrder;
        TargetWorkspaceId = targetWorkspaceId?.Trim();
        TargetTemplateCode = targetTemplateCode;
        RegisteredCommandCode = registeredCommandCode?.Trim().ToUpperInvariant();
        PermissionRequirement = permissionRequirement;
        RequiresSelection = requiresSelection;
        ConfirmationMode = confirmationMode;
        ContextRule = contextRule;
    }

    public string CommandId { get; }
    public string CommandCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public IconKey IconKey { get; }
    public RibbonCommandType CommandType { get; }
    public int DisplayOrder { get; }
    public string? TargetWorkspaceId { get; }
    public TemplateCode? TargetTemplateCode { get; }
    public string? RegisteredCommandCode { get; }
    public PresentationRequirement? PermissionRequirement { get; }
    public bool RequiresSelection { get; }
    public RibbonConfirmationMode ConfirmationMode { get; }
    public RibbonContextRule? ContextRule { get; }
}

public sealed record RibbonGroupDefinition
{
    public RibbonGroupDefinition(
        string groupId,
        string groupCode,
        LocalizationKey displayNameKey,
        IEnumerable<RibbonCommandDefinition> commands,
        int displayOrder = 0,
        IconKey? iconKey = null,
        bool isVisible = true,
        PresentationRequirement? permissionRequirement = null,
        RibbonContextRule? contextRule = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupCode);
        ArgumentNullException.ThrowIfNull(commands);
        GroupId = groupId.Trim();
        GroupCode = groupCode.Trim().ToUpperInvariant();
        DisplayNameKey = displayNameKey;
        Commands = commands.OrderBy(x => x.DisplayOrder).ThenBy(x => x.CommandCode, StringComparer.Ordinal).ToImmutableArray();
        DisplayOrder = displayOrder;
        IconKey = iconKey;
        IsVisible = isVisible;
        PermissionRequirement = permissionRequirement;
        ContextRule = contextRule;
    }

    public string GroupId { get; }
    public string GroupCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public ImmutableArray<RibbonCommandDefinition> Commands { get; }
    public int DisplayOrder { get; }
    public IconKey? IconKey { get; }
    public bool IsVisible { get; }
    public PresentationRequirement? PermissionRequirement { get; }
    public RibbonContextRule? ContextRule { get; }
}

public sealed record RibbonTabDefinition
{
    public RibbonTabDefinition(
        string tabId,
        string tabCode,
        LocalizationKey displayNameKey,
        IEnumerable<RibbonGroupDefinition> groups,
        int displayOrder = 0,
        IconKey? iconKey = null,
        bool isVisible = true,
        PresentationRequirement? permissionRequirement = null,
        RibbonContextRule? contextRule = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tabId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tabCode);
        ArgumentNullException.ThrowIfNull(groups);
        TabId = tabId.Trim();
        TabCode = tabCode.Trim().ToUpperInvariant();
        DisplayNameKey = displayNameKey;
        Groups = groups.OrderBy(x => x.DisplayOrder).ThenBy(x => x.GroupCode, StringComparer.Ordinal).ToImmutableArray();
        DisplayOrder = displayOrder;
        IconKey = iconKey;
        IsVisible = isVisible;
        PermissionRequirement = permissionRequirement;
        ContextRule = contextRule;
    }

    public string TabId { get; }
    public string TabCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public ImmutableArray<RibbonGroupDefinition> Groups { get; }
    public int DisplayOrder { get; }
    public IconKey? IconKey { get; }
    public bool IsVisible { get; }
    public PresentationRequirement? PermissionRequirement { get; }
    public RibbonContextRule? ContextRule { get; }
}

public sealed record RibbonDefinition
{
    public RibbonDefinition(string ribbonId, string code, int version, IEnumerable<RibbonTabDefinition> tabs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ribbonId);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(tabs);
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        RibbonId = ribbonId.Trim();
        Code = code.Trim().ToUpperInvariant();
        Version = version;
        Tabs = tabs.OrderBy(x => x.DisplayOrder).ThenBy(x => x.TabCode, StringComparer.Ordinal).ToImmutableArray();
    }

    public string RibbonId { get; }
    public string Code { get; }
    public int Version { get; }
    public ImmutableArray<RibbonTabDefinition> Tabs { get; }
}
