using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using DynamicUI24.Core.Privacy;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Small reusable adapter; raw values are never assigned to tooltip or automation unless policy allows.</summary>
public sealed class SensitiveValuePresenterControl : ContentControl
{
    private readonly ISensitiveValuePresenter presenter;
    public SensitiveValuePresenterControl(ISensitiveValuePresenter? presenter = null) =>
        this.presenter = presenter ?? new SensitiveValuePresenter();

    public void Present(object? value, SensitiveContentDefinition? metadata,
        ResolvedPrivacyPresentation resolution, CultureInfo? culture = null)
    {
        var safe = presenter.Present(value, metadata, resolution, culture);
        Content = safe.IsVisible ? new TextBlock { Text = safe.DisplayValue } : new TextBlock { Text = safe.DisplayValue, Opacity = 0.75 };
        ToolTip.SetTip(this, safe.TooltipValue);
        AutomationProperties.SetName(this, safe.AccessibleValue);
    }
}
