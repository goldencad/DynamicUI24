using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Editors;

public readonly record struct EditorCode
{
    public EditorCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct EditorSemanticId
{
    public EditorSemanticId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }
    public string Value { get; }
    public override string ToString() => Value;
}

public enum EditorValueType
{
    String, LongString, Integer, Decimal, Currency, Percentage, Boolean, Date, Time, DateTime,
    DateRange, Choice, MultiChoice, LookupKey, Secret, Hyperlink
}

public enum EditorKind
{
    Text, MultilineText, Integer, Decimal, Currency, Percentage, Boolean, Date, Time, DateTime,
    DateRange, Choice, MultiChoice, Lookup, SearchLookup, TreeLookup, ButtonEdit, Hyperlink, Password
}

[Flags]
public enum EditorCapability
{
    None = 0, Clear = 1, Search = 2, Reveal = 4, Clipboard = 8, Increment = 16,
    MultiSelect = 32, Tree = 64, EmbeddedActions = 128, ExternalNavigation = 256
}

public enum PercentageStorageScale { Fraction, WholeNumber }
public enum EditorCommitPolicy { Explicit, OnEnter, OnFocusLoss }
/// <summary>Semantic form width; presentation maps this identity to theme-owned measurements.</summary>
public enum EditorWidthClass { Auto, Short, Compact, Medium, Long, Fill }
public enum EditorMaskKind { Simple, Numeric, DateTime, TimeSpan, Regex }
public enum EditorActionKind { Browse, Select, Open, Clear, Refresh, Help, Reveal, Custom }

public sealed record EditorMaskDefinition(EditorMaskKind Kind, string Pattern,
    bool ValidateOnCommit = true)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Pattern);
}

public sealed record EditorFormattingDefinition(string? Format = null, string? CurrencyCode = null,
    PercentageStorageScale PercentageScale = PercentageStorageScale.Fraction, int? Precision = null,
    int? Scale = null);

public sealed record EditorChromeDefinition(
    LocalizationKey? LabelKey = null,
    LocalizationKey? PlaceholderKey = null,
    LocalizationKey? HelperTextKey = null,
    IconKey? LeadingIcon = null,
    IconKey? TrailingIcon = null,
    string? Tooltip = null,
    bool FloatingLabel = false,
    bool ShowRequiredIndicator = true);

public sealed record EditorActionDefinition(string ActionCode, EditorActionKind Kind,
    LocalizationKey LabelKey, IconKey? Icon = null, PresentationRequirement? Requirement = null);

public sealed record EditorChoiceOption(string SemanticOptionId, LocalizationKey DisplayLabelKey,
    string? SafeDisplayText = null, bool IsEnabled = true)
{
    public override string ToString() => SafeDisplayText ?? DisplayLabelKey.Value;
}

public sealed record EditorDefinition
{
    public EditorDefinition(EditorCode editorCode, EditorSemanticId consumerSemanticId,
        EditorValueType valueType, EditorKind? explicitKind = null,
        EditorCapability capabilities = EditorCapability.None, EditorChromeDefinition? chrome = null,
        EditorFormattingDefinition? formatting = null, EditorValidationDefinition? validation = null,
        IEnumerable<EditorChoiceOption>? choices = null, IEnumerable<EditorActionDefinition>? actions = null,
        PresentationRequirement? presentationRequirement = null,
        SensitiveContentDefinition? sensitiveContent = null, HelpContextCode? helpContextCode = null,
        EditorMaskDefinition? mask = null, bool isReadOnly = false, bool isDisabled = false,
        bool allowsNull = true, EditorCommitPolicy commitPolicy = EditorCommitPolicy.Explicit,
        decimal? minimum = null, decimal? maximum = null, decimal? increment = null,
        int? maxLength = null, bool wrapText = true, EditorWidthClass width = EditorWidthClass.Auto)
    {
        EditorCode = editorCode;
        ConsumerSemanticId = consumerSemanticId;
        ValueType = valueType;
        ExplicitKind = explicitKind;
        Capabilities = capabilities;
        Chrome = chrome ?? new();
        Formatting = formatting ?? new();
        Validation = validation ?? new();
        Choices = (choices ?? []).ToImmutableArray();
        Actions = (actions ?? []).ToImmutableArray();
        PresentationRequirement = presentationRequirement;
        SensitiveContent = sensitiveContent ?? SensitiveContentDefinition.Normal;
        HelpContextCode = helpContextCode;
        Mask = mask;
        IsReadOnly = isReadOnly;
        IsDisabled = isDisabled;
        AllowsNull = allowsNull;
        CommitPolicy = commitPolicy;
        Minimum = minimum;
        Maximum = maximum;
        Increment = increment;
        MaxLength = maxLength;
        WrapText = wrapText;
        Width = width;
    }

    public EditorCode EditorCode { get; }
    public EditorSemanticId ConsumerSemanticId { get; }
    public EditorValueType ValueType { get; }
    public EditorKind? ExplicitKind { get; }
    public EditorCapability Capabilities { get; }
    public EditorChromeDefinition Chrome { get; }
    public EditorFormattingDefinition Formatting { get; }
    public EditorValidationDefinition Validation { get; }
    public ImmutableArray<EditorChoiceOption> Choices { get; }
    public ImmutableArray<EditorActionDefinition> Actions { get; }
    public PresentationRequirement? PresentationRequirement { get; }
    public SensitiveContentDefinition SensitiveContent { get; }
    public HelpContextCode? HelpContextCode { get; }
    public EditorMaskDefinition? Mask { get; }
    public bool IsReadOnly { get; }
    public bool IsDisabled { get; }
    public bool AllowsNull { get; }
    public EditorCommitPolicy CommitPolicy { get; }
    public decimal? Minimum { get; }
    public decimal? Maximum { get; }
    public decimal? Increment { get; }
    public int? MaxLength { get; }
    public bool WrapText { get; }
    public EditorWidthClass Width { get; }
}

public readonly record struct DateRangeValue(DateOnly? Start, DateOnly? End)
{
    public bool IsOrdered => Start is null || End is null || Start <= End;
}
