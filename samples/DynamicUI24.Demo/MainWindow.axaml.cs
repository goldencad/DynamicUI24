using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Core.ApplicationMenu;
using DynamicUI24.Core.Ribbon;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Setup;
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
    private readonly SetupWorkspaceHost setupWorkspaceHost;
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
    private readonly DynamicActionBarHost topActionBar;
    private readonly DynamicActionBarHost bottomActionBar;
    private readonly TreeDefinition treeDefinition = DemoTree.Create();
    private readonly ComboBox workspaceSelector = new();
    private readonly ComboBox themeSelector = new();
    private readonly ComboBox languageSelector = new();
    private readonly ComboBox stateSelector = new();
    private readonly ComboBox companySelector = new();
    private readonly ComboBox unauthorizedBehaviorSelector = new();
    private readonly ComboBox selectionSelector = new();
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
    private readonly TextBlock selectionLabel = new();
    private DispatcherTimer? smokeTimer;
    private int smokeStep;
    private bool smokeAdvancing;
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
        iconRegistry = CreateDemoIconRegistry();
        shellPresentation = new ShellPresentation(
            new ApplicationBrand("Framework Demo", DemoLogoKey, "#7C3AED"));
        workspaceHost = new DynamicUI24.Avalonia.DynamicWorkspaceHost(composition.Registry, localization);
        setupWorkspaceHost = new SetupWorkspaceHost(DemoSetup.Categories, new DemoSetupProvider(),
            new DemoSetupValidator(), DemoSetup.CreateEditors(), localization, iconRegistry,
            companyContext.CurrentCompany);
        workspaceHost.RegisterViewFactory(StandardTemplateCodes.Setup, _ => setupWorkspaceHost);
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
        var menuComposer = new ApplicationMenuComposer();
        menuComposer.Register(new DemoPreferencesContributor());
        shell.ApplicationMenuContent = new ApplicationMenuView(
            shellPresentation.Brand,
            menuComposer,
            localization,
            iconRegistry,
            themeService,
            new AppearancePreferenceService(),
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
        var dispatcher = new RibbonCommandDispatcher(
            new DemoRibbonNavigationService(workspaces, NavigateFromRibbon),
            new DemoRibbonRefreshService(RefreshFromRibbon),
            commandRegistry);
        var actionCommands = new ActionCommandRegistry();
        actionCommands.Register("DEMO.ACTION.CUSTOM", (_, _) =>
            Task.FromResult(ActionCommandResult.Success("Custom registered Action Bar command dispatched.")));
        actionCommands.Register("DEMO.ACTION.GATED", (_, _) =>
            Task.FromResult(ActionCommandResult.Success("Permission-gated Action Bar command dispatched.")));
        var actionDispatcher = new ActionBarCommandDispatcher(
            workspaceNavigation, new DemoActionRefreshService(RefreshFromActionBar), actionCommands);
        topActionBar = new DynamicActionBarHost(actionDispatcher, localization, iconRegistry);
        bottomActionBar = new DynamicActionBarHost(actionDispatcher, localization, iconRegistry);
        topActionBar.CommandCompleted += ActionBarCommandCompleted;
        bottomActionBar.CommandCompleted += ActionBarCommandCompleted;
        ribbonHost = new DynamicRibbonHost(
            DemoRibbon.Create(), workspaces, CreateRibbonContext(workspaces[0]),
            new DynamicRibbonResolver(), dispatcher, localization, iconRegistry);
        ribbonHost.CommandCompleted += (_, result) =>
            shellPresentation.StatusMessage = $"Ribbon: {result.Status} · {result.DiagnosticCode ?? result.Message ?? "OK"}";
        shell.RibbonContent = ribbonHost;
        treeHost = new DynamicTreeHost(localization, iconRegistry);
        treeHost.NodeSelected += (_, args) => NavigateTreeNode(args.Node);
        shell.NavigationContent = treeHost;
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
            ColumnDefinitions = new ColumnDefinitions("2*,*,*,*,*"),
            ColumnSpacing = 12,
            Children =
            {
                Field(workspaceLabel, workspaceSelector, 0),
                Field(themeLabel, themeSelector, 1),
                Field(languageLabel, languageSelector, 2),
                Field(stateLabel, stateSelector, 3),
                Field(selectionLabel, selectionSelector, 4),
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

    private static Control Field(TextBlock label, ComboBox selector, int column)
    {
        selector.MinWidth = 120;
        var panel = new StackPanel { Spacing = 5, Children = { label, selector } };
        Grid.SetColumn(panel, column);
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
        RefreshRibbon();
        RefreshActionBars();
        RefreshTree(snapshot);
        setupWorkspaceHost.UpdateContext(snapshot.Company, snapshot.AuthorizationContext);
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
        RefreshRibbon();
        RefreshActionBars();
        treeHost.SelectWorkspace(definition.WorkspaceId);
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
        companyScope.Snapshot.AuthorizationContext,
        new RibbonSelectionContext(selectionSelector.SelectedItem is int count ? count : 0));

    private void RefreshRibbon()
    {
        var workspace = workspaceHost.CurrentDefinition ?? workspaces[0];
        ribbonHost.UpdateContext(CreateRibbonContext(workspace));
    }

    private ActionBarResolutionContext CreateActionBarContext(WorkspaceDefinition workspace) => new(
        companyContext.CurrentCompany,
        workspace,
        workspace.TemplateCode,
        companyScope.Snapshot.AuthorizationContext,
        new ActionSelectionContext(selectionSelector.SelectedItem is int count ? count : 0),
        shellPresentation.State,
        CreateActionBarStatus(workspace));

    private ActionBarStatus CreateActionBarStatus(WorkspaceDefinition workspace)
    {
        var selected = selectionSelector.SelectedItem is int count ? count : 0;
        return workspace.WorkspaceId switch
        {
            "data-entry-demo" => new(125, 125, selected, 2, 0, 3, false),
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
        ShowActionBar(topActionBar, bars.FirstOrDefault(x => x.Position == ActionBarPosition.Top), context);
        ShowActionBar(bottomActionBar, bars.FirstOrDefault(x => x.Position == ActionBarPosition.Bottom), context);
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
        var index = workspaces.ToList().FindIndex(x => x.WorkspaceId.Equals(workspace.WorkspaceId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) workspaceSelector.SelectedIndex = index;
    }

    private void RefreshFromActionBar()
    {
        actionRefreshCount++;
        if (workspaceHost.CurrentDefinition is { } workspace) workspaceHost.ShowWorkspace(workspace);
        shellPresentation.StatusMessage = $"{localization.Get(new("ActionBar.RefreshComplete"))} #{actionRefreshCount}";
        RefreshActionBars();
    }

    private void NavigateFromRibbon(WorkspaceDefinition workspace)
    {
        var index = workspaces.ToList().FindIndex(x => x.WorkspaceId == workspace.WorkspaceId);
        if (index >= 0) workspaceSelector.SelectedIndex = index;
    }

    private void NavigateTreeNode(TreeNodeDefinition node)
    {
        if (node.WorkspaceId is null) return;
        var index = workspaces.ToList().FindIndex(x => x.WorkspaceId.Equals(node.WorkspaceId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) workspaceSelector.SelectedIndex = index;
        else shellPresentation.StatusMessage = "Tree: WORKSPACE_UNKNOWN";
    }

    private void RefreshTree(CompanyScopeSnapshot snapshot)
    {
        var previouslySelectedNodeId = treeHost.SelectedNodeId;
        var tree = treeResolver.Resolve(treeDefinition,
            new TreeResolutionContext(snapshot.Company, snapshot.AuthorizationContext), workspaces);
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
                await RunRibbonSmokeAsync();
                await RunActionBarSmokeAsync();
                await RunSetupSmokeAsync();
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
        workspaceSelector.SelectedIndex = workspaces.ToList().FindIndex(x => x.WorkspaceId == "dashboard-demo");
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
        Console.WriteLine("SMOKE ACTION_NAVIGATE: PASS");

        selectionSelector.SelectedItem = 0;
        RefreshActionBars();
        if (ActionBarAction(topActionBar, "EDIT").IsEnabled)
            throw new InvalidOperationException("Selection action must be disabled at zero selection.");
        selectionSelector.SelectedItem = 1;
        RefreshActionBars();
        if (!ActionBarAction(topActionBar, "EDIT").IsEnabled || bottomActionBar.ResolvedActionBar?.Status?.SelectedRows != 1)
            throw new InvalidOperationException("Selection state or Bottom Action Bar status was not refreshed.");
        Console.WriteLine("SMOKE ACTION_SELECTION_STATUS: PASS");

        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        RefreshActionBars();
        if (!ActionBarAction(topActionBar, "EDIT").IsEnabled)
            throw new InvalidOperationException("Company A Action Bar permission was not enabled.");
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyBId);
        RefreshActionBars();
        if (ActionBarAction(topActionBar, "EDIT").IsEnabled)
            throw new InvalidOperationException("Company B Action Bar permission was not disabled.");
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyCId);
        RefreshActionBars();
        if (ActionBarAction(topActionBar, "EDIT").IsEnabled)
            throw new InvalidOperationException("Unavailable Company authorization did not fail closed for Action Bars.");
        Console.WriteLine("SMOKE ACTION_COMPANY_RERESOLUTION: A=ENABLED B=DISABLED C=FAIL_CLOSED");

        workspaceSelector.SelectedIndex = workspaces.ToList().FindIndex(x => x.WorkspaceId == "signing-demo");
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

    private async Task RunSetupSmokeAsync()
    {
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        setupWorkspaceHost.UpdateContext(companyScope.Snapshot.Company, companyScope.Snapshot.AuthorizationContext);
        workspaceSelector.SelectedIndex = workspaces.ToList().FindIndex(x => x.WorkspaceId == "setup-demo");
        if (workspaceHost.Content != setupWorkspaceHost ||
            !new[] { "GENERAL", "MASTER_CATALOGS", "WORKSPACES", "COLUMNS_VARIABLES", "NAVIGATION_TREE", "RIBBON", "ACTION_BARS", "DASHBOARD", "REPORTS" }
                .All(setupWorkspaceHost.VisibleCategoryCodes.Contains) ||
            setupWorkspaceHost.VisibleCategoryCodes.Count(x => x.StartsWith("CATALOG_", StringComparison.Ordinal)) < 9)
            throw new InvalidOperationException("Setup tree or standard/catalog categories were not rendered.");
        Console.WriteLine("SMOKE SETUP_TREE: PASS CATALOGS=" + setupWorkspaceHost.VisibleCategoryCodes.Count(x => x.StartsWith("CATALOG_", StringComparison.Ordinal)));

        var initialCatalogWindow = setupWorkspaceHost.GetCategoryChildWindow("catalogs");
        if (!setupWorkspaceHost.SetCategoryExpanded("catalogs", true) || initialCatalogWindow.VisibleCount != 5 ||
            !initialCatalogWindow.CanShowMore || initialCatalogWindow.CanShowLess ||
            !setupWorkspaceHost.ShowMoreCategories("catalogs"))
            throw new InvalidOperationException("Setup shared Tree initial overflow window failed.");
        var expandedCatalogWindow = setupWorkspaceHost.GetCategoryChildWindow("catalogs");
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyBId);
        setupWorkspaceHost.UpdateContext(companyScope.Snapshot.Company, companyScope.Snapshot.AuthorizationContext);
        if (setupWorkspaceHost.GetCategoryChildWindow("catalogs").VisibleCount != 10)
            throw new InvalidOperationException("Setup Tree overflow state was lost on Company change.");
        await companyScope.SwitchCompanyAsync(DemoCompanyData.CompanyAId);
        setupWorkspaceHost.UpdateContext(companyScope.Snapshot.Company, companyScope.Snapshot.AuthorizationContext);
        languageSelector.SelectedItem = "vi-VN";
        themeSelector.SelectedItem = ThemeMode.Light;
        languageSelector.SelectedItem = "en-US";
        themeSelector.SelectedItem = ThemeMode.Dark;
        if (expandedCatalogWindow.VisibleCount != 10 || expandedCatalogWindow.CanShowMore || !expandedCatalogWindow.CanShowLess ||
            setupWorkspaceHost.GetCategoryChildWindow("catalogs").VisibleCount != 10 ||
            !setupWorkspaceHost.IsCategoryExpanded("catalogs") ||
            !setupWorkspaceHost.ShowLessCategories("catalogs") || setupWorkspaceHost.GetCategoryChildWindow("catalogs").VisibleCount != 5)
            throw new InvalidOperationException("Setup shared Tree incremental/show-less or presentation-state preservation failed.");
        Console.WriteLine("SMOKE SETUP_TREE_OVERFLOW: INITIAL=5 MORE=10 LESS=5 COMPANY_CULTURE_THEME_PRESERVED");

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

        var catalogSelected = setupWorkspaceHost.SelectCategory("catalog-01");
        var definitionSelected = setupWorkspaceHost.SelectDefinition("catalog-definition-01-a");
        if (!catalogSelected || setupWorkspaceHost.DefinitionCount != 1 || !definitionSelected ||
            setupWorkspaceHost.LastEditorKind != SetupEditorKind.PropertyForm)
            throw new InvalidOperationException($"Catalog selection, definition list, or generic editor failed: " +
                $"category={catalogSelected}/{setupWorkspaceHost.SelectedCategoryId}, rows={setupWorkspaceHost.DefinitionCount}, " +
                $"definition={definitionSelected}, editor={setupWorkspaceHost.LastEditorKind}.");
        Console.WriteLine("SMOKE SETUP_CATALOG_EDITOR: PASS");

        if (!setupWorkspaceHost.OpenActionMenu(SetupActionCodes.Clone) ||
            !setupWorkspaceHost.IsActionMenuOpen(SetupActionCodes.Clone) ||
            !setupWorkspaceHost.CloseActionMenu(SetupActionCodes.Clone) ||
            setupWorkspaceHost.IsActionMenuOpen(SetupActionCodes.Clone) ||
            setupWorkspaceHost.GetCategoryVisualState("catalog-01") != TreeRowVisualState.Selected ||
            setupWorkspaceHost.GetCategoryVisualState("catalog-01", hover: true) != TreeRowVisualState.SelectedHover ||
            setupWorkspaceHost.GetCategoryVisualState("catalog-02", hover: true) != TreeRowVisualState.Hover ||
            setupWorkspaceHost.GetCategoryVisualState("catalog-02", focus: true) != TreeRowVisualState.KeyboardFocus ||
            SetupWorkspaceHost.GetOverflowVisualState(hover: true) != TreeRowVisualState.Hover ||
            treeHost.GetNodeVisualState("disabled") != TreeRowVisualState.Disabled ||
            (await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.ToggleDetails)).Status != ActionCommandResultStatus.Success)
            throw new InvalidOperationException("Shared tree row states, dropdown mouse behavior, or toggle action failed.");
        Console.WriteLine("SMOKE SHARED_TREE_ROW_UX: NORMAL HOVER SELECTED SELECTED_HOVER DISABLED FOCUS OVERFLOW PASS");

        setupWorkspaceHost.SetCandidateValue("NAME", "changed");
        var resizeWorkspaceId = workspaceHost.CurrentDefinition?.WorkspaceId;
        var resizeCategoryId = setupWorkspaceHost.SelectedCategoryId;
        if (!setupWorkspaceHost.HasResizableNavigationSplitter || setupWorkspaceHost.NavigationPaneWidth != 260 ||
            setupWorkspaceHost.ResizeNavigationPane(390) != 390 ||
            setupWorkspaceHost.NavigationPaneWidth != 390 || setupWorkspaceHost.ResizeNavigationPane(215) != 215 ||
            setupWorkspaceHost.NavigationPaneWidth != 215 || !setupWorkspaceHost.Lifecycle.Buffer!.IsDirty ||
            !Equals(setupWorkspaceHost.Lifecycle.Buffer.Candidate.Values["NAME"], "changed") ||
            setupWorkspaceHost.SelectedCategoryId != resizeCategoryId || workspaceHost.CurrentDefinition?.WorkspaceId != resizeWorkspaceId ||
            shellPresentation.CultureName != "en-US" || shellPresentation.Theme != ThemeMode.Dark)
            throw new InvalidOperationException("Setup split-navigation resize lost layout or workspace state.");
        Console.WriteLine("SMOKE SETUP_SPLITTER: 260_TO_390_TO_215 STATE_PRESERVED");
        setupWorkspaceHost.SelectCategory("catalog-02");
        if (setupWorkspaceHost.SelectedCategoryId != "catalog-01")
            throw new InvalidOperationException("Dirty Setup navigation was not blocked.");
        if ((await setupWorkspaceHost.ExecuteActionAsync(SetupActionCodes.Cancel)).Status != ActionCommandResultStatus.Success ||
            setupWorkspaceHost.Lifecycle.Buffer!.IsDirty)
            throw new InvalidOperationException("Setup cancel/revert failed.");
        Console.WriteLine("SMOKE SETUP_DIRTY_CANCEL: PASS");

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
        setupWorkspaceHost.SelectCategory("columns");
        setupWorkspaceHost.SelectDefinition("columns-1");
        if (setupWorkspaceHost.LastEditorKind != SetupEditorKind.Unavailable)
            throw new InvalidOperationException("Specialized editor placeholder was not safe.");
        Console.WriteLine("SMOKE SETUP_PLACEHOLDER: SAFE_UNAVAILABLE");

        languageSelector.SelectedItem = "vi-VN";
        themeSelector.SelectedItem = ThemeMode.Light;
        languageSelector.SelectedItem = "en-US";
        themeSelector.SelectedItem = ThemeMode.Dark;
        if (setupWorkspaceHost.SelectedCategoryId != "columns")
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
