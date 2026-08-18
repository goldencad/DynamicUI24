using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Globalization;
using DynamicUI24.Avalonia.Presentation.Editors;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

internal enum DemoAuthoringProfile { Viewer, Editor, Administrator }
internal sealed record DemoWorkspaceResolution(IReadOnlyList<WorkspaceDefinition> VisibleWorkspaces,
    WorkspaceDefinition? ActiveWorkspace, bool WasEvicted);

internal sealed class DemoProfileContext
{
    private static readonly PermissionCode View = new("PEOPLE.VIEW");
    private static readonly PermissionCode Edit = new("PEOPLE.EDIT");
    private static readonly PermissionCode Insights = new("INSIGHTS.VIEW");
    private readonly GenerationSafeUiAuthorizationService authorization = new(new DefaultUiAuthorizationResolver());
    private long generation = 1;

    public DemoAuthoringProfile CurrentProfile { get; private set; } = DemoAuthoringProfile.Viewer;
    public long Generation => generation;
    public event EventHandler? Changed;

    public void Select(DemoAuthoringProfile profile)
    {
        if (CurrentProfile == profile) return;
        CurrentProfile = profile;
        generation = checked(generation + 1);
        authorization.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public UserSecurityContext Security => DemoUiAuthoring.Security(CurrentProfile, generation);

    public EffectiveAuthorizationContext Merge(EffectiveAuthorizationContext? applicationContext, CompanyId companyId)
    {
        var permissions = (applicationContext?.PermissionCodes ?? ImmutableHashSet<PermissionCode>.Empty)
            .Concat(Security.Permissions).ToImmutableHashSet();
        var capabilities = (applicationContext?.CapabilityCodes ?? ImmutableHashSet<CapabilityCode>.Empty)
            .Concat(Security.Capabilities).ToImmutableHashSet();
        return new(new("demo-user"), companyId, permissions, capabilities, $"demo-profile-{generation}");
    }

    public async ValueTask<bool> CanOpenAuthoringAsync(CompanyId companyId, CancellationToken cancellationToken = default)
    {
        var context = new UiAuthorizationContext(Security, companyId, null, new("DEMO.UI"), new(1),
            1, 1, generation, PrivacyMode.On);
        var result = await authorization.ResolveAsync(new(new("WORKSPACE.UI_AUTHORING"),
            new(Capability: StandardUiCapabilities.CanOpenUiAuthoring), StandardUiCapabilities.CanOpenUiAuthoring,
            context), cancellationToken);
        return result.IsCurrent(context) && result.State == UiAuthorizationState.Enabled;
    }

    public bool IsWorkspaceVisible(WorkspaceDefinition workspace) =>
        !workspace.WorkspaceId.Equals("ui-authoring-demo", StringComparison.OrdinalIgnoreCase) ||
        Security.Capabilities.Contains(StandardUiCapabilities.CanOpenUiAuthoring);

    public DemoWorkspaceResolution ResolveWorkspaces(IEnumerable<WorkspaceDefinition> workspaces, string? activeWorkspaceCode)
    {
        var visible = workspaces.Where(IsWorkspaceVisible).ToArray();
        var active = activeWorkspaceCode is null ? null : visible.FirstOrDefault(x =>
            x.WorkspaceId.Equals(activeWorkspaceCode, StringComparison.OrdinalIgnoreCase));
        var evicted = activeWorkspaceCode is not null && active is null;
        return new(visible, active ?? (evicted ? visible.FirstOrDefault() : null), evicted);
    }
}

internal static class DemoUiAuthoring
{
    public static UiDefinition CreateDefinition()
    {
        var workspace = new UiElementDefinition(new("WORKSPACE.PEOPLE"), UiElementKind.Workspace,
            new("Authoring.People"), authorization: new(new("PEOPLE.VIEW")), helpContextCode: new("DEMO.PEOPLE"));
        var field = new UiElementDefinition(new("FIELD.PERSON_NAME"), UiElementKind.Field,
            new("Authoring.PersonName"), workspace.Code, editor: new(new("PERSON_NAME"), new("PERSON_NAME"),
                EditorValueType.String, EditorKind.Text, helpContextCode: new("DEMO.PERSON.NAME")),
            authorization: new(Permission: new("PEOPLE.EDIT"), DeniedBehavior: UnauthorizedBehavior.ReadOnly));
        return new(new("DEMO.UI"), new(1), 1, DateTimeOffset.UnixEpoch,
        [
            workspace,
            new(new("WORKSPACE.INSIGHTS"), UiElementKind.Workspace, new("Authoring.Insights"), authorization: new(new("INSIGHTS.VIEW"))),
            new(new("RIBBON.PEOPLE"), UiElementKind.RibbonTab, new("Authoring.Ribbon"), workspace.Code),
            new(new("COMMAND.EXPORT"), UiElementKind.Command, new("Authoring.Export"), workspace.Code,
                "DEMO.EXPORT.VIEW", authorization: new(Capability: StandardUiCapabilities.CanExport),
                eligibleSurfaces: [UiSurface.Ribbon, UiSurface.Menu, UiSurface.ActionBar, UiSurface.CommandPalette]),
            new(new("FORM.PERSON"), UiElementKind.Form, new("Authoring.Form"), workspace.Code), field,
            new(new("GRID.PEOPLE"), UiElementKind.Grid, new("Authoring.Grid"), workspace.Code),
            new(new("GRID.PEOPLE.NAME"), UiElementKind.GridColumn, new("Authoring.NameColumn"), new("GRID.PEOPLE"),
                "PERSON_NAME", layout: new(180, 80, 480), personalization: new(true, true, true, true, true)),
            new(new("REPORT.PEOPLE"), UiElementKind.Report, new("Authoring.Report"), workspace.Code, "PEOPLE_REPORT"),
            new(new("PANE.CONTEXT"), UiElementKind.Pane, new("Authoring.ContextPane"), workspace.Code,
                layout: new(320, 240, 560, DefaultVisible: true, Collapsible: true, UserResizable: true)),
            new(new("COMPOSER.NOTES"), UiElementKind.Composer, new("Authoring.Composer"), workspace.Code,
                "SUBMIT=DEMO.NOTES.SUBMIT;ATTACHMENTS=IMAGE,DOCUMENT")
        ], "Initial demo definition");
    }

