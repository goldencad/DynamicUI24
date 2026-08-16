using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace DynamicUI24.Core.ImportExport;

public static class BuiltInImportExportProviders
{
    public static void Register(ImportExportRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Register(new DelimitedImportParser(ImportParserCodes.Csv, ',', "csv"));
        registry.Register(new DelimitedImportParser(ImportParserCodes.Tsv, '\t', "tsv"));
        registry.Register(new JsonImportParser()); registry.Register(new XmlImportParser()); registry.Register(new FixedWidthImportParser());
        registry.Register(new DelimitedExportWriter(ExportWriterCodes.Csv, ',', "csv"));
        registry.Register(new DelimitedExportWriter(ExportWriterCodes.Tsv, '\t', "tsv"));
        registry.Register(new JsonExportWriter()); registry.Register(new XmlExportWriter()); registry.Register(new FixedWidthExportWriter());
        BuiltInImportConverters.Register(registry);
    }
}

public sealed class DelimitedImportParser(string parserCode, char defaultDelimiter, string extension) : IImportParserProvider
{
    public string ParserCode { get; } = parserCode;
    public IReadOnlyCollection<string> FileExtensions { get; } = [extension];
    public async Task<ImportSourceSchema> InspectAsync(Stream source, ImportParserContext context, CancellationToken cancellationToken = default)
    {
        var rows = new List<ImportSourceRecord>();
        await foreach (var row in ParseAsync(source, context, cancellationToken).ConfigureAwait(false))
        { rows.Add(row); if (rows.Count >= context.Limits.SchemaSampleRows) break; }
        var fields = rows.SelectMany(x => x.Values.Keys).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select((x, i) => new ImportSourceField(x, x, Ordinal: i)).ToImmutableArray();
        return new(context.SourceName, fields, SampleRecords: rows.ToImmutableArray());
    }
    public async IAsyncEnumerable<ImportSourceRecord> ParseAsync(Stream source, ImportParserContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.CanSeek) source.Position = 0;
        using var reader = new StreamReader(source, context.Definition.Encoding, true, 4096, leaveOpen: true);
        var delimiter = context.Definition.Delimiter ?? defaultDelimiter;
        List<string>? headers = null; long physical = -1, record = 0;
        await foreach (var fields in ReadRecordsAsync(reader, delimiter, context.Limits, cancellationToken).ConfigureAwait(false))
        {
            physical++;
            if (physical < context.Definition.HeaderRowIndex) continue;
            if (context.Definition.HasHeader && physical == context.Definition.HeaderRowIndex)
            {
                headers = UniqueHeaders(fields); continue;
            }
            if (physical < context.Definition.DataStartRowIndex) continue;
            if (fields.All(string.IsNullOrEmpty) && context.Definition.EmptyRowPolicy == ImportEmptyRowPolicy.Skip) continue;
            headers ??= Enumerable.Range(0, fields.Count).Select(x => $"FIELD_{x + 1}").ToList();
            var values = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Math.Max(headers.Count, fields.Count); i++)
                values[i < headers.Count ? headers[i] : $"FIELD_{i + 1}"] = i < fields.Count ? fields[i] : null;
            yield return new(++record, values.ToImmutable());
        }
    }
    private static List<string> UniqueHeaders(List<string> fields)
    {
        var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); var result = new List<string>(fields.Count);
        for (var i = 0; i < fields.Count; i++)
        {
            var value = string.IsNullOrWhiteSpace(fields[i]) ? $"FIELD_{i + 1}" : fields[i].Trim();
            used[value] = used.GetValueOrDefault(value) + 1; result.Add(used[value] == 1 ? value : $"{value}_{used[value]}");
        }
        return result;
    }
    private static async IAsyncEnumerable<List<string>> ReadRecordsAsync(StreamReader reader, char delimiter,
        ImportSafetyLimits limits, [EnumeratorCancellation] CancellationToken token)
    {
        var fields = new List<string>(); var field = new StringBuilder(); var quoted = false; var any = false; var recordChars = 0; var skipLf = false;
        var buffer = new char[4096];
        while (true)
        {
            var count = await reader.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false);
            if (count == 0) break;
            for (var i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested(); var ch = buffer[i];
                if (skipLf) { skipLf = false; if (ch == '\n') continue; }
                any = true;
                if (++recordChars > limits.MaximumRecordCharacters) throw new FormatException("IMPORT_RECORD_TOO_LARGE");
                if (quoted)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < count && buffer[i + 1] == '"') { field.Append('"'); i++; recordChars++; }
                        else if (i + 1 == count && reader.Peek() == '"') { field.Append('"'); await reader.ReadAsync(new char[1], 0, 1).ConfigureAwait(false); recordChars++; }
                        else quoted = false;
                    }
                    else field.Append(ch);
                }
                else if (ch == '"' && field.Length == 0) quoted = true;
                else if (ch == delimiter) { AddField(); }
                else if (ch is '\r' or '\n')
                {
                    if (ch == '\r') { if (i + 1 < count && buffer[i + 1] == '\n') i++; else skipLf = true; }
                    AddField(); yield return fields; fields = []; field.Clear(); any = false; recordChars = 0;
                }
                else field.Append(ch);
                if (field.Length > limits.MaximumFieldCharacters) throw new FormatException("IMPORT_FIELD_TOO_LARGE");
            }
        }
        if (quoted) throw new FormatException("IMPORT_CSV_UNCLOSED_QUOTE");
        if (any || field.Length > 0 || fields.Count > 0) { AddField(); yield return fields; }
        void AddField() { if (fields.Count >= limits.MaximumFields) throw new FormatException("IMPORT_FIELD_LIMIT"); fields.Add(field.ToString()); field.Clear(); }
    }
}

