using System.Collections.Immutable;
using System.Diagnostics;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.ActionBars;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Editors;
using DynamicUI24.Core.ModernWorkspace;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Core.Reports;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;
using DynamicUI24.Avalonia.Presentation;
using Xunit;
using Xunit.Abstractions;

namespace DynamicUI24.Tests;

public sealed class ReportRuntimeTests
{
    private readonly ITestOutputHelper output;
    public ReportRuntimeTests(ITestOutputHelper output) => this.output = output;
    private static readonly CompanyDescriptor CompanyA = new(new("A"), "A", "Company A");
    private static readonly CompanyDescriptor CompanyB = new(new("B"), "B", "Company B");

    [Fact]
    public void Identity_is_semantic_and_independent_of_titles_and_order()
    {
        var original = Definition("Title.One"); var localized = Definition("Tiêu đề");
        Assert.Equal(original.ReportCode, localized.ReportCode);
        Assert.Equal(original.Columns.Select(x => x.ColumnCode.Value).Order(), localized.Columns.Reverse().Select(x => x.ColumnCode.Value).Order());
        Assert.Throws<ArgumentException>(() => new ReportDefinition(new("DUP"), new("Title"), [original.Columns[0], original.Columns[0]]));
    }

    [Fact]
    public void Parameters_are_universal_editors_and_project_to_developer_authoring_elements()
    {
        var definition = Definition();
        var parameter = definition.Parameters[0];
        Assert.Equal(EditorValueType.String, parameter.Editor.ValueType);
        Assert.Equal(new EditorSemanticId("REPORT_PARAMETER:QUERY"), parameter.Editor.ConsumerSemanticId);
        var elements = definition.ToAuthoringElements();
        Assert.Contains(elements, x => x.Kind == UiElementKind.Report && x.SemanticReference == "ACTIVITY");
        Assert.Contains(elements, x => x.Kind == UiElementKind.ReportParameter && x.Editor == parameter.Editor);
        Assert.Contains(elements, x => x.Kind == UiElementKind.ReportColumn && x.SemanticReference == "NAME");
    }

    [Fact]
    public void Action_metadata_contributes_to_shared_top_bottom_contextual_overflow_and_hidden_surfaces()
    {
        var actions = new[]
        {
            ReportAction("RESET", ReportActionPlacement.Top, 20),
            ReportAction("RUN", ReportActionPlacement.Top, 10, primary: true),
            ReportAction("EXPORT", ReportActionPlacement.Bottom, 10),
            ReportAction("VIEW", ReportActionPlacement.Contextual, 10, selection: true),
            ReportAction("ADVANCED", ReportActionPlacement.Overflow, 10),
            ReportAction("SECRET", ReportActionPlacement.Hidden, 0),
        };
        var definition = Definition(actions: actions);
        var contributions = ReportActionContributionAdapter.Create(definition);

        Assert.Equal(["RUN", "RESET"], contributions.Top.Actions.Select(x => x.ActionCode));
        Assert.Equal("EXPORT", Assert.Single(contributions.Bottom.Actions).ActionCode);
        Assert.Equal("VIEW", Assert.Single(contributions.Contextual).ActionCode);
        Assert.Equal("ADVANCED", Assert.Single(Assert.Single(contributions.Overflow.Actions).MenuItems).ItemCode);
        Assert.DoesNotContain("SECRET", contributions.Top.Actions.Select(x => x.ActionCode));
        Assert.Equal(ActionControlSizePreset.Large, contributions.Top.Actions[0].Geometry.SizePreset);
        Assert.Contains(definition.ToAuthoringElements(), x => x.Kind == UiElementKind.Command && x.SemanticReference == actions[0].CommandCode);
    }

    [Fact]
    public async Task Localized_labels_do_not_change_action_identity_and_authorization_hidden_wins_over_top_placement()
    {
        var protectedAction = ReportAction("EXPORT", ReportActionPlacement.Top, 10,
            authorization: new(Capability: StandardUiCapabilities.CanExport));
        var english = Definition("English", actions: [protectedAction]);
        var vietnamese = Definition("Tiếng Việt", actions: [protectedAction with { DisplayNameKey = new("Nhãn.TiếngViệt") }]);
        Assert.Equal(english.Actions[0].ActionCode, vietnamese.Actions[0].ActionCode);
        Assert.Equal(english.Actions[0].CommandCode, vietnamese.Actions[0].CommandCode);
        var context = new UiAuthorizationContext(new("USER", 1, ImmutableHashSet<PermissionCode>.Empty,
            ImmutableHashSet<CapabilityCode>.Empty), new("A"), "REPORT", new("REPORTS"), new(1), 1, 1, 1, PrivacyMode.On);
        var snapshot = await new ReportAuthorizationResolver(new DefaultUiAuthorizationResolver()).ResolveAsync(english, context);
        Assert.Equal(UiAuthorizationState.Hidden, snapshot.ActionState(new("EXPORT")));
    }

