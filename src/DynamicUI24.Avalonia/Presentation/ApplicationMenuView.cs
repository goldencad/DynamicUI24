using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicUI24.Core.ApplicationMenu;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>A reusable, localized application-level menu. It never replaces the workspace.</summary>
public sealed class ApplicationMenuView : UserControl
{
    private readonly ApplicationBrand brand;
    private readonly ApplicationMenuComposer composer;
    private readonly ILocalizationService localization;
    private readonly IIconRegistry icons;
    private readonly IThemeService themeService;
    private readonly IAppearancePreferenceService appearance;
    private readonly ILayoutResetService layoutReset;
    private readonly IApplicationExitService exit;
    private readonly ICompanyContextProvider companies;
    private readonly CompanyScopeCoordinator companyScope;
    private readonly IAccountPresentationProvider? accountProvider;
    private readonly ILicensePresentationProvider? licenseProvider;
    private readonly StackPanel navigation = new() { Spacing = 4 };
    private readonly StackPanel page = new() { Spacing = 14, Margin = new Thickness(24) };
    private string currentPage = StandardApplicationMenuCodes.CompanyContext;

    public ApplicationMenuView(
        ApplicationBrand brand,
        ApplicationMenuComposer composer,
        ILocalizationService localization,
        IIconRegistry icons,
        IThemeService themeService,
        IAppearancePreferenceService appearance,
        ILayoutResetService layoutReset,
        IApplicationExitService exit,
        ICompanyContextProvider companies,
        CompanyScopeCoordinator companyScope,
        IAccountPresentationProvider? accountProvider = null,
        ILicensePresentationProvider? licenseProvider = null)
    {
        this.brand = brand ?? throw new ArgumentNullException(nameof(brand));
        this.composer = composer ?? throw new ArgumentNullException(nameof(composer));
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.icons = icons ?? throw new ArgumentNullException(nameof(icons));
        this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        this.appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        this.layoutReset = layoutReset ?? throw new ArgumentNullException(nameof(layoutReset));
        this.exit = exit ?? throw new ArgumentNullException(nameof(exit));
        this.companies = companies ?? throw new ArgumentNullException(nameof(companies));
        this.companyScope = companyScope ?? throw new ArgumentNullException(nameof(companyScope));
        this.accountProvider = accountProvider;
        this.licenseProvider = licenseProvider;

        Focusable = true;
        Content = BuildLayout();
        localization.CultureChanged += (_, _) => Refresh();
        companyScope.SnapshotChanged += (_, _) => Dispatcher.UIThread.Post(Refresh);
        appearance.PreferencesChanged += (_, _) => Dispatcher.UIThread.Post(Refresh);
        KeyDown += OnKeyDown;
        Refresh();
    }

    public event EventHandler? CloseRequested;
    public string CurrentPageCode => currentPage;

    private Control BuildLayout()
    {
        var navBorder = new Border
        {
            Width = 290,
            Padding = new Thickness(16),
            Child = new ScrollViewer { Content = navigation },
            BorderThickness = new Thickness(0, 0, 1, 0),
        };
        navBorder.Bind(Border.BackgroundProperty, navBorder.GetResourceObservable("DuiSurfaceRaisedBrush"));
        navBorder.Bind(Border.BorderBrushProperty, navBorder.GetResourceObservable("DuiBorderBrush"));

        var close = new Button { HorizontalAlignment = HorizontalAlignment.Right, Content = "×", FontSize = 22 };
        close.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        var content = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        content.Children.Add(close);
        var scroll = new ScrollViewer { Content = page, HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        Grid.SetRow(scroll, 1);
        content.Children.Add(scroll);
        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        root.Children.Add(navBorder);
        Grid.SetColumn(content, 1);
        root.Children.Add(content);
        return root;
    }

    private void Refresh()
    {
        var authorization = companyScope.Snapshot.AuthorizationContext;
        navigation.Children.Clear();
        navigation.Children.Add(Heading(brand.ApplicationName, 18));
        navigation.Children.Add(Muted(companies.CurrentCompany.DisplayName));
        navigation.Children.Add(new Border { Height = 8 });
        foreach (var resolved in composer.Compose(authorization))
        {
            var item = resolved.Item;
            var button = new Button
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                IsEnabled = resolved.PresentationState == AuthorizationPresentationState.VisibleEnabled,
                Tag = item.Code,
                Content = MenuLabel(item),
            };
            if (item.Code == currentPage)
                button.Bind(Button.BackgroundProperty, button.GetResourceObservable("DuiSelectionBrush"));
            button.Click += MenuClicked;
            navigation.Children.Add(button);
        }
        RenderCurrentPage();
    }

