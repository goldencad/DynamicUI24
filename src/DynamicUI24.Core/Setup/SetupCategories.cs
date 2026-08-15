using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Setup;

public static class StandardSetupCategoryCodes
{
    public const string General = "GENERAL";
    public const string MasterCatalogs = "MASTER_CATALOGS";
    public const string Workspaces = "WORKSPACES";
    public const string ColumnsVariables = "COLUMNS_VARIABLES";
    public const string NavigationTree = "NAVIGATION_TREE";
    public const string Ribbon = "RIBBON";
    public const string ActionBars = "ACTION_BARS";
    public const string Dashboard = "DASHBOARD";
    public const string Reports = "REPORTS";
}

public sealed record SetupCategoryDefinition
{
    public SetupCategoryDefinition(string categoryId, string categoryCode, LocalizationKey displayNameKey,
        IconKey iconKey, int displayOrder = 0, string? parentCategoryId = null,
        string? definitionType = null, PresentationRequirement? permissionRequirement = null,
        bool isVisible = true, string? scopeKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryCode);
        if (!TechnicalCode.IsValid(categoryCode))
            throw new ArgumentException("Category code contains unsupported characters.", nameof(categoryCode));
        CategoryId = categoryId.Trim();
        CategoryCode = categoryCode.Trim().ToUpperInvariant();
        DisplayNameKey = displayNameKey;
        IconKey = iconKey;
        DisplayOrder = displayOrder;
        ParentCategoryId = string.IsNullOrWhiteSpace(parentCategoryId) ? null : parentCategoryId.Trim();
        DefinitionType = string.IsNullOrWhiteSpace(definitionType) ? null : definitionType.Trim().ToUpperInvariant();
        PermissionRequirement = permissionRequirement;
        IsVisible = isVisible;
        ScopeKey = string.IsNullOrWhiteSpace(scopeKey) ? null : scopeKey.Trim();
    }

    public string CategoryId { get; }
    public string CategoryCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public IconKey IconKey { get; }
    public int DisplayOrder { get; }
    public string? ParentCategoryId { get; }
    public string? DefinitionType { get; }
    public PresentationRequirement? PermissionRequirement { get; }
    public bool IsVisible { get; }
    public string? ScopeKey { get; }
}

public sealed record SetupMetadataDiagnostic(string Code, string Message, string? ItemId = null);
public sealed record SetupCategoryValidationResult(bool IsValid, ImmutableArray<SetupMetadataDiagnostic> Diagnostics);

public static class SetupCategoryValidator
{
    public static SetupCategoryValidationResult Validate(IEnumerable<SetupCategoryDefinition> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        var items = categories.ToArray();
        var diagnostics = ImmutableArray.CreateBuilder<SetupMetadataDiagnostic>();
        var groups = items.GroupBy(x => x.CategoryId, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var group in groups.Where(x => x.Count() > 1))
            diagnostics.Add(new("SETUP_DUPLICATE_CATEGORY_ID", $"Duplicate category id '{group.Key}'.", group.Key));
        var byId = groups.ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var category in items)
        {
            if (category.ParentCategoryId is not { } parent) continue;
            if (!byId.ContainsKey(parent))
                diagnostics.Add(new("SETUP_CATEGORY_ORPHAN", $"Parent '{parent}' does not exist.", category.CategoryId));
            else if (parent.Equals(category.CategoryId, StringComparison.OrdinalIgnoreCase))
                diagnostics.Add(new("SETUP_CATEGORY_CYCLE", "A category cannot parent itself.", category.CategoryId));
        }
        foreach (var category in items)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = category;
            while (visited.Add(current.CategoryId) && current.ParentCategoryId is { } parent && byId.TryGetValue(parent, out var next))
                current = next;
            if (current.ParentCategoryId is not null && !visited.Add(current.CategoryId))
                diagnostics.Add(new("SETUP_CATEGORY_CYCLE", $"A parent cycle includes '{category.CategoryId}'.", category.CategoryId));
        }
        return new(!diagnostics.Any(), diagnostics.Distinct().ToImmutableArray());
    }
}

public sealed record ResolvedSetupCategory(SetupCategoryDefinition Definition,
    AuthorizationPresentationState State, ImmutableArray<ResolvedSetupCategory> Children);
public sealed record ResolvedSetupCategories(ImmutableArray<ResolvedSetupCategory> Roots,
    ImmutableArray<SetupMetadataDiagnostic> Diagnostics);

public sealed class SetupCategoryResolver
{
    public ResolvedSetupCategories Resolve(IEnumerable<SetupCategoryDefinition> categories,
        EffectiveAuthorizationContext? authorization)
    {
        var items = categories?.ToArray() ?? throw new ArgumentNullException(nameof(categories));
        var validation = SetupCategoryValidator.Validate(items);
        if (!validation.IsValid) return new([], validation.Diagnostics);
        var children = items.ToLookup(x => x.ParentCategoryId, StringComparer.OrdinalIgnoreCase);
        ImmutableArray<ResolvedSetupCategory> Build(string? parent) => children[parent]
            .Where(x => x.IsVisible)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.CategoryCode, StringComparer.Ordinal)
            .Select(x => new ResolvedSetupCategory(x,
                x.PermissionRequirement is null ? AuthorizationPresentationState.VisibleEnabled :
                    AuthorizationPresentationResolver.Resolve(x.PermissionRequirement, authorization),
                Build(x.CategoryId)))
            .Where(x => x.State != AuthorizationPresentationState.Hidden)
            .ToImmutableArray();
        return new(Build(null), []);
    }
}

internal static class TechnicalCode
{
    public static bool IsValid(string value) => value.Trim().All(character =>
        char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.');
}