    [Fact]
    public async Task Dynamic_authorization_fail_closes_run_and_uses_current_content_state()
    {
        var definition = Definition(authorization: new(Capability: StandardUiCapabilities.CanExecute));
        var context = new UiAuthorizationContext(new("USER", 1,
            ImmutableHashSet<PermissionCode>.Empty, ImmutableHashSet<CapabilityCode>.Empty),
            new("A"), "REPORT", new("REPORTS"), new(1), 1, 1, 1, PrivacyMode.On);
        var snapshot = await new ReportAuthorizationResolver(new DefaultUiAuthorizationResolver())
            .ResolveAsync(definition, context);
        var provider = new FakeProvider();
        var runtime = new ReportRuntime(definition, provider);
        runtime.SetParameter(new("QUERY"), "x");
        await runtime.RunAsync(Context(CompanyA), reportAuthorization: snapshot);
        Assert.Equal(ContentPresentationState.Unauthorized, runtime.State);
        Assert.Empty(provider.Requests);
    }

    [Fact]
    public async Task Long_running_execution_projects_through_shared_operation_coordinator()
    {
        var operations = new OperationCoordinator();
        var runtime = new ReportRuntime(Definition(), new FakeProvider(), operations: operations);
        runtime.SetParameter(new("QUERY"), "x");
        await runtime.RunAsync(Context(CompanyA));
        var operation = Assert.Single(operations.Current);
        Assert.Equal(OperationState.Succeeded, operation.State);
        Assert.Equal("ACTIVITY", operation.TargetSemanticId);
    }

    [Fact]
    public async Task Run_refresh_dispatches_once_through_semantic_registry_and_acknowledges_identical_results()
    {
        var provider = new FakeProvider();
        var runtime = new ReportRuntime(Definition(queryDefault: "default"), provider);
        runtime.SetParameter(new("QUERY"), "same result");
        var states = new List<ContentPresentationState>();
        runtime.Changed += (_, _) => states.Add(runtime.State);
        var registry = new ActionCommandRegistry();
        var host = new ReportWorkspaceHost(runtime, new DictionaryLocalizationService("en-US"),
            () => Context(CompanyA), commandRegistry: registry, commandContext: CommandContext);

        var generation = runtime.Generation;
        var requests = runtime.ResultProviderRequestCount;
        var first = await host.DispatchCommandAsync(host.RunCommandCode);

        Assert.Equal(ActionCommandResultStatus.Success, first.Status);
        Assert.Equal(requests + 1, runtime.ResultProviderRequestCount);
        Assert.True(runtime.Generation > generation);
        Assert.Contains(ContentPresentationState.Loading, states);
        Assert.Equal(ContentPresentationState.Ready, states[^1]);
        Assert.Contains("Refreshed", host.CommandStatusText);

        requests = runtime.ResultProviderRequestCount;
        var second = await host.DispatchCommandAsync(host.RunCommandCode);
        Assert.Equal(ActionCommandResultStatus.Success, second.Status);
        Assert.Equal(requests + 1, runtime.ResultProviderRequestCount);
        Assert.Contains("Refreshed", host.CommandStatusText);
    }

    [Fact]
    public async Task Reset_command_restores_owned_query_state_and_refreshes_exactly_once()
    {
        var provider = new FakeProvider();
        var runtime = new ReportRuntime(Definition(queryDefault: "default"), provider);
        runtime.SetParameter(new("QUERY"), "changed");
        var registry = new ActionCommandRegistry();
        var host = new ReportWorkspaceHost(runtime, new DictionaryLocalizationService("en-US"),
            () => Context(CompanyA), commandRegistry: registry, commandContext: CommandContext);
        await host.DispatchCommandAsync(host.RunCommandCode);
        await runtime.Grid.SetSortAsync([new(new("REPORT_AMOUNT"), GridSortDirection.Descending)], null);
        await runtime.Grid.SetFiltersAsync([new(new("REPORT_NAME"), GridFilterOperator.IsNotEmpty)], null);
        await runtime.SetGroupsAsync([new(new("STATUS"))]);
        runtime.Grid.Select([runtime.Grid.Rows[0].RowKey]);
        Assert.NotEmpty(runtime.Sorts); Assert.NotEmpty(runtime.Filters); Assert.NotEmpty(runtime.Groups);
        Assert.NotEmpty(runtime.Grid.SelectedRowKeys);
        var requests = runtime.ResultProviderRequestCount;

        var result = await host.DispatchCommandAsync(host.ResetCommandCode);

        Assert.Equal(ActionCommandResultStatus.Success, result.Status);
        Assert.Equal(requests + 1, runtime.ResultProviderRequestCount);
        Assert.True(runtime.Definition.DefaultSort.SequenceEqual(runtime.Sorts));
        Assert.True(runtime.Definition.DefaultFilter.SequenceEqual(runtime.Filters));
        Assert.True(runtime.Definition.DefaultGroups.SequenceEqual(runtime.Groups));
        Assert.Equal("default", runtime.Parameters[new("QUERY")]);
        Assert.Empty(runtime.Grid.SelectedRowKeys);
        Assert.Equal(ContentPresentationState.Ready, runtime.State);
        Assert.Contains("Defaults restored", host.CommandStatusText);
    }

