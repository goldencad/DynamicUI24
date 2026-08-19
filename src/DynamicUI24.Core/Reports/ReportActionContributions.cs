using System.Collections.Immutable;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.ModernWorkspace;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Reports;

/// <summary>Adapts Report presentation metadata into the existing Action Bar and contextual contracts.</summary>
public sealed record ReportActionContributions(ActionBarDefinition Top, ActionBarDefinition Bottom,
    ImmutableArray<ContextualActionDefinition> Contextual, ActionBarDefinition Overflow);

public static class ReportActionContributionAdapter
{
    public static ReportActionContributions Create(ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var visible = report.Actions.Where(x => x.Placement != ReportActionPlacement.Hidden).ToImmutableArray();
        var top = Bar(report, ReportActionPlacement.Top, ActionBarPosition.Top);
        var bottom = Bar(report, ReportActionPlacement.Bottom, ActionBarPosition.Bottom);
        var contextual = visible.Where(x => x.Placement == ReportActionPlacement.Contextual)
            .OrderBy(x => x.Order).ThenBy(x => x.ActionCode.Value, StringComparer.Ordinal)
            .Select(x => new ContextualActionDefinition(x.ActionCode.Value, x.CommandCode,
                ContextualActionPlacement.ContextualToolbar, x.AuthorizationRequirement?.Capability,
                HasKeyboardAlternative: true)).ToImmutableArray();
        var overflowItems = visible.Where(x => x.Placement == ReportActionPlacement.Overflow)
            .OrderBy(x => x.Order).ThenBy(x => x.ActionCode.Value, StringComparer.Ordinal)
            .Select(x => new ActionMenuItemDefinition($"report-overflow-{x.ActionCode.Value}", x.ActionCode.Value,
                x.DisplayNameKey, x.IconKey, x.CommandCode, x.Order)).ToImmutableArray();
        var overflowAction = overflowItems.IsEmpty ? Array.Empty<ActionDefinition>() :
            [new ActionDefinition("report-overflow", "REPORT_OVERFLOW", new("Report.Action.More"),
                new IconKey("MORE"), ActionType.CustomRegistered, buttonVariant: ActionButtonVariant.DropdownButton,
                menuItems: overflowItems, registeredCommandCode: overflowItems[0].RegisteredCommandCode)];
        return new(top, bottom, contextual, new($"report-{report.ReportCode.Value}-overflow", "REPORT_OVERFLOW",
            ActionBarPosition.Top, overflowAction));
    }

    private static ActionBarDefinition Bar(ReportDefinition report, ReportActionPlacement placement, ActionBarPosition position) =>
        new($"report-{report.ReportCode.Value}-{placement}", $"REPORT_{placement}", position,
            report.Actions.Where(x => x.Placement == placement)
                .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Order).ThenBy(x => x.ActionCode.Value, StringComparer.Ordinal)
                .Select(ToAction));

    private static ActionDefinition ToAction(ReportActionDefinition value) => new(
        $"report-action-{value.ActionCode.Value}", value.ActionCode.Value, value.DisplayNameKey, value.IconKey,
        ActionType.CustomRegistered, value.Order, requiresSelection: value.RequiresSelection,
        registeredCommandCode: value.CommandCode,
        geometry: value.IsPrimary ? new(ActionControlSizePreset.Large) : new());
}
