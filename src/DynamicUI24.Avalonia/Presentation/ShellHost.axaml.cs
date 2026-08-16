using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Automation;
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
        SizeChanged += (_, _) => RefreshResponsiveSearch();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.K && e.KeyModifiers.HasFlag(OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control))
            {
                IsSearchOpen = true;
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape && SearchOverlay.IsVisible)
            {
                IsSearchOpen = false;
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape && ApplicationMenuOverlay.IsVisible)
            {
                IsApplicationMenuOpen = false;
                e.Handled = true;
            }
        };
    }

    public Control? WorkspaceContent
    {
        get => WorkspacePresenter.Content as Control;
        set => WorkspacePresenter.Content = value;
    }

    /// <summary>Optional left navigation surface; the shell owns layout only.</summary>
    public Control? NavigationContent
    {
        get => NavigationPresenter.Content as Control;
        set => NavigationPresenter.Content = value;
    }

    /// <summary>The Ribbon is a separate shell region; the application menu never becomes a Ribbon tab.</summary>
    public Control? RibbonContent
    {
        get => RibbonPresenter.Content as Control;
        set => RibbonPresenter.Content = value;
    }

    /// <summary>Shared shell-owned notification and guidance region.</summary>
    public Control? NotificationContent
    {
        get => NotificationPresenter.Content as Control;
        set => NotificationPresenter.Content = value;
    }

    public Control? ApplicationMenuContent
    {
        get => ApplicationMenuPresenter.Content as Control;
        set
        {
            if (ApplicationMenuPresenter.Content is ApplicationMenuView oldView) oldView.CloseRequested -= MenuCloseRequested;
            ApplicationMenuPresenter.Content = value;
            if (value is ApplicationMenuView newView) newView.CloseRequested += MenuCloseRequested;
        }
    }

    public SearchPaletteView? SearchContent
    {
        get => SearchPresenter.Content as SearchPaletteView;
        set
        {
            if (SearchPresenter.Content is SearchPaletteView oldView) oldView.CloseRequested -= SearchCloseRequested;
            SearchPresenter.Content = value;
            if (value is not null) value.CloseRequested += SearchCloseRequested;
        }
    }

    public bool IsSearchOpen
    {
        get => SearchOverlay.IsVisible;
        set
        {
            SearchOverlay.IsVisible = value;
            if (value) { IsApplicationMenuOpen = false; SearchContent?.Open(); }
        }
    }

    public bool IsApplicationMenuOpen
    {
        get => ApplicationMenuOverlay.IsVisible;
        set
        {
            ApplicationMenuOverlay.IsVisible = value;
            if (value) ApplicationMenuContent?.Focus();
        }
    }

    private void PresentationChanged(object? sender, PropertyChangedEventArgs e) => RefreshText();
    private void LocalizationChanged(object? sender, EventArgs e) => RefreshText();

    private void RefreshText()
    {
        ApplicationNameText.Text = presentation.Brand.ApplicationName;
        WorkspaceTitleText.Text = presentation.CurrentWorkspaceTitle ?? string.Empty;
        StatusText.Text = presentation.StatusMessage ?? localization.Get(presentation.State.MessageKey);
        ExitButton.Content = localization.Get(new LocalizationKey("Shell.Exit"));
        SearchButtonLabel.Text = Localized("Search.Placeholder", "Search…");
        SearchShortcutLabel.Text = OperatingSystem.IsMacOS() ? "⌘K" : "Ctrl+K";
        AutomationProperties.SetName(SearchButton, Localized("Search.AccessibleName", "Search commands and destinations"));
        AutomationProperties.SetName(ApplicationMenuButton, localization.Get(new LocalizationKey("AppMenu.Open")));
    }

    private string Localized(string key, string fallback)
    { var value = localization.Get(new LocalizationKey(key)); return value == key ? fallback : value; }

    private void RefreshResponsiveSearch()
    {
        var narrow = Bounds.Width < 720;
        SearchButtonLabel.IsVisible = !narrow;
        SearchShortcutLabel.IsVisible = !narrow;
        SearchButton.MinWidth = narrow ? 44 : 126;
    }

    private void ExitClicked(object? sender, RoutedEventArgs e) => exitService.RequestExit();
    private void ApplicationMenuClicked(object? sender, RoutedEventArgs e) => IsApplicationMenuOpen = !IsApplicationMenuOpen;
    private void MenuCloseRequested(object? sender, EventArgs e) => IsApplicationMenuOpen = false;
    private void SearchClicked(object? sender, RoutedEventArgs e) => IsSearchOpen = !IsSearchOpen;
    private void SearchCloseRequested(object? sender, EventArgs e) => IsSearchOpen = false;
    private void SearchBackdropPressed(object? sender, PointerPressedEventArgs e) => IsSearchOpen = false;
    private void SearchContentPressed(object? sender, PointerPressedEventArgs e) => e.Handled = true;

    private sealed class NoOpExitService : IApplicationExitService
    {
        public void RequestExit()
        {
        }
    }
}