    public static UserSecurityContext Security(DemoAuthoringProfile profile, long generation) => profile switch
    {
        DemoAuthoringProfile.Viewer => new("VIEWER", generation, ImmutableHashSet.Create(new PermissionCode("PEOPLE.VIEW")),
            ImmutableHashSet<CapabilityCode>.Empty),
        DemoAuthoringProfile.Editor => new("EDITOR", generation,
            ImmutableHashSet.Create(new PermissionCode("PEOPLE.VIEW"), new PermissionCode("PEOPLE.EDIT")),
            ImmutableHashSet.Create(StandardUiCapabilities.CanEdit)),
        _ => new("ADMINISTRATOR", generation,
            ImmutableHashSet.Create(new PermissionCode("PEOPLE.VIEW"), new PermissionCode("PEOPLE.EDIT"), new PermissionCode("INSIGHTS.VIEW")),
            ImmutableHashSet.Create(StandardUiCapabilities.CanEdit, StandardUiCapabilities.CanExport,
                StandardUiCapabilities.CanOpenUiAuthoring, StandardUiCapabilities.CanEditUiDefinition,
                StandardUiCapabilities.CanPreviewUiDefinition, StandardUiCapabilities.CanPublishUiDefinition,
                StandardUiCapabilities.CanRollbackUiDefinition, StandardUiCapabilities.CanEditAuthorizationBindings))
    };
}

internal sealed class DemoUiAuthoringSession
{
    private readonly InMemoryUiDefinitionRepository repository;
    private readonly UiDefinitionLifecycleService lifecycle;
    private readonly Func<UserSecurityContext> security;
    private readonly SemaphoreSlim publishGate = new(1, 1);
    private string pendingPublishRequestId = Guid.NewGuid().ToString("N");