public sealed class JsonImportParser : IImportParserProvider
{
    public string ParserCode => ImportParserCodes.Json;
    public IReadOnlyCollection<string> FileExtensions => ["json"];
    public async Task<ImportSourceSchema> InspectAsync(Stream source, ImportParserContext context, CancellationToken cancellationToken = default)
    {
        var rows = new List<ImportSourceRecord>();
        await foreach (var row in ParseAsync(source, context, cancellationToken)) { rows.Add(row); if (rows.Count >= context.Limits.SchemaSampleRows) break; }
        var fields = rows.SelectMany(x => x.Values.Keys).Distinct(StringComparer.OrdinalIgnoreCase).Select((x, i) => new ImportSourceField(x, x, Ordinal: i, Path: x)).ToImmutableArray();
        return new(context.SourceName, fields, SampleRecords: rows.ToImmutableArray());
    }
    public async IAsyncEnumerable<ImportSourceRecord> ParseAsync(Stream source, ImportParserContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source.CanSeek) source.Position = 0;
        using var document = await JsonDocument.ParseAsync(source, new JsonDocumentOptions { MaxDepth = 64 }, cancellationToken).ConfigureAwait(false);
        var records = Resolve(document.RootElement, context.Definition.RecordPath);
        if (records.ValueKind != JsonValueKind.Array) throw new FormatException("IMPORT_JSON_RECORD_PATH_NOT_ARRAY");
        long index = 0;
        foreach (var element in records.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested(); if (++index > context.Limits.MaximumRecords) throw new FormatException("IMPORT_RECORD_LIMIT");
            if (element.ValueKind != JsonValueKind.Object) throw new FormatException("IMPORT_JSON_RECORD_NOT_OBJECT");
            var values = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.OrdinalIgnoreCase);
            Flatten(element, null, values, context.Limits);
            yield return new(index, values.ToImmutable());
        }
    }
    private static JsonElement Resolve(JsonElement root, string? path)
    {
        var value = root; if (string.IsNullOrWhiteSpace(path)) return value;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(part, out value)) throw new FormatException("IMPORT_JSON_RECORD_PATH_MISSING");
        return value;
    }
    private static void Flatten(JsonElement element, string? prefix, ImmutableDictionary<string, object?>.Builder values, ImportSafetyLimits limits)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix is null ? property.Name : $"{prefix}.{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.Object) Flatten(property.Value, key, values, limits);
            else
            {
                if (values.Count >= limits.MaximumFields) throw new FormatException("IMPORT_FIELD_LIMIT");
                values[key] = property.Value.ValueKind switch
                { JsonValueKind.Null => null, JsonValueKind.String => property.Value.TryGetDateTime(out var date) ? date : property.Value.GetString(),
                  JsonValueKind.Number => property.Value.TryGetInt64(out var number) ? number : property.Value.GetDecimal(),
                  JsonValueKind.True => true, JsonValueKind.False => false, _ => property.Value.GetRawText() };
            }
        }
    }
}

