using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Reports;

public readonly record struct ReportCode
{
    public ReportCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
    public override string ToString() => Value;
}
public readonly record struct ReportColumnCode
{
    public ReportColumnCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
    public override string ToString() => Value;
}
public readonly record struct ReportParameterCode
{
    public ReportParameterCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
    public override string ToString() => Value;
}
public readonly record struct ReportAggregateCode
{
    public ReportAggregateCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
}
public readonly record struct ReportActionCode
{
    public ReportActionCode(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value.Trim().ToUpperInvariant(); }
    public string Value { get; }
    public override string ToString() => Value;
}

public enum ReportDataType { Text, Integer, Decimal, Boolean, Date, DateTime, Status, Reference }
public enum ReportAlignment { Start, Center, End }
public enum ReportAggregateKind { Count, Sum, Min, Max, Average }
public enum ReportAggregateScope { Report, Group }
public enum ReportOutputFormat { Csv, Xlsx, Pdf, Docx, Xml }
public enum ReportExportScope { FullEligibleReport, FilteredReport, Selection, VisibleColumns, AllEligibleColumns }
[Flags] public enum ReportOutputCapability { None = 0, Export = 1, Print = 2, View = 4, Open = 8, Download = 16 }
public enum ReportActionPlacement { Top, Bottom, Contextual, Overflow, Hidden }

public static class ReportCommandCodes
{
    public static string RunRefresh(ReportCode reportCode) => $"REPORT.{reportCode.Value}.RUN_REFRESH";
    public static string Reset(ReportCode reportCode) => $"REPORT.{reportCode.Value}.RESET";
    public static string Export(ReportCode reportCode) => $"REPORT.{reportCode.Value}.EXPORT";
    public static string Print(ReportCode reportCode) => $"REPORT.{reportCode.Value}.PRINT";
    public static string ViewOutput(ReportCode reportCode) => $"REPORT.{reportCode.Value}.VIEW_OUTPUT";
}

public sealed record ReportParameterDefinition(
    ReportParameterCode ParameterCode, EditorDefinition Editor, object? DefaultValue = null,
    UiAuthorizationBinding? Authorization = null, bool IsAdvanced = false)
{
    public UiElementDefinition ToAuthoringElement(UiElementCode reportElementCode) => new(
        new($"REPORT_PARAMETER:{ParameterCode.Value}"), UiElementKind.ReportParameter,
        Editor.Chrome.LabelKey ?? new(Editor.EditorCode.Value), reportElementCode, ParameterCode.Value,
        Editor, Editor.HelpContextCode, Authorization, isSensitive: Editor.SensitiveContent.Sensitivity != Sensitivity.Normal);
}

public sealed record ReportColumnDefinition(
    ReportColumnCode ColumnCode, LocalizationKey DisplayNameKey, ReportDataType DataType,
    VariableCode? VariableCode = null, string? Format = null, ReportAlignment Alignment = ReportAlignment.Start,
    decimal? DefaultWidth = null, bool IsVisible = true, bool IsSortable = true, bool IsFilterable = true,
    bool IsGroupable = true, bool IsAggregateEligible = false, SensitiveContentDefinition? SensitiveContent = null,
    HelpContextCode? HelpContextCode = null, UiAuthorizationBinding? Authorization = null);

public sealed record ReportSortDescriptor(ReportColumnCode ColumnCode, GridSortDirection Direction, int Priority = 0);
public sealed record ReportFilterDescriptor(ReportColumnCode ColumnCode, GridFilterOperatorKind Operator,
    GridFilterDataType DataType, object? Value = null, object? Value2 = null);
public sealed record ReportGroupDescriptor(ReportColumnCode ColumnCode, int Order = 0,
    GridSortDirection Direction = GridSortDirection.Ascending);
public sealed record ReportAggregateDefinition(ReportAggregateCode AggregateCode, ReportColumnCode ColumnCode,
    ReportAggregateKind Kind, ReportAggregateScope Scope, string? Format = null);
public sealed record ReportDrillDownDefinition(string DrillDownCode, LocalizationKey DisplayNameKey,
    string? RequiredCapabilityCode = null);
public sealed record ReportExportCapability(ReportOutputFormat Format, ImmutableArray<ReportExportScope> Scopes,
    ReportOutputCapability Capabilities = ReportOutputCapability.Export,
    UiAuthorizationBinding? ExportAuthorization = null, UiAuthorizationBinding? PrintAuthorization = null,
    UiAuthorizationBinding? ViewAuthorization = null);
/// <summary>Presentation contribution metadata; command registration and permission resolution remain external.</summary>
public sealed record ReportActionDefinition(ReportActionCode ActionCode, string CommandCode,
    ReportActionPlacement Placement, LocalizationKey DisplayNameKey, IconKey IconKey, int Order = 0,
    bool IsPrimary = false, HelpContextCode? HelpContextCode = null,
    UiAuthorizationBinding? AuthorizationRequirement = null, bool RequiresSelection = false);
public sealed record ReportPresentationHints(bool ParametersInitiallyCollapsed = false, bool ShowRowHeader = true,
    bool ShowStatus = true, bool AllowNestedGroups = true);

