using Avalonia;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Core.Notifications;
using DynamicUI24.Core.ApplicationMenu;
using DynamicUI24.Core.Ribbon;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Setup;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.ImportExport;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Search;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.Sheets;
using DynamicUI24.Core.ModernWorkspace;
using DynamicUI24.Core.Reports;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

public sealed partial class MainWindow : Window
{
    private static readonly IconKey DemoLogoKey = new("DEMO_LOGO");
    private readonly IReadOnlyList<WorkspaceDefinition> workspaces;
    private readonly ShellPresentation shellPresentation;
    private readonly DictionaryLocalizationService localization;
    private readonly AvaloniaThemeService themeService;
    private readonly AppearancePreferenceService appearanceService;
    private readonly SemanticIconRegistry iconRegistry;
    private readonly DynamicUI24.Avalonia.DynamicWorkspaceHost workspaceHost;
    private readonly WorkspacePaneSessionStateStore paneSessionState = new();
    private readonly SetupWorkspaceHost setupWorkspaceHost;
    private DemoDataEntryProvider dataEntryProvider = null!;
    private DataEntryGridHost dataEntryGridHost = null!;
    private DemoMultiSheetWorkspace multiSheetWorkspace = null!;
    private readonly IPrivacyStateService privacyState;
    private ImportExportWorkspaceHost importExportHost = null!;
    private TabControl dataEntryTabs = null!;
    private readonly Lazy<Control> dataEntryWorkspace;
    private readonly Lazy<Control> reportWorkspace;
    private readonly SharedStateView stateView;
    private readonly ICompanyContextProvider companyContext;
    private readonly CompanyScopeCoordinator companyScope;
    private readonly ShellHost shell;
    private readonly DynamicRibbonHost ribbonHost;
    private readonly DynamicTreeHost treeHost;
    private readonly DynamicTreeResolver treeResolver = new();
    private readonly DynamicActionBarResolver actionBarResolver = new();
    private readonly WorkspaceActionBarDefinitions actionBarDefinitions = DemoActionBars.Create();
    private readonly WorkspaceNavigationService workspaceNavigation;
    private readonly InMemoryQuickAccessStore quickAccess = new(12);
    private readonly SearchCoordinator searchCoordinator;
    private readonly ActionCommandRegistry actionCommands;
    private readonly ActionBarCommandDispatcher actionDispatcher;
    private readonly DynamicActionBarHost topActionBar;
    private readonly DynamicActionBarHost bottomActionBar;
    private readonly NotificationCoordinator notificationCoordinator;
    private readonly NotificationActionBarAdapter notificationActionBars = new();
    private readonly NotificationActionDispatcher notificationDispatcher;
    private readonly NotificationHost notificationHost;
    private readonly ContextPanelCoordinator contextCoordinator;
    private readonly ContextPanelHost contextPanelHost;
    private readonly ContextItemPresenter contextItemPresenter;
    private readonly BreadcrumbHost breadcrumbHost;
    private readonly TreeDefinition treeDefinition = DemoTree.Create();
    private readonly DemoProfileContext demoProfile = new();
    private IReadOnlyList<WorkspaceDefinition> visibleWorkspaces = [];
    private readonly ComboBox workspaceSelector = new();
    private readonly ComboBox demoProfileSelector = new();
    private readonly ComboBox themeSelector = new();
    private readonly ComboBox languageSelector = new();
    private readonly ComboBox stateSelector = new();
    private readonly ComboBox companySelector = new();
    private readonly ComboBox unauthorizedBehaviorSelector = new();
    private readonly ComboBox selectionSelector = new();
    private readonly TextBlock workspaceLabel = new();
    private readonly TextBlock demoProfileLabel = new() { Text = "Demo profile" };
    private readonly TextBlock themeLabel = new();
    private readonly TextBlock languageLabel = new();
    private readonly TextBlock stateLabel = new();
    private readonly TextBlock iconLabel = new();
    private readonly TextBlock companyLabel = new();
    private readonly TextBlock currentCompanyLabel = new();
    private readonly TextBlock currentCompanyValue = new();
    private readonly TextBlock profileLabel = new();
    private readonly TextBlock profileValue = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock demoProfileStateValue = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock permissionLabel = new();
    private readonly TextBlock permissionValue = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock capabilityLabel = new();
    private readonly TextBlock capabilityValue = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock requirementLabel = new();
    private readonly TextBlock behaviorLabel = new();
    private readonly TextBlock resolutionLabel = new();
    private readonly TextBlock resolutionValue = new();
    private readonly TextBlock companyStateValue = new();
    private readonly TextBlock selectionLabel = new();
    private DispatcherTimer? smokeTimer;
    private int smokeStep;
    private bool smokeAdvancing;
    private string smokeStage = "STARTUP";
    private int actionRefreshCount;

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
        appearanceService = new AppearancePreferenceService();
        iconRegistry = CreateDemoIconRegistry();
        shellPresentation = new ShellPresentation(
            new ApplicationBrand("Framework Demo", DemoLogoKey, "#7C3AED"));
        workspaceHost = new DynamicUI24.Avalonia.DynamicWorkspaceHost(composition.Registry, localization);
        var setupProvider = new DemoSetupProvider();
        setupWorkspaceHost = new SetupWorkspaceHost(DemoSetup.Categories, setupProvider,
            new SpecializedSetupValidator(setupProvider, composition.Registry), DemoSetup.CreateEditors(composition.Registry, setupProvider), localization, iconRegistry,
            companyContext.CurrentCompany, appearance: appearanceService);
        privacyState = new PrivacyStateService();
        var privacyResolver = new PrivacyPolicyResolver();
        var sensitiveValuePresenter = new SensitiveValuePresenter();
        contextItemPresenter = new ContextItemPresenter(privacyResolver, sensitiveValuePresenter);
        contextCoordinator = new ContextPanelCoordinator([new DemoContextProvider()]);
        dataEntryWorkspace = new Lazy<Control>(
            () => CreateDataEntryWorkspace(privacyResolver, sensitiveValuePresenter),
            LazyThreadSafetyMode.ExecutionAndPublication);
        reportWorkspace = new Lazy<Control>(
            () => CreateReportWorkspace(),
            LazyThreadSafetyMode.ExecutionAndPublication);
        workspaceHost.RegisterViewFactory(StandardTemplateCodes.Setup, _ => setupWorkspaceHost);
        workspaceHost.RegisterViewFactory(StandardTemplateCodes.DataEntry, _ => dataEntryWorkspace.Value);
        workspaceHost.RegisterViewFactory(StandardTemplateCodes.Report, _ => reportWorkspace.Value);
        workspaceNavigation = new WorkspaceNavigationService(workspaces);
        workspaceNavigation.NavigationChanged += (_, args) =>
        {
            if (args.CurrentWorkspace is not null) NavigateFromActionBar(args.CurrentWorkspace);
        };
        stateView = new SharedStateView(localization, iconRegistry);

        var lifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
        var exitService = new AvaloniaApplicationExitService(lifetime);
        shell = new ShellHost(
            shellPresentation,
            localization,
            iconRegistry,
            exitService);
        contextPanelHost = new ContextPanelHost(localization);
        contextPanelHost.CloseRequested += (_, _) => shell.IsContextPanelOpen = false;
        shell.ContextPanelContent = contextPanelHost;
        shell.IsContextPanelOpen = true;
        breadcrumbHost = new BreadcrumbHost(localization);
        breadcrumbHost.ItemActivated += async (_, item) =>
        {
            if (item.NavigationTarget is not null) await workspaceNavigation.NavigateAsync(item.NavigationTarget);
        };
        shell.BreadcrumbContent = breadcrumbHost;
        contextCoordinator.Changed += (_, result) => Dispatcher.UIThread.Post(() => ShowContext(result));
        var menuComposer = new ApplicationMenuComposer();
        menuComposer.Register(new DemoPreferencesContributor());
        menuComposer.Register(new PrivacyMenuContributor());
        shell.ApplicationMenuContent = new ApplicationMenuView(
            shellPresentation.Brand,
            menuComposer,
            localization,
            iconRegistry,
            themeService,
            appearanceService,
            new DemoLayoutResetService(),
            exitService,
            companyContext,
            companyScope,
            new DemoAccountPresentationProvider(),
            new DemoLicensePresentationProvider());
        var commandRegistry = new UiCommandRegistry();
        commandRegistry.Register("DEMO.HELLO", (_, _) =>
            Task.FromResult(RibbonCommandResult.Success("Hello from a registered UI command.")));
        commandRegistry.Register("DEMO.SELECTION", (_, _) =>
            Task.FromResult(RibbonCommandResult.Success("Selection command dispatched.")));
        foreach (var code in new[] { "SHEET_A", "SHEET_B", "SHEET_C", "SHEET_D" })
        {
            var captured = new SheetCode(code);
            commandRegistry.Register($"DEMO.SHEET.{code}", async (_, token) =>
            {
                await workspaceNavigation.NavigateAsync("data-entry-demo", token);
                return multiSheetWorkspace.Activate(captured)
                    ? RibbonCommandResult.Success($"Activated {captured.Value}")
                    : RibbonCommandResult.Unavailable("SHEET_ACTIVATION_REJECTED");
            });
        }
        var searchProviders = DemoSearch.CreateProviders(workspaces, quickAccess, out _).ToList();
        searchProviders.Add(new DemoSearchProvider("SHEETS", new[] { "SHEET_A", "SHEET_B", "SHEET_C", "SHEET_D" }
            .Select((code, index) => new SearchResult($"sheet:{code}", SearchResultKind.Command, "SHEETS",
                $"Go to Sheet {code[^1]}", $"Workspace + SheetCode: data-entry-demo/{code}", providerRank: index,
                workspaceId: "data-entry-demo", registeredCommandCode: $"DEMO.SHEET.{code}",
                deduplicationKey: $"data-entry-demo:{code}", canFavorite: true, canPin: true, canRecordRecent: true)).ToArray()));
        searchCoordinator = new SearchCoordinator(searchProviders, new(48, 10));
        quickAccess.AddFavorite(new("workspace:dashboard-demo", SearchResultKind.Workspace, "dashboard-demo", "WORKSPACES"));
        quickAccess.Pin(new("workspace:report-demo", SearchResultKind.Workspace, "report-demo", "WORKSPACES"));
        quickAccess.AddFavorite(new("workspace:report-demo", SearchResultKind.Workspace, "report-demo", "WORKSPACES"));
        var dispatcher = new RibbonCommandDispatcher(
            new DemoRibbonNavigationService(workspaces, NavigateFromRibbon),
            new DemoRibbonRefreshService(RefreshFromRibbon),
            commandRegistry);
        actionCommands = new ActionCommandRegistry();
        actionCommands.Register("DEMO.ACTION.CUSTOM", (_, _) =>
            Task.FromResult(ActionCommandResult.Success("Custom registered Action Bar command dispatched.")));
        actionCommands.Register("DEMO.ACTION.GATED", (_, _) =>
            Task.FromResult(ActionCommandResult.Success("Permission-gated Action Bar command dispatched.")));
        actionCommands.Register("DEMO.GRID.COPY", async (_, token) => GridActionResult(await dataEntryGridHost.CopySelectionAsync(token)));
        actionCommands.Register("DEMO.GRID.CUT", async (_, token) => GridActionResult(await dataEntryGridHost.CutSelectionAsync(token)));
        actionCommands.Register("DEMO.GRID.PASTE", async (_, token) => GridActionResult(await dataEntryGridHost.PasteSelectionAsync(token)));
        actionCommands.Register("DEMO.GRID.UNDO", async (_, token) => GridActionResult(await dataEntryGridHost.UndoAsync(token)));
        actionCommands.Register("DEMO.GRID.REDO", async (_, token) => GridActionResult(await dataEntryGridHost.RedoAsync(token)));
        actionCommands.Register("DEMO.GRID.CLEAR", async (_, token) => GridActionResult(await dataEntryGridHost.ClearSelectionAsync(token)));
        foreach (var command in new[] { "DEMO.IMPORT.PROFILE", "DEMO.IMPORT.XLSX", "DEMO.IMPORT.CSV", "DEMO.IMPORT.CUSTOM",
                     "DEMO.EXPORT.VIEW", "DEMO.EXPORT.SELECTED", "DEMO.EXPORT.FILTERED" })
            actionCommands.Register(command, (_, _) =>
            {
                dataEntryTabs.SelectedIndex = 1;
                importExportHost.ShowProfiles(DemoImportExport.ImportProfiles, DemoImportExport.ExportProfiles);
                return Task.FromResult(ActionCommandResult.Success($"Opened generic import/export host for {command}."));
            });
        actionCommands.Register("DEMO.UPDATE_AND_RESTART", (_, _) =>
            Task.FromResult(ActionCommandResult.Success("Update-ready guidance command dispatched; no updater was run.")));
        DemoEditorActions.Register(actionCommands);
        workspaceHost.RegisterViewFactory(StandardTemplateCodes.Dashboard, definition =>
            definition.WorkspaceId == "editor-demo"
                ? new DemoEditorWorkspace(localization, actionCommands, () => new(CreateActionBarContext(definition)))
                : definition.WorkspaceId == "ui-authoring-demo"
                    ? new DemoUiAuthoringWorkspace(() => demoProfile.Security)
                    : definition.WorkspaceId == "modern-workspace-demo"
                        ? new DemoModernWorkspace(paneSessionState)
                    : new StackPanel { Margin = new Thickness(18), Children =
                        { new TextBlock { Text = definition.DisplayName, FontSize = 24 } } });
        actionDispatcher = new ActionBarCommandDispatcher(
            workspaceNavigation, new DemoActionRefreshService(RefreshFromActionBar), actionCommands);
        topActionBar = new DynamicActionBarHost(actionDispatcher, localization, iconRegistry, appearanceService);
        bottomActionBar = new DynamicActionBarHost(actionDispatcher, localization, iconRegistry, appearanceService);
        topActionBar.CommandCompleted += ActionBarCommandCompleted;
        bottomActionBar.CommandCompleted += ActionBarCommandCompleted;
        notificationCoordinator = new NotificationCoordinator(
            [new DemoNotificationProvider(), new ThrowingNotificationProvider()]);
        notificationDispatcher = new NotificationActionDispatcher(workspaceNavigation, actionCommands,
            () => new ActionCommandExecutionContext(CreateActionBarContext(workspaceHost.CurrentDefinition ?? workspaces[0])),
            new DemoFocusTargetService(), new DemoNotificationMenuService());
        notificationHost = new NotificationHost(notificationCoordinator, notificationDispatcher, localization, iconRegistry);
        shell.NotificationContent = notificationHost;
        notificationHost.ActionCompleted += (_, result) =>
            shellPresentation.StatusMessage = $"Notification: {result.Status} · {result.DiagnosticCode ?? "OK"}";
        notificationCoordinator.Changed += (_, _) => Dispatcher.UIThread.Post(RefreshActionBars);
        ribbonHost = new DynamicRibbonHost(
            DemoRibbon.Create(), workspaces, CreateRibbonContext(workspaces[0]),
            new DynamicRibbonResolver(), dispatcher, localization, iconRegistry);
        ribbonHost.CommandCompleted += (_, result) =>
            shellPresentation.StatusMessage = $"Ribbon: {result.Status} · {result.DiagnosticCode ?? result.Message ?? "OK"}";
        shell.RibbonContent = ribbonHost;
        treeHost = new DynamicTreeHost(localization, iconRegistry);
        treeHost.NodeSelected += (_, args) => NavigateTreeNode(args.Node);
        shell.NavigationContent = treeHost;
        var searchActivation = new SearchActivationService(workspaceNavigation, commandRegistry,
            () => new RibbonCommandExecutionContext(CreateRibbonContext(workspaceHost.CurrentDefinition ?? workspaces[0]),
                workspaceHost.CurrentDefinition),
            new DemoSettingNavigationService(_ => shell.IsApplicationMenuOpen = true), quickAccess);
        shell.SearchContent = new SearchPaletteView(searchCoordinator,
            new SearchResultPresenter(privacyResolver, sensitiveValuePresenter), localization, iconRegistry,
            () => new SearchQuery("", SearchScope.GlobalSearch,
                companyContext.CurrentCompany.CompanyId.Value, workspaceHost.CurrentDefinition?.WorkspaceId,
                workspaceHost.CurrentDefinition?.TemplateCode.Value, treeHost.SelectedNodeId,
                localization.CurrentCulture, CurrentAuthorization,
                new PrivacyResolutionContext(true, null, privacyState.RequestedMode,
                    new MandatoryPrivacyPolicy(), companyContext.CurrentCompany.CompanyId,
                    workspaceHost.CurrentDefinition?.WorkspaceId, Generation: privacyState.Generation)),
            searchActivation.ActivateAsync, quickAccess);
        privacyState.StateChanged += (_, _) => InvalidateSearch();
        shell.WorkspaceContent = BuildDemoSurface();
        ShellContainer.Content = shell;

