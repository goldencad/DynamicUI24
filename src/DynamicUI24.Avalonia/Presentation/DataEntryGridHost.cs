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
    private readonly TextBlock stateText = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly ScrollViewer scroller = new() { HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
    private EffectiveAuthorizationContext? authorization;
    private GridProviderContext? context;
    private TextBox? activeEditor;

    public DataEntryGridHost(DataEntryGridRuntime runtime, ILocalizationService localization,
        AppearancePreferenceService? appearance = null)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        this.appearance = appearance;
        MinHeight = 320;
        runtime.Changed += (_, args) => Dispatcher.UIThread.Post(() =>
        {
            if (args.Reason != "EDIT_CANDIDATE" || activeEditor is null) Rebuild();
            Changed?.Invoke(this, EventArgs.Empty);
        });
        localization.CultureChanged += (_, _) => Rebuild();
        if (appearance is not null) appearance.PreferencesChanged += (_, _) => Rebuild();
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
    public Task SortAsync(VariableCode variableCode, GridSortDirection direction, CancellationToken cancellationToken = default) =>
        runtime.SetSortAsync([new(variableCode, direction)], authorization, cancellationToken);
    public Task FilterAsync(GridFilterDefinition filter, CancellationToken cancellationToken = default) =>
        runtime.SetFiltersAsync([filter], authorization, cancellationToken);
    public Task ClearFilterAsync(CancellationToken cancellationToken = default) =>
        runtime.SetFiltersAsync([], authorization, cancellationToken);
    public Task RefreshAsync(CancellationToken cancellationToken = default) => context is null
        ? Task.CompletedTask : runtime.LoadAsync(context, authorization, cancellationToken);

    private void Rebuild()
    {
        activeEditor = null;
        if (runtime.State != GridProviderState.Ready)
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
        foreach (var row in runtime.Rows) stack.Children.Add(BuildRow(row, columns, scale));
        scroller.Content = stack;
        Content = scroller;
    }

    private Control BuildHeader(IReadOnlyList<ResolvedGridColumn> columns, double scale)
    {
        var grid = CreateColumns(columns, scale);
        grid.MinHeight = 38 * scale;
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
            Grid.SetColumn(button, index); grid.Children.Add(button);
        }
        return grid;
    }

    private Control BuildRow(GridRow row, IReadOnlyList<ResolvedGridColumn> columns, double scale)
    {
        var rowGrid = CreateColumns(columns, scale);
        rowGrid.MinHeight = DensityHeight(scale);
        for (var index = 0; index < columns.Count; index++)
        {
            var cell = BuildCell(row, columns[index], scale);
            Grid.SetColumn(cell, index); rowGrid.Children.Add(cell);
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

    private Control BuildCell(GridRow row, ResolvedGridColumn column, double scale)
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
        var border = new Border { Child = content, Padding = new Thickness(8 * scale, 3 * scale),
            BorderThickness = new Thickness(0, 0, 1, 0), Tag = column.Definition.VariableCode };
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("DuiBorderBrush"));
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable(column.CanEdit && runtime.ResolvedDefinition.CanEdit
            ? "DuiSurfaceBrush" : "DuiSurfaceRaisedBrush"));
        var mode = column.CanEdit && runtime.ResolvedDefinition.CanEdit ? localization.Get(new("Grid.Editable")) : localization.Get(new("Grid.ReadOnly"));
        AutomationProperties.SetName(border, $"{localization.Get(new(column.Definition.DisplayNameKey))}, {mode}");
        border.DoubleTapped += (_, _) => BeginEdit(row.RowKey, column.Definition.VariableCode);
        return border;
    }

    private static Grid CreateColumns(IEnumerable<ResolvedGridColumn> columns, double scale)
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

    private void HandleKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape && runtime.EditBuffer is not null) { CancelEdit(); args.Handled = true; return; }
        if (runtime.Rows.Length == 0 || args.Key is not (Key.Up or Key.Down or Key.Enter)) return;
        var index = runtime.SelectionCount == 0 ? 0 : Array.FindIndex(runtime.Rows.ToArray(), x => runtime.SelectedRowKeys.Contains(x.RowKey));
        if (args.Key == Key.Up) index = Math.Max(0, index - 1);
        if (args.Key == Key.Down) index = Math.Min(runtime.Rows.Length - 1, index + 1);
        runtime.Select([runtime.Rows[index].RowKey]);
        if (args.Key == Key.Enter)
        {
            var column = runtime.ResolvedDefinition.Columns.FirstOrDefault(x => runtime.CanEdit(runtime.Rows[index].RowKey, x.Definition.VariableCode));
            if (column is not null) BeginEdit(runtime.Rows[index].RowKey, column.Definition.VariableCode);
        }
        args.Handled = true;
    }
}