    [Fact]
    public async Task Command_authorization_denial_blocks_dispatch_without_generation_or_provider_work()
    {
        var provider = new FakeProvider();
        var runtime = new ReportRuntime(Definition(), provider);
        runtime.SetParameter(new("QUERY"), "x");
        var registry = new ActionCommandRegistry();
        var denied = new ReportAuthorizationSnapshot(
            new(new("REPORT:ACTIVITY"), UiAuthorizationState.Hidden, [], 1, new(1), "DENIED"),
            ImmutableDictionary<ReportParameterCode, UiAuthorizationResult>.Empty,
            ImmutableDictionary<ReportColumnCode, UiAuthorizationResult>.Empty);
        var host = new ReportWorkspaceHost(runtime, new DictionaryLocalizationService("en-US"),
            () => Context(CompanyA), commandRegistry: registry, commandContext: CommandContext,
            authorization: _ => ValueTask.FromResult<ReportAuthorizationSnapshot?>(denied));
        var generation = runtime.Generation;

        var run = await host.DispatchCommandAsync(host.RunCommandCode);
        var reset = await host.DispatchCommandAsync(host.ResetCommandCode);
        var export = await host.DispatchCommandAsync(host.ExportCommandCode);

        Assert.Equal(ActionCommandResultStatus.Denied, run.Status);
        Assert.Equal(ActionCommandResultStatus.Denied, reset.Status);
        Assert.Equal(ActionCommandResultStatus.Denied, export.Status);
        Assert.Equal(generation, runtime.Generation);
        Assert.Equal(0, runtime.ResultProviderRequestCount);
    }

    [Fact]
    public async Task Required_defaults_reset_and_unicode_parameters_are_preserved()
    {
        var provider = new FakeProvider(); var runtime = new ReportRuntime(Definition(), provider);
        await runtime.RunAsync(Context(CompanyA));
        Assert.Equal(ContentPresentationState.Initial, runtime.State);
        runtime.SetParameter(new("QUERY"), "Tiếng Việt – dữ liệu"); runtime.SetParameter(new("ACTIVE"), true);
        await runtime.RunAsync(Context(CompanyA));
        Assert.Equal("Tiếng Việt – dữ liệu", runtime.LastExecutedParameters[new("QUERY")]);
        Assert.Equal(ContentPresentationState.Ready, runtime.State);
        runtime.ResetParameters();
        Assert.Null(runtime.Parameters[new("QUERY")]); Assert.Equal(true, runtime.Parameters[new("ACTIVE")]);
    }

    [Fact]
    public async Task Provider_is_windowed_and_far_jump_stays_bounded_for_100k_rows()
    {
        var provider = new FakeProvider(); var runtime = new ReportRuntime(Definition(), provider,
            viewportOptions: new GridViewportOptions(60, 20, 20, 3, 300));
        runtime.SetParameter(new("QUERY"), "record"); await runtime.RunAsync(Context(CompanyA));
        await runtime.Grid.RequestViewportAsync(90_000, 60);
        Assert.Equal(100_000, runtime.Grid.TotalRows); Assert.True(runtime.Grid.Rows.Length <= 100);
        Assert.True(runtime.Grid.CachedRowCount <= 300); Assert.Contains(provider.Requests, x => x.Window.StartIndex == 89_980);
    }

    [Fact]
    public void Opening_parameters_is_metadata_only_and_materializes_no_result_rows()
    {
        var localization = new DictionaryLocalizationService("en-US");
        var provider = new FakeProvider();
        var runtime = new ReportRuntime(Definition("Report.Activity.Title"), provider);
        var host = new ReportWorkspaceHost(runtime, localization, () => Context(CompanyA));

        var firstToggle = Stopwatch.StartNew();
        host.SetParametersOpen(false);
        host.SetParametersOpen(true);
        firstToggle.Stop();
        var subsequentToggle = Stopwatch.StartNew();
        host.SetParametersOpen(false);
        host.SetParametersOpen(true);
        subsequentToggle.Stop();

        Assert.True(host.AreParametersOpen);
        Assert.Empty(provider.Requests);
        Assert.Equal(0, host.MaterializedResultRowCount);
        Assert.Equal(ContentPresentationState.Initial, runtime.State);
        Assert.Equal(1, host.WorkspaceBuildCount);
        Assert.Equal(2, host.ParameterControlBuildCount);
        output.WriteLine($"cold-workspace={host.LastConstructionTiming.Total.TotalMilliseconds:F3}ms; grid-host={host.LastConstructionTiming.GridHost.TotalMilliseconds:F3}ms; parameters={host.LastConstructionTiming.ParameterControls.TotalMilliseconds:F3}ms; requests={provider.Requests.Count}; rows={host.MaterializedResultRowCount}");
        output.WriteLine($"first-parameter-toggle={firstToggle.Elapsed.TotalMilliseconds:F3}ms; subsequent-parameter-toggle={subsequentToggle.Elapsed.TotalMilliseconds:F3}ms");
    }