public sealed class XmlImportParser : IImportParserProvider
{
    public string ParserCode => ImportParserCodes.Xml;
    public IReadOnlyCollection<string> FileExtensions => ["xml"];
    public async Task<ImportSourceSchema> InspectAsync(Stream source, ImportParserContext context, CancellationToken cancellationToken = default)
    {
        var rows = new List<ImportSourceRecord>(); await foreach (var row in ParseAsync(source, context, cancellationToken)) { rows.Add(row); if (rows.Count >= context.Limits.SchemaSampleRows) break; }
        var fields = rows.SelectMany(x => x.Values.Keys).Distinct(StringComparer.OrdinalIgnoreCase).Select((x, i) => new ImportSourceField(x, x, Ordinal: i, Path: x)).ToImmutableArray();
        return new(context.SourceName, fields, SampleRecords: rows.ToImmutableArray());
    }
    public async IAsyncEnumerable<ImportSourceRecord> ParseAsync(Stream source, ImportParserContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source.CanSeek) source.Position = 0;
        var recordName = (context.Definition.RecordPath ?? "record").Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        var settings = new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = (long)context.Limits.MaximumRecordCharacters * Math.Min(context.Limits.MaximumRecords, 1000) };
        using var reader = XmlReader.Create(source, settings); long index = 0;
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType != XmlNodeType.Element || !reader.LocalName.Equals(recordName, StringComparison.OrdinalIgnoreCase)) continue;
            using var subtree = reader.ReadSubtree(); var values = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.OrdinalIgnoreCase);
            await subtree.ReadAsync().ConfigureAwait(false);
            if (subtree.HasAttributes) while (subtree.MoveToNextAttribute()) values[$"@{subtree.LocalName}"] = subtree.Value;
            subtree.MoveToElement();
            while (await subtree.ReadAsync().ConfigureAwait(false))
                if (subtree.NodeType == XmlNodeType.Element)
                {
                    var name = subtree.LocalName; if (subtree.HasAttributes) while (subtree.MoveToNextAttribute()) values[$"{name}.@{subtree.LocalName}"] = subtree.Value;
                    subtree.MoveToElement(); if (!subtree.IsEmptyElement) values[name] = await subtree.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    if (values.Count > context.Limits.MaximumFields) throw new FormatException("IMPORT_FIELD_LIMIT");
                }
            yield return new(++index, values.ToImmutable());
        }
    }
}

public sealed class FixedWidthImportParser : IImportParserProvider
{
    public string ParserCode => ImportParserCodes.FixedWidth;
    public IReadOnlyCollection<string> FileExtensions => ["txt", "fw"];
    public Task<ImportSourceSchema> InspectAsync(Stream source, ImportParserContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ImportSourceSchema(context.SourceName, context.Definition.FixedWidthSchema.Select((x, i) => new ImportSourceField(x.FieldCode, x.FieldCode, x.DataType, i)).ToImmutableArray()));
    public async IAsyncEnumerable<ImportSourceRecord> ParseAsync(Stream source, ImportParserContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (context.Definition.FixedWidthSchema.Length == 0) throw new FormatException("IMPORT_FIXED_WIDTH_SCHEMA_MISSING");
        if (source.CanSeek) source.Position = 0; using var reader = new StreamReader(source, context.Definition.Encoding, true, leaveOpen: true); long index = 0;
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (line.Length > context.Limits.MaximumRecordCharacters) throw new FormatException("IMPORT_RECORD_TOO_LARGE");
            var values = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in context.Definition.FixedWidthSchema)
            {
                if (line.Length < field.Start + field.Length) throw new FormatException("IMPORT_FIXED_WIDTH_SHORT_LINE");
                var value = line.Substring(field.Start, field.Length); values[field.FieldCode] = field.TrimMode switch
                { FixedWidthTrimMode.Start => value.TrimStart(), FixedWidthTrimMode.End => value.TrimEnd(), FixedWidthTrimMode.Both => value.Trim(), _ => value };
            }
            yield return new(++index, values.ToImmutable());
        }
    }
}

