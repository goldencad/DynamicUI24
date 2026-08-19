using System.Collections.Immutable;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;

namespace DynamicUI24.Core.Reports;

public sealed record ReportExecutionContext(CompanyDescriptor Company, string ContextKey);
public sealed record ReportResultWindow(int StartIndex, int RowCount, int OverscanBefore = 0, int OverscanAfter = 0);
public sealed record ReportRequest(ReportCode ReportCode, ImmutableDictionary<ReportParameterCode, object?> Parameters,
    ImmutableArray<ReportSortDescriptor> Sorts, ImmutableArray<ReportFilterDescriptor> Filters,
    ImmutableArray<ReportGroupDescriptor> Groups, ImmutableArray<ReportColumnCode> RequestedColumns,
    ReportResultWindow Window, long Generation, ReportExecutionContext Context);
public sealed record ReportRow(RowKey RowKey, ImmutableDictionary<ReportColumnCode, object?> Values);
public sealed record ReportAggregateValue(ReportAggregateCode AggregateCode, object? Value,
    string? GroupKey = null, ImmutableArray<object?> GroupPath = default);
public sealed record ReportResult(ImmutableArray<ReportRow> Rows, int? TotalRowCount, int? FilteredRowCount,
    ImmutableArray<ReportAggregateValue> Aggregates, long Generation, bool HasPrevious, bool HasNext,
    string? ProviderState = null);

public interface IReportProvider
{
    Task<ReportResult> ExecuteAsync(ReportRequest request, CancellationToken cancellationToken = default);
}

public sealed record ReportDrillDownRequest(ReportCode ReportCode, string DrillDownCode, RowKey RowKey,
    ReportColumnCode? ColumnCode, object? SemanticReference, long Generation, ReportExecutionContext Context);
public sealed record ReportNavigationTarget(string TargetKind, string TargetCode,
    IReadOnlyDictionary<string, string>? Parameters = null);
public interface IReportDrillDownProvider
{
    Task<ReportNavigationTarget?> ResolveAsync(ReportDrillDownRequest request, CancellationToken cancellationToken = default);
}
public interface IReportNavigationDispatcher
{
    Task DispatchAsync(ReportNavigationTarget target, CancellationToken cancellationToken = default);
}

public sealed record ReportExportRequest(ReportCode ReportCode, ReportOutputFormat Format, ReportExportScope Scope,
    ImmutableDictionary<ReportParameterCode, object?> Parameters, ImmutableArray<ReportSortDescriptor> Sorts,
    ImmutableArray<ReportFilterDescriptor> Filters, ImmutableArray<ReportGroupDescriptor> Groups,
    ImmutableArray<ReportColumnCode> Columns, ImmutableArray<RowKey> SelectedRows, long Generation,
    ReportExecutionContext Context);
public sealed record ReportOutputArtifact(ReportCode ReportCode, ReportOutputFormat Format, string ArtifactReference,
    string? SafeDisplayName = null, string? MediaType = null);
public sealed record ReportOutputResult(bool IsSuccess, ReportOutputArtifact? Artifact = null, string? DiagnosticCode = null);
public sealed record DocumentViewRequest(ReportOutputArtifact Artifact, ReportOutputCapability Action);
public sealed record DocumentViewResult(bool IsSuccess, string? DiagnosticCode = null);
public interface IDocumentViewLauncher
{
    Task<DocumentViewResult> LaunchAsync(DocumentViewRequest request, CancellationToken cancellationToken = default);
}
/// <summary>Streaming/provider-owned output seam; the UI never materializes the full logical report.</summary>
public interface IReportOutputProvider
{
    Task<ReportOutputResult> ExportAsync(ReportExportRequest request, CancellationToken cancellationToken = default);
    Task<ReportOutputResult> PrintAsync(ReportExportRequest request,
        CancellationToken cancellationToken = default);
}