    [Fact]
    public async Task Physical_parameter_layout_transition_after_run_does_not_resize_query_or_rebuild_grid()
    {
        var provider = new FakeProvider();
        var runtime = new ReportRuntime(Definition("Report.Activity.Title"), provider);
        runtime.SetParameter(new("QUERY"), "Tiếng Việt đang soạn");
        await runtime.RunAsync(Context(CompanyA));
        var host = new ReportWorkspaceHost(runtime, new DictionaryLocalizationService("en-US"), () => Context(CompanyA));
        var presenter = host.ParameterPresenterIdentity;
        var rows = runtime.Grid.Rows;
        var generation = runtime.Generation;
        var requests = runtime.ResultProviderRequestCount;

        host.SetParametersOpen(false);
        host.SetParametersOpen(true);
        host.SetParametersOpen(false);
        host.SetParametersOpen(true);
        await Task.Delay(250);

        Assert.Equal(requests, runtime.ResultProviderRequestCount);
        Assert.Equal(generation, runtime.Generation);
        Assert.Equal(rows, runtime.Grid.Rows);
        Assert.Same(presenter, host.ParameterPresenterIdentity);
        Assert.Equal(1, host.WorkspaceBuildCount);
        Assert.Equal(2, host.ParameterControlBuildCount);
        Assert.Equal("Tiếng Việt đang soạn", runtime.Parameters[new("QUERY")]);
        Assert.True(host.AreParametersOpen);
    }

    [Fact]
    public async Task Default_first_run_requests_only_the_small_bounded_first_paint_window()
    {
        var provider = new FakeProvider();
        var runtime = new ReportRuntime(Definition(), provider);
        runtime.SetParameter(new("QUERY"), "x");

        await runtime.RunAsync(Context(CompanyA));

        var request = Assert.Single(provider.Requests);
        Assert.Equal(0, request.Window.StartIndex);
        Assert.Equal(38, request.Window.RowCount);
        Assert.Equal(38, runtime.Grid.Rows.Length);
        Assert.Equal(38, provider.ProducedRows);
        Assert.Equal(100_000, runtime.Grid.TotalRows);
        Assert.NotNull(runtime.LastExecutionTrace);
        Assert.Equal(38, runtime.LastExecutionTrace!.MaterializedRows);
        Assert.Single(runtime.Aggregates);
        output.WriteLine($"validation={runtime.LastExecutionTrace.ParameterValidation.TotalMilliseconds:F3}ms; first-window={runtime.LastExecutionTrace.FirstWindowAcquisition.TotalMilliseconds:F3}ms; provider={runtime.LastExecutionTrace.ProviderAcquisition.TotalMilliseconds:F3}ms; mapping={runtime.LastExecutionTrace.RowMapping.TotalMilliseconds:F3}ms; rows={runtime.LastExecutionTrace.MaterializedRows}; aggregates={runtime.LastExecutionTrace.AggregateValues}");

        await runtime.RunAsync(Context(CompanyA));
        Assert.Equal(2, provider.Requests.Count);
        Assert.Equal(38, runtime.Grid.Rows.Length);
        output.WriteLine($"warm-run-first-window={runtime.LastExecutionTrace!.FirstWindowAcquisition.TotalMilliseconds:F3}ms; provider={runtime.LastExecutionTrace.ProviderAcquisition.TotalMilliseconds:F3}ms; mapping={runtime.LastExecutionTrace.RowMapping.TotalMilliseconds:F3}ms");
    }

