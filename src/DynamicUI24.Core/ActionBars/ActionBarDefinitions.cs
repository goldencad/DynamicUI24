using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.ActionBars;

public enum ActionBarPosition { Top, Bottom }
public enum ActionButtonVariant { Button, DropdownButton, SplitButton, IconButton, ToggleButton }
public enum ActionMenuItemKind { Command, Separator }

public enum ActionType
{
    Navigate,
    Refresh,
    Search,
    Filter,
    Add,
    Edit,
    Delete,
    Import,
    Export,
    Preview,
    Validate,
    Commit,
    ApplicationCommand,
    BatchAction,
    CustomRegistered,
}

public enum ActionConfirmationMode { None, Confirm }

public sealed record ActionMenuItemDefinition
{
    public ActionMenuItemDefinition(string itemId, string itemCode, LocalizationKey displayNameKey,
        IconKey? iconKey = null, string? registeredCommandCode = null, int displayOrder = 0,
        PresentationRequirement? permissionRequirement = null, bool isVisible = true,
        string? groupCode = null, string? shortcutDisplay = null,
        IEnumerable<ActionMenuItemDefinition>? children = null, ActionMenuItemKind kind = ActionMenuItemKind.Command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemCode);
        var materialized = (children ?? []).OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ItemCode, StringComparer.Ordinal).ToImmutableArray();
        if (materialized.Any(x => x.Children.Length > 0))
            throw new ArgumentException("Action menus support at most two hierarchy levels.", nameof(children));
        if (materialized.GroupBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new ArgumentException("Menu item identifiers must be unique among siblings.", nameof(children));
        ItemId = itemId.Trim(); ItemCode = itemCode.Trim().ToUpperInvariant(); DisplayNameKey = displayNameKey;
        IconKey = iconKey; RegisteredCommandCode = registeredCommandCode?.Trim().ToUpperInvariant();
        DisplayOrder = displayOrder; PermissionRequirement = permissionRequirement; IsVisible = isVisible;
        GroupCode = groupCode?.Trim().ToUpperInvariant(); ShortcutDisplay = shortcutDisplay?.Trim();
        Children = materialized; Kind = kind;
    }

    public string ItemId { get; }
    public string ItemCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public IconKey? IconKey { get; }
    public string? RegisteredCommandCode { get; }
    public int DisplayOrder { get; }
    public PresentationRequirement? PermissionRequirement { get; }
    public bool IsVisible { get; }
    public string? GroupCode { get; }
    public string? ShortcutDisplay { get; }
    public ImmutableArray<ActionMenuItemDefinition> Children { get; }
    public ActionMenuItemKind Kind { get; }
}

/// <summary>Immutable declarative action metadata. It never contains executable code.</summary>
public sealed record ActionDefinition
{
    public ActionDefinition(
        string actionId,
        string actionCode,
        LocalizationKey displayNameKey,
        IconKey iconKey,
        ActionType commandType,
        int displayOrder = 0,
        PresentationRequirement? permissionRequirement = null,
        bool requiresSelection = false,
        int? minSelection = null,
        int? maxSelection = null,
        bool isVisible = true,
        ActionConfirmationMode confirmationMode = ActionConfirmationMode.None,
        string? targetWorkspaceId = null,
        string? registeredCommandCode = null,
        string? batchActionCode = null,
        ActionButtonVariant buttonVariant = ActionButtonVariant.Button,
        IEnumerable<ActionMenuItemDefinition>? menuItems = null,
        bool isChecked = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionCode);
        if (!IsTechnicalCode(actionCode)) throw new ArgumentException("Action code contains unsupported characters.", nameof(actionCode));
        if (minSelection < 0) throw new ArgumentOutOfRangeException(nameof(minSelection));
        if (maxSelection < 0) throw new ArgumentOutOfRangeException(nameof(maxSelection));
        if (minSelection > maxSelection) throw new ArgumentException("Minimum selection cannot exceed maximum selection.");

