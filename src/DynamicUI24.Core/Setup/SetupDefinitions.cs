using System.Collections.Immutable;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Core.Setup;

public enum SetupDefinitionStatus { Draft, Valid, Invalid, Published, Retired }
public enum SetupValidationState { NotValidated, Valid, Invalid }
public enum SetupDiagnosticSeverity { Info, Warning, Error }

public static class SetupDefinitionMetadataValidator
{
    public static ImmutableArray<SetupMetadataDiagnostic> Validate(IEnumerable<SetupDefinitionDescriptor> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        return definitions.GroupBy(x => x.DefinitionId, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => new SetupMetadataDiagnostic("SETUP_DUPLICATE_DEFINITION_ID",
                $"Duplicate definition id '{x.Key}'.", x.Key)).ToImmutableArray();
    }
}

public sealed record SetupDefinitionDescriptor
{
    public SetupDefinitionDescriptor(string definitionId, string definitionCode, string displayName,
        string definitionType, int version = 1, SetupDefinitionStatus status = SetupDefinitionStatus.Draft,
        DateOnly? effectiveFrom = null, DateOnly? effectiveTo = null, bool isSystem = false,
        bool isEditable = true, bool cloneAllowed = true, PresentationRequirement? permissionRequirement = null,
        SetupValidationState validationState = SetupValidationState.NotValidated,
        IReadOnlyDictionary<string, object?>? values = null, string? categoryId = null, string? scopeKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionType);
        if (!TechnicalCode.IsValid(definitionCode))
            throw new ArgumentException("Definition code contains unsupported characters.", nameof(definitionCode));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        DefinitionId = definitionId.Trim();
        DefinitionCode = definitionCode.Trim().ToUpperInvariant();
        DisplayName = displayName.Trim();
        DefinitionType = definitionType.Trim().ToUpperInvariant();
        Version = version;
        Status = status;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsSystem = isSystem;
        IsEditable = isEditable;
        CloneAllowed = cloneAllowed;
        PermissionRequirement = permissionRequirement;
        ValidationState = validationState;
        Values = (values ?? new Dictionary<string, object?>()).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        CategoryId = categoryId?.Trim();
        ScopeKey = scopeKey?.Trim();
    }

    public string DefinitionId { get; init; }
    public string DefinitionCode { get; init; }
    public string DisplayName { get; init; }
    public string DefinitionType { get; init; }
    public int Version { get; init; }
    public SetupDefinitionStatus Status { get; init; }
    public DateOnly? EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
    public bool IsSystem { get; init; }
    public bool IsEditable { get; init; }
    public bool CloneAllowed { get; init; }
    public bool IsPublished => Status == SetupDefinitionStatus.Published;
    public PresentationRequirement? PermissionRequirement { get; init; }
    public SetupValidationState ValidationState { get; init; }
    public ImmutableDictionary<string, object?> Values { get; init; }
    public string? CategoryId { get; init; }
    public string? ScopeKey { get; init; }
}

public sealed record SetupValidationDiagnostic(SetupDiagnosticSeverity Severity, string Code,
    LocalizationKey MessageKey, string? Message = null, string? FieldCode = null);
public sealed record SetupValidationResult(ImmutableArray<SetupValidationDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.All(x => x.Severity != SetupDiagnosticSeverity.Error);
    public static SetupValidationResult Success { get; } = new([]);
}

public interface ISetupDefinitionValidator
{
    SetupValidationResult Validate(SetupDefinitionDescriptor candidate);
}

public interface ISetupDefinitionProvider
{
    IReadOnlyList<SetupDefinitionDescriptor> GetDefinitions(string categoryId, string? scopeKey = null);
    SetupDefinitionDescriptor SaveDraft(SetupDefinitionDescriptor candidate);
    SetupDefinitionDescriptor Publish(SetupDefinitionDescriptor candidate);
    SetupDefinitionDescriptor Retire(SetupDefinitionDescriptor definition);
}

public sealed class SetupEditBuffer
{
    private SetupDefinitionDescriptor source;
    public SetupEditBuffer(SetupDefinitionDescriptor source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        Candidate = source;
    }
    public SetupDefinitionDescriptor Source => source;
    public SetupDefinitionDescriptor Candidate { get; private set; }
    public bool IsDirty => Candidate != source;
    public void Update(SetupDefinitionDescriptor candidate) => Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
    public void SetValue(string fieldCode, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldCode);
        Candidate = Candidate with { Values = Candidate.Values.SetItem(fieldCode, value), ValidationState = SetupValidationState.NotValidated };
    }
    public void Revert() => Candidate = source;
    public void Accept(SetupDefinitionDescriptor saved)
    {
        source = saved ?? throw new ArgumentNullException(nameof(saved));
        Revert();
    }
}

