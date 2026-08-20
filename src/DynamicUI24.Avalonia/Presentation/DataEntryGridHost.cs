using System.Globalization;
using System.Collections.Immutable;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Editors;
using DynamicUI24.Avalonia.Presentation.Editors;
using DynamicUI24.Core.Setup;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Shared.Presentation;
using GridMetadataColumn = DynamicUI24.Core.Setup.ColumnDefinition;

namespace DynamicUI24.Avalonia.Presentation;

public enum GridActivationStage { WorkspaceVisible, DataAvailable, InteractiveReady }
public sealed record GridActivationTiming(TimeSpan WorkspaceVisible, TimeSpan DataAvailable,
    TimeSpan InteractiveReady, TimeSpan StableLayout, int ProviderRequests, int Rebuilds, int MaterializedRows);
public sealed record GridHorizontalScrollMetrics(double ExtentWidth, double ViewportWidth, double Maximum, double OffsetX);

/// <summary>Metadata-driven table adapter. All data and editing behavior remains in the Avalonia-free runtime.</summary>
public sealed class DataEntryGridHost : UserControl
{
    private readonly EditorResolver editorResolver = new();
    private readonly DataEntryGridRuntime runtime;
    private readonly ILocalizationService localization;
    private readonly AppearancePreferenceService? appearance;
    private readonly IGridClipboardService clipboard;
    private readonly IPrivacyPolicyResolver privacyResolver;
    private readonly IPrivacyStateService privacyState;
    private readonly ISensitiveValuePresenter sensitivePresenter;
    private readonly IMessageService messages;
    private readonly TextBlock stateText = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly ScrollViewer scroller = new() { HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden };
    private readonly ScrollViewer columnHeaderScroller = new()
    {
        HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
        VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
    };
    private readonly ScrollViewer rowHeaderScroller = new()
    {
        HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
    };
    private readonly global::Avalonia.Controls.Primitives.ScrollBar logicalVerticalScrollbar = new()
    {
        Orientation = Orientation.Vertical,
    };
    private bool updatingLogicalScrollbar;
    private EffectiveAuthorizationContext? authorization;
    private GridProviderContext? context;
    private TextBox? activeEditor;
    private CancellationTokenSource? viewportNavigation;
    private CancellationTokenSource? viewportPrefetch;
    private CancellationTokenSource? viewportResize;
    private DateTimeOffset scrollRequestsEnabledAt;
    private bool pointerSelecting;
    private GridRangeEndpoint pointerAnchor;
    private ImmutableArray<GridCellRange> pointerRetainedRanges = [];
    private readonly List<(GridCellAddress Address, int Position, Border Presenter, TextBlock? ValuePresenter)> cellPresenters = [];
    private readonly List<(RowKey RowKey, Border DataRow, Border RowHeader)> rowPresenters = [];
    private Button? gridActionsButton;
    private int rebuildCount;
    private bool activationInProgress;
    private DateTimeOffset suppressViewportResizeUntil;
    private bool showingLoadingShell;
    private readonly TextBlock activeValue = new() { TextWrapping = TextWrapping.Wrap, MaxHeight = 120,
        Margin = new Thickness(10, 7), IsVisible = false };
    private readonly Flyout expandedCell = new() { Placement = PlacementMode.BottomEdgeAlignedLeft };
    private readonly TextBox expandedValue = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
        MinHeight = 96 };
    private bool expandedEditing;
    private bool expandedClosing;
    private bool placeExpandedCaretAtEnd;
    private readonly TextBox findQuery = new() { Watermark = "Find", MinWidth = 180 };
    private readonly Button findScope = new() { MinWidth = 170 };
    private readonly TextBlock findStatus = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly Border findSurface = new() { IsVisible = false };
    private VariableCode? findColumn;
    private RowKey? findRow;
    private GridFindScope activeFindScope = GridFindScope.AllVisibleColumns;

    public DataEntryGridHost(DataEntryGridRuntime runtime, ILocalizationService localization,
        AppearancePreferenceService? appearance = null, IGridClipboardService? clipboard = null,
        IPrivacyPolicyResolver? privacyResolver = null, IPrivacyStateService? privacyState = null,
        ISensitiveValuePresenter? sensitivePresenter = null, IMessageService? messages = null)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.appearance = appearance;
        this.clipboard = clipboard ?? new AvaloniaGridClipboardService(this);
        this.privacyResolver = privacyResolver ?? new PrivacyPolicyResolver();
        this.privacyState = privacyState ?? new PrivacyStateService();
        this.sensitivePresenter = sensitivePresenter ?? new SensitiveValuePresenter();
        this.messages = messages ?? new AvaloniaMessageService(() => TopLevel.GetTopLevel(this) as Window);
        ValidateThemeGeometry();
        ApplyScrollbarRecipe(scroller);
        ApplyScrollbarRecipe(logicalVerticalScrollbar);
        MinWidth = ResourceNumber("DuiGridMinimumViewportWidth", 320);
        MinHeight = ResourceNumber("DuiGridMinimumViewportHeight", 180);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        ClipToBounds = true;
        Focusable = true;
        runtime.Changed += (_, args) => Dispatcher.UIThread.Post(() =>
        {
            if (args.Reason == "ROWS_DELETED")
            {
                expandedEditing = false; expandedClosing = true; expandedCell.Hide(); expandedClosing = false;
                Rebuild();
            }
            else if (args.Reason is "CELL_SELECTION" or "CELL_SELECT_ALL" or "CELL_SELECTION_CLEAR")
            {
                UpdateSelectionPresentation();
            }
            else if (args.Reason == "ROW_HEIGHT_PERCENTAGE") UpdateMaterializedRowHeights();
            else if (args.Reason == "VIEWPORT_LOADING" && showingLoadingShell) { }
            else if (showingLoadingShell && args.Reason is "AUTHORIZATION" or "VIEW_PREFERENCE") { }
            else if (expandedEditing && args.Reason == "EDIT_COMMIT" && args.Cell is { } committedCell)
            {
                RefreshMaterializedCell(committedCell);
                UpdateSelectionPresentation();
            }
            else if (expandedEditing && args.Reason is "EDIT_BEGIN" or "EDIT_CANDIDATE" or "EDIT_CANCEL") { }
            else if (args.Reason != "EDIT_CANDIDATE" || activeEditor is null) Rebuild();
            Changed?.Invoke(this, EventArgs.Empty);
        });
        this.privacyState.StateChanged += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            if (!showingLoadingShell) Rebuild();
        });
        localization.CultureChanged += (_, _) => Rebuild();
        if (appearance is not null) appearance.PreferencesChanged += (_, _) =>
        {
            ApplyScrollbarRecipe(scroller);
            ApplyScrollbarRecipe(logicalVerticalScrollbar);
            Rebuild();
            QueueViewportResize();
        };
        scroller.ScrollChanged += (_, _) =>
        {
            if (Math.Abs(columnHeaderScroller.Offset.X - scroller.Offset.X) > .1)
                columnHeaderScroller.Offset = new Vector(scroller.Offset.X, 0);
            if (Math.Abs(rowHeaderScroller.Offset.Y - scroller.Offset.Y) > .1)
                rowHeaderScroller.Offset = new Vector(0, scroller.Offset.Y);
            QueueNextViewportFromScroll();
        };
        scroller.PointerWheelChanged += (_, args) =>
        {
            // Leave horizontal-dominant trackpad gestures to ScrollViewer's authoritative X offset.
            if (!runtime.IsVirtualized || Math.Abs(args.Delta.Y) < .01 || Math.Abs(args.Delta.X) >= Math.Abs(args.Delta.Y)) return;
            var step = Math.Max(1, runtime.RequestedViewportRowCount / 8);
            NavigateLogicalPosition((int)Math.Round(logicalVerticalScrollbar.Value) -
                (int)Math.Sign(args.Delta.Y) * step);
            args.Handled = true;
        };
        logicalVerticalScrollbar.ValueChanged += (_, _) =>
        {
            if (!updatingLogicalScrollbar)
                NavigateLogicalPosition((int)Math.Round(logicalVerticalScrollbar.Value));
        };
        SizeChanged += (_, _) =>
        {
            if (!activationInProgress && DateTimeOffset.UtcNow >= suppressViewportResizeUntil) QueueViewportResize();
        };
        AddHandler(KeyDownEvent, HandleKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        TextInput += HandleTextInput;
        PointerMoved += (_, args) => ContinuePointerSelection(args.GetPosition(this));
        PointerReleased += (_, _) => pointerSelecting = false;
        PointerCaptureLost += (_, _) => pointerSelecting = false;
        expandedCell.Content = expandedValue;
        InputMethod.SetIsInputMethodEnabled(expandedValue, true);
        ScrollViewer.SetVerticalScrollBarVisibility(expandedValue,
            global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(expandedValue,
            global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);
        expandedValue.KeyDown += async (_, args) =>
        {
            if (args.Key == Key.Escape) { CancelExpandedEdit(); args.Handled = true; }
            else if (expandedEditing && args.Key is Key.Enter or Key.Tab)
            {
                await CommitExpandedEditAsync();
                args.Handled = true;
            }
        };
        expandedCell.Closed += (_, _) => { if (!expandedClosing) CancelExpandedEdit(); };
        expandedCell.Opened += (_, _) =>
        {
            expandedValue.Focus();
            if (expandedEditing)
            {
                var caret = placeExpandedCaretAtEnd ? expandedValue.Text?.Length ?? 0 : 0;
                expandedValue.SelectionStart = caret;
                expandedValue.SelectionEnd = caret;
            }
        };
        findSurface.Child = BuildFindSurface();
        Rebuild();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var availableHeight = ResolveAvailableWorkspaceHeight(availableSize.Height);
        var targetHeight = ResolveConfiguredHeight(availableHeight);
        var measured = base.MeasureOverride(new Size(availableSize.Width, targetHeight));
        return new Size(measured.Width, targetHeight);
    }

    public DataEntryGridRuntime Runtime => runtime;
    public int RenderedColumnCount => runtime.PresentedColumns.Length;
    public int RenderedRowCount => runtime.Rows.Length;
    public bool HasActiveEditor => activeEditor is not null;
    public int RebuildCount => rebuildCount;
    public GridActivationStage ActivationStage { get; private set; } = GridActivationStage.WorkspaceVisible;
    public GridActivationTiming? LastActivationTiming { get; private set; }
    public GridHorizontalScrollMetrics HorizontalScrollMetrics => new(scroller.Extent.Width, scroller.Viewport.Width,
        Math.Max(0, scroller.Extent.Width - scroller.Viewport.Width), scroller.Offset.X);
    public double ColumnHeaderHorizontalOffset => columnHeaderScroller.Offset.X;
    public double GridViewportHeight => scroller.Bounds.Height;
    public EditorResolution? LastEditorResolution { get; private set; }
    public event EventHandler? Changed;

    public bool ScrollHorizontallyTo(double offset)
    {
        var maximum = Math.Max(0, scroller.Extent.Width - scroller.Viewport.Width);
        if (maximum <= 0) return false;
        var value = Math.Clamp(offset, 0, maximum);
        scroller.Offset = new Vector(value, scroller.Offset.Y);
        columnHeaderScroller.Offset = new Vector(value, 0);
        return Math.Abs(scroller.Offset.X - value) < .1 && Math.Abs(columnHeaderScroller.Offset.X - value) < .1;
    }

    public async Task LoadAsync(GridProviderContext providerContext, EffectiveAuthorizationContext? effectiveAuthorization,
        CancellationToken cancellationToken = default)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var rebuildsBefore = rebuildCount;
        var requestsBefore = runtime.ProviderRequestCount;
        activationInProgress = true;
        ActivationStage = GridActivationStage.WorkspaceVisible;
        CancelViewportResize();
        expandedEditing = false; expandedClosing = true; expandedCell.Hide(); expandedClosing = false;
        context = providerContext;
        authorization = effectiveAuthorization;
        if (VisualRoot is not null)
        {
            var layoutReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() => layoutReady.TrySetResult(), DispatcherPriority.Render);
            await Task.WhenAny(layoutReady.Task, Task.Delay(16, cancellationToken));
        }
        var workspaceVisible = clock.Elapsed;
        var scale = appearance?.Current.UiScale ?? 1d;
        var viewportHeight = EffectiveViewportHeight();
        var initialRows = viewportHeight > 0 ? Math.Clamp((int)Math.Ceiling(viewportHeight / DensityHeight(scale)) + 4,
            Math.Min(20, ViewportRowLimit()), ViewportRowLimit()) : (int?)null;
        await runtime.LoadAsync(providerContext, effectiveAuthorization, cancellationToken, initialRows).ConfigureAwait(false);
        ActivationStage = GridActivationStage.DataAvailable;
        var dataAvailable = clock.Elapsed;
        if (VisualRoot is not null)
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render, cancellationToken);
        ActivationStage = GridActivationStage.InteractiveReady;
        var interactiveReady = clock.Elapsed;
        clock.Stop();
        suppressViewportResizeUntil = DateTimeOffset.UtcNow.AddMilliseconds(500);
        activationInProgress = false;
        LastActivationTiming = new(workspaceVisible, dataAvailable, interactiveReady, interactiveReady,
            runtime.ProviderRequestCount - requestsBefore, rebuildCount - rebuildsBefore, runtime.Rows.Length);
    }

    public void UpdateAuthorization(EffectiveAuthorizationContext? effectiveAuthorization)
    {
        authorization = effectiveAuthorization;
        runtime.UpdateAuthorization(effectiveAuthorization);
    }

    public bool BeginEdit(RowKey rowKey, VariableCode variableCode)
    {
        var result = runtime.BeginEdit(rowKey, variableCode);
        if (result) Rebuild();
        return result;
    }

    public GridValidationDiagnostic? SetCandidate(object? candidate)
    {
        var result = runtime.SetCandidate(candidate);
        Rebuild();
        return result;
    }

    public async Task<GridCommitResult> CommitEditAsync(CancellationToken cancellationToken = default)
    {
        if (activeEditor is not null) runtime.SetCandidate(activeEditor.Text);
        var result = await runtime.CommitEditAsync(cancellationToken);
        Rebuild();
        return result;
    }

    public void CancelEdit() { runtime.CancelEdit(); Rebuild(); }
    public Task<GridPasteResult> CopySelectionAsync(CancellationToken cancellationToken = default) =>
        runtime.CopyAsync(clipboard, cancellationToken: cancellationToken);
    public Task<GridPasteResult> CutSelectionAsync(CancellationToken cancellationToken = default) =>
        runtime.CutAsync(clipboard, cancellationToken: cancellationToken);
    public Task<GridPasteResult> PasteSelectionAsync(CancellationToken cancellationToken = default) =>
        runtime.PasteAsync(clipboard, cancellationToken: cancellationToken);
    public Task<GridPasteResult> ClearSelectionAsync(CancellationToken cancellationToken = default) =>
        runtime.ClearSelectedCellsAsync(cancellationToken: cancellationToken);
    public Task<GridPasteResult> UndoAsync(CancellationToken cancellationToken = default) => runtime.UndoAsync(cancellationToken);
    public Task<GridPasteResult> RedoAsync(CancellationToken cancellationToken = default) => runtime.RedoAsync(cancellationToken);
    public Task<GridPasteResult> FillDownAsync(CancellationToken cancellationToken = default) => runtime.FillDownAsync(cancellationToken);
    public Task<GridPasteResult> FillRightAsync(CancellationToken cancellationToken = default) => runtime.FillRightAsync(cancellationToken);
    public bool ReorderColumn(VariableCode variableCode, int visibleIndex) => runtime.ReorderColumn(variableCode, visibleIndex);
    public bool SetColumnVisible(VariableCode variableCode, bool visible) => runtime.SetColumnVisible(variableCode, visible);
    public bool SetColumnPinned(VariableCode variableCode, bool pinned) => runtime.SetColumnPin(variableCode,
        pinned ? GridColumnPin.Left : GridColumnPin.None, (decimal)Math.Max(240, Bounds.Width * .55));
    public bool PrepareCellContext(GridCellAddress address, int logicalRowPosition)
    {
        if (runtime.IsCellSelected(address)) return false;
        return runtime.SelectCell(address, logicalRowPosition);
    }
    public void ResetLayout() => runtime.ResetView();
    public Task SortAsync(VariableCode variableCode, GridSortDirection direction, CancellationToken cancellationToken = default)
    {
        CancelViewportResize(); return runtime.SetSortAsync([new(variableCode, direction)], authorization, cancellationToken);
    }
    public Task FilterAsync(GridFilterDefinition filter, CancellationToken cancellationToken = default)
    {
        CancelViewportResize(); return runtime.SetFiltersAsync([filter], authorization, cancellationToken);
    }
    public Task ClearFilterAsync(CancellationToken cancellationToken = default)
    {
        CancelViewportResize(); return runtime.SetFiltersAsync([], authorization, cancellationToken);
    }
    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        CancelViewportResize(); return runtime.RefreshAsync(authorization, cancellationToken);
    }
    public Task RequestViewportAsync(int startIndex, CancellationToken cancellationToken = default)
    {
        CancelViewportResize();
        scrollRequestsEnabledAt = DateTimeOffset.UtcNow.AddMilliseconds(750);
        return runtime.RequestViewportAsync(startIndex, runtime.RequestedViewportRowCount > 0
            ? runtime.RequestedViewportRowCount : runtime.ViewportOptions.VisibleRowCount, cancellationToken);
    }
    public Task RetryAsync(CancellationToken cancellationToken = default) => runtime.RetryViewportAsync(cancellationToken);
    public void Deactivate() => runtime.Deactivate();

    private void Rebuild()
    {
        rebuildCount++;
        activeEditor = null;
        cellPresenters.Clear();
        rowPresenters.Clear();
        if (activeValue.Parent is Panel previousDetailPanel) previousDetailPanel.Children.Remove(activeValue);
        else if (activeValue.Parent is Border previousDetail) previousDetail.Child = null;
        if (stateText.Parent is Panel statePanel) statePanel.Children.Remove(stateText);
        else if (stateText.Parent is Border stateBorder) stateBorder.Child = null;
        else if (ReferenceEquals(Content, stateText)) Content = null;
        if (scroller.Parent is Panel previousPanel) previousPanel.Children.Remove(scroller);
        else if (ReferenceEquals(Content, scroller)) Content = null;
        if (columnHeaderScroller.Parent is Panel previousColumnHeaderPanel) previousColumnHeaderPanel.Children.Remove(columnHeaderScroller);
        if (rowHeaderScroller.Parent is Panel previousHeaderPanel) previousHeaderPanel.Children.Remove(rowHeaderScroller);
        if (logicalVerticalScrollbar.Parent is Panel previousLogicalScrollbarPanel)
            previousLogicalScrollbarPanel.Children.Remove(logicalVerticalScrollbar);
        var loadingShell = runtime.State == GridProviderState.Loading && runtime.Rows.Length == 0;
        if (loadingShell)
        {
            showingLoadingShell = true;
            Content = new Border { Child = BuildLoadingShell(), Margin = ResolveOuterInset(appearance?.Current.UiScale ?? 1d) };
            return;
        }
        showingLoadingShell = false;
        if (runtime.State != GridProviderState.Ready && runtime.Rows.Length == 0)
        {
            stateText.Text = runtime.State switch
            {
                GridProviderState.Loading => localization.Get(new("Grid.State.Loading")),
                GridProviderState.Empty when runtime.Filters.Length > 0 => "No rows match current filters",
                GridProviderState.Empty => localization.Get(runtime.Definition.EmptyStateKey),
                GridProviderState.Error => localization.Get(new("Grid.State.Error")),
                GridProviderState.Unavailable => localization.Get(new("Grid.State.Unavailable")),
                _ => localization.Get(new("Grid.State.Empty")),
            };
            AutomationProperties.SetName(stateText, stateText.Text);
            if (runtime.State == GridProviderState.Empty && runtime.Filters.Length > 0)
            {
                var clear = new Button { Content = "Clear filters", HorizontalAlignment = HorizontalAlignment.Center };
                clear.Click += async (_, _) => await ClearFilterAsync();
                Content = new StackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center, Children = { stateText, clear } };
            }
            else Content = stateText;
            return;
        }

        var columns = runtime.PresentedColumns.Select(x => x.Column with { Width = x.Width }).ToArray();
        var scale = appearance?.Current.UiScale ?? 1d;
        var stack = new StackPanel { Spacing = 0 };
        var rowHeaders = new StackPanel { Spacing = 0 };
        for (var index = 0; index < runtime.Rows.Length; index++)
        {
            var dataRow = (Border)BuildRow(runtime.Rows[index], index, columns, scale);
            var rowHeader = (Border)BuildRowHeader(runtime.Rows[index].RowKey,
                runtime.ViewportStartIndex + index + 1, scale);
            stack.Children.Add(dataRow); rowHeaders.Children.Add(rowHeader);
            rowPresenters.Add((runtime.Rows[index].RowKey, dataRow, rowHeader));
        }
        scroller.Content = stack;
        columnHeaderScroller.Content = BuildHeader(columns, scale);
        rowHeaderScroller.Content = rowHeaders;
        var logicalOffset = Math.Max(0, runtime.RequestedViewportStartIndex - runtime.ViewportStartIndex) * DensityHeight(scale);
        scroller.Offset = new Vector(scroller.Offset.X, logicalOffset);
        rowHeaderScroller.Offset = new Vector(0, logicalOffset);
        var rebuiltGuard = DateTimeOffset.UtcNow.AddMilliseconds(250);
        if (scrollRequestsEnabledAt < rebuiltGuard) scrollRequestsEnabledAt = rebuiltGuard;
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        var topControls = new StackPanel { Spacing = 0 };
        var bottomControls = new StackPanel { Spacing = 0 };
        var detailContent = new StackPanel { Children = { activeValue } };
        var detail = new Border { Child = detailContent, BorderThickness = new Thickness(1, 0, 0, 0) };
        detail.Bind(Border.BorderBrushProperty, detail.GetResourceObservable("DuiBorderBrush"));
        bottomControls.Children.Add(detail);
        var sizing = BuildSizingBar();
        topControls.Children.Add(sizing);
        if (findSurface.Parent is Panel oldFindParent) oldFindParent.Children.Remove(findSurface);
        topControls.Children.Add(findSurface);
        if (runtime.IsVirtualized)
        {
            var navigation = BuildViewportNavigation(scale);
            var navigationAtBottom = runtime.Definition.Presentation.NavigationPlacement == GridNavigationPlacement.Bottom ||
                runtime.Definition.Presentation.NavigationPlacement == GridNavigationPlacement.Auto && Bounds.Width < 640;
            (navigationAtBottom ? bottomControls : topControls).Children.Add(navigation);
        }
        var gridSurface = new Grid
        {
            ColumnDefinitions = runtime.ShowRowNumbers ? new ColumnDefinitions("Auto,*,Auto") : new ColumnDefinitions("0,*,Auto"),
            RowDefinitions = new RowDefinitions("Auto,*"),
        };
        rowHeaderScroller.IsVisible = runtime.ShowRowNumbers;
        var corner = BuildRowHeaderHeading(scale); corner.IsVisible = runtime.ShowRowNumbers;
        Grid.SetColumn(corner, 0); Grid.SetRow(corner, 0);
        Grid.SetColumn(columnHeaderScroller, 1); Grid.SetRow(columnHeaderScroller, 0);
        Grid.SetColumn(rowHeaderScroller, 0); Grid.SetRow(rowHeaderScroller, 1);
        Grid.SetColumn(scroller, 1); Grid.SetRow(scroller, 1);
        updatingLogicalScrollbar = true;
        logicalVerticalScrollbar.Minimum = 0;
        logicalVerticalScrollbar.Maximum = Math.Max(0, runtime.TotalRows - Math.Max(1, runtime.RequestedViewportRowCount));
        logicalVerticalScrollbar.ViewportSize = Math.Max(1, runtime.RequestedViewportRowCount);
        logicalVerticalScrollbar.SmallChange = Math.Max(1, runtime.RequestedViewportRowCount / 8);
        logicalVerticalScrollbar.LargeChange = Math.Max(1, runtime.RequestedViewportRowCount);
        logicalVerticalScrollbar.Value = Math.Min(logicalVerticalScrollbar.Maximum, runtime.RequestedViewportStartIndex);
        logicalVerticalScrollbar.MinWidth = ResourceNumber(GridThemeResourceKeys.ScrollbarHitTarget, 14) * scale;
        updatingLogicalScrollbar = false;
        Grid.SetColumn(logicalVerticalScrollbar, 2); Grid.SetRow(logicalVerticalScrollbar, 1);
        gridSurface.Children.Add(corner); gridSurface.Children.Add(columnHeaderScroller);
        gridSurface.Children.Add(rowHeaderScroller); gridSurface.Children.Add(scroller);
        gridSurface.Children.Add(logicalVerticalScrollbar);
        var scrollbarClearance = RoleNumber("DuiGridScrollbarClearance",
            runtime.Definition.Presentation.ScrollbarClearance, 2) * scale;
        var frame = new Border { Child = gridSurface, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, scrollbarClearance, scrollbarClearance) };
        frame.Bind(Border.BorderBrushProperty, frame.GetResourceObservable("DuiBorderBrush"));
        Grid.SetRow(topControls, 0); Grid.SetRow(frame, 1); Grid.SetRow(bottomControls, 2);
        layout.Children.Add(topControls); layout.Children.Add(frame); layout.Children.Add(bottomControls);
        Content = new Border { Child = layout, Margin = ResolveOuterInset(scale) };
        UpdateSelectionPresentation();
    }

    private Control BuildLoadingShell()
    {
        stateText.Text = localization.Get(new("Grid.State.Loading"));
        AutomationProperties.SetName(stateText, stateText.Text);
        var toolbar = new Border { Height = ResourceNumber("DuiControlHeightStandard", 34),
            Margin = ResourceThickness("DuiGridActionsMargin", new Thickness(0, 0, 0, 2)) };
        toolbar.Bind(Border.BackgroundProperty, toolbar.GetResourceObservable("DuiGridRowHeaderBrush"));
        var frame = new Border { Child = stateText, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4) };
        frame.Bind(Border.BorderBrushProperty, frame.GetResourceObservable("DuiBorderBrush"));
        var shell = new DockPanel(); DockPanel.SetDock(toolbar, Dock.Top); shell.Children.Add(toolbar); shell.Children.Add(frame);
        return shell;
    }

    private Control BuildHeader(IReadOnlyList<ResolvedGridColumn> columns, double scale)
    {
        var grid = CreateColumns(columns, scale);
        grid.MinHeight = RoleNumber("DuiGridHeader", runtime.Definition.Presentation.HeaderHeight, 38) * scale;
        const int columnOffset = 0;
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var button = new Button
            {
                Content = localization.Get(new(column.Definition.DisplayNameKey)),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Padding = new Thickness(8 * scale, 4 * scale),
            };
            AutomationProperties.SetName(button, localization.Get(new(column.Definition.DisplayNameKey)));
            AutomationProperties.SetHelpText(button, HeaderState(column.Definition.VariableCode));
            button.Click += async (_, _) =>
            {
                var current = runtime.Sorts.FirstOrDefault(x => x.VariableCode == column.Definition.VariableCode);
                var direction = current?.Direction == GridSortDirection.Ascending ? GridSortDirection.Descending : GridSortDirection.Ascending;
                await SortAsync(column.Definition.VariableCode, direction);
            };
            var code = column.Definition.VariableCode;
            var header = new Grid(); header.Children.Add(button);
            var dropdown = new Button { Content = "⌄", Width = 28 * scale, Padding = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Stretch,
                ContextMenu = BuildHeaderMenu(code) };
            AutomationProperties.SetName(dropdown, $"{localization.Get(new(column.Definition.DisplayNameKey))} column menu");
            dropdown.Click += (_, _) => dropdown.ContextMenu?.Open(dropdown);
            header.Children.Add(dropdown);
            var separator = new Border { Child = header, BorderThickness = new Thickness(0, 0, 1, 1) };
            separator.Bind(Border.BorderBrushProperty, separator.GetResourceObservable("DuiBorderBrush"));
            Grid.SetColumn(separator, index + columnOffset); grid.Children.Add(separator);
        }
        return grid;
    }

    private Control BuildRow(GridRow row, int localIndex, IReadOnlyList<ResolvedGridColumn> columns, double scale)
    {
        var rowGrid = CreateColumns(columns, scale);
        var rowHeight = (double)runtime.ResolveRowHeight(row.RowKey, (decimal)(DensityHeight(scale) / scale)) * scale;
        rowGrid.MinHeight = rowHeight;
        const int columnOffset = 0;
        for (var index = 0; index < columns.Count; index++)
        {
            var cell = BuildCell(row, runtime.ViewportStartIndex + localIndex, columns[index], scale);
            Grid.SetColumn(cell, index + columnOffset); rowGrid.Children.Add(cell);
        }
        var border = new Border { Child = rowGrid, Height = rowHeight, BorderThickness = new Thickness(0, 0, 0, 1), Focusable = true,
            Tag = row.RowKey };
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("DuiBorderBrush"));
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable(
            runtime.SelectedRowKeys.Contains(row.RowKey) ? "DuiSelectionBrush" : "DuiSurfaceBrush"));
        border.PointerPressed += (_, args) =>
        {
            if (runtime.Definition.SelectionMode == GridSelectionMode.Multiple && args.KeyModifiers.HasFlag(KeyModifiers.Control))
                runtime.ToggleSelection(row.RowKey);
            else runtime.Select([row.RowKey]);
            border.Focus();
        };
        border.DoubleTapped += (_, _) =>
        {
            var editable = columns.FirstOrDefault(x => runtime.CanEdit(row.RowKey, x.Definition.VariableCode));
            if (editable is not null) BeginEdit(row.RowKey, editable.Definition.VariableCode);
        };
        border.ContextMenu = BuildRowMenu(row.RowKey);
        AutomationProperties.SetName(border, $"{localization.Get(new("Grid.Row"))} {row.RowKey}");
        return border;
    }

    private Control BuildCell(GridRow row, int logicalRowPosition, ResolvedGridColumn column, double scale)
    {
        var isEditing = runtime.EditBuffer is { } edit && edit.RowKey == row.RowKey && edit.VariableCode == column.Definition.VariableCode;
        Control content;
        TextBlock? valuePresenter = null;
        if (isEditing)
        {
            LastEditorResolution = editorResolver.Resolve(
                GridEditorDefinitionAdapter.Create(runtime.Definition.GridCode, column.Definition),
                EditorPlatformCapabilities.AllNative);
            var editor = new TextBox { Text = runtime.EditBuffer!.CandidateValue?.ToString(), Margin = new Thickness(2),
                Watermark = column.Definition.IsRequired ? localization.Get(new("Grid.Required")) : null };
            activeEditor = editor;
            editor.TextChanged += (_, _) => runtime.SetCandidate(editor.Text);
            editor.KeyDown += async (_, args) =>
            {
                if (args.Key == Key.Enter) { await CommitEditAsync(); args.Handled = true; }
                else if (args.Key == Key.Escape) { CancelEdit(); args.Handled = true; }
            };
            Dispatcher.UIThread.Post(() => { if (ReferenceEquals(activeEditor, editor)) { editor.Focus(); editor.SelectAll(); } });
            content = editor;
        }
        else
        {
            var value = runtime.GetValue(row.RowKey, column.Definition.VariableCode, out var diagnostic);
            var resolution = privacyResolver.Resolve(new(true, column.Definition.SensitiveContent, privacyState.RequestedMode,
                CompanyId: context?.Company.CompanyId, WorkspaceId: context?.WorkspaceId,
                IsTemporarilyRevealed: privacyState.IsRevealed($"{row.RowKey}:{column.Definition.VariableCode}", privacyState.Generation),
                Generation: privacyState.Generation));
            var safe = sensitivePresenter.Present(value, column.Definition.SensitiveContent, resolution, CultureInfo.CurrentCulture);
            var text = new TextBlock { Text = diagnostic is null ? safe.DisplayValue : "⚠ —", IsHitTestVisible = false,
                TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center };
            valuePresenter = text;
            AutomationProperties.SetName(text, safe.AccessibleValue);
            if (column.IsFormulaDerived)
            {
                var marker = new TextBlock { Text = "fx", FontStyle = FontStyle.Italic, FontWeight = FontWeight.SemiBold,
                    Opacity = .68, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false };
                AutomationProperties.SetName(marker, localization.Get(new("Grid.CalculatedValue")));
                content = new StackPanel { Orientation = Orientation.Horizontal, IsHitTestVisible = false,
                    Children = { marker, text } };
            }
            else content = text;
        }
        var address = new GridCellAddress(row.RowKey, column.Definition.VariableCode);
        var isActive = runtime.ActiveCell == address;
        var isSelected = runtime.IsCellSelected(address);
        var border = new Border { Child = content, Padding = new Thickness(8 * scale, 3 * scale), Focusable = true,
            BorderThickness = isActive ? new Thickness(2) : new Thickness(0, 0, 1, 0), Tag = column.Definition.VariableCode };
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable(isActive ? "DuiFocusBrush" : "DuiBorderBrush"));
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable(isSelected ? "DuiSelectionBrush" :
            column.CanEdit && runtime.ResolvedDefinition.CanEdit ? "DuiSurfaceBrush" : "DuiSurfaceRaisedBrush"));
        var mode = column.CanEdit && runtime.ResolvedDefinition.CanEdit ? localization.Get(new("Grid.Editable")) : localization.Get(new("Grid.ReadOnly"));
        AutomationProperties.SetName(border, $"{localization.Get(new(column.Definition.DisplayNameKey))}, {mode}" +
            (column.IsFormulaDerived ? $", {localization.Get(new("Grid.CalculatedValue"))}" : string.Empty));
        border.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(border).Properties.IsLeftButtonPressed) return;
            var additive = HasPrimaryModifier(args.KeyModifiers);
            var endpoint = new GridRangeEndpoint(address, logicalRowPosition);
            pointerAnchor = args.KeyModifiers.HasFlag(KeyModifiers.Shift) && runtime.AnchorCell is { } anchor ? anchor : endpoint;
            pointerRetainedRanges = additive ? runtime.SelectedRanges : [];
            pointerSelecting = true;
            runtime.SelectRange(pointerAnchor, endpoint, pointerRetainedRanges);
            border.Focus();
            args.Handled = true;
        };
        border.PointerEntered += (_, _) =>
        {
            if (pointerSelecting) runtime.SelectRange(pointerAnchor,
                new GridRangeEndpoint(address, logicalRowPosition), pointerRetainedRanges);
        };
        border.PointerReleased += (_, _) => pointerSelecting = false;
        border.DoubleTapped += (_, args) =>
        {
            pointerSelecting = false;
            OpenExpandedCell(border, address, column);
            args.Handled = true;
        };
        border.ContextMenu = BuildCellMenu(border, address, logicalRowPosition, column);
        cellPresenters.Add((address, logicalRowPosition, border, valuePresenter));
        return border;
    }

    private void RefreshMaterializedCell(GridCellAddress address)
    {
        var materialized = cellPresenters.FirstOrDefault(x => x.Address == address);
        if (materialized.ValuePresenter is null || context is null) return;
        var column = runtime.ResolvedDefinition.Columns.FirstOrDefault(x => x.Definition.VariableCode == address.VariableCode);
        if (column is null) return;
        var value = runtime.GetValue(address.RowKey, address.VariableCode, out var diagnostic);
        var resolution = privacyResolver.Resolve(new(true, column.Definition.SensitiveContent, privacyState.RequestedMode,
            CompanyId: context.Company.CompanyId, WorkspaceId: context.WorkspaceId,
            IsTemporarilyRevealed: privacyState.IsRevealed($"{address.RowKey}:{address.VariableCode}", privacyState.Generation),
            Generation: privacyState.Generation));
        var safe = sensitivePresenter.Present(value, column.Definition.SensitiveContent, resolution, CultureInfo.CurrentCulture);
        materialized.ValuePresenter.Text = diagnostic is null ? safe.DisplayValue : "⚠ —";
        AutomationProperties.SetName(materialized.ValuePresenter, safe.AccessibleValue);
    }

    private void ContinuePointerSelection(Point point)
    {
        if (!pointerSelecting) return;
        foreach (var cell in cellPresenters)
        {
            var origin = cell.Presenter.TranslatePoint(default, this);
            if (origin is null || !new Rect(origin.Value, cell.Presenter.Bounds.Size).Contains(point)) continue;
            runtime.SelectRange(pointerAnchor, new(cell.Address, cell.Position), pointerRetainedRanges);
            return;
        }
    }

    private void UpdateSelectionPresentation()
    {
        foreach (var cell in cellPresenters)
        {
            var active = runtime.ActiveCell == cell.Address;
            cell.Presenter.BorderThickness = active ? new Thickness(2) : new Thickness(0, 0, 1, 0);
            cell.Presenter.Bind(Border.BorderBrushProperty, cell.Presenter.GetResourceObservable(
                active ? "DuiFocusBrush" : "DuiBorderBrush"));
            var presentedColumn = runtime.ResolvedDefinition.Columns.First(x => x.Definition.VariableCode == cell.Address.VariableCode);
            var background = runtime.IsCellSelected(cell.Address) ? "DuiSelectionBrush" :
                presentedColumn.CanEdit && runtime.ResolvedDefinition.CanEdit ? "DuiSurfaceBrush" : "DuiSurfaceRaisedBrush";
            cell.Presenter.Bind(Border.BackgroundProperty, cell.Presenter.GetResourceObservable(background));
        }
        activeValue.IsVisible = false;
        if (runtime.ActiveCell is not { } address || context is null) return;
        var column = runtime.ResolvedDefinition.Columns.FirstOrDefault(x => x.Definition.VariableCode == address.VariableCode);
        if (column is null) return;
        var value = runtime.GetValue(address.RowKey, address.VariableCode, out var diagnostic);
        var resolution = privacyResolver.Resolve(new(true, column.Definition.SensitiveContent, privacyState.RequestedMode,
            CompanyId: context.Company.CompanyId, WorkspaceId: context.WorkspaceId,
            IsTemporarilyRevealed: privacyState.IsRevealed($"{address.RowKey}:{address.VariableCode}", privacyState.Generation),
            Generation: privacyState.Generation));
        var safe = sensitivePresenter.Present(value, column.Definition.SensitiveContent, resolution, CultureInfo.CurrentCulture);
        activeValue.Text = diagnostic is null ? safe.DisplayValue : "⚠ —";
        AutomationProperties.SetName(activeValue, safe.AccessibleValue);
        activeValue.IsVisible = true;
    }

    private void OpenExpandedCell(Control anchor, GridCellAddress address, ResolvedGridColumn column,
        bool printableReplacement = false)
    {
        if (pointerSelecting || expandedCell.IsOpen) return;
        if (runtime.ActiveCell != address)
        {
            var localIndex = Array.FindIndex(runtime.Rows.ToArray(), x => x.RowKey == address.RowKey);
            if (localIndex < 0 || !runtime.SelectCell(address, runtime.ViewportStartIndex + localIndex)) return;
            UpdateSelectionPresentation();
        }
        var privacy = privacyResolver.Resolve(new(true, column.Definition.SensitiveContent, privacyState.RequestedMode,
            CompanyId: context?.Company.CompanyId, WorkspaceId: context?.WorkspaceId,
            IsTemporarilyRevealed: privacyState.IsRevealed($"{address.RowKey}:{address.VariableCode}", privacyState.Generation),
            Generation: privacyState.Generation));
        var editable = privacy.Presentation == PrivacyPresentation.None && runtime.CanEdit(address.RowKey, address.VariableCode);
        var existingValue = runtime.GetValue(address.RowKey, address.VariableCode, out _);
        var projected = sensitivePresenter.Present(existingValue, column.Definition.SensitiveContent, privacy,
            CultureInfo.CurrentCulture);
        expandedEditing = editable;
        if (editable && !runtime.BeginEdit(address.RowKey, address.VariableCode)) expandedEditing = false;
        var editValue = runtime.EditBuffer is { RowKey: var editRow, VariableCode: var editVariable } buffer &&
            editRow == address.RowKey && editVariable == address.VariableCode
            ? buffer.CandidateValue : existingValue;
        placeExpandedCaretAtEnd = printableReplacement;
        expandedValue.Text = expandedEditing ? editValue?.ToString() ?? string.Empty : projected.DisplayValue;
        expandedValue.IsReadOnly = !expandedEditing;
        expandedValue.AcceptsReturn = column.Definition.DataType == ColumnDataType.MultilineText;
        var expandedSize = DataEntryExpandedCellLayout.Resolve(expandedValue.Text, anchor.Bounds.Size, Bounds.Size);
        expandedValue.Width = expandedSize.Width; expandedValue.Height = expandedSize.Height;
        AutomationProperties.SetName(expandedValue, AutomationProperties.GetName(activeValue));
        expandedCell.ShowAt(anchor);
    }

    private async Task CommitExpandedEditAsync()
    {
        if (!expandedEditing) return;
        runtime.SetCandidate(expandedValue.Text);
        var result = await runtime.CommitEditAsync();
        if (!result.IsSuccess) return;
        expandedClosing = true; expandedCell.Hide(); expandedClosing = false;
        Dispatcher.UIThread.Post(() => { expandedEditing = false; UpdateSelectionPresentation(); });
    }

    private void CancelExpandedEdit()
    {
        if (expandedEditing) runtime.CancelEdit();
        expandedClosing = true; expandedCell.Hide(); expandedClosing = false;
        Dispatcher.UIThread.Post(() => { expandedEditing = false; UpdateSelectionPresentation(); });
    }

    private ContextMenu BuildHeaderMenu(VariableCode code)
    {
        MenuItem Item(string text, Action action, bool enabled = true)
        {
            var item = new MenuItem { Header = text, IsEnabled = enabled };
            item.Click += (_, _) => action(); return item;
        }
        var width = BuildColumnWidthMenu(code);
        return new ContextMenu
        {
            Items =
            {
                Item("Sort ascending", () => _ = SortAsync(code, GridSortDirection.Ascending)),
                Item("Sort descending", () => _ = SortAsync(code, GridSortDirection.Descending)),
                Item("Clear sort", () => _ = runtime.SetSortAsync([], authorization), runtime.Sorts.Length > 0),
                new Separator(),
                BuildFilterMenu(code),
                Item("Clear filter", () => _ = ClearFilterAsync(), runtime.Filters.Any(x => x.VariableCode == code)),
                new Separator(),
                Item("Find in Column…", () => OpenGridFind(code), runtime.CanFind),
                new Separator(),
                width,
            },
        };
    }

    private MenuItem BuildFilterMenu(VariableCode code)
    {
        var menu = new MenuItem { Header = "Filter…" };
        menu.Items.Add(AsyncMenu("Is Empty", () => FilterAsync(new(code, GridFilterOperator.IsEmpty)), true));
        menu.Items.Add(AsyncMenu("Is Not Empty", () => FilterAsync(new(code, GridFilterOperator.IsNotEmpty)), true));
        var metadata = runtime.ResolvedDefinition.Columns.First(x => x.Definition.VariableCode == code).Definition;
        if (metadata.DataType == ColumnDataType.Boolean)
        {
            menu.Items.Add(AsyncMenu("True", () => FilterAsync(new(code, GridFilterOperator.True)), true));
            menu.Items.Add(AsyncMenu("False", () => FilterAsync(new(code, GridFilterOperator.False)), true));
        }
        return menu;
    }

    private Control BuildFindSurface()
    {
        var previous = new Button { Content = "↑", MinWidth = 34 };
        var next = new Button { Content = "↓", MinWidth = 34 };
        var close = new Button { Content = "×", MinWidth = 34 };
        previous.Click += async (_, _) => await FindAsync(GridFindDirection.Previous);
        next.Click += async (_, _) => await FindAsync(GridFindDirection.Next);
        close.Click += (_, _) => CloseGridFind();
        findScope.Click += (_, _) =>
        {
            findScope.ContextMenu = BuildFindScopeMenu();
            findScope.ContextMenu.Open(findScope);
        };
        findQuery.KeyDown += async (_, args) =>
        {
            if (args.Key == Key.Escape) { CloseGridFind(); args.Handled = true; }
            else if (args.Key == Key.Enter)
            {
                await FindAsync(args.KeyModifiers.HasFlag(KeyModifiers.Shift) ? GridFindDirection.Previous :
                    GridFindDirection.Next); args.Handled = true;
            }
        };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6,
            Margin = new Thickness(6, 4), Children = { findQuery, findScope, previous, next, close, findStatus } };
        var border = new Border { Child = panel, BorderThickness = new Thickness(0, 0, 0, 1) };
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("DuiBorderBrush"));
        AutomationProperties.SetName(border, "Find in DataEntry grid"); return border;
    }

    private void OpenGridFind(VariableCode? column = null, RowKey? row = null, bool useContextNatural = true)
    {
        findColumn = column ?? runtime.ActiveCell?.VariableCode;
        findRow = row;
        var natural = !useContextNatural ? GridFindScope.AllVisibleColumns : row is not null ? GridFindScope.CurrentRow : column is not null
            ? GridFindScope.CurrentColumn : GridFindScope.AllVisibleColumns;
        activeFindScope = runtime.ResolveFindScope(natural, findRow, findColumn);
        UpdateFindScopeLabel();
        findStatus.Text = string.Empty; findSurface.IsVisible = true;
        Dispatcher.UIThread.Post(() => { findQuery.Focus(); findQuery.SelectAll(); });
    }

    private ContextMenu BuildFindScopeMenu()
    {
        var menu = new ContextMenu();
        if (findRow is not null) menu.Items.Add(FindScopeItem(GridFindScope.CurrentRow, "Current Row"));
        if (findColumn is not null && runtime.PresentedColumns.Any(x => x.VariableCode == findColumn))
            menu.Items.Add(FindScopeItem(GridFindScope.CurrentColumn, "Current Column"));
        menu.Items.Add(FindScopeItem(GridFindScope.AllVisibleColumns, "All Visible Columns"));
        return menu;
    }

    private MenuItem FindScopeItem(GridFindScope scope, string label)
    {
        var item = new MenuItem { Header = $"{(activeFindScope == scope ? "✓ " : "  ")}{label}" };
        item.Click += (_, _) =>
        {
            activeFindScope = ValidFindScope(scope); runtime.RememberFindScope(activeFindScope); UpdateFindScopeLabel();
        };
        return item;
    }

    private GridFindScope ValidFindScope(GridFindScope scope) => scope switch
    {
        GridFindScope.CurrentRow when findRow is not null && runtime.Rows.Any(x => x.RowKey == findRow) => scope,
        GridFindScope.CurrentColumn when findColumn is not null && runtime.PresentedColumns.Any(x =>
            x.VariableCode == findColumn && x.Column.Definition.SensitiveContent is null) => scope,
        GridFindScope.AllVisibleColumns => scope,
        _ => GridFindScope.AllVisibleColumns,
    };

    private void UpdateFindScopeLabel() => findScope.Content = activeFindScope switch
    {
        GridFindScope.CurrentRow => "✓ Current Row  ⌄",
        GridFindScope.CurrentColumn => "✓ Current Column  ⌄",
        _ => "✓ All Visible Columns  ⌄",
    };

    private void CloseGridFind() { findSurface.IsVisible = false; findStatus.Text = string.Empty; Focus(); }

    private void OpenGridFindFromShortcut() => OpenGridFind(runtime.ActiveCell?.VariableCode,
        runtime.ActiveCell?.RowKey, useContextNatural: false);

    private async Task FindAsync(GridFindDirection direction)
    {
        activeFindScope = ValidFindScope(activeFindScope); UpdateFindScopeLabel();
        var result = await runtime.FindAsync(findQuery.Text ?? string.Empty, activeFindScope, findColumn, findRow, direction);
        findStatus.Text = result.IsMatch ? "Match" : "No match";
    }

    private Control BuildSizingBar()
    {
        var button = new Button { Content = $"Grid Actions · {runtime.RowHeightScalePercent:0}%  ⌄",
            HorizontalAlignment = runtime.Definition.Presentation.GridActionsAlignment == GridActionsAlignment.Start
                ? HorizontalAlignment.Left : HorizontalAlignment.Right,
            MinHeight = ResourceNumber("DuiControlHeightCompact", 28) };
        var menu = new ContextMenu();
        menu.Items.Add(BuildRowHeightMenu());
        if (runtime.Definition.ShowRowNumbers)
            menu.Items.Add(Menu($"{(runtime.ShowRowNumbers ? "✓ " : string.Empty)}Show Row Numbers",
                () => runtime.SetRowNumbersVisible(!runtime.ShowRowNumbers), true));
        button.ContextMenu = menu; button.Click += (_, _) => menu.Open(button);
        AutomationProperties.SetName(button, "Grid actions menu"); gridActionsButton = button;
        return new Border { Child = button,
            Margin = ResourceThickness("DuiGridActionsMargin", new Thickness(0, 0, 0, 2)) };
    }

    private ContextMenu BuildCellMenu(Control anchor, GridCellAddress address, int logicalRowPosition,
        ResolvedGridColumn column)
    {
        var edit = Menu("", () => OpenExpandedCell(anchor, address, column), true);
        var cut = Menu($"Cut                          {PrimaryShortcut("X")}", () => _ = CutSelectionAsync(), false);
        var copy = Menu($"Copy                         {PrimaryShortcut("C")}", () => _ = CopySelectionAsync(), true);
        var paste = Menu($"Paste                        {PrimaryShortcut("V")}", () => _ = PasteSelectionAsync(), false);
        var clear = Menu("Clear                        Delete", () => _ = ClearSelectionAsync(), false);
        var insertAbove = AsyncMenu("Insert Row Above", () => RunInsertAsync(address.RowKey,
            GridRowInsertPlacement.Before), false);
        var insertBelow = AsyncMenu("Insert Row Below", () => RunInsertAsync(address.RowKey,
            GridRowInsertPlacement.After), false);
        var deleteRow = Menu("Delete Row", () => _ = ConfirmDeleteRowAsync(address.RowKey), false);
        var deleteSelected = Menu("Delete Selected Rows", () => _ = ConfirmDeleteSelectedRowsAsync(), false);
        var menu = new ContextMenu
        {
            Items =
            {
                edit,
                new Separator(),
                cut, copy, paste, clear,
                new Separator(),
                insertAbove, insertBelow, deleteRow, deleteSelected,
                new Separator(),
                Menu("Select Row", () => runtime.Select([address.RowKey]), true),
                new Separator(),
                BuildColumnWidthMenu(address.VariableCode),
                BuildRowHeightMenu(),
            },
        };
        menu.Opening += (_, _) =>
        {
            PrepareCellContext(address, logicalRowPosition);
            var editable = runtime.CanEdit(address.RowKey, address.VariableCode);
            edit.Header = $"{(editable ? "Edit" : "View")}                         F2";
            edit.IsEnabled = editable || !column.CanEdit;
            cut.IsEnabled = editable && runtime.CanClearCellSelection();
            paste.IsEnabled = editable;
            clear.IsEnabled = editable && runtime.CanClearCellSelection();
            insertAbove.IsEnabled = insertBelow.IsEnabled = runtime.CanInsertRows;
            deleteRow.IsEnabled = runtime.CanDeleteRows;
            deleteSelected.IsEnabled = runtime.CanDeleteRows && SelectedRowKeysForCommand().Count > 1;
        };
        return menu;
    }

    private MenuItem BuildColumnWidthMenu(VariableCode code)
    {
        var menu = new MenuItem { Header = $"Column Width ({runtime.GetColumnWidthPercentage(code):0}%)" };
        menu.Items.Add(Menu("Narrower  -10%", () => runtime.DecreaseColumnWidth(code), true));
        menu.Items.Add(Menu("Wider  +10%", () => runtime.IncreaseColumnWidth(code), true));
        menu.Items.Add(new Separator());
        foreach (var percentage in new decimal[] { 75, 90, 100, 110, 125, 150, 200 })
            menu.Items.Add(Menu($"{percentage:0}%{(percentage == 100 ? " Default" : string.Empty)}" +
                $"{(runtime.GetColumnWidthPercentage(code) == percentage ? "  ✓" : string.Empty)}",
                () => runtime.SetColumnWidthPercentage(code, percentage), true));
        var custom = new NumericUpDown { Minimum = 50, Maximum = 300, Increment = 10,
            Value = runtime.GetColumnWidthPercentage(code), Width = 100 };
        custom.ValueChanged += (_, _) => { if (custom.Value is { } value) runtime.SetColumnWidthPercentage(code, value); };
        menu.Items.Add(new MenuItem { Header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6,
            Children = { new TextBlock { Text = "Custom %" }, custom } } });
        menu.Items.Add(Menu("Reset to Default", () => runtime.ResetColumnWidth(code), true));
        return menu;
    }

    private MenuItem BuildRowHeightMenu()
    {
        var menu = new MenuItem { Header = $"Row Height ({runtime.RowHeightScalePercent:0}%)" };
        foreach (var item in BuildRowHeightItems()) menu.Items.Add(item);
        return menu;
    }

    private IEnumerable<Control> BuildRowHeightItems()
    {
        foreach (var command in GridRowHeightCommands.Choices)
        {
            if (command.Kind == GridRowHeightCommandKind.Set && command.Percentage == 90) yield return new Separator();
            yield return Menu(command.Label + (command.Percentage == runtime.RowHeightScalePercent ? "  ✓" : string.Empty),
                () => runtime.ExecuteRowHeightCommand(command), true);
        }
        var custom = new NumericUpDown { Minimum = 75, Maximum = 300, Increment = 10,
            Value = runtime.RowHeightScalePercent, Width = 100 };
        custom.ValueChanged += (_, _) => { if (custom.Value is { } value) runtime.SetRowHeightPercentage(value); };
        yield return new MenuItem { Header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6,
            Children = { new TextBlock { Text = "Custom %" }, custom } } };
    }

    private void UpdateMaterializedRowHeights()
    {
        var scale = appearance?.Current.UiScale ?? 1d;
        foreach (var presenter in rowPresenters)
        {
            var height = (double)runtime.ResolveRowHeight(presenter.RowKey, (decimal)(DensityHeight(scale) / scale)) * scale;
            presenter.DataRow.Height = height; presenter.DataRow.MinHeight = height; presenter.RowHeader.Height = height;
        }
        if (gridActionsButton is not null)
            gridActionsButton.Content = $"Grid Actions · {runtime.RowHeightScalePercent:0}%  ⌄";
        QueueViewportResize();
    }

    private static string PrimaryShortcut(string key) => $"{(OperatingSystem.IsMacOS() ? "Cmd" : "Ctrl")}+{key}";

    private HashSet<RowKey> SelectedRowKeysForCommand()
    {
        var keys = runtime.SelectedRowKeys.ToHashSet();
        foreach (var range in runtime.SelectedRanges)
            foreach (var row in runtime.Rows.Where((_, index) => runtime.ViewportStartIndex + index >= range.MinimumRowPosition &&
                runtime.ViewportStartIndex + index <= range.MaximumRowPosition)) keys.Add(row.RowKey);
        if (keys.Count == 0 && runtime.ActiveCell is { } active) keys.Add(active.RowKey);
        return keys;
    }

    private ContextMenu BuildRowMenu(RowKey rowKey)
    {
        var editView = Menu("Edit / View", () => OpenRowEditView(rowKey), true);
        var insertAbove = AsyncMenu("Insert Row Above    Cmd/Ctrl+Shift+↑", () => RunInsertAsync(rowKey, GridRowInsertPlacement.Before), false);
        var insertBelow = AsyncMenu("Insert Row Below    Cmd/Ctrl+Shift+↓", () => RunInsertAsync(rowKey, GridRowInsertPlacement.After), false);
        var deleteRow = Menu("Delete Row    Cmd/Ctrl+Delete", () => _ = ConfirmDeleteRowAsync(rowKey), false);
        var deleteSelected = Menu("Delete Selected Rows    Cmd/Ctrl+Delete", () => _ = ConfirmDeleteSelectedRowsAsync(), false);
        var menu = new ContextMenu
        {
            Items =
            {
                editView,
                new Separator(),
                insertAbove, insertBelow, deleteRow, deleteSelected,
                new Separator(),
                Menu("Find in Row…", () => { SelectRowForAction(rowKey); OpenGridFind(runtime.ActiveCell?.VariableCode, rowKey); }, runtime.CanFind),
                AsyncMenu("Copy Row", () => runtime.CopyRowAsync(rowKey, clipboard), true),
                AsyncMenu("Clear Editable Values", () => runtime.ClearRowEditableValuesAsync(rowKey), true),
                new Separator(),
                BuildRowHeightMenu(),
            },
        };
        menu.Opening += (_, _) =>
        {
            SelectRowForAction(rowKey);
            insertAbove.IsEnabled = insertBelow.IsEnabled = runtime.CanInsertRows;
            deleteRow.IsEnabled = runtime.CanDeleteRows;
            deleteSelected.IsEnabled = runtime.CanDeleteRows && SelectedRowKeysForCommand().Count > 1;
        };
        return menu;
    }

    private void SelectRowForAction(RowKey rowKey)
    {
        var local = Array.FindIndex(runtime.Rows.ToArray(), x => x.RowKey == rowKey);
        if (runtime.ActiveCell?.RowKey == rowKey || runtime.SelectedRowKeys.Contains(rowKey) || local >= 0 &&
            runtime.SelectedRanges.Any(range => runtime.ViewportStartIndex + local >= range.MinimumRowPosition &&
                runtime.ViewportStartIndex + local <= range.MaximumRowPosition)) return;
        var column = runtime.PresentedColumns.FirstOrDefault()?.VariableCode;
        if (local >= 0 && column is { } code) runtime.SelectCell(new GridCellAddress(rowKey, code), runtime.ViewportStartIndex + local);
    }

    private void OpenRowEditView(RowKey rowKey)
    {
        SelectRowForAction(rowKey);
        var address = runtime.ActiveCell is { } active && active.RowKey == rowKey ? active :
            new GridCellAddress(rowKey, runtime.PresentedColumns[0].VariableCode);
        var presenter = cellPresenters.FirstOrDefault(x => x.Address == address).Presenter;
        var column = runtime.ResolvedDefinition.Columns.FirstOrDefault(x => x.Definition.VariableCode == address.VariableCode);
        if (presenter is not null && column is not null) OpenExpandedCell(presenter, address, column);
    }

    private static MenuItem Menu(string text, Action action, bool enabled)
    {
        var item = new MenuItem { Header = text, IsEnabled = enabled };
        item.Click += (_, _) => action(); return item;
    }

    private static MenuItem AsyncMenu(string text, Func<Task> action, bool enabled)
    {
        var item = new MenuItem { Header = text, IsEnabled = enabled };
        item.Click += async (_, _) => await action(); return item;
    }

    private async Task RunInsertAsync(RowKey rowKey, GridRowInsertPlacement placement)
    {
        var result = await runtime.InsertRowAsync(rowKey, placement);
        if (!result.IsSuccess) await messages.ShowAsync(new(MessageKind.Warning, "Insert row",
            result.DiagnosticCode ?? "The row could not be inserted."));
    }

    private RowKey? ActiveRowKey() => runtime.ActiveCell?.RowKey ??
        runtime.Rows.FirstOrDefault(x => runtime.SelectedRowKeys.Contains(x.RowKey))?.RowKey;

    private async Task InsertActiveRowAsync(GridRowInsertPlacement placement)
    {
        if (!runtime.CanInsertRows || ActiveRowKey() is not { } rowKey) return;
        await RunInsertAsync(rowKey, placement);
    }

    private async Task ConfirmDeleteRowAsync(RowKey rowKey)
    {
        if (!runtime.CanDeleteRows) return;
        var confirmed = await messages.ShowAsync(new(MessageKind.Confirmation, "Delete row",
            "Delete this row? This action is applied by the data provider."));
        if (confirmed == MessageResult.Confirmed)
            await SurfaceDeleteResultAsync(await runtime.DeleteRowAsync(rowKey));
    }

    private async Task ConfirmDeleteSelectedRowsAsync()
    {
        if (!runtime.CanDeleteRows) return;
        var count = SelectedRowKeysForCommand().Count;
        var confirmed = await messages.ShowAsync(new(MessageKind.Confirmation, "Delete rows",
            count == 1 ? "Delete the selected row?" : $"Delete {count} selected rows?"));
        if (confirmed == MessageResult.Confirmed)
            await SurfaceDeleteResultAsync(await runtime.DeleteSelectedRowsAsync());
    }

    private async Task SurfaceDeleteResultAsync(GridRowDeleteResult result)
    {
        if (!result.IsSuccess) await messages.ShowAsync(new(MessageKind.Warning, "Delete row",
            result.DiagnosticCode ?? "The row could not be deleted."));
    }

    private async Task ConfirmDeleteFromKeyboardAsync()
    {
        if (runtime.SelectedRowKeys.Count > 0 || runtime.SelectedRanges.Length > 1 ||
            runtime.SelectedRanges.FirstOrDefault()?.RowCount > 1)
            await ConfirmDeleteSelectedRowsAsync();
        else if (ActiveRowKey() is { } rowKey) await ConfirmDeleteRowAsync(rowKey);
    }

    private string HeaderState(VariableCode code)
    {
        var column = runtime.PresentedColumns.First(x => x.VariableCode == code);
        var sort = runtime.Sorts.FirstOrDefault(x => x.VariableCode == code)?.Direction.ToString() ?? "unsorted";
        var filter = runtime.Filters.Any(x => x.VariableCode == code) ? "filtered" : "unfiltered";
        return $"{sort}, {filter}, {(column.Pin == GridColumnPin.Left ? "pinned left" : "not pinned")}";
    }

    private Grid CreateColumns(IEnumerable<ResolvedGridColumn> columns, double scale)
    {
        var grid = new Grid();
        foreach (var column in columns)
            grid.ColumnDefinitions.Add(new global::Avalonia.Controls.ColumnDefinition(new GridLength((double)column.Width * scale, GridUnitType.Pixel))
            {
                MinWidth = (double)column.MinWidth * scale,
                MaxWidth = (double)column.MaxWidth * scale,
            });
        return grid;
    }

    private Control BuildRowHeaderHeading(double scale)
    {
        var border = new Border { Width = ResolveRowHeaderWidth() * scale,
            Height = RoleNumber("DuiGridHeader", runtime.Definition.Presentation.HeaderHeight, 38) * scale,
            BorderThickness = new Thickness(0, 0, 1, 1) };
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("DuiBorderBrush"));
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable("DuiGridRowHeaderBrush"));
        AutomationProperties.SetName(border, "Grid corner header"); return border;
    }

    private Control BuildRowHeader(RowKey rowKey, int logicalPosition, double scale)
    {
        var label = new TextBlock { Text = logicalPosition.ToString("N0", CultureInfo.CurrentCulture),
            Margin = new Thickness(6 * scale, 3 * scale), VerticalAlignment = VerticalAlignment.Center };
        AutomationProperties.SetName(label, $"{localization.Get(new("Grid.Row"))} {logicalPosition}");
        label.PointerPressed += (_, args) =>
        {
            if (HasPrimaryModifier(args.KeyModifiers)) runtime.ToggleSelection(rowKey);
            else if (args.KeyModifiers.HasFlag(KeyModifiers.Shift) && runtime.ActiveCell is { } active &&
                runtime.PresentedColumns.FirstOrDefault()?.VariableCode is { } code)
            {
                var local = Array.FindIndex(runtime.Rows.ToArray(), x => x.RowKey == rowKey);
                if (local >= 0) runtime.SelectCell(new GridCellAddress(active.RowKey, code),
                    runtime.ViewportStartIndex + Math.Max(0, Array.FindIndex(runtime.Rows.ToArray(), x => x.RowKey == active.RowKey)));
                if (local >= 0) runtime.SelectCell(new GridCellAddress(rowKey, code), runtime.ViewportStartIndex + local, extend: true);
            }
            else runtime.Select([rowKey]);
        };
        var action = new Button { Content = "⌄", Width = 24 * scale, Padding = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Stretch,
            ContextMenu = BuildRowMenu(rowKey) };
        action.Click += (_, _) => action.ContextMenu?.Open(action);
        AutomationProperties.SetName(action, $"Row {logicalPosition} actions");
        var leading = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        Grid.SetColumn(label, 0); Grid.SetColumn(action, 1); leading.Children.Add(label); leading.Children.Add(action);
        var height = (double)runtime.ResolveRowHeight(rowKey, (decimal)(DensityHeight(scale) / scale)) * scale;
        var border = new Border { Child = leading,
            Width = ResolveRowHeaderWidth() * scale, Height = height,
            BorderThickness = new Thickness(0, 0, 1, 1) };
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("DuiBorderBrush"));
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable(
            runtime.SelectedRowKeys.Contains(rowKey) ? "DuiSelectionBrush" : "DuiGridRowHeaderBrush"));
        return border;
    }

    private Control BuildViewportNavigation(double scale)
    {
        var compactHeight = ResourceNumber("DuiControlHeightCompact", 28) * scale;
        var hitTarget = ResourceNumber("DuiHitTargetMinimum", 32) * scale;
        var previous = new Button { Content = "‹", IsEnabled = runtime.HasPreviousViewport,
            MinWidth = hitTarget, MinHeight = compactHeight };
        var next = new Button { Content = "›", IsEnabled = runtime.HasNextViewport,
            MinWidth = hitTarget, MinHeight = compactHeight };
        var retry = new Button { Content = localization.Get(new("Grid.Retry")), IsVisible = runtime.State == GridProviderState.Error };
        var slider = new Slider { Minimum = 0, Maximum = Math.Max(0, runtime.TotalRows - 1),
            Value = runtime.RequestedViewportStartIndex, TickFrequency = Math.Max(1, runtime.RequestedViewportRowCount),
            HorizontalAlignment = HorizontalAlignment.Stretch };
        var status = new TextBlock
        {
            Text = $"{runtime.ViewportStartIndex + 1:N0}–{Math.Min(runtime.TotalRows, runtime.ViewportStartIndex + runtime.Rows.Length):N0} / {runtime.TotalRows:N0}",
            VerticalAlignment = VerticalAlignment.Center,
        };
        previous.Click += async (_, _) => await RequestViewportAsync(Math.Max(0,
            runtime.RequestedViewportStartIndex - runtime.RequestedViewportRowCount));
        next.Click += async (_, _) => await RequestViewportAsync(Math.Min(Math.Max(0, runtime.TotalRows - 1),
            runtime.RequestedViewportStartIndex + runtime.RequestedViewportRowCount));
        retry.Click += async (_, _) => await RetryAsync();
        slider.ValueChanged += (_, _) =>
        {
            QueueSliderViewport((int)Math.Round(slider.Value));
        };
        AutomationProperties.SetName(slider, "Logical row position");
        var panel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto,Auto"),
            ColumnSpacing = ResourceNumber("DuiGridControlGap", 4) * scale,
            Margin = ResourceThickness("DuiGridNavigationMargin", new Thickness(0, 0, 0, 4)),
            MinWidth = ResourceNumber("DuiGridMinimumViewportWidth", 320) * scale,
        };
        Grid.SetColumn(previous, 0); Grid.SetColumn(next, 1); Grid.SetColumn(slider, 2); Grid.SetColumn(status, 3); Grid.SetColumn(retry, 4);
        panel.Children.Add(previous); panel.Children.Add(next); panel.Children.Add(slider); panel.Children.Add(status); panel.Children.Add(retry);
        return panel;
    }

    private static double ResourceNumber(string key, double fallback) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is double number ? number : fallback;

    private static Thickness ResourceThickness(string key, Thickness fallback) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is Thickness thickness ? thickness : fallback;

    private static double RoleNumber(string prefix, GridGeometryRole role, double fallback) =>
        ResourceNumber(prefix + role, fallback);

    private Thickness ResolveOuterInset(double scale)
    {
        var inset = runtime.Definition.Presentation.EffectiveOuterInset;
        return new Thickness(
            RoleNumber("DuiGridInset", inset.Left, 0) * scale,
            RoleNumber("DuiGridInset", inset.Top, 0) * scale,
            RoleNumber("DuiGridInset", inset.Right, 0) * scale,
            RoleNumber("DuiGridInset", inset.Bottom, 0) * scale);
    }

    private double ResolveAvailableWorkspaceHeight(double measuredAvailable)
    {
        if (!double.IsInfinity(measuredAvailable) && !double.IsNaN(measuredAvailable) && measuredAvailable > 0)
            return measuredAvailable;
        var topLevel = TopLevel.GetTopLevel(this);
        var origin = topLevel is null ? null : this.TranslatePoint(default, topLevel);
        var remaining = topLevel is null ? ResourceNumber("DuiGridHeightStandard", 520) :
            topLevel.Bounds.Height - Math.Max(0, origin?.Y ?? 0);
        return Math.Max(ResourceNumber("DuiGridMinimumViewportHeight", 180), remaining);
    }

    private double ResolveConfiguredHeight(double availableHeight)
    {
        var presentation = runtime.Definition.Presentation;
        var requested = presentation.HeightMode switch
        {
            GridHeightMode.FitWorkspace => availableHeight,
            GridHeightMode.Compact => ResourceNumber("DuiGridHeightCompact", 360),
            GridHeightMode.Standard => ResourceNumber("DuiGridHeightStandard", 520),
            GridHeightMode.Expanded => ResourceNumber("DuiGridHeightExpanded", 720),
            GridHeightMode.FixedSemanticHeight => RoleNumber("DuiGridHeight", presentation.FixedHeightRole, 520),
            _ => throw new InvalidOperationException("Unknown Grid height mode."),
        };
        if (presentation.HeightMode == GridHeightMode.FixedSemanticHeight && presentation.AllowFixedHeightBeyondWorkspace)
            return requested;
        return Math.Min(availableHeight, requested);
    }

    private double EffectiveViewportHeight()
    {
        if (scroller.Bounds.Height > 0) return scroller.Bounds.Height;
        if (Bounds.Height <= 0) return 0;
        var chrome = ResourceNumber("DuiControlHeightCompact", 28) * 2 +
            RoleNumber("DuiGridHeader", runtime.Definition.Presentation.HeaderHeight, 38);
        return Math.Max(ResourceNumber("DuiGridMinimumViewportHeight", 180), Bounds.Height - chrome);
    }

    private int ViewportRowLimit()
    {
        var materializedLimit = runtime.ViewportOptions.MaximumMaterializedRows -
            runtime.ViewportOptions.OverscanBefore - runtime.ViewportOptions.OverscanAfter;
        var profileLimit = runtime.Definition.Presentation.ViewportProfile switch
        {
            GridViewportProfile.Compact => 32,
            GridViewportProfile.Standard => 60,
            GridViewportProfile.Large => 84,
            GridViewportProfile.MaximumWorkspace => materializedLimit,
            _ => throw new InvalidOperationException("Unknown Grid viewport profile."),
        };
        return Math.Max(1, Math.Min(materializedLimit, profileLimit));
    }

    private void ValidateThemeGeometry()
    {
        var presentation = runtime.Definition.Presentation;
        var inset = presentation.EffectiveOuterInset;
        foreach (var value in new[] { inset.Left, inset.Top, inset.Right, inset.Bottom })
            if (RoleNumber("DuiGridInset", value, DefaultInset(value)) < 0)
                throw new InvalidOperationException("Grid outer inset theme mappings must be non-negative.");
        var rowHeader = ResolveRowHeaderWidth();
        if (presentation.RowNumbersCanBeShown && rowHeader < 72)
            throw new InvalidOperationException("Grid row-header width must preserve the readable minimum.");
        var clearance = RoleNumber("DuiGridScrollbarClearance", presentation.ScrollbarClearance, 2);
        var thickness = ResourceNumber(GridThemeResourceKeys.ScrollbarThickness, 10);
        var thumbMinimum = ResourceNumber(GridThemeResourceKeys.ScrollbarThumbMinLength, 28);
        var hitTarget = ResourceNumber(GridThemeResourceKeys.ScrollbarHitTarget, 14);
        var viewportMinimum = ResourceNumber("DuiGridMinimumViewportWidth", 320);
        var viewportHeightMinimum = ResourceNumber("DuiGridMinimumViewportHeight", 180);
        if (clearance < 0 || thickness < 6 || hitTarget < thickness || thumbMinimum < thickness ||
            viewportMinimum < 240 || viewportHeightMinimum < 160)
            throw new InvalidOperationException("Grid scrollbar or viewport theme mappings are invalid.");
    }

    private static void ApplyScrollbarRecipe(Control control)
    {
        static object? Resource(string key) =>
            Application.Current?.TryFindResource(key, out var value) == true ? value : null;
        var track = Resource(GridThemeResourceKeys.ScrollbarTrack);
        var thumb = Resource(GridThemeResourceKeys.ScrollbarThumb);
        var hover = Resource(GridThemeResourceKeys.ScrollbarHover);
        var pressed = Resource(GridThemeResourceKeys.ScrollbarPressed);
        var disabled = Resource(GridThemeResourceKeys.ScrollbarDisabled);
        foreach (var key in new[] { "ScrollBarBackgroundBrushHorizontal", "ScrollBarBackgroundBrushVertical" })
            if (track is not null) control.Resources[key] = track;
        foreach (var key in new[] { "ScrollBarThumbBackgroundBrushHorizontal", "ScrollBarThumbBackgroundBrushVertical" })
            if (thumb is not null) control.Resources[key] = thumb;
        foreach (var key in new[] { "ScrollBarThumbBackgroundBrushHorizontalPointerOver", "ScrollBarThumbBackgroundBrushVerticalPointerOver" })
            if (hover is not null) control.Resources[key] = hover;
        foreach (var key in new[] { "ScrollBarThumbBackgroundBrushHorizontalPressed", "ScrollBarThumbBackgroundBrushVerticalPressed" })
            if (pressed is not null) control.Resources[key] = pressed;
        foreach (var key in new[] { "ScrollBarBackgroundBrushHorizontalDisabled", "ScrollBarBackgroundBrushVerticalDisabled" })
            if (disabled is not null) control.Resources[key] = disabled;
        control.Resources["ScrollBarMinAscent"] = ResourceNumber(GridThemeResourceKeys.ScrollbarHitTarget, 14);
        control.Resources["ScrollBarThumbMinAscent"] = ResourceNumber(GridThemeResourceKeys.ScrollbarThumbMinLength, 28);
    }

    private static double DefaultInset(GridGeometryRole role) => role switch
    {
        GridGeometryRole.None or GridGeometryRole.Minimal => 0,
        GridGeometryRole.Compact => 4,
        GridGeometryRole.Standard => 8,
        GridGeometryRole.Comfortable => 12,
        _ => -1,
    };

    private double ResolveRowHeaderWidth() => runtime.Definition.Presentation.RowHeaderWidthOverride ??
        RoleNumber("DuiGridRowHeader", runtime.Definition.Presentation.RowHeaderWidth, 72);

    private void QueueSliderViewport(int startIndex)
    {
        viewportNavigation?.Cancel(); viewportNavigation?.Dispose();
        viewportNavigation = new CancellationTokenSource();
        var token = viewportNavigation.Token;
        _ = DebouncedViewportAsync(startIndex, token);
    }

    private async Task DebouncedViewportAsync(int startIndex, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(120, cancellationToken);
            await RequestViewportAsync(startIndex, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private void NavigateLogicalPosition(int requestedPosition)
    {
        if (!runtime.IsVirtualized || runtime.Rows.Length == 0) return;
        var position = Math.Clamp(requestedPosition, 0, Math.Max(0, runtime.TotalRows - 1));
        updatingLogicalScrollbar = true;
        logicalVerticalScrollbar.Value = Math.Min(logicalVerticalScrollbar.Maximum, position);
        updatingLogicalScrollbar = false;
        var visible = Math.Max(1, runtime.RequestedViewportRowCount);
        var threshold = Math.Max(4, Math.Min(runtime.ViewportOptions.OverscanAfter, visible / 3));
        var first = runtime.ViewportStartIndex;
        var endExclusive = first + runtime.Rows.Length;
        if (position >= first && position + visible <= endExclusive)
        {
            var scale = appearance?.Current.UiScale ?? 1d;
            var offset = Math.Max(0, position - first) * DensityHeight(scale);
            scroller.Offset = new Vector(scroller.Offset.X, offset);
            rowHeaderScroller.Offset = new Vector(0, offset);
            if (position + visible >= endExclusive - threshold && runtime.HasNextViewport)
                QueueViewportPrefetch(Math.Min(Math.Max(0, runtime.TotalRows - 1),
                    runtime.RequestedViewportStartIndex + visible));
            else if (position <= first + threshold && runtime.HasPreviousViewport)
                QueueViewportPrefetch(Math.Max(0, runtime.RequestedViewportStartIndex - visible));
            return;
        }
        QueueViewportPrefetch(position);
        QueueSliderViewport(position);
    }

    private void QueueViewportPrefetch(int startIndex)
    {
        viewportPrefetch?.Cancel(); viewportPrefetch?.Dispose();
        viewportPrefetch = new CancellationTokenSource();
        var token = viewportPrefetch.Token;
        _ = PrefetchViewportAsync(startIndex, token);
    }

    private async Task PrefetchViewportAsync(int startIndex, CancellationToken cancellationToken)
    {
        try
        {
            await runtime.PrefetchViewportAsync(startIndex,
                runtime.RequestedViewportRowCount > 0 ? runtime.RequestedViewportRowCount : runtime.ViewportOptions.VisibleRowCount,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch { /* Prefetch is opportunistic; visible state and diagnostics remain unchanged. */ }
    }

    private void QueueNextViewportFromScroll()
    {
        if (!IsNearViewportEnd()) return;
        QueueViewportPrefetch(Math.Min(Math.Max(0, runtime.TotalRows - 1),
            runtime.RequestedViewportStartIndex + Math.Max(1, runtime.RequestedViewportRowCount)));
    }

    private bool IsNearViewportEnd() => DateTimeOffset.UtcNow >= scrollRequestsEnabledAt && runtime.IsVirtualized &&
        runtime.HasNextViewport && scroller.Viewport.Height > 0 &&
        scroller.Offset.Y >= scroller.Extent.Height - scroller.Viewport.Height -
            Math.Max(4, runtime.ViewportOptions.OverscanAfter / 2) * DensityHeight(appearance?.Current.UiScale ?? 1d);

    private Task ReevaluateViewportAsync(CancellationToken cancellationToken = default)
    {
        if (!runtime.IsVirtualized || context is null || Bounds.Height <= 0) return Task.CompletedTask;
        var scale = appearance?.Current.UiScale ?? 1d;
        var count = Math.Clamp((int)Math.Ceiling(EffectiveViewportHeight() / DensityHeight(scale)) + 4,
            Math.Min(20, ViewportRowLimit()), ViewportRowLimit());
        return count == runtime.RequestedViewportRowCount ? Task.CompletedTask : runtime.ResizeViewportAsync(count, cancellationToken);
    }

    private void QueueViewportResize()
    {
        viewportResize?.Cancel(); viewportResize?.Dispose(); viewportResize = new CancellationTokenSource();
        var token = viewportResize.Token;
        _ = DebouncedResizeAsync(token);
    }

    private void CancelViewportResize()
    {
        viewportResize?.Cancel(); viewportResize?.Dispose(); viewportResize = null;
    }

    private async Task DebouncedResizeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(180, cancellationToken);
            await ReevaluateViewportAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private double DensityHeight(double scale) => runtime.Definition.Presentation.Density == GridGeometryRole.Standard
        ? (appearance?.Current.GridDensity ?? GridDensityPreference.Comfortable) switch
        {
            GridDensityPreference.Compact => RoleNumber("DuiGridDensity", GridGeometryRole.Compact, 32) * scale,
            GridDensityPreference.Large => RoleNumber("DuiGridDensity", GridGeometryRole.Comfortable, 44) * scale,
            _ => RoleNumber("DuiGridDensity", GridGeometryRole.Standard, 38) * scale,
        }
        : RoleNumber("DuiGridDensity", runtime.Definition.Presentation.Density, 38) * scale;

    private static string Format(object? value, GridMetadataColumn column) => value switch
    {
        null => "—",
        bool boolean => boolean ? "✓" : "○",
        DateOnly date => date.ToString(column.Format ?? "d", CultureInfo.CurrentCulture),
        DateTime dateTime => dateTime.ToString(column.Format ?? "g", CultureInfo.CurrentCulture),
        IFormattable formattable when !string.IsNullOrWhiteSpace(column.Format) => formattable.ToString(column.Format, CultureInfo.CurrentCulture),
        _ => value.ToString() ?? "—",
    };

    private async void HandleKeyDown(object? sender, KeyEventArgs args)
    {
        if (NativeEditorInputOwnership.Owns(args.Source as InputElement)) return;
        if (expandedCell.IsOpen && expandedValue.IsKeyboardFocusWithin) return;
        if (findSurface.IsVisible && findQuery.IsKeyboardFocusWithin &&
            !(HasPrimaryModifier(args.KeyModifiers) && args.Key == Key.F)) return;
        if (args.Key == Key.Escape && runtime.EditBuffer is not null) { CancelEdit(); args.Handled = true; return; }
        var primary = HasPrimaryModifier(args.KeyModifiers);
        if (primary)
        {
            if (args.Key == Key.F) OpenGridFindFromShortcut();
            else if (args.KeyModifiers.HasFlag(KeyModifiers.Shift) && args.Key == Key.Up)
                await InsertActiveRowAsync(GridRowInsertPlacement.Before);
            else if (args.KeyModifiers.HasFlag(KeyModifiers.Shift) && args.Key == Key.Down)
                await InsertActiveRowAsync(GridRowInsertPlacement.After);
            else if (args.Key is Key.Delete or Key.Back) await ConfirmDeleteFromKeyboardAsync();
            else if (args.Key == Key.C) await runtime.CopyAsync(clipboard);
            else if (args.Key == Key.X) await runtime.CutAsync(clipboard);
            else if (args.Key == Key.V)
            {
                if (runtime.EditBuffer is not null) runtime.CancelEdit();
                await runtime.PasteAsync(clipboard);
            }
            else if (args.Key == Key.Z && args.KeyModifiers.HasFlag(KeyModifiers.Shift)) await runtime.RedoAsync();
            else if (args.Key == Key.Z) await runtime.UndoAsync();
            else if (args.Key == Key.Y) await runtime.RedoAsync();
            else if (args.Key == Key.A) runtime.SelectAllCells();
            else return;
            args.Handled = true;
            return;
        }
        if (runtime.Rows.Length == 0) return;
        var shift = args.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var handled = args.Key switch
        {
            Key.Up => runtime.MoveActiveCell(-1, 0, shift),
            Key.Down => runtime.MoveActiveCell(1, 0, shift),
            Key.Left => runtime.MoveActiveCell(0, -1, shift),
            Key.Right => runtime.MoveActiveCell(0, 1, shift),
            Key.Tab => runtime.MoveToNextCell(shift, editableOnly: true),
            _ => false,
        };
        if (args.Key is Key.Delete or Key.Back or Key.Clear)
        {
            await runtime.ClearSelectedCellsAsync(); handled = true;
        }
        else if (args.Key is Key.Enter or Key.F2 && runtime.ActiveCell is { } active)
        {
            var presenter = cellPresenters.FirstOrDefault(x => x.Address == active).Presenter;
            var column = runtime.ResolvedDefinition.Columns.FirstOrDefault(x => x.Definition.VariableCode == active.VariableCode);
            if (presenter is not null && column is not null) OpenExpandedCell(presenter, active, column);
            handled = true;
        }
        else if (args.Key == Key.Escape && runtime.CellSelection.HasCellSelection)
        {
            runtime.ClearCellSelection(); handled = true;
        }
        args.Handled = handled;
    }

    private void HandleTextInput(object? sender, TextInputEventArgs args)
    {
        if (args.Handled || expandedCell.IsOpen || string.IsNullOrEmpty(args.Text) ||
            runtime.ActiveCell is not { } active) return;
        var presenter = cellPresenters.FirstOrDefault(x => x.Address == active).Presenter;
        var column = runtime.ResolvedDefinition.Columns.FirstOrDefault(x => x.Definition.VariableCode == active.VariableCode);
        if (presenter is null || column is null) return;
        if (!runtime.CanEdit(active.RowKey, active.VariableCode)) { args.Handled = true; return; }
        OpenExpandedCell(presenter, active, column, printableReplacement: true);
        if (!expandedEditing) { args.Handled = true; return; }
        expandedValue.Text = args.Text;
        expandedValue.CaretIndex = expandedValue.Text.Length;
        args.Handled = true;
    }

    private static bool HasPrimaryModifier(KeyModifiers modifiers) =>
        modifiers.HasFlag(KeyModifiers.Control) || modifiers.HasFlag(KeyModifiers.Meta);
}
