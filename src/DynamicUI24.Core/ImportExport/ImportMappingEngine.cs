using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.ImportExport;

public static class ImportExportAuthorization
{
    public static bool IsAllowed(PermissionCode? permission, CapabilityCode? capability, EffectiveAuthorizationContext? context)
    {
        if (permission is null && capability is null) return true;
        if (context is null || context.Status != EffectiveAuthorizationStatus.Ready) return false;
        return (permission is null || context.PermissionCodes.Contains(permission.Value)) &&
            (capability is null || context.CapabilityCodes.Contains(capability.Value));
    }
}

public static class ImportDefinitionValidator
{
    public static ImmutableArray<ImportDiagnostic> Validate(ImportDefinition definition,
        ResolvedGridDefinition grid, ImportExportRegistry registry, EffectiveAuthorizationContext? authorization = null)
    {
        ArgumentNullException.ThrowIfNull(definition); ArgumentNullException.ThrowIfNull(grid); ArgumentNullException.ThrowIfNull(registry);
        var result = ImmutableArray.CreateBuilder<ImportDiagnostic>();
        if (!registry.TryGetParser(definition.ParserCode, out _))
            result.Add(ImportDiagnostic.Error("IMPORT_PARSER_UNKNOWN", "The selected import parser is not registered."));
        if (!ImportExportAuthorization.IsAllowed(definition.PermissionCode, definition.CapabilityCode, authorization))
            result.Add(ImportDiagnostic.Error("IMPORT_NOT_AUTHORIZED", "Import is unavailable in the current context."));
        var targets = grid.Columns.ToDictionary(x => x.Definition.VariableCode);
        foreach (var duplicate in definition.Mappings.GroupBy(x => x.TargetVariableCode).Where(x => x.Count() > 1))
            result.Add(ImportDiagnostic.Error("IMPORT_DUPLICATE_TARGET", "More than one source field maps to the same VariableCode.", target: duplicate.Key));
        foreach (var mapping in definition.Mappings)
        {
            if (!targets.TryGetValue(mapping.TargetVariableCode, out var target))
                result.Add(ImportDiagnostic.Error("IMPORT_TARGET_UNKNOWN", "The mapped VariableCode is not in the current workspace.", target: mapping.TargetVariableCode));
            else if (!grid.CanEdit || !target.CanEdit)
                result.Add(ImportDiagnostic.Error("IMPORT_TARGET_NOT_EDITABLE", "The mapped target is not an editable INPUT field.", target: mapping.TargetVariableCode));
            if (!ImportExportAuthorization.IsAllowed(mapping.PermissionCode, mapping.CapabilityCode, authorization))
                result.Add(ImportDiagnostic.Error("IMPORT_MAPPING_NOT_AUTHORIZED", "A mapped target is unavailable in the current context.", target: mapping.TargetVariableCode));
            if (mapping.ConverterCode is { } converter && !registry.TryGetConverter(converter, out _))
                result.Add(ImportDiagnostic.Error("IMPORT_CONVERTER_UNKNOWN", "The selected converter is not registered.", target: mapping.TargetVariableCode));
        }
        return result.ToImmutable();
    }
}