        ConfigureSelectors();
        ConfigureCompanyProof();
        demoProfile.Changed += async (_, _) => await ApplyDemoProfileAsync();
        localization.CultureChanged += (_, _) => RefreshLocalizedLabels();
        RefreshLocalizedLabels();
        RefreshWorkspaceSelector();
        workspaceSelector.SelectedIndex = 0;
        stateSelector.SelectedIndex = 0;

        companyScope.SnapshotChanged += CompanyScopeSnapshotChanged;
        Opened += async (_, _) => await companyScope.InitializeAsync();
        Closed += (_, _) => { contextCoordinator.Dispose(); companyScope.Dispose(); };

        if (Program.IsSmokeRun)
        {
            Opened += StartSmokeRun;
        }
    }

    private Control CreateDataEntryWorkspace(IPrivacyPolicyResolver privacyResolver,
        ISensitiveValuePresenter sensitiveValuePresenter)
    {
        dataEntryProvider = new DemoDataEntryProvider();
        dataEntryGridHost = new DataEntryGridHost(new DataEntryGridRuntime(
                DemoDataEntry.CreateDefinition(), dataEntryProvider, privacyResolver: privacyResolver,
                privacyState: privacyState, sensitiveValuePresenter: sensitiveValuePresenter),
            localization, appearanceService, privacyResolver: privacyResolver, privacyState: privacyState,
            sensitivePresenter: sensitiveValuePresenter);
        multiSheetWorkspace = new(dataEntryGridHost, dataEntryProvider, localization, appearanceService, privacyState,
            privacyResolver, sensitiveValuePresenter, companyContext.CurrentCompany, () =>
            {
                contextCoordinator.Invalidate();
                _ = RefreshContextAsync();
            });
        importExportHost = new ImportExportWorkspaceHost(localization);
        importExportHost.ShowProfiles(DemoImportExport.ImportProfiles, DemoImportExport.ExportProfiles);
        dataEntryTabs = new TabControl
        {
            ItemsSource = new object[]
            {
                new TabItem { Header = "Multi-Sheet Data", Content = multiSheetWorkspace.View },
                new TabItem { Header = "Import / Export", Content = importExportHost },
                new TabItem { Header = "Privacy", Content = new DemoPrivacyPanel(privacyState) },
            },
            SelectedIndex = 0,
        };
        dataEntryGridHost.Changed += (_, _) =>
        {
            if (workspaceHost.CurrentDefinition?.WorkspaceId == "data-entry-demo")
            {
                RefreshRibbon();
                RefreshActionBars();
                _ = RefreshContextAsync();
            }
        };
        return dataEntryTabs;
    }

    private Control CreateReportWorkspace()
    {
        var provider = new DemoReportProvider();
        var definition = DemoReport.CreateDefinition();
        var runtime = new ReportRuntime(definition, provider, provider, provider,
            operations: new OperationCoordinator());
        var reportAuthorization = new ReportAuthorizationResolver(new DefaultUiAuthorizationResolver());
        var workspace = workspaces.Single(x => x.WorkspaceId == "report-demo");
        return new ReportWorkspaceHost(runtime, localization,
            () => new ReportExecutionContext(companyContext.CurrentCompany, "DEMO"), appearanceService, paneSessionState,
            actionCommands, () => new(CreateActionBarContext(workspace)),
            async token => (ReportAuthorizationSnapshot?)await reportAuthorization.ResolveAsync(definition, new(demoProfile.Security,
                companyContext.CurrentCompany.CompanyId, workspace.WorkspaceId, new("DEMO.REPORT"), new(1),
                companyScope.Snapshot.Version, demoProfile.Generation, demoProfile.Generation, PrivacyMode.On), token),
            actionDispatcher, iconRegistry);
    }

    private Control BuildDemoSurface()
    {
        var selectors = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 12,
            Children =
            {
                Field(workspaceLabel, workspaceSelector, 0),
                Field(demoProfileLabel, demoProfileSelector, 1),
                Field(themeLabel, themeSelector, 2),
                Field(languageLabel, languageSelector, 0, 1),
                Field(stateLabel, stateSelector, 1, 1),
                Field(selectionLabel, selectionSelector, 2, 1),
            },
        };

        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
            RowSpacing = 14,
            Children =
            {
                selectors,
                Place(topActionBar, 1),
                Framed(workspaceHost, 2),
                Place(bottomActionBar, 3),
                Framed(BuildCompanyProofSurface(), 4),
                Framed(stateView, 5),
                BuildIconSamples(6),
            },
        };
        return new ScrollViewer { Content = content };
    }

    private static Control Field(TextBlock label, ComboBox selector, int column, int row = 0)
    {
        selector.MinWidth = 120;
        var panel = new StackPanel { Spacing = 5, Children = { label, selector } };
        Grid.SetColumn(panel, column);
        Grid.SetRow(panel, row);
        return panel;
    }

    private static T Place<T>(T control, int row) where T : Control
    {
        Grid.SetRow(control, row);
        return control;
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
        foreach (var text in new[] { companyStateValue, permissionValue, capabilityValue, resolutionValue, demoProfileStateValue })
        {
            text.Bind(TextBlock.ForegroundProperty, text.GetResourceObservable("DuiTextMutedBrush"));
        }

        return new StackPanel
        {
            Spacing = 12,
            Children = { selectors, identity, profile, demoProfileStateValue, access, requirement },
        };
    }

    private void ConfigureSelectors()
    {
        workspaceSelector.SelectionChanged += WorkspaceSelectionChanged;
        demoProfileSelector.ItemsSource = Enum.GetValues<DemoAuthoringProfile>();
        demoProfileSelector.SelectedItem = DemoAuthoringProfile.Viewer;
        demoProfileStateValue.Text = "Viewer · field ReadOnly · feature Hidden · Export denied";
        demoProfileSelector.SelectionChanged += (_, _) =>
        {
            if (demoProfileSelector.SelectedItem is DemoAuthoringProfile profile) demoProfile.Select(profile);
        };
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
        selectionSelector.ItemsSource = new[] { 0, 1, 5 };
        selectionSelector.SelectedIndex = 0;
        selectionSelector.SelectionChanged += (_, _) =>
        {
            RefreshRibbon();
            RefreshActionBars();
        };
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

    private EffectiveAuthorizationContext CurrentAuthorization => demoProfile.Merge(
        companyScope.Snapshot.AuthorizationContext, companyContext.CurrentCompany.CompanyId);

    private void RefreshWorkspaceSelector(string? preferredWorkspaceId = null)
    {
        preferredWorkspaceId ??= workspaceHost.CurrentDefinition?.WorkspaceId;
        var resolution = demoProfile.ResolveWorkspaces(workspaces, preferredWorkspaceId);
        visibleWorkspaces = resolution.VisibleWorkspaces;
        workspaceSelector.ItemsSource = visibleWorkspaces.Select(workspace => workspace.DisplayName).ToArray();
        var preferredIndex = resolution.ActiveWorkspace is null ? -1 : visibleWorkspaces.ToList().FindIndex(x =>
            x.WorkspaceId.Equals(resolution.ActiveWorkspace.WorkspaceId, StringComparison.OrdinalIgnoreCase));
        workspaceSelector.SelectedIndex = preferredIndex >= 0 ? preferredIndex : visibleWorkspaces.Count > 0 ? 0 : -1;
    }

    private async Task ApplyDemoProfileAsync()
    {
        var requestedGeneration = demoProfile.Generation;
        var authoringAllowed = await demoProfile.CanOpenAuthoringAsync(companyContext.CurrentCompany.CompanyId);
        if (requestedGeneration != demoProfile.Generation) return;
        demoProfileStateValue.Text = demoProfile.CurrentProfile switch
        {
            DemoAuthoringProfile.Viewer => "Viewer · field ReadOnly · feature Hidden · Export denied",
            DemoAuthoringProfile.Editor => "Editor · field Editable · normal runtime enabled · Developer Mode hidden",
            _ => "Administrator · Developer Mode visible · Publish enabled · Export allowed",
        };
        var active = workspaceHost.CurrentDefinition;
        RefreshWorkspaceSelector(authoringAllowed ? active?.WorkspaceId : null);
        InvalidateSearch();
        RefreshTree(companyScope.Snapshot);
        RefreshRibbon();
        RefreshActionBars();
        if (!authoringAllowed && active?.WorkspaceId.Equals("ui-authoring-demo", StringComparison.OrdinalIgnoreCase) == true)
            shellPresentation.StatusMessage = "Developer UI Authoring closed: Demo profile is not authorized.";
    }

    private void CompanyScopeSnapshotChanged(object? sender, CompanyScopeSnapshot snapshot) =>
        Dispatcher.UIThread.Post(() => ApplyCompanySnapshot(snapshot));

    private void ApplyCompanySnapshot(CompanyScopeSnapshot snapshot)
    {
        contextCoordinator.Invalidate();
        InvalidateSearch();
        privacyState.InvalidateContext(snapshot.Company.CompanyId.Value, workspaceHost.CurrentDefinition?.WorkspaceId);
        currentCompanyValue.Text = $"{snapshot.Company.DisplayName} · CompanyId={snapshot.Company.CompanyId}";
        companyStateValue.Text = $"{snapshot.Status} · v{snapshot.Version}";
        var profile = snapshot.ProfileResult?.Profile;
        profileValue.Text = profile is null
            ? "—"
            : $"{profile.LegalName}\nTax Code: {profile.TaxCode}\n{profile.Address}\n{profile.Email} · {profile.Phone}\n" +
              string.Join(" · ", profile.AdditionalFields.Select(pair => $"{pair.Key}: {pair.Value}"));
        permissionValue.Text = string.Join(", ", CurrentAuthorization.PermissionCodes.OrderBy(code => code.Value));
        capabilityValue.Text = string.Join(", ", CurrentAuthorization.CapabilityCodes.OrderBy(code => code.Value));
        RefreshRequirementResolution();
        RefreshRibbon();
        RefreshActionBars();
        RefreshTree(snapshot);
        setupWorkspaceHost.UpdateContext(snapshot.Company, CurrentAuthorization);
        if (workspaceHost.CurrentDefinition?.WorkspaceId == "data-entry-demo")
            _ = LoadDataEntryAsync(snapshot.Company, snapshot.AuthorizationContext);
        _ = RefreshContextAsync();
        _ = RefreshNotificationsAsync();
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
        InvalidateSearch();
        var index = workspaceSelector.SelectedIndex;
        if (index < 0 || index >= visibleWorkspaces.Count)
        {
            return;
        }

        var definition = visibleWorkspaces[index];
        if (workspaceHost.CurrentDefinition?.WorkspaceId is { } previousWorkspace &&
            !previousWorkspace.Equals(definition.WorkspaceId, StringComparison.OrdinalIgnoreCase))
            privacyState.InvalidateContext(workspaceId: definition.WorkspaceId);
        if (workspaceHost.CurrentDefinition?.WorkspaceId == "data-entry-demo" && definition.WorkspaceId != "data-entry-demo")
            dataEntryGridHost.Deactivate();
        var result = workspaceHost.ShowWorkspace(definition);
        shellPresentation.CurrentWorkspaceId = definition.WorkspaceId;
        shellPresentation.CurrentWorkspaceTitle = definition.DisplayName;
        shellPresentation.StatusMessage = result.IsSuccess
            ? $"{definition.TemplateCode} · {result.Workspace!.TemplateModule}"
            : $"{definition.TemplateCode} · SAFE FAILURE";
        RefreshRibbon();
        RefreshActionBars();
        RefreshBreadcrumb(definition);
        treeHost.SelectWorkspace(definition.WorkspaceId);
        if (definition.WorkspaceId == "data-entry-demo")
            _ = LoadDataEntryAsync(companyContext.CurrentCompany, companyScope.Snapshot.AuthorizationContext);
        _ = RefreshContextAsync();
        _ = RefreshNotificationsAsync();
    }

    private void InvalidateSearch()
    {
        searchCoordinator.Invalidate();
        if (shell.IsSearchOpen && shell.SearchContent is { } palette) _ = palette.RefreshAsync();
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
        RefreshActionBars();
    }

    private void RefreshLocalizedLabels()
    {
        workspaceLabel.Text = localization.Get(new("Demo.Workspace"));
        themeLabel.Text = localization.Get(new("Demo.Theme"));
        languageLabel.Text = localization.Get(new("Demo.Language"));
        stateLabel.Text = localization.Get(new("Demo.State"));
        iconLabel.Text = localization.Get(new("Demo.IconSamples"));
        selectionLabel.Text = localization.Get(new("Demo.SelectionCount"));
        companyLabel.Text = localization.Get(new("Demo.Company"));
        currentCompanyLabel.Text = localization.Get(new("Demo.CurrentCompany"));
        profileLabel.Text = localization.Get(new("Demo.CompanyProfile"));
        permissionLabel.Text = localization.Get(new("Demo.PermissionCodes"));
        capabilityLabel.Text = localization.Get(new("Demo.CapabilityCodes"));
        requirementLabel.Text = $"{localization.Get(new("Demo.Requirement"))}: PermissionCode=DATA.EDIT";
        behaviorLabel.Text = localization.Get(new("Demo.UnauthorizedBehavior"));
        resolutionLabel.Text = localization.Get(new("Demo.ResolvedPresentation"));
    }

    private RibbonResolutionContext CreateRibbonContext(WorkspaceDefinition workspace) => new(
        companyContext.CurrentCompany,
        workspace,
        workspace.TemplateCode,
        CurrentAuthorization,
        new RibbonSelectionContext(CurrentSelectionCount(workspace)));

    private void RefreshRibbon()
    {
        var workspace = workspaceHost.CurrentDefinition ?? workspaces[0];
        ribbonHost.UpdateContext(CreateRibbonContext(workspace));
    }

    private ActionBarResolutionContext CreateActionBarContext(WorkspaceDefinition workspace) => new(
        companyContext.CurrentCompany,
        workspace,
        workspace.TemplateCode,
        CurrentAuthorization,
        new ActionSelectionContext(CurrentSelectionCount(workspace)),
        shellPresentation.State,
        CreateActionBarStatus(workspace),
        workspace.WorkspaceId == "data-entry-demo" ? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["COPY"] = dataEntryGridHost.Runtime.CellSelection.HasCellSelection,
            ["CUT"] = dataEntryGridHost.Runtime.CanClearCellSelection(),
            ["PASTE"] = dataEntryGridHost.Runtime.ActiveCell is not null,
            ["CLEAR"] = dataEntryGridHost.Runtime.CanClearCellSelection(),
            ["UNDO"] = dataEntryGridHost.Runtime.CanUndo,
            ["REDO"] = dataEntryGridHost.Runtime.CanRedo,
        } : null);

    private ActionBarStatus CreateActionBarStatus(WorkspaceDefinition workspace)
    {
        var selected = CurrentSelectionCount(workspace);
        return workspace.WorkspaceId switch
        {
            "data-entry-demo" => dataEntryGridHost.Runtime.Status,
            "dashboard-demo" => new(125, 125, selected, 0, 1, actionRefreshCount, false),
            _ => new(0, 0, selected, 0, 0, 0, shellPresentation.State.Kind == PresentationStateKind.ReadOnly),
        };
    }

    private void RefreshActionBars()
    {
        var workspace = workspaceHost.CurrentDefinition;
        if (workspace is null)
        {
            topActionBar.Clear();
            bottomActionBar.Clear();
            return;
        }
        var context = CreateActionBarContext(workspace);
        var bars = actionBarDefinitions.ForWorkspace(workspace.WorkspaceId);
        var notificationTop = notificationActionBars.Create(NotificationSurface.TopActionBar,
            notificationCoordinator.Current.ForSurface(NotificationSurface.TopActionBar));
        var notificationBottom = notificationActionBars.Create(NotificationSurface.BottomActionBar,
            notificationCoordinator.Current.ForSurface(NotificationSurface.BottomActionBar));
        ShowActionBar(topActionBar, MergeActionBars(ActionBarPosition.Top,
            bars.FirstOrDefault(x => x.Position == ActionBarPosition.Top), notificationTop), context);
        ShowActionBar(bottomActionBar, MergeActionBars(ActionBarPosition.Bottom,
            bars.FirstOrDefault(x => x.Position == ActionBarPosition.Bottom), notificationBottom), context);
    }

    private static ActionBarDefinition? MergeActionBars(ActionBarPosition position, ActionBarDefinition? workspace,
        ActionBarDefinition notifications)
    {
        var actions = (workspace?.Actions ?? []).Concat(notifications.Actions).ToArray();
        return actions.Length == 0 ? null : new ActionBarDefinition($"combined-{position}", $"COMBINED_{position}", position, actions);
    }

    private async Task RefreshNotificationsAsync()
    {
        var workspaceId = workspaceHost.CurrentDefinition?.WorkspaceId;
        await notificationCoordinator.RefreshAsync(companyContext.CurrentCompany, workspaceId,
            CurrentAuthorization);
    }

    private void ShowActionBar(DynamicActionBarHost host, ActionBarDefinition? definition,
        ActionBarResolutionContext context)
    {
        if (definition is null) host.Clear();
        else host.Show(actionBarResolver.Resolve(definition, context, workspaces), new(context));
    }

    private void ActionBarCommandCompleted(object? sender, ActionCommandResult result) =>
        shellPresentation.StatusMessage = $"Action Bar: {result.Status} · {result.DiagnosticCode ?? result.Message ?? "OK"}";

    private void NavigateFromActionBar(WorkspaceDefinition workspace)
    {
        var index = visibleWorkspaces.ToList().FindIndex(x => x.WorkspaceId.Equals(workspace.WorkspaceId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) workspaceSelector.SelectedIndex = index;
    }

    private async Task RefreshContextAsync()
    {
        var workspace = workspaceHost.CurrentDefinition;
        var selected = workspace?.WorkspaceId == "data-entry-demo"
            ? multiSheetWorkspace.ActiveRuntime?.SelectedRowKeys.FirstOrDefault() ?? default : default;
        var rowKey = string.IsNullOrWhiteSpace(selected.Value) ? null : selected.Value;
        var sheetCode = workspace?.WorkspaceId == "data-entry-demo" ? multiSheetWorkspace.ActiveSheetCode : null;
        await contextCoordinator.ResolveAsync("DEMO.CONTEXT", (generation, token) => new(
            companyContext.CurrentCompany.CompanyId, workspace?.WorkspaceId, workspace?.TemplateCode.Value,
            workspace?.WorkspaceId, new ContextSelection(EntityKey: sheetCode is null ? null : $"SHEET:{sheetCode.Value.Value}", RowKey: rowKey),
            new HelpContextCode(workspace?.WorkspaceId == "data-entry-demo" ? "DATAENTRY.ROW" : "SHELL.WORKSPACE"),
            localization.CurrentCulture, privacyState.RequestedMode,
            companyScope.Snapshot.AuthorizationContext, generation, token));
    }

    private void ShowContext(ContextPanelResult result)
    {
        var workspace = workspaceHost.CurrentDefinition;
        var request = new ContextPanelRequest(companyContext.CurrentCompany.CompanyId, workspace?.WorkspaceId,
            workspace?.TemplateCode.Value, workspace?.WorkspaceId, new(), null,
            localization.CurrentCulture, privacyState.RequestedMode,
            companyScope.Snapshot.AuthorizationContext, result.Generation, CancellationToken.None);
        contextPanelHost.ShowResult(result, item => contextItemPresenter.Present(item, request,
            new MandatoryPrivacyPolicy(ProtectConfidential: true, ProtectRestricted: true)).DisplayValue);
    }

    private void RefreshBreadcrumb(WorkspaceDefinition definition)
    {
        breadcrumbHost.Path = new BreadcrumbPath([
            new("HOME", "Home", NavigationTarget: "dashboard-demo"),
            new("DATA", "Data", NavigationTarget: "dashboard-demo"),
            new("WORKSPACE", definition.DisplayName, NavigationTarget: definition.WorkspaceId),
            new("CURRENT", definition.DisplayName, IsCurrent: true),
        ]);
    }

    private void RefreshFromActionBar()
    {
        actionRefreshCount++;
        if (workspaceHost.CurrentDefinition is { WorkspaceId: "data-entry-demo" }) _ = dataEntryGridHost.RefreshAsync();
        else if (workspaceHost.CurrentDefinition is { } workspace) workspaceHost.ShowWorkspace(workspace);
        shellPresentation.StatusMessage = $"{localization.Get(new("ActionBar.RefreshComplete"))} #{actionRefreshCount}";
        RefreshActionBars();
    }

    private int CurrentSelectionCount(WorkspaceDefinition workspace) => workspace.WorkspaceId == "data-entry-demo"
        ? dataEntryGridHost.Runtime.InteractionSelectionCount
        : selectionSelector.SelectedItem is int count ? count : 0;

    private static ActionCommandResult GridActionResult(GridPasteResult result) => result.DiagnosticCode is null
        ? ActionCommandResult.Success($"{result.AppliedCellCount} cell(s)")
        : ActionCommandResult.Unavailable(result.DiagnosticCode);

    private Task LoadDataEntryAsync(CompanyDescriptor company, EffectiveAuthorizationContext? effectiveAuthorization)
        => multiSheetWorkspace.UpdateContextAsync(company, effectiveAuthorization);

    private void NavigateFromRibbon(WorkspaceDefinition workspace)
    {
        var index = visibleWorkspaces.ToList().FindIndex(x => x.WorkspaceId == workspace.WorkspaceId);
        if (index >= 0) workspaceSelector.SelectedIndex = index;
    }

    private void NavigateTreeNode(TreeNodeDefinition node)
    {
        if (node.WorkspaceId is null) return;
        var index = visibleWorkspaces.ToList().FindIndex(x => x.WorkspaceId.Equals(node.WorkspaceId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) workspaceSelector.SelectedIndex = index;
        else shellPresentation.StatusMessage = "Tree: WORKSPACE_UNKNOWN";
    }

    private void RefreshTree(CompanyScopeSnapshot snapshot)
    {
        var previouslySelectedNodeId = treeHost.SelectedNodeId;
        var tree = treeResolver.Resolve(treeDefinition,
            new TreeResolutionContext(snapshot.Company, CurrentAuthorization), visibleWorkspaces);
        treeHost.Show(tree);
        var current = workspaceHost.CurrentDefinition;
        if (current is not null && TreeContainsWorkspace(tree.RootNodes, current.WorkspaceId))
        {
            treeHost.SelectWorkspace(current.WorkspaceId);
            return;
        }
        var fallback = NearestNavigableAncestor(previouslySelectedNodeId, tree.RootNodes) ?? FirstNavigable(tree.RootNodes);
        if (fallback?.Definition.WorkspaceId is { } workspaceId)
        {
            NavigateTreeNode(fallback.Definition);
        }
        else
        {
            workspaceHost.Clear();
            shellPresentation.CurrentWorkspaceId = null;
            shellPresentation.CurrentWorkspaceTitle = null;
            shellPresentation.StatusMessage = localization.Get(new("State.Empty"));
        }
    }

    private ResolvedTreeNode? NearestNavigableAncestor(string? nodeId, IEnumerable<ResolvedTreeNode> visibleNodes)
    {
        if (nodeId is null) return null;
        var visible = Flatten(visibleNodes).ToDictionary(x => x.Definition.NodeId, StringComparer.OrdinalIgnoreCase);
        var definitions = treeDefinition.Nodes.ToDictionary(x => x.NodeId, StringComparer.OrdinalIgnoreCase);
        while (definitions.TryGetValue(nodeId, out var node))
        {
            if (visible.TryGetValue(node.NodeId, out var resolved) && resolved.IsNavigable) return resolved;
            nodeId = node.ParentNodeId;
            if (nodeId is null) break;
        }
        return null;
    }

    private static bool TreeContainsWorkspace(IEnumerable<ResolvedTreeNode> nodes, string workspaceId) => nodes.Any(node =>
        node.IsNavigable && string.Equals(node.Definition.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase) ||
        TreeContainsWorkspace(node.Children, workspaceId));

    private static ResolvedTreeNode? FirstNavigable(IEnumerable<ResolvedTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.IsNavigable) return node;
            var nested = FirstNavigable(node.Children);
            if (nested is not null) return nested;
        }
        return null;
    }

    private static IEnumerable<ResolvedTreeNode> Flatten(IEnumerable<ResolvedTreeNode> nodes) => nodes.SelectMany(node =>
        new[] { node }.Concat(Flatten(node.Children)));

    private void RefreshFromRibbon()
    {
        if (workspaceHost.CurrentDefinition is { } workspace) workspaceHost.ShowWorkspace(workspace);
        shellPresentation.StatusMessage = localization.Get(new("Ribbon.RefreshComplete"));
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

    private sealed class DemoLayoutResetService : ILayoutResetService
    {
        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class DemoActionRefreshService(Action refresh) : IActionRefreshService
    {
        public Task<ActionCommandResult> RefreshAsync(ActionCommandExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            refresh();
            return Task.FromResult(ActionCommandResult.Success());
        }
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
                var result = workspaceHost.CurrentResult ?? workspaceHost.ShowWorkspace(workspaces[smokeStep]);
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
                var snapshot = companyScope.Snapshot;
                var currentWorkspace = workspaceHost.CurrentDefinition ?? workspaces[0];
                ribbonHost.UpdateContext(new RibbonResolutionContext(
                    snapshot.Company, currentWorkspace, currentWorkspace.TemplateCode,
                    snapshot.AuthorizationContext,
                    new RibbonSelectionContext(selectionSelector.SelectedItem is int count ? count : 0)));
                EnsureSmokeCompanySwitchPreserved(companyIndex);
                var resolved = AuthorizationPresentationResolver.Resolve(
                    new PresentationRequirement(new PermissionCode("DATA.EDIT"), null, UnauthorizedBehavior.ReadOnly),
                    snapshot.AuthorizationContext);
                var reportCommands = ribbonHost.ResolvedRibbon.Tabs.SelectMany(x => x.Groups)
                    .Where(x => x.Definition.GroupCode == "REPORT_TOOLS")
                    .SelectMany(x => x.Commands).ToArray();
                if (companyIndex == 2 && (reportCommands.Length == 0 || reportCommands.Any(x => x.IsEnabled)))
                    throw new InvalidOperationException("Unavailable Company authorization did not fail closed.");
                if (companyIndex != 2 && !reportCommands.Any(x => x.Definition.CommandCode == "EXPORT" && x.IsEnabled))
                    throw new InvalidOperationException("Company Ribbon authorization state was not refreshed.");
                Console.WriteLine($"SMOKE COMPANY: {snapshot.Company.Code} {snapshot.Status} {resolved} " +
                                  $"PROFILE={snapshot.ProfileResult!.Profile!.LegalName} " +
                                  $"PERMISSIONS={snapshot.AuthorizationContext!.PermissionCodes.Count} " +
                                  $"CAPABILITIES={snapshot.AuthorizationContext.CapabilityCodes.Count}");
                Console.WriteLine($"SMOKE RIBBON_COMPANY: {snapshot.Company.Code} " +
                                  (companyIndex == 2 ? "FAIL_CLOSED" : "EXPORT_ENABLED"));
            }
            else
            {
                smokeTimer!.Stop();
                smokeStage = "SEARCH";
                await RunSearchSmokeAsync();
                smokeStage = "RIBBON";
                await RunRibbonSmokeAsync();
                smokeStage = "ACTION_BARS";
                await RunActionBarSmokeAsync();
                smokeStage = "DATA_ENTRY";
                await RunDataEntrySmokeAsync();
                smokeStage = "CONTEXT";
                await RunContextSmokeAsync();
                smokeStage = "IMPORT_EXPORT";
                await RunImportExportSmokeAsync();
                smokeStage = "NOTIFICATIONS";
                await RunNotificationSmokeAsync();
                smokeStage = "SETUP";
                await RunSetupSmokeAsync();
                smokeStage = "PRIVACY";
                RunPrivacySmoke();
                smokeStage = "CLEAN_EXIT";
                Console.WriteLine("SMOKE CLEAN_EXIT: PASS");
                Close();
                return;
            }

            smokeStep++;
        }
        catch (Exception exception)
        {
            smokeTimer?.Stop();
            Console.Error.WriteLine($"SMOKE FAILURE: STAGE={smokeStage} STEP={smokeStep} " +
                                    $"{exception.GetType().FullName}: {exception.Message}");
            Console.Error.WriteLine(exception);
            Close();
        }
        finally
        {
            smokeAdvancing = false;
        }
    }

    private async Task RunSearchSmokeAsync()
    {
        var palette = shell.SearchContent ?? throw new InvalidOperationException("Global Search is not configured.");
        shell.IsSearchOpen = true;
        await palette.SetQueryAsync("report");
        if (!shell.IsSearchOpen || palette.CurrentResults.Count == 0 ||
            !palette.CurrentResults.Any(x => x.Result.ResultKind is SearchResultKind.Workspace or SearchResultKind.TreeNode))
            throw new InvalidOperationException("Global Search did not render navigation results.");
        var target = palette.CurrentResults.First(x => x.Result.WorkspaceId == "report-demo");
        var activation = await palette.ActivateAsync(target.Result.ResultId);
        if (activation.Status != SearchActivationStatus.Success || workspaceHost.CurrentDefinition?.WorkspaceId != "report-demo" ||
            treeHost.SelectedWorkspaceId != "report-demo")
            throw new InvalidOperationException("Search activation did not synchronize Workspace and Tree state.");
        await palette.SetQueryAsync("restricted");
        var sensitive = palette.CurrentResults.Single(x => x.Result.ResultId == "restricted-record");
        if (sensitive.Subtitle.Contains("123456789", StringComparison.Ordinal))
            throw new InvalidOperationException("Search leaked a restricted subtitle.");
        await palette.SetQueryAsync("Data Entry Demo");
        var recentActivation = await palette.ActivateAsync("data-entry-demo");
        if (recentActivation.Status != SearchActivationStatus.Success)
            throw new InvalidOperationException("Recent destination was not recorded from successful navigation.");
        await palette.SetQueryAsync("");
        if (!palette.CurrentResults.Any(x => x.Result.ResultKind == SearchResultKind.Pinned) ||
            !palette.CurrentResults.Any(x => x.Result.ResultKind == SearchResultKind.Favorite) ||
            !palette.CurrentResults.Any(x => x.Result.ResultKind == SearchResultKind.Recent))
            throw new InvalidOperationException("Empty-query Quick Access groups were not resolved.");
        treeHost.NavigationQuery = "standard report";
        if (!treeHost.VisibleNodeIds.Contains("standard-report") || !treeHost.VisibleNodeIds.Contains("dashboard"))
            throw new InvalidOperationException("Navigation Search did not retain matching descendant hierarchy.");
        treeHost.NavigationQuery = string.Empty;
        if (!treeHost.VisibleNodeIds.Contains("safe-unknown"))
            throw new InvalidOperationException("Clearing Navigation Search did not restore the tree.");
        await workspaceNavigation.NavigateAsync("report-demo");
        shell.IsSearchOpen = false;
        Console.WriteLine("SMOKE SEARCH_PALETTE: PASS WORKSPACE_TREE_COMMAND_SETTING_RECORD PRIVACY FAILURE_ISOLATED");
        Console.WriteLine("SMOKE SEARCH_QUICK_ACCESS: PASS PINNED FAVORITES RECENT");
        Console.WriteLine("SMOKE NAVIGATION_SEARCH: PASS NESTED SEE_MORE RESTORE");
    }

    private void RunPrivacySmoke()
    {
        var generatedBeforePrivacyToggle = dataEntryProvider.GeneratedRowCount;
        var resolver = new PrivacyPolicyResolver();
        var presenter = new SensitiveValuePresenter();
        var confidential = new SensitiveContentDefinition(Sensitivity.Confidential, PrivacyPresentation.PartialMask,
            AllowTemporaryReveal: true, TemporaryRevealDuration: TimeSpan.FromSeconds(2),
            PartialMask: new(0, 4, "•••• "));
        var restricted = new SensitiveContentDefinition(Sensitivity.Restricted, PrivacyPresentation.CaptureProtect,
            PrivacyPresentation.Mask);
        foreach (var mode in Enum.GetValues<PrivacyMode>())
        {
            privacyState.SetRequestedMode(mode);
            var restrictedResult = resolver.Resolve(new(true, restricted, mode,
                CaptureCapability: CaptureProtectionCapability.Unsupported));
            if (restrictedResult.Presentation != PrivacyPresentation.Mask || !restrictedResult.FallbackApplied)
                throw new InvalidOperationException($"Restricted fallback failed in {mode}.");
            Console.WriteLine($"SMOKE PRIVACY_MODE: REQUESTED={mode} EFFECTIVE={restrictedResult.EffectivePrivacyMode} RESTRICTED=MASK");
        }
        privacyState.SetRequestedMode(PrivacyMode.On);
        var before = resolver.Resolve(new(true, confidential, privacyState.RequestedMode));
        if (presenter.Present("CONTACT-12345678", confidential, before).DisplayValue.Contains("12345678", StringComparison.Ordinal))
            throw new InvalidOperationException("Confidential value was not masked.");
        var generation = privacyState.Generation;
        if (!privacyState.BeginReveal(new("CONTACT_REFERENCE", RevealScope.Field, TimeSpan.FromSeconds(2), generation)))
            throw new InvalidOperationException("Temporary reveal did not start.");
        var revealed = resolver.Resolve(new(true, confidential, privacyState.RequestedMode,
            IsTemporarilyRevealed: privacyState.IsRevealed("CONTACT_REFERENCE", generation)));
        if (revealed.Presentation != PrivacyPresentation.None || revealed.CanCopy || revealed.CanExport)
            throw new InvalidOperationException("Reveal/copy/export separation failed.");
        privacyState.RevokeReveal();
        if (privacyState.IsRevealed("CONTACT_REFERENCE", generation))
            throw new InvalidOperationException("Temporary reveal did not revoke.");
        var oldGeneration = privacyState.Generation;
        privacyState.InvalidateContext("SMOKE_COMPANY", "SMOKE_WORKSPACE");
        if (oldGeneration == privacyState.Generation)
            throw new InvalidOperationException("Privacy context generation did not advance.");
        if (!dataEntryGridHost.Runtime.IsVirtualized ||
            dataEntryGridHost.Runtime.Rows.Length > dataEntryGridHost.Runtime.ViewportOptions.MaximumMaterializedRows ||
            dataEntryProvider.GeneratedRowCount != generatedBeforePrivacyToggle)
            throw new InvalidOperationException("Privacy changed 100K Grid virtualization bounds.");
        Console.WriteLine("SMOKE PRIVACY_MENU: VISIBLE");
        Console.WriteLine("SMOKE PRIVACY_REVEAL_REVOKE: PASS COPY_EXPORT_INDEPENDENT");
        Console.WriteLine("SMOKE PRIVACY_GRID_FORM_NOTIFICATION_IMPORT_EXPORT_ACCESSIBILITY: SAFE_SHARED_RESOLVER");
        Console.WriteLine("SMOKE PRIVACY_CAPTURE: UNSUPPORTED SAFE_FALLBACK_MASK");
        Console.WriteLine("SMOKE PRIVACY_CONTEXT: COMPANY_WORKSPACE_GENERATION_SAFE");
    }

    private async Task RunRibbonSmokeAsync()
    {
        if (ribbonHost.Content?.GetType().FullName != "ActiproSoftware.UI.Avalonia.Controls.Bars.Ribbon" ||
            ribbonHost.ResolvedRibbon.Tabs.Length < 3)
            throw new InvalidOperationException("Real metadata-driven Ribbon was not rendered.");
        Console.WriteLine($"SMOKE RIBBON_CONTROL: PASS TABS={ribbonHost.ResolvedRibbon.Tabs.Length}");

        foreach (var code in new[] { "REFRESH", "HELLO" })
        {
            var result = await ribbonHost.ExecuteCommandAsync(code);
            if (result.Status != RibbonCommandResultStatus.Success)
                throw new InvalidOperationException($"Ribbon command {code} failed: {result.Status}");
            Console.WriteLine($"SMOKE RIBBON_{code}: PASS");
        }
        var unknown = await ribbonHost.ExecuteCommandAsync("UNKNOWN_SAFE");
        if (unknown.Status != RibbonCommandResultStatus.Unavailable)
            throw new InvalidOperationException("Unknown registered command did not fail safely.");
        Console.WriteLine("SMOKE RIBBON_UNKNOWN: SAFE_FAILURE");

        selectionSelector.SelectedItem = 0;
        RefreshRibbon();
        if (RibbonCommand("SELECTION_ACTION").IsEnabled)
            throw new InvalidOperationException("Selection command must be disabled at zero selection.");
        selectionSelector.SelectedItem = 1;
        RefreshRibbon();
        if (!RibbonCommand("SELECTION_ACTION").IsEnabled)
            throw new InvalidOperationException("Selection command must be enabled with a selection.");
        Console.WriteLine("SMOKE RIBBON_SELECTION: PASS");

        workspaceSelector.SelectedIndex = 1;
        if (ribbonHost.ResolvedRibbon.Tabs.SelectMany(x => x.Groups)
            .Any(x => x.Definition.GroupCode == "REPORT_TOOLS"))
            throw new InvalidOperationException("Report contextual group leaked outside report context.");
        workspaceSelector.SelectedIndex = 2;
        if (!ribbonHost.ResolvedRibbon.Tabs.SelectMany(x => x.Groups)
            .Any(x => x.Definition.GroupCode == "REPORT_TOOLS"))
            throw new InvalidOperationException("Report contextual group did not appear.");
        Console.WriteLine("SMOKE RIBBON_CONTEXT: PASS");

        var navigation = await ribbonHost.ExecuteCommandAsync("OPEN_REPORT");
        if (navigation.Status != RibbonCommandResultStatus.Success || workspaceHost.CurrentDefinition?.WorkspaceId != "report-demo")
            throw new InvalidOperationException("Ribbon navigation failed.");
        Console.WriteLine("SMOKE RIBBON_NAVIGATE: PASS");
    }

    private ResolvedRibbonCommand RibbonCommand(string commandCode) => ribbonHost.ResolvedRibbon.Tabs
        .SelectMany(x => x.Groups).SelectMany(x => x.Commands)
        .Single(x => x.Definition.CommandCode == commandCode);

    private async Task RunActionBarSmokeAsync()
    {
        stateSelector.SelectedIndex = 0;
        workspaceSelector.SelectedIndex = visibleWorkspaces.ToList().FindIndex(x => x.WorkspaceId == "dashboard-demo");
        RefreshActionBars();
        if (!topActionBar.IsVisible || !bottomActionBar.IsVisible ||
            topActionBar.ResolvedActionBar?.Definition.Position != ActionBarPosition.Top ||
            bottomActionBar.ResolvedActionBar?.Definition.Position != ActionBarPosition.Bottom)
            throw new InvalidOperationException("Top and Bottom Action Bars were not rendered.");
        Console.WriteLine("SMOKE ACTION_BARS: TOP_BOTTOM_RENDERED");

        var refresh = await topActionBar.ExecuteActionAsync("REFRESH");
        var custom = await bottomActionBar.ExecuteActionAsync("CUSTOM");
        if (refresh.Status != ActionCommandResultStatus.Success || custom.Status != ActionCommandResultStatus.Success || actionRefreshCount == 0)
            throw new InvalidOperationException("Action Bar refresh/custom dispatch failed.");
        Console.WriteLine("SMOKE ACTION_REFRESH_CUSTOM: PASS");

        var navigate = await bottomActionBar.ExecuteActionAsync("OPEN_DATA");
        if (navigate.Status != ActionCommandResultStatus.Success || workspaceHost.CurrentDefinition?.WorkspaceId != "data-entry-demo")
            throw new InvalidOperationException("Action Bar navigation failed.");
        await LoadDataEntryAsync(companyContext.CurrentCompany, companyScope.Snapshot.AuthorizationContext);
        Console.WriteLine("SMOKE ACTION_NAVIGATE: PASS");

        dataEntryGridHost.Runtime.Select([]);
        RefreshActionBars();
        if (ActionBarAction(topActionBar, "EDIT").IsEnabled)
            throw new InvalidOperationException("Selection action must be disabled at zero selection.");
        dataEntryGridHost.Runtime.Select([dataEntryGridHost.Runtime.Rows[0].RowKey]);
        RefreshActionBars();
        if (!ActionBarAction(topActionBar, "EDIT").IsEnabled || bottomActionBar.ResolvedActionBar?.Status?.SelectedRows != 1)
            throw new InvalidOperationException("Selection state or Bottom Action Bar status was not refreshed.");
        Console.WriteLine("SMOKE ACTION_SELECTION_STATUS: PASS");

        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        await LoadDataEntryAsync(companyContext.CurrentCompany, companyScope.Snapshot.AuthorizationContext);
        dataEntryGridHost.Runtime.Select([dataEntryGridHost.Runtime.Rows[0].RowKey]);
        RefreshActionBars();
        if (!ActionBarAction(topActionBar, "EDIT").IsEnabled)
            throw new InvalidOperationException("Company A Action Bar permission was not enabled.");
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyBId);
        await LoadDataEntryAsync(companyContext.CurrentCompany, companyScope.Snapshot.AuthorizationContext);
        dataEntryGridHost.Runtime.Select([dataEntryGridHost.Runtime.Rows[0].RowKey]);
        RefreshActionBars();
        if (ActionBarAction(topActionBar, "EDIT").IsEnabled)
            throw new InvalidOperationException("Company B Action Bar permission was not disabled.");
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyCId);
        await LoadDataEntryAsync(companyContext.CurrentCompany, companyScope.Snapshot.AuthorizationContext);
        dataEntryGridHost.Runtime.Select([dataEntryGridHost.Runtime.Rows[0].RowKey]);
        RefreshActionBars();
        if (ActionBarAction(topActionBar, "EDIT").IsEnabled)
            throw new InvalidOperationException("Unavailable Company authorization did not fail closed for Action Bars.");
        Console.WriteLine("SMOKE ACTION_COMPANY_RERESOLUTION: A=ENABLED B=DISABLED C=FAIL_CLOSED");

        workspaceSelector.SelectedIndex = visibleWorkspaces.ToList().FindIndex(x => x.WorkspaceId == "signing-demo");
        RefreshActionBars();
        var unknown = await bottomActionBar.ExecuteActionAsync("UNKNOWN_COMMAND");
        if (unknown.Status != ActionCommandResultStatus.Unavailable ||
            !iconRegistry.Resolve(new IconKey("UNKNOWN_ACTION_ICON")).IsFallback)
            throw new InvalidOperationException("Unknown Action Bar command/icon did not fail safely.");
        Console.WriteLine("SMOKE ACTION_UNKNOWN_COMMAND_ICON: SAFE_FAILURE");

        var malformed = new ActionBarDefinition("malformed", "malformed", ActionBarPosition.Top,
        [
            new("missing", "MISSING_TARGET", new("ActionBar.Unknown"), StandardIconKeys.Info, ActionType.Navigate),
            new("unknown", "UNKNOWN_TARGET", new("ActionBar.Unknown"), StandardIconKeys.Info, ActionType.Navigate,
                targetWorkspaceId: "not-a-workspace"),
        ]);
        var malformedResult = actionBarResolver.Resolve(malformed,
            CreateActionBarContext(workspaceHost.CurrentDefinition!), workspaces);
        if (!malformedResult.Actions.IsEmpty || malformedResult.Diagnostics.Length != 2)
            throw new InvalidOperationException("Malformed Action Bar metadata was not contained safely.");
        Console.WriteLine("SMOKE ACTION_UNKNOWN_TARGET: SAFE_FAILURE");
    }

    private static ResolvedAction ActionBarAction(DynamicActionBarHost host, string actionCode) =>
        host.ResolvedActionBar!.Actions.Single(x => x.Definition.ActionCode == actionCode);

    private async Task RunDataEntrySmokeAsync()
    {
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        workspaceSelector.SelectedIndex = visibleWorkspaces.ToList().FindIndex(x => x.WorkspaceId == "data-entry-demo");
        await LoadDataEntryAsync(companyContext.CurrentCompany, companyScope.Snapshot.AuthorizationContext);
        var runtime = dataEntryGridHost.Runtime;
        dataEntryTabs.SelectedIndex = 0;
        if (workspaceHost.Content != dataEntryTabs || dataEntryGridHost.RenderedColumnCount < 10 ||
            dataEntryGridHost.RenderedRowCount < 20 || runtime.ResolvedDefinition.Columns.Any(x =>
                x.Definition.VariableCode == new VariableCode("PRIVILEGED_NOTE") && x.IsVisible))
            throw new InvalidOperationException("Dynamic DataEntry columns, rows, or permission-hidden values failed.");
        if (!runtime.IsVirtualized || runtime.TotalRows != DemoDataEntryProvider.LogicalRowCount ||
            runtime.Rows.Length > runtime.ViewportOptions.MaximumMaterializedRows)
            throw new InvalidOperationException("100K logical count or bounded initial viewport failed.");
        Console.WriteLine($"SMOKE GRID_RENDER: PASS COLUMNS={dataEntryGridHost.RenderedColumnCount} ROWS={dataEntryGridHost.RenderedRowCount}");
        Console.WriteLine($"SMOKE GRID_100K_INITIAL: TOTAL={runtime.TotalRows} MATERIALIZED={runtime.Rows.Length} PASS");

        var first = runtime.Rows[0]; var second = runtime.Rows[1];
        var layoutRows = runtime.Rows.Length;
        var code = new VariableCode("ITEM_CODE");
        runtime.SelectCell(new(first.RowKey, code), runtime.ViewportStartIndex);
        if (!runtime.SetColumnWidthPercentage(code, 150) || !dataEntryGridHost.ReorderColumn(code, 2) ||
            runtime.ActiveCell != new GridCellAddress(first.RowKey, code) || runtime.Rows.Length != layoutRows)
            throw new InvalidOperationException("Resize/reorder changed semantic active cell or rematerialized rows.");
        if (!dataEntryGridHost.SetColumnPinned(code, true) || runtime.PresentedColumns[0].VariableCode != code ||
            !dataEntryGridHost.SetColumnPinned(code, false))
            throw new InvalidOperationException("Pin/unpin failed.");
        if (!dataEntryGridHost.SetColumnVisible(code, false) || runtime.ActiveCell?.VariableCode == code ||
            !dataEntryGridHost.SetColumnVisible(code, true))
            throw new InvalidOperationException("Hide/show or active-cell relocation failed.");
        var preferenceStore = new InMemoryGridViewPreferenceStore();
        var preferenceScope = new GridPreferenceScope(GridPreferenceScopeKind.UserGrid, "demo-user", runtime.Definition.GridCode);
        await runtime.SaveViewAsync(preferenceStore, preferenceScope);
        dataEntryGridHost.ResetLayout();
        await runtime.RestoreViewAsync(preferenceStore, preferenceScope);
        if (runtime.GetColumnWidthPercentage(code) != 150 || runtime.Rows.Length != layoutRows)
            throw new InvalidOperationException("Preference restore/reset changed rows or lost width.");
        dataEntryGridHost.ResetLayout();
        runtime.SelectAllCells();
        if (!runtime.CellSelection.IsAllSelected || runtime.SelectedRanges.Length != 0 || runtime.Rows.Length != layoutRows)
            throw new InvalidOperationException("Semantic select-all was not bounded.");
        runtime.ClearCellSelection();
        Console.WriteLine("SMOKE GRID_10E_LAYOUT: RESIZE REORDER HIDE SHOW PIN UNPIN RESET RESTORE SELECT_ALL_BOUNDED PASS");

        runtime.Select([first.RowKey]); RefreshActionBars();
        if (!ActionBarAction(topActionBar, "EDIT").IsEnabled || runtime.SelectionCount != 1)
            throw new InvalidOperationException("Single-row selection did not feed the Action Bar.");
        runtime.Select([first.RowKey, second.RowKey]); RefreshActionBars();
        if (ActionBarAction(topActionBar, "EDIT").IsEnabled || runtime.SelectionCount != 2)
            throw new InvalidOperationException("Multi-row selection did not feed the Action Bar.");
        Console.WriteLine("SMOKE GRID_SELECTION_ACTION_STATUS: SINGLE MULTIPLE ROWKEY PASS");

        var quantity = new VariableCode("QUANTITY");
        if (!dataEntryGridHost.BeginEdit(first.RowKey, quantity) || dataEntryGridHost.SetCandidate("invalid")?.Code != "GRID_VALUE_TYPE_INVALID")
            throw new InvalidOperationException("Invalid integer candidate was accepted.");
        dataEntryGridHost.SetCandidate("42");
        if (!(await dataEntryGridHost.CommitEditAsync()).IsSuccess || !Equals(runtime.GetValue(first.RowKey, quantity, out _), 42))
            throw new InvalidOperationException("Single-cell commit failed.");
        var name = new VariableCode("ITEM_NAME"); var originalName = runtime.GetValue(first.RowKey, name, out _);
        dataEntryGridHost.BeginEdit(first.RowKey, name); dataEntryGridHost.SetCandidate("Cancelled candidate"); dataEntryGridHost.CancelEdit();
        if (!Equals(runtime.GetValue(first.RowKey, name, out _), originalName) ||
            dataEntryGridHost.BeginEdit(first.RowKey, new("TOTAL")) || dataEntryGridHost.BeginEdit(first.RowKey, new("UPDATED_AT")))
            throw new InvalidOperationException("Cancel or formula/system read-only behavior failed.");
        Console.WriteLine("SMOKE GRID_EDIT: INPUT_INVALID_COMMIT_CANCEL FORMULA_SYSTEM_READONLY PASS");

        var visible = runtime.PresentedColumns.Select(x => x.Column).ToArray();
        var nameColumn = Array.FindIndex(visible, x => x.Definition.VariableCode == name);
        var quantityColumn = Array.FindIndex(visible, x => x.Definition.VariableCode == quantity);
        runtime.SelectCell(new(first.RowKey, name), runtime.ViewportStartIndex);
        runtime.SelectCell(new(second.RowKey, quantity), runtime.ViewportStartIndex + 1, extend: true);
        var nativeClipboard = TopLevel.GetTopLevel(dataEntryGridHost)?.Clipboard ??
            throw new InvalidOperationException("Native clipboard unavailable during GUI smoke.");
        if ((await dataEntryGridHost.CopySelectionAsync()).DiagnosticCode is not null)
            throw new InvalidOperationException("2x3 native clipboard copy failed.");
        var copied = await nativeClipboard.GetTextAsync();
        if (copied?.Split('\n').Length != 2 || copied.Split('\n')[0].Split('\t').Length != quantityColumn - nameColumn + 1)
            throw new InvalidOperationException("Native clipboard rectangle shape failed.");
        var third = runtime.Rows[2]; var fourth = runtime.Rows[3];
        runtime.SelectCell(new(third.RowKey, name), runtime.ViewportStartIndex + 2);
        runtime.SelectCell(new(fourth.RowKey, quantity), runtime.ViewportStartIndex + 3, extend: true);
        var expectedClipboardCells = 2 * (Math.Abs(quantityColumn - nameColumn) + 1);
        if ((await dataEntryGridHost.PasteSelectionAsync()).AppliedCellCount != expectedClipboardCells ||
            !Equals(runtime.GetValue(third.RowKey, name, out _), originalName))
            throw new InvalidOperationException("2x3 native clipboard paste failed.");

        var notes = new VariableCode("PUBLIC_NOTE");
        await nativeClipboard.SetTextAsync("filled note");
        runtime.SelectCell(new(first.RowKey, notes), runtime.ViewportStartIndex);
        runtime.SelectCell(new(third.RowKey, notes), runtime.ViewportStartIndex + 2, extend: true);
        if ((await dataEntryGridHost.PasteSelectionAsync()).AppliedCellCount != 3 ||
            (await dataEntryGridHost.FillDownAsync()).AppliedCellCount != 2 ||
            (await dataEntryGridHost.UndoAsync()).AppliedCellCount != 2 ||
            (await dataEntryGridHost.RedoAsync()).AppliedCellCount != 2)
            throw new InvalidOperationException("Single value range fill failed.");
        runtime.SelectCell(new(first.RowKey, quantity), runtime.ViewportStartIndex);
        await nativeClipboard.SetTextAsync("not-an-integer");
        if ((await dataEntryGridHost.PasteSelectionAsync()).DiagnosticCode != "GRID_PASTE_ATOMIC_REJECTED")
            throw new InvalidOperationException("Invalid paste was not diagnosed.");
        runtime.SelectCell(new(first.RowKey, new("TOTAL")), runtime.ViewportStartIndex);
        await nativeClipboard.SetTextAsync("1");
        if ((await dataEntryGridHost.PasteSelectionAsync()).DiagnosticCode != "GRID_PASTE_ATOMIC_REJECTED")
            throw new InvalidOperationException("Formula paste protection failed.");
        runtime.SelectCell(new(first.RowKey, notes), runtime.ViewportStartIndex);
        if ((await dataEntryGridHost.CutSelectionAsync()).AppliedCellCount != 1 || runtime.GetValue(first.RowKey, notes, out _) is not null)
            throw new InvalidOperationException("Cut failed.");
        if ((await dataEntryGridHost.UndoAsync()).AppliedCellCount != 1 ||
            (await dataEntryGridHost.RedoAsync()).AppliedCellCount != 1)
            throw new InvalidOperationException("Undo/redo failed.");
        runtime.SelectCell(new(second.RowKey, notes), runtime.ViewportStartIndex + 1);
        if ((await dataEntryGridHost.ClearSelectionAsync()).AppliedCellCount != 1 ||
            (await dataEntryGridHost.UndoAsync()).AppliedCellCount != 1 ||
            (await dataEntryGridHost.RedoAsync()).AppliedCellCount != 1)
            throw new InvalidOperationException("Clear undo/redo failed.");
        if ((await runtime.CopyAsync(new UnavailableGridClipboard())).DiagnosticCode != "GRID_CLIPBOARD_UNAVAILABLE")
            throw new InvalidOperationException("Unavailable clipboard did not fail safely.");
        Console.WriteLine("SMOKE GRID_CLIPBOARD_EDITING: ACTIVE SHIFT 2X3 COPY_PASTE FILL INVALID READONLY CUT CLEAR UNDO REDO PASS");

        runtime.SelectCell(new(first.RowKey, name), runtime.ViewportStartIndex);
        await dataEntryGridHost.RequestViewportAsync(60);
        var across = runtime.Rows.Single(x => x.RowKey.Value.EndsWith(":ROW:000062", StringComparison.Ordinal));
        runtime.SelectCell(new(across.RowKey, name), 61, extend: true);
        await nativeClipboard.SetTextAsync("cross-window");
        if ((await dataEntryGridHost.PasteSelectionAsync()).AppliedCellCount != 62 || runtime.SelectedRanges.Length != 1)
            throw new InvalidOperationException("Cross-window paste or compact range failed.");
        await dataEntryGridHost.RequestViewportAsync(0);
        if (!Equals(runtime.GetValue(first.RowKey, name, out _), "cross-window") || runtime.SelectedRanges.Length != 1)
            throw new InvalidOperationException("Cross-window persisted value or selection scroll survival failed.");
        runtime.SelectCell(new(first.RowKey, name), 0);
        runtime.SelectCell(new(new($"{companyContext.CurrentCompany.CompanyId.Value}:ROW:010001"), name), 10_000, extend: true);
        if (!(await runtime.PasteTextAsync("large")).RequiresConfirmation)
            throw new InvalidOperationException("Large paste confirmation guard failed.");
        Console.WriteLine("SMOKE GRID_VIRTUAL_RANGE: CROSS_WINDOW SELECTION_SCROLL LARGE_GUARD 100K_BOUNDED PASS");

        runtime.Select([first.RowKey]);
        dataEntryGridHost.BeginEdit(first.RowKey, name); dataEntryGridHost.SetCandidate("Viewport draft");
        await dataEntryGridHost.RequestViewportAsync(90_000);
        if (!runtime.SelectedRowKeys.Contains(first.RowKey) || runtime.EditBuffer?.CandidateValue?.ToString() != "Viewport draft" ||
            runtime.RequestedViewportStartIndex < 90_000 ||
            runtime.RequestedViewportStartIndex > 90_000 + runtime.RequestedViewportRowCount ||
            runtime.Rows.Length > runtime.ViewportOptions.MaximumMaterializedRows)
            throw new InvalidOperationException($"Far jump failed: selected={runtime.SelectedRowKeys.Contains(first.RowKey)} " +
                $"edit={runtime.EditBuffer?.CandidateValue} start={runtime.RequestedViewportStartIndex} rows={runtime.Rows.Length}.");
        await dataEntryGridHost.RequestViewportAsync(0);
        if (!runtime.Rows.Any(x => x.RowKey == first.RowKey) || runtime.CachedWindowCount > runtime.ViewportOptions.MaximumCachedWindows)
            throw new InvalidOperationException("Return navigation or bounded cache failed.");
        dataEntryGridHost.CancelEdit();
        Console.WriteLine($"SMOKE GRID_FAR_JUMP_EDIT_CACHE: ROW=90001 MATERIALIZED={runtime.Rows.Length} CACHE={runtime.CachedWindowCount} PASS");

        smokeStage = "DATA_ENTRY_SORT";
        var selectedKey = first.RowKey; runtime.Select([selectedKey]);
        var sortGeneration = runtime.Generation;
        await dataEntryGridHost.SortAsync(new("ITEM_CODE"), GridSortDirection.Descending);
        await dataEntryGridHost.RequestViewportAsync(0);
        if (runtime.Generation <= sortGeneration || runtime.State != GridProviderState.Ready || runtime.Rows.IsEmpty ||
            runtime.Sorts is not [{ VariableCode.Value: "ITEM_CODE", Direction: GridSortDirection.Descending }] ||
            !runtime.SelectedRowKeys.Contains(selectedKey) || runtime.Rows[0].RowKey == selectedKey)
            throw new InvalidOperationException($"Sort did not preserve RowKey selection: selected={runtime.SelectedRowKeys.Contains(selectedKey)} " +
                $"selectedKey={selectedKey} first={runtime.Rows.FirstOrDefault()?.RowKey.ToString() ?? "NONE"} " +
                $"state={runtime.State} generation={runtime.Generation}/{sortGeneration} sort={runtime.Sorts.FirstOrDefault()?.Direction}.");
        smokeStage = "DATA_ENTRY_FILTER";
        var filterGeneration = runtime.Generation;
        await dataEntryGridHost.FilterAsync(new(new("ITEM_CODE"), GridFilterOperator.Contains, "-00"));
        if (runtime.Generation <= filterGeneration || runtime.State != GridProviderState.Ready || runtime.Filters.Length != 1 ||
            runtime.VisibleRows is <= 0 or >= DemoDataEntryProvider.LogicalRowCount || !runtime.SelectedRowKeys.Contains(selectedKey))
            throw new InvalidOperationException($"Filter/count/selection behavior failed: state={runtime.State} " +
                $"generation={runtime.Generation}/{filterGeneration} filters={runtime.Filters.Length} visible={runtime.VisibleRows} " +
                $"selected={runtime.SelectedRowKeys.Contains(selectedKey)}.");
        smokeStage = "DATA_ENTRY_CLEAR_FILTER";
        var clearGeneration = runtime.Generation;
        await dataEntryGridHost.ClearFilterAsync();
        if (runtime.Generation <= clearGeneration || runtime.State != GridProviderState.Ready || !runtime.Filters.IsEmpty ||
            runtime.VisibleRows != DemoDataEntryProvider.LogicalRowCount)
            throw new InvalidOperationException($"Clear filter completion failed: state={runtime.State} " +
                $"generation={runtime.Generation}/{clearGeneration} filters={runtime.Filters.Length} visible={runtime.VisibleRows}.");
        Console.WriteLine("SMOKE GRID_SORT_FILTER_STATUS: ASC_DESC_CONTAINS_CLEAR PASS");

        await dataEntryGridHost.FilterAsync(new(new("QUANTITY"), GridFilterOperator.GreaterThan, 199_990));
        if (runtime.VisibleRows != 5) throw new InvalidOperationException("Numeric filter failed.");
        await dataEntryGridHost.FilterAsync(new(new("START_DATE"), GridFilterOperator.After, new DateOnly(2035, 1, 1)));
        if (runtime.VisibleRows <= 0 || runtime.VisibleRows >= DemoDataEntryProvider.LogicalRowCount)
            throw new InvalidOperationException("Date filter failed.");
        await dataEntryGridHost.FilterAsync(new(new("ITEM_CODE"), GridFilterOperator.Equals, "NO-MATCH"));
        await dataEntryGridHost.RequestViewportAsync(0);
        if (runtime.State != GridProviderState.Empty || runtime.VisibleRows != 0)
            throw new InvalidOperationException($"Filtered empty state failed: state={runtime.State} visible={runtime.VisibleRows} diagnostic={runtime.DiagnosticCode}.");
        await dataEntryGridHost.ClearFilterAsync();
        Console.WriteLine("SMOKE GRID_10E_FILTER: TEXT NUMBER DATE FILTERED_EMPTY CLEAR PASS");

        smokeStage = "DATA_ENTRY_PRESENTATION";
        localization.TrySetCulture("vi-VN"); themeSelector.SelectedItem = ThemeMode.System;
        localization.TrySetCulture("en-US"); themeSelector.SelectedItem = ThemeMode.Light; themeSelector.SelectedItem = ThemeMode.Dark;
        if (!runtime.SelectedRowKeys.Contains(selectedKey)) throw new InvalidOperationException("Presentation changes lost grid selection.");
        Console.WriteLine("SMOKE GRID_LOCALIZATION_THEME_KEYBOARD_ACCESSIBILITY: VI_EN_SYSTEM_LIGHT_DARK PASS");

        smokeStage = "DATA_ENTRY_PROVIDER_RECOVERY";
        var requestsBeforeRefresh = dataEntryProvider.ViewportRequestCount;
        dataEntryProvider.SimulateFailure = true; await dataEntryGridHost.RefreshAsync();
        if (runtime.State != GridProviderState.Error || runtime.DiagnosticCode != "GRID_PROVIDER_FAILED")
            throw new InvalidOperationException("Provider failure was not isolated.");
        dataEntryProvider.SimulateFailure = false; await dataEntryGridHost.RefreshAsync();
        if (dataEntryProvider.ViewportRequestCount != requestsBeforeRefresh + 2 || runtime.Rows.Length > runtime.ViewportOptions.MaximumMaterializedRows)
            throw new InvalidOperationException("Refresh did not remain scoped to the current viewport.");
        Console.WriteLine("SMOKE GRID_PROVIDER_ERROR: SAFE_FAILURE_RECOVERY PASS");

        smokeStage = "DATA_ENTRY_STALE_COMPANY";
        var companyA = companyContext.AvailableCompanies.Single(x => x.CompanyId == DemoCompanyData.CompanyAId);
        var companyB = companyContext.AvailableCompanies.Single(x => x.CompanyId == DemoCompanyData.CompanyBId);
        var staleA = dataEntryGridHost.LoadAsync(new(companyA, "data-entry-demo"), companyScope.Snapshot.AuthorizationContext);
        var currentB = dataEntryGridHost.LoadAsync(new(companyB, "data-entry-demo"), companyScope.Snapshot.AuthorizationContext);
        await Task.WhenAll(staleA, currentB);
        if (runtime.Rows.Any(x => !x.RowKey.Value.StartsWith($"{companyB.CompanyId.Value}:", StringComparison.Ordinal)))
            throw new InvalidOperationException("Stale Company A response replaced Company B rows.");
        Console.WriteLine("SMOKE GRID_COMPANY_STALE_RESPONSE: A_TO_B_BLOCKED PASS");
    }

    private async Task RunContextSmokeAsync()
    {
        workspaceSelector.SelectedIndex = visibleWorkspaces.ToList().FindIndex(x => x.WorkspaceId == "data-entry-demo");
        await dataEntryGridHost.RequestViewportAsync(90_000);
        var runtime = dataEntryGridHost.Runtime;
        var first = runtime.Rows[0];
        runtime.Select([first.RowKey]);
        await RefreshContextAsync();
        if (contextCoordinator.Current?.ContextKey.Contains(first.RowKey.Value, StringComparison.Ordinal) != true ||
            runtime.Rows.Length > runtime.ViewportOptions.MaximumMaterializedRows)
            throw new InvalidOperationException("Context did not resolve the 90K selection by bounded RowKey.");
        var savedWidth = shell.ResizeContextPanel(410);
        shell.IsContextPanelOpen = false; shell.IsContextPanelOpen = true;
        if (Math.Abs(shell.SplitLayout.Context.NavigationWidth - savedWidth) > .1)
            throw new InvalidOperationException("Context width was not preserved across close/reopen.");
        var breadcrumbPath = breadcrumbHost.Path;
        if (breadcrumbPath is null || breadcrumbPath.Items.Length < 3 || !breadcrumbPath.Items[^1].IsCurrent)
            throw new InvalidOperationException("Breadcrumb current path was not synchronized.");
        var help = await new DemoHelpProvider().GetHelpAsync(new(new("DATAENTRY.ROW"), localization.CurrentCulture,
            companyContext.CurrentCompany.CompanyId, "data-entry-demo", companyScope.Snapshot.AuthorizationContext,
            privacyState.RequestedMode, 1, CancellationToken.None));
        if (help is null) throw new InvalidOperationException("Local contextual help did not resolve.");
        Console.WriteLine($"SMOKE CONTEXT_S2: ROWKEY={first.RowKey} MATERIALIZED={runtime.Rows.Length} WIDTH={savedWidth} BREADCRUMB=PASS HELP=LOCAL PASS");
        await dataEntryGridHost.RequestViewportAsync(0);
    }

    private async Task RunImportExportSmokeAsync()
    {
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        workspaceSelector.SelectedIndex = visibleWorkspaces.ToList().FindIndex(x => x.WorkspaceId == "data-entry-demo");
        dataEntryTabs.SelectedIndex = 1;
        var registry = DemoImportExport.CreateRegistry();
        var parserCodes = registry.Parsers.Select(x => x.ParserCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var code in new[] { ImportParserCodes.Xlsx, ImportParserCodes.Csv, ImportParserCodes.Tsv, ImportParserCodes.Json,
                     ImportParserCodes.Xml, ImportParserCodes.FixedWidth, DemoImportExport.CustomCode })
            if (!parserCodes.Contains(code)) throw new InvalidOperationException($"Import parser {code} is missing from the generic host.");
        Console.WriteLine("SMOKE IMPORT_PROFILES: XLSX CSV TSV JSON XML FIXED_WIDTH CUSTOM_DEMO VISIBLE");

        var definition = DemoImportExport.ImportProfiles.Single(x => x.ParserCode == DemoImportExport.CustomCode);
        var text = "@record\ncode=NEW-001\nname=Imported sample\nquantity=12\n@end\n" +
                   "@record\ncode=NEW-002\nname=Invalid sample\nquantity=bad\n@end\n";
        var bytes = Encoding.UTF8.GetBytes(text); var engine = new ImportEngine(registry);
        await using var inspectStream = new MemoryStream(bytes);
        var schema = await engine.InspectAsync(inspectStream, definition, "demo.custom");
        var resolved = GridMetadataResolver.Resolve(DemoDataEntry.CreateDefinition(), companyScope.Snapshot.AuthorizationContext);
        var auto = ImportAutoMapper.Map(schema, resolved.Columns);
        if (schema.Fields.Length != 3 || auto.Mappings.Length != 3) throw new InvalidOperationException("Custom schema inspection/auto-map failed.");
        var manual = definition.Mappings.Select(x => x.SourceField == "name" ? new ImportFieldMapping(x.MappingId, x.SourceField,
            new VariableCode("NOTES"), x.DisplayOrder, x.SourceIndex, x.Required, x.DataTypeOverride, x.ConverterCode) : x).ToArray();
        if (!manual.Any(x => x.TargetVariableCode == new VariableCode("NOTES"))) throw new InvalidOperationException("Manual mapping change failed.");
        Console.WriteLine("SMOKE IMPORT_SCHEMA_AUTOMAP_MANUAL: PASS");

        await using var previewStream = new MemoryStream(bytes);
        var preview = await engine.PreviewAsync(previewStream, definition, resolved,
            new Progress<ImportExportProgress>(importExportHost.ReportProgress));
        importExportHost.ShowSource("demo.custom", definition, schema, definition.Mappings,
            resolved.Columns.Where(x => x.CanEdit).Select(x => x.Definition.VariableCode.Value));
        importExportHost.ShowPreview(preview); importExportHost.EndProgress();
        if (preview.MaterializedRowCount > definition.MaxPreviewRows || preview.ValidRows != 1 || preview.InvalidRows != 1 ||
            !preview.Diagnostics.Any(x => x.Code == "IMPORT_CONVERSION_FAILED"))
            throw new InvalidOperationException("Bounded preview or diagnostics failed.");
        Console.WriteLine("SMOKE IMPORT_PREVIEW_DIAGNOSTICS: VALID=1 INVALID=1 BOUNDED PASS");

        var partial = new ImportDefinition(definition.ImportId, definition.ImportCode, definition.DisplayNameKey, definition.ParserCode,
            definition.FileExtensions, definition.Mappings, commitMode: ImportCommitMode.PartialValid);
        var operation = new ImportExportOperationContext(new(companyContext.CurrentCompany, "data-entry-demo"), companyScope.Snapshot.AuthorizationContext);
        await using var commitStream = new MemoryStream(bytes);
        var committed = await engine.CommitAsync(commitStream, partial, resolved, dataEntryProvider, operation,
            progress: new Progress<ImportExportProgress>(importExportHost.ReportProgress));
        if (committed.ImportedRecords != 1 || committed.InvalidRecords != 1) throw new InvalidOperationException("Partial import commit failed.");
        Console.WriteLine("SMOKE IMPORT_COMMIT_CUSTOM: IMPORTED=1 INVALID=1 PASS");

        using var cancel = new CancellationTokenSource(); cancel.Cancel(); await using var cancelledStream = new MemoryStream(bytes);
        await AssertCancelledAsync(() => engine.PreviewAsync(cancelledStream, definition, resolved, cancellationToken: cancel.Token));
        var session = new ImportSession(definition, operation); session.MoveTo(ImportSessionState.Inspect);
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyBId);
        var changed = operation with { GridContext = new(companyContext.CurrentCompany, "data-entry-demo") };
        if (session.IsCurrent(changed)) throw new InvalidOperationException("Company session guard did not invalidate stale context.");
        session.Invalidate(); Console.WriteLine("SMOKE IMPORT_CANCEL_COMPANY_GUARD: PASS");

        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        operation = new(new(companyContext.CurrentCompany, "data-entry-demo"), companyScope.Snapshot.AuthorizationContext);
        var exportEngine = new ExportEngine(registry);
        foreach (var scope in new[] { ExportScope.CurrentView, ExportScope.SelectedRows, ExportScope.AllFiltered })
        {
            var profile = DemoImportExport.ExportProfiles.First(x => x.WriterCode == ExportWriterCodes.Csv);
            var scoped = new ExportDefinition(profile.ExportId, $"{profile.ExportCode}_{scope}", profile.DisplayNameKey, profile.WriterCode,
                profile.FileExtension, profile.Fields, scope: scope);
            await using var output = new MemoryStream();
            var result = await exportEngine.ExportAsync(output, scoped, resolved, dataEntryProvider, operation,
                scope == ExportScope.SelectedRows ? dataEntryGridHost.Runtime.SelectedRowKeys : null, cancellationToken: default);
            if (!result.IsSuccess) throw new InvalidOperationException($"Export scope {scope} failed.");
        }
        var customExport = DemoImportExport.ExportProfiles.Single(x => x.WriterCode == DemoImportExport.CustomCode);
        var allRows = new ExportDefinition(customExport.ExportId, "DEMO_CUSTOM_ALL", customExport.DisplayNameKey,
            customExport.WriterCode, customExport.FileExtension, customExport.Fields, scope: ExportScope.AllRows);
        var largeResult = await exportEngine.ExportAsync(Stream.Null, allRows, resolved, dataEntryProvider, operation,
            progress: new Progress<ImportExportProgress>(importExportHost.ReportProgress));
        importExportHost.EndProgress();
        if (!largeResult.IsSuccess || largeResult.RecordsWritten != DemoDataEntryProvider.LogicalRowCount)
            throw new InvalidOperationException("100K custom streaming export failed.");
        Console.WriteLine("SMOKE EXPORT_SCOPES_CUSTOM_100K_PROGRESS: CURRENT SELECTED FILTERED ALL_ROWS PASS");
        dataEntryTabs.SelectedIndex = 0;

        static async Task AssertCancelledAsync(Func<Task> action)
        {
            try { await action(); throw new InvalidOperationException("Cancelled import completed unexpectedly."); }
            catch (OperationCanceledException) { }
        }
    }

    private sealed class UnavailableGridClipboard : IGridClipboardService
    {
        public Task<string?> ReadTextAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<string?>(new InvalidOperationException("unavailable"));
        public Task WriteTextAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("unavailable"));
    }

    private async Task RunNotificationSmokeAsync()
    {
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        workspaceSelector.SelectedIndex = visibleWorkspaces.ToList().FindIndex(x => x.WorkspaceId == "data-entry-demo");
        var model = await notificationCoordinator.RefreshAsync(companyContext.CurrentCompany, "data-entry-demo",
            companyScope.Snapshot.AuthorizationContext);
        var update = AssertSingle("DEMO.UPDATE_READY", model);
        if (update.Surfaces.Length != 4 || update.Instance.CurrentProgress?.CurrentValue != 75 ||
            model.ForSurface(NotificationSurface.Toast).Length == 0 ||
            model.ForSurface(NotificationSurface.Banner).Length == 0 ||
            model.ForSurface(NotificationSurface.AlertCard).Length == 0 ||
            model.ForSurface(NotificationSurface.BlockingNotice).Length == 0 ||
            model.AttentionCount == 0 || !notificationHost.IsVisible)
            throw new InvalidOperationException("Notification surfaces, progress, or attention count were not rendered.");
        notificationHost.IsCenterOpen = true;
        if (!notificationHost.IsCenterOpen || notificationHost.AttentionCount != model.AttentionCount)
            throw new InvalidOperationException("Notification Center did not open or show its attention count.");
        RefreshActionBars();
        if (!topActionBar.ResolvedActionBar!.Actions.Any(x => x.Definition.ActionCode == "UPDATE_RESTART") ||
            !bottomActionBar.ResolvedActionBar!.Actions.Any(x => x.Definition.ActionCode == "UPDATE_RESTART"))
            throw new InvalidOperationException("Multi-surface notification did not contribute to both Action Bars.");
        var updateCommand = await topActionBar.ExecuteActionAsync("UPDATE_RESTART");
        if (updateCommand.Status != ActionCommandResultStatus.Success)
            throw new InvalidOperationException("Update presentation command did not use the registered command pipeline.");
        Console.WriteLine("SMOKE NOTIFICATION_SURFACES: CENTER TOAST BANNER ALERT BLOCKING TOP BOTTOM PASS");
        Console.WriteLine("SMOKE NOTIFICATION_MULTI_SURFACE: ONE_INSTANCE PROGRESS=75/100 PASS");

        var configuration = AssertSingle("DEMO.CONFIG", model);
        var navigate = await notificationDispatcher.DispatchAsync(configuration.PrimaryAction!);
        if (navigate.Status != GuidanceActionResultStatus.Success || workspaceHost.CurrentDefinition?.WorkspaceId != "setup-demo" ||
            treeHost.SelectedNodeId is null)
            throw new InvalidOperationException("Guidance navigation did not synchronize workspace/tree context.");
        RefreshRibbon(); RefreshActionBars();
        Console.WriteLine("SMOKE NOTIFICATION_NAVIGATE_TREE_RIBBON_ACTIONBAR: PASS");

        var unknownWorkspace = await notificationDispatcher.DispatchAsync(AssertSingle("DEMO.UNKNOWN_WORKSPACE", model).PrimaryAction!);
        var unknownCommand = await notificationDispatcher.DispatchAsync(AssertSingle("DEMO.UNKNOWN_COMMAND", model).PrimaryAction!);
        var unknownFocus = await notificationDispatcher.DispatchAsync(AssertSingle("DEMO.UNKNOWN_FOCUS", model).PrimaryAction!);
        if (unknownWorkspace.Status != GuidanceActionResultStatus.Unavailable ||
            unknownCommand.Status != GuidanceActionResultStatus.Unavailable ||
            unknownFocus.Status != GuidanceActionResultStatus.PartialSuccess)
            throw new InvalidOperationException("Unknown notification guidance target did not fail safely.");
        Console.WriteLine("SMOKE NOTIFICATION_UNKNOWN_WORKSPACE_COMMAND_FOCUS: SAFE_FAILURE");

        if (!model.Diagnostics.Any(x => x.Code == "NOTIFICATION_PROVIDER_FAILED") ||
            model.Notifications.Any(x => x.Instance.Definition.NotificationCode is "DEMO.EXPIRED" or "DEMO.RESOLVED"))
            throw new InvalidOperationException("Provider isolation, expiration, or resolution failed.");
        if (notificationCoordinator.Dismiss(AssertSingle("DEMO.BLOCKING", model).Instance.InstanceId))
            throw new InvalidOperationException("Non-dismissible blocking notice was dismissed.");
        if (!notificationCoordinator.Dismiss(configuration.Instance.InstanceId) ||
            notificationCoordinator.Current.Notifications.Any(x => x.Instance.Definition.NotificationCode == "DEMO.CONFIG"))
            throw new InvalidOperationException("Shared notification dismissal failed.");
        Console.WriteLine("SMOKE NOTIFICATION_PROVIDER_LIFECYCLE_DISMISS: PASS");

        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyBId);
        model = await notificationCoordinator.RefreshAsync(companyContext.CurrentCompany, "setup-demo",
            companyScope.Snapshot.AuthorizationContext);
        if (model.Notifications.Any(x => x.Instance.Definition.NotificationCode == "DEMO.COMPANY") ||
            !model.Notifications.Any(x => x.Instance.Definition.NotificationCode == "DEMO.UPDATE_READY"))
            throw new InvalidOperationException("Company stale-state guard or global notification preservation failed.");
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        model = await notificationCoordinator.RefreshAsync(companyContext.CurrentCompany, "setup-demo",
            companyScope.Snapshot.AuthorizationContext);
        if (!model.Notifications.Any(x => x.Instance.Definition.NotificationCode == "DEMO.COMPANY"))
            throw new InvalidOperationException("Company-scoped notification did not re-resolve on return.");
        Console.WriteLine("SMOKE NOTIFICATION_COMPANY: A_TO_B_BLOCKED GLOBAL_PRESERVED A_RERESOLVED");

        var instanceId = AssertSingle("DEMO.UPDATE_READY", model).Instance.InstanceId;
        localization.TrySetCulture("vi-VN"); themeSelector.SelectedItem = ThemeMode.System;
        localization.TrySetCulture("en-US"); themeSelector.SelectedItem = ThemeMode.Light;
        themeSelector.SelectedItem = ThemeMode.Dark;
        var preserved = AssertSingle("DEMO.UPDATE_READY", notificationCoordinator.Current);
        if (preserved.Instance.InstanceId != instanceId || preserved.Instance.CurrentProgress?.CurrentValue != 75)
            throw new InvalidOperationException("Localization/theme changes replaced notification runtime state.");
        Console.WriteLine("SMOKE NOTIFICATION_VI_EN_SYSTEM_LIGHT_DARK: STATE_PRESERVED");
        Console.WriteLine("SMOKE NOTIFICATION_UPDATE_PRESENTATION_ONLY: REGISTERED_COMMAND PASS NO_UPDATER");
    }

    private static ResolvedNotification AssertSingle(string notificationCode, NotificationPresentationModel model) =>
        model.Notifications.Single(x => x.Instance.Definition.NotificationCode == notificationCode);

    private async Task RunSetupSmokeAsync()
    {
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        setupWorkspaceHost.UpdateContext(companyScope.Snapshot.Company, companyScope.Snapshot.AuthorizationContext);
        var setupIndex = visibleWorkspaces.ToList().FindIndex(x => x.WorkspaceId == "setup-demo");
        workspaceSelector.SelectedIndex = setupIndex;
        if (workspaceHost.CurrentDefinition?.WorkspaceId != "setup-demo" || workspaceHost.Content != setupWorkspaceHost)
            workspaceHost.ShowWorkspace(workspaces[setupIndex]);
        if (workspaceHost.Content != setupWorkspaceHost ||
            !new[] { "GENERAL", "MASTER_CATALOGS", "WORKSPACES", "COLUMNS_VARIABLES", "NAVIGATION_TREE", "RIBBON", "ACTION_BARS", "DASHBOARD", "REPORTS" }
                .All(setupWorkspaceHost.VisibleCategoryCodes.Contains) ||
            !new[] { "COLUMNS", "VARIABLES", "FORMULAS" }.All(setupWorkspaceHost.VisibleCategoryCodes.Contains))
            throw new InvalidOperationException("Setup tree or specialized categories were not rendered.");
        Console.WriteLine("SMOKE SETUP_TREE: PASS FIVE_SPECIALIZED_EDITORS");

        var initialCatalogWindow = setupWorkspaceHost.GetCategoryChildWindow(null);
        if (initialCatalogWindow.VisibleCount != 5 ||
            !initialCatalogWindow.CanShowMore || initialCatalogWindow.CanShowLess ||
            !setupWorkspaceHost.ShowMoreCategories(null))
            throw new InvalidOperationException("Setup shared Tree initial overflow window failed.");
        var expandedCatalogWindow = setupWorkspaceHost.GetCategoryChildWindow(null);
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyBId);
        setupWorkspaceHost.UpdateContext(companyScope.Snapshot.Company, companyScope.Snapshot.AuthorizationContext);
        if (setupWorkspaceHost.GetCategoryChildWindow(null).VisibleCount != 9)
            throw new InvalidOperationException("Setup Tree overflow state was lost on Company change.");
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        setupWorkspaceHost.UpdateContext(companyScope.Snapshot.Company, companyScope.Snapshot.AuthorizationContext);
        languageSelector.SelectedItem = "vi-VN";
        themeSelector.SelectedItem = ThemeMode.Light;
        languageSelector.SelectedItem = "en-US";
        themeSelector.SelectedItem = ThemeMode.Dark;
        if (expandedCatalogWindow.VisibleCount != 9 || expandedCatalogWindow.CanShowMore || !expandedCatalogWindow.CanShowLess ||
            setupWorkspaceHost.GetCategoryChildWindow(null).VisibleCount != 9 ||
            !setupWorkspaceHost.ShowLessCategories(null) || setupWorkspaceHost.GetCategoryChildWindow(null).VisibleCount != 5)
            throw new InvalidOperationException("Setup shared Tree incremental/show-less or presentation-state preservation failed.");
        Console.WriteLine("SMOKE SETUP_TREE_OVERFLOW: INITIAL=5 MORE=9 LESS=5 COMPANY_CULTURE_THEME_PRESERVED");

        if (!setupWorkspaceHost.NavigateActionMenu(SetupActionCodes.New, Key.Down) ||
            !setupWorkspaceHost.IsActionMenuOpen(SetupActionCodes.New) ||
            !setupWorkspaceHost.LastActionMenuOpenUsedKeyboard(SetupActionCodes.New) ||
            setupWorkspaceHost.FocusedActionMenuItemCode(SetupActionCodes.New) != "NEW_STANDARD" ||
            setupWorkspaceHost.ActionMenuItemState(SetupActionCodes.New, "ADMIN_ONLY") != AuthorizationPresentationState.VisibleDisabled ||
            setupWorkspaceHost.ActionMenuItemState(SetupActionCodes.New, "HIDDEN_ITEM") is not null ||
            !setupWorkspaceHost.NavigateActionMenu(SetupActionCodes.New, Key.Escape) ||
            setupWorkspaceHost.IsActionMenuOpen(SetupActionCodes.New))
            throw new InvalidOperationException("Setup dropdown keyboard/open/close or permission presentation failed.");
        if ((await setupWorkspaceHost.ExecuteActionMenuItemAsync(SetupActionCodes.New, "NEW_STANDARD")).Status != ActionCommandResultStatus.Success ||
            (await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.New)).Status != ActionCommandResultStatus.Success ||
            (await setupWorkspaceHost.ExecuteActionMenuItemAsync(SetupActionCodes.New, "UNKNOWN_SAFE")).Status != ActionCommandResultStatus.Unavailable ||
            (await setupWorkspaceHost.ExecuteActionMenuItemAsync(SetupActionCodes.New, "ADMIN_ONLY")).Status != ActionCommandResultStatus.Denied)
            throw new InvalidOperationException("Setup menu selection, split default, or safe unknown-command handling failed.");
        Console.WriteLine("SMOKE SETUP_ACTION_VARIANTS: DROPDOWN_KEYBOARD SPLIT_DEFAULT PERMISSIONS UNKNOWN_SAFE PASS");

        var catalogSelected = setupWorkspaceHost.SelectCategory("catalogs");
        var definitionSelected = setupWorkspaceHost.SelectDefinition("catalog-02");
        if (!catalogSelected || setupWorkspaceHost.DefinitionCount != 10 || !definitionSelected ||
            setupWorkspaceHost.LastEditorKind != SetupEditorKind.Custom)
            throw new InvalidOperationException($"Catalog selection, 9+ definition list, or specialized editor failed: " +
                $"category={catalogSelected}/{setupWorkspaceHost.SelectedCategoryId}, rows={setupWorkspaceHost.DefinitionCount}, " +
                $"definition={definitionSelected}, editor={setupWorkspaceHost.LastEditorKind}.");
        Console.WriteLine("SMOKE SETUP_MASTER_CATALOG_DESIGNER: PASS CATALOGS=10");

        if (!setupWorkspaceHost.OpenActionMenu(SetupActionCodes.Clone) ||
            !setupWorkspaceHost.IsActionMenuOpen(SetupActionCodes.Clone) ||
            !setupWorkspaceHost.CloseActionMenu(SetupActionCodes.Clone) ||
            setupWorkspaceHost.IsActionMenuOpen(SetupActionCodes.Clone) ||
            setupWorkspaceHost.GetCategoryVisualState("catalogs") != TreeRowVisualState.Selected ||
            setupWorkspaceHost.GetCategoryVisualState("catalogs", hover: true) != TreeRowVisualState.SelectedHover ||
            setupWorkspaceHost.GetCategoryVisualState("workspaces", hover: true) != TreeRowVisualState.Hover ||
            setupWorkspaceHost.GetCategoryVisualState("workspaces", focus: true) != TreeRowVisualState.KeyboardFocus ||
            SetupWorkspaceHost.GetOverflowVisualState(hover: true) != TreeRowVisualState.Hover ||
            treeHost.GetNodeVisualState("disabled") != TreeRowVisualState.Disabled ||
            (await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.ToggleDetails)).Status != ActionCommandResultStatus.Success)
            throw new InvalidOperationException("Shared tree row states, dropdown mouse behavior, or toggle action failed.");
        Console.WriteLine("SMOKE SHARED_TREE_ROW_UX: NORMAL HOVER SELECTED SELECTED_HOVER DISABLED FOCUS OVERFLOW PASS");

        setupWorkspaceHost.SetCandidateValue("DISPLAY_NAME_KEY", "Catalog.Changed");
        var baselineGeometry = setupWorkspaceHost.ActionGeometry(SetupActionCodes.New)!;
        appearanceService.Update(appearanceService.Current with { UiScale = 1.25, FontSize = FontSizePreference.Large });
        var scaledGeometry = setupWorkspaceHost.ActionGeometry(SetupActionCodes.New)!;
        themeSelector.SelectedItem = ThemeMode.System;
        languageSelector.SelectedItem = "vi-VN";
        themeSelector.SelectedItem = ThemeMode.Light;
        languageSelector.SelectedItem = "en-US";
        themeSelector.SelectedItem = ThemeMode.Dark;
        if (scaledGeometry.Height != baselineGeometry.Height * 1.25 ||
            scaledGeometry.IconSize != baselineGeometry.IconSize * 1.25 ||
            scaledGeometry.FontSize <= baselineGeometry.FontSize * 1.25 ||
            scaledGeometry.MinWidth != baselineGeometry.MinWidth * 1.25 ||
            setupWorkspaceHost.SelectedCategoryId != "catalogs" || !setupWorkspaceHost.Lifecycle.Buffer!.IsDirty ||
            iconRegistry.Resolve(StandardIconKeys.Add).Source is not SvgIconSource ||
            iconRegistry.Resolve(StandardIconKeys.More).Source is not FontGlyphIconSource)
            throw new InvalidOperationException("Action geometry/global scaling or generic SVG/font-glyph icon rendering failed.");
        appearanceService.Update(new(ThemeMode.Dark, 1, FontSizePreference.Normal));
        Console.WriteLine("SMOKE ACTION_GEOMETRY_ICONS: XS_SMALL_MEDIUM_LARGE_XL SCALE_FONT SVG_FONT_GLYPH VARIANTS PASS");
        var resizeWorkspaceId = workspaceHost.CurrentDefinition?.WorkspaceId;
        var resizeCategoryId = setupWorkspaceHost.SelectedCategoryId;
        if (!setupWorkspaceHost.HasResizableNavigationSplitter || setupWorkspaceHost.NavigationPaneWidth != 260 ||
            setupWorkspaceHost.ResizeNavigationPane(390) != 390 ||
            setupWorkspaceHost.NavigationPaneWidth != 390 || setupWorkspaceHost.ResizeNavigationPane(215) != 215 ||
            setupWorkspaceHost.NavigationPaneWidth != 215 || !setupWorkspaceHost.Lifecycle.Buffer!.IsDirty ||
            !Equals(setupWorkspaceHost.Lifecycle.Buffer.Candidate.Values["DISPLAY_NAME_KEY"], "Catalog.Changed") ||
            setupWorkspaceHost.SelectedCategoryId != resizeCategoryId || workspaceHost.CurrentDefinition?.WorkspaceId != resizeWorkspaceId ||
            shellPresentation.CultureName != "en-US" || shellPresentation.Theme != ThemeMode.Dark)
            throw new InvalidOperationException("Setup split-navigation resize lost layout or workspace state.");
        Console.WriteLine("SMOKE SETUP_SPLITTER: 260_TO_390_TO_215 STATE_PRESERVED");
        setupWorkspaceHost.SelectCategory("workspaces");
        if (setupWorkspaceHost.SelectedCategoryId != "catalogs")
            throw new InvalidOperationException("Dirty Setup navigation was not blocked.");
        if ((await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Cancel)).Status != ActionCommandResultStatus.Success ||
            setupWorkspaceHost.Lifecycle.Buffer!.IsDirty)
            throw new InvalidOperationException("Setup cancel/revert failed.");
        Console.WriteLine("SMOKE SETUP_DIRTY_CANCEL: PASS");

        setupWorkspaceHost.SelectCategory("workspaces");
        setupWorkspaceHost.SelectDefinition("workspace-3");
        var templateChoices = setupWorkspaceHost.LastEditorDescriptor!.Fields.Single(x => x.FieldCode == "TEMPLATE_CODE").Choices;
        if (!templateChoices.Any(x => x.Value == "CALENDAR"))
            throw new InvalidOperationException("TemplateRegistry-driven CALENDAR choice was not available.");
        Console.WriteLine("SMOKE SETUP_WORKSPACE_DESIGNER: PASS TEMPLATE_REGISTRY CALENDAR");

        setupWorkspaceHost.SelectCategory("columns");
        setupWorkspaceHost.SelectDefinition("column-08");
        if (setupWorkspaceHost.LastEditorKind != SetupEditorKind.Custom || setupWorkspaceHost.Lifecycle.Buffer!.Candidate.Values["COLUMN_MODE"]?.ToString() != "FORMULA")
            throw new InvalidOperationException("Column designer FORMULA mode failed.");
        setupWorkspaceHost.SelectDefinition("column-09");
        if (setupWorkspaceHost.Lifecycle.Buffer!.Candidate.Values["COLUMN_MODE"]?.ToString() != "SYSTEM")
            throw new InvalidOperationException("Column designer SYSTEM mode failed.");
        Console.WriteLine("SMOKE SETUP_COLUMN_DESIGNER: PASS INPUT FORMULA SYSTEM GEOMETRY");

        setupWorkspaceHost.SelectCategory("variables");
        setupWorkspaceHost.SelectDefinition("variable-01");
        setupWorkspaceHost.SetCandidateValue("VARIABLE_CODE", "QUANTITY_DRAFT");
        if (!setupWorkspaceHost.Lifecycle.Buffer!.IsDirty)
            throw new InvalidOperationException("Draft VariableCode was not editable.");
        setupWorkspaceHost.Lifecycle.CancelChanges();
        Console.WriteLine("SMOKE SETUP_VARIABLE_DESIGNER: PASS DRAFT_VARIABLE_CODE");

        setupWorkspaceHost.SelectCategory("formulas");
        setupWorkspaceHost.SelectDefinition("formula-total");
        if (setupWorkspaceHost.LastEditorDescriptor!.Fields.Single(x => x.FieldCode == "REFERENCED_VARIABLE_CODES").FieldType != EditorFieldType.MultiChoice ||
            (await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Validate)).Status != ActionCommandResultStatus.Success ||
            setupWorkspaceHost.Lifecycle.LastValidation?.IsValid != true)
            throw new InvalidOperationException("Formula picker or valid reference validation failed.");
        setupWorkspaceHost.Lifecycle.CancelChanges();
        setupWorkspaceHost.SelectDefinition("formula-invalid");
        await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Validate);
        if (setupWorkspaceHost.Lifecycle.LastValidation?.IsValid != false)
            throw new InvalidOperationException("Bad VariableCode reference was accepted.");
        setupWorkspaceHost.Lifecycle.CancelChanges();
        Console.WriteLine("SMOKE SETUP_FORMULA_METADATA: PASS PICKER VALID UNKNOWN_REFERENCE_REJECTED");

        setupWorkspaceHost.SelectCategory("general");
        if ((await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.New)).Status != ActionCommandResultStatus.Success)
            throw new InvalidOperationException("Setup New failed.");
        setupWorkspaceHost.SetCandidateValue("NAME", "Published smoke definition");
        if ((await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Save)).Status != ActionCommandResultStatus.Success ||
            (await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Validate)).Status != ActionCommandResultStatus.Success ||
            setupWorkspaceHost.Lifecycle.LastValidation?.IsValid != true ||
            (await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Publish)).Status != ActionCommandResultStatus.Success)
            throw new InvalidOperationException("Setup draft/validate/publish flow failed.");
        Console.WriteLine("SMOKE SETUP_NEW_SAVE_VALIDATE_PUBLISH: PASS");
        if ((await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Retire)).Status != ActionCommandResultStatus.Success ||
            setupWorkspaceHost.Lifecycle.Buffer?.Source.Status != SetupDefinitionStatus.Retired)
            throw new InvalidOperationException("Setup retire transition failed.");
        Console.WriteLine("SMOKE SETUP_RETIRE: PASS");

        var invalidSelected = setupWorkspaceHost.SelectDefinition("invalid-1");
        var invalidValidation = await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Validate);
        var invalidPublish = await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Publish);
        if (!invalidSelected || invalidValidation.Status != ActionCommandResultStatus.Success ||
            setupWorkspaceHost.Lifecycle.LastValidation?.IsValid != false || invalidPublish.Status != ActionCommandResultStatus.Denied)
            throw new InvalidOperationException($"Invalid Setup draft flow failed: selected={invalidSelected}, " +
                $"validate={invalidValidation.Status}, valid={setupWorkspaceHost.Lifecycle.LastValidation?.IsValid}, publish={invalidPublish.Status}.");
        setupWorkspaceHost.Lifecycle.CancelChanges();
        Console.WriteLine("SMOKE SETUP_INVALID_PUBLISH: BLOCKED");

        setupWorkspaceHost.SelectDefinition("system-1");
        if (!setupWorkspaceHost.IsCandidateReadOnly ||
            (await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Clone)).Status != ActionCommandResultStatus.Success)
            throw new InvalidOperationException("System read-only/clone behavior failed.");
        Console.WriteLine("SMOKE SETUP_READONLY_CLONE: PASS");

        setupWorkspaceHost.Lifecycle.CancelChanges();
        setupWorkspaceHost.SelectCategory("navigation");
        setupWorkspaceHost.SelectDefinition("navigation-1");
        if (setupWorkspaceHost.LastEditorKind != SetupEditorKind.Unavailable)
            throw new InvalidOperationException("Missing specialized editor was not safe.");
        Console.WriteLine("SMOKE SETUP_MISSING_EDITOR: SAFE_UNAVAILABLE");

        languageSelector.SelectedItem = "vi-VN";
        themeSelector.SelectedItem = ThemeMode.Light;
        languageSelector.SelectedItem = "en-US";
        themeSelector.SelectedItem = ThemeMode.Dark;
        if (setupWorkspaceHost.SelectedCategoryId != "navigation")
            throw new InvalidOperationException("Setup culture/theme switch lost selection.");
        Console.WriteLine("SMOKE SETUP_LOCALIZATION_THEME: PASS");
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
