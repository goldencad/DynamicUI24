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
        VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
    private readonly ScrollViewer rowHeaderScroller = new()
    {
        HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
    };
    private EffectiveAuthorizationContext? authorization;
    private GridProviderContext? context;
    private TextBox? activeEditor;
    private CancellationTokenSource? viewportNavigation;
    private CancellationTokenSource? viewportResize;
    private bool scrollRequestPending;
    private DateTimeOffset scrollRequestsEnabledAt;
    private bool pointerSelecting;
    private GridRangeEndpoint pointerAnchor;
    private ImmutableArray<GridCellRange> pointerRetainedRanges = [];
    private readonly List<(GridCellAddress Address, int Position, Border Presenter, TextBlock? ValuePresenter)> cellPresenters = [];
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
        MinHeight = 320;
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
            else if (expandedEditing && args.Reason == "EDIT_COMMIT" && args.Cell is { } committedCell)
            {
                RefreshMaterializedCell(committedCell);
                UpdateSelectionPresentation();
            }
            else if (expandedEditing && args.Reason is "EDIT_BEGIN" or "EDIT_CANDIDATE" or "EDIT_CANCEL") { }
            else if (args.Reason != "EDIT_CANDIDATE" || activeEditor is null) Rebuild();
            Changed?.Invoke(this, EventArgs.Empty);
        });
        this.privacyState.StateChanged += (_, _) => Dispatcher.UIThread.Post(Rebuild);
        localization.CultureChanged += (_, _) => Rebuild();
        if (appearance is not null) appearance.PreferencesChanged += (_, _) =>
        {
            Rebuild();
            QueueViewportResize();
        };
        scroller.ScrollChanged += (_, _) =>
        {
            if (Math.Abs(rowHeaderScroller.Offset.Y - scroller.Offset.Y) > .1)
                rowHeaderScroller.Offset = new Vector(0, scroller.Offset.Y);
            QueueNextViewportFromScroll();
        };
        SizeChanged += (_, _) => QueueViewportResize();
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

    public DataEntryGridRuntime Runtime => runtime;
    public int RenderedColumnCount => runtime.PresentedColumns.Length;
    public int RenderedRowCount => runtime.Rows.Length;
    public bool HasActiveEditor => activeEditor is not null;
    public EditorResolution? LastEditorResolution { get; private set; }
    public event EventHandler? Changed;

    public Task LoadAsync(GridProviderContext providerContext, EffectiveAuthorizationContext? effectiveAuthorization,
        CancellationToken cancellationToken = default)
    {
        CancelViewportResize();
        expandedEditing = false; expandedClosing = true; expandedCell.Hide(); expandedClosing = false;
        context = providerContext;
        authorization = effectiveAuthorization;
        return runtime.LoadAsync(providerContext, effectiveAuthorization, cancellationToken);
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
        activeEditor = null;
        cellPresenters.Clear();
        if (activeValue.Parent is Panel previousDetailPanel) previousDetailPanel.Children.Remove(activeValue);
        else if (activeValue.Parent is Border previousDetail) previousDetail.Child = null;
        if (stateText.Parent is Panel statePanel) statePanel.Children.Remove(stateText);
        else if (ReferenceEquals(Content, stateText)) Content = null;
        if (scroller.Parent is Panel previousPanel) previousPanel.Children.Remove(scroller);
        else if (ReferenceEquals(Content, scroller)) Content = null;
        if (rowHeaderScroller.Parent is Panel previousHeaderPanel) previousHeaderPanel.Children.Remove(rowHeaderScroller);
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
        stack.Children.Add(BuildHeader(columns, scale));
        rowHeaders.Children.Add(BuildRowHeaderHeading(scale));
        for (var index = 0; index < runtime.Rows.Length; index++)
        {
            stack.Children.Add(BuildRow(runtime.Rows[index], index, columns, scale));
            rowHeaders.Children.Add(BuildRowHeader(runtime.Rows[index].RowKey,
                runtime.ViewportStartIndex + index + 1, scale));
        }
        scroller.Content = stack;
        rowHeaderScroller.Content = rowHeaders;
        scroller.Offset = new Vector(scroller.Offset.X, 0);
        rowHeaderScroller.Offset = default;
        var rebuiltGuard = DateTimeOffset.UtcNow.AddMilliseconds(250);
        if (scrollRequestsEnabledAt < rebuiltGuard) scrollRequestsEnabledAt = rebuiltGuard;
        var layout = new DockPanel();
        var detailContent = new StackPanel { Children = { activeValue } };
        var detail = new Border { Child = detailContent, BorderThickness = new Thickness(1, 0, 0, 0) };
        detail.Bind(Border.BorderBrushProperty, detail.GetResourceObservable("DuiBorderBrush"));
        DockPanel.SetDock(detail, Dock.Bottom); layout.Children.Add(detail);
        var sizing = BuildSizingBar(); DockPanel.SetDock(sizing, Dock.Top); layout.Children.Add(sizing);
        if (findSurface.Parent is Panel oldFindParent) oldFindParent.Children.Remove(findSurface);
        DockPanel.SetDock(findSurface, Dock.Top); layout.Children.Add(findSurface);
        if (runtime.IsVirtualized)
        {
            var navigation = BuildViewportNavigation(scale);
            DockPanel.SetDock(navigation, Dock.Top); layout.Children.Add(navigation);
        }
        var gridSurface = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        Grid.SetColumn(rowHeaderScroller, 0); Grid.SetColumn(scroller, 1);
        gridSurface.Children.Add(rowHeaderScroller); gridSurface.Children.Add(scroller);
        layout.Children.Add(gridSurface);
        Content = layout;
        UpdateSelectionPresentation();
    }

    private Control BuildHeader(IReadOnlyList<ResolvedGridColumn> columns, double scale)
    {
        var grid = CreateColumns(columns, scale);
        grid.MinHeight = 38 * scale;
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
            Grid.SetColumn(header, index + columnOffset); grid.Children.Add(header);
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
        var button = new Button { Content = $"Row Height {runtime.RowHeightScalePercent:0}%  ⌄",
            HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(4, 2), Padding = new Thickness(8, 3) };
        var rowHeight = BuildRowHeightMenu();
        var menu = new ContextMenu { ItemsSource = rowHeight.Items };
        button.ContextMenu = menu; button.Click += (_, _) => menu.Open(button);
        AutomationProperties.SetName(button, "Grid row height menu"); return button;
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
        menu.Items.Add(Menu("Shorter  -10%", runtime.DecreaseRowHeight, true));
        menu.Items.Add(Menu("Taller  +10%", runtime.IncreaseRowHeight, true));
        menu.Items.Add(new Separator());
        foreach (var percentage in new decimal[] { 90, 100, 110, 125, 150, 200 })
            menu.Items.Add(Menu($"{percentage:0}%{(percentage == 100 ? " Default" : string.Empty)}" +
                $"{(runtime.RowHeightScalePercent == percentage ? "  ✓" : string.Empty)}",
                () => runtime.SetRowHeightPercentage(percentage), true));
        var custom = new NumericUpDown { Minimum = 75, Maximum = 300, Increment = 10,
            Value = runtime.RowHeightScalePercent, Width = 100 };
        custom.ValueChanged += (_, _) => { if (custom.Value is { } value) runtime.SetRowHeightPercentage(value); };
        menu.Items.Add(new MenuItem { Header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6,
            Children = { new TextBlock { Text = "Custom %" }, custom } } });
        menu.Items.Add(Menu("Reset", runtime.ResetRowHeightPercentage, true));
        return menu;
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
        var border = new Border { Width = 72 * scale, Height = 38 * scale,
            BorderThickness = new Thickness(0, 0, 1, 1) };
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("DuiBorderBrush"));
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable("DuiSurfaceRaisedBrush"));
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
        var border = new Border { Child = leading, Width = 72 * scale, Height = height,
            BorderThickness = new Thickness(0, 0, 1, 1) };
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("DuiBorderBrush"));
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable(
            runtime.SelectedRowKeys.Contains(rowKey) ? "DuiSelectionBrush" : "DuiSurfaceRaisedBrush"));
        return border;
    }

    private Control BuildViewportNavigation(double scale)
    {
        var previous = new Button { Content = "‹", IsEnabled = runtime.HasPreviousViewport, MinWidth = 36 * scale };
        var next = new Button { Content = "›", IsEnabled = runtime.HasNextViewport, MinWidth = 36 * scale };
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
        var panel = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto,Auto"), Margin = new Thickness(0, 0, 0, 4) };
        Grid.SetColumn(previous, 0); Grid.SetColumn(next, 1); Grid.SetColumn(slider, 2); Grid.SetColumn(status, 3); Grid.SetColumn(retry, 4);
        panel.Children.Add(previous); panel.Children.Add(next); panel.Children.Add(slider); panel.Children.Add(status); panel.Children.Add(retry);
        return panel;
    }

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

    private void QueueNextViewportFromScroll()
    {
        if (!IsNearViewportEnd() || scrollRequestPending) return;
        scrollRequestPending = true;
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                if (IsNearViewportEnd())
                    await RequestViewportAsync(runtime.RequestedViewportStartIndex + runtime.RequestedViewportRowCount);
            }
            finally { scrollRequestPending = false; }
        }, DispatcherPriority.Background);
    }

    private bool IsNearViewportEnd() => DateTimeOffset.UtcNow >= scrollRequestsEnabledAt && runtime.IsVirtualized &&
        runtime.HasNextViewport && scroller.Viewport.Height > 0 &&
        scroller.Offset.Y >= scroller.Extent.Height - scroller.Viewport.Height - DensityHeight(appearance?.Current.UiScale ?? 1d);

    private Task ReevaluateViewportAsync(CancellationToken cancellationToken = default)
    {
        if (!runtime.IsVirtualized || context is null || Bounds.Height <= 0) return Task.CompletedTask;
        var scale = appearance?.Current.UiScale ?? 1d;
        var count = Math.Clamp((int)Math.Ceiling(Bounds.Height / DensityHeight(scale)) + 4, 20,
            runtime.ViewportOptions.MaximumMaterializedRows - runtime.ViewportOptions.OverscanBefore - runtime.ViewportOptions.OverscanAfter);
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

    private double DensityHeight(double scale) => (appearance?.Current.GridDensity ?? GridDensityPreference.Comfortable) switch
    {
        GridDensityPreference.Compact => 30 * scale,
        GridDensityPreference.Large => 46 * scale,
        _ => 38 * scale,
    };

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
