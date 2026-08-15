using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Collections.Immutable;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Core.Setup;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Generic Setup surface. Application data and persistence enter only through metadata providers.</summary>
public sealed class SetupWorkspaceHost : Grid
{
    private static readonly WorkspaceDefinition SetupWorkspace = new("setup-internal", "Setup", StandardTemplateCodes.Setup);
    private readonly ILocalizationService localization;
    private readonly ISetupDefinitionProvider provider;
    private readonly SetupEditorRegistry editors;
    private readonly SetupDefinitionLifecycle lifecycle;
    private readonly SetupCategoryResolver categoryResolver = new();
    private readonly IReadOnlyList<SetupCategoryDefinition> categories;
    private readonly DynamicTreeHost categoryTree;
    private readonly ListBox definitionList = new();
    private readonly StackPanel editorPanel = new() { Spacing = 8 };
    private readonly StackPanel diagnosticsPanel = new() { Spacing = 4 };
    private readonly TextBlock heading = new();
    private readonly TextBlock definitionHeader = new();
    private readonly TextBlock status = new();
    private readonly TextBox search = new() { Watermark = "Search" };
    private readonly DynamicActionBarHost topActions;
    private readonly DynamicActionBarHost bottomActions;
    private readonly DynamicSplitNavigationHost splitLayout;
    private EffectiveAuthorizationContext? authorization;
    private CompanyDescriptor company;
    private SetupCategoryDefinition? selectedCategory;
    private bool suppressSelection;
    private ImmutableArray<string> visibleCategoryCodes = [];

    public SetupWorkspaceHost(IEnumerable<SetupCategoryDefinition> categories, ISetupDefinitionProvider provider,
        ISetupDefinitionValidator validator, SetupEditorRegistry editors, ILocalizationService localization,
        IIconRegistry icons, CompanyDescriptor company, EffectiveAuthorizationContext? authorization = null)
    {
        this.categories = (categories ?? throw new ArgumentNullException(nameof(categories))).ToArray();
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        this.editors = editors ?? throw new ArgumentNullException(nameof(editors));
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        ArgumentNullException.ThrowIfNull(icons);
        this.company = company ?? throw new ArgumentNullException(nameof(company));
        this.authorization = authorization;
        lifecycle = new(provider, validator ?? throw new ArgumentNullException(nameof(validator)));
        categoryTree = new(localization, icons, new TreeOverflowOptions(5, 5)) { ShowTitle = false };

        var commands = new ActionCommandRegistry();
        RegisterCommands(commands);
        var dispatcher = new ActionBarCommandDispatcher(new WorkspaceNavigationService([SetupWorkspace]),
            new SetupRefreshService(RefreshDefinitions), commands);
        topActions = new(dispatcher, localization, icons);
        bottomActions = new(dispatcher, localization, icons);
        splitLayout = new(new SplitNavigationLayoutState(260, 180, 520, 5));

        BuildLayout();
        categoryTree.NodeSelected += CategoryNodeSelected;
        definitionList.SelectionChanged += DefinitionSelectionChanged;
        search.TextChanged += (_, _) => RefreshDefinitions();
        localization.CultureChanged += (_, _) => RenderAllPreservingState();
        RenderAllPreservingState();
    }

    public SetupDefinitionLifecycle Lifecycle => lifecycle;
    public string? SelectedCategoryId => selectedCategory?.CategoryId;
    public ImmutableArray<string> VisibleCategoryCodes => visibleCategoryCodes;
    public int DefinitionCount => definitionList.ItemsSource?.Cast<object>().Count() ?? 0;
    public SetupEditorKind? LastEditorKind { get; private set; }
    public bool IsCandidateReadOnly => lifecycle.Buffer?.Candidate is { IsEditable: false } or { IsSystem: true };
    public bool HasResizableNavigationSplitter => splitLayout.IsRuntimeResizable;
    public double NavigationPaneWidth => splitLayout.NavigationWidth;
    public double ResizeNavigationPane(double requestedWidth) => splitLayout.ResizeNavigation(requestedWidth);