    [Fact]
    public void Report_columns_resolve_usable_type_defaults_and_respect_explicit_width()
    {
        var definition = new ReportDefinition(new("GEOMETRY"), new("Report.Title"),
        [
            new(new("TEXT"), new("Text"), ReportDataType.Text),
            new(new("INTEGER"), new("Integer"), ReportDataType.Integer),
            new(new("DECIMAL"), new("Decimal"), ReportDataType.Decimal),
            new(new("DATE"), new("Date"), ReportDataType.Date),
            new(new("BOOLEAN"), new("Boolean"), ReportDataType.Boolean),
            new(new("STATUS"), new("Status"), ReportDataType.Status),
            new(new("EXPLICIT"), new("Explicit"), ReportDataType.Text, DefaultWidth: 240m),
        ]);
        var runtime = new ReportRuntime(definition, new FakeProvider());
        var widths = runtime.Grid.PresentedColumns.ToDictionary(x => x.Column.Definition.ColumnCode, x => x.Width);

        Assert.Equal(180m, widths["TEXT"]);
        Assert.Equal(112m, widths["INTEGER"]);
        Assert.Equal(132m, widths["DECIMAL"]);
        Assert.Equal(128m, widths["DATE"]);
        Assert.Equal(96m, widths["BOOLEAN"]);
        Assert.Equal(120m, widths["STATUS"]);
        Assert.Equal(240m, widths["EXPLICIT"]);
        Assert.All(widths.Values, width => Assert.True(width >= 64m));
    }

    [Fact]
    public void Report_preferences_follow_accepted_shared_grid_clamping_without_cross_column_contamination()
    {
        var runtime = new ReportRuntime(Definition(), new FakeProvider());
        var name = runtime.Grid.PresentedColumns.Single(x => x.Column.Definition.ColumnCode == "NAME");
        var amount = runtime.Grid.PresentedColumns.Single(x => x.Column.Definition.ColumnCode == "AMOUNT");
        runtime.Grid.ApplyViewPreference(new GridViewPreference(runtime.Grid.Definition.GridCode, GridViewPreference.CurrentSchemaVersion,
            [new(name.VariableCode, 0, 0m, true, WidthScalePercent: 0m),
             new(amount.VariableCode, 1, amount.Width, true, WidthScalePercent: 100m)], [], []));

        var repairedName = runtime.Grid.PresentedColumns.Single(x => x.VariableCode == name.VariableCode);
        var repairedAmount = runtime.Grid.PresentedColumns.Single(x => x.VariableCode == amount.VariableCode);
        Assert.Equal(name.Column.Width * .5m, repairedName.Width);
        Assert.Equal(50m, runtime.Grid.GetColumnWidthPercentage(name.VariableCode));
        Assert.Equal(amount.Column.Width, repairedAmount.Width);
        Assert.DoesNotContain(runtime.Grid.CurrentViewPreference.Columns, x => x.Width is <= 0);
    }

    [Fact]
    public async Task First_and_far_materialized_rows_project_values_and_keep_shared_overflow_geometry()
    {
        var provider = new FakeProvider();
        var runtime = new ReportRuntime(Definition(), provider); runtime.SetParameter(new("QUERY"), "x");
        await runtime.RunAsync(Context(CompanyA));
        var first = runtime.Grid.Rows[0];
        Assert.True(first.TryGetValue(new("REPORT_NAME"), out var name));
        Assert.Equal("Record 0", name);
        Assert.True(first.TryGetValue(new("REPORT_AMOUNT"), out var amount));
        Assert.Equal(0m, amount);
        var widths = runtime.Grid.PresentedColumns.Select(x => x.Width).ToImmutableArray();
        Assert.True(widths.Sum() > 500m);
        var host = new ReportWorkspaceHost(runtime, new DictionaryLocalizationService("en-US"), () => Context(CompanyA));
        Assert.Same(runtime.Grid, host.ResultGrid.Runtime);

        await runtime.Grid.RequestViewportAsync(90_000, 30);
        Assert.True(widths.SequenceEqual(runtime.Grid.PresentedColumns.Select(x => x.Width)));
        Assert.Contains(runtime.Grid.Rows, x => x.RowKey == new RowKey("R90000"));
        Assert.Equal(2, provider.Requests.Count);
    }

    [Fact]
    public async Task Ordinary_values_present_raw_while_sensitive_values_follow_P1()
    {
        var runtime = new ReportRuntime(Definition(), new FakeProvider()); runtime.SetParameter(new("QUERY"), "x");
        await runtime.RunAsync(Context(CompanyA));
        var row = runtime.Grid.Rows[0];
        Assert.Equal("Record 0", row.Values[new("REPORT_NAME")]);
        var secret = row.Values[new VariableCode("REPORT_SECRET")];
        var metadata = runtime.Definition.Columns.Single(x => x.ColumnCode == new ReportColumnCode("SECRET")).SensitiveContent;
        var resolved = new PrivacyPolicyResolver().Resolve(new(true, metadata, PrivacyMode.Auto));
        var presented = new SensitiveValuePresenter().Present(secret, metadata, resolved);
        Assert.Equal(SensitiveValuePresenter.Mask, presented.DisplayValue);
        Assert.NotEqual(secret?.ToString(), presented.AccessibleValue);
    }

