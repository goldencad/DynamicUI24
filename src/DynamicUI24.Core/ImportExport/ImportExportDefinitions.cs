using System.Collections.Immutable;
using System.Text;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.ImportExport;

public static class ImportParserCodes
{
    public const string Csv = "CSV";
    public const string Tsv = "TSV";
    public const string Json = "JSON";
    public const string Xml = "XML";
    public const string FixedWidth = "FIXED_WIDTH";
    public const string Xlsx = "XLSX";
    public const string CustomProvider = "CUSTOM_PROVIDER";
}

public static class ExportWriterCodes
{
    public const string Csv = "CSV";
    public const string Tsv = "TSV";
    public const string Json = "JSON";
    public const string Xml = "XML";
    public const string FixedWidth = "FIXED_WIDTH";
    public const string Xlsx = "XLSX";
    public const string CustomProvider = "CUSTOM_PROVIDER";
}

public enum ImportValidationMode { ValidateAll, ValidateMapped }
public enum ImportCommitMode { Atomic, PartialValid, Batched }
public enum ImportMutationMode { InsertOnly, UpdateOnly, Upsert }
public enum ImportDuplicatePolicy { Reject, KeepFirst, KeepLast, UpdateExisting, Skip }
public enum ImportEmptyRowPolicy { Skip, Keep, Reject }
public enum ImportNullPolicy { EmptyAsNull, EmptyAsEmptyString, DefaultValue, RejectRequired }
public enum FixedWidthTrimMode { None, Start, End, Both }
public enum ImportDiagnosticSeverity { Information, Warning, Error }
public enum ImportSessionState { SelectSource, Inspect, Map, Preview, Validate, Ready, Committing, Completed, Failed, Cancelled, Invalidated }
public enum ExportScope { CurrentView, SelectedRows, AllFiltered, AllRows }

public sealed record FixedWidthFieldDefinition
{
    public FixedWidthFieldDefinition(string fieldCode, int start, int length,
        FixedWidthTrimMode trimMode = FixedWidthTrimMode.Both, ColumnDataType? dataType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldCode);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        FieldCode = fieldCode.Trim(); Start = start; Length = length; TrimMode = trimMode; DataType = dataType;
    }
    public string FieldCode { get; }
    public int Start { get; }
    public int Length { get; }
    public FixedWidthTrimMode TrimMode { get; }
    public ColumnDataType? DataType { get; }
}

public sealed record ImportFieldMapping
{
    public ImportFieldMapping(string mappingId, string sourceField, VariableCode targetVariableCode,
        int displayOrder = 0, int? sourceIndex = null, bool required = false,
        ColumnDataType? dataTypeOverride = null, string? converterCode = null, object? defaultValue = null,
        bool trim = true, IEnumerable<string>? nullTokens = null,
        PermissionCode? permissionCode = null, CapabilityCode? capabilityCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mappingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceField);
        if (sourceIndex < 0) throw new ArgumentOutOfRangeException(nameof(sourceIndex));
        MappingId = mappingId.Trim(); SourceField = sourceField.Trim(); TargetVariableCode = targetVariableCode;
        DisplayOrder = displayOrder; SourceIndex = sourceIndex; Required = required; DataTypeOverride = dataTypeOverride;
        ConverterCode = Normalize(converterCode); DefaultValue = defaultValue; Trim = trim;
        NullTokens = (nullTokens ?? []).Select(x => x.Trim()).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        PermissionCode = permissionCode; CapabilityCode = capabilityCode;
    }
    public string MappingId { get; }
    public string SourceField { get; }
    public int? SourceIndex { get; }
    public VariableCode TargetVariableCode { get; }
    public int DisplayOrder { get; }
    public bool Required { get; }
    public ColumnDataType? DataTypeOverride { get; }
    public string? ConverterCode { get; }
    public object? DefaultValue { get; }
    public bool Trim { get; }
    public ImmutableHashSet<string> NullTokens { get; }
    public PermissionCode? PermissionCode { get; }
    public CapabilityCode? CapabilityCode { get; }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}

