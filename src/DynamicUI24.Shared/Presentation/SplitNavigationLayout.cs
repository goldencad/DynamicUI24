namespace DynamicUI24.Shared.Presentation;

/// <summary>Runtime-only split layout dimensions. Persistence is intentionally outside this foundation.</summary>
public sealed class SplitNavigationLayoutState
{
    public SplitNavigationLayoutState(double initialNavigationWidth = 260, double minimumNavigationWidth = 180,
        double maximumNavigationWidth = 520, double splitterWidth = 5)
    {
        if (!double.IsFinite(minimumNavigationWidth) || minimumNavigationWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(minimumNavigationWidth));
        if (!double.IsFinite(maximumNavigationWidth) || maximumNavigationWidth < minimumNavigationWidth)
            throw new ArgumentOutOfRangeException(nameof(maximumNavigationWidth));
        if (!double.IsFinite(initialNavigationWidth)) throw new ArgumentOutOfRangeException(nameof(initialNavigationWidth));
        if (!double.IsFinite(splitterWidth) || splitterWidth <= 0) throw new ArgumentOutOfRangeException(nameof(splitterWidth));
        MinimumNavigationWidth = minimumNavigationWidth;
        MaximumNavigationWidth = maximumNavigationWidth;
        SplitterWidth = splitterWidth;
        NavigationWidth = Clamp(initialNavigationWidth);
    }

    public double NavigationWidth { get; private set; }
    public double MinimumNavigationWidth { get; }
    public double MaximumNavigationWidth { get; }
    public double SplitterWidth { get; }

    public double Resize(double requestedWidth)
    {
        if (!double.IsFinite(requestedWidth)) throw new ArgumentOutOfRangeException(nameof(requestedWidth));
        NavigationWidth = Clamp(requestedWidth);
        return NavigationWidth;
    }

    private double Clamp(double width) => Math.Clamp(width, MinimumNavigationWidth, MaximumNavigationWidth);
}

/// <summary>Shared three-region shell dimensions for navigation, workspace, and optional context.</summary>
public sealed class ShellSplitLayoutState
{
    public ShellSplitLayoutState(double navigationWidth = 260, double contextWidth = 320,
        double minimumNavigationWidth = 180, double maximumNavigationWidth = 520,
        double minimumContextWidth = 240, double maximumContextWidth = 560,
        double minimumWorkspaceWidth = 420, double splitterWidth = 5)
    {
        Navigation = new(navigationWidth, minimumNavigationWidth, maximumNavigationWidth, splitterWidth);
        Context = new(contextWidth, minimumContextWidth, maximumContextWidth, splitterWidth);
        if (!double.IsFinite(minimumWorkspaceWidth) || minimumWorkspaceWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(minimumWorkspaceWidth));
        MinimumWorkspaceWidth = minimumWorkspaceWidth;
    }
    public SplitNavigationLayoutState Navigation { get; }
    public SplitNavigationLayoutState Context { get; }
    public double MinimumWorkspaceWidth { get; }
    public double SplitterWidth => Navigation.SplitterWidth;
    public double BoundContextWidth(double requested, double availableWidth)
    {
        var room = Math.Max(Context.MinimumNavigationWidth,
            availableWidth - Navigation.NavigationWidth - MinimumWorkspaceWidth - (SplitterWidth * 2));
        return Context.Resize(Math.Min(requested, room));
    }
}
