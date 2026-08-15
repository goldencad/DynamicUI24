using Avalonia;
using Avalonia.Controls;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Reusable, runtime-resizable navigation/workspace layout with no persistence policy.</summary>
public sealed class DynamicSplitNavigationHost : Grid
{
    private readonly SplitNavigationLayoutState state;
    private readonly ColumnDefinition navigationColumn;
    private readonly ContentControl navigationPresenter = new();
    private readonly ContentControl workspacePresenter = new();
    private bool applyingWidth;

    public DynamicSplitNavigationHost(SplitNavigationLayoutState? state = null)
    {
        this.state = state ?? new SplitNavigationLayoutState();
        navigationColumn = new(new GridLength(this.state.NavigationWidth, GridUnitType.Pixel))
        {
            MinWidth = this.state.MinimumNavigationWidth,
            MaxWidth = this.state.MaximumNavigationWidth,
        };
        ColumnDefinitions.Add(navigationColumn);
        ColumnDefinitions.Add(new(new GridLength(this.state.SplitterWidth, GridUnitType.Pixel)));
        ColumnDefinitions.Add(new(GridLength.Star));

        navigationPresenter.HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
        navigationPresenter.VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
        workspacePresenter.HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
        workspacePresenter.VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
        Splitter = new GridSplitter
        {
            ResizeDirection = GridResizeDirection.Columns,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
        };
        Splitter.Bind(BackgroundProperty, this.GetResourceObservable("DuiBorderBrush"));
        Grid.SetColumn(navigationPresenter, 0);
        Grid.SetColumn(Splitter, 1);
        Grid.SetColumn(workspacePresenter, 2);
        Children.Add(navigationPresenter);
        Children.Add(Splitter);
        Children.Add(workspacePresenter);
        navigationColumn.PropertyChanged += (_, args) =>
        {
            if (!applyingWidth && args.Property == ColumnDefinition.WidthProperty && navigationColumn.Width.IsAbsolute)
                this.state.Resize(navigationColumn.Width.Value);
        };
    }

    public GridSplitter Splitter { get; }
    public bool IsRuntimeResizable => Splitter.ResizeDirection == GridResizeDirection.Columns;
    public double NavigationWidth => state.NavigationWidth;
    public SplitNavigationLayoutState LayoutState => state;

    public Control? NavigationContent
    {
        get => navigationPresenter.Content as Control;
        set => navigationPresenter.Content = value;
    }

    public Control? WorkspaceContent
    {
        get => workspacePresenter.Content as Control;
        set => workspacePresenter.Content = value;
    }

    public double ResizeNavigation(double requestedWidth)
    {
        var width = state.Resize(requestedWidth);
        applyingWidth = true;
        navigationColumn.Width = new(width, GridUnitType.Pixel);
        applyingWidth = false;
        return width;
    }
}