/// <summary>Immutable semantic report metadata. Runtime and rendered controls never mutate this object.</summary>
public sealed record ReportDefinition
{
    public ReportDefinition(ReportCode reportCode, LocalizationKey titleKey, IEnumerable<ReportColumnDefinition> columns,
        IEnumerable<ReportParameterDefinition>? parameters = null, LocalizationKey? subtitleKey = null,
        HelpContextCode? helpContextCode = null, IEnumerable<ReportSortDescriptor>? defaultSort = null,
        IEnumerable<ReportFilterDescriptor>? defaultFilter = null, IEnumerable<ReportGroupDescriptor>? defaultGroups = null,
        IEnumerable<ReportAggregateDefinition>? aggregates = null, IEnumerable<ReportDrillDownDefinition>? drillDowns = null,
        IEnumerable<ReportExportCapability>? exports = null, UiAuthorizationBinding? authorization = null,
        ReportPresentationHints? presentation = null, IEnumerable<ReportActionDefinition>? actions = null)
    {
        ReportCode = reportCode; TitleKey = titleKey; SubtitleKey = subtitleKey; HelpContextCode = helpContextCode;
        Columns = columns?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(columns));
        Parameters = (parameters ?? []).ToImmutableArray(); DefaultSort = (defaultSort ?? []).OrderBy(x => x.Priority).ToImmutableArray();
        DefaultFilter = (defaultFilter ?? []).ToImmutableArray(); DefaultGroups = (defaultGroups ?? []).OrderBy(x => x.Order).ToImmutableArray();
        Aggregates = (aggregates ?? []).ToImmutableArray(); DrillDowns = (drillDowns ?? []).ToImmutableArray();
        Exports = (exports ?? []).ToImmutableArray(); Authorization = authorization; Presentation = presentation ?? new();
        Actions = (actions ?? []).OrderBy(x => x.Order).ThenBy(x => x.ActionCode.Value, StringComparer.Ordinal).ToImmutableArray();
        if (Columns.IsEmpty) throw new ArgumentException("REPORT_COLUMNS_REQUIRED", nameof(columns));
        if (Columns.Select(x => x.ColumnCode).Distinct().Count() != Columns.Length) throw new ArgumentException("REPORT_DUPLICATE_COLUMN_CODE", nameof(columns));
        if (Parameters.Select(x => x.ParameterCode).Distinct().Count() != Parameters.Length) throw new ArgumentException("REPORT_DUPLICATE_PARAMETER_CODE", nameof(parameters));
        if (Actions.Select(x => x.ActionCode).Distinct().Count() != Actions.Length) throw new ArgumentException("REPORT_DUPLICATE_ACTION_CODE", nameof(actions));
    }
    public ReportCode ReportCode { get; }
    public LocalizationKey TitleKey { get; }
    public LocalizationKey? SubtitleKey { get; }
    public HelpContextCode? HelpContextCode { get; }
    public ImmutableArray<ReportColumnDefinition> Columns { get; }
    public ImmutableArray<ReportParameterDefinition> Parameters { get; }
    public ImmutableArray<ReportSortDescriptor> DefaultSort { get; }
    public ImmutableArray<ReportFilterDescriptor> DefaultFilter { get; }
    public ImmutableArray<ReportGroupDescriptor> DefaultGroups { get; }
    public ImmutableArray<ReportAggregateDefinition> Aggregates { get; }
    public ImmutableArray<ReportDrillDownDefinition> DrillDowns { get; }
    public ImmutableArray<ReportExportCapability> Exports { get; }
    public UiAuthorizationBinding? Authorization { get; }
    public ReportPresentationHints Presentation { get; }
    public ImmutableArray<ReportActionDefinition> Actions { get; }
    public ImmutableArray<UiElementDefinition> ToAuthoringElements()
    {
        var report = new UiElementCode($"REPORT:{ReportCode.Value}");
        return [new(report, UiElementKind.Report, TitleKey, semanticReference: ReportCode.Value,
            helpContextCode: HelpContextCode, authorization: Authorization),
            .. Parameters.Select(x => x.ToAuthoringElement(report)),
            .. Columns.Select(x => new UiElementDefinition(new($"REPORT_COLUMN:{x.ColumnCode.Value}"),
                UiElementKind.ReportColumn, x.DisplayNameKey, report, x.ColumnCode.Value,
                helpContextCode: x.HelpContextCode, authorization: x.Authorization,
                layout: new(x.DefaultWidth is { } width ? (double)width : null, 64, 420, DefaultVisible: x.IsVisible),
                isSensitive: x.SensitiveContent?.Sensitivity != Sensitivity.Normal)),
            .. Actions.Select(x => new UiElementDefinition(new($"REPORT_ACTION:{x.ActionCode.Value}"),
                UiElementKind.Command, x.DisplayNameKey, report, x.CommandCode,
                helpContextCode: x.HelpContextCode, authorization: x.AuthorizationRequirement,
                eligibleSurfaces: x.Placement switch
                {
                    ReportActionPlacement.Contextual => [UiSurface.ContextualToolbar],
                    ReportActionPlacement.Overflow => [UiSurface.Menu],
                    ReportActionPlacement.Hidden => [],
                    _ => [UiSurface.ActionBar],
                }))];
    }
}
