using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Editors;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class UniversalEditorFoundationTests
{
    private static EditorDefinition Definition(EditorValueType type, EditorKind? kind = null,
        EditorValidationDefinition? validation = null, decimal? min = null, decimal? max = null) =>
        new(new($"TEST.{type}"), new("FORM.FIELD"), type, kind, validation: validation,
            minimum: min, maximum: max);

    [Theory]
    [InlineData(EditorValueType.String, EditorKind.Text)]
    [InlineData(EditorValueType.LongString, EditorKind.MultilineText)]
    [InlineData(EditorValueType.Integer, EditorKind.Integer)]
    [InlineData(EditorValueType.Decimal, EditorKind.Decimal)]
    [InlineData(EditorValueType.Currency, EditorKind.Currency)]
    [InlineData(EditorValueType.Percentage, EditorKind.Percentage)]
    [InlineData(EditorValueType.Boolean, EditorKind.Boolean)]
    [InlineData(EditorValueType.Date, EditorKind.Date)]
    [InlineData(EditorValueType.Time, EditorKind.Time)]
    [InlineData(EditorValueType.DateTime, EditorKind.DateTime)]
    [InlineData(EditorValueType.Choice, EditorKind.Choice)]
    [InlineData(EditorValueType.LookupKey, EditorKind.Lookup)]
    [InlineData(EditorValueType.Secret, EditorKind.Password)]
    public void ResolverHasDeterministicDefaults(EditorValueType type, EditorKind expected)
    {
        var result = new EditorResolver().Resolve(Definition(type), EditorPlatformCapabilities.AllNative);
        Assert.Equal(EditorResolutionStatus.Resolved, result.Status);
        Assert.Equal(expected, result.Kind);
    }

    [Fact]
    public void ResolverAcceptsCompatibleOverrideAndRejectsIncompatibleOne()
    {
        var resolver = new EditorResolver();
        Assert.Equal(EditorKind.ButtonEdit, resolver.Resolve(Definition(EditorValueType.String, EditorKind.ButtonEdit),
            EditorPlatformCapabilities.AllNative).Kind);
        Assert.Equal(EditorResolutionStatus.Incompatible,
            resolver.Resolve(Definition(EditorValueType.Date, EditorKind.Currency), EditorPlatformCapabilities.AllNative).Status);
    }

    [Fact]
    public void ResolverFailsClosedForPermissionAndUnsupportedPlatform()
    {
        var guarded = new EditorDefinition(new("GUARDED"), new("FIELD"), EditorValueType.String,
            presentationRequirement: new(new PermissionCode("EDIT"), UnauthorizedBehavior: UnauthorizedBehavior.ReadOnly));
        var result = new EditorResolver().Resolve(guarded, EditorPlatformCapabilities.AllNative);
        Assert.Equal(EditorInteractionState.ReadOnly, result.InteractionState);
        var unsupported = new EditorPlatformCapabilities(new Dictionary<EditorKind, EditorPlatformCapabilityStatus>
            { [EditorKind.Text] = EditorPlatformCapabilityStatus.Unsupported });
        Assert.Equal(EditorResolutionStatus.Unsupported, new EditorResolver().Resolve(Definition(EditorValueType.String), unsupported).Status);
    }

    [Theory]
    [InlineData("en-US", "1,234.50")]
    [InlineData("vi-VN", "1.234,50")]
    public void DecimalRoundTripsAcrossCultures(string name, string text)
    {
        var culture = CultureInfo.GetCultureInfo(name);
        var definition = Definition(EditorValueType.Decimal);
        var parsed = EditorValueParser.Parse(text, definition, culture);
        Assert.True(parsed.IsSuccess); Assert.Equal(1234.50m, parsed.Candidate);
        Assert.Equal(1234.50m, parsed.Candidate); // culture affects text, never the semantic decimal
    }

    [Fact]
    public void InvalidParseIsAResultAndPercentageScaleIsExplicit()
    {
        Assert.False(EditorValueParser.Parse("not-number", Definition(EditorValueType.Decimal), CultureInfo.InvariantCulture).IsSuccess);
        var percentage = new EditorDefinition(new("P"), new("P"), EditorValueType.Percentage,
            formatting: new("0.00%", PercentageScale: PercentageStorageScale.Fraction));
        Assert.Equal(.15m, EditorValueParser.Parse("15%", percentage, CultureInfo.GetCultureInfo("en-US")).Candidate);
        Assert.Equal("15.00%", EditorValueFormatter.Format(.15m, percentage, CultureInfo.GetCultureInfo("en-US")));
        var whole = new EditorDefinition(new("PW"), new("PW"), EditorValueType.Percentage,
            formatting: new(PercentageScale: PercentageStorageScale.WholeNumber));
        Assert.Equal(15m, EditorValueParser.Parse("15", whole, CultureInfo.GetCultureInfo("en-US")).Candidate);
    }

    [Fact]
    public void PercentageIncrementIsDefinitionMetadataIndependentOfStorageScale()
    {
        var percentage = new EditorDefinition(new("P"), new("P"), EditorValueType.Percentage,
            formatting: new("P1", PercentageScale: PercentageStorageScale.Fraction), increment: .01m);

        Assert.Equal(.01m, percentage.Increment);
        Assert.Equal(.15m, EditorValueParser.Parse("15%", percentage,
            CultureInfo.GetCultureInfo("en-US")).Candidate);
    }

    [Fact]
    public async Task ValidationCoversRequiredLengthPatternRangeSyncAndAsync()
    {
        EditorSynchronousRule sync = context => context.Candidate?.ToString() == "BLOCK"
            ? EditorValidationResult.Error("SYNC", context.Definition.ConsumerSemanticId) : EditorValidationResult.Valid;
        EditorAsynchronousRule asyncRule = (context, _) => ValueTask.FromResult(context.Candidate?.ToString() == "WAIT"
            ? EditorValidationResult.Error("ASYNC", context.Definition.ConsumerSemanticId) : EditorValidationResult.Valid);
        var definition = Definition(EditorValueType.String, validation: new(true, 2, 8, "^[A-Z]+$",
            SynchronousRules: [sync], AsynchronousRules: [asyncRule]));
        var validator = new EditorValidator();
        Assert.Equal("EDITOR_REQUIRED", (await validator.ValidateAsync(new(definition, null, new Dictionary<EditorSemanticId, object?>()))).MessageCode);
        Assert.Equal("EDITOR_MIN_LENGTH", (await validator.ValidateAsync(new(definition, "A", new Dictionary<EditorSemanticId, object?>()))).MessageCode);
        Assert.Equal("EDITOR_PATTERN", (await validator.ValidateAsync(new(definition, "ab", new Dictionary<EditorSemanticId, object?>()))).MessageCode);
        Assert.Equal("SYNC", (await validator.ValidateAsync(new(definition, "BLOCK", new Dictionary<EditorSemanticId, object?>()))).MessageCode);
        Assert.Equal("ASYNC", (await validator.ValidateAsync(new(definition, "WAIT", new Dictionary<EditorSemanticId, object?>()))).MessageCode);
        var numeric = Definition(EditorValueType.Decimal, min: 1, max: 10);
        Assert.Equal("EDITOR_RANGE_MAX", (await validator.ValidateAsync(new(numeric, 11m, new Dictionary<EditorSemanticId, object?>()))).MessageCode);
    }

    [Fact]
    public void CompositionCandidateAndCommitBoundariesAreDistinct()
    {
        var state = new EditorRuntimeState(Definition(EditorValueType.String), "Tiếng");
        state.BeginComposition(); state.SetCandidate("Tieng");
        Assert.False(state.Commit(EditorValidationResult.Valid)); Assert.Equal("Tiếng", state.CommittedValue);
        state.EndComposition("Tiếng Việt 🌏"); state.SetCandidate("Tiếng Việt 🌏");
        Assert.True(state.Commit(EditorValidationResult.Valid)); Assert.Equal("Tiếng Việt 🌏", state.CommittedValue);
        state.SetCandidate("changed"); state.Cancel(); Assert.Equal("Tiếng Việt 🌏", state.CandidateValue);
    }

    [Fact]
    public async Task LookupIsBoundedAndSemantic()
    {
        var provider = new CountingProvider(); var coordinator = new EditorLookupCoordinator();
        var accepted = await coordinator.QueryAsync(provider, new("LOOKUP"), new("FIELD"), "999", 10_000);
        Assert.True(accepted); Assert.InRange(coordinator.Items.Length, 1, EditorLookupRequest.MaximumWindowSize);
        Assert.StartsWith("ID-", coordinator.Items[0].SemanticOptionId); Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task LateLookupGenerationAndCompanyAreRejected()
    {
        var provider = new DeferredProvider(); var coordinator = new EditorLookupCoordinator();
        coordinator.SetContext(new CompanyId("A"), "1");
        var first = coordinator.QueryAsync(provider, new("L"), new("F"), "a").AsTask();
        while (provider.Request is null) await Task.Yield();
        coordinator.SetContext(new CompanyId("B"), "2"); provider.Complete();
        Assert.False(await first); Assert.Equal(EditorLookupRuntimeStatus.StaleIgnored, coordinator.Status);
    }

    [Fact]
    public void DateRangeOrderingIsDeterministic() =>
        Assert.False(new DateRangeValue(new(2026, 2, 2), new(2026, 2, 1)).IsOrdered);

    [Fact]
    public void SemanticWidthIsMetadataAndDoesNotChangeEditorMeaning()
    {
        var compact = new EditorDefinition(new("CHOICE"), new("FORM.CHOICE"), EditorValueType.Choice,
            choices: [new("ACTIVE", new("Status.Active"), "Active")], width: EditorWidthClass.Compact);
        var localized = compact.Choices[0] with { SafeDisplayText = "Đang hoạt động" };

        Assert.Equal(EditorWidthClass.Compact, compact.Width);
        Assert.Equal("ACTIVE", localized.SemanticOptionId);
        Assert.Equal(EditorKind.Choice, new EditorResolver().Resolve(compact,
            EditorPlatformCapabilities.AllNative).Kind);
    }

    [Fact]
    public void MultiChoiceSelectionUsesStableSemanticIdentities()
    {
        var selected = new HashSet<string>(StringComparer.Ordinal) { "VI", "EN" };
        var relocalized = new[] { new EditorChoiceOption("VI", new("Language.Vietnamese"), "Tiếng Việt"),
            new EditorChoiceOption("EN", new("Language.English"), "English") };

        Assert.All(relocalized, option => Assert.Contains(option.SemanticOptionId, selected));
    }

    private sealed class CountingProvider : IEditorLookupProvider
    {
        public string ProviderCode => "COUNT"; public int Calls { get; private set; }
        public ValueTask<EditorLookupResult> QueryAsync(EditorLookupRequest request)
        {
            Calls++; var items = Enumerable.Range(0, request.BoundedWindowSize)
                .Select(x => new EditorLookupOption($"ID-{x}", $"Item {x}")).ToImmutableArray();
            return ValueTask.FromResult(new EditorLookupResult(items, "next", 100_000, request.Generation,
                request.CompanyId, request.ContextRevision));
        }
    }
    private sealed class DeferredProvider : IEditorLookupProvider
    {
        private readonly TaskCompletionSource<bool> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string ProviderCode => "DEFER"; public EditorLookupRequest? Request { get; private set; }
        public async ValueTask<EditorLookupResult> QueryAsync(EditorLookupRequest request)
        { Request = request; await gate.Task; return new([], null, 0, request.Generation, request.CompanyId, request.ContextRevision); }
        public void Complete() => gate.SetResult(true);
    }
}