        ActionId = actionId.Trim();
        ActionCode = actionCode.Trim().ToUpperInvariant();
        DisplayNameKey = displayNameKey;
        IconKey = iconKey;
        CommandType = commandType;
        DisplayOrder = displayOrder;
        PermissionRequirement = permissionRequirement;
        RequiresSelection = requiresSelection;
        MinSelection = minSelection;
        MaxSelection = maxSelection;
        IsVisible = isVisible;
        ConfirmationMode = confirmationMode;
        TargetWorkspaceId = targetWorkspaceId?.Trim();
        RegisteredCommandCode = registeredCommandCode?.Trim().ToUpperInvariant();
        BatchActionCode = batchActionCode?.Trim().ToUpperInvariant();
        ButtonVariant = buttonVariant;
        MenuItems = (menuItems ?? []).OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ItemCode, StringComparer.Ordinal).ToImmutableArray();
        IsChecked = isChecked;
        if (buttonVariant is ActionButtonVariant.DropdownButton or ActionButtonVariant.SplitButton && MenuItems.Length == 0)
            throw new ArgumentException("Dropdown and Split actions require menu metadata.", nameof(menuItems));
        if (buttonVariant == ActionButtonVariant.SplitButton && string.IsNullOrWhiteSpace(RegisteredCommandCode))
            throw new ArgumentException("A Split action requires a default registered command.", nameof(registeredCommandCode));
    }

    public string ActionId { get; }
    public string ActionCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public IconKey IconKey { get; }
    public ActionType CommandType { get; }
    public int DisplayOrder { get; }
    public PresentationRequirement? PermissionRequirement { get; }
    public bool RequiresSelection { get; }
    public int? MinSelection { get; }
    public int? MaxSelection { get; }
    public bool IsVisible { get; }
    public ActionConfirmationMode ConfirmationMode { get; }
    public string? TargetWorkspaceId { get; }
    public string? RegisteredCommandCode { get; }
    public string? BatchActionCode { get; }
    public ActionButtonVariant ButtonVariant { get; }
    public ImmutableArray<ActionMenuItemDefinition> MenuItems { get; }
    public bool IsChecked { get; }

    private static bool IsTechnicalCode(string value) => value.Trim().All(character =>
        char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.');
}

public sealed record ActionBarDefinition
{
    public ActionBarDefinition(string actionBarId, string code, ActionBarPosition position,
        IEnumerable<ActionDefinition> actions, int displayOrder = 0, bool isVisible = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionBarId);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(actions);
        var materialized = actions.ToArray();
        if (materialized.GroupBy(x => x.ActionId, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new ArgumentException("Action identifiers must be unique within an Action Bar.", nameof(actions));

        ActionBarId = actionBarId.Trim();
        Code = code.Trim().ToUpperInvariant();
        Position = position;
        DisplayOrder = displayOrder;
        IsVisible = isVisible;
        Actions = materialized.OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ActionCode, StringComparer.Ordinal).ToImmutableArray();
    }

    public string ActionBarId { get; }
    public string Code { get; }
    public ActionBarPosition Position { get; }
    public int DisplayOrder { get; }
    public bool IsVisible { get; }
    public ImmutableArray<ActionDefinition> Actions { get; }
}

public sealed class WorkspaceActionBarDefinitions
{
    private readonly ImmutableDictionary<string, ImmutableArray<ActionBarDefinition>> definitions;

    public WorkspaceActionBarDefinitions(IEnumerable<KeyValuePair<string, IEnumerable<ActionBarDefinition>>> workspaceDefinitions)
    {
        ArgumentNullException.ThrowIfNull(workspaceDefinitions);
        var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<ActionBarDefinition>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in workspaceDefinitions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.Key);
            var bars = entry.Value?.ToArray() ?? throw new ArgumentNullException(nameof(workspaceDefinitions));
            if (bars.GroupBy(x => x.ActionBarId, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
                throw new ArgumentException($"Action Bar identifiers for workspace '{entry.Key}' must be unique.");
            if (!builder.TryAdd(entry.Key.Trim(), bars.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code, StringComparer.Ordinal).ToImmutableArray()))
                throw new ArgumentException($"Workspace '{entry.Key}' is duplicated.");
        }
        definitions = builder.ToImmutable();
    }

    public ImmutableArray<ActionBarDefinition> ForWorkspace(string workspaceId) =>
        definitions.TryGetValue(workspaceId, out var bars) ? bars : [];
}
