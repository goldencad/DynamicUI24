using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicUI24.Core.ImportExport;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

/// <summary>Reusable presentation host. Parsing, mapping and commits remain in Core services supplied by the app.</summary>
public sealed class ImportExportWorkspaceHost : UserControl
{
    private readonly ILocalizationService localization;
    private readonly TextBlock source = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock summary = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ProgressBar progress = new() { Minimum = 0, Maximum = 100, IsVisible = false };
    private readonly StackPanel mappings = new() { Spacing = 6 };
    private readonly ListBox preview = new() { MinHeight = 180 };
    private readonly ListBox diagnostics = new() { MinHeight = 100 };
    private readonly Button validate = new() { MinWidth = 100 };
    private readonly Button import = new() { MinWidth = 100 };
    private readonly Button cancel = new() { MinWidth = 100 };
    private readonly ObservableCollection<string> previewItems = [];
    private readonly ObservableCollection<string> diagnosticItems = [];

    public ImportExportWorkspaceHost(ILocalizationService localization)
    {
        this.localization = localization ?? throw new ArgumentNullException(nameof(localization));
        preview.ItemsSource = previewItems; diagnostics.ItemsSource = diagnosticItems;
        validate.Click += (_, _) => ValidateRequested?.Invoke(this, EventArgs.Empty);
        import.Click += (_, _) => ImportRequested?.Invoke(this, EventArgs.Empty);
        cancel.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
        Content = Build(); RefreshText(); localization.CultureChanged += (_, _) => RefreshText();
    }

    public event EventHandler? ValidateRequested;
    public event EventHandler? ImportRequested;
    public event EventHandler? CancelRequested;
    public event EventHandler<ImportMappingChangedEventArgs>? MappingChanged;

    public void ShowProfiles(IEnumerable<ImportDefinition> importDefinitions, IEnumerable<ExportDefinition>? exportDefinitions = null)
    {
        var imports = string.Join(" · ", importDefinitions.Select(x => $"{x.ParserCode} (*.{string.Join(", *.", x.FileExtensions)})"));
        var exports = string.Join(" · ", (exportDefinitions ?? []).Select(x => x.WriterCode));
        source.Text = $"Import: {imports}" + (string.IsNullOrEmpty(exports) ? string.Empty : $"\nExport: {exports}");
    }

    public void ShowSource(string sourceName, ImportDefinition definition, ImportSourceSchema schema,
        IEnumerable<ImportFieldMapping> currentMappings, IEnumerable<string> targetVariableCodes)
    {
        source.Text = $"{sourceName} · {definition.ImportCode} · {definition.ParserCode}";
        mappings.Children.Clear(); var selected = currentMappings.ToDictionary(x => x.SourceField, StringComparer.OrdinalIgnoreCase);
        foreach (var field in schema.Fields)
        {
            var selector = new ComboBox { MinWidth = 220, ItemsSource = new[] { "—" }.Concat(targetVariableCodes).ToArray(),
                SelectedItem = selected.TryGetValue(field.SourceFieldCode, out var mapping) ? mapping.TargetVariableCode.Value : "—",
                [AutomationProperties.NameProperty] = $"{field.DisplayName} target VariableCode" };
            selector.SelectionChanged += (_, _) => MappingChanged?.Invoke(this,
                new(field.SourceFieldCode, selector.SelectedItem?.ToString() is { } value && value != "—" ? value : null));
            var row = new Grid { ColumnDefinitions = new("*,Auto,*"), ColumnSpacing = 10 };
            var left = new TextBlock { Text = field.DisplayName, VerticalAlignment = VerticalAlignment.Center };
            var arrow = new TextBlock { Text = "→", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(arrow, 1); Grid.SetColumn(selector, 2); row.Children.Add(left); row.Children.Add(arrow); row.Children.Add(selector); mappings.Children.Add(row);
        }
    }

    public void ShowPreview(ImportPreviewResult result)
    {
        previewItems.Clear(); diagnosticItems.Clear();
        foreach (var row in result.Rows) previewItems.Add($"#{row.RecordIndex}: {string.Join(" · ", row.Values.Select(x => $"{x.Key}={x.Value}"))}");
        foreach (var item in result.Diagnostics) diagnosticItems.Add($"{item.Severity} · {item.Code} · #{item.RecordIndex?.ToString() ?? "—"} · {item.SafeMessage}");
        summary.Text = $"{result.RecordsExamined} records · {result.ValidRows} valid · {result.WarningRows} warnings · {result.InvalidRows} invalid · preview {result.MaterializedRowCount}/{result.MaxPreviewRows}";
        import.IsEnabled = result.InvalidRows == 0;
    }

    public void ReportProgress(ImportExportProgress value)
    { progress.IsVisible = true; progress.IsIndeterminate = value.Percentage is null; progress.Value = value.Percentage ?? 0; summary.Text = $"{value.Stage} · {value.ProcessedRecords:N0}"; }
    public void EndProgress() { progress.IsVisible = false; progress.IsIndeterminate = false; }

    private Control Build()
    {
        var split = new Grid { ColumnDefinitions = new("2*,3*"), ColumnSpacing = 14 };
        var left = new StackPanel { Spacing = 10, Children = { source, new ScrollViewer { Content = mappings, MaxHeight = 400 } } };
        var right = new StackPanel { Spacing = 10, Children = { summary, progress, preview, diagnostics } }; Grid.SetColumn(right, 1);
        split.Children.Add(left); split.Children.Add(right);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8,
            Children = { validate, import, cancel } };
        return new Grid { Margin = new Thickness(16), RowDefinitions = new("*,Auto"), RowSpacing = 12,
            Children = { split, Place(actions, 1) } };
    }
    private static Control Place(Control control, int row) { Grid.SetRow(control, row); return control; }
    private void RefreshText() { validate.Content = Text("Import.Validate", "Validate"); import.Content = Text("Import.Commit", "Import"); cancel.Content = Text("Common.Cancel", "Cancel"); }
    private string Text(string key, string fallback) { var value = localization.Get(new LocalizationKey(key)); return string.Equals(value, key, StringComparison.Ordinal) ? fallback : value; }
}

public sealed class ImportMappingChangedEventArgs(string sourceField, string? targetVariableCode) : EventArgs
{
    public string SourceField { get; } = sourceField;
    public string? TargetVariableCode { get; } = targetVariableCode;
}
