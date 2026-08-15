using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicUI24.Avalonia.Presentation;
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
    private readonly ComboBox workspaceSelector = new();
    private readonly ComboBox themeSelector = new();
    private readonly ComboBox languageSelector = new();
    private readonly ComboBox stateSelector = new();
    private readonly TextBlock workspaceLabel = new();
    private readonly TextBlock themeLabel = new();
    private readonly TextBlock languageLabel = new();
    private readonly TextBlock stateLabel = new();
    private readonly TextBlock iconLabel = new();
    private DispatcherTimer? smokeTimer;
    private int smokeStep;

    public MainWindow()
        : this(DemoComposition.Create())
    {
    }

    private MainWindow(DemoComposition composition)
    {
        InitializeComponent();
        workspaces = composition.Workspaces;
        localization = new DictionaryLocalizationService();
        themeService = new AvaloniaThemeService(Application.Current!);
        iconRegistry = CreateDemoIconRegistry();
        shellPresentation = new ShellPresentation(
            new ApplicationBrand("GoldenCAD DynamicUI24", DemoLogoKey, "#7C3AED"));
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
        localization.CultureChanged += (_, _) => RefreshLocalizedLabels();
        RefreshLocalizedLabels();
        workspaceSelector.SelectedIndex = 0;
        stateSelector.SelectedIndex = 0;

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

        return new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 14,
            Children =
            {
                selectors,
                Framed(workspaceHost, 1),
                Framed(stateView, 2),
                BuildIconSamples(),
            },
        };
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

    private Control BuildIconSamples()
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
        Grid.SetRow(panel, 3);
        return panel;
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

    private void AdvanceSmokeRun(object? sender, EventArgs e)
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
        else
        {
            smokeTimer!.Stop();
            Console.WriteLine("SMOKE CLEAN_EXIT: PASS");
            Close();
            return;
        }

        smokeStep++;
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
}