public sealed record ImportDefinition
{
    public const int DefaultMaxPreviewRows = 200;
    public ImportDefinition(string importId, string importCode, LocalizationKey displayNameKey, string parserCode,
        IEnumerable<string> fileExtensions, IEnumerable<ImportFieldMapping>? mappings = null,
        Encoding? encoding = null, bool hasHeader = true, int headerRowIndex = 0, int? dataStartRowIndex = null,
        string? sheetSelector = null, string? recordPath = null, char? delimiter = null,
        IEnumerable<FixedWidthFieldDefinition>? fixedWidthSchema = null,
        ImportValidationMode validationMode = ImportValidationMode.ValidateAll,
        ImportCommitMode commitMode = ImportCommitMode.Atomic,
        ImportMutationMode mutationMode = ImportMutationMode.InsertOnly,
        ImportDuplicatePolicy duplicatePolicy = ImportDuplicatePolicy.Reject,
        ImportEmptyRowPolicy emptyRowPolicy = ImportEmptyRowPolicy.Skip,
        ImportNullPolicy nullPolicy = ImportNullPolicy.EmptyAsNull,
        int maxPreviewRows = DefaultMaxPreviewRows, IEnumerable<VariableCode>? matchKeyVariableCodes = null,
        PermissionCode? permissionCode = null, CapabilityCode? capabilityCode = null,
        IReadOnlyDictionary<string, object?>? parserOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(importId); ArgumentException.ThrowIfNullOrWhiteSpace(importCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(parserCode); ArgumentOutOfRangeException.ThrowIfNegative(headerRowIndex);
        if (dataStartRowIndex < 0) throw new ArgumentOutOfRangeException(nameof(dataStartRowIndex));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPreviewRows);
        ImportId = importId.Trim(); ImportCode = importCode.Trim().ToUpperInvariant(); DisplayNameKey = displayNameKey;
        ParserCode = parserCode.Trim().ToUpperInvariant();
        FileExtensions = fileExtensions.Select(NormalizeExtension).Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableArray();
        Mappings = (mappings ?? []).OrderBy(x => x.DisplayOrder).ToImmutableArray(); Encoding = encoding ?? Encoding.UTF8;
        HasHeader = hasHeader; HeaderRowIndex = headerRowIndex; DataStartRowIndex = dataStartRowIndex ?? (hasHeader ? headerRowIndex + 1 : headerRowIndex);
        SheetSelector = Null(sheetSelector); RecordPath = Null(recordPath); Delimiter = delimiter;
        FixedWidthSchema = (fixedWidthSchema ?? []).ToImmutableArray(); ValidationMode = validationMode; CommitMode = commitMode;
        MutationMode = mutationMode; DuplicatePolicy = duplicatePolicy; EmptyRowPolicy = emptyRowPolicy; NullPolicy = nullPolicy;
        MaxPreviewRows = maxPreviewRows; MatchKeyVariableCodes = (matchKeyVariableCodes ?? []).ToImmutableArray();
        PermissionCode = permissionCode; CapabilityCode = capabilityCode;
        ParserOptions = (parserOptions ?? new Dictionary<string, object?>()).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
    }
    public string ImportId { get; }
    public string ImportCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public string ParserCode { get; }
    public ImmutableArray<string> FileExtensions { get; }
    public Encoding Encoding { get; }
    public bool HasHeader { get; }
    public int HeaderRowIndex { get; }
    public int DataStartRowIndex { get; }
    public string? SheetSelector { get; }
    public string? RecordPath { get; }
    public char? Delimiter { get; }
    public ImmutableArray<FixedWidthFieldDefinition> FixedWidthSchema { get; }
    public ImmutableArray<ImportFieldMapping> Mappings { get; }
    public ImportValidationMode ValidationMode { get; }
    public ImportCommitMode CommitMode { get; }
    public ImportMutationMode MutationMode { get; }
    public ImportDuplicatePolicy DuplicatePolicy { get; }
    public ImportEmptyRowPolicy EmptyRowPolicy { get; }
    public ImportNullPolicy NullPolicy { get; }
    public int MaxPreviewRows { get; }
    public ImmutableArray<VariableCode> MatchKeyVariableCodes { get; }
    public PermissionCode? PermissionCode { get; }
    public CapabilityCode? CapabilityCode { get; }
    public ImmutableDictionary<string, object?> ParserOptions { get; }
    private static string NormalizeExtension(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); return value.Trim().TrimStart('.').ToLowerInvariant(); }
    private static string? Null(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record ExportFieldDefinition
{
    public ExportFieldDefinition(string targetFieldCode, string outputName, VariableCode sourceVariableCode,
        int displayOrder = 0, string? format = null, string? converterCode = null, bool include = true,
        PermissionCode? permissionCode = null, CapabilityCode? capabilityCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFieldCode); ArgumentException.ThrowIfNullOrWhiteSpace(outputName);
        TargetFieldCode = targetFieldCode.Trim(); OutputName = outputName.Trim(); SourceVariableCode = sourceVariableCode;
        DisplayOrder = displayOrder; Format = string.IsNullOrWhiteSpace(format) ? null : format;
        ConverterCode = string.IsNullOrWhiteSpace(converterCode) ? null : converterCode.Trim().ToUpperInvariant();
        Include = include; PermissionCode = permissionCode; CapabilityCode = capabilityCode;
    }
    public string TargetFieldCode { get; }
    public string OutputName { get; }
    public VariableCode SourceVariableCode { get; }
    public int DisplayOrder { get; }
    public string? Format { get; }
    public string? ConverterCode { get; }
    public bool Include { get; }
    public PermissionCode? PermissionCode { get; }
    public CapabilityCode? CapabilityCode { get; }
}