    private Control MenuLabel(ApplicationMenuItem item)
    {
        var icon = new SemanticIcon { Width = 18, Height = 18 };
        icon.SetIcon(icons, item.IconKey);
        icon.Bind(SemanticIcon.ForegroundProperty, icon.GetResourceObservable("DuiTextBrush"));
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children = { icon, new TextBlock { Text = localization.Get(item.DisplayNameKey), VerticalAlignment = VerticalAlignment.Center } },
        };
    }

    private void MenuClicked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string code }) return;
        if (code == StandardApplicationMenuCodes.Exit)
        {
            exit.RequestExit();
            return;
        }
        currentPage = code;
        Refresh();
    }

    private void RenderCurrentPage()
    {
        page.Children.Clear();
        switch (currentPage)
        {
            case StandardApplicationMenuCodes.CompanyContext: RenderCompany(); break;
            case StandardApplicationMenuCodes.Language: RenderLanguage(); break;
            case StandardApplicationMenuCodes.Appearance: RenderAppearance(); break;
            case StandardApplicationMenuCodes.GeneralSettings: RenderEmpty("AppMenu.GeneralSettings", "AppMenu.NoSettings"); break;
            case StandardApplicationMenuCodes.Account: _ = RenderAccountAsync(); break;
            case StandardApplicationMenuCodes.License: _ = RenderLicenseAsync(); break;
            case StandardApplicationMenuCodes.About: RenderAbout(); break;
            default: RenderEmpty("AppMenu.Contributed", "AppMenu.ContributedDescription"); break;
        }
    }

    private void RenderCompany()
    {
        AddTitle("AppMenu.Company");
        var snapshot = companyScope.Snapshot;
        page.Children.Add(Heading(snapshot.Company.DisplayName, 20));
        if (!string.IsNullOrWhiteSpace(snapshot.Company.TaxCode)) page.Children.Add(Muted(snapshot.Company.TaxCode!));
        if (snapshot.Status == CompanyScopeLoadStatus.Loading) page.Children.Add(Muted(L("State.Loading")));
        if (snapshot.Status is CompanyScopeLoadStatus.Error or CompanyScopeLoadStatus.Unavailable)
            page.Children.Add(Muted(L(snapshot.Status == CompanyScopeLoadStatus.Error ? "State.Error" : "State.Unavailable")));

        page.Children.Add(Heading(L("AppMenu.SwitchCompany"), 16));
        foreach (var company in companies.AvailableCompanies)
        {
            var selected = company.CompanyId == companies.CurrentCompany.CompanyId;
            var button = new Button { Content = $"{(selected ? "✓ " : string.Empty)}{company.DisplayName}", IsEnabled = !selected && snapshot.Status != CompanyScopeLoadStatus.Loading, HorizontalAlignment = HorizontalAlignment.Stretch, Tag = company.CompanyId };
            button.Click += async (sender, _) =>
            {
                if (sender is Button { Tag: CompanyId id }) await companyScope.SwitchCompanyAsync(id);
            };
            page.Children.Add(button);
        }

        page.Children.Add(Heading(L("AppMenu.CompanyProfile"), 16));
        if (snapshot.ProfileResult?.Profile is { } profile)
        {
            AddField("AppMenu.LegalName", profile.LegalName);
            AddField("AppMenu.ShortName", profile.ShortName);
            AddField("AppMenu.TaxCode", profile.TaxCode);
            AddField("AppMenu.Address", profile.Address);
            AddField("AppMenu.Phone", profile.Phone);
            AddField("AppMenu.Email", profile.Email);
            AddField("AppMenu.Website", profile.Website);
            AddField("AppMenu.Representative", profile.RepresentativeName);
            AddField("AppMenu.Status", profile.Status);
            foreach (var field in profile.AdditionalFields) AddField(field.Key, field.Value, false);
        }
        else if (snapshot.Status != CompanyScopeLoadStatus.Loading) page.Children.Add(Muted(L("State.Unavailable")));
    }

    private void RenderLanguage()
    {
        AddTitle("AppMenu.Language");
        foreach (var (culture, label) in new[] { ("vi-VN", "Tiếng Việt"), ("en-US", "English") })
        {
            var button = new Button { Content = $"{(localization.CurrentCulture.Name == culture ? "✓ " : string.Empty)}{label}", Tag = culture, HorizontalAlignment = HorizontalAlignment.Stretch };
            button.Click += (sender, _) => { if (sender is Button { Tag: string value }) localization.TrySetCulture(value); };
            page.Children.Add(button);
        }
    }

    private void RenderAppearance()
    {
        AddTitle("AppMenu.Appearance");
        page.Children.Add(Heading(L("AppMenu.Theme"), 16));
        foreach (var theme in Enum.GetValues<ThemeMode>())
        {
            var button = new Button { Content = $"{(themeService.Current == theme ? "✓ " : string.Empty)}{L($"AppMenu.Theme.{theme}")}", Tag = theme };
            button.Click += (sender, _) =>
            {
                if (sender is Button { Tag: ThemeMode selected })
                {
                    themeService.SetTheme(selected);
                    appearance.Update(appearance.Current with { Theme = selected });
                }
            };
            page.Children.Add(button);
        }
        page.Children.Add(Heading(L("AppMenu.FontSize"), 16));
        var font = new ComboBox { ItemsSource = Enum.GetValues<FontSizePreference>(), SelectedItem = appearance.Current.FontSize };
        font.SelectionChanged += (_, _) => { if (font.SelectedItem is FontSizePreference value) appearance.Update(appearance.Current with { FontSize = value }); };
        page.Children.Add(font);
        page.Children.Add(Heading(L("AppMenu.GridDensity"), 16));
        var density = new ComboBox { ItemsSource = Enum.GetValues<GridDensityPreference>(), SelectedItem = appearance.Current.GridDensity };
        density.SelectionChanged += (_, _) => { if (density.SelectedItem is GridDensityPreference value) appearance.Update(appearance.Current with { GridDensity = value }); };
        page.Children.Add(density);
        page.Children.Add(Muted(L("AppMenu.UiScaleFoundation")));
        var reset = new Button { Content = L("AppMenu.ResetLayout") };
        reset.Click += async (_, _) => await layoutReset.ResetAsync();
        page.Children.Add(reset);
    }

    private async Task RenderAccountAsync()
    {
        AddTitle("AppMenu.Account");
        page.Children.Add(Muted(L("State.Loading")));
        Func<CancellationToken, Task<AccountPresentation?>>? load = accountProvider is null
            ? null
            : accountProvider.GetAsync;
        var result = await OptionalPresentationLoader.LoadAsync(load);
        if (currentPage != StandardApplicationMenuCodes.Account) return;
        page.Children.Clear(); AddTitle("AppMenu.Account");
        if (result.Status != OptionalPresentationStatus.Ready)
        {
            page.Children.Add(Muted(L(result.Status == OptionalPresentationStatus.Error ? "State.Error" : "State.Unavailable")));
            return;
        }
        page.Children.Add(Heading(result.Value!.DisplayName, 18));
        if (result.Value.Detail is { } detail) page.Children.Add(Muted(detail));
    }

    private async Task RenderLicenseAsync()
    {
        AddTitle("AppMenu.License");
        page.Children.Add(Muted(L("State.Loading")));
        Func<CancellationToken, Task<LicensePresentation?>>? load = licenseProvider is null
            ? null
            : licenseProvider.GetAsync;
        var result = await OptionalPresentationLoader.LoadAsync(load);
        if (currentPage != StandardApplicationMenuCodes.License) return;
        page.Children.Clear(); AddTitle("AppMenu.License");
        if (result.Status != OptionalPresentationStatus.Ready)
        {
            page.Children.Add(Muted(L(result.Status == OptionalPresentationStatus.Error ? "State.Error" : "State.Unavailable")));
            return;
        }
        AddField("AppMenu.Edition", result.Value!.Edition); AddField("AppMenu.LicenseState", result.Value.State);
        AddField("AppMenu.Expiration", result.Value.Expiration?.ToString("yyyy-MM-dd")); AddField("AppMenu.Entitlements", result.Value.EntitlementSummary);
    }

    private void RenderAbout()
    {
        AddTitle("AppMenu.About");
        var appAssembly = global::Avalonia.Application.Current?.GetType().Assembly;
        AddField("AppMenu.ApplicationName", brand.ApplicationName);
        AddField("AppMenu.ApplicationVersion", appAssembly?.GetName().Version?.ToString());
        AddField("AppMenu.FrameworkVersion", typeof(ApplicationMenuView).Assembly.GetName().Version?.ToString());
        AddField("AppMenu.Runtime", RuntimeInformation.FrameworkDescription);
        AddField("AppMenu.Platform", RuntimeInformation.OSDescription);
    }

    private void RenderEmpty(string title, string message) { AddTitle(title); page.Children.Add(Muted(L(message))); }
    private void AddTitle(string key) => page.Children.Add(Heading(L(key), 24));
    private void AddField(string key, string? value, bool localizeKey = true)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        page.Children.Add(new TextBlock { Text = $"{(localizeKey ? L(key) : key)}: {value}", TextWrapping = TextWrapping.Wrap });
    }
    private string L(string key) => localization.Get(new LocalizationKey(key));
    private static TextBlock Heading(string text, double size) => new() { Text = text, FontSize = size, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    private static TextBlock Muted(string text)
    {
        var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        block.Bind(TextBlock.ForegroundProperty, block.GetResourceObservable("DuiTextMutedBrush"));
        return block;
    }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { CloseRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; }
    }
}
