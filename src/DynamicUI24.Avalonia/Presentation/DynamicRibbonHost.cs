using ActiproSoftware.UI.Avalonia.Controls.Bars;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using DynamicUI24.Core.Ribbon;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Builds an Actipro Ribbon exclusively from resolved metadata.</summary>
public sealed class DynamicRibbonHost : UserControl
{
    private readonly RibbonDefinition definition;
    private readonly IReadOnlyList<WorkspaceDefinition> knownWorkspaces;
    private readonly DynamicRibbonResolver resolver;
    private readonly RibbonCommandDispatcher dispatcher;
    private readonly ILocalizationService localization;
    private readonly IIconRegistry icons;
    private RibbonResolutionContext context;
    private Ribbon? ribbon;

    public DynamicRibbonHost(
        RibbonDefinition definition,
        IEnumerable<WorkspaceDefinition> knownWorkspaces,
        RibbonResolutionContext context,
        DynamicRibbonResolver resolver,
        RibbonCommandDispatcher dispatcher,
        ILocalizationService localization,
        IIconRegistry icons)
    {
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.knownWorkspaces = (knownWorkspaces ?? throw new ArgumentNullException(nameof(knownWorkspaces))).ToArray();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.icons = icons ?? throw new ArgumentNullException(nameof(icons));
        localization.CultureChanged += (_, _) => Rebuild();
        Rebuild();
    }

    public ResolvedRibbon ResolvedRibbon { get; private set; } = null!;
    public string? SelectedTabCode { get; private set; }
    public event EventHandler<RibbonCommandResult>? CommandCompleted;

    public Task<RibbonCommandResult> ExecuteCommandAsync(string commandCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandCode);
        var command = ResolvedRibbon.Tabs.SelectMany(x => x.Groups).SelectMany(x => x.Commands)
            .FirstOrDefault(x => x.Definition.CommandCode.Equals(commandCode, StringComparison.OrdinalIgnoreCase));
        return command is null
            ? Task.FromResult(RibbonCommandResult.Unavailable("RIBBON_COMMAND_NOT_VISIBLE"))
            : dispatcher.DispatchAsync(command, new RibbonCommandExecutionContext(context, context.Workspace), cancellationToken);
    }

    public void UpdateContext(RibbonResolutionContext newContext)
    {
        context = newContext ?? throw new ArgumentNullException(nameof(newContext));
        Rebuild();
    }

    private void Rebuild()
    {
        if (ribbon?.SelectedItem is RibbonTabItem selected) SelectedTabCode = selected.Key;
        ResolvedRibbon = resolver.Resolve(definition, context, knownWorkspaces);
        ribbon = new Ribbon
        {
            IsOptionsButtonVisible = false,
            IsApplicationButtonVisible = false,
            QuickAccessToolBarMode = RibbonQuickAccessToolBarMode.None,
            CanChangeLayoutMode = false,
            IsCollapsible = false,
            IsMinimizable = false,
        };

        foreach (var resolvedTab in ResolvedRibbon.Tabs)
        {
            var tab = new RibbonTabItem
            {
                Key = resolvedTab.Definition.TabCode,
                Label = localization.Get(resolvedTab.Definition.DisplayNameKey),
                IsEnabled = resolvedTab.State == Core.Authorization.AuthorizationPresentationState.VisibleEnabled,
            };
            foreach (var resolvedGroup in resolvedTab.Groups)
            {
                var group = new RibbonGroup
                {
                    Key = resolvedGroup.Definition.GroupCode,
                    Label = localization.Get(resolvedGroup.Definition.DisplayNameKey),
                    IsEnabled = resolvedGroup.State == Core.Authorization.AuthorizationPresentationState.VisibleEnabled,
                };
                foreach (var resolvedCommand in resolvedGroup.Commands)
                    group.Items.Add(CreateButton(resolvedCommand));
                tab.Items.Add(group);
            }
            ribbon.Items.Add(tab);
        }

        var selectedTab = ribbon.Items.OfType<RibbonTabItem>()
            .FirstOrDefault(x => string.Equals(x.Key, SelectedTabCode, StringComparison.OrdinalIgnoreCase))
            ?? ribbon.Items.OfType<RibbonTabItem>().FirstOrDefault();
        if (selectedTab is not null)
        {
            selectedTab.IsSelected = true;
            SelectedTabCode = selectedTab.Key;
        }
        Content = ribbon;
    }

    private BarButton CreateButton(ResolvedRibbonCommand command)
    {
        var icon = CreateIcon(command.Definition.IconKey, 20);
        var button = new BarButton
        {
            Key = command.Definition.CommandCode,
            Label = localization.Get(command.Definition.DisplayNameKey),
            SmallIcon = icon,
            LargeIcon = CreateIcon(command.Definition.IconKey, 32),
            IsEnabled = command.IsEnabled,
        };
        AutomationProperties.SetName(button, button.Label);
        button.Click += async (_, _) =>
        {
            var result = await dispatcher.DispatchAsync(command,
                new RibbonCommandExecutionContext(context, context.Workspace));
            CommandCompleted?.Invoke(this, result);
        };
        return button;
    }

    private PathIcon CreateIcon(IconKey key, double size) => new()
    {
        Data = Geometry.Parse(icons.Resolve(key).SvgPathData),
        Width = size,
        Height = size,
    };
}
