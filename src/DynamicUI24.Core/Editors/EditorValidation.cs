using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace DynamicUI24.Core.Editors;

public enum EditorValidationSeverity { None, Info, Warning, Error }
public sealed record EditorValidationResult(bool IsValid, EditorValidationSeverity Severity,
    string MessageCode, string? SafeLocalizedMessage, EditorSemanticId? TargetSemanticId = null)
{
    public static EditorValidationResult Valid { get; } = new(true, EditorValidationSeverity.None, "EDITOR_VALID", null);
    public static EditorValidationResult Error(string code, EditorSemanticId target, string? safeMessage = null) =>
        new(false, EditorValidationSeverity.Error, code, safeMessage, target);
}

public sealed record EditorValidationContext(EditorDefinition Definition, object? Candidate,
    IReadOnlyDictionary<EditorSemanticId, object?> SemanticValues);
public delegate EditorValidationResult EditorSynchronousRule(EditorValidationContext context);
public delegate ValueTask<EditorValidationResult> EditorAsynchronousRule(EditorValidationContext context,
    CancellationToken cancellationToken);

public sealed record EditorValidationDefinition(
    bool IsRequired = false, int? MinimumLength = null, int? MaximumLength = null,
    string? Pattern = null, decimal? Minimum = null, decimal? Maximum = null,
    IEnumerable<EditorSynchronousRule>? SynchronousRules = null,
    IEnumerable<EditorAsynchronousRule>? AsynchronousRules = null)
{
    public ImmutableArray<EditorSynchronousRule> SyncRules { get; } = (SynchronousRules ?? []).ToImmutableArray();
    public ImmutableArray<EditorAsynchronousRule> AsyncRules { get; } = (AsynchronousRules ?? []).ToImmutableArray();
}

public sealed class EditorValidator
{
    public async ValueTask<EditorValidationResult> ValidateAsync(EditorValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var target = context.Definition.ConsumerSemanticId;
        var rule = context.Definition.Validation;
        if (rule.IsRequired && (context.Candidate is null || context.Candidate is string s && string.IsNullOrWhiteSpace(s)))
            return EditorValidationResult.Error("EDITOR_REQUIRED", target);
        if (context.Candidate is string text)
        {
            if (rule.MinimumLength is { } min && text.Length < min) return EditorValidationResult.Error("EDITOR_MIN_LENGTH", target);
            if (rule.MaximumLength is { } max && text.Length > max) return EditorValidationResult.Error("EDITOR_MAX_LENGTH", target);
            if (rule.Pattern is { } pattern && !Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
                return EditorValidationResult.Error("EDITOR_PATTERN", target);
        }
        if (TryDecimal(context.Candidate, out var number))
        {
            var minimum = rule.Minimum ?? context.Definition.Minimum;
            var maximum = rule.Maximum ?? context.Definition.Maximum;
            if (minimum is { } min && number < min) return EditorValidationResult.Error("EDITOR_RANGE_MIN", target);
            if (maximum is { } max && number > max) return EditorValidationResult.Error("EDITOR_RANGE_MAX", target);
        }
        if (context.Candidate is DateRangeValue range && !range.IsOrdered)
            return EditorValidationResult.Error("EDITOR_DATE_RANGE_ORDER", target);
        foreach (var sync in rule.SyncRules)
        { var result = sync(context); if (!result.IsValid) return result; }
        foreach (var asyncRule in rule.AsyncRules)
        { var result = await asyncRule(context, cancellationToken); if (!result.IsValid) return result; }
        return EditorValidationResult.Valid;
    }

    private static bool TryDecimal(object? value, out decimal number)
    {
        try { if (value is not null) { number = Convert.ToDecimal(value); return true; } }
        catch (Exception) when (value is not decimal) { }
        number = 0; return false;
    }
}

public sealed record CrossFieldValidationRequest(EditorSemanticId Target,
    ImmutableArray<EditorSemanticId> RelatedFields, IReadOnlyDictionary<EditorSemanticId, object?> Values);
public interface ICrossFieldEditorValidator
{
    ValueTask<EditorValidationResult> ValidateAsync(CrossFieldValidationRequest request,
        CancellationToken cancellationToken = default);
}
