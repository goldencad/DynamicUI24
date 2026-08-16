using System.Collections.Immutable;
using System.Text;
using System.Xml;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.ImportExport;
using DynamicUI24.Core.Setup;
using DynamicUI24.Extensions.Excel;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class ImportExportEngineTests
{
    [Fact]
    public async Task Csv_handles_quotes_escaped_quotes_empty_and_trailing_fields()
    {
        var parser = new DelimitedImportParser(ImportParserCodes.Csv, ',', "csv");
        var definition = Definition(ImportParserCodes.Csv, ',', mappings: []);
        await using var stream = Text("name,note,empty,tail\r\n\"Ada, A\",\"said \"\"hi\"\"\",,\r\n");
        var rows = await Collect(parser.ParseAsync(stream, new(definition, new())));
        Assert.Single(rows); Assert.Equal("Ada, A", rows[0].Values["name"]); Assert.Equal("said \"hi\"", rows[0].Values["note"]);
        Assert.Equal(string.Empty, rows[0].Values["empty"]); Assert.Equal(string.Empty, rows[0].Values["tail"]);
    }

    [Fact]
    public async Task Delimited_no_header_uses_stable_field_codes()
    {
        var parser = new DelimitedImportParser(ImportParserCodes.Tsv, '\t', "tsv");
        var definition = new ImportDefinition("id", "NO_HEADER", new("Import.Test"), ImportParserCodes.Tsv, ["tsv"], hasHeader: false, delimiter: '\t');
        await using var stream = Text("A\tB\n"); var rows = await Collect(parser.ParseAsync(stream, new(definition, new())));
        Assert.Equal("A", rows[0].Values["FIELD_1"]); Assert.Equal("B", rows[0].Values["FIELD_2"]);
    }

    [Fact]
    public async Task Json_supports_record_path_and_nested_properties()
    {
        var parser = new JsonImportParser(); var definition = new ImportDefinition("id", "JSON", new("Import.Test"), ImportParserCodes.Json, ["json"], recordPath: "data.items");
        await using var stream = Text("{\"data\":{\"items\":[{\"code\":\"1\",\"person\":{\"name\":\"Ada\"}}]}}");
        var rows = await Collect(parser.ParseAsync(stream, new(definition, new())));
        Assert.Equal("Ada", rows[0].Values["person.name"]);
    }

    [Fact]
    public async Task Xml_reads_elements_and_attributes_and_prohibits_dtd()
    {
        var parser = new XmlImportParser(); var definition = new ImportDefinition("id", "XML", new("Import.Test"), ImportParserCodes.Xml, ["xml"], recordPath: "records/record");
        await using var valid = Text("<records><record id=\"1\"><name>Ada</name></record></records>");
        var rows = await Collect(parser.ParseAsync(valid, new(definition, new()))); Assert.Equal("1", rows[0].Values["@id"]); Assert.Equal("Ada", rows[0].Values["name"]);
        await using var unsafeXml = Text("<!DOCTYPE r [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><records><record><name>&x;</name></record></records>");
        await Assert.ThrowsAnyAsync<XmlException>(async () => await Collect(parser.ParseAsync(unsafeXml, new(definition, new()))));
    }

    [Fact]
    public async Task Fixed_width_applies_boundaries_trim_and_rejects_short_lines()
    {
        var parser = new FixedWidthImportParser(); var definition = new ImportDefinition("id", "FW", new("Import.Test"), ImportParserCodes.FixedWidth, ["txt"],
            hasHeader: false, fixedWidthSchema: [new("CODE", 0, 4), new("NAME", 4, 6)]);
        await using var valid = Text("01  Ada   \n"); var rows = await Collect(parser.ParseAsync(valid, new(definition, new()))); Assert.Equal("01", rows[0].Values["CODE"]); Assert.Equal("Ada", rows[0].Values["NAME"]);
        await using var invalid = Text("short\n"); await Assert.ThrowsAsync<FormatException>(async () => await Collect(parser.ParseAsync(invalid, new(definition, new()))));
    }

    [Fact]
    public void Auto_map_is_deterministic_and_ambiguous_alias_is_unmapped()
    {
        var schema = new ImportSourceSchema("test", [new("ITEM_CODE", "Code", Ordinal: 0), new("same", "Same", Ordinal: 1)]);
        var columns = Grid.Columns;
        var aliases = new Dictionary<VariableCode, IEnumerable<string>> { [new("ITEM_CODE")] = ["same"], [new("ITEM_NAME")] = ["same"] };
        var result = ImportAutoMapper.Map(schema, columns, aliases);
        Assert.Contains(result.Mappings, x => x.TargetVariableCode == new VariableCode("ITEM_CODE") && x.SourceField == "ITEM_CODE");
        Assert.DoesNotContain(result.Mappings, x => x.SourceField == "same"); Assert.Contains(result.Diagnostics, x => x.Code == "IMPORT_AUTOMAP_AMBIGUOUS");
    }

    [Fact]
    public void Definition_validation_rejects_duplicate_formula_and_unknown_targets()
    {
        var registry = Registry(); var definition = Definition(ImportParserCodes.Csv, ',',
            [new("a", "a", new("ITEM_CODE")), new("b", "b", new("ITEM_CODE")), new("c", "c", new("TOTAL")), new("d", "d", new("UNKNOWN"))]);
        var diagnostics = ImportDefinitionValidator.Validate(definition, Grid, registry);
        Assert.Contains(diagnostics, x => x.Code == "IMPORT_DUPLICATE_TARGET"); Assert.Contains(diagnostics, x => x.Code == "IMPORT_TARGET_NOT_EDITABLE"); Assert.Contains(diagnostics, x => x.Code == "IMPORT_TARGET_UNKNOWN");
    }

    [Fact]
    public async Task Preview_of_100k_records_is_bounded()
    {
        var registry = Registry(); registry.Register(new GeneratedParser(100_000));
        var definition = new ImportDefinition("id", "LARGE", new("Import.Test"), GeneratedParser.Code, ["generated"],
            [new("code", "code", new("ITEM_CODE"), required: true)], maxPreviewRows: 100);
        var result = await new ImportEngine(registry).PreviewAsync(Stream.Null, definition, Grid);
        Assert.Equal(100, result.MaterializedRowCount); Assert.True(result.IsBounded); Assert.Equal(100, result.RecordsExamined);
    }

    [Fact]
    public async Task Atomic_invalid_import_never_calls_provider()
    {
        var registry = Registry(); var definition = Definition(ImportParserCodes.Csv, ',', [new("quantity", "quantity", new("QUANTITY"), required: true)]);
        await using var stream = Text("quantity\nnot-a-number\n"); var provider = new CapturingImportProvider();
        var result = await new ImportEngine(registry).CommitAsync(stream, definition, Grid, provider, Context());
        Assert.False(result.IsSuccess); Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task Partial_and_batched_import_use_bounded_provider_requests()
    {
        var registry = Registry(); registry.Register(new GeneratedParser(2_505));
        var definition = new ImportDefinition("id", "BATCH", new("Import.Test"), GeneratedParser.Code, ["generated"],
            [new("code", "code", new("ITEM_CODE"), required: true)], commitMode: ImportCommitMode.Batched);
        var provider = new CapturingImportProvider(); var result = await new ImportEngine(registry, new(BatchSize: 500)).CommitAsync(Stream.Null, definition, Grid, provider, Context());
        Assert.True(result.IsSuccess); Assert.Equal(2_505, result.ImportedRecords); Assert.Equal(6, provider.CallCount); Assert.True(provider.MaximumBatch <= 500);
    }

    [Fact]
    public async Task Export_streams_100k_rows_without_grid_materialization()
    {
        var registry = Registry(); var provider = new GeneratedExportProvider(100_000);
        var definition = new ExportDefinition("id", "CSV", new("Export.Test"), ExportWriterCodes.Csv, "csv",
            [new("code", "Code", new("ITEM_CODE"))], scope: ExportScope.AllRows);
        await using var output = new CountingStream(); var result = await new ExportEngine(registry).ExportAsync(output, definition, Grid, provider, Context());
        Assert.True(result.IsSuccess); Assert.Equal(100_000, result.RecordsWritten); Assert.Equal(100_000, provider.Generated); Assert.True(output.BytesWritten > 100_000);
    }

    [Fact]
    public async Task Xlsx_round_trip_preserves_basic_values_and_sheet_schema()
    {
        var writer = new XlsxExportWriter(); await using var stream = new MemoryStream();
        var fields = ImmutableArray.Create(new ExportFieldDefinition("code", "code", new("ITEM_CODE")), new ExportFieldDefinition("quantity", "quantity", new("QUANTITY")));
        var definition = new ExportDefinition("id", "XLSX", new("Export.Test"), ExportWriterCodes.Xlsx, "xlsx", fields);
        async IAsyncEnumerable<ExportRecord> Records() { yield return new(1, ImmutableDictionary<VariableCode, object?>.Empty.Add(new("ITEM_CODE"), "A01").Add(new("QUANTITY"), 12L)); await Task.CompletedTask; }
        Assert.True((await writer.WriteAsync(stream, new(definition, fields), Records())).IsSuccess);
        var import = new ImportDefinition("id", "XLSX", new("Import.Test"), ImportParserCodes.Xlsx, ["xlsx"]); var parser = new XlsxImportParser();
        var schema = await parser.InspectAsync(stream, new(import, new())); Assert.Contains("Data", schema.SheetNames);
        var rows = await Collect(parser.ParseAsync(stream, new(import, new()))); Assert.Equal("A01", rows[0].Values["code"]); Assert.Equal(12L, rows[0].Values["quantity"]);
    }

    [Fact]
    public void Session_fails_closed_when_context_changes()
    {
        var session = new ImportSession(Definition(ImportParserCodes.Csv, ',', []), Context()); Assert.True(session.MoveTo(ImportSessionState.Inspect));
        var changed = Context(new CompanyDescriptor(new("B"), "B", "Company B")); Assert.False(session.IsCurrent(changed)); session.Invalidate(); Assert.Equal(ImportSessionState.Invalidated, session.State);
    }

    private static ImportExportRegistry Registry() { var registry = new ImportExportRegistry(); BuiltInImportExportProviders.Register(registry); ExcelImportExportRegistration.Register(registry); return registry; }
    private static ImportDefinition Definition(string parser, char delimiter, IEnumerable<ImportFieldMapping> mappings) => new("id", "TEST", new("Import.Test"), parser, [parser.ToLowerInvariant()], mappings, delimiter: delimiter);
    private static MemoryStream Text(string text) => new(Encoding.UTF8.GetBytes(text));
    private static async Task<List<T>> Collect<T>(IAsyncEnumerable<T> source) { var result = new List<T>(); await foreach (var item in source) result.Add(item); return result; }
    private static ImportExportOperationContext Context(CompanyDescriptor? company = null) => new(new(company ?? new(new("A"), "A", "Company A"), "workspace"), null);
    private static ResolvedGridDefinition Grid => GridMetadataResolver.Resolve(new GridDefinition("id", "GRID",
        [Column("code", "ITEM_CODE", ColumnDataType.Text, ColumnMode.Input, true), Column("name", "ITEM_NAME", ColumnDataType.Text, ColumnMode.Input, true), Column("quantity", "QUANTITY", ColumnDataType.Integer, ColumnMode.Input, true), Column("total", "TOTAL", ColumnDataType.Formula, ColumnMode.Formula, false)], allowEdit: true), null);
    private static ColumnDefinition Column(string id, string variable, ColumnDataType type, ColumnMode mode, bool required) => new(id, id, new(variable), id, null, type,
        mode == ColumnMode.Input ? ColumnEditorKind.TextBox : ColumnEditorKind.Formula, mode, 0, 100, 50, 200, true, required, null, null, null, null, null, 1, SetupDefinitionStatus.Published);

    private sealed class GeneratedParser(int count) : IImportParserProvider
    { public const string Code = "GENERATED_TEST"; public string ParserCode => Code; public IReadOnlyCollection<string> FileExtensions => ["generated"];
      public Task<ImportSourceSchema> InspectAsync(Stream source, ImportParserContext context, CancellationToken cancellationToken = default) => Task.FromResult(new ImportSourceSchema("generated", [new("code", "Code")]));
      public async IAsyncEnumerable<ImportSourceRecord> ParseAsync(Stream source, ImportParserContext context, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
      { for (var i = 1; i <= count; i++) { cancellationToken.ThrowIfCancellationRequested(); yield return new(i, [new("code", $"I-{i:000000}")]); if ((i & 1023) == 0) await Task.Yield(); } } }
    private sealed class CapturingImportProvider : IGridBatchRowImportProvider
    { public int CallCount { get; private set; } public int MaximumBatch { get; private set; }
      public Task<ImportBatchResult> ImportRowsAsync(GridProviderContext context, ImportBatchRequest request, CancellationToken cancellationToken = default)
      { CallCount++; MaximumBatch = Math.Max(MaximumBatch, request.Rows.Length); return Task.FromResult(new ImportBatchResult(request.Rows.Select((_, i) => new ImportRowResult(true, new($"R{i}"))).ToImmutableArray())); } }
    private sealed class GeneratedExportProvider(int count) : IGridExportProvider
    { public int Generated { get; private set; }
      public async IAsyncEnumerable<GridRow> ExportRowsAsync(ExportProviderRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
      { for (var i = 1; i <= count; i++) { cancellationToken.ThrowIfCancellationRequested(); Generated++; yield return new(new($"R{i}"), new Dictionary<VariableCode, object?> { [new("ITEM_CODE")] = $"I-{i:000000}" }); if ((i & 1023) == 0) await Task.Yield(); } }
      public Task<long?> GetExportCountAsync(ExportProviderRequest request, CancellationToken cancellationToken = default) => Task.FromResult<long?>(count); }
    private sealed class CountingStream : Stream
    { public long BytesWritten { get; private set; } public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true; public override long Length => BytesWritten; public override long Position { get => BytesWritten; set => throw new NotSupportedException(); }
      public override void Flush() { } public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask; public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException(); public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(); public override void SetLength(long value) => throw new NotSupportedException(); public override void Write(byte[] buffer, int offset, int count) => BytesWritten += count; public override void Write(ReadOnlySpan<byte> buffer) => BytesWritten += buffer.Length; public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) { BytesWritten += buffer.Length; return ValueTask.CompletedTask; } }
}
