using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace DynamicUI24.Avalonia.Presentation.Editors;

/// <summary>
/// Applies the one shared DynamicUI24 visual ControlTheme while leaving CheckBox state,
/// input, focus, automation and accessibility semantics owned by Avalonia.
/// </summary>
public static class DynamicCheckBoxPresentation
{
    public const string ThemeResourceKey = "DuiCheckBoxTheme";
    public const string PresentationClass = "dui-check-box";

    public static void Apply(CheckBox checkBox)
    {
        ArgumentNullException.ThrowIfNull(checkBox);
        if (!checkBox.Classes.Contains(PresentationClass)) checkBox.Classes.Add(PresentationClass);
        checkBox.Bind(TemplatedControl.ThemeProperty, checkBox.GetResourceObservable(ThemeResourceKey));
    }
}