    [Fact]
    public async Task Sort_filter_group_and_aggregates_are_provider_owned()
    {
        var provider = new FakeProvider(); var runtime = new ReportRuntime(Definition(), provider);
        runtime.SetParameter(new("QUERY"), "x"); await runtime.RunAsync(Context(CompanyA));
        await runtime.SetSortAsync([new(new("AMOUNT"), GridSortDirection.Descending)]);
        await runtime.SetFiltersAsync([new(new("NAME"), GridFilterOperatorKind.Contains, GridFilterDataType.Text, "25")]);
        await runtime.SetGroupsAsync([new(new("STATUS"))]);
        Assert.Single(runtime.Sorts); Assert.Single(runtime.Filters); Assert.Single(runtime.Groups);
        Assert.Equal(100_000m, runtime.Aggregates.Single().Value);
        Assert.Contains(provider.Requests, x => x.Sorts.Length == 1 && x.Filters.Length == 1 && x.Groups.Length == 1);
    }

    [Fact]
    public async Task Sensitive_group_is_denied_and_filter_can_produce_filtered_empty()
    {
        var runtime = new ReportRuntime(Definition(), new FakeProvider()); runtime.SetParameter(new("QUERY"), "x");
        await runtime.RunAsync(Context(CompanyA));
        await runtime.SetGroupsAsync([new(new("SECRET"))]); Assert.Empty(runtime.Groups);
        await runtime.SetFiltersAsync([new(new("NAME"), GridFilterOperatorKind.Equals, GridFilterDataType.Text, "EMPTY")]);
        Assert.Equal(ContentPresentationState.FilteredEmpty, runtime.State);
    }

    [Fact]
    public async Task Company_switch_invalidates_selection_and_stale_generation()
    {
        var delayed = new DelayedProvider(); var runtime = new ReportRuntime(Definition(), delayed); runtime.SetParameter(new("QUERY"), "x");
        var old = runtime.RunAsync(Context(CompanyA)); await delayed.Started.Task;
        var newer = runtime.RunAsync(Context(CompanyB)); delayed.Release.TrySetResult(); await Task.WhenAll(old, newer);
        Assert.Contains(delayed.Requests, x => x.Context.Company.Code == "B"); Assert.Equal(ContentPresentationState.Ready, runtime.State);
        Assert.Empty(runtime.Grid.SelectedRowKeys);
        Assert.Equal(200_000m, runtime.Aggregates.Single().Value);
    }

    [Fact]
    public async Task Find_reuses_grid_engine_and_resolves_far_semantic_match()
    {
        var provider = new FakeProvider(); var runtime = new ReportRuntime(Definition(), provider); runtime.SetParameter(new("QUERY"), "x");
        await runtime.RunAsync(Context(CompanyA));
        var result = await runtime.Grid.FindAsync("far", GridFindScope.AllVisibleColumns);
        Assert.True(result.IsMatch); Assert.Equal(90_000, result.LogicalPosition); Assert.Equal("R90000", result.RowKey?.Value);
        Assert.Equal(90_000, runtime.Grid.RequestedViewportStartIndex);
    }

    [Fact]
    public async Task Export_scope_is_explicit_and_does_not_materialize_rows()
    {
        var provider = new FakeProvider(); var output = new FakeOutput(); var runtime = new ReportRuntime(Definition(), provider, output);
        runtime.SetParameter(new("QUERY"), "x"); await runtime.RunAsync(Context(CompanyA)); var before = runtime.Grid.CachedRowCount;
        var result = await runtime.ExportAsync(ReportOutputFormat.Csv, ReportExportScope.FilteredReport, [new("NAME")]);
        Assert.True(result.IsSuccess); Assert.Equal(ReportExportScope.FilteredReport, output.Last!.Scope); Assert.Equal(before, runtime.Grid.CachedRowCount);
        Assert.False((await runtime.ExportAsync(ReportOutputFormat.Docx, ReportExportScope.FullEligibleReport, [])).IsSuccess);
    }

    [Fact]
    public async Task Export_command_dispatches_one_semantic_request_and_retains_report_identity()
    {
        var provider = new FakeProvider(); var output = new FakeOutput();
        var runtime = new ReportRuntime(Definition(queryDefault: "x"), provider, output);
        var registry = new ActionCommandRegistry();
        var host = new ReportWorkspaceHost(runtime, new DictionaryLocalizationService("en-US"),
            () => Context(CompanyA), commandRegistry: registry, commandContext: CommandContext);
        await host.DispatchCommandAsync(host.RunCommandCode);

        var result = await host.DispatchCommandAsync(host.ExportCommandCode);

        Assert.Equal(ActionCommandResultStatus.Success, result.Status);
        Assert.Equal(1, output.RequestCount);
        Assert.Equal(runtime.Definition.ReportCode, output.Last!.ReportCode);
        Assert.Equal(runtime.Definition.ReportCode, runtime.LastOutputArtifact!.ReportCode);
    }

