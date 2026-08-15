using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class SplitNavigationLayoutTests
{
    [Fact]
    public void RuntimeResizeUsesConfiguredBounds()
    {
        var state = new SplitNavigationLayoutState(260, 180, 520, 6);
        Assert.Equal(260, state.NavigationWidth);
        Assert.Equal(400, state.Resize(400));
        Assert.Equal(180, state.Resize(20));
        Assert.Equal(520, state.Resize(900));
        Assert.Equal(6, state.SplitterWidth);
    }

    [Fact]
    public void InitialWidthIsClampedAndInvalidDimensionsAreRejected()
    {
        Assert.Equal(180, new SplitNavigationLayoutState(100, 180, 520).NavigationWidth);
        Assert.Equal(520, new SplitNavigationLayoutState(900, 180, 520).NavigationWidth);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SplitNavigationLayoutState(260, 0, 520));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SplitNavigationLayoutState(260, 520, 180));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SplitNavigationLayoutState(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SplitNavigationLayoutState(splitterWidth: 0));
    }

    [Fact]
    public void WidthIsRuntimeOnlyAndNewLayoutStartsFromItsOwnInitialValue()
    {
        var first = new SplitNavigationLayoutState(260);
        first.Resize(410);
        var second = new SplitNavigationLayoutState(260);
        Assert.Equal(410, first.NavigationWidth);
        Assert.Equal(260, second.NavigationWidth);
    }
}
