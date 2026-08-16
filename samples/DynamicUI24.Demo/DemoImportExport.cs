using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using DynamicUI24.Core.ImportExport;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Demo;

internal static class DemoImportExport
{
    public const string CustomCode = "CUSTOM_DEMO";
    public static ImportExportRegistry CreateRegistry()
    {
        var registry = new ImportExportRegistry(); BuiltInImportExportProviders.Register(registry);
        DynamicUI24.Extensions.Excel.ExcelImportExportRegistration.Register(registry);
        registry.Register(new DemoParser()); registry.Register(new DemoWriter()); return registry;
    }
    public static IReadOnlyList<ImportDefinition> ImportProfiles { get; } =
    [
        Definition("xlsx", ImportParserCodes.Xlsx, "xlsx"), Definition("csv", ImportParserCodes.Csv, "csv", ','),
        Definition("tsv", ImportParserCodes.Tsv, "tsv", '\t'), Definition("json", ImportParserCodes.Json, "json"),
        Definition("xml", ImportParserCodes.Xml, "xml"),
        new("fixed", "FIXED_DEMO", new("Import.FixedWidth"), ImportParserCodes.FixedWidth, ["txt"],
            fixedWidthSchema: [new("ITEM_CODE", 0, 10), new("ITEM_NAME", 10, 30), new("QUANTITY", 40, 8, dataType: ColumnDataType.Integer)]),
        Definition("custom", CustomCode, "demo")
    ];
    public static IReadOnlyList<ExportDefinition> ExportProfiles { get; } =
    [
        Export("csv", ExportWriterCodes.Csv, "csv"), Export("tsv", ExportWriterCodes.Tsv, "tsv"),
        Export("json", ExportWriterCodes.Json, "json"), Export("xml", ExportWriterCodes.Xml, "xml"),
        Export("xlsx", ExportWriterCodes.Xlsx, "xlsx"), Export("custom", CustomCode, "demo")
    ];
    private static ImportDefinition Definition(string id, string parser, string extension, char? delimiter = null) =>
        new(id, $"DEMO_{parser}", new($"Import.{parser}"), parser, [extension], delimiter: delimiter,
            mappings: [new("code", "code", new("ITEM_CODE"), required: true), new("name", "name", new("ITEM_NAME"), 10, required: true), new("quantity", "quantity", new("QUANTITY"), 20, converterCode: "TEXT_TO_INTEGER")]);
    private static ExportDefinition Export(string id, string writer, string extension) => new(id, $"DEMO_{writer}",
        new($"Export.{writer}"), writer, extension,
        [new("code", "Code", new("ITEM_CODE")), new("name", "Name", new("ITEM_NAME"), 10), new("quantity", "Quantity", new("QUANTITY"), 20)]);

    private sealed class DemoParser : IImportParserProvider
    {
        public string ParserCode => CustomCode; public IReadOnlyCollection<string> FileExtensions => ["demo"];
        public async Task<ImportSourceSchema> InspectAsync(Stream source, ImportParserContext context, CancellationToken cancellationToken = default)
        { var samples = new List<ImportSourceRecord>(); await foreach (var row in ParseAsync(source, context, cancellationToken)) { samples.Add(row); if (samples.Count == 5) break; } return new(context.SourceName,
            [new("code", "Code", ColumnDataType.Text, 0), new("name", "Name", ColumnDataType.Text, 1), new("quantity", "Quantity", ColumnDataType.Integer, 2)], SampleRecords: samples.ToImmutableArray()); }
        public async IAsyncEnumerable<ImportSourceRecord> ParseAsync(Stream source, ImportParserContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source.CanSeek) source.Position = 0; using var reader = new StreamReader(source, context.Definition.Encoding, leaveOpen: true);
            Dictionary<string, object?>? values = null; long index = 0;
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (line == "@record") { values = new(StringComparer.OrdinalIgnoreCase); continue; }
                if (line == "@end") { if (values is not null) yield return new(++index, values); values = null; continue; }
                var separator = line.IndexOf('='); if (values is not null && separator > 0) values[line[..separator].Trim()] = line[(separator + 1)..].Trim();
            }
            if (values is not null) throw new FormatException("CUSTOM_DEMO_UNCLOSED_RECORD");
        }
    }
    private sealed class DemoWriter : IExportWriterProvider
    {
        public string WriterCode => CustomCode; public IReadOnlyCollection<string> FileExtensions => ["demo"];
        public async Task<ExportWriteResult> WriteAsync(Stream destination, ExportWriterContext context, IAsyncEnumerable<ExportRecord> records,
            IProgress<ImportExportProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            using var writer = new StreamWriter(destination, context.Definition.Encoding, leaveOpen: true); long count = 0;
            await foreach (var record in records.WithCancellation(cancellationToken)) { await writer.WriteLineAsync("@record"); foreach (var field in context.Fields) await writer.WriteLineAsync($"{field.TargetFieldCode}={record.Values.GetValueOrDefault(field.SourceVariableCode)}"); await writer.WriteLineAsync("@end"); count++; }
            await writer.FlushAsync(cancellationToken); return new(count, []);
        }
    }
}
