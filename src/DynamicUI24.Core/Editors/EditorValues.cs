using System.Globalization;

namespace DynamicUI24.Core.Editors;

public sealed record EditorParseResult(bool IsSuccess, object? Candidate, string? DiagnosticCode = null)
{
    public static EditorParseResult Success(object? value) => new(true, value);
    public static EditorParseResult Failure(string code = "EDITOR_PARSE_INVALID") => new(false, null, code);
}

public static class EditorValueFormatter
{
    public static string Format(object? value, EditorDefinition definition, CultureInfo culture)
    {
        if (value is null) return string.Empty;
        if (definition.ValueType == EditorValueType.Percentage && value is decimal percentage)
        {
            var fraction = definition.Formatting.PercentageScale == PercentageStorageScale.Fraction
                ? percentage : percentage / 100m;
            return fraction.ToString(definition.Formatting.Format ?? "P", culture);
        }
        if (value is DateOnly date) return date.ToString(definition.Formatting.Format ?? "d", culture);
        if (value is TimeOnly time) return time.ToString(definition.Formatting.Format ?? "t", culture);
        if (value is DateTime dateTime) return dateTime.ToString(definition.Formatting.Format ?? "g", culture);
        if (value is IFormattable formattable)
        {
            var format = definition.Formatting.Format;
            if (definition.ValueType == EditorValueType.Currency) format ??= "C";
            return formattable.ToString(format, culture) ?? string.Empty;
        }
        return value.ToString() ?? string.Empty;
    }
}

public static class EditorValueParser
{
    public static EditorParseResult Parse(string? text, EditorDefinition definition, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(text))
            return definition.AllowsNull ? EditorParseResult.Success(null) : EditorParseResult.Failure("EDITOR_VALUE_REQUIRED");
        return definition.ValueType switch
        {
            EditorValueType.Integer => long.TryParse(text, NumberStyles.Integer, culture, out var value)
                ? EditorParseResult.Success(value) : EditorParseResult.Failure(),
            EditorValueType.Decimal or EditorValueType.Currency =>
                decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, culture, out var value)
                    ? EditorParseResult.Success(value) : EditorParseResult.Failure(),
            EditorValueType.Percentage => ParsePercentage(text, definition, culture),
            EditorValueType.Boolean => bool.TryParse(text, out var value)
                ? EditorParseResult.Success(value) : EditorParseResult.Failure(),
            EditorValueType.Date => DateOnly.TryParse(text, culture, DateTimeStyles.None, out var value)
                ? EditorParseResult.Success(value) : EditorParseResult.Failure(),
            EditorValueType.Time => TimeOnly.TryParse(text, culture, DateTimeStyles.None, out var value)
                ? EditorParseResult.Success(value) : EditorParseResult.Failure(),
            EditorValueType.DateTime => System.DateTime.TryParse(text, culture, DateTimeStyles.None, out var value)
                ? EditorParseResult.Success(value) : EditorParseResult.Failure(),
            _ => EditorParseResult.Success(text),
        };
    }

    private static EditorParseResult ParsePercentage(string text, EditorDefinition definition, CultureInfo culture)
    {
        var symbol = culture.NumberFormat.PercentSymbol;
        var hasSymbol = text.Contains(symbol, StringComparison.CurrentCulture);
        var normalized = text.Replace(symbol, string.Empty, StringComparison.CurrentCulture).Trim();
        if (!decimal.TryParse(normalized, NumberStyles.Number, culture, out var number)) return EditorParseResult.Failure();
        if (definition.Formatting.PercentageScale == PercentageStorageScale.Fraction && hasSymbol) number /= 100m;
        return EditorParseResult.Success(number);
    }
}

public sealed class EditorRuntimeState
{
    public EditorRuntimeState(EditorDefinition definition, object? committedValue = null)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CommittedValue = committedValue;
        CandidateValue = committedValue;
    }
    public EditorDefinition Definition { get; }
    public object? CommittedValue { get; private set; }
    public object? CandidateValue { get; private set; }
    public string? InputText { get; private set; }
    public bool IsDirty => !Equals(CommittedValue, CandidateValue);
    public bool IsNativeCompositionActive { get; private set; }
    public EditorValidationResult Validation { get; private set; } = EditorValidationResult.Valid;
    public long Revision { get; private set; }

    public void BeginComposition() => IsNativeCompositionActive = true;
    public void EndComposition(string completedText) { IsNativeCompositionActive = false; InputText = completedText; Revision++; }
    public void SetCandidate(object? candidate, string? inputText = null)
    { CandidateValue = candidate; InputText = inputText; Revision++; }
    public bool Commit(EditorValidationResult validation)
    {
        Validation = validation;
        if (!validation.IsValid || IsNativeCompositionActive) return false;
        CommittedValue = CandidateValue; Revision++; return true;
    }
    public void Cancel() { CandidateValue = CommittedValue; InputText = null; Validation = EditorValidationResult.Valid; Revision++; }
    public void SetValidation(EditorValidationResult validation) { Validation = validation; Revision++; }
}
