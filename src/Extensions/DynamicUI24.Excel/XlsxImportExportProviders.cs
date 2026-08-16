using System.Collections.Immutable;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using DynamicUI24.Core.ImportExport;

namespace DynamicUI24.Extensions.Excel;

public static class ExcelImportExportRegistration
{
    public static void Register(ImportExportRegistry registry)
    { registry.Register(new XlsxImportParser()); registry.Register(new XlsxExportWriter()); }
}

public sealed class XlsxImportParser : IImportParserProvider
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";
    public string ParserCode => ImportParserCodes.Xlsx;
    public IReadOnlyCollection<string> FileExtensions => ["xlsx"];

    public async Task<ImportSourceSchema> InspectAsync(Stream source, ImportParserContext context, CancellationToken cancellationToken = default)
    {
        var sheets = GetSheets(source); var rows = new List<ImportSourceRecord>();
        await foreach (var row in ParseAsync(source, context, cancellationToken)) { rows.Add(row); if (rows.Count >= context.Limits.SchemaSampleRows) break; }
        var fields = rows.SelectMany(x => x.Values.Keys).Distinct(StringComparer.OrdinalIgnoreCase).Select((x, i) => new ImportSourceField(x, x, Ordinal: i)).ToImmutableArray();
        return new(context.SourceName, fields, SheetNames: sheets.Select(x => x.Name).ToImmutableArray(), SampleRecords: rows.ToImmutableArray());
    }

    public async IAsyncEnumerable<ImportSourceRecord> ParseAsync(Stream source, ImportParserContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!source.CanSeek) throw new NotSupportedException("XLSX_IMPORT_REQUIRES_SEEKABLE_STREAM"); source.Position = 0;
        using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
        var shared = ReadSharedStrings(archive); var styles = ReadDateStyleIndexes(archive); var sheets = ReadSheets(archive);
        var selected = string.IsNullOrWhiteSpace(context.Definition.SheetSelector) ? sheets.FirstOrDefault() :
            sheets.FirstOrDefault(x => x.Name.Equals(context.Definition.SheetSelector, StringComparison.OrdinalIgnoreCase));
        if (selected == default) throw new FormatException("IMPORT_XLSX_SHEET_MISSING");
        var entry = archive.GetEntry(selected.Path) ?? throw new FormatException("IMPORT_XLSX_SHEET_MISSING");
        using var stream = entry.Open(); using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        List<string>? headers = null; long physical = -1, record = 0;
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "row") continue;
            physical++; var row = await ReadRowAsync(reader.ReadSubtree(), shared, styles, cancellationToken).ConfigureAwait(false);
            if (context.Definition.HasHeader && physical == context.Definition.HeaderRowIndex) { headers = ToHeaders(row); continue; }
            if (physical < context.Definition.DataStartRowIndex) continue;
            headers ??= Enumerable.Range(0, row.Count).Select(x => $"FIELD_{x + 1}").ToList();
            var values = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Math.Max(headers.Count, row.Count); i++) values[i < headers.Count ? headers[i] : $"FIELD_{i + 1}"] = row.GetValueOrDefault(i);
            if (values.Values.All(x => x is null or "") && context.Definition.EmptyRowPolicy == ImportEmptyRowPolicy.Skip) continue;
            yield return new(++record, values.ToImmutable());
        }
    }

    private static async Task<Dictionary<int, object?>> ReadRowAsync(XmlReader reader, IReadOnlyList<string> shared, HashSet<int> dateStyles, CancellationToken token)
    {
        using (reader) { var row = new Dictionary<int, object?>(); await reader.ReadAsync();
            while (await reader.ReadAsync())
            {
                token.ThrowIfCancellationRequested(); if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "c") continue;
                var reference = reader.GetAttribute("r") ?? "A1"; var type = reader.GetAttribute("t"); int.TryParse(reader.GetAttribute("s"), out var style);
                var column = ColumnIndex(reference); using var cell = reader.ReadSubtree(); string? raw = null;
                while (await cell.ReadAsync()) if (cell.NodeType == XmlNodeType.Element && cell.LocalName is "v" or "t") raw = await cell.ReadElementContentAsStringAsync();
                row[column] = Parse(raw, type, style, shared, dateStyles);
            } return row; }
    }
    private static object? Parse(string? raw, string? type, int style, IReadOnlyList<string> shared, HashSet<int> dateStyles)
    {
        if (raw is null) return null; if (type == "s" && int.TryParse(raw, out var index) && index >= 0 && index < shared.Count) return shared[index];
        if (type is "str" or "inlineStr") return raw; if (type == "b") return raw == "1";
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        { if (dateStyles.Contains(style)) return DateTime.FromOADate(number); if (Math.Abs(number % 1) < double.Epsilon && number <= long.MaxValue) return (long)number; return (decimal)number; }
        return raw;
    }
    private static List<string> ToHeaders(Dictionary<int, object?> row)
    { var max = row.Count == 0 ? -1 : row.Keys.Max(); var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); var result = new List<string>(); for (var i = 0; i <= max; i++) { var name = row.GetValueOrDefault(i)?.ToString()?.Trim(); name = string.IsNullOrEmpty(name) ? $"FIELD_{i + 1}" : name; used[name] = used.GetValueOrDefault(name) + 1; result.Add(used[name] == 1 ? name : $"{name}_{used[name]}"); } return result; }
    private static int ColumnIndex(string cellReference) { var value = 0; foreach (var ch in cellReference.TakeWhile(char.IsLetter)) value = value * 26 + char.ToUpperInvariant(ch) - 'A' + 1; return value - 1; }
    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    { var entry = archive.GetEntry("xl/sharedStrings.xml"); if (entry is null) return []; using var stream = entry.Open(); var document = XDocument.Load(stream); return document.Descendants(Main + "si").Select(x => string.Concat(x.Descendants(Main + "t").Select(y => y.Value))).ToArray(); }
    private static HashSet<int> ReadDateStyleIndexes(ZipArchive archive)
    { var result = new HashSet<int>(); var entry = archive.GetEntry("xl/styles.xml"); if (entry is null) return result; using var stream = entry.Open(); var document = XDocument.Load(stream); var custom = document.Descendants(Main + "numFmt").Where(x => IsDateFormat((string?)x.Attribute("formatCode"))).Select(x => (int?)x.Attribute("numFmtId") ?? -1).ToHashSet(); var xfs = document.Descendants(Main + "cellXfs").Elements(Main + "xf").ToArray(); for (var i = 0; i < xfs.Length; i++) { var id = (int?)xfs[i].Attribute("numFmtId") ?? 0; if (id is >= 14 and <= 22 || custom.Contains(id)) result.Add(i); } return result; }
    private static bool IsDateFormat(string? value) => value?.IndexOfAny(['d', 'y', 'h', 's']) >= 0;
    private static ImmutableArray<(string Name, string Path)> GetSheets(Stream source) { if (!source.CanSeek) return []; source.Position = 0; using var archive = new ZipArchive(source, ZipArchiveMode.Read, true); return ReadSheets(archive); }
    private static ImmutableArray<(string Name, string Path)> ReadSheets(ZipArchive archive)
    {
        var workbook = archive.GetEntry("xl/workbook.xml") ?? throw new FormatException("IMPORT_XLSX_WORKBOOK_MISSING");
        var relationships = archive.GetEntry("xl/_rels/workbook.xml.rels") ?? throw new FormatException("IMPORT_XLSX_RELATIONSHIPS_MISSING");
        using var ws = workbook.Open(); using var rs = relationships.Open(); var wd = XDocument.Load(ws); var rd = XDocument.Load(rs);
        var targets = rd.Descendants(PackageRel + "Relationship").ToDictionary(x => (string)x.Attribute("Id")!, x => (string)x.Attribute("Target")!);
        return wd.Descendants(Main + "sheet").Select(x => ((string)x.Attribute("name")!, NormalizeSheetPath(targets[(string)x.Attribute(Rel + "id")!]))).ToImmutableArray();
    }
    private static string NormalizeSheetPath(string value) { var path = value.Replace('\\', '/').TrimStart('/'); if (path.StartsWith("xl/")) return path; return "xl/" + path.Replace("../", ""); }
}

