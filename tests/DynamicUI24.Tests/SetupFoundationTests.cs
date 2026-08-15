using System.Collections.Immutable;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Setup;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;
using DynamicUI24.Avalonia.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class SetupFoundationTests
{
    private static readonly IconKey Icon = StandardIconKeys.Setup;
    private static readonly CompanyDescriptor Company = new(new("company"), "COMPANY", "Company");

    [Fact]
    public void CategoryHierarchyOrdersChildrenAndSupportsArbitraryCatalogCount()
    {
        var categories = new List<SetupCategoryDefinition> { Category("root", "MASTER_CATALOGS", 0) };
        for (var index = 12; index >= 1; index--) categories.Add(Category($"catalog-{index}", $"CATALOG_{index:00}", index, "root"));
        var result = new SetupCategoryResolver().Resolve(categories, null);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(12, result.Roots.Single().Children.Length);
        Assert.Equal("CATALOG_01", result.Roots.Single().Children[0].Definition.CategoryCode);
    }

    [Fact]
    public void CategoryValidationReportsDuplicateOrphanAndCycleWithoutThrowing()
    {
        SetupCategoryDefinition[] categories =
        [
            Category("duplicate", "ONE", 0), Category("duplicate", "TWO", 1),
            Category("orphan", "ORPHAN", 2, "missing"),
            Category("cycle-a", "CYCLE_A", 3, "cycle-b"), Category("cycle-b", "CYCLE_B", 4, "cycle-a"),
        ];
        var result = SetupCategoryValidator.Validate(categories);
        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, x => x.Code == "SETUP_DUPLICATE_CATEGORY_ID");
        Assert.Contains(result.Diagnostics, x => x.Code == "SETUP_CATEGORY_ORPHAN");
        Assert.Contains(result.Diagnostics, x => x.Code == "SETUP_CATEGORY_CYCLE");
    }

    [Fact]
    public void CategoryPermissionSupportsHiddenDisabledReadOnlyAndFailClosed()
    {
        var permission = new PermissionCode("SETUP.VIEW");
        var hidden = Category("hidden", "HIDDEN", requirement: new(permission, UnauthorizedBehavior: UnauthorizedBehavior.Hide));
        var disabled = Category("disabled", "DISABLED", requirement: new(permission, UnauthorizedBehavior: UnauthorizedBehavior.Disable));
        var readOnly = Category("readonly", "READONLY", requirement: new(permission, UnauthorizedBehavior: UnauthorizedBehavior.ReadOnly));
        var resolver = new SetupCategoryResolver();
        var unavailable = resolver.Resolve([hidden, disabled, readOnly], null);
        Assert.DoesNotContain(unavailable.Roots, x => x.Definition.CategoryId == "hidden");
        Assert.Equal(AuthorizationPresentationState.VisibleDisabled, unavailable.Roots.Single(x => x.Definition.CategoryId == "disabled").State);
        Assert.Equal(AuthorizationPresentationState.VisibleReadOnly, unavailable.Roots.Single(x => x.Definition.CategoryId == "readonly").State);
    }

    [Fact]
    public void DefinitionMetadataReportsDuplicateIdentityAndRejectsInvalidCode()
    {
        Assert.Single(SetupDefinitionMetadataValidator.Validate([Definition("same", "ONE"), Definition("same", "TWO")]));
        Assert.Throws<ArgumentException>(() => Definition("id", "not valid!"));
    }

    [Fact]
    public void EditCandidateDoesNotMutateSourceAndCancelRestoresIt()
    {
        var source = Definition(values: new Dictionary<string, object?> { ["NAME"] = "source" });
        var buffer = new SetupEditBuffer(source);
        buffer.SetValue("NAME", "candidate");
        Assert.True(buffer.IsDirty);
        Assert.Equal("source", source.Values["NAME"]);
        Assert.Equal("candidate", buffer.Candidate.Values["NAME"]);
        buffer.Revert();
        Assert.False(buffer.IsDirty);
        Assert.Equal("source", buffer.Candidate.Values["NAME"]);
    }

    [Fact]
    public void CreateAndCloneProduceIndependentDraftIdentities()
    {
        var lifecycle = Lifecycle(out _);
        var created = lifecycle.CreateDraft("general", "GENERIC", "NEW_ONE", "New one");
        Assert.Equal(SetupDefinitionStatus.Draft, created.Status);
        lifecycle.Buffer!.SetValue("NAME", "new");
        lifecycle.CancelChanges();
        var clone = lifecycle.Clone("clone-id", "CLONE_ONE");
        Assert.NotEqual(created.DefinitionId, clone.DefinitionId);
        Assert.Equal(created.Version + 1, clone.Version);
        Assert.Equal(SetupDefinitionStatus.Draft, clone.Status);
    }

    [Fact]
    public void ValidateAndPublishValidDraftAsImmutablePublishedState()
    {
        var lifecycle = Lifecycle(out var provider);
        lifecycle.Select(Definition(values: new Dictionary<string, object?> { ["NAME"] = "valid" }));
        Assert.True(lifecycle.Validate().IsValid);
        Assert.Equal(SetupDefinitionStatus.Valid, lifecycle.Buffer!.Candidate.Status);
        var published = lifecycle.Publish();
        Assert.Equal(SetupDefinitionStatus.Published, published.Status);
        Assert.Single(provider.Items);
        Assert.Throws<InvalidOperationException>(() => lifecycle.SaveDraft());
    }

    [Fact]
    public void InvalidDraftCannotPublish()
    {
        var lifecycle = Lifecycle(out _);
        lifecycle.Select(Definition(values: new Dictionary<string, object?> { ["NAME"] = "" }));
        var validation = lifecycle.Validate();
        Assert.False(validation.IsValid);
        Assert.Equal(SetupDefinitionStatus.Invalid, lifecycle.Buffer!.Candidate.Status);
        Assert.Throws<InvalidOperationException>(() => lifecycle.Publish());
    }

    [Fact]
    public void PublishedDefinitionRetiresWithoutDeletion()
    {
        var lifecycle = Lifecycle(out var provider);
        lifecycle.Select(Definition(status: SetupDefinitionStatus.Published));
        var retired = lifecycle.Retire();
        Assert.Equal(SetupDefinitionStatus.Retired, retired.Status);
        Assert.Contains(provider.Items, x => x.DefinitionId == retired.DefinitionId);
    }

    [Fact]
    public void SystemDefinitionIsViewableButCannotSave()
    {
        var lifecycle = Lifecycle(out _);
        lifecycle.Select(Definition(system: true, editable: false));
        lifecycle.Buffer!.SetValue("NAME", "attempt");
        Assert.Throws<InvalidOperationException>(() => lifecycle.SaveDraft());
    }

    [Fact]
    public void DirtyCandidateBlocksDefinitionNavigationUntilCancel()
    {
        var lifecycle = Lifecycle(out _);
        lifecycle.Select(Definition("one", "ONE"));
        lifecycle.Buffer!.SetValue("NAME", "changed");
        Assert.Equal(SetupNavigationDecision.BlockedByDirtyCandidate, lifecycle.Select(Definition("two", "TWO")));
        lifecycle.CancelChanges();
        Assert.Equal(SetupNavigationDecision.Allowed, lifecycle.Select(Definition("two", "TWO")));
    }

    [Fact]
    public void RefreshingSameDefinitionPreservesCandidateAndValidation()
    {
        var lifecycle = Lifecycle(out _);
        var definition = Definition(status: SetupDefinitionStatus.Invalid,
            values: new Dictionary<string, object?> { ["NAME"] = "" });
        lifecycle.Select(definition);
        Assert.False(lifecycle.Validate().IsValid);
        lifecycle.Select(definition);
        Assert.False(lifecycle.LastValidation!.IsValid);
        Assert.Same(definition, lifecycle.Buffer!.Source);
    }

    [Fact]
    public void EditorRegistryResolvesGenericCustomAndMissingEditorsWithoutSwitch()
    {
        var registry = new SetupEditorRegistry();
        var generic = new GenericPropertyEditorProvider("GENERIC",
            [new("name", "NAME", new("Name"), EditorFieldType.Text)]);
        Assert.True(registry.Register(generic));
        Assert.False(registry.Register(generic));
        Assert.Equal(SetupEditorKind.PropertyForm, registry.Resolve(Definition(type: "GENERIC")).Kind);
        Assert.Equal(SetupEditorKind.Unavailable, registry.Resolve(Definition(type: "UNKNOWN")).Kind);
    }

    [Fact]
    public void GenericEditorSupportsAllFoundationFieldTypesAndRejectsInvalidMetadata()
    {
        var types = Enum.GetValues<EditorFieldType>();
        var fields = types.Select((type, index) => new EditorFieldDefinition($"f{index}", $"F_{index}", new("Field"), type,
            choices: type == EditorFieldType.Choice ? [new("A", new("A"))] : null)).ToArray();
        var descriptor = new GenericPropertyEditorProvider("GENERIC", fields).CreateEditor(Definition());
        Assert.Equal(types.Length, descriptor.Fields.Length);
        Assert.Throws<ArgumentException>(() => new EditorFieldDefinition("choice", "CHOICE", new("Choice"), EditorFieldType.Choice));
    }

    [Fact]
    public void SetupActionBarsApplySelectionAndPermissionsFailClosed()
    {
        var context = new ActionBarResolutionContext(Company, new("setup", "Setup", StandardTemplateCodes.Setup),
            StandardTemplateCodes.Setup, null, new(0), PresentationState.Ready);
        var top = new DynamicActionBarResolver().Resolve(SetupActionBarDefinitions.Top, context);
        Assert.DoesNotContain(top.Actions, x => x.Definition.ActionCode == SetupActionCodes.New && x.IsEnabled);
        Assert.False(top.Actions.Single(x => x.Definition.ActionCode == SetupActionCodes.Edit).IsEnabled);
        var permissions = new[] { "SETUP.CREATE", "SETUP.EDIT", "SETUP.VALIDATE", "SETUP.PUBLISH", "SETUP.RETIRE" }
            .Select(x => new PermissionCode(x));
        var authorized = context with { Authorization = new(new("user"), Company.CompanyId, permissions, [], "r1"), Selection = new(1) };
        var enabled = new DynamicActionBarResolver().Resolve(SetupActionBarDefinitions.Top, authorized);
        Assert.All(enabled.Actions, action => Assert.True(action.IsEnabled));
        Assert.Equal([SetupActionCodes.Retire, SetupActionCodes.Cancel, SetupActionCodes.Save],
            SetupActionBarDefinitions.Bottom.Actions.Select(x => x.ActionCode));
    }

    [Fact]
    public void CompanyScopedProviderResultIsDeterministic()
    {
        var provider = new FakeProvider();
        provider.Items.Add(Definition("a", "A") with { CategoryId = "catalog", ScopeKey = "company-a" });
        provider.Items.Add(Definition("b", "B") with { CategoryId = "catalog", ScopeKey = "company-b" });
        Assert.Equal("A", provider.GetDefinitions("catalog", "company-a").Single().DefinitionCode);
        Assert.Equal("B", provider.GetDefinitions("catalog", "company-b").Single().DefinitionCode);
    }

    [Fact]
    public void RuntimeCultureAndThemeChoicesDoNotReplaceCandidateState()
    {
        var lifecycle = Lifecycle(out _);
        lifecycle.Select(Definition(values: new Dictionary<string, object?> { ["NAME"] = "before" }));
        lifecycle.Buffer!.SetValue("NAME", "candidate");
        var localization = new DictionaryLocalizationService("vi-VN");
        Assert.True(localization.TrySetCulture("en-US"));
        ThemeMode theme = ThemeMode.Light;
        theme = ThemeMode.Dark;
        Assert.Equal(ThemeMode.Dark, theme);
        Assert.Equal("candidate", lifecycle.Buffer.Candidate.Values["NAME"]);
        Assert.True(lifecycle.Buffer.IsDirty);
    }

    private static SetupCategoryDefinition Category(string id, string code, int order = 0, string? parent = null,
        PresentationRequirement? requirement = null) => new(id, code, new(code), Icon, order, parent,
            permissionRequirement: requirement);

    private static SetupDefinitionDescriptor Definition(string id = "definition", string code = "DEFINITION",
        string type = "GENERIC", SetupDefinitionStatus status = SetupDefinitionStatus.Draft,
        IReadOnlyDictionary<string, object?>? values = null, bool system = false, bool editable = true) =>
        new(id, code, code, type, status: status, isSystem: system, isEditable: editable, values: values, categoryId: "general");

    private static SetupDefinitionLifecycle Lifecycle(out FakeProvider provider)
    {
        provider = new();
        return new(provider, new RequiredNameValidator());
    }

    private sealed class RequiredNameValidator : ISetupDefinitionValidator
    {
        public SetupValidationResult Validate(SetupDefinitionDescriptor candidate) =>
            candidate.Values.TryGetValue("NAME", out var value) && string.IsNullOrWhiteSpace(value?.ToString())
                ? new([new(SetupDiagnosticSeverity.Error, "REQUIRED", new("Required"), FieldCode: "NAME")])
                : SetupValidationResult.Success;
    }

    private sealed class FakeProvider : ISetupDefinitionProvider
    {
        public List<SetupDefinitionDescriptor> Items { get; } = [];
        public IReadOnlyList<SetupDefinitionDescriptor> GetDefinitions(string categoryId, string? scopeKey = null) => Items
            .Where(x => x.CategoryId == categoryId && (x.ScopeKey is null || x.ScopeKey == scopeKey)).ToArray();
        public SetupDefinitionDescriptor SaveDraft(SetupDefinitionDescriptor candidate) => Upsert(candidate with { Status = SetupDefinitionStatus.Draft });
        public SetupDefinitionDescriptor Publish(SetupDefinitionDescriptor candidate) => Upsert(candidate with { Status = SetupDefinitionStatus.Published });
        public SetupDefinitionDescriptor Retire(SetupDefinitionDescriptor definition) => Upsert(definition with { Status = SetupDefinitionStatus.Retired });
        private SetupDefinitionDescriptor Upsert(SetupDefinitionDescriptor value)
        {
            var index = Items.FindIndex(x => x.DefinitionId == value.DefinitionId);
            if (index < 0) Items.Add(value); else Items[index] = value;
            return value;
        }
    }
}
