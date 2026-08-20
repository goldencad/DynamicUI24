using System.Diagnostics;
using Avalonia.Controls;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.ModernWorkspace;
using DynamicUI24.Core.Reports;
using DynamicUI24.Demo;
using DynamicUI24.Shared.Presentation;
using Xunit;
using Xunit.Abstractions;

namespace DynamicUI24.Tests;

public sealed class DataEntryActivationPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DataEntryAndReportExposeComparableColdActivationMilestones()
    {
        var company = new CompanyDescriptor(new("perf"), "PERF", "Performance");
        var localization = new DictionaryLocalizationService("en-US");

        var dataClock = Stopwatch.StartNew();
        var dataRuntime = new DataEntryGridRuntime(DemoDataEntry.CreateDefinition(), new DemoDataEntryProvider());
        var dataHost = new DataEntryGridHost(dataRuntime, localization);
        var dataConstructed = dataClock.Elapsed;
        var dataSurface = new ContentControl { Content = dataHost };
        var dataVisible = dataClock.Elapsed;
        var dataRequestStart = dataClock.Elapsed;
        await dataHost.LoadAsync(new(company, "data-entry-demo"), null);
        var dataRequestEnd = dataClock.Elapsed;
        var dataFirstRows = dataClock.Elapsed;
        var dataReady = dataClock.Elapsed;
        var dataBusyDismissed = dataClock.Elapsed;

        var reportClock = Stopwatch.StartNew();
        var reportProvider = new DemoReportProvider();
        var reportRuntime = new ReportRuntime(DemoReport.CreateDefinition(), reportProvider);
        var reportHost = new ReportWorkspaceHost(reportRuntime, localization,
            () => new ReportExecutionContext(company, "PERF"));
        var reportConstructed = reportClock.Elapsed;
        var reportSurface = new ContentControl { Content = reportHost };
        var reportVisible = reportClock.Elapsed;
        var reportRequestStart = reportClock.Elapsed;
        await reportHost.RunAsync();
        var reportRequestEnd = reportClock.Elapsed;
        var reportFirstRows = reportClock.Elapsed;
        var reportReady = reportClock.Elapsed;
        var reportBusyDismissed = reportClock.Elapsed;

        Assert.Equal(GridProviderState.Ready, dataRuntime.State);
        Assert.Equal(1, dataHost.LastActivationTiming!.ProviderRequests);
        Assert.InRange(dataHost.LastActivationTiming.Rebuilds, 0, 2);
        Assert.Equal(GridActivationStage.InteractiveReady, dataHost.ActivationStage);
        Assert.InRange(dataRuntime.Rows.Length, 1, dataRuntime.ViewportOptions.MaximumMaterializedRows);
        Assert.Equal(ContentPresentationState.Ready, reportRuntime.State);
        Assert.InRange(reportRuntime.Grid.Rows.Length, 1, reportRuntime.Grid.ViewportOptions.MaximumMaterializedRows);
        Assert.NotNull(dataSurface.Content); Assert.NotNull(reportSurface.Content);

        output.WriteLine($"DataEntry navigation=0.000 visible={dataVisible.TotalMilliseconds:F3} " +
            $"constructed={dataConstructed.TotalMilliseconds:F3} request-start={dataRequestStart.TotalMilliseconds:F3} " +
            $"request-end={dataRequestEnd.TotalMilliseconds:F3} first-rows={dataFirstRows.TotalMilliseconds:F3} " +
            $"ready={dataReady.TotalMilliseconds:F3} busy-dismissed={dataBusyDismissed.TotalMilliseconds:F3} rows={dataRuntime.Rows.Length}");
        output.WriteLine($"DataEntry host-visible={dataHost.LastActivationTiming.WorkspaceVisible.TotalMilliseconds:F3} " +
            $"data={dataHost.LastActivationTiming.DataAvailable.TotalMilliseconds:F3} " +
            $"interactive={dataHost.LastActivationTiming.InteractiveReady.TotalMilliseconds:F3} " +
            $"stable={dataHost.LastActivationTiming.StableLayout.TotalMilliseconds:F3} " +
            $"requests={dataHost.LastActivationTiming.ProviderRequests} rebuilds={dataHost.LastActivationTiming.Rebuilds}");
        output.WriteLine($"Report navigation=0.000 visible={reportVisible.TotalMilliseconds:F3} " +
            $"constructed={reportConstructed.TotalMilliseconds:F3} request-start={reportRequestStart.TotalMilliseconds:F3} " +
            $"request-end={reportRequestEnd.TotalMilliseconds:F3} first-rows={reportFirstRows.TotalMilliseconds:F3} " +
            $"ready={reportReady.TotalMilliseconds:F3} busy-dismissed={reportBusyDismissed.TotalMilliseconds:F3} rows={reportRuntime.Grid.Rows.Length}");
    }
}
