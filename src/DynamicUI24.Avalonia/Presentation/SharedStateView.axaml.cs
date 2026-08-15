using Avalonia.Controls;
using Avalonia.Media;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

public sealed partial class SharedStateView : UserControl
{
    private readonly ILocalizationService localization;
    private readonly IIconRegistry icons;
    private PresentationState state = PresentationState.Ready;

    public SharedStateView()
        : this(new DictionaryLocalizationService(), new SemanticIconRegistry())
    {
    }

    public SharedStateView(ILocalizationService localization, IIconRegistry icons)
    {
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.icons = icons ?? throw new ArgumentNullException(nameof(icons));
        InitializeComponent();
        localization.CultureChanged += (_, _) => Refresh();
        Refresh();
    }

    public PresentationState State
    {
        get => state;
        set
        {
            state = value ?? throw new ArgumentNullException(nameof(value));
            Refresh();
        }
    }

    private void Refresh()
    {
        MessageText.Text = state.Error?.FriendlyMessage ?? localization.Get(state.MessageKey);
        LoadingBar.IsVisible = state.Kind == PresentationStateKind.Loading;
        DiagnosticText.IsVisible = state.Error is not null;
        DiagnosticText.Text = state.Error is null ? string.Empty : $"Code: {state.Error.DiagnosticCode}";
        DetailsExpander.IsVisible = state.Error?.HasDetails == true;
        DetailsText.Text = state.Error?.Details ?? string.Empty;
        RetryButton.IsVisible = state.Error?.CanRetry == true;

        var key = state.Kind switch
        {
            PresentationStateKind.Error => StandardIconKeys.Error,
            PresentationStateKind.Unavailable => StandardIconKeys.Warning,
            PresentationStateKind.Empty => StandardIconKeys.Search,
            PresentationStateKind.Loading => StandardIconKeys.Refresh,
            PresentationStateKind.ReadOnly => StandardIconKeys.Info,
            PresentationStateKind.PermissionDenied => StandardIconKeys.Warning,
            _ => StandardIconKeys.Success,
        };
        StateIcon.Data = Geometry.Parse(icons.Resolve(key).SvgPathData);
    }
}