    public bool SelectCategory(string categoryId)
    {
        var category = categories.FirstOrDefault(x => x.CategoryId.Equals(categoryId, StringComparison.OrdinalIgnoreCase));
        if (category is null || !visibleCategoryCodes.Contains(category.CategoryCode)) return false;
        categoryTree.SelectNode(categoryId);
        ActivateCategory(category);
        return true;
    }
    public TreeChildWindow GetCategoryChildWindow(string? parentCategoryId) => categoryTree.GetChildWindow(parentCategoryId);
    public bool SetCategoryExpanded(string categoryId, bool isExpanded) => categoryTree.SetNodeExpanded(categoryId, isExpanded);
    public bool IsCategoryExpanded(string categoryId) => categoryTree.IsNodeExpanded(categoryId);
    public bool ShowMoreCategories(string? parentCategoryId) => categoryTree.ShowMore(parentCategoryId);
    public bool ShowLessCategories(string? parentCategoryId) => categoryTree.ShowLess(parentCategoryId);
    public bool SelectDefinition(string definitionId)
    {
        var row = definitionList.ItemsSource?.Cast<DefinitionRow>()
            .FirstOrDefault(x => x.Definition.DefinitionId.Equals(definitionId, StringComparison.OrdinalIgnoreCase));
        if (row is null) return false;
        definitionList.SelectedItem = row;
        return true;
    }
    public void SetCandidateValue(string fieldCode, object? value) => UpdateField(fieldCode, value);
    public Task<ActionCommandResult> ExecuteActionAsync(string actionCode) =>
        SetupActionBarDefinitions.Top.Actions.Any(x => x.ActionCode == actionCode)
            ? topActions.ExecuteActionAsync(actionCode)
            : bottomActions.ExecuteActionAsync(actionCode);

    public void UpdateContext(CompanyDescriptor newCompany, EffectiveAuthorizationContext? newAuthorization)
    {
        company = newCompany ?? throw new ArgumentNullException(nameof(newCompany));
        authorization = newAuthorization;
        RenderCategories();
        RefreshDefinitions();
    }

    private void BuildLayout()
    {
        var left = Frame(new StackPanel { Spacing = 8, Children = { new TextBlock { Text = "Setup" }, categoryTree } });
        var workspace = new Grid { RowDefinitions = new("Auto,Auto,*,Auto"), RowSpacing = 8 };
        workspace.Children.Add(topActions);
        Grid.SetRow(search, 1); workspace.Children.Add(search);
        var split = new Grid { ColumnDefinitions = new("3*,4*"), ColumnSpacing = 10 };
        definitionHeader.Bind(TextBlock.ForegroundProperty, definitionHeader.GetResourceObservable("DuiTextMutedBrush"));
        split.Children.Add(Frame(new StackPanel { Spacing = 6, Children = { heading, definitionHeader, definitionList } }));
        var detail = Frame(new ScrollViewer { Content = new StackPanel { Spacing = 12, Children = { editorPanel, diagnosticsPanel, status } } });
        Grid.SetColumn(detail, 1); split.Children.Add(detail);
        Grid.SetRow(split, 2); workspace.Children.Add(split);
        Grid.SetRow(bottomActions, 3); workspace.Children.Add(bottomActions);
        splitLayout.NavigationContent = left;
        splitLayout.WorkspaceContent = workspace;
        Children.Add(splitLayout);
    }

