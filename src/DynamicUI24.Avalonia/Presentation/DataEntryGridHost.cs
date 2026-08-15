using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.DataEntry;
using DynamicUI24.Core.Setup;
using DynamicUI24.Shared.Presentation;
using GridMetadataColumn = DynamicUI24.Core.Setup.ColumnDefinition;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Metadata-driven table adapter. All data and editing behavior remains in the Avalonia-free runtime.</summary>
public sealed class DataEntryGridHost : UserControl
{
    private readonly DataEntryGridRuntime runtime;
    private readonly ILocalizationService localization;
    private readonly AppearancePreferenceService? appearance;
    private readonly IGridClipboardService clipboard;
    private readonly TextBlock stateText = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly ScrollViewer scroller = new() { HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
    private EffectiveAuthorizationContext? authorization;
    private GridProviderContext? context;
    private TextBox? activeEditor;
    private CancellationTokenSource? viewportNavigation;
    private CancellationTokenSource? viewportResize;
    private bool scrollRequestPending;
    private DateTimeOffset scrollRequestsEnabledAt;

    public DataEntryGridHost(DataEntryGridRuntime runtime, ILocalizationService localization,
        AppearancePreferenceService? appearance = null, IGridClipboardService? clipboard = null)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.appearance = appearance;
        this.clipboard = clipboard ?? new AvaloniaGridClipboardService(this);
        MinHeight = 320;
        runtime.Changed += (_, args) => Dispatcher.UIThread.Post(() =>
        {
            if (args.Reason != "EDIT_CANDIDATE" || activeEditor is null) Rebuild();
            Changed?.Invoke(this, EventArgs.Empty);
        });
        localization.CultureChanged += (_, _) => Rebuild();
        if (appearance is not null) appearance.PreferencesChanged += (_, _) =>
        {
            Rebuild();
            QueueViewportResize();
        };
        scroller.ScrollChanged += (_, _) => QueueNextViewportFromScroll();
        SizeChanged += (_, _) => QueueViewportResize();
        KeyDown += HandleKeyDown;
        Rebuild();
    }

    public DataEntryGridRuntime Runtime => runtime;
    public int RenderedColumnCount => runtime.ResolvedDefinition.Columns.Count(x => x.IsVisible);
    public int RenderedRowCount => runtime.Rows.Length;
    public bool HasActiveEditor => activeEditor is not null;
    public event EventHandler? Changed;

    public Task LoadAsync(GridProviderContext providerContext, EffectiveAuthorizationContext? effectiveAuthorization,
        CancellationToken cancellationToken = default)
    {
        CancelViewportResize();
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
        if (scroller.Parent is Panel previousPanel) previousPanel.Children.Remove(scroller);
        else if (ReferenceEquals(Content, scroller)) Content = null;
        if (runtime.State != GridProviderState.Ready && runtime.Rows.Length == 0)
        {
            stateText.Text = runtime.State switch
            {
                GridProviderState.Loading => localization.Get(new("Grid.State.Loading")),
                GridProviderState.Empty => localization.Get(runtime.Definition.EmptyStateKey),
                GridProviderState.Error => localization.Get(new("Grid.State.Error")),
                GridProviderState.Unavailable => localization.Get(new("Grid.State.Unavailable")),
                _ => localization.Get(new("Grid.State.Empty")),
            };
            AutomationProperties.SetName(stateText, stateText.Text);
            Content = stateText;
            return;
        }

        var columns = runtime.ResolvedDefinition.Columns.Where(x => x.IsVisible).ToArray();
        var scale = appearance?.Current.UiScale ?? 1d;
        var stack = new StackPanel { Spacing = 0 };
        stack.Children.Add(BuildHeader(columns, scale));
        for (var index = 0; index < runtime.Rows.Length; index++)
            stack.Children.Add(BuildRow(runtime.Rows[index], index, columns, scale));
        scroller.Content = stack;
        scroller.Offset = new Vector(scroller.Offset.X, 0);
        var rebuiltGuard = DateTimeOffset.UtcNow.AddMilliseconds(250);
        if (scrollRequestsEnabledAt < rebuiltGuard) scrollRequestsEnabledAt = rebuiltGuard;
        if (!runtime.IsVirtualized) { Content = scroller; return; }
        var layout = new DockPanel();
        var navigation = BuildViewportNavigation(scale);
        DockPanel.SetDock(navigation, Dock.Top); layout.Children.Add(navigation); layout.Children.Add(scroller);
        Content = layout;
    }

