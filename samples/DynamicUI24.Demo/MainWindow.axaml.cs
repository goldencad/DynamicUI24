using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

public sealed partial class MainWindow : Window
{
    private static readonly IconKey DemoLogoKey = new("DEMO_LOGO");
    private readonly IReadOnlyList<WorkspaceDefinition> workspaces;
    private readonly ShellPresentation shellPresentation;
    private readonly DictionaryLocalizationService localization;
    private readonly AvaloniaThemeService themeService;
    private readonly SemanticIconRegistry iconRegistry;
    private readonly DynamicUI24.Avalonia.DynamicWorkspaceHost workspaceHost;
    private readonly SharedStateView stateView;
    private readonly ICompanyContextProvider companyContext;
    private readonly CompanyScopeCoordinator companyScope;
    private readonly ComboBox workspaceSelector = new();
    private readonly ComboBox themeSelector = new();
    private readonly ComboBox languageSelector = new();
    private readonly ComboBox stateSelector = new();
    private readonly ComboBox companySelector = new();
    private readonly ComboBox unauthorizedBehaviorSelector = new();
    private readonly TextBlock workspaceLabel = new();
    private readonly TextBlock themeLabel = new();
    private readonly TextBlock languageLabel = new();
    private readonly TextBlock stateLabel = new();
    private readonly TextBlock iconLabel = new();
    private readonly TextBlock companyLabel = new();
    private readonly TextBlock currentCompanyLabel = new();
    private readonly TextBlock currentCompanyValue = new();
    private readonly TextBlock profileLabel = new();
    private readonly TextBlock profileValue = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock permissionLabel = new();
    private readonly TextBlock permissionValue = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock capabilityLabel = new();
    private readonly TextBlock capabilityValue = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock requirementLabel = new();
    private readonly TextBlock behaviorLabel = new();
    private readonly TextBlock resolutionLabel = new();
    private readonly TextBlock resolutionValue = new();
    private readonly TextBlock companyStateValue = new();
    private DispatcherTimer? smokeTimer;
    private int smokeStep;
    private bool smokeAdvancing;

    public MainWindow()
        : this(DemoComposition.Create())
    {
    }

    private MainWindow(DemoComposition composition)
    {
        InitializeComponent();
        workspaces = composition.Workspaces;
        companyContext = composition.CompanyContext;
        companyScope = composition.CompanyScope;
        localization = new DictionaryLocalizationService();
        themeService = new AvaloniaThemeService(Application.Current!);
        iconRegistry = CreateDemoIconRegistry();
        shellPresentation = new ShellPresentation(
            new ApplicationBrand("Framework Demo", DemoLogoKey, "#7C3AED"));
        workspaceHost = new DynamicUI24.Avalonia.DynamicWorkspaceHost(composition.Registry, localization);
        stateView = new SharedStateView(localization, iconRegistry);

        var lifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
        var shell = new ShellHost(
            shellPresentation,
            localization,
            iconRegistry,
            new AvaloniaApplicationExitService(lifetime));
        shell.WorkspaceContent = BuildDemoSurface();
        ShellContainer.Content = shell;

        ConfigureSelectors();
        ConfigureCompanyProof();
        localization.CultureChanged += (_, _) => RefreshLocalizedLabels();
        RefreshLocalizedLabels();
        workspaceSelector.SelectedIndex = 0;
        stateSelector.SelectedIndex = 0;

        companyScope.SnapshotChanged += CompanyScopeSnapshotChanged;
        Opened += async (_, _) => await companyScope.InitializeAsync();
        Closed += (_, _) => companyScope.Dispose();

        if (Program.IsSmokeRun)
        {
            Opened += StartSmokeRun;
        }
    }

