using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

public interface IGridClipboardService
{
    Task<string?> ReadTextAsync(CancellationToken cancellationToken = default);
    Task WriteTextAsync(string text, CancellationToken cancellationToken = default);
}

public sealed record ClipboardMatrix
{
    private ClipboardMatrix(ImmutableArray<ImmutableArray<string>> rows)
    {
        Rows = rows;
        ColumnCount = rows.IsDefaultOrEmpty ? 0 : rows.Max(x => x.Length);
    }

    public ImmutableArray<ImmutableArray<string>> Rows { get; }
    public int RowCount => Rows.Length;
    public int ColumnCount { get; }
    public bool IsEmpty => RowCount == 0 || ColumnCount == 0;

    public string this[int row, int column] => column < Rows[row].Length ? Rows[row][column] : string.Empty;

    public static ClipboardMatrix Parse(string? text)
    {
        if (string.IsNullOrEmpty(text)) return new([]);
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        if (normalized.EndsWith('\n')) normalized = normalized[..^1];
        if (normalized.Length == 0) return new([]);
        var rows = normalized.Split('\n').Select(row => row.Split('\t').ToImmutableArray()).ToImmutableArray();
        return new(rows);
    }

    public static ClipboardMatrix FromRows(IEnumerable<IEnumerable<string?>> rows) => new(rows
        .Select(row => row.Select(value => value ?? string.Empty).ToImmutableArray()).ToImmutableArray());
}

public static class GridClipboardText
{
    public static string Serialize(IEnumerable<IEnumerable<object?>> rows, IReadOnlyList<ColumnDefinition> columns,
        CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(columns);
        culture ??= CultureInfo.CurrentCulture;
        var builder = new StringBuilder();
        var rowIndex = 0;
        foreach (var row in rows)
        {
            if (rowIndex++ > 0) builder.Append('\n');
            var values = row.ToArray();
            for (var column = 0; column < values.Length; column++)
            {
                if (column > 0) builder.Append('\t');
                builder.Append(Format(values[column], column < columns.Count ? columns[column] : null, culture)
                    .Replace('\t', ' ').Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Replace('\n', ' '));
            }
        }
        return builder.ToString();
    }

    public static string Format(object? value, ColumnDefinition? column, CultureInfo culture) => value switch
    {
        null => string.Empty,
        bool boolean => boolean ? "TRUE" : "FALSE",
        DateOnly date => date.ToString(column?.Format ?? "yyyy-MM-dd", culture),
        DateTime dateTime => dateTime.ToString(column?.Format ?? "yyyy-MM-dd HH:mm:ss", culture),
        IFormattable formattable => formattable.ToString(column?.Format, culture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty,
    };
}