public sealed record ExportDefinition
{
    public ExportDefinition(string exportId, string exportCode, LocalizationKey displayNameKey, string writerCode,
        string fileExtension, IEnumerable<ExportFieldDefinition> fields, bool includeHeader = true,
        Encoding? encoding = null, ExportScope scope = ExportScope.CurrentView,
        IEnumerable<DynamicUI24.Core.DataEntry.GridSortDefinition>? sort = null,
        IEnumerable<DynamicUI24.Core.DataEntry.GridFilterDefinition>? filter = null,
        PermissionCode? permissionCode = null, CapabilityCode? capabilityCode = null,
        IReadOnlyDictionary<string, object?>? writerOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exportId); ArgumentException.ThrowIfNullOrWhiteSpace(exportCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(writerCode); ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);
        ExportId = exportId.Trim(); ExportCode = exportCode.Trim().ToUpperInvariant(); DisplayNameKey = displayNameKey;
        WriterCode = writerCode.Trim().ToUpperInvariant(); FileExtension = fileExtension.Trim().TrimStart('.').ToLowerInvariant();
        Fields = fields.OrderBy(x => x.DisplayOrder).ToImmutableArray(); IncludeHeader = includeHeader; Encoding = encoding ?? Encoding.UTF8;
        Scope = scope; Sort = (sort ?? []).ToImmutableArray(); Filter = (filter ?? []).ToImmutableArray();
        PermissionCode = permissionCode; CapabilityCode = capabilityCode;
        WriterOptions = (writerOptions ?? new Dictionary<string, object?>()).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
    }
    public string ExportId { get; }
    public string ExportCode { get; }
    public LocalizationKey DisplayNameKey { get; }
    public string WriterCode { get; }
    public string FileExtension { get; }
    public Encoding Encoding { get; }
    public bool IncludeHeader { get; }
    public ImmutableArray<ExportFieldDefinition> Fields { get; }
    public ImmutableArray<DynamicUI24.Core.DataEntry.GridSortDefinition> Sort { get; }
    public ImmutableArray<DynamicUI24.Core.DataEntry.GridFilterDefinition> Filter { get; }
    public ExportScope Scope { get; }
    public PermissionCode? PermissionCode { get; }
    public CapabilityCode? CapabilityCode { get; }
    public ImmutableDictionary<string, object?> WriterOptions { get; }
}