public sealed class XlsxExportWriter : IExportWriterProvider
{
    public string WriterCode => ExportWriterCodes.Xlsx; public IReadOnlyCollection<string> FileExtensions => ["xlsx"];
    public async Task<ExportWriteResult> WriteAsync(Stream destination, ExportWriterContext context, IAsyncEnumerable<ExportRecord> records,
        IProgress<ImportExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        Write(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
        Write(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
        Write(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Data\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
        Write(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
        var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest); await using var stream = entry.Open();
        var settings = new XmlWriterSettings { Async = true, Encoding = new UTF8Encoding(false), CloseOutput = false }; using var writer = XmlWriter.Create(stream, settings);
        await writer.WriteStartDocumentAsync(); await writer.WriteStartElementAsync(null, "worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"); await writer.WriteStartElementAsync(null, "sheetData", null); long row = 0;
        if (context.Definition.IncludeHeader) { await StartRow(++row); for (var column = 0; column < context.Fields.Length; column++) await Cell(context.Fields[column].OutputName, column, row); await writer.WriteEndElementAsync(); }
        await foreach (var record in records.WithCancellation(cancellationToken)) { await StartRow(++row); for (var column = 0; column < context.Fields.Length; column++) await Cell(record.Values.GetValueOrDefault(context.Fields[column].SourceVariableCode), column, row); await writer.WriteEndElementAsync(); }
        await writer.WriteEndElementAsync(); await writer.WriteEndElementAsync(); await writer.WriteEndDocumentAsync(); await writer.FlushAsync(); return new(row - (context.Definition.IncludeHeader ? 1 : 0), []);
        Task StartRow(long index) { writer.WriteStartElement("row"); writer.WriteAttributeString("r", index.ToString(CultureInfo.InvariantCulture)); return Task.CompletedTask; }
        async Task Cell(object? value, int column, long rowIndex) { await writer.WriteStartElementAsync(null, "c", null); writer.WriteAttributeString("r", $"{ColumnName(column)}{rowIndex}"); if (value is bool boolean) { writer.WriteAttributeString("t", "b"); await writer.WriteElementStringAsync(null, "v", null, boolean ? "1" : "0"); } else if (value is byte or short or int or long or float or double or decimal) await writer.WriteElementStringAsync(null, "v", null, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty); else { writer.WriteAttributeString("t", "inlineStr"); await writer.WriteStartElementAsync(null, "is", null); await writer.WriteElementStringAsync(null, "t", null, value switch { DateTime date => date.ToString("O", CultureInfo.InvariantCulture), DateOnly date => date.ToString("O", CultureInfo.InvariantCulture), _ => value?.ToString() ?? "" }); await writer.WriteEndElementAsync(); } await writer.WriteEndElementAsync(); }
    }
    private static void Write(ZipArchive archive, string path, string content) { var entry = archive.CreateEntry(path, CompressionLevel.Fastest); using var stream = entry.Open(); using var writer = new StreamWriter(stream, new UTF8Encoding(false)); writer.Write(content); }
    private static string ColumnName(int index) { var value = index + 1; var result = string.Empty; while (value > 0) { value--; result = (char)('A' + value % 26) + result; value /= 26; } return result; }
}
