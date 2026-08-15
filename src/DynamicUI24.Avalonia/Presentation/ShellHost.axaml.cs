using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Reusable application identity, workspace, status, and graceful-exit host.</summary>
public sealed partial class ShellHost : UserControl
{
    private readonly ShellPresentation presentation;
    private readonly ILocalizationService localization;
    private readonly IApplicationExitService exitService;

    public ShellHost()
        : this(
            new ShellPresentation(ApplicationBrand.Default),
            new DictionaryLocalizationService(),
            new SemanticIconRegistry(),
            new NoOpExitService())
    {
    }

    public ShellHost(
        ShellPresentation presentation,
        ILocalizationService localization,
        IIconRegistry icons,
        IApplicationExitService exitService)
    {
        this.presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.exitService = exitService ?? throw new ArgumentNullException(nameof(exitService));
        ArgumentNullException.ThrowIfNull(icons);

        InitializeComponent();
        LogoIcon.Data = Geometry.Parse(icons.Resolve(presentation.Brand.ApplicationLogoKey).SvgPathData);
        presentation.PropertyChanged += PresentationChanged;
        localization.CultureChanged += LocalizationChanged;
        RefreshText();
    }

    public Control? WorkspaceContent
    {
        get => WorkspacePresenter.Content as Control;
        set => WorkspacePresenter.Content = value;
    }

    private void PresentationChanged(object? sender, PropertyChangedEventArgs e) => RefreshText();
    private void LocalizationChanged(object? sender, EventArgs e) => RefreshText();

    private void RefreshText()
    {
        ApplicationNameText.Text = presentation.Brand.ApplicationName;
        WorkspaceTitleText.Text = presentation.CurrentWorkspaceTitle ?? string.Empty;
        StatusText.Text = presentation.StatusMessage ?? localization.Get(presentation.State.MessageKey);
        ExitButton.Content = localization.Get(new LocalizationKey("Shell.Exit"));
    }

    private void ExitClicked(object? sender, RoutedEventArgs e) => exitService.RequestExit();

    private sealed class NoOpExitService : IApplicationExitService
    {
        public void RequestExit()
        {
        }
    }
}