    public DemoUiAuthoringSession(Func<UserSecurityContext> security)
    {
        this.security = security;
        ActiveDefinition = DemoUiAuthoring.CreateDefinition();
        repository = new([ActiveDefinition]);
        lifecycle = new(repository, new UiDefinitionValidator());
        Draft = new(ActiveDefinition);
    }

    public UiDefinition ActiveDefinition { get; private set; }
    public UiDefinitionDraft Draft { get; private set; }
    public UiDefinition? LastPreview { get; private set; }
    public UiDefinitionValidationResult? LastValidation { get; private set; }
    public IReadOnlyList<UiDefinitionVersionInfo> Versions { get; private set; } = [];

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default) =>
        Versions = await repository.GetVersionsAsync(ActiveDefinition.Code, cancellationToken);

    public void EditSafeLabel(UiElementCode code, string value)
    {
        Demand(StandardUiCapabilities.CanEditUiDefinition);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var element = Find(code);
        Draft.Upsert(Copy(element, titleKey: new(value.Trim())));
        EnsurePendingPublishIdentity();
        LastValidation = null;
    }

    public void SetMissingParentInvalid(UiElementCode code, bool invalid)
    {
        Demand(StandardUiCapabilities.CanEditUiDefinition);
        var element = Find(code);
        var published = ActiveDefinition.Elements.First(x => x.Code == code);
        Draft.Upsert(Copy(element, parentCode: invalid ? new("MISSING.PARENT") : published.ParentCode,
            replaceParent: true));
        EnsurePendingPublishIdentity();
        LastValidation = null;
    }

    public bool Undo() { Demand(StandardUiCapabilities.CanEditUiDefinition); LastValidation = null; return Draft.Undo(); }
    public bool Redo() { Demand(StandardUiCapabilities.CanEditUiDefinition); LastValidation = null; return Draft.Redo(); }

