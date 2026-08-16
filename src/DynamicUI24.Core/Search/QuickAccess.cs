using System.Collections.Immutable;

namespace DynamicUI24.Core.Search;

public sealed record QuickAccessEntry(string EntryId, SearchResultKind Kind, string TargetCode,
    string? ProviderCode = null, CompanyScopeKind CompanyScope = CompanyScopeKind.Global,
    string? CompanyId = null, string? WorkspaceScope = null, int? PinnedOrder = null,
    DateTimeOffset? LastUsedAt = null);

public interface IQuickAccessStore
{
    IReadOnlyList<QuickAccessEntry> Favorites { get; }
    IReadOnlyList<QuickAccessEntry> Pinned { get; }
    IReadOnlyList<QuickAccessEntry> Recent { get; }
    bool AddFavorite(QuickAccessEntry entry);
    bool RemoveFavorite(string entryId);
    bool Pin(QuickAccessEntry entry);
    bool Unpin(string entryId);
    bool MovePinned(string entryId, int newIndex);
    void RecordRecent(QuickAccessEntry entry);
    bool RemoveRecent(string entryId);
    void ClearRecent();
}

public sealed class InMemoryQuickAccessStore(int recentLimit = 20) : IQuickAccessStore
{
    private readonly int recentLimit = recentLimit > 0 ? recentLimit : throw new ArgumentOutOfRangeException(nameof(recentLimit));
    private readonly List<QuickAccessEntry> favorites = [];
    private readonly List<QuickAccessEntry> pinned = [];
    private readonly List<QuickAccessEntry> recent = [];
    public IReadOnlyList<QuickAccessEntry> Favorites => favorites.ToImmutableArray();
    public IReadOnlyList<QuickAccessEntry> Pinned => pinned.OrderBy(x => x.PinnedOrder).ThenBy(x => x.EntryId, StringComparer.Ordinal).ToImmutableArray();
    public IReadOnlyList<QuickAccessEntry> Recent => recent.OrderByDescending(x => x.LastUsedAt).ThenBy(x => x.EntryId, StringComparer.Ordinal).ToImmutableArray();
    public bool AddFavorite(QuickAccessEntry entry) => AddUnique(favorites, entry with { PinnedOrder = null, LastUsedAt = null });
    public bool RemoveFavorite(string entryId) => Remove(favorites, entryId);
    public bool Pin(QuickAccessEntry entry) => AddUnique(pinned, entry with { PinnedOrder = pinned.Count, LastUsedAt = null });
    public bool Unpin(string entryId) { var changed = Remove(pinned, entryId); NormalizePins(); return changed; }
    public bool MovePinned(string entryId, int newIndex)
    {
        var index = pinned.FindIndex(x => Same(x.EntryId, entryId));
        if (index < 0 || newIndex < 0 || newIndex >= pinned.Count) return false;
        var item = pinned[index]; pinned.RemoveAt(index); pinned.Insert(newIndex, item); NormalizePins(); return true;
    }
    public void RecordRecent(QuickAccessEntry entry)
    {
        Remove(recent, entry.EntryId);
        var now = DateTimeOffset.UtcNow;
        var latest = recent.Count == 0 ? null : recent.Max(x => x.LastUsedAt);
        if (latest is { } previous && now <= previous) now = previous.AddTicks(1);
        recent.Insert(0, entry with { LastUsedAt = now, PinnedOrder = null });
        if (recent.Count > recentLimit) recent.RemoveRange(recentLimit, recent.Count - recentLimit);
    }
    public bool RemoveRecent(string entryId) => Remove(recent, entryId);
    public void ClearRecent() => recent.Clear();
    private static bool AddUnique(List<QuickAccessEntry> list, QuickAccessEntry entry)
    { if (list.Any(x => Same(x.EntryId, entry.EntryId))) return false; list.Add(entry); return true; }
    private static bool Remove(List<QuickAccessEntry> list, string entryId)
    { var index = list.FindIndex(x => Same(x.EntryId, entryId)); if (index < 0) return false; list.RemoveAt(index); return true; }
    private void NormalizePins() { for (var i = 0; i < pinned.Count; i++) pinned[i] = pinned[i] with { PinnedOrder = i }; }
    private static bool Same(string left, string right) => left.Equals(right, StringComparison.OrdinalIgnoreCase);
}

public interface IQuickAccessResolver
{
    SearchResult? Resolve(QuickAccessEntry entry, SearchQuery context);
}

public sealed class QuickAccessSearchProvider(IQuickAccessStore store, IQuickAccessResolver resolver) : ISearchProvider
{
    public string ProviderCode => "QUICK_ACCESS";
    public IReadOnlySet<SearchResultKind> SupportedKinds { get; } = new HashSet<SearchResultKind>
        { SearchResultKind.Pinned, SearchResultKind.Favorite, SearchResultKind.Recent };
    public Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = Project(store.Pinned, SearchResultKind.Pinned, query)
            .Concat(Project(store.Favorites, SearchResultKind.Favorite, query))
            .Concat(Project(store.Recent, SearchResultKind.Recent, query)).ToArray();
        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }
    private IEnumerable<SearchResult> Project(IReadOnlyList<QuickAccessEntry> entries, SearchResultKind kind, SearchQuery query) =>
        entries.Select(entry => (Entry: entry, Result: resolver.Resolve(entry, query))).Where(x => x.Result is not null)
            .Select(x => x.Result! with { ResultKind = kind, IsPinned = kind == SearchResultKind.Pinned,
                IsFavorite = kind == SearchResultKind.Favorite, LastUsedAt = x.Entry.LastUsedAt });
}