public static class ExportDefinitionValidator
{
    public static ImmutableArray<ImportDiagnostic> Validate(ExportDefinition definition,
        ResolvedGridDefinition grid, ImportExportRegistry registry, EffectiveAuthorizationContext? authorization = null)
    {
        ArgumentNullException.ThrowIfNull(definition); ArgumentNullException.ThrowIfNull(grid); ArgumentNullException.ThrowIfNull(registry);
        var result = ImmutableArray.CreateBuilder<ImportDiagnostic>();
        if (!registry.TryGetWriter(definition.WriterCode, out _))
            result.Add(ImportDiagnostic.Error("EXPORT_WRITER_UNKNOWN", "The selected export writer is not registered."));
        if (!ImportExportAuthorization.IsAllowed(definition.PermissionCode, definition.CapabilityCode, authorization))
            result.Add(ImportDiagnostic.Error("EXPORT_NOT_AUTHORIZED", "Export is unavailable in the current context."));
        var visible = grid.Columns.Where(x => x.IsVisible).Select(x => x.Definition.VariableCode).ToHashSet();
        foreach (var duplicate in definition.Fields.Where(x => x.Include).GroupBy(x => x.TargetFieldCode, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
            result.Add(ImportDiagnostic.Error("EXPORT_DUPLICATE_FIELD", "An output field code is duplicated."));
        foreach (var field in definition.Fields.Where(x => x.Include))
        {
            if (!visible.Contains(field.SourceVariableCode))
                result.Add(ImportDiagnostic.Error("EXPORT_SOURCE_UNAVAILABLE", "The export field is hidden or unavailable.", target: field.SourceVariableCode));
            if (!ImportExportAuthorization.IsAllowed(field.PermissionCode, field.CapabilityCode, authorization))
                result.Add(ImportDiagnostic.Error("EXPORT_FIELD_NOT_AUTHORIZED", "The export field is not authorized.", target: field.SourceVariableCode));
        }
        return result.ToImmutable();
    }
}

public static class ImportAutoMapper
{
    public static (ImmutableArray<ImportFieldMapping> Mappings, ImmutableArray<ImportDiagnostic> Diagnostics) Map(
        ImportSourceSchema schema, IEnumerable<ResolvedGridColumn> columns,
        IReadOnlyDictionary<VariableCode, IEnumerable<string>>? aliases = null)
    {
        ArgumentNullException.ThrowIfNull(schema); ArgumentNullException.ThrowIfNull(columns);
        var mappings = ImmutableArray.CreateBuilder<ImportFieldMapping>();
        var diagnostics = ImmutableArray.CreateBuilder<ImportDiagnostic>();
        var available = columns.Where(x => x.CanEdit && x.IsVisible).ToArray();
        foreach (var field in schema.Fields)
        {
            var normalized = Normalize(field.SourceFieldCode);
            var matches = available.Where(x =>
                x.Definition.VariableCode.Value.Equals(field.SourceFieldCode, StringComparison.OrdinalIgnoreCase) ||
                Normalize(x.Definition.ColumnCode) == normalized || Normalize(x.Definition.VariableCode.Value) == normalized ||
                aliases?.TryGetValue(x.Definition.VariableCode, out var names) == true && names.Any(y => Normalize(y) == normalized))
                .Select(x => x.Definition).DistinctBy(x => x.VariableCode).ToArray();
            if (matches.Length == 1)
                mappings.Add(new($"AUTO_{field.Ordinal ?? mappings.Count}", field.SourceFieldCode, matches[0].VariableCode,
                    field.Ordinal ?? mappings.Count, field.Ordinal, matches[0].IsRequired));
            else if (matches.Length > 1)
                diagnostics.Add(ImportDiagnostic.Warning("IMPORT_AUTOMAP_AMBIGUOUS", "The source field matches more than one target and was left unmapped.", field: field.SourceFieldCode));
        }
        return (mappings.ToImmutable(), diagnostics.ToImmutable());
    }
    private static string Normalize(string value) => string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();
}

public sealed class ImportMappingEngine
{
    private readonly ImportExportRegistry registry;
    public ImportMappingEngine(ImportExportRegistry registry) => this.registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public async ValueTask<ImportCandidateRecord> MapAsync(ImportSourceRecord source, ImportDefinition definition,
        ResolvedGridDefinition grid, IFormatProvider? formatProvider = null, CancellationToken cancellationToken = default)
    {
        var values = ImmutableDictionary.CreateBuilder<VariableCode, object?>();
        var diagnostics = ImmutableArray.CreateBuilder<ImportDiagnostic>();
        var columns = grid.Columns.ToDictionary(x => x.Definition.VariableCode);
        foreach (var mapping in definition.Mappings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!columns.TryGetValue(mapping.TargetVariableCode, out var resolved) || !resolved.CanEdit || !grid.CanEdit)
            {
                diagnostics.Add(ImportDiagnostic.Error("IMPORT_TARGET_NOT_EDITABLE", "The mapped target is not editable.", source.RecordIndex, mapping.SourceField, mapping.TargetVariableCode));
                continue;
            }
            object? raw = Resolve(source, mapping);
            if (raw is string text)
            {
                if (mapping.Trim) raw = text.Trim();
                if (mapping.NullTokens.Contains(raw?.ToString() ?? string.Empty)) raw = null;
            }
            if (raw is null || raw is string { Length: 0 })
            {
                raw = definition.NullPolicy switch
                {
                    ImportNullPolicy.EmptyAsEmptyString => string.Empty,
                    ImportNullPolicy.DefaultValue => mapping.DefaultValue,
                    _ => raw is string { Length: 0 } ? null : raw,
                };
                if (raw is null && (mapping.Required || resolved.Definition.IsRequired || definition.NullPolicy == ImportNullPolicy.RejectRequired))
                {
                    diagnostics.Add(ImportDiagnostic.Error("IMPORT_REQUIRED_VALUE_MISSING", "A required value is missing.", source.RecordIndex, mapping.SourceField, mapping.TargetVariableCode));
                    continue;
                }
            }
            try
            {
                if (mapping.ConverterCode is { } code)
                {
                    if (!registry.TryGetConverter(code, out var converter))
                        throw new InvalidOperationException("Converter is not registered.");
                    raw = await converter.ConvertAsync(raw, new(mapping, resolved.Definition, formatProvider ?? CultureInfo.CurrentCulture), cancellationToken);
                }
                var target = mapping.DataTypeOverride ?? resolved.Definition.DataType;
                var converted = ConvertValue(raw, target, formatProvider ?? CultureInfo.CurrentCulture);
                var validation = GridValueValidator.Validate(resolved.Definition, converted);
                if (validation is not null)
                    diagnostics.Add(ImportDiagnostic.Error(validation.Code, "The value does not satisfy target metadata validation.", source.RecordIndex, mapping.SourceField, mapping.TargetVariableCode, raw));
                else values[mapping.TargetVariableCode] = converted;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                diagnostics.Add(ImportDiagnostic.Error("IMPORT_CONVERSION_FAILED", "The source value could not be converted safely.", source.RecordIndex,
                    mapping.SourceField, mapping.TargetVariableCode, raw, exception));
            }
        }
        return new(source.RecordIndex, values.ToImmutable(), diagnostics.ToImmutable());
    }

    private static object? Resolve(ImportSourceRecord record, ImportFieldMapping mapping)
    {
        if (record.Values.TryGetValue(mapping.SourceField, out var value)) return value;
        if (mapping.SourceIndex is { } index && index < record.Values.Count) return record.Values.ElementAt(index).Value;
        return mapping.DefaultValue;
    }

    public static object? ConvertValue(object? value, ColumnDataType target, IFormatProvider provider)
    {
        if (value is null) return null;
        if (target is ColumnDataType.Text or ColumnDataType.MultilineText or ColumnDataType.Choice or ColumnDataType.Reference) return value.ToString();
        if (target == ColumnDataType.Integer) return value is long ? value : Convert.ToInt64(value, provider);
        if (target == ColumnDataType.Decimal) return value is decimal ? value : Convert.ToDecimal(value, provider);
        if (target == ColumnDataType.Boolean)
        {
            if (value is bool) return value;
            var text = value.ToString();
            if (text == "1" || text?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true) return true;
            if (text == "0" || text?.Equals("no", StringComparison.OrdinalIgnoreCase) == true) return false;
            return Convert.ToBoolean(value, provider);
        }
        if (target == ColumnDataType.Date)
        {
            if (value is DateOnly) return value;
            if (value is DateTime dateTime) return DateOnly.FromDateTime(dateTime);
            return DateOnly.Parse(value.ToString()!, provider);
        }
        if (target == ColumnDataType.DateTime) return value is DateTime ? value : DateTime.Parse(value.ToString()!, provider);
        throw new InvalidOperationException("FORMULA and SYSTEM fields are not importable.");
    }
}

public static class BuiltInImportConverters
{
    public static void Register(ImportExportRegistry registry)
    {
        registry.Register(new DelegateConverter("TEXT_TO_INTEGER", (x, c) => ImportMappingEngine.ConvertValue(x, ColumnDataType.Integer, c.FormatProvider)));
        registry.Register(new DelegateConverter("TEXT_TO_DECIMAL", (x, c) => ImportMappingEngine.ConvertValue(x, ColumnDataType.Decimal, c.FormatProvider)));
        registry.Register(new DelegateConverter("TEXT_TO_DATE", (x, c) => ImportMappingEngine.ConvertValue(x, ColumnDataType.Date, c.FormatProvider)));
        registry.Register(new DelegateConverter("TEXT_TO_BOOLEAN", (x, c) => ImportMappingEngine.ConvertValue(x, ColumnDataType.Boolean, c.FormatProvider)));
        registry.Register(new DelegateConverter("TRIM", (x, _) => x?.ToString()?.Trim()));
        registry.Register(new DelegateConverter("UPPERCASE", (x, _) => x?.ToString()?.ToUpperInvariant()));
        registry.Register(new DelegateConverter("LOWERCASE", (x, _) => x?.ToString()?.ToLowerInvariant()));
    }
    private sealed class DelegateConverter(string code, Func<object?, ImportConversionContext, object?> convert) : IImportValueConverter
    {
        public string ConverterCode => code;
        public ValueTask<object?> ConvertAsync(object? value, ImportConversionContext context, CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); return ValueTask.FromResult(convert(value, context)); }
    }
}