    public async ValueTask<UiDefinitionValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        Demand(StandardUiCapabilities.CanEditUiDefinition);
        LastValidation = await lifecycle.ValidateAsync(Draft, "Demo administrator", cancellationToken);
        return LastValidation;
    }

    public async ValueTask<UiDefinition> PreviewAsync(CancellationToken cancellationToken = default)
    {
        Demand(StandardUiCapabilities.CanPreviewUiDefinition);
        LastPreview = await lifecycle.PreviewAsync(Draft, "Demo administrator", cancellationToken);
        return LastPreview;
    }

    public async ValueTask<UiDefinition> PublishAsync(CancellationToken cancellationToken = default)
    {
        Demand(StandardUiCapabilities.CanPublishUiDefinition);
        if (!await publishGate.WaitAsync(0, cancellationToken)) throw new InvalidOperationException("UI_DEFINITION_PUBLISH_IN_PROGRESS");
        try
        {
            LastValidation = await lifecycle.ValidateAsync(Draft, "Demo administrator", cancellationToken);
            if (!LastValidation.CanPublish) throw new InvalidOperationException("UI_DEFINITION_VALIDATION_FAILED");
            var published = await lifecycle.PublishAsync(Draft, "Safe label changed", "Demo administrator",
                cancellationToken, pendingPublishRequestId);
            var versions = await repository.GetVersionsAsync(published.Code, cancellationToken);
            // Rebase only after the repository returned one atomically active published version.
            ActiveDefinition = published;
            Versions = versions;
            Draft = new(ActiveDefinition);
            pendingPublishRequestId = Guid.NewGuid().ToString("N");
            LastPreview = null;
            return ActiveDefinition;
        }
        finally { publishGate.Release(); }
    }

    public async ValueTask<UiDefinition> RollbackPreviousAsync(CancellationToken cancellationToken = default)
    {
        Demand(StandardUiCapabilities.CanRollbackUiDefinition);
        Versions = await repository.GetVersionsAsync(ActiveDefinition.Code, cancellationToken);
        var previous = Versions.Where(x => x.Version.Value < ActiveDefinition.Version.Value)
            .OrderByDescending(x => x.Version.Value).FirstOrDefault()
            ?? throw new InvalidOperationException("UI_DEFINITION_PREVIOUS_VERSION_NOT_FOUND");
        await lifecycle.RollbackAsync(ActiveDefinition.Code, previous.Version, "Demo administrator", cancellationToken);
        ActiveDefinition = await repository.GetActiveAsync(ActiveDefinition.Code, cancellationToken)
            ?? throw new InvalidOperationException("UI_DEFINITION_NOT_FOUND");
        Versions = await repository.GetVersionsAsync(ActiveDefinition.Code, cancellationToken);
        Draft = new(ActiveDefinition);
        pendingPublishRequestId = Guid.NewGuid().ToString("N");
        LastPreview = null; LastValidation = null;
        return ActiveDefinition;
    }

    public string ActiveLabel(UiElementCode code) => ActiveDefinition.Elements.First(x => x.Code == code).TitleKey.Value;
    public string DraftLabel(UiElementCode code) => Find(code).TitleKey.Value;

    private UiElementDefinition Find(UiElementCode code) => Draft.Elements.First(x => x.Code == code);
    private void EnsurePendingPublishIdentity()
    { if (!Draft.IsDirty) pendingPublishRequestId = Guid.NewGuid().ToString("N"); }
    private void Demand(CapabilityCode capability)
    { if (!security().Capabilities.Contains(capability)) throw new UnauthorizedAccessException("UI_AUTHORING_CAPABILITY_DENIED"); }
    private static UiElementDefinition Copy(UiElementDefinition source, LocalizationKey? titleKey = null,
        UiElementCode? parentCode = null, bool replaceParent = false) => new(source.Code, source.Kind,
        titleKey ?? source.TitleKey, replaceParent ? parentCode : source.ParentCode, source.SemanticReference,
        source.Editor, source.HelpContextCode, source.Authorization, source.Layout, source.Personalization,
        source.EligibleSurfaces, source.IsSensitive);
}

