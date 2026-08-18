using DynamicUI24.Core.Editors;
using DynamicUI24.Core.Setup;

namespace DynamicUI24.Core.DataEntry;

/// <summary>Maps Task 10F metadata to the shared editor model; the grid retains transaction authority.</summary>
public static class GridEditorDefinitionAdapter
{
    public static EditorDefinition Create(string sheetCode, ColumnDefinition column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetCode); ArgumentNullException.ThrowIfNull(column);
        var valueType = column.DataType switch
        {
            ColumnDataType.MultilineText => EditorValueType.LongString, ColumnDataType.Integer => EditorValueType.Integer,
            ColumnDataType.Decimal => EditorValueType.Decimal, ColumnDataType.Boolean => EditorValueType.Boolean,
            ColumnDataType.Date => EditorValueType.Date, ColumnDataType.DateTime => EditorValueType.DateTime,
            ColumnDataType.Choice => EditorValueType.Choice, ColumnDataType.Reference => EditorValueType.LookupKey,
            _ => EditorValueType.String,
        };
        var explicitKind = column.EditorKind switch
        {
            ColumnEditorKind.TextBox when valueType == EditorValueType.LongString => EditorKind.MultilineText,
            ColumnEditorKind.TextBox => EditorKind.Text,
            ColumnEditorKind.Number when valueType == EditorValueType.Integer => EditorKind.Integer,
            ColumnEditorKind.Number => EditorKind.Decimal, ColumnEditorKind.Checkbox => EditorKind.Boolean,
            ColumnEditorKind.DatePicker when valueType == EditorValueType.DateTime => EditorKind.DateTime,
            ColumnEditorKind.DatePicker => EditorKind.Date, ColumnEditorKind.ComboBox => EditorKind.Choice,
            ColumnEditorKind.Lookup => EditorKind.Lookup, _ => (EditorKind?)null,
        };
        return new(new($"DATAENTRY.{column.ColumnCode}"), new($"{sheetCode}:{column.VariableCode.Value}"),
            valueType, explicitKind, chrome: new(new(column.DisplayNameKey)), formatting: new(column.Format),
            validation: new(column.IsRequired), sensitiveContent: column.SensitiveContent,
            isReadOnly: column.Mode != ColumnMode.Input);
    }
}
