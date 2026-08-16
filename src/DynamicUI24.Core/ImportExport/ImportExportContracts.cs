using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.ImportExport;

public sealed record ImportSourceField(string SourceFieldCode, string DisplayName,
    ColumnDataType? DataTypeHint = null, int? Ordinal = null, string? Path = null);

public sealed record ImportSourceSchema(string SourceName, ImmutableArray<ImportSourceField> Fields,
    long? DetectedRecordCount = null, ImmutableArray<string> SheetNames = default,
    ImmutableArray<ImportSourceRecord> SampleRecords = default,
    ImmutableDictionary<string, object?>? Metadata = null)
{
    public ImmutableArray<string> SheetNames { get; init; } = SheetNames.IsDefault ? [] : SheetNames;
    public ImmutableArray<ImportSourceRecord> SampleRecords { get; init; } = SampleRecords.IsDefault ? [] : SampleRecords;
    public ImmutableDictionary<string, object?> Metadata { get; init; } = Metadata
        ?? ImmutableDictionary<string, object?>.Empty;
}

public sealed record ImportSourceRecord(long RecordIndex, ImmutableDictionary<string, object?> Values)
{
    public ImportSourceRecord(long recordIndex, IEnumerable<KeyValuePair<string, object?>> values)
        : this(recordIndex, values.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase)) { }
    public bool TryGetValue(string field, out object? value) => Values.TryGetValue(field, out value);
}

public sealed record ImportDiagnostic(ImportDiagnosticSeverity Severity, string Code, string SafeMessage,
    long? RecordIndex = null, string? SourceField = null, VariableCode? TargetVariableCode = null,
    string? RawValuePreview = null, string? ExceptionCategory = null)
{
    public static ImportDiagnostic Error(string code, string message, long? record = null, string? field = null,
        VariableCode? target = null, object? raw = null, Exception? exception = null) =>
        new(ImportDiagnosticSeverity.Error, code, message, record, field, target, SafePreview(raw), exception?.GetType().Name);
    public static ImportDiagnostic Warning(string code, string message, long? record = null, string? field = null,
        VariableCode? target = null) => new(ImportDiagnosticSeverity.Warning, code, message, record, field, target);
    private static string? SafePreview(object? value)
    {
        if (value is null) return null;
        var text = value.ToString() ?? string.Empty;
        return text.Length <= 128 ? text : text[..128] + "…";
    }
}

public sealed record ImportSafetyLimits(int MaximumFields = 1024, long MaximumRecords = 2_000_000,
    int MaximumRecordCharacters = 4_000_000, int MaximumFieldCharacters = 1_000_000,
    int MaximumDetailedDiagnostics = 1_000, int BatchSize = 1_000, int SchemaSampleRows = 25);

public sealed record ImportParserContext(ImportDefinition Definition, ImportSafetyLimits Limits, string SourceName = "stream");

