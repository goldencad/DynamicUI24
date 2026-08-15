namespace DynamicUI24.Shared.Presentation;

public enum TreeRowVisualState { Normal, Hover, Selected, SelectedHover, Disabled, KeyboardFocus }

public static class TreeRowVisualStateResolver
{
    public static TreeRowVisualState Resolve(bool isSelected, bool isPointerOver, bool isEnabled,
        bool hasKeyboardFocus)
    {
        if (!isEnabled) return TreeRowVisualState.Disabled;
        if (hasKeyboardFocus) return TreeRowVisualState.KeyboardFocus;
        if (isSelected && isPointerOver) return TreeRowVisualState.SelectedHover;
        if (isSelected) return TreeRowVisualState.Selected;
        return isPointerOver ? TreeRowVisualState.Hover : TreeRowVisualState.Normal;
    }
}