/// <summary>Neutral three-pane demo. It edits semantic draft metadata, never controls.</summary>
internal sealed class DemoUiAuthoringWorkspace : UserControl
{
    private readonly DemoUiAuthoringSession session;
    private readonly ListBox tree = new();
    private readonly ContentControl inspector = new();
    private readonly Border preview = new() { Padding = new Thickness(20) };
    private readonly TextBlock status = new();
    private readonly TextBlock lifecycleStatus = new() { TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
    private readonly ListBox history = new() { MaxHeight = 130 };
    private UiElementCode selectedCode;
    private IReadOnlyList<UiElementDefinition> visibleElements = [];

    public DemoUiAuthoringWorkspace(Func<UserSecurityContext> security)
    {
        session = new(security);
        visibleElements = session.Draft.Elements;
        tree.ItemsSource = Items(visibleElements);
        tree.SelectionChanged += (_, _) => Select(tree.SelectedIndex);
        var search = new TextBox { Watermark = "Search semantic code, type, feature or permission…" };
        search.TextChanged += (_, _) => { visibleElements = session.Draft.Elements.Where(x => Matches(x, search.Text)).ToArray(); tree.ItemsSource = Items(visibleElements); };
        var validate = new Button { Content = "Validate" }; validate.Click += async (_, _) => await ValidateAsync();
        var previewButton = new Button { Content = "Preview Draft" }; previewButton.Click += async (_, _) => await PreviewAsync();
        var publish = new Button { Content = "Publish" }; publish.Click += async (_, _) => await PublishAsync();
        var rollback = new Button { Content = "Activate Previous Version" }; rollback.Click += async (_, _) => await RollbackAsync();
        var undo = new Button { Content = "Undo" }; undo.Click += (_, _) => { session.Undo(); RefreshAll(); };
        var redo = new Button { Content = "Redo" }; redo.Click += (_, _) => { session.Redo(); RefreshAll(); };
        var toolbar = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8,
            Children = { validate, previewButton, publish, rollback, undo, redo, status } };
        var panes = new Grid { ColumnDefinitions = new("240,*,300"), ColumnSpacing = 12 };
        panes.Children.Add(Panel("Semantic UI Tree", tree, 0)); panes.Children.Add(Panel("Preview / Published Runtime", preview, 1));
        panes.Children.Add(Panel("Definition Inspector", new StackPanel { Spacing = 10, Children = { lifecycleStatus, inspector, new TextBlock { Text = "Version History", FontWeight = global::Avalonia.Media.FontWeight.SemiBold }, history } }, 2));
        Content = new Grid { Margin = new Thickness(16), RowDefinitions = new("Auto,Auto,*"), RowSpacing = 10, Children = { At(search, 0), At(toolbar, 1), At(panes, 2) } };
        _ = InitializeAsync();
    }
    private async Task InitializeAsync() { await session.InitializeAsync(); selectedCode = session.Draft.Elements[0].Code; tree.SelectedIndex = 0; RefreshAll(); }
    private static Control At(Control control, int row) { Grid.SetRow(control, row); return control; }
    private static Border Panel(string title, Control content, int column)
    { var border = new Border { Padding = new Thickness(12), BorderThickness = new Thickness(1), Child = new StackPanel { Spacing = 10, Children = { new TextBlock { Text = title, FontSize = 18, FontWeight = global::Avalonia.Media.FontWeight.SemiBold }, content } } }; Grid.SetColumn(border, column); return border; }
    private static bool Matches(UiElementDefinition x, string? text) => string.IsNullOrWhiteSpace(text) ||
        x.Code.Value.Contains(text, StringComparison.OrdinalIgnoreCase) || x.Kind.ToString().Contains(text, StringComparison.OrdinalIgnoreCase) ||
        (x.Authorization?.Feature?.Value.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (x.Authorization?.Permission?.Value.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false);
    private static string[] Items(IEnumerable<UiElementDefinition> elements) => elements.Select(x => $"{x.Kind} · {x.Code.Value}").ToArray();
    private void Select(int index) { if (index < 0 || index >= visibleElements.Count) return; selectedCode = visibleElements[index].Code; BuildInspector(); }
    private void BuildInspector()
    {
        var element = session.Draft.Elements.First(x => x.Code == selectedCode);
        var definition = new EditorDefinition(new("AUTHORING.SAFE_LABEL"), new(selectedCode.Value), EditorValueType.String,
            EditorKind.Text, chrome: new(new("Safe Demo label"), new("Enter a safe localized label")),
            validation: new(IsRequired: true, MaximumLength: 120));
        var editorState = new EditorRuntimeState(definition, element.TitleKey.Value);
        var presenter = new AvaloniaEditorPresenter(definition, editorState,
            new EditorResolver().Resolve(definition, EditorPlatformCapabilities.AllNative), CultureInfo.CurrentCulture);
        presenter.RefreshLocalizedPresentation(CultureInfo.CurrentCulture, key => key.Value);
        var apply = new Button { Content = "Apply Draft Edit" };
        apply.Click += async (_, _) => { if (await presenter.CommitAsync()) { session.EditSafeLabel(selectedCode, editorState.CommittedValue?.ToString() ?? string.Empty); status.Text = "Draft metadata updated"; RefreshAll(rebuildInspector: false); } };
        var invalid = new CheckBox { Content = "Demo invalid case: missing parent reference" };
        invalid.IsChecked = element.ParentCode?.Value == "MISSING.PARENT";
        invalid.IsCheckedChanged += (_, _) => { session.SetMissingParentInvalid(selectedCode, invalid.IsChecked == true); status.Text = "Draft metadata updated"; RefreshAll(rebuildInspector: false); };
        inspector.Content = new StackPanel { Spacing = 8, Children =
        {
            new TextBlock { Text = $"General · {element.Code.Value}", FontWeight = global::Avalonia.Media.FontWeight.SemiBold }, presenter, apply,
            new Expander { Header = "Advanced validation", Content = invalid },
            new TextBlock { Text = $"Layout width: {element.Layout.DefaultWidth?.ToString() ?? "Auto"}\nEditor: {element.Editor?.EditorCode.Value ?? "—"}\nPermission: {element.Authorization?.Permission?.Value ?? "—"}\nCapability: {element.Authorization?.Capability?.Value ?? "—"}\nHelp: {element.HelpContextCode?.Value ?? "—"}", TextWrapping = global::Avalonia.Media.TextWrapping.Wrap }
        } };
    }
    private async Task ValidateAsync() { var result = await session.ValidateAsync(); status.Text = result.CanPublish ? "Validation PASS" : $"Validation BLOCKED · {string.Join(", ", result.Diagnostics.Where(x => x.Severity == UiDefinitionDiagnosticSeverity.Error).Select(x => x.Code))}"; RefreshLifecycle(); }
    private async Task PreviewAsync() { await session.PreviewAsync(); status.Text = "Draft preview rendered · active version unchanged"; RefreshPreview(); RefreshLifecycle(); }
    private async Task PublishAsync() { try { await session.PublishAsync(); status.Text = $"Published and activated {session.ActiveDefinition.Version}"; RefreshAll(); } catch (InvalidOperationException ex) { status.Text = $"Publish blocked · {ex.Message}"; RefreshLifecycle(); } }
    private async Task RollbackAsync() { try { await session.RollbackPreviousAsync(); status.Text = $"Activated {session.ActiveDefinition.Version}; newer history retained"; selectedCode = session.Draft.Elements[0].Code; RefreshAll(); } catch (InvalidOperationException ex) { status.Text = $"Rollback unavailable · {ex.Message}"; } }
    private void RefreshAll(bool rebuildInspector = true) { visibleElements = session.Draft.Elements; tree.ItemsSource = Items(visibleElements); tree.SelectedIndex = Math.Max(0, visibleElements.ToList().FindIndex(x => x.Code == selectedCode)); RefreshLifecycle(); RefreshPreview(); if (rebuildInspector) BuildInspector(); }
    private void RefreshLifecycle() { lifecycleStatus.Text = $"Definition: {session.ActiveDefinition.Code.Value}\nActive: {session.ActiveDefinition.Version}\nDraft base: {session.Draft.BasedOnVersion}\nDraft: {(session.Draft.IsDirty ? "Modified" : "Clean")}"; history.ItemsSource = session.Versions.OrderByDescending(x => x.Version.Value).Select(x => $"{x.Version}{(x.IsActive ? " (active)" : "")} · {x.SafeChangeSummary}").ToArray(); }
    private void RefreshPreview()
    {
        var draftLabel = session.LastPreview?.Elements.FirstOrDefault(x => x.Code == selectedCode)?.TitleKey.Value;
        preview.Child = new StackPanel { Spacing = 14, Children =
        {
            new TextBlock { Text = session.LastPreview is null ? "DRAFT PREVIEW · not rendered" : "DRAFT PREVIEW", FontWeight = global::Avalonia.Media.FontWeight.Bold },
            new TextBlock { Text = draftLabel ?? "Click Preview Draft to render current Draft metadata", FontSize = 22, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
            new TextBlock { Text = "PUBLISHED RUNTIME", FontWeight = global::Avalonia.Media.FontWeight.Bold },
            new TextBlock { Text = $"{session.ActiveDefinition.Version} · {session.ActiveLabel(selectedCode)}", FontSize = 22, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
            new TextBlock { Text = "Preview does not activate a version or persist business data." }
        } };
    }
}