/// <summary>Format adapter only. Implementations inspect and parse data; they never map or commit business values.</summary>
public interface IImportParserProvider
{
    string ParserCode { get; }
    IReadOnlyCollection<string> FileExtensions { get; }
    Task<ImportSourceSchema> InspectAsync(Stream source, ImportParserContext context, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ImportSourceRecord> ParseAsync(Stream source, ImportParserContext context, CancellationToken cancellationToken = default);
}

public sealed record ExportWriterContext(ExportDefinition Definition, ImmutableArray<ExportFieldDefinition> Fields);

/// <summary>Format adapter only. Rows are already authorized and resolved before reaching a writer.</summary>
public interface IExportWriterProvider
{
    string WriterCode { get; }
    IReadOnlyCollection<string> FileExtensions { get; }
    Task<ExportWriteResult> WriteAsync(Stream destination, ExportWriterContext context,
        IAsyncEnumerable<ExportRecord> records, IProgress<ImportExportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record ExportRecord(long RecordIndex, ImmutableDictionary<VariableCode, object?> Values);
public sealed record ExportWriteResult(long RecordsWritten, ImmutableArray<ImportDiagnostic> Diagnostics)
{
    public bool IsSuccess => Diagnostics.All(x => x.Severity != ImportDiagnosticSeverity.Error);
}

public sealed record ImportExportProgress(string Stage, long ProcessedRecords, long? TotalRecords = null)
{
    public double? Percentage => TotalRecords is > 0 ? Math.Clamp((double)ProcessedRecords / TotalRecords.Value * 100d, 0d, 100d) : null;
}

public interface IImportValueConverter
{
    string ConverterCode { get; }
    ValueTask<object?> ConvertAsync(object? value, ImportConversionContext context, CancellationToken cancellationToken = default);
}

public sealed record ImportConversionContext(ImportFieldMapping Mapping, ColumnDefinition TargetColumn,
    IFormatProvider FormatProvider);

public sealed record ImportCandidateRecord(long RecordIndex, ImmutableDictionary<VariableCode, object?> Values,
    ImmutableArray<ImportDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.All(x => x.Severity != ImportDiagnosticSeverity.Error);
}

public sealed record ImportPreviewResult(ImmutableArray<ImportCandidateRecord> Rows,
    ImmutableArray<ImportDiagnostic> Diagnostics, long RecordsExamined, long ValidRows, long WarningRows,
    long InvalidRows, bool IsBounded, int MaxPreviewRows, long TotalDiagnosticCount)
{
    public int MaterializedRowCount => Rows.Length;
}

public sealed record ImportBatchRow(ImmutableDictionary<VariableCode, object?> Values,
    ImmutableArray<VariableCode> MatchKeyVariableCodes, ImportMutationMode MutationMode);

public sealed record ImportBatchRequest(Guid TransactionId, ImmutableArray<ImportBatchRow> Rows,
    ImportCommitMode CommitMode, string ImportCode, CompanyId CompanyId, string WorkspaceId);

public sealed record ImportRowResult(bool IsSuccess, RowKey? RowKey = null, string? DiagnosticCode = null);
public sealed record ImportBatchResult(ImmutableArray<ImportRowResult> Rows, string? DiagnosticCode = null)
{
    public bool IsSuccess => DiagnosticCode is null && Rows.All(x => x.IsSuccess);
}

/// <summary>Optional DataEntry capability for inserts/updates. The request remains VariableCode-based.</summary>
public interface IGridBatchRowImportProvider
{
    Task<ImportBatchResult> ImportRowsAsync(GridProviderContext context, ImportBatchRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ExportProviderRequest(GridProviderContext Context, ExportScope Scope,
    ImmutableArray<VariableCode> VariableCodes, ImmutableArray<RowKey> SelectedRowKeys,
    ImmutableArray<GridSortDefinition> Sorts, ImmutableArray<GridFilterDefinition> Filters, int BatchSize);

/// <summary>Streams logical rows independently from Grid visual materialization.</summary>
public interface IGridExportProvider
{
    IAsyncEnumerable<GridRow> ExportRowsAsync(ExportProviderRequest request, CancellationToken cancellationToken = default);
    Task<long?> GetExportCountAsync(ExportProviderRequest request, CancellationToken cancellationToken = default);
}

public sealed record ImportExportOperationContext(GridProviderContext GridContext,
    EffectiveAuthorizationContext? Authorization, long Generation = 0);

public sealed class ImportExportRegistry
{
    private readonly Dictionary<string, IImportParserProvider> parsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IExportWriterProvider> writers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IImportValueConverter> converters = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<IImportParserProvider> Parsers => parsers.Values;
    public IReadOnlyCollection<IExportWriterProvider> Writers => writers.Values;
    public IReadOnlyCollection<IImportValueConverter> Converters => converters.Values;
    public void Register(IImportParserProvider parser) => Add(parsers, parser.ParserCode, parser);
    public void Register(IExportWriterProvider writer) => Add(writers, writer.WriterCode, writer);
    public void Register(IImportValueConverter converter) => Add(converters, converter.ConverterCode, converter);
    public bool TryGetParser(string code, out IImportParserProvider provider) => parsers.TryGetValue(code, out provider!);
    public bool TryGetWriter(string code, out IExportWriterProvider provider) => writers.TryGetValue(code, out provider!);
    public bool TryGetConverter(string code, out IImportValueConverter converter) => converters.TryGetValue(code, out converter!);
    private static void Add<T>(Dictionary<string, T> target, string code, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code); ArgumentNullException.ThrowIfNull(value);
        if (!target.TryAdd(code.Trim().ToUpperInvariant(), value)) throw new InvalidOperationException($"Provider '{code}' is already registered.");
    }
}