public sealed class DelimitedExportWriter(string writerCode, char delimiter, string extension) : IExportWriterProvider
{
    public string WriterCode { get; } = writerCode;
    public IReadOnlyCollection<string> FileExtensions { get; } = [extension];
    public async Task<ExportWriteResult> WriteAsync(Stream destination, ExportWriterContext context, IAsyncEnumerable<ExportRecord> records,
        IProgress<ImportExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        using var writer = new StreamWriter(destination, context.Definition.Encoding, 4096, leaveOpen: true); long count = 0;
        if (context.Definition.IncludeHeader) await writer.WriteLineAsync(string.Join(delimiter, context.Fields.Select(x => Quote(x.OutputName)))).ConfigureAwait(false);
        await foreach (var record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
        { await writer.WriteLineAsync(string.Join(delimiter, context.Fields.Select(x => Quote(Format(record.Values.GetValueOrDefault(x.SourceVariableCode), x.Format))))).ConfigureAwait(false); count++; }
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false); return new(count, []);
    }
    private string Quote(string? value) { value ??= string.Empty; return value.IndexOfAny([delimiter, '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value; }
    internal static string Format(object? value, string? format) => value switch { null => "", IFormattable x => x.ToString(format, System.Globalization.CultureInfo.InvariantCulture), _ => value.ToString() ?? "" };
}

public sealed class JsonExportWriter : IExportWriterProvider
{
    public string WriterCode => ExportWriterCodes.Json; public IReadOnlyCollection<string> FileExtensions => ["json"];
    public async Task<ExportWriteResult> WriteAsync(Stream destination, ExportWriterContext context, IAsyncEnumerable<ExportRecord> records, IProgress<ImportExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        using var writer = new Utf8JsonWriter(destination, new JsonWriterOptions { Indented = false }); long count = 0; writer.WriteStartArray();
        await foreach (var record in records.WithCancellation(cancellationToken))
        { writer.WriteStartObject(); foreach (var field in context.Fields) { writer.WritePropertyName(field.OutputName); JsonSerializer.Serialize(writer, record.Values.GetValueOrDefault(field.SourceVariableCode)); } writer.WriteEndObject(); count++; }
        writer.WriteEndArray(); await writer.FlushAsync(cancellationToken); return new(count, []);
    }
}

public sealed class XmlExportWriter : IExportWriterProvider
{
    public string WriterCode => ExportWriterCodes.Xml; public IReadOnlyCollection<string> FileExtensions => ["xml"];
    public async Task<ExportWriteResult> WriteAsync(Stream destination, ExportWriterContext context, IAsyncEnumerable<ExportRecord> records, IProgress<ImportExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var settings = new XmlWriterSettings { Async = true, Encoding = context.Definition.Encoding, CloseOutput = false, Indent = true };
        using var writer = XmlWriter.Create(destination, settings); await writer.WriteStartDocumentAsync(); await writer.WriteStartElementAsync(null, "records", null); long count = 0;
        await foreach (var record in records.WithCancellation(cancellationToken))
        { await writer.WriteStartElementAsync(null, "record", null); foreach (var field in context.Fields) { await writer.WriteStartElementAsync(null, XmlConvert.EncodeLocalName(field.OutputName), null); await writer.WriteStringAsync(DelimitedExportWriter.Format(record.Values.GetValueOrDefault(field.SourceVariableCode), field.Format)); await writer.WriteEndElementAsync(); } await writer.WriteEndElementAsync(); count++; }
        await writer.WriteEndElementAsync(); await writer.WriteEndDocumentAsync(); await writer.FlushAsync(); return new(count, []);
    }
}

public sealed class FixedWidthExportWriter : IExportWriterProvider
{
    public string WriterCode => ExportWriterCodes.FixedWidth; public IReadOnlyCollection<string> FileExtensions => ["txt", "fw"];
    public async Task<ExportWriteResult> WriteAsync(Stream destination, ExportWriterContext context, IAsyncEnumerable<ExportRecord> records, IProgress<ImportExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        using var writer = new StreamWriter(destination, context.Definition.Encoding, leaveOpen: true); long count = 0;
        await foreach (var record in records.WithCancellation(cancellationToken))
        { foreach (var field in context.Fields) { var width = context.Definition.WriterOptions.TryGetValue($"WIDTH:{field.TargetFieldCode}", out var value) && int.TryParse(value?.ToString(), out var parsed) ? parsed : 20; var text = DelimitedExportWriter.Format(record.Values.GetValueOrDefault(field.SourceVariableCode), field.Format); await writer.WriteAsync(text.Length > width ? text[..width] : text.PadRight(width)); } await writer.WriteLineAsync(); count++; }
        await writer.FlushAsync(cancellationToken); return new(count, []);
    }
}
