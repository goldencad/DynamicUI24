using System.Collections.Immutable;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Reports;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Demo;

internal static class DemoReport
{
    public static ReportDefinition CreateDefinition() => new(new("ACTIVITY_RECORDS"), new("Report.Activity.Title"),
        [
            Column("ID", ReportDataType.Integer, groupable: false), Column("NAME", ReportDataType.Text),
            Column("CATEGORY", ReportDataType.Text), Column("STATUS", ReportDataType.Status),
            Column("CREATED", ReportDataType.Date), Column("UPDATED", ReportDataType.DateTime),
            Column("ACTIVE", ReportDataType.Boolean), Column("QUANTITY", ReportDataType.Integer, aggregate: true),
            Column("AMOUNT", ReportDataType.Decimal, aggregate: true), Column("SCORE", ReportDataType.Decimal, aggregate: true),
            Column("OWNER", ReportDataType.Text),
            Column("REFERENCE", ReportDataType.Reference, sensitive: new(Sensitivity.Restricted, PrivacyPresentation.Mask)),
        ],
        [Parameter("QUERY", "Report.Parameter.Query", EditorValueType.String, defaultValue: "hoạt động"),
         Parameter("FROM", "Report.Parameter.From", EditorValueType.Date, defaultValue: new DateOnly(2026, 1, 1)),
         Parameter("RANGE", "Report.Parameter.Range", EditorValueType.DateRange,
             defaultValue: new DateRangeValue(new(2026, 1, 1), new(2026, 12, 31))),
         Parameter("ACTIVE", "Report.Parameter.Active", EditorValueType.Boolean, defaultValue: true),
         Parameter("STATUS", "Report.Parameter.Status", EditorValueType.Choice, defaultValue: "ANY",
             choices: [new("ANY", new("Report.Status.Any")), new("OPEN", new("Report.Status.Open")), new("CLOSED", new("Report.Status.Closed"))])],
        subtitleKey: new("Report.Activity.Subtitle"), helpContextCode: new("REPORT.ACTIVITY"),
        defaultSort: [new(new("ID"), GridSortDirection.Ascending)],
        defaultGroups: [new(new("CATEGORY"))],
        aggregates: [new(new("COUNT"), new("ID"), ReportAggregateKind.Count, ReportAggregateScope.Report),
                     new(new("TOTAL_AMOUNT"), new("AMOUNT"), ReportAggregateKind.Sum, ReportAggregateScope.Report),
                     new(new("GROUP_AMOUNT"), new("AMOUNT"), ReportAggregateKind.Sum, ReportAggregateScope.Group)],
        drillDowns: [new("VIEW_ACTIVITY", new("Report.Action.View"))],
        exports: [new(ReportOutputFormat.Csv, [ReportExportScope.FullEligibleReport, ReportExportScope.FilteredReport, ReportExportScope.VisibleColumns],
                      ReportOutputCapability.Export | ReportOutputCapability.View),
                  new(ReportOutputFormat.Xlsx, [ReportExportScope.FilteredReport]), new(ReportOutputFormat.Pdf, [ReportExportScope.FilteredReport])],
        presentation: new(ParametersInitiallyCollapsed: true),
        actions:
        [
            Action("RUN_REFRESH", ReportCommandCodes.RunRefresh(new("ACTIVITY_RECORDS")), ReportActionPlacement.Top,
                "Report.Action.Run", StandardIconKeys.Refresh, 10, primary: true),
            Action("RESET", ReportCommandCodes.Reset(new("ACTIVITY_RECORDS")), ReportActionPlacement.Top,
                "Report.Action.Reset", StandardIconKeys.Settings, 20),
            Action("EXPORT", ReportCommandCodes.Export(new("ACTIVITY_RECORDS")), ReportActionPlacement.Bottom,
                "Report.Action.Export", StandardIconKeys.Export, 10),
            Action("PRINT", ReportCommandCodes.Print(new("ACTIVITY_RECORDS")), ReportActionPlacement.Bottom,
                "Report.Action.Print", StandardIconKeys.Preview, 20),
            Action("VIEW_OUTPUT", ReportCommandCodes.ViewOutput(new("ACTIVITY_RECORDS")), ReportActionPlacement.Contextual,
                "Report.Action.ViewOutput", StandardIconKeys.Preview, 10),
            Action("ADVANCED", "REPORT.ACTIVITY_RECORDS.ADVANCED", ReportActionPlacement.Overflow,
                "Report.Action.More", StandardIconKeys.More, 100),
            Action("HIDDEN", "REPORT.ACTIVITY_RECORDS.HIDDEN", ReportActionPlacement.Hidden,
                "Report.Action.More", StandardIconKeys.More, 200),
        ]);