    private static Border Frame(Control content)
    {
        var border = new Border { Padding = new Thickness(10), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6), Child = content };
        border.Bind(BackgroundProperty, border.GetResourceObservable("DuiSurfaceRaisedBrush"));
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("DuiBorderBrush"));
        return border;
    }

    private void RenderCategories()
    {
        var preserve = selectedCategory?.CategoryId;
        var result = categoryResolver.Resolve(categories, authorization);
        visibleCategoryCodes = Flatten(result.Roots).Select(x => x.Definition.CategoryCode).ToImmutableArray();
        if (result.Diagnostics.Length == 0)
        {
            var definition = new TreeDefinition("setup-categories", "SETUP_CATEGORIES", 1, categories.Select(x =>
                new TreeNodeDefinition(x.CategoryId, x.CategoryCode, x.DisplayNameKey, x.ParentCategoryId,
                    x.IconKey, x.DisplayOrder, isVisible: x.IsVisible, permissionRequirement: x.PermissionRequirement)));
            categoryTree.Show(new DynamicTreeResolver().Resolve(definition,
                new TreeResolutionContext(company, authorization), []));
        }
        else
        {
            var empty = new TreeDefinition("setup-categories-invalid", "SETUP_CATEGORIES_INVALID", 1, []);
            categoryTree.Show(new(empty, [], result.Diagnostics.Select(x => new TreeDiagnostic(x.Code, x.Message)).ToImmutableArray()));
        }
        if (result.Diagnostics.Length > 0) status.Text = string.Join(" · ", result.Diagnostics.Select(x => x.Code));
        if (preserve is not null) SelectCategory(preserve);
        else if (result.Roots.FirstOrDefault()?.Definition is { } category)
        { categoryTree.SelectNode(category.CategoryId); ActivateCategory(category); }
    }

    private static IEnumerable<ResolvedSetupCategory> Flatten(IEnumerable<ResolvedSetupCategory> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children)) yield return child;
        }
    }

    private void CategoryNodeSelected(object? sender, TreeNodeSelectedEventArgs e)
    {
        if (suppressSelection) return;
        var category = categories.FirstOrDefault(x => x.CategoryId.Equals(e.Node.NodeId, StringComparison.OrdinalIgnoreCase));
        if (category is not null) ActivateCategory(category);
    }

    private void ActivateCategory(SetupCategoryDefinition category)
    {
        if (selectedCategory?.CategoryId.Equals(category.CategoryId, StringComparison.OrdinalIgnoreCase) == true) return;
        if (lifecycle.Buffer?.IsDirty == true)
        {
            status.Text = localization.Get(new("Setup.Dirty.Blocked"));
            suppressSelection = true;
            if (selectedCategory is not null) categoryTree.SelectNode(selectedCategory.CategoryId);
            suppressSelection = false;
            return;
        }
        selectedCategory = category;
        heading.Text = localization.Get(category.DisplayNameKey);
        RefreshDefinitions();
    }

    private void RefreshDefinitions()
    {
        if (selectedCategory is null) return;
        var selectedId = lifecycle.Buffer?.Source.DefinitionId;
        var query = provider.GetDefinitions(selectedCategory.CategoryId, selectedCategory.ScopeKey ?? company.CompanyId.Value)
            .OrderBy(x => x.DefinitionCode).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search.Text)) query = query.Where(x => x.DefinitionCode.Contains(search.Text, StringComparison.OrdinalIgnoreCase)
            || x.DisplayName.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase));
        var rows = query.ToArray();
        definitionList.ItemsSource = rows.Select(x => new DefinitionRow(x)).ToArray();
        if (selectedId is not null) definitionList.SelectedItem = definitionList.ItemsSource.Cast<DefinitionRow>()
            .FirstOrDefault(x => x.Definition.DefinitionId == selectedId);
        RefreshActions();
    }

    private void DefinitionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (suppressSelection || definitionList.SelectedItem is not DefinitionRow row) return;
        var decision = lifecycle.Select(row.Definition);
        if (decision == SetupNavigationDecision.BlockedByDirtyCandidate)
        {
            status.Text = localization.Get(new("Setup.Dirty.Blocked"));
            suppressSelection = true;
            definitionList.SelectedItem = definitionList.ItemsSource?.Cast<DefinitionRow>()
                .FirstOrDefault(x => x.Definition.DefinitionId == lifecycle.Buffer!.Source.DefinitionId);
            suppressSelection = false;
            return;
        }
        RenderEditor();
    }

    private void RenderEditor()
    {
        editorPanel.Children.Clear(); diagnosticsPanel.Children.Clear();
        if (lifecycle.Buffer is not { } buffer) { RefreshActions(); return; }
        var definition = buffer.Candidate;
        editorPanel.Children.Add(new TextBlock { Text = $"{definition.DefinitionCode} · v{definition.Version} · {definition.Status}" });
        var descriptor = editors.Resolve(definition);
        LastEditorKind = descriptor.Kind;
        if (descriptor.Kind == SetupEditorKind.Unavailable)
        {
            editorPanel.Children.Add(new TextBlock { Text = localization.Get(descriptor.MessageKey ?? new("Setup.Editor.Unavailable")), TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
            RefreshActions(); return;
        }
        foreach (var field in descriptor.Fields)
        {
            var label = new TextBlock { Text = localization.Get(field.DisplayNameKey) + (field.IsRequired ? " *" : "") };
            var control = CreateFieldControl(field, definition.Values.GetValueOrDefault(field.FieldCode),
                !definition.IsEditable || definition.IsSystem || field.IsReadOnly);
            editorPanel.Children.Add(new StackPanel { Spacing = 3, Children = { label, control } });
        }
        RefreshActions();
    }

    private Control CreateFieldControl(EditorFieldDefinition field, object? value, bool readOnly)
    {
        if (field.FieldType == EditorFieldType.Boolean)
        {
            var check = new CheckBox { IsChecked = value as bool? ?? false, IsEnabled = !readOnly };
            check.IsCheckedChanged += (_, _) => UpdateField(field.FieldCode, check.IsChecked == true);
            return check;
        }
        if (field.FieldType == EditorFieldType.Choice)
        {
            var combo = new ComboBox { ItemsSource = field.Choices.Select(x => x.Value).ToArray(), SelectedItem = value?.ToString(), IsEnabled = !readOnly };
            combo.SelectionChanged += (_, _) => UpdateField(field.FieldCode, combo.SelectedItem);
            return combo;
        }
        var text = new TextBox { Text = value?.ToString() ?? field.DefaultValue?.ToString() ?? string.Empty,
            IsReadOnly = readOnly, AcceptsReturn = field.FieldType == EditorFieldType.MultilineText,
            MinHeight = field.FieldType == EditorFieldType.MultilineText ? 70 : 0 };
        text.TextChanged += (_, _) => UpdateField(field.FieldCode, ConvertValue(field.FieldType, text.Text));
        return text;
    }

    private static object? ConvertValue(EditorFieldType fieldType, string? text) => fieldType switch
    {
        EditorFieldType.Integer when int.TryParse(text, out var integer) => integer,
        EditorFieldType.Decimal when decimal.TryParse(text, out var number) => number,
        EditorFieldType.Date or EditorFieldType.OptionalDate when DateOnly.TryParse(text, out var date) => date,
        EditorFieldType.OptionalDate when string.IsNullOrWhiteSpace(text) => null,
        _ => text,
    };

    private void UpdateField(string code, object? value)
    {
        lifecycle.Buffer?.SetValue(code, value);
        status.Text = localization.Get(new("Setup.Dirty.Pending"));
        RefreshActions();
    }

    private void RegisterCommands(ActionCommandRegistry commands)
    {
        commands.Register("SETUP.NEW", (_, _) => Execute(() => lifecycle.CreateDraft(selectedCategory!.CategoryId,
            selectedCategory.DefinitionType ?? "GENERIC", $"NEW_{DateTime.UtcNow:HHmmss}", "New definition")));
        commands.Register("SETUP.EDIT", (_, _) => Execute(() => status.Text = localization.Get(new("Setup.Editor.Edit"))));
        commands.Register("SETUP.CLONE", (_, _) => Execute(() => lifecycle.Clone(Guid.NewGuid().ToString("N"),
            lifecycle.Buffer!.Source.DefinitionCode + "_COPY")));
        commands.Register("SETUP.VALIDATE", (_, _) => Execute(() => RenderDiagnostics(lifecycle.Validate())));
        commands.Register("SETUP.PUBLISH", (_, _) => Execute(() => lifecycle.Publish()));
        commands.Register("SETUP.RETIRE", (_, _) => Execute(() => lifecycle.Retire()));
        commands.Register("SETUP.CANCEL", (_, _) => Execute(() => lifecycle.CancelChanges()));
        commands.Register("SETUP.SAVE", (_, _) => Execute(() => lifecycle.SaveDraft()));
    }

    private Task<ActionCommandResult> Execute(Action action)
    {
        try { action(); RenderEditor(); RefreshDefinitions(); return Task.FromResult(ActionCommandResult.Success()); }
        catch (Exception ex) { status.Text = ex.Message; return Task.FromResult(ActionCommandResult.Failed(message: ex.Message)); }
    }

    private void RenderDiagnostics(SetupValidationResult result)
    {
        diagnosticsPanel.Children.Clear();
        foreach (var diagnostic in result.Diagnostics)
            diagnosticsPanel.Children.Add(new TextBlock { Text = $"{diagnostic.Severity} · {diagnostic.Code} · {diagnostic.Message ?? localization.Get(diagnostic.MessageKey)}" });
        status.Text = result.IsValid ? localization.Get(new("Setup.Validation.Valid")) : localization.Get(new("Setup.Validation.Invalid"));
    }

    private void RefreshActions()
    {
        var definition = lifecycle.Buffer?.Candidate;
        var selection = definition is null ? 0 : 1;
        var presentation = PresentationState.Ready;
        var context = new ActionBarResolutionContext(company, SetupWorkspace, StandardTemplateCodes.Setup, authorization,
            new(selection), presentation, new(provider.GetDefinitions(selectedCategory?.CategoryId ?? string.Empty,
                selectedCategory?.ScopeKey ?? company.CompanyId.Value).Count,
                SelectedRows: selection, ErrorCount: lifecycle.LastValidation?.Diagnostics.Count(x => x.Severity == SetupDiagnosticSeverity.Error),
                WarningCount: lifecycle.LastValidation?.Diagnostics.Count(x => x.Severity == SetupDiagnosticSeverity.Warning),
                PendingChangeCount: lifecycle.Buffer?.IsDirty == true ? 1 : 0,
                ReadOnlyState: definition is { IsEditable: false } or { IsSystem: true }));
        var resolver = new DynamicActionBarResolver();
        var top = resolver.Resolve(SetupActionBarDefinitions.Top, context);
        var bottom = resolver.Resolve(SetupActionBarDefinitions.Bottom, context);
        top = top with { Actions = top.Actions.Select(x => ApplyLifecycleState(x, definition)).ToImmutableArray() };
        bottom = bottom with { Actions = bottom.Actions.Select(x => ApplyLifecycleState(x, definition)).ToImmutableArray() };
        var execution = new ActionCommandExecutionContext(context);
        topActions.Show(top, execution); bottomActions.Show(bottom, execution);
    }

    private ResolvedAction ApplyLifecycleState(ResolvedAction action, SetupDefinitionDescriptor? definition)
    {
        var code = action.Definition.ActionCode;
        var enabled = code switch
        {
            "SETUP.NEW" => selectedCategory?.DefinitionType is not null,
            "SETUP.EDIT" => definition is { IsEditable: true, IsSystem: false, IsPublished: false },
            "SETUP.CLONE" => definition?.CloneAllowed == true,
            "SETUP.VALIDATE" => definition is { IsEditable: true, IsSystem: false },
            "SETUP.PUBLISH" => definition is { IsEditable: true, IsSystem: false } && lifecycle.Buffer?.Candidate.ValidationState == SetupValidationState.Valid,
            "SETUP.RETIRE" => definition?.Status == SetupDefinitionStatus.Published && lifecycle.Buffer?.IsDirty == false,
            "SETUP.CANCEL" => lifecycle.Buffer?.IsDirty == true,
            "SETUP.SAVE" => lifecycle.Buffer?.IsDirty == true && definition is { IsEditable: true, IsSystem: false },
            _ => true,
        };
        return enabled ? action : action with { State = AuthorizationPresentationState.VisibleDisabled };
    }

    private void RenderAllPreservingState()
    {
        definitionHeader.Text = localization.Get(new("Setup.List.Columns"));
        search.Watermark = localization.Get(new("Setup.Search"));
        RenderCategories(); RenderEditor(); RefreshActions();
    }

    private sealed record DefinitionRow(SetupDefinitionDescriptor Definition)
    {
        public override string ToString() => $"{Definition.DefinitionCode,-18} {Definition.DisplayName,-24} {Definition.DefinitionType,-14} v{Definition.Version}  {Definition.Status}  {Definition.EffectiveFrom:yyyy-MM-dd}  {Definition.EffectiveTo:yyyy-MM-dd}";
    }

    private sealed class SetupRefreshService(Action refresh) : IActionRefreshService
    {
        public Task<ActionCommandResult> RefreshAsync(ActionCommandExecutionContext context, CancellationToken cancellationToken = default)
        { refresh(); return Task.FromResult(ActionCommandResult.Success()); }
    }

}
