using Avalonia.Controls;
using Avalonia.Threading;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;

namespace DynamicUI24.Demo;

public sealed partial class MainWindow : Window
{
    private readonly IReadOnlyList<WorkspaceDefinition> workspaces;
    private readonly Avalonia.DynamicWorkspaceHost workspaceHost;
    private DispatcherTimer? smokeTimer;

    public MainWindow()
        : this(DemoComposition.Create())
    {
    }

    private MainWindow(DemoComposition composition)
        : this(composition.Registry, composition.Workspaces)
    {
    }

    public MainWindow(TemplateRegistry registry, IReadOnlyList<WorkspaceDefinition> workspaces)
    {
        InitializeComponent();
        this.workspaces = workspaces;
        workspaceHost = new Avalonia.DynamicWorkspaceHost(registry);
        HostContainer.Content = workspaceHost;
        WorkspaceSelector.ItemsSource = workspaces.Select(workspace => workspace.DisplayName).ToArray();
        WorkspaceSelector.SelectedIndex = 0;

        if (Program.IsSmokeRun)
        {
            Opened += StartSmokeRun;
        }
    }

    private void WorkspaceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (WorkspaceSelector.SelectedIndex is var index && index >= 0 && index < workspaces.Count)
        {
            workspaceHost.ShowWorkspace(workspaces[index]);
        }
    }

    private void StartSmokeRun(object? sender, EventArgs e)
    {
        smokeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        smokeTimer.Tick += AdvanceSmokeRun;
        smokeTimer.Start();
    }

    private void AdvanceSmokeRun(object? sender, EventArgs e)
    {
        var definition = workspaces[WorkspaceSelector.SelectedIndex];
        var result = workspaceHost.ShowWorkspace(definition);
        Console.WriteLine(
            $"SMOKE {definition.TemplateCode}: {(result.IsSuccess ? "RESOLVED" : "SAFE_FAILURE")}");

        if (WorkspaceSelector.SelectedIndex == workspaces.Count - 1)
        {
            smokeTimer!.Stop();
            Close();
            return;
        }

        WorkspaceSelector.SelectedIndex++;
    }
}