    private Control BuildDemoSurface()
    {
        var selectors = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,*,*,*"),
            ColumnSpacing = 12,
            Children =
            {
                Field(workspaceLabel, workspaceSelector, 0),
                Field(themeLabel, themeSelector, 1),
                Field(languageLabel, languageSelector, 2),
                Field(stateLabel, stateSelector, 3),
            },
        };

        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
            RowSpacing = 14,
            Children =
            {
                selectors,
                Framed(workspaceHost, 1),
                Framed(BuildCompanyProofSurface(), 2),
                Framed(stateView, 3),
                BuildIconSamples(4),
            },
        };
        return new ScrollViewer { Content = content };
    }

    private static Control Field(TextBlock label, ComboBox selector, int column)
    {
        selector.MinWidth = 120;
        var panel = new StackPanel { Spacing = 5, Children = { label, selector } };
        Grid.SetColumn(panel, column);
        return panel;
    }

    private static Border Framed(Control child, int row)
    {
        var border = new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Child = child,
        };
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable("DuiSurfaceRaisedBrush"));
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("DuiBorderBrush"));
        Grid.SetRow(border, row);
        return border;
    }

    private Control BuildIconSamples(int rowIndex)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
        foreach (var key in new[]
                 {
                     StandardIconKeys.Search,
                     StandardIconKeys.Filter,
                     StandardIconKeys.Add,
                     StandardIconKeys.Formula,
                     new IconKey("DEMO_SPARK"),
                     new IconKey("UNKNOWN_SAFE_FALLBACK"),
                 })
        {
            var icon = new SemanticIcon { Width = 22, Height = 22 };
            icon.SetIcon(iconRegistry, key);
            icon.Bind(SemanticIcon.ForegroundProperty, icon.GetResourceObservable("DuiAccentBrush"));
            row.Children.Add(icon);
        }

        var panel = new StackPanel { Spacing = 7, Children = { iconLabel, row } };
        Grid.SetRow(panel, rowIndex);
        return panel;
    }

    private Control BuildCompanyProofSurface()
    {
        var selectorField = Field(companyLabel, companySelector, 0);
        var behaviorField = Field(behaviorLabel, unauthorizedBehaviorSelector, 1);
        var selectors = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 12,
            Children = { selectorField, behaviorField },
        };

        var identity = new StackPanel
        {
            Spacing = 4,
            Children = { currentCompanyLabel, currentCompanyValue, companyStateValue },
        };
        var profile = new StackPanel { Spacing = 4, Children = { profileLabel, profileValue } };
        var access = new StackPanel
        {
            Spacing = 4,
            Children = { permissionLabel, permissionValue, capabilityLabel, capabilityValue },
        };
        var requirement = new StackPanel
        {
            Spacing = 4,
            Children = { requirementLabel, resolutionLabel, resolutionValue },
        };
        foreach (var text in new[] { companyStateValue, permissionValue, capabilityValue, resolutionValue })
        {
            text.Bind(TextBlock.ForegroundProperty, text.GetResourceObservable("DuiTextMutedBrush"));
        }

        return new StackPanel
        {
            Spacing = 12,
            Children = { selectors, identity, profile, access, requirement },
        };
    }

    private void ConfigureSelectors()
    {
        workspaceSelector.ItemsSource = workspaces.Select(workspace => workspace.DisplayName).ToArray();
        workspaceSelector.SelectionChanged += WorkspaceSelectionChanged;
        themeSelector.ItemsSource = Enum.GetValues<ThemeMode>();
        themeSelector.SelectedItem = ThemeMode.System;
        themeSelector.SelectionChanged += (_, _) =>
        {
            if (themeSelector.SelectedItem is ThemeMode theme)
            {
                themeService.SetTheme(theme);
                shellPresentation.Theme = theme;
            }
        };
        languageSelector.ItemsSource = new[] { "vi-VN", "en-US" };
        languageSelector.SelectedItem = "vi-VN";
        languageSelector.SelectionChanged += (_, _) =>
        {
            if (languageSelector.SelectedItem is string culture && localization.TrySetCulture(culture))
            {
                shellPresentation.CultureName = culture;
            }
        };
        stateSelector.ItemsSource = new[]
        {
            PresentationStateKind.Ready,
            PresentationStateKind.Empty,
            PresentationStateKind.Loading,
            PresentationStateKind.Error,
            PresentationStateKind.ReadOnly,
            PresentationStateKind.Unavailable,
        };
        stateSelector.SelectionChanged += (_, _) => SetPresentationState();
    }

    private void ConfigureCompanyProof()
    {
        companySelector.ItemsSource = companyContext.AvailableCompanies
            .Select(company => company.DisplayName)
            .ToArray();
        companySelector.SelectedIndex = 0;
        companySelector.SelectionChanged += async (_, _) =>
        {
            var index = companySelector.SelectedIndex;
            if (index >= 0 && index < companyContext.AvailableCompanies.Count)
            {
                await companyScope.SwitchCompanyAsync(companyContext.AvailableCompanies[index].CompanyId);
            }
        };

        unauthorizedBehaviorSelector.ItemsSource = Enum.GetValues<UnauthorizedBehavior>();
        unauthorizedBehaviorSelector.SelectedItem = UnauthorizedBehavior.ReadOnly;
        unauthorizedBehaviorSelector.SelectionChanged += (_, _) => RefreshRequirementResolution();
    }

    private void CompanyScopeSnapshotChanged(object? sender, CompanyScopeSnapshot snapshot) =>
        Dispatcher.UIThread.Post(() => ApplyCompanySnapshot(snapshot));

    private void ApplyCompanySnapshot(CompanyScopeSnapshot snapshot)
    {
        currentCompanyValue.Text = $"{snapshot.Company.DisplayName} · CompanyId={snapshot.Company.CompanyId}";
        companyStateValue.Text = $"{snapshot.Status} · v{snapshot.Version}";
        var profile = snapshot.ProfileResult?.Profile;
        profileValue.Text = profile is null
            ? "—"
            : $"{profile.LegalName}\nTax Code: {profile.TaxCode}\n{profile.Address}\n{profile.Email} · {profile.Phone}\n" +
              string.Join(" · ", profile.AdditionalFields.Select(pair => $"{pair.Key}: {pair.Value}"));
        permissionValue.Text = snapshot.AuthorizationContext is null
            ? "—"
            : string.Join(", ", snapshot.AuthorizationContext.PermissionCodes.OrderBy(code => code.Value));
        capabilityValue.Text = snapshot.AuthorizationContext is null
            ? "—"
            : string.Join(", ", snapshot.AuthorizationContext.CapabilityCodes.OrderBy(code => code.Value));
        RefreshRequirementResolution();
    }

    private void RefreshRequirementResolution()
    {
        var behavior = unauthorizedBehaviorSelector.SelectedItem is UnauthorizedBehavior selected
            ? selected
            : UnauthorizedBehavior.Disable;
        var requirement = new PresentationRequirement(new PermissionCode("DATA.EDIT"), null, behavior);
        resolutionValue.Text = AuthorizationPresentationResolver.Resolve(
            requirement, companyScope.Snapshot.AuthorizationContext).ToString();
    }

    private void WorkspaceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var index = workspaceSelector.SelectedIndex;
        if (index < 0 || index >= workspaces.Count)
        {
            return;
        }

        var definition = workspaces[index];
        var result = workspaceHost.ShowWorkspace(definition);
        shellPresentation.CurrentWorkspaceId = definition.WorkspaceId;
        shellPresentation.CurrentWorkspaceTitle = definition.DisplayName;
        shellPresentation.StatusMessage = result.IsSuccess
            ? $"{definition.TemplateCode} · {result.Workspace!.TemplateModule}"
            : $"{definition.TemplateCode} · SAFE FAILURE";
    }

    private void SetPresentationState()
    {
        if (stateSelector.SelectedItem is not PresentationStateKind kind)
        {
            return;
        }

        var state = kind == PresentationStateKind.Error
            ? PresentationState.For(kind, new ErrorPresentation(
                localization.Get(new("State.Error")),
                "DUI-DEMO-001",
                "Safe developer detail; no stack trace is exposed by default.",
                true))
            : PresentationState.For(kind);
        shellPresentation.State = state;
        shellPresentation.StatusMessage = null;
        stateView.State = state;
    }

    private void RefreshLocalizedLabels()
    {
        workspaceLabel.Text = localization.Get(new("Demo.Workspace"));
        themeLabel.Text = localization.Get(new("Demo.Theme"));
        languageLabel.Text = localization.Get(new("Demo.Language"));
        stateLabel.Text = localization.Get(new("Demo.State"));
        iconLabel.Text = localization.Get(new("Demo.IconSamples"));
        companyLabel.Text = localization.Get(new("Demo.Company"));
        currentCompanyLabel.Text = localization.Get(new("Demo.CurrentCompany"));
        profileLabel.Text = localization.Get(new("Demo.CompanyProfile"));
        permissionLabel.Text = localization.Get(new("Demo.PermissionCodes"));
        capabilityLabel.Text = localization.Get(new("Demo.CapabilityCodes"));
        requirementLabel.Text = $"{localization.Get(new("Demo.Requirement"))}: PermissionCode=DATA.EDIT";
        behaviorLabel.Text = localization.Get(new("Demo.UnauthorizedBehavior"));
        resolutionLabel.Text = localization.Get(new("Demo.ResolvedPresentation"));
    }

    private static SemanticIconRegistry CreateDemoIconRegistry()
    {
        var registry = new SemanticIconRegistry();
        registry.Register(new IconDefinition(StandardIconKeys.Search,
            "M11,3 A8,8 0 1 0 11,19 A8,8 0 1 0 11,3 M17,17 L22,22"), replace: true);
        registry.Register(new IconDefinition(DemoLogoKey,
            "M12,2 L15,9 L22,9 L17,14 L19,22 L12,17 L5,22 L7,14 L2,9 L9,9 Z"));
        registry.Register(new IconDefinition(new IconKey("DEMO_SPARK"),
            "M12,2 L14,9 L22,12 L14,15 L12,22 L10,15 L2,12 L10,9 Z"));
        return registry;
    }

    private void StartSmokeRun(object? sender, EventArgs e)
    {
        smokeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        smokeTimer.Tick += AdvanceSmokeRun;
        smokeTimer.Start();
    }

    private async void AdvanceSmokeRun(object? sender, EventArgs e)
    {
        if (smokeAdvancing)
        {
            return;
        }

        smokeAdvancing = true;
        try
        {
            if (smokeStep < workspaces.Count)
            {
                workspaceSelector.SelectedIndex = smokeStep;
                var result = workspaceHost.CurrentResult!;
                Console.WriteLine($"SMOKE {workspaces[smokeStep].TemplateCode}: " +
                                  (result.IsSuccess ? "RESOLVED" : "SAFE_FAILURE"));
            }
            else if (smokeStep < workspaces.Count + 3)
            {
                if (smokeStep == workspaces.Count)
                {
                    workspaceSelector.SelectedIndex = 2;
                }

                themeSelector.SelectedIndex = smokeStep - workspaces.Count;
                EnsureSmokeWorkspacePreserved();
                Console.WriteLine($"SMOKE THEME: {themeSelector.SelectedItem}");
            }
            else if (smokeStep < workspaces.Count + 5)
            {
                languageSelector.SelectedIndex = smokeStep - workspaces.Count - 3;
                EnsureSmokeWorkspacePreserved();
                Console.WriteLine($"SMOKE CULTURE: {languageSelector.SelectedItem}");
            }
            else if (smokeStep < workspaces.Count + 11)
            {
                stateSelector.SelectedIndex = smokeStep - workspaces.Count - 5;
                Console.WriteLine($"SMOKE STATE: {stateSelector.SelectedItem}");
            }
            else if (smokeStep < workspaces.Count + 15)
            {
                var companyOffset = smokeStep - workspaces.Count - 11;
                var companyIndex = companyOffset switch { 0 => 0, 1 => 1, 2 => 2, _ => 0 };
                companySelector.SelectedIndex = companyIndex;
                await companyScope.SwitchCompanyAsync(companyContext.AvailableCompanies[companyIndex].CompanyId);
                EnsureSmokeCompanySwitchPreserved(companyIndex);
                var snapshot = companyScope.Snapshot;
                var resolved = AuthorizationPresentationResolver.Resolve(
                    new PresentationRequirement(new PermissionCode("DATA.EDIT"), null, UnauthorizedBehavior.ReadOnly),
                    snapshot.AuthorizationContext);
                Console.WriteLine($"SMOKE COMPANY: {snapshot.Company.Code} {snapshot.Status} {resolved} " +
                                  $"PROFILE={snapshot.ProfileResult!.Profile!.LegalName} " +
                                  $"PERMISSIONS={snapshot.AuthorizationContext!.PermissionCodes.Count} " +
                                  $"CAPABILITIES={snapshot.AuthorizationContext.CapabilityCodes.Count}");
            }
            else
            {
                smokeTimer!.Stop();
                Console.WriteLine("SMOKE CLEAN_EXIT: PASS");
                Close();
                return;
            }

            smokeStep++;
        }
        finally
        {
            smokeAdvancing = false;
        }
    }

    private void EnsureSmokeWorkspacePreserved()
    {
        if (shellPresentation.CurrentWorkspaceTitle != "Report Demo" ||
            workspaceHost.CurrentDefinition?.TemplateCode.Value != "REPORT")
        {
            throw new InvalidOperationException("Theme/culture switch did not preserve the selected workspace.");
        }

        Console.WriteLine("SMOKE WORKSPACE_PRESERVED: REPORT");
    }

    private void EnsureSmokeCompanySwitchPreserved(int expectedCompanyIndex)
    {
        EnsureSmokeWorkspacePreserved();
        var expectedCompany = companyContext.AvailableCompanies[expectedCompanyIndex];
        var snapshot = companyScope.Snapshot;
        if (snapshot.Company.CompanyId != expectedCompany.CompanyId ||
            snapshot.ProfileResult?.Profile?.CompanyId != expectedCompany.CompanyId ||
            snapshot.AuthorizationContext?.CompanyId != expectedCompany.CompanyId ||
            shellPresentation.Theme != ThemeMode.Dark ||
            shellPresentation.CultureName != "en-US")
        {
            throw new InvalidOperationException("Company switch did not preserve shell state or published stale data.");
        }

        Console.WriteLine("SMOKE COMPANY_STATE_PRESERVED: THEME=Dark CULTURE=en-US WORKSPACE=REPORT");
    }
}