    [Fact]
    public async Task View_output_delegates_to_document_launcher_and_missing_launcher_fails_safely()
    {
        var capability = new ReportExportCapability(ReportOutputFormat.Csv,
            [ReportExportScope.FilteredReport], ReportOutputCapability.Export | ReportOutputCapability.View);
        var output = new FakeOutput(); var viewer = new FakeViewer();
        var runtime = new ReportRuntime(Definition(queryDefault: "x", exports: [capability]), new FakeProvider(), output,
            documentViewer: viewer);
        await runtime.RunAsync(Context(CompanyA));
        await runtime.ExportAsync(ReportOutputFormat.Csv, ReportExportScope.FilteredReport, [new("NAME")]);
        Assert.True((await runtime.ViewOutputAsync()).IsSuccess);
        Assert.Equal(runtime.Definition.ReportCode, viewer.Last!.Artifact.ReportCode);

        var withoutViewer = new ReportRuntime(Definition(queryDefault: "x", exports: [capability]), new FakeProvider(), output);
        await withoutViewer.RunAsync(Context(CompanyA));
        await withoutViewer.ExportAsync(ReportOutputFormat.Csv, ReportExportScope.FilteredReport, [new("NAME")]);
        Assert.Equal("REPORT_DOCUMENT_VIEW_UNAVAILABLE", (await withoutViewer.ViewOutputAsync()).DiagnosticCode);
    }

    [Fact]
    public async Task Drill_down_uses_semantic_context_and_rejects_stale_or_missing_rows()
    {
        var drill = new FakeDrill(); var runtime = new ReportRuntime(Definition(), new FakeProvider(), drillDownProvider: drill);
        runtime.SetParameter(new("QUERY"), "x"); await runtime.RunAsync(Context(CompanyA)); var row = runtime.Grid.Rows[0].RowKey;
        var target = await runtime.DrillDownAsync("VIEW", row, new("NAME"));
        Assert.Equal("DETAIL", target?.TargetCode); Assert.Equal(row, drill.Last?.RowKey);
        Assert.Null(await runtime.DrillDownAsync("VIEW", new("MISSING")));
    }

    [Fact]
    public async Task Repeated_runtime_language_switch_refreshes_text_without_reparenting_or_runtime_reset()
    {
        var localization = new DictionaryLocalizationService("en-US");
        var provider = new FakeProvider();
        var runtime = new ReportRuntime(Definition("Report.Activity.Title"), provider);
        runtime.SetParameter(new("QUERY"), "Tiếng Việt");
        await runtime.RunAsync(Context(CompanyA));
        runtime.Grid.Select([runtime.Grid.Rows[0].RowKey]);
        var host = new ReportWorkspaceHost(runtime, localization, () => Context(CompanyA));
        var visualTree = host.Content;
        var generation = runtime.Generation;
        var rows = runtime.Grid.Rows;
        var selection = runtime.Grid.SelectedRowKeys;
        var requestCount = provider.Requests.Count;

        foreach (var culture in new[] { "vi-VN", "en-US", "vi-VN" })
        {
            Assert.True(localization.TrySetCulture(culture));
            Assert.Same(visualTree, host.Content);
            Assert.Equal(culture == "vi-VN" ? "Bản ghi hoạt động" : "Activity records", host.PresentedTitle);
            Assert.Equal(2, host.ParameterEditorCount);
        }

        Assert.Equal(generation, runtime.Generation);
        Assert.Equal("Tiếng Việt", runtime.Parameters[new("QUERY")]);
        Assert.Equal(rows, runtime.Grid.Rows);
        Assert.Equal(selection, runtime.Grid.SelectedRowKeys);
        Assert.Equal(requestCount, provider.Requests.Count);
        Assert.Empty(runtime.Sorts);
        Assert.Empty(runtime.Filters);
        Assert.Empty(runtime.Groups);
    }

    private static ReportExecutionContext Context(CompanyDescriptor company) => new(company, "demo");
    private static ActionCommandExecutionContext CommandContext() => new(new(CompanyA,
        new("report-test", "Report Test", StandardTemplateCodes.Report), StandardTemplateCodes.Report,
        null, new(0), PresentationState.For(PresentationStateKind.Ready)));
    private static ReportDefinition Definition(string title = "Report.Title", UiAuthorizationBinding? authorization = null,
        object? queryDefault = null, IEnumerable<ReportExportCapability>? exports = null,
        IEnumerable<ReportActionDefinition>? actions = null) => new(new("ACTIVITY"), new(title),
        [new(new("NAME"), new("Report.Name"), ReportDataType.Text), new(new("AMOUNT"), new("Report.Amount"), ReportDataType.Decimal, IsAggregateEligible: true),
         new(new("STATUS"), new("Report.Status"), ReportDataType.Status), new(new("SECRET"), new("Report.Secret"), ReportDataType.Text,
             SensitiveContent: new(DynamicUI24.Core.Privacy.Sensitivity.Restricted, DynamicUI24.Core.Privacy.PrivacyPresentation.Mask))],
        [Parameter("QUERY", "Report.Query", EditorValueType.String, required: true, defaultValue: queryDefault),
         Parameter("ACTIVE", "Report.Active", EditorValueType.Boolean, defaultValue: true)],
        aggregates: [new(new("TOTAL"), new("AMOUNT"), ReportAggregateKind.Sum, ReportAggregateScope.Report)],
        drillDowns: [new("VIEW", new("Report.View"))],
        exports: exports ?? [new(ReportOutputFormat.Csv, [ReportExportScope.FilteredReport, ReportExportScope.FullEligibleReport])],
        authorization: authorization, actions: actions);

