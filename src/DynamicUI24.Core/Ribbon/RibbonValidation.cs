using System.Collections.Immutable;
using DynamicUI24.Core.Workspaces;

namespace DynamicUI24.Core.Ribbon;

public sealed record RibbonDiagnostic(string Code, string Message, string MetadataPath);

public sealed record RibbonValidationResult(ImmutableArray<RibbonDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.IsEmpty;
}

public static class RibbonDefinitionValidator
{
    public static RibbonValidationResult Validate(
        RibbonDefinition definition,
        IEnumerable<WorkspaceDefinition>? knownWorkspaces = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var diagnostics = ImmutableArray.CreateBuilder<RibbonDiagnostic>();
        var workspaceIds = knownWorkspaces?.Select(x => x.WorkspaceId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        AddDuplicates(definition.Tabs, x => x.TabCode, "RIBBON_DUPLICATE_TAB", "tabs", diagnostics);
        foreach (var tab in definition.Tabs)
        {
            var tabPath = $"tabs/{tab.TabCode}";
            AddDuplicates(tab.Groups, x => x.GroupCode, "RIBBON_DUPLICATE_GROUP", tabPath, diagnostics);
            foreach (var group in tab.Groups)
            {
                var groupPath = $"{tabPath}/groups/{group.GroupCode}";
                AddDuplicates(group.Commands, x => x.CommandCode, "RIBBON_DUPLICATE_COMMAND", groupPath, diagnostics);
                ValidateRule(group.ContextRule, groupPath, diagnostics);
                foreach (var command in group.Commands)
                {
                    var commandPath = $"{groupPath}/commands/{command.CommandCode}";
                    ValidateRule(command.ContextRule, commandPath, diagnostics);
                    if (command.CommandType == RibbonCommandType.Navigate &&
                        string.IsNullOrWhiteSpace(command.TargetWorkspaceId) && command.TargetTemplateCode is null)
                        diagnostics.Add(new("RIBBON_INVALID_COMMAND", "NAVIGATE requires a workspace or template target.", commandPath));
                    if (command.CommandType is RibbonCommandType.CustomRegistered or RibbonCommandType.ApplicationCommand &&
                        string.IsNullOrWhiteSpace(command.RegisteredCommandCode))
                        diagnostics.Add(new("RIBBON_INVALID_COMMAND", "Registered command code is required.", commandPath));
                    if (workspaceIds is not null && command.TargetWorkspaceId is { } target && !workspaceIds.Contains(target))
                        diagnostics.Add(new("RIBBON_UNKNOWN_WORKSPACE", $"Workspace '{target}' is not registered.", commandPath));
                }
            }
            ValidateRule(tab.ContextRule, tabPath, diagnostics);
        }
        return new(diagnostics.ToImmutable());
    }

    private static void AddDuplicates<T>(IEnumerable<T> items, Func<T, string> key, string code, string path,
        ImmutableArray<RibbonDiagnostic>.Builder diagnostics)
    {
        foreach (var duplicate in items.GroupBy(key, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
            diagnostics.Add(new(code, $"Duplicate code '{duplicate.Key}'.", path));
    }

    private static void ValidateRule(RibbonContextRule? rule, string path,
        ImmutableArray<RibbonDiagnostic>.Builder diagnostics)
    {
        if (rule is not null && !rule.IsWellFormed)
            diagnostics.Add(new("RIBBON_INVALID_CONTEXT_RULE", "Context rule is empty or malformed.", path));
    }
}