    private Control BuildHeader(IReadOnlyList<ResolvedGridColumn> columns, double scale)
    {
        var grid = CreateColumns(columns, scale);
        grid.MinHeight = 38 * scale;
        var columnOffset = AddRowNumberHeader(grid, scale);
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
            button.Click += async (_, _) =>
            {
                var current = runtime.Sorts.FirstOrDefault(x => x.VariableCode == column.Definition.VariableCode);
                var direction = current?.Direction == GridSortDirection.Ascending ? GridSortDirection.Descending : GridSortDirection.Ascending;
                await SortAsync(column.Definition.VariableCode, direction);
            };
            Grid.SetColumn(button, index + columnOffset); grid.Children.Add(button);
        }
        return grid;
    }

    private Control BuildRow(GridRow row, int localIndex, IReadOnlyList<ResolvedGridColumn> columns, double scale)
    {
        var rowGrid = CreateColumns(columns, scale);
        rowGrid.MinHeight = DensityHeight(scale);
        var columnOffset = AddRowNumber(rowGrid, runtime.ViewportStartIndex + localIndex + 1, scale);
        for (var index = 0; index < columns.Count; index++)
        {
            var cell = BuildCell(row, runtime.ViewportStartIndex + localIndex, columns[index], scale);
            Grid.SetColumn(cell, index + columnOffset); rowGrid.Children.Add(cell);
        }
        var border = new Border { Child = rowGrid, BorderThickness = new Thickness(0, 0, 0, 1), Focusable = true,
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
        AutomationProperties.SetName(border, $"{localization.Get(new("Grid.Row"))} {row.RowKey}");
        return border;
    }

    private Control BuildCell(GridRow row, int logicalRowPosition, ResolvedGridColumn column, double scale)
    {
        var isEditing = runtime.EditBuffer is { } edit && edit.RowKey == row.RowKey && edit.VariableCode == column.Definition.VariableCode;
        Control content;
        if (isEditing)
        {
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
            var text = new TextBlock { Text = diagnostic is null ? Format(value, column.Definition) : "⚠ —",
                TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            content = text;
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
        AutomationProperties.SetName(border, $"{localization.Get(new(column.Definition.DisplayNameKey))}, {mode}");
        border.PointerPressed += (_, args) =>
        {
            runtime.SelectCell(address, logicalRowPosition, args.KeyModifiers.HasFlag(KeyModifiers.Shift),
                HasPrimaryModifier(args.KeyModifiers));
            border.Focus();
            args.Handled = true;
        };
        border.DoubleTapped += (_, _) => BeginEdit(row.RowKey, column.Definition.VariableCode);
        return border;
    }

    private Grid CreateColumns(IEnumerable<ResolvedGridColumn> columns, double scale)
    {
        var grid = new Grid();
        if (runtime.Definition.ShowRowNumbers)
            grid.ColumnDefinitions.Add(new global::Avalonia.Controls.ColumnDefinition(new GridLength(72 * scale, GridUnitType.Pixel)));
        foreach (var column in columns)
            grid.ColumnDefinitions.Add(new global::Avalonia.Controls.ColumnDefinition(new GridLength((double)column.Width * scale, GridUnitType.Pixel))
            {
                MinWidth = (double)column.MinWidth * scale,
                MaxWidth = (double)column.MaxWidth * scale,
            });
        return grid;
    }

    private int AddRowNumberHeader(Grid grid, double scale)
    {
        if (!runtime.Definition.ShowRowNumbers) return 0;
        var label = new TextBlock { Text = "№", Margin = new Thickness(8 * scale, 4 * scale),
            VerticalAlignment = VerticalAlignment.Center };
        grid.Children.Add(label); return 1;
    }

    private int AddRowNumber(Grid grid, int logicalPosition, double scale)
    {
        if (!runtime.Definition.ShowRowNumbers) return 0;
        var label = new TextBlock { Text = logicalPosition.ToString("N0", CultureInfo.CurrentCulture),
            Margin = new Thickness(8 * scale, 3 * scale), VerticalAlignment = VerticalAlignment.Center };
        AutomationProperties.SetName(label, $"{localization.Get(new("Grid.Row"))} {logicalPosition}");
        grid.Children.Add(label); return 1;
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
        if (args.Key == Key.Escape && runtime.EditBuffer is not null) { CancelEdit(); args.Handled = true; return; }
        var primary = HasPrimaryModifier(args.KeyModifiers);
        if (primary)
        {
            if (args.Key == Key.C) await runtime.CopyAsync(clipboard);
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
        if (args.Key is Key.Delete or Key.Back)
        {
            await runtime.ClearSelectedCellsAsync(); handled = true;
        }
        else if (args.Key == Key.Enter && runtime.ActiveCell is { } active)
        {
            if (runtime.EditBuffer is null) BeginEdit(active.RowKey, active.VariableCode);
            handled = true;
        }
        else if (args.Key == Key.Escape && runtime.CellSelection.HasCellSelection)
        {
            runtime.ClearCellSelection(); handled = true;
        }
        args.Handled = handled;
    }

    private static bool HasPrimaryModifier(KeyModifiers modifiers) =>
        modifiers.HasFlag(KeyModifiers.Control) || modifiers.HasFlag(KeyModifiers.Meta);
}
