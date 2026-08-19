using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation;
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
    private readonly ListBox navigation = new();
    private readonly TextBlock navigationTitle = new();
    private readonly TextBlock navigationCompany = new();
    private readonly Button navigationExit = new();
    private readonly StackPanel page = new() { Spacing = 24, Margin = new Thickness(24), HorizontalAlignment = HorizontalAlignment.Left };
    private readonly SettingsNavigationState navigationState = new(StandardApplicationMenuCodes.CompanyContext);
    private bool refreshingNavigation;

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
        AvaloniaTypography.ApplyUiFont(this);
        page.MaxWidth = ResourceNumber("DuiFormReadableWidth", 760);
        navigation.SelectionChanged += NavigationChanged;
        Content = BuildLayout();
        localization.CultureChanged += (_, _) => Refresh();
        companyScope.SnapshotChanged += (_, _) => Dispatcher.UIThread.Post(Refresh);
        appearance.PreferencesChanged += (_, _) => Dispatcher.UIThread.Post(Refresh);
        KeyDown += OnKeyDown;
        Refresh();
    }

    public event EventHandler? CloseRequested;
    public string CurrentPageCode => navigationState.CurrentPageCode;

    private Control BuildLayout()
    {
        navigationTitle.FontWeight = FontWeight.SemiBold;
        navigationTitle.FontSize = ResourceNumber("DuiTypographyTitle", 16);
        navigationCompany.Bind(TextBlock.ForegroundProperty, navigationCompany.GetResourceObservable("DuiTextSecondaryBrush"));
        navigationCompany.FontSize = ResourceNumber("DuiTypographyBodySmall", 12);
        navigationExit.HorizontalContentAlignment = HorizontalAlignment.Left;
        navigationExit.MinHeight = ResourceNumber("DuiControlHeightStandard", 34);
        navigationExit.Click += (_, _) => exit.RequestExit();
        var navChrome = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"), RowSpacing = 4 };
        navChrome.Children.Add(navigationTitle);
        Grid.SetRow(navigationCompany, 1); navChrome.Children.Add(navigationCompany);
        var navScroll = new ScrollViewer { Content = navigation, Margin = new Thickness(0, 12, 0, 8) };
        Grid.SetRow(navScroll, 2); navChrome.Children.Add(navScroll);
        Grid.SetRow(navigationExit, 3); navChrome.Children.Add(navigationExit);
        var navBorder = new Border
        {
            Width = ResourceNumber("DuiSettingsNavigationWidth", 248),
            Padding = new Thickness(12, 16),
            Child = navChrome,
            BorderThickness = new Thickness(0, 0, 1, 0),
        };
        navBorder.Bind(Border.BackgroundProperty, navBorder.GetResourceObservable("DuiSurfacePanelBrush"));
        navBorder.Bind(Border.BorderBrushProperty, navBorder.GetResourceObservable("DuiBorderSubtleBrush"));

        var close = new Button { HorizontalAlignment = HorizontalAlignment.Right, Content = "×", FontSize = 18,
            Margin = new Thickness(8), MinWidth = ResourceNumber("DuiHitTargetMinimum", 32) };
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
        navigationTitle.Text = brand.ApplicationName;
        navigationCompany.Text = companies.CurrentCompany.DisplayName;
        navigationExit.Content = L("AppMenu.Exit");
        AutomationProperties.SetName(navigationExit, L("AppMenu.Exit"));
        refreshingNavigation = true;
        navigation.Items.Clear();
        foreach (var resolved in composer.Compose(authorization))
        {
            var item = resolved.Item;
            if (item.Code == StandardApplicationMenuCodes.Exit) continue;
            var row = new ListBoxItem
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                IsEnabled = resolved.PresentationState == AuthorizationPresentationState.VisibleEnabled,
                Tag = item.Code,
                Content = MenuLabel(item),
                MinHeight = ResourceNumber("DuiControlHeightStandard", 34),
                Padding = new Thickness(8, 4),
            };
            AutomationProperties.SetName(row, localization.Get(item.DisplayNameKey));
            navigation.Items.Add(row);
            if (item.Code == CurrentPageCode) navigation.SelectedItem = row;
        }
        refreshingNavigation = false;
        RenderCurrentPage();
    }

    private Control MenuLabel(ApplicationMenuItem item)
    {
        var icon = new SemanticIcon { Width = ResourceNumber("DuiIconSizeStandard", 16),
            Height = ResourceNumber("DuiIconSizeStandard", 16) };
        icon.SetIcon(icons, item.IconKey);
        icon.Bind(SemanticIcon.ForegroundProperty, icon.GetResourceObservable("DuiTextPrimaryBrush"));
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children = { icon, new TextBlock { Text = localization.Get(item.DisplayNameKey), VerticalAlignment = VerticalAlignment.Center } },
        };
    }

    private void NavigationChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (refreshingNavigation || navigation.SelectedItem is not ListBoxItem { Tag: string code }) return;
        navigationState.Navigate(code);
        RenderCurrentPage();
    }

    private void RenderCurrentPage()
    {
        page.Children.Clear();
        switch (CurrentPageCode)
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
        var current = new StackPanel { Spacing = 4 };
        current.Children.Add(Heading(snapshot.Company.DisplayName, "DuiTypographySectionTitle"));
        if (!string.IsNullOrWhiteSpace(snapshot.Company.TaxCode)) current.Children.Add(Muted(snapshot.Company.TaxCode!));
        if (snapshot.Status == CompanyScopeLoadStatus.Loading) current.Children.Add(Muted(L("State.Loading")));
        if (snapshot.Status is CompanyScopeLoadStatus.Error or CompanyScopeLoadStatus.Unavailable)
            current.Children.Add(Muted(L(snapshot.Status == CompanyScopeLoadStatus.Error ? "State.Error" : "State.Unavailable")));
        AddSection("AppMenu.Company.Active", current);

        var choices = new ListBox { MaxWidth = ResourceNumber("DuiEditorWidthMedium", 360),
            HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var company in companies.AvailableCompanies)
        {
            var selected = company.CompanyId == companies.CurrentCompany.CompanyId;
            var item = new ListBoxItem { Content = company.DisplayName,
                IsEnabled = snapshot.Status != CompanyScopeLoadStatus.Loading, Tag = company.CompanyId,
                MinHeight = ResourceNumber("DuiControlHeightStandard", 34) };
            choices.Items.Add(item);
            if (selected) choices.SelectedItem = item;
        }
        choices.SelectionChanged += async (_, _) =>
        {
            if (choices.SelectedItem is ListBoxItem { Tag: CompanyId id } && id != companies.CurrentCompany.CompanyId)
                await companyScope.SwitchCompanyAsync(id);
        };
        AddSection("AppMenu.SwitchCompany", choices);

        var profilePanel = new StackPanel { Spacing = 8 };
        if (snapshot.ProfileResult?.Profile is { } profile)
        {
            AddField(profilePanel, "AppMenu.LegalName", profile.LegalName);
            AddField(profilePanel, "AppMenu.ShortName", profile.ShortName);
            AddField(profilePanel, "AppMenu.TaxCode", profile.TaxCode);
            AddField(profilePanel, "AppMenu.Address", profile.Address);
            AddField(profilePanel, "AppMenu.Phone", profile.Phone);
            AddField(profilePanel, "AppMenu.Email", profile.Email);
            AddField(profilePanel, "AppMenu.Website", profile.Website);
            AddField(profilePanel, "AppMenu.Representative", profile.RepresentativeName);
            AddField(profilePanel, "AppMenu.Status", profile.Status);
            foreach (var field in profile.AdditionalFields) AddField(profilePanel, field.Key, field.Value, false);
        }
        else if (snapshot.Status != CompanyScopeLoadStatus.Loading) profilePanel.Children.Add(EmptyState(L("State.Unavailable")));
        AddSection("AppMenu.CompanyProfile", profilePanel);
    }

    private void RenderLanguage()
    {
        AddTitle("AppMenu.Language");
        var choices = new ListBox { SelectionMode = SelectionMode.Single,
            MaxWidth = ResourceNumber("DuiEditorWidthMedium", 360), HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var (culture, label) in new[] { ("vi-VN", "Tiếng Việt"), ("en-US", "English") })
        {
            var item = new ListBoxItem { Content = label, Tag = culture,
                MinHeight = ResourceNumber("DuiControlHeightStandard", 34) };
            choices.Items.Add(item);
            if (localization.CurrentCulture.Name == culture) choices.SelectedItem = item;
        }
        choices.SelectionChanged += (_, _) =>
        { if (choices.SelectedItem is ListBoxItem { Tag: string value }) localization.TrySetCulture(value); };
        AddSection("AppMenu.Language.Selection", choices);
    }

    private void RenderAppearance()
    {
        AddTitle("AppMenu.Appearance");
        var themes = new ListBox { SelectionMode = SelectionMode.Single,
            MaxWidth = ResourceNumber("DuiEditorWidthMedium", 360), HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var theme in Enum.GetValues<ThemeMode>())
        {
            var item = new ListBoxItem { Content = L($"AppMenu.Theme.{theme}"), Tag = theme,
                MinHeight = ResourceNumber("DuiControlHeightStandard", 34) };
            themes.Items.Add(item);
            if (themeService.Current == theme) themes.SelectedItem = item;
        }
        themes.SelectionChanged += (_, _) =>
        {
            if (themes.SelectedItem is not ListBoxItem { Tag: ThemeMode selected }) return;
            themeService.SetTheme(selected);
            appearance.Update(appearance.Current with { Theme = selected });
        };
        AddSection("AppMenu.Theme", themes);
        var presentation = new StackPanel { Spacing = 12 };
        var font = new ComboBox { ItemsSource = Enum.GetValues<FontSizePreference>(), SelectedItem = appearance.Current.FontSize,
            Width = ResourceNumber("DuiEditorWidthCompact", 220), HorizontalAlignment = HorizontalAlignment.Left };
        font.SelectionChanged += (_, _) => { if (font.SelectedItem is FontSizePreference value) appearance.Update(appearance.Current with { FontSize = value }); };
        presentation.Children.Add(Setting(L("AppMenu.FontSize"), font));
        var density = new ComboBox { ItemsSource = Enum.GetValues<GridDensityPreference>(), SelectedItem = appearance.Current.GridDensity,
            Width = ResourceNumber("DuiEditorWidthCompact", 220), HorizontalAlignment = HorizontalAlignment.Left };
        density.SelectionChanged += (_, _) => { if (density.SelectedItem is GridDensityPreference value) appearance.Update(appearance.Current with { GridDensity = value }); };
        presentation.Children.Add(Setting(L("AppMenu.GridDensity"), density, L("AppMenu.UiScaleFoundation")));
        var reset = new Button { Content = L("AppMenu.ResetLayout"), HorizontalAlignment = HorizontalAlignment.Left };
        reset.Click += async (_, _) => await layoutReset.ResetAsync();
        presentation.Children.Add(reset);
        AddSection("AppMenu.Appearance.Presentation", presentation);
    }

    private async Task RenderAccountAsync()
    {
        AddTitle("AppMenu.Account");
        page.Children.Add(Muted(L("State.Loading")));
        Func<CancellationToken, Task<AccountPresentation?>>? load = accountProvider is null
            ? null
            : accountProvider.GetAsync;
        var result = await OptionalPresentationLoader.LoadAsync(load);
        if (CurrentPageCode != StandardApplicationMenuCodes.Account) return;
        page.Children.Clear(); AddTitle("AppMenu.Account");
        if (result.Status != OptionalPresentationStatus.Ready)
        {
            page.Children.Add(EmptyState(L(result.Status == OptionalPresentationStatus.Error ? "State.Error" : "State.Unavailable")));
            return;
        }
        page.Children.Add(Heading(result.Value!.DisplayName, "DuiTypographySectionTitle"));
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
        if (CurrentPageCode != StandardApplicationMenuCodes.License) return;
        page.Children.Clear(); AddTitle("AppMenu.License");
        if (result.Status != OptionalPresentationStatus.Ready)
        {
            page.Children.Add(EmptyState(L(result.Status == OptionalPresentationStatus.Error ? "State.Error" : "State.Unavailable")));
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

    private void RenderEmpty(string title, string message) { AddTitle(title); page.Children.Add(EmptyState(L(message))); }
    private void AddTitle(string key) => page.Children.Add(Heading(L(key), "DuiTypographyPageTitle"));
    private void AddSection(string titleKey, Control content)
    {
        var section = new StackPanel { Spacing = 8 };
        section.Children.Add(Heading(L(titleKey), "DuiTypographySectionTitle"));
        section.Children.Add(content);
        page.Children.Add(section);
    }
    private static StackPanel Setting(string label, Control control, string? support = null)
    {
        var group = new StackPanel { Spacing = 4 };
        group.Children.Add(Heading(label, "DuiTypographyBodySmall"));
        group.Children.Add(control);
        if (!string.IsNullOrWhiteSpace(support)) group.Children.Add(Muted(support));
        return group;
    }
    private void AddField(string key, string? value, bool localizeKey = true)
        => AddField(page, key, value, localizeKey);
    private void AddField(Panel target, string key, string? value, bool localizeKey = true)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var field = new Grid { ColumnDefinitions = new ColumnDefinitions("180,*"), ColumnSpacing = 12 };
        field.Children.Add(Muted(localizeKey ? L(key) : key));
        var text = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap };
        text.Bind(TextBlock.ForegroundProperty, text.GetResourceObservable("DuiTextPrimaryBrush"));
        Grid.SetColumn(text, 1); field.Children.Add(text);
        target.Children.Add(field);
    }
    private string L(string key) => localization.Get(new LocalizationKey(key));
    private static TextBlock Heading(string text, string sizeToken)
    {
        var block = new TextBlock { Text = text, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
        block.Bind(TextBlock.FontSizeProperty, block.GetResourceObservable(sizeToken));
        block.Bind(TextBlock.ForegroundProperty, block.GetResourceObservable("DuiTextPrimaryBrush"));
        return block;
    }
    private static Border EmptyState(string message)
    {
        var state = new Border { Padding = new Thickness(16), CornerRadius = new CornerRadius(7),
            MaxWidth = ResourceNumber("DuiEditorWidthLong", 560), HorizontalAlignment = HorizontalAlignment.Left };
        state.Bind(Border.BackgroundProperty, state.GetResourceObservable("DuiDisabledSurfaceBrush"));
        state.Child = Muted(message);
        AutomationProperties.SetName(state, message);
        return state;
    }
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
    private static double ResourceNumber(string key, double fallback) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is double number ? number : fallback;
}