    private static ReportActionDefinition Action(string code, string command, ReportActionPlacement placement,
        string label, IconKey icon, int order, bool primary = false) =>
        new(new(code), command, placement, new(label), icon, order, primary,
            new HelpContextCode($"REPORT.ACTION.{code}"));

    private static ReportColumnDefinition Column(string code, ReportDataType type, bool groupable = true,
        bool aggregate = false, SensitiveContentDefinition? sensitive = null) =>
        new(new(code), new($"Report.Column.{code}"), type, IsGroupable: groupable,
            IsAggregateEligible: aggregate, SensitiveContent: sensitive, HelpContextCode: new($"REPORT.COLUMN.{code}"));

    private static ReportParameterDefinition Parameter(string code, string label, EditorValueType type,
        object? defaultValue = null, IEnumerable<EditorChoiceOption>? choices = null) => new(new(code),
        new(new($"REPORT_{code}"), new($"REPORT_PARAMETER:{code}"), type,
            chrome: new(LabelKey: new(label)), validation: new(), choices: choices,
            helpContextCode: new($"REPORT.PARAMETER.{code}")), defaultValue);
}

internal sealed class DemoReportProvider : IReportProvider, IReportFindProvider, IReportOutputProvider, IReportDrillDownProvider
{
    public Task<ReportResult> ExecuteAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); const int total = 100_000;
        var start = Math.Max(0, request.Window.StartIndex - request.Window.OverscanBefore);
        var count = Math.Min(request.Window.RowCount + request.Window.OverscanBefore + request.Window.OverscanAfter, total - start);
        var rows = Enumerable.Range(start, count).Select(Row).ToImmutableArray();
        return Task.FromResult(new ReportResult(rows, total, total,
            [new(new("COUNT"), total), new(new("TOTAL_AMOUNT"), 49_999_500m)], request.Generation,
            start > 0, start + count < total, "DEMO_BOUNDED_100K"));
    }
    public Task<ReportFindResult> FindAsync(ReportFindRequest request, CancellationToken cancellationToken = default)
    {
        var position = request.Query.Contains("90000", StringComparison.OrdinalIgnoreCase) ? 90_000 : Math.Clamp(request.StartPosition + 1, 0, 99_999);
        return Task.FromResult(new ReportFindResult(true, new($"ACT-{position:D6}"), new("NAME"), position));
    }
    public Task<ReportOutputResult> ExportAsync(ReportExportRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(request.Format == ReportOutputFormat.Csv
            ? new ReportOutputResult(true, new(request.ReportCode, request.Format, "demo://streamed/activity.csv", "activity.csv", "text/csv"))
            : new ReportOutputResult(false, DiagnosticCode: "REPORT_DOCUMENT_ADAPTER_NOT_CONFIGURED"));
    public Task<ReportOutputResult> PrintAsync(ReportExportRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ReportOutputResult(false, DiagnosticCode: "REPORT_OUTPUT_ADAPTER_NOT_CONFIGURED"));
    public Task<ReportNavigationTarget?> ResolveAsync(ReportDrillDownRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult<ReportNavigationTarget?>(new("WORKSPACE", "activity-detail", new Dictionary<string, string> { ["RowKey"] = request.RowKey.Value }));

    private static ReportRow Row(int i) => new(new($"ACT-{i:D6}"), new Dictionary<ReportColumnCode, object?>
    {
        [new("ID")] = i, [new("NAME")] = $"Activity {i:N0}", [new("CATEGORY")] = $"Group {i % 12 + 1}",
        [new("STATUS")] = i % 3 == 0 ? "Closed" : "Open", [new("CREATED")] = new DateOnly(2026, i % 12 + 1, i % 28 + 1),
        [new("UPDATED")] = new DateTime(2026, i % 12 + 1, i % 28 + 1, i % 24, 0, 0), [new("ACTIVE")] = i % 3 != 0,
        [new("QUANTITY")] = i % 100, [new("AMOUNT")] = i * 10m, [new("SCORE")] = (i % 1000) / 10m,
        [new("OWNER")] = $"Operator {i % 20 + 1}", [new("REFERENCE")] = $"PRIVATE-{i:D8}",
    }.ToImmutableDictionary());
}
