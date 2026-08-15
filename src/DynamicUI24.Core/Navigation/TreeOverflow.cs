namespace DynamicUI24.Core.Navigation;

public sealed record TreeOverflowOptions
{
    public TreeOverflowOptions(int initialVisibleChildCount = 8, int expansionPageSize = 8,
        bool showLessEnabled = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialVisibleChildCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expansionPageSize);
        InitialVisibleChildCount = initialVisibleChildCount;
        ExpansionPageSize = expansionPageSize;
        ShowLessEnabled = showLessEnabled;
    }

    public int InitialVisibleChildCount { get; }
    public int ExpansionPageSize { get; }
    public bool ShowLessEnabled { get; }
}

public sealed record TreeChildWindow(int TotalCount, int VisibleCount, bool CanShowMore, bool CanShowLess)
{
    public int RemainingCount => Math.Max(0, TotalCount - VisibleCount);
}

/// <summary>Keeps incremental child windows by parent identity independently of UI and tree contents.</summary>
public sealed class TreeOverflowController
{
    public const string RootParentKey = "$ROOT";
    private readonly Dictionary<string, int> visibleCounts = new(StringComparer.OrdinalIgnoreCase);

    public TreeOverflowController(TreeOverflowOptions? options = null) =>
        Options = options ?? new TreeOverflowOptions();

    public TreeOverflowOptions Options { get; }

    public TreeChildWindow GetWindow(string? parentNodeId, int totalCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        var key = Key(parentNodeId);
        var requested = visibleCounts.GetValueOrDefault(key, Options.InitialVisibleChildCount);
        var visible = Math.Min(requested, totalCount);
        return new(totalCount, visible, visible < totalCount,
            Options.ShowLessEnabled && visible > Math.Min(Options.InitialVisibleChildCount, totalCount));
    }

    public TreeChildWindow ShowMore(string? parentNodeId, int totalCount)
    {
        var current = GetWindow(parentNodeId, totalCount);
        visibleCounts[Key(parentNodeId)] = Math.Min(totalCount, current.VisibleCount + Options.ExpansionPageSize);
        return GetWindow(parentNodeId, totalCount);
    }

    public TreeChildWindow ShowLess(string? parentNodeId, int totalCount)
    {
        visibleCounts.Remove(Key(parentNodeId));
        return GetWindow(parentNodeId, totalCount);
    }

    public void EnsureVisible(string? parentNodeId, int zeroBasedChildIndex, int totalCount)
    {
        if (zeroBasedChildIndex < 0 || zeroBasedChildIndex >= totalCount)
            throw new ArgumentOutOfRangeException(nameof(zeroBasedChildIndex));
        var needed = zeroBasedChildIndex + 1;
        if (needed > GetWindow(parentNodeId, totalCount).VisibleCount)
            visibleCounts[Key(parentNodeId)] = needed;
    }

    public void Reset(string? parentNodeId = null)
    {
        if (parentNodeId is null) visibleCounts.Clear();
        else visibleCounts.Remove(Key(parentNodeId));
    }

    private static string Key(string? parentNodeId) => parentNodeId ?? RootParentKey;
}
