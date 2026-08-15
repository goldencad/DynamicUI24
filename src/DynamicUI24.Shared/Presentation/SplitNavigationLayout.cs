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
