using System.Collections.Immutable;
using DynamicUI24.Core.Authoring;

namespace DynamicUI24.Core.Reports;

public sealed record ReportAuthorizationSnapshot(
    UiAuthorizationResult Report,
    ImmutableDictionary<ReportParameterCode, UiAuthorizationResult> Parameters,
    ImmutableDictionary<ReportColumnCode, UiAuthorizationResult> Columns,
    ImmutableDictionary<ReportOutputAuthorizationKey, UiAuthorizationResult>? Outputs = null,
    ImmutableDictionary<ReportActionCode, UiAuthorizationResult>? Actions = null)
{
    public bool CanRun => Report.State == UiAuthorizationState.Enabled;
    public bool CanOutput(ReportOutputFormat format, ReportOutputCapability capability) =>
        Outputs is null || !Outputs.TryGetValue(new(format, capability), out var result) || result.State == UiAuthorizationState.Enabled;
    public UiAuthorizationState ActionState(ReportActionCode code) => Actions is not null && Actions.TryGetValue(code, out var result)
        ? result.State : UiAuthorizationState.Enabled;
}
public readonly record struct ReportOutputAuthorizationKey(ReportOutputFormat Format, ReportOutputCapability Capability);

/// <summary>Projects Report semantic elements through the single Task 10H authorization resolver.</summary>
public sealed class ReportAuthorizationResolver(IUiAuthorizationResolver resolver)
{
    private readonly GenerationSafeUiAuthorizationService authorization = new(resolver);

    public async ValueTask<ReportAuthorizationSnapshot> ResolveAsync(ReportDefinition definition,
        UiAuthorizationContext context, CancellationToken cancellationToken = default)
    {
        var reportElement = new UiElementCode($"REPORT:{definition.ReportCode.Value}");
        var report = await authorization.ResolveAsync(new(reportElement, definition.Authorization,
            definition.Authorization?.Capability, context), cancellationToken).ConfigureAwait(false);
        var parameters = ImmutableDictionary.CreateBuilder<ReportParameterCode, UiAuthorizationResult>();
        foreach (var parameter in definition.Parameters)
            parameters[parameter.ParameterCode] = await authorization.ResolveAsync(new(
                new($"REPORT_PARAMETER:{parameter.ParameterCode.Value}"), parameter.Authorization,
                StandardUiCapabilities.CanEdit, context), cancellationToken).ConfigureAwait(false);
        var columns = ImmutableDictionary.CreateBuilder<ReportColumnCode, UiAuthorizationResult>();
        foreach (var column in definition.Columns)
            columns[column.ColumnCode] = await authorization.ResolveAsync(new(
                new($"REPORT_COLUMN:{column.ColumnCode.Value}"), column.Authorization, null, context),
                cancellationToken).ConfigureAwait(false);
        var outputs = ImmutableDictionary.CreateBuilder<ReportOutputAuthorizationKey, UiAuthorizationResult>();
        foreach (var definitionOutput in definition.Exports)
        {
            foreach (var action in new[] { ReportOutputCapability.Export, ReportOutputCapability.Print, ReportOutputCapability.View })
            {
                if (!definitionOutput.Capabilities.HasFlag(action)) continue;
                var binding = action switch { ReportOutputCapability.Export => definitionOutput.ExportAuthorization,
                    ReportOutputCapability.Print => definitionOutput.PrintAuthorization, _ => definitionOutput.ViewAuthorization };
                var requested = action switch { ReportOutputCapability.Export => StandardUiCapabilities.CanExport,
                    ReportOutputCapability.Print => StandardUiCapabilities.CanPrint, _ => StandardUiCapabilities.CanOpen };
                outputs[new(definitionOutput.Format, action)] = await authorization.ResolveAsync(new(
                    new($"REPORT_OUTPUT:{definition.ReportCode.Value}:{definitionOutput.Format}:{action}"), binding,
                    requested, context), cancellationToken).ConfigureAwait(false);
            }
        }
        var actions = ImmutableDictionary.CreateBuilder<ReportActionCode, UiAuthorizationResult>();
        foreach (var action in definition.Actions)
            actions[action.ActionCode] = await authorization.ResolveAsync(new(
                new($"REPORT_ACTION:{action.ActionCode.Value}"), action.AuthorizationRequirement,
                action.AuthorizationRequirement?.Capability, context), cancellationToken).ConfigureAwait(false);
        return new(report, parameters.ToImmutable(), columns.ToImmutable(), outputs.ToImmutable(), actions.ToImmutable());
    }
}
