using System.Collections.Immutable;
using DynamicUI24.Core.Authoring;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.DesignSystem;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class ThemeLifecycleFoundationTests
{
    [Fact]
    public void SemanticIdentityDoesNotDependOnDisplayName()
    {
        var code = new ThemeCode(" current ");
        var first = Published(code, 1, "First name", ValidMappings());
        var renamed = first with { DisplayName = "Localized name" };

        Assert.Equal("CURRENT", code.Value);
        Assert.Equal(first.Code, renamed.Code);
        Assert.Equal(first.Version, renamed.Version);
    }

    [Fact]
    public void DraftMutationIsSeparateFromImmutablePublishedVersion()
    {
        var published = Published(new("CURRENT"), 1, "Current", ValidMappings());
        var draft = Draft(published);

        draft.Update("Draft name", ValidMappings(accent: "#000000"));

        Assert.Equal("Current", published.DisplayName);
        Assert.Equal("#FFFFFF", published.Mappings.Colors.Values[ThemeMode.Light][DesignTokens.Color.TextPrimary]);
        Assert.Equal(1, draft.Generation.Value);
    }

    [Fact]
    public async Task CriticalValidationFailureBlocksPublish()
    {
        var repository = new TestThemeRepository(Published(new("CURRENT"), 1, "Current", ValidMappings()));
        var service = Service(repository);
        var invalid = Draft(repository.Active, ValidMappings(includeDark: false)).Snapshot();

        var validation = Validator().Validate(invalid);
        var error = Assert.Single(validation.Diagnostics.Where(item =>
            item.Code == "THEME_APPEARANCE_MAPPING_MISSING"));
        Assert.Equal(ThemeValidationSeverity.Error, error.Severity);
        Assert.False(validation.CanPublish);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.PublishAsync(invalid, repository.Generation, "invalid", "invalid", true, Context(AllCapabilities())));
        Assert.Single(repository.History);
    }

    [Fact]
    public async Task PreviewIsIsolatedFromActiveTheme()
    {
        var repository = new TestThemeRepository(Published(new("CURRENT"), 1, "Current", ValidMappings()));
        var service = Service(repository);
        var draft = Draft(repository.Active, ValidMappings(accent: "#123456")).Snapshot();

        var preview = await service.PreviewAsync(draft, Context(DesignSystemCapabilities.Preview));

        Assert.Equal("#123456", preview.Resolve(DesignTokens.Color.AccentPrimary, ThemeMode.Light)?.Value);
        Assert.Equal(new ThemeVersion(1), repository.Active.Version);
        Assert.Equal("#0066CC", repository.Active.Mappings.Colors.Values[ThemeMode.Light][DesignTokens.Color.AccentPrimary]);
    }

    [Fact]
    public void EditorGeometryIsThemeResolvedAndInvalidRelationshipsCannotPublish()
    {
        var mappings = ValidMappings() with
        {
            Spacing = new(ImmutableDictionary<DesignTokenKey, double>.Empty
                .Add(DesignTokens.Editor.ContentPadding, 4)
                .Add(DesignTokens.Editor.MultiChoiceOptionGap, 8)),
            Sizing = new(ImmutableDictionary<DesignTokenKey, double>.Empty
                .Add(DesignTokens.Size.HitTargetMinimum, 32)
                .Add(DesignTokens.Size.EditorControlHeight, 32)
                .Add(DesignTokens.Size.EditorIconSize, 16)
                .Add(DesignTokens.Size.EditorTrailingSlotWidth, 12)
                .Add(DesignTokens.Size.EditorLeadingSlotWidth, 24)
                .Add(DesignTokens.Size.MultiChoiceCheckSize, 20)
                .Add(DesignTokens.Size.PopupMaxHeight, 32)
                .Add(DesignTokens.Size.PopupOptionHeight, 32))
        };
        var draft = Draft(Published(new("CURRENT"), 1, "Current", mappings)).Snapshot();
        var result = Validator().Validate(draft);
        var preview = new ThemePreviewSession(new("preview"), draft);

        Assert.Contains(result.Diagnostics, item => item.Code == "THEME_EDITOR_TRAILING_SLOT_INVALID");
        Assert.Contains(result.Diagnostics, item => item.Code == "THEME_EDITOR_LEADING_CHECK_SLOT_INVALID");
        Assert.Contains(result.Diagnostics, item => item.Code == "THEME_EDITOR_CONTENT_PADDING_BELOW_MINIMUM");
        Assert.Contains(result.Diagnostics, item => item.Code == "THEME_POPUP_HEIGHT_INVALID");
        Assert.Equal("16", preview.Resolve(DesignTokens.Size.EditorIconSize, ThemeMode.Light)?.Value);
    }

    [Fact]
    public async Task PublishAndActivateIsAtomicRetainsHistoryAndSupportsIdempotentRetry()
    {
        var repository = new TestThemeRepository(Published(new("CURRENT"), 1, "Current", ValidMappings()));
        var service = Service(repository);
        var draft = Draft(repository.Active, ValidMappings(accent: "#123456")).Snapshot();
        var context = Context(DesignSystemCapabilities.Publish, DesignSystemCapabilities.Activate);

        var first = await service.PublishAsync(draft, repository.Generation, "publish-2", "New palette", true, context);
        var replay = await service.PublishAsync(draft, new(1), "publish-2", "New palette", true, context);

        Assert.Equal(new ThemeVersion(2), first.Definition.Version);
        Assert.Equal(new ThemeVersion(2), repository.Active.Version);
        Assert.Equal(2, repository.History.Count);
        Assert.True(replay.IsIdempotentReplay);
    }

    [Fact]
    public async Task PublicationAndLaterActivationRemainSeparateAuthorizedMutations()
    {
        var repository = new TestThemeRepository(Published(new("CURRENT"), 1, "Current", ValidMappings()));
        var service = Service(repository);
        var published = await service.PublishAsync(Draft(repository.Active).Snapshot(), repository.Generation,
            "publish-only", "Publish only", activate: false,
            context: Context(DesignSystemCapabilities.Publish));

        Assert.False(published.IsActive);
        Assert.Equal(new ThemeVersion(1), repository.Active.Version);

        await service.ActivateAsync(new("CURRENT"), published.Definition.Version, repository.Generation,
            "activate-2", Context(DesignSystemCapabilities.Activate));

        Assert.Equal(new ThemeVersion(2), repository.Active.Version);
        Assert.Equal(2, repository.History.Count);
    }

    [Fact]
    public async Task RollbackRetainsNewerHistoryAndNextPublishUsesHistoricalMaximum()
    {
        var repository = new TestThemeRepository(Published(new("CURRENT"), 1, "Current", ValidMappings()));
        var service = Service(repository);
        var publishContext = Context(DesignSystemCapabilities.Publish, DesignSystemCapabilities.Activate);
        var secondDraft = Draft(repository.Active, ValidMappings(accent: "#222222")).Snapshot();
        await service.PublishAsync(secondDraft, repository.Generation, "publish-2", "Second", true, publishContext);

        await service.RollbackAsync(new("CURRENT"), new(1), repository.Generation, "rollback-1",
            Context(DesignSystemCapabilities.Rollback));
        var thirdDraft = Draft(repository.Active, ValidMappings(accent: "#333333")).Snapshot();
        var third = await service.PublishAsync(thirdDraft, repository.Generation, "publish-3", "Third", true, publishContext);

        Assert.Equal(new ThemeVersion(3), third.Definition.Version);
        Assert.Equal(3, repository.History.Count);
        Assert.Contains(repository.History, item => item.Version == new ThemeVersion(2));
    }

    [Fact]
    public async Task ConflictFailsBeforeHistoryOrActiveMutation()
    {
        var repository = new TestThemeRepository(Published(new("CURRENT"), 1, "Current", ValidMappings()));
        var service = Service(repository);
        var draft = Draft(repository.Active).Snapshot();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.PublishAsync(draft, new(99), "conflict", "Conflict", true,
                Context(DesignSystemCapabilities.Publish, DesignSystemCapabilities.Activate)));

        Assert.Single(repository.History);
        Assert.Equal(new ThemeVersion(1), repository.Active.Version);
        Assert.Equal(new ThemeGeneration(1), repository.Generation);
    }

    [Fact]
    public async Task AuthorizationDenialOccursBeforeRepositoryMutation()
    {
        var repository = new TestThemeRepository(Published(new("CURRENT"), 1, "Current", ValidMappings()));
        var service = Service(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await service.PublishAsync(Draft(repository.Active).Snapshot(), repository.Generation,
                "denied", "Denied", true, Context()));

        Assert.Single(repository.History);
    }

    [Fact]
    public void LifecycleContractsContainNoBusinessOrVisualControlState()
    {
        var lifecycleTypes = new[]
        {
            typeof(ThemeLifecycleService), typeof(ThemePublishRequest), typeof(ThemeActivationRequest),
            typeof(ThemePreviewSession), typeof(ThemeVersionDefinition),
        };

        Assert.All(lifecycleTypes.SelectMany(type => type.GetProperties()), property =>
        {
            Assert.DoesNotContain("Business", property.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Control", property.PropertyType.FullName ?? string.Empty, StringComparison.Ordinal);
        });
    }

    private static ThemeLifecycleService Service(IThemeLifecycleRepository repository) =>
        new(repository, Validator());

    private static ThemeValidator Validator() => new(new TestValidationPolicy());

    private static ThemeLifecycleContext Context(params CapabilityCode[] capabilities) =>
        new(new UserSecurityContext("SUBJECT", 1, new HashSet<PermissionCode>(), capabilities.ToHashSet()));

    private static CapabilityCode[] AllCapabilities() =>
        [DesignSystemCapabilities.EditDraft, DesignSystemCapabilities.Preview,
         DesignSystemCapabilities.Publish, DesignSystemCapabilities.Activate, DesignSystemCapabilities.Rollback];

    private static ThemeDraft Draft(ThemeVersionDefinition published, ThemeMappings? mappings = null) =>
        new(new("draft"), published.Code, published.Version, new(0), published.StandardVersion,
            published.DisplayName, mappings ?? published.Mappings);

    private static ThemeVersionDefinition Published(ThemeCode code, long version, string name, ThemeMappings mappings) =>
        new(code, new(version), DefaultPresentationStandard.Version, name, mappings,
            DateTimeOffset.UnixEpoch.AddDays(version), $"Version {version}");

    private static ThemeMappings ValidMappings(bool includeDark = true, string accent = "#0066CC")
    {
        var light = ImmutableDictionary<DesignTokenKey, string>.Empty
            .Add(DesignTokens.Color.TextPrimary, "#FFFFFF")
            .Add(DesignTokens.Color.AccentPrimary, accent);
        var colors = ImmutableDictionary<ThemeMode, ImmutableDictionary<DesignTokenKey, string>>.Empty
            .Add(ThemeMode.Light, light);
        if (includeDark) colors = colors.Add(ThemeMode.Dark, light);
        var typography = ImmutableDictionary<DesignTokenKey, FontMapping>.Empty
            .Add(DesignTokens.Typography.Body, new("system-ui", ["sans-serif"]));
        var sizes = ImmutableDictionary<DesignTokenKey, double>.Empty
            .Add(DesignTokens.Size.HitTargetMinimum, 32);
        var motion = ImmutableDictionary<DesignTokenKey, MotionRecipe>.Empty
            .Add(DesignTokens.Motion.Standard, new(160, "standard", true));
        return new(new(typography), new(colors), new(ImmutableDictionary<DesignTokenKey, double>.Empty), new(sizes),
            new(ImmutableDictionary<DesignTokenKey, double>.Empty), new(ImmutableDictionary<DesignTokenKey, double>.Empty),
            new(ImmutableDictionary<DesignTokenKey, string>.Empty), new(ImmutableDictionary<DesignTokenKey, double>.Empty),
            new(ImmutableDictionary<DesignTokenKey, string>.Empty), new(motion),
            new(DensityRole.Standard, ImmutableDictionary<DensityRole,
                ImmutableDictionary<DesignTokenKey, double>>.Empty),
            ImmutableDictionary<string, ImmutableHashSet<DesignTokenKey>>.Empty);
    }

    private sealed class TestValidationPolicy : IThemeValidationPolicy
    {
        public string StandardVersion => DefaultPresentationStandard.Version;
        public IReadOnlySet<DesignTokenKey> RequiredColorTokens { get; } =
            new HashSet<DesignTokenKey> { DesignTokens.Color.TextPrimary, DesignTokens.Color.AccentPrimary };
        public IReadOnlySet<DesignTokenKey> RequiredTypographyTokens { get; } =
            new HashSet<DesignTokenKey> { DesignTokens.Typography.Body };
        public double MinimumHitTarget => 32;
        public bool IsFontMappingSupported(FontMapping mapping) =>
            !string.IsNullOrWhiteSpace(mapping.PrimaryFamily) && mapping.FallbackFamilies.Length > 0;
        public IEnumerable<ThemeValidationDiagnostic> ValidateAccessibility(ThemeDraftSnapshot draft) => [];
    }

    private sealed class TestThemeRepository : IThemeLifecycleRepository
    {
        private readonly SortedDictionary<long, ThemeVersionDefinition> versions = [];
        private readonly Dictionary<string, ThemePublishResult> publishReceipts = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ThemeActivationResult> activationReceipts = new(StringComparer.Ordinal);
        private readonly Dictionary<ThemeDraftId, ThemeDraftSnapshot> drafts = [];

        public TestThemeRepository(ThemeVersionDefinition seed)
        {
            versions.Add(seed.Version.Value, seed);
            Active = seed;
            Generation = new(1);
        }

        public ThemeVersionDefinition Active { get; private set; }
        public ThemeGeneration Generation { get; private set; }
        public IReadOnlyCollection<ThemeVersionDefinition> History => versions.Values;

        public ValueTask SaveDraftAsync(ThemeDraftSnapshot draft, ThemeGeneration expectedGeneration,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (drafts.TryGetValue(draft.Id, out var current) && current.Generation != expectedGeneration)
                throw new InvalidOperationException("THEME_DRAFT_GENERATION_CONFLICT");
            drafts[draft.Id] = draft;
            return ValueTask.CompletedTask;
        }

        public ValueTask<ThemeDraftSnapshot?> GetDraftAsync(ThemeDraftId id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(drafts.GetValueOrDefault(id));

        public ValueTask<ThemeVersionDefinition?> GetActiveAsync(ThemeCode code, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ThemeVersionDefinition?>(Active.Code == code ? Active : null);

        public ValueTask<IReadOnlyList<ThemeVersionInfo>> GetVersionHistoryAsync(ThemeCode code,
            CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyList<ThemeVersionInfo>>(
                versions.Values.Where(item => item.Code == code).Select(item => new ThemeVersionInfo(item.Code,
                    item.Version, item.PublishedAt, item.SafeChangeSummary, item.Version == Active.Version)).ToArray());

        public ValueTask<ThemePublishResult> PublishAsync(ThemePublishRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (publishReceipts.TryGetValue(request.PublishRequestId, out var replay))
            {
                if (replay.Definition.Code != request.Draft.Code ||
                    replay.Definition.SafeChangeSummary != request.SafeChangeSummary)
                    throw new InvalidOperationException("THEME_PUBLISH_REQUEST_CONFLICT");
                return ValueTask.FromResult(replay with { IsIdempotentReplay = true });
            }
            if (Generation != request.ExpectedRepositoryGeneration || Active.Version != request.ExpectedActiveVersion)
                throw new InvalidOperationException("THEME_GENERATION_CONFLICT");

            var next = checked(versions.Keys.Max() + 1);
            var definition = new ThemeVersionDefinition(request.Draft.Code, new(next), request.Draft.StandardVersion,
                request.Draft.DisplayName, request.Draft.Mappings, request.PublishedAt, request.SafeChangeSummary);
            var nextGeneration = new ThemeGeneration(checked(Generation.Value + 1));
            versions.Add(next, definition);
            if (request.Activate) Active = definition;
            Generation = nextGeneration;
            var result = new ThemePublishResult(definition, Generation, false, request.Activate);
            publishReceipts.Add(request.PublishRequestId, result);
            return ValueTask.FromResult(result);
        }

        public ValueTask<ThemeActivationResult> ActivateAsync(ThemeActivationRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (activationReceipts.TryGetValue(request.RequestId, out var replay))
                return ValueTask.FromResult(replay with { IsIdempotentReplay = true });
            if (Generation != request.ExpectedRepositoryGeneration)
                throw new InvalidOperationException("THEME_GENERATION_CONFLICT");
            if (!versions.TryGetValue(request.Version.Value, out var definition) || definition.Code != request.Code)
                throw new InvalidOperationException("THEME_VERSION_NOT_FOUND");
            Active = definition;
            Generation = new(checked(Generation.Value + 1));
            var result = new ThemeActivationResult(request.Code, request.Version, Generation, false);
            activationReceipts.Add(request.RequestId, result);
            return ValueTask.FromResult(result);
        }
    }
}