public enum SetupNavigationDecision { Allowed, BlockedByDirtyCandidate }

public sealed class SetupDefinitionLifecycle
{
    private readonly ISetupDefinitionProvider provider;
    private readonly ISetupDefinitionValidator validator;
    public SetupDefinitionLifecycle(ISetupDefinitionProvider provider, ISetupDefinitionValidator validator)
    {
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }
    public SetupEditBuffer? Buffer { get; private set; }
    public SetupValidationResult? LastValidation { get; private set; }
    public SetupNavigationDecision Select(SetupDefinitionDescriptor definition)
    {
        if (Buffer?.Source.DefinitionId.Equals(definition.DefinitionId, StringComparison.OrdinalIgnoreCase) == true)
            return SetupNavigationDecision.Allowed;
        if (Buffer?.IsDirty == true) return SetupNavigationDecision.BlockedByDirtyCandidate;
        Buffer = new(definition);
        LastValidation = null;
        return SetupNavigationDecision.Allowed;
    }
    public SetupDefinitionDescriptor CreateDraft(string categoryId, string definitionType, string definitionCode, string displayName)
    {
        var draft = new SetupDefinitionDescriptor(Guid.NewGuid().ToString("N"), definitionCode, displayName,
            definitionType, categoryId: categoryId);
        Buffer = new(draft);
        LastValidation = null;
        return draft;
    }
    public SetupDefinitionDescriptor Clone(string newDefinitionId, string newDefinitionCode)
    {
        var source = Buffer?.Source ?? throw new InvalidOperationException("No definition is selected.");
        if (!source.CloneAllowed) throw new InvalidOperationException("The definition cannot be cloned.");
        var clone = source with { DefinitionId = newDefinitionId, DefinitionCode = newDefinitionCode.ToUpperInvariant(),
            Version = source.Version + 1, Status = SetupDefinitionStatus.Draft, ValidationState = SetupValidationState.NotValidated,
            IsSystem = false, IsEditable = true };
        Buffer = new(clone);
        LastValidation = null;
        return clone;
    }
    public SetupValidationResult Validate()
    {
        var buffer = Buffer ?? throw new InvalidOperationException("No definition is selected.");
        LastValidation = validator.Validate(buffer.Candidate);
        buffer.Update(buffer.Candidate with { Status = LastValidation.IsValid ? SetupDefinitionStatus.Valid : SetupDefinitionStatus.Invalid,
            ValidationState = LastValidation.IsValid ? SetupValidationState.Valid : SetupValidationState.Invalid });
        return LastValidation;
    }
    public SetupDefinitionDescriptor SaveDraft()
    {
        var buffer = Buffer ?? throw new InvalidOperationException("No definition is selected.");
        EnsureEditable(buffer.Source);
        var saved = provider.SaveDraft(buffer.Candidate);
        buffer.Accept(saved);
        return saved;
    }
    public SetupDefinitionDescriptor Publish()
    {
        var buffer = Buffer ?? throw new InvalidOperationException("No definition is selected.");
        EnsureEditable(buffer.Source);
        var validation = Validate();
        if (!validation.IsValid) throw new InvalidOperationException("An invalid definition cannot be published.");
        var published = provider.Publish(buffer.Candidate);
        buffer.Accept(published);
        return published;
    }
    public SetupDefinitionDescriptor Retire()
    {
        var buffer = Buffer ?? throw new InvalidOperationException("No definition is selected.");
        if (buffer.IsDirty) throw new InvalidOperationException("Resolve pending changes before retiring.");
        var retired = provider.Retire(buffer.Source);
        buffer.Accept(retired);
        return retired;
    }
    public void CancelChanges() { Buffer?.Revert(); LastValidation = null; }
    private static void EnsureEditable(SetupDefinitionDescriptor definition)
    {
        if (!definition.IsEditable || definition.IsSystem || definition.Status is SetupDefinitionStatus.Published or SetupDefinitionStatus.Retired)
            throw new InvalidOperationException("The definition is read-only.");
    }
}
