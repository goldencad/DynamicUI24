using System.Collections.Immutable;

namespace DynamicUI24.Core.Authoring;

public sealed record UiElementPreference(UiElementCode ElementCode, bool? IsVisible = null,
    double? Width = null, int? Order = null, bool? IsPinned = null, bool? IsCollapsed = null);
public sealed record ResolvedUiElement(UiElementDefinition Definition, UiAuthorizationState Authorization,
    bool IsVisible, double? Width, int Order, bool IsPinned, bool IsCollapsed, ImmutableArray<string> RepairCodes);

public static class UiPreferenceResolver
{
    public static ResolvedUiElement Resolve(UiElementDefinition definition, UiElementPreference? preference,
        UiAuthorizationState authorization, bool platformAvailable = true)
    {
        var repairs = ImmutableArray.CreateBuilder<string>();
        var visible = definition.Layout.DefaultVisible;
        var width = definition.Layout.DefaultWidth;
        var order = definition.Layout.Priority;
        var pinned = false; var collapsed = false;
        if (preference is not null)
        {
            if (preference.IsVisible is { } desired && definition.Personalization.UserCanHide) visible = desired;
            if (preference.Order is { } desiredOrder && definition.Personalization.UserCanReorder) order = Math.Max(0, desiredOrder);
            if (preference.IsPinned is { } desiredPin && definition.Personalization.UserCanPin) pinned = desiredPin;
            if (preference.IsCollapsed is { } desiredCollapse && definition.Personalization.UserCanCollapse) collapsed = desiredCollapse;
            if (preference.Width is { } desiredWidth && definition.Personalization.UserCanResize)
            {
                if (!double.IsFinite(desiredWidth) || desiredWidth <= 0) repairs.Add("UI_PREFERENCE_WIDTH_RESET");
                else width = Math.Clamp(desiredWidth, definition.Layout.MinimumWidth ?? 0, definition.Layout.MaximumWidth ?? double.MaxValue);
            }
        }
        if (authorization == UiAuthorizationState.Hidden || !platformAvailable) visible = false;
        if (!visible) pinned = false;
        return new(definition, authorization, visible, width, order, pinned, collapsed, repairs.ToImmutable());
    }

    public static ImmutableArray<UiElementPreference> Repair(IEnumerable<UiElementPreference> preferences, UiDefinition definition) =>
        (preferences ?? []).Where(x => definition.Elements.Any(e => e.Code == x.ElementCode)).ToImmutableArray();
}