    private static ReportActionDefinition ReportAction(string code, ReportActionPlacement placement, int order,
        bool primary = false, bool selection = false, UiAuthorizationBinding? authorization = null) => new(new(code),
        $"REPORT.ACTIVITY.{code}", placement, new($"Report.Action.{code}"), StandardIconKeys.Action, order,
        primary, AuthorizationRequirement: authorization, RequiresSelection: selection);

    private static ReportParameterDefinition Parameter(string code, string label, EditorValueType type,
        bool required = false, object? defaultValue = null) => new(new(code),
        new(new($"REPORT_{code}"), new($"REPORT_PARAMETER:{code}"), type,
            chrome: new(LabelKey: new(label)), validation: new(IsRequired: required)), defaultValue);

    private class FakeProvider : IReportProvider, IReportFindProvider
    {
        public List<ReportRequest> Requests { get; } = [];
        public int ProducedRows { get; private set; }
        public virtual Task<ReportResult> ExecuteAsync(ReportRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request); var empty = request.Filters.Any(x => Equals(x.Value, "EMPTY"));
            var start = Math.Max(0, request.Window.StartIndex - request.Window.OverscanBefore);
            var count = empty ? 0 : Math.Min(request.Window.RowCount + request.Window.OverscanBefore + request.Window.OverscanAfter, 100_000 - start);
            var rows = Enumerable.Range(start, count).Select(i => new ReportRow(new($"R{i}"), new Dictionary<ReportColumnCode, object?>
            { [new("NAME")] = $"Record {i}", [new("AMOUNT")] = (decimal)i, [new("STATUS")] = i % 2 == 0 ? "Open" : "Closed", [new("SECRET")] = $"S{i}" }.ToImmutableDictionary())).ToImmutableArray();
            ProducedRows += rows.Length;
            return Task.FromResult(new ReportResult(rows, 100_000, empty ? 0 : 100_000,
                [new(new("TOTAL"), request.Context.Company.Code == "B" ? 200_000m : 100_000m)], request.Generation, start > 0, start + count < 100_000));
        }
        public Task<ReportFindResult> FindAsync(ReportFindRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(request.Query == "far" ? new ReportFindResult(true, new("R90000"), new("NAME"), 90_000) : new(false));
    }
    private sealed class DelayedProvider : FakeProvider
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int calls;
        public override async Task<ReportResult> ExecuteAsync(ReportRequest request, CancellationToken cancellationToken = default)
        { if (Interlocked.Increment(ref calls) == 1) { Started.TrySetResult(); await Release.Task; } return await base.ExecuteAsync(request, CancellationToken.None); }
    }
    private sealed class FakeOutput : IReportOutputProvider
    {
        public ReportExportRequest? Last { get; private set; }
        public int RequestCount { get; private set; }
        public Task<ReportOutputResult> ExportAsync(ReportExportRequest request, CancellationToken cancellationToken = default) { RequestCount++; Last = request; return Task.FromResult(new ReportOutputResult(true, new(request.ReportCode, request.Format, "stream://artifact"))); }
        public Task<ReportOutputResult> PrintAsync(ReportExportRequest request, CancellationToken cancellationToken = default) => ExportAsync(request, cancellationToken);
    }
    private sealed class FakeViewer : IDocumentViewLauncher
    {
        public DocumentViewRequest? Last { get; private set; }
        public Task<DocumentViewResult> LaunchAsync(DocumentViewRequest request, CancellationToken cancellationToken = default)
        { Last = request; return Task.FromResult(new DocumentViewResult(true)); }
    }
    private sealed class FakeDrill : IReportDrillDownProvider
    {
        public ReportDrillDownRequest? Last { get; private set; }
        public Task<ReportNavigationTarget?> ResolveAsync(ReportDrillDownRequest request, CancellationToken cancellationToken = default)
        { Last = request; return Task.FromResult<ReportNavigationTarget?>(new("WORKSPACE", "DETAIL")); }
    }
}
