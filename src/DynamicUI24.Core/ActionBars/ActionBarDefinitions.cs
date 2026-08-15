using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.ActionBars;

public enum ActionBarPosition { Top, Bottom }

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
        string? batchActionCode = null)
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
