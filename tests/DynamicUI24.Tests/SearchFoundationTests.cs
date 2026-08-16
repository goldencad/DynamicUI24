using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Navigation;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Ribbon;
using DynamicUI24.Core.Search;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class SearchFoundationTests
{
    [Theory]
    [InlineData("  ĐàTa  ", "DATA")]
    [InlineData("Report", "REPORT")]
    [InlineData(null, "")]
    public void TextNormalizationIsTrimmedCaseAndDiacriticInsensitive(string? input, string expected) =>
        Assert.Equal(expected, SearchText.Normalize(input));

    [Fact]
    public async Task ExactPrefixContainsPinnedAndStableIdRankingIsDeterministic()
    {
        SearchResult[] results =
        [
            Result("z", "My data contains"), Result("prefix", "Data entry"),
            Result("exact", "Data"), Result("a", "Data alphabet", pinned: true),
        ];
        var coordinator = new SearchCoordinator([new Provider("p", results)]);
        var first = await coordinator.SearchAsync(new("data", SearchScope.GlobalSearch));
        var second = await coordinator.SearchAsync(new("data", SearchScope.GlobalSearch));
        Assert.Equal(["exact", "a", "prefix", "z"], first.Results.Select(x => x.ResultId));
        Assert.Equal(first.Results.Select(x => x.ResultId), second.Results.Select(x => x.ResultId));
    }

    [Fact]
    public async Task SemanticDeduplicationAndLimitsAreApplied()
    {
        var left = Result("left", "Shared") with { DeduplicationKey = "workspace:one" };
        var right = Result("right", "Shared") with { DeduplicationKey = "workspace:one" };
        var many = Enumerable.Range(0, 20).Select(i => Result($"r{i:00}", $"Result {i}"));
        var coordinator = new SearchCoordinator([new Provider("a", [left]), new Provider("b", [right, .. many])], new(5, 5));
        var response = await coordinator.SearchAsync(new("", SearchScope.GlobalSearch));
        Assert.Equal(5, response.Results.Length);
        Assert.Single(response.Results.Where(x => x.SemanticIdentity == "workspace:one"));
    }

    [Fact]
    public async Task ProviderFailureIsIsolatedAndMalformedLabelsAreRejected()
    {
        var coordinator = new SearchCoordinator([new ThrowingProvider(), new Provider("ok", [Result("ok", "Safe"), Result("bad", "")])]);
        var response = await coordinator.SearchAsync(new("", SearchScope.GlobalSearch));
        Assert.Single(response.Results);
        Assert.Equal("FAIL", Assert.Single(response.FailedProviderCodes));
    }

    [Fact]
    public async Task LateOlderGenerationCannotPublish()
    {
        var provider = new DelayedProvider();
        var coordinator = new SearchCoordinator([provider]);
        var oldTask = coordinator.SearchAsync(new("a", SearchScope.GlobalSearch));
        await provider.FirstStarted.Task;
        var currentTask = coordinator.SearchAsync(new("abc", SearchScope.GlobalSearch));
        provider.ReleaseSecond.SetResult();
        var current = await currentTask;
        provider.ReleaseFirst.SetResult();
        var old = await oldTask;
        Assert.False(current.IsStale);
        Assert.Equal("abc", Assert.Single(current.Results).ResultId);
        Assert.True(old.IsStale);
        Assert.Empty(old.Results);
    }

    [Fact]
    public async Task PermissionAndCompanyScopeFailClosed()
    {
        var company = new CompanyId("b");
        var auth = new EffectiveAuthorizationContext(new("u"), company, [], [], "r");
        var hidden = Result("hidden", "Hidden") with { PresentationRequirement =
            new(new PermissionCode("VIEW"), UnauthorizedBehavior: UnauthorizedBehavior.Hide) };
        var disabled = Result("disabled", "Disabled") with { PresentationRequirement =
            new(new PermissionCode("EDIT"), UnauthorizedBehavior: UnauthorizedBehavior.Disable) };
        var companyA = Result("a", "A") with { CompanyScope = CompanyScopeKind.CompanyScoped, CompanyId = "a" };
        var result = await new SearchCoordinator([new Provider("p", [hidden, disabled, companyA])])
            .SearchAsync(new("", SearchScope.GlobalSearch, "b", PermissionContext: auth));
        Assert.Single(result.Results);
        Assert.False(result.Results[0].IsActionable);
    }

    [Fact]
    public void PrivacyPresentationMasksWithoutChangingIdentity()
    {
        var result = new SearchResult("secret", SearchResultKind.Record, "p", "Employee A", "123456789",
            workspaceId: "data", privacyMetadata: new(Sensitivity.Restricted, PrivacyPresentation.Mask));
        var presenter = new SearchResultPresenter(new PrivacyPolicyResolver(), new SensitiveValuePresenter());
        var context = new PrivacyResolutionContext(true, null, PrivacyMode.Off, new MandatoryPrivacyPolicy());
        var shown = presenter.Present(result, context);
        Assert.DoesNotContain("123456789", shown.Subtitle);
        Assert.Equal(result.SemanticIdentity, shown.Result.SemanticIdentity);
    }

    [Fact]
    public void NavigationSearchRetainsAncestorsAndFindsCollapsedDescendants()
    {
        var tree = new TreeDefinition("t", "t", 1,
        [
            new("root", "ROOT", new("Root")),
            new("nested", "NESTED", new("Nested"), "root", workspaceId: "one"),
        ]);
        var company = new CompanyDescriptor(new("a"), "A", "A");
        var resolved = new DynamicTreeResolver().Resolve(tree, new(company, null),
            [new WorkspaceDefinition("one", "One", new TemplateCode("ONE"))]);
        var filtered = new NavigationTreeSearch().Filter(resolved.RootNodes, "nested");
        Assert.Equal("root", Assert.Single(filtered).Definition.NodeId);
        Assert.Equal("nested", Assert.Single(filtered[0].Children).Definition.NodeId);
        Assert.Equal(resolved.RootNodes, new NavigationTreeSearch().Filter(resolved.RootNodes, ""));
    }

    [Fact]
    public void FavoritesPinnedAndRecentAreIndependentIdempotentOrderedAndBounded()
    {
        var store = new InMemoryQuickAccessStore(2);
        var one = Entry("one"); var two = Entry("two"); var three = Entry("three");
        Assert.True(store.AddFavorite(one)); Assert.False(store.AddFavorite(one));
        Assert.True(store.Pin(one)); Assert.True(store.Pin(two)); Assert.True(store.MovePinned("two", 0));
        Assert.Equal(["two", "one"], store.Pinned.Select(x => x.EntryId));
        Assert.True(store.Unpin("one")); Assert.Single(store.Favorites);
        store.RecordRecent(one); store.RecordRecent(two); store.RecordRecent(one); store.RecordRecent(three);
        Assert.Equal(["three", "one"], store.Recent.Select(x => x.EntryId));
        store.ClearRecent(); Assert.Empty(store.Recent);
    }

    [Fact]
    public async Task ActivationReusesNavigationAndCommandAndRecordsOnlySuccess()
    {
        var workspace = new WorkspaceDefinition("one", "One", new TemplateCode("ONE"));
        var navigation = new WorkspaceNavigationService([workspace]);
        var commands = new UiCommandRegistry();
        commands.Register("OK", (_, _) => Task.FromResult(RibbonCommandResult.Success()));
        var store = new InMemoryQuickAccessStore();
        var service = new SearchActivationService(navigation, commands, CommandContext, quickAccess: store);
        var nav = Result("one", "One") with { ResultKind = SearchResultKind.Workspace, WorkspaceId = "one", CanRecordRecent = true };
        Assert.Equal(SearchActivationStatus.Success, (await service.ActivateAsync(nav)).Status);
        Assert.Equal("one", navigation.CurrentWorkspace?.WorkspaceId);
        Assert.Single(store.Recent);
        Assert.Equal(SearchActivationStatus.Unavailable, (await service.ActivateAsync(nav with { WorkspaceId = "missing" })).Status);
        Assert.Single(store.Recent);
        var command = Result("cmd", "Command") with { ResultKind = SearchResultKind.Command, RegisteredCommandCode = "OK" };
        Assert.Equal(SearchActivationStatus.Success, (await service.ActivateAsync(command)).Status);
        Assert.Equal(SearchActivationStatus.Unavailable,
            (await service.ActivateAsync(command with { RegisteredCommandCode = "UNKNOWN" })).Status);
    }

    private static SearchResult Result(string id, string text, bool pinned = false) =>
        new(id, SearchResultKind.Workspace, "p", text, workspaceId: id, isPinned: pinned);
    private static QuickAccessEntry Entry(string id) => new(id, SearchResultKind.Workspace, id);
    private static RibbonCommandExecutionContext CommandContext() => new(new(
        new CompanyDescriptor(new("a"), "A", "A"),
        new WorkspaceDefinition("one", "One", new TemplateCode("ONE")), new TemplateCode("ONE"), null, new(0)));

    private sealed class Provider(string code, IReadOnlyList<SearchResult> results) : ISearchProvider
    {
        public string ProviderCode => code;
        public IReadOnlySet<SearchResultKind> SupportedKinds { get; } = new HashSet<SearchResultKind> { SearchResultKind.Workspace };
        public Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) => Task.FromResult(results);
    }
    private sealed class ThrowingProvider : ISearchProvider
    {
        public string ProviderCode => "FAIL";
        public IReadOnlySet<SearchResultKind> SupportedKinds { get; } = new HashSet<SearchResultKind>();
        public Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) => throw new Exception();
    }
    private sealed class DelayedProvider : ISearchProvider
    {
        private int calls;
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFirst { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseSecond { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string ProviderCode => "DELAY";
        public IReadOnlySet<SearchResultKind> SupportedKinds { get; } = new HashSet<SearchResultKind> { SearchResultKind.Workspace };
        public async Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref calls) == 1) { FirstStarted.SetResult(); await ReleaseFirst.Task; }
            else await ReleaseSecond.Task;
            return [Result(query.QueryText, query.QueryText)];
        }
    }
}
