namespace DynamicUI24.ArchitectureTests;

using Xunit;

public sealed class DataEntryGridArchitectureTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void CoreGridContractsArePlatformAndBusinessNeutral()
    {
        var source = ReadDirectory("src/DynamicUI24.Core/DataEntry");
        Assert.DoesNotContain("Avalonia", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayCalc24", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Odoo", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TreeHost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RibbonHost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OperatingSystem", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GridReusesColumnDefinitionAndVariableCodeContracts()
    {
        var definitions = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridDefinitions.cs"));
        var provider = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridProvider.cs"));
        Assert.Contains("IEnumerable<ColumnDefinition>", definitions, StringComparison.Ordinal);
        Assert.Contains("VariableCode", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("class GridColumnDefinition", definitions, StringComparison.Ordinal);
    }

    [Fact]
    public void ProvidersExposeOptionalAsyncViewportCapabilityWithoutBreakingSmallDataContract()
    {
        var provider = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridProvider.cs"));
        var viewport = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridViewport.cs"));
        Assert.Contains("Task<GridDataResult> LoadAsync", provider, StringComparison.Ordinal);
        Assert.Contains("IVirtualizedGridDataProvider : IDataEntryGridProvider", provider, StringComparison.Ordinal);
        Assert.Contains("Task<GridViewportResult> LoadViewportAsync", provider, StringComparison.Ordinal);
        Assert.Contains("GridViewportRequest", viewport, StringComparison.Ordinal);
        Assert.Contains("GridViewportResult", viewport, StringComparison.Ordinal);
        Assert.Contains("CancellationToken", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("Avalonia", viewport, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WindowRuntimeUsesBoundedCacheGenerationAndRowKeyIdentity()
    {
        var viewport = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridViewport.cs"));
        var runtime = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRuntime.cs"));
        Assert.Contains("MaximumCachedWindows", viewport, StringComparison.Ordinal);
        Assert.Contains("MaximumMaterializedRows", viewport, StringComparison.Ordinal);
        Assert.Contains("GridWindowCache", runtime, StringComparison.Ordinal);
        Assert.Contains("RequestGeneration", runtime, StringComparison.Ordinal);
        Assert.Contains("SelectedRowKeys", runtime, StringComparison.Ordinal);
        Assert.Contains("GridEditBuffer", runtime, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedIndexes", runtime, StringComparison.Ordinal);
    }

    [Fact]
    public void LargeDataDemoGeneratesOnlyRequestedRowsAndContainsNoApplicationBusinessIntegration()
    {
        var source = File.ReadAllText(Path("samples/DynamicUI24.Demo/DemoDataEntry.cs"));
        Assert.Contains("LogicalRowCount = 100_000", source, StringComparison.Ordinal);
        Assert.Contains("request.MaterializedRowCount", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Enumerable.Range(1, 100_000)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PayCalc24", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Odoo", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RendererUsesExistingSharedFoundationsAndNoDirectSvgPaths()
    {
        var source = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("AppearancePreferenceService", source, StringComparison.Ordinal);
        Assert.Contains("ILocalizationService", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".svg", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#", source, StringComparison.Ordinal);
    }

    [Fact]
    public void NoFormulaExecutionOrImportExportEngineWasAdded()
    {
        var source = ReadDirectory("src/DynamicUI24.Core/DataEntry");
        Assert.DoesNotContain("ExpressionText", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CSharpScript", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Csv", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Excel", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormulaMarkerIsPresentationOnlyAndSemanticModeRemainsAuthoritative()
    {
        var definitions = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridDefinitions.cs"));
        var host = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("Definition.Mode == ColumnMode.Formula", definitions, StringComparison.Ordinal);
        Assert.Contains("IsFormulaDerived", host, StringComparison.Ordinal);
        Assert.Contains("Text = \"fx\"", host, StringComparison.Ordinal);
        Assert.DoesNotContain("FormulaExpression", host, StringComparison.Ordinal);
        Assert.DoesNotContain("EvaluateFormula", host, StringComparison.Ordinal);
    }

    [Fact]
    public void RangeClipboardAndTransactionsStaySemanticPlatformFreeAndBounded()
    {
        var selection = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridSelection.cs"));
        var clipboard = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridClipboard.cs"));
        var editing = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridEditing.cs"));
        var runtime = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRuntimeEditing.cs"));
        Assert.Contains("GridCellAddress(RowKey RowKey, VariableCode VariableCode)", selection, StringComparison.Ordinal);
        Assert.Contains("ImmutableArray<GridCellRange>", selection, StringComparison.Ordinal);
        Assert.Contains("IsAllSelected", selection, StringComparison.Ordinal);
        Assert.DoesNotContain("Control", selection, StringComparison.Ordinal);
        Assert.Contains("IGridClipboardService", clipboard, StringComparison.Ordinal);
        Assert.DoesNotContain("Avalonia", clipboard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IGridBatchEditProvider", editing, StringComparison.Ordinal);
        Assert.Contains("GridEditTransaction", editing, StringComparison.Ordinal);
        Assert.Contains("HistoryDepth", editing, StringComparison.Ordinal);
        Assert.Contains("IGridLogicalRowProvider", runtime, StringComparison.Ordinal);
        Assert.DoesNotContain("OperatingSystem", runtime, StringComparison.Ordinal);
    }

    [Fact]
    public void PercentageSizingAndPointerSelectionRemainSemanticBoundedAndPlatformSeparated()
    {
        var heights = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRowHeights.cs"));
        var editing = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRuntimeEditing.cs"));
        var host = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("Dictionary<RowKey", heights, StringComparison.Ordinal);
        Assert.Contains("MaximumOverrides", heights, StringComparison.Ordinal);
        Assert.DoesNotContain("Avalonia", heights, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SelectRange(GridRangeEndpoint anchor", editing, StringComparison.Ordinal);
        Assert.DoesNotContain("Pointer", editing, StringComparison.Ordinal);
        Assert.DoesNotContain("StandardCursorType.SizeNorthSouth", host, StringComparison.Ordinal);
        Assert.DoesNotContain("StandardCursorType.SizeWestEast", host, StringComparison.Ordinal);
        Assert.DoesNotContain("GridSplitter", host, StringComparison.Ordinal);
        Assert.DoesNotContain("ResizeDirection", host, StringComparison.Ordinal);
        Assert.DoesNotContain("ColumnDefinitions[index + columnOffset].ActualWidth", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Post(() => Focus())", host, StringComparison.Ordinal);
        Assert.Contains("expandedCell.Opened", host, StringComparison.Ordinal);
        Assert.Contains("TextInput += HandleTextInput", host, StringComparison.Ordinal);
        Assert.Contains("Key.Delete or Key.Back or Key.Clear", host, StringComparison.Ordinal);
    }

    [Fact]
    public void PlatformClipboardAndShortcutMappingRemainInPresentationLayer()
    {
        var bridge = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/AvaloniaGridClipboardService.cs"));
        var host = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("TopLevel.GetTopLevel", bridge, StringComparison.Ordinal);
        Assert.Contains("KeyModifiers.Meta", host, StringComparison.Ordinal);
        Assert.Contains("KeyModifiers.Control", host, StringComparison.Ordinal);
        Assert.Contains("DuiSelectionBrush", host, StringComparison.Ordinal);
    }

    [Fact]
    public void CellCommitUsesSemanticTargetedPresenterInvalidationWithoutGridRebuild()
    {
        var runtime = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRuntime.cs"));
        var host = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("OnChanged(\"EDIT_COMMIT\", new(buffer.RowKey, buffer.VariableCode))", runtime, StringComparison.Ordinal);
        Assert.Contains("RefreshMaterializedCell(committedCell)", host, StringComparison.Ordinal);
        Assert.Contains("runtime.GetValue(address.RowKey, address.VariableCode", host, StringComparison.Ordinal);
        Assert.DoesNotContain("args.Reason == \"EDIT_COMMIT\") Rebuild", host, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpandedCellEditorIsLightweightAndHasNoFormActionButtons()
    {
        var host = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("expandedCell.Content = expandedValue", host, StringComparison.Ordinal);
        Assert.Contains("expandedEditing && args.Key is Key.Enter or Key.Tab", host, StringComparison.Ordinal);
        Assert.Contains("args.Key == Key.Escape", host, StringComparison.Ordinal);
        Assert.DoesNotContain("expandedSave", host, StringComparison.Ordinal);
        Assert.DoesNotContain("expandedCancel", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Content = \"Done\"", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Content = \"Apply\"", host, StringComparison.Ordinal);
        Assert.DoesNotContain("expandedCell.Content = new Border", host, StringComparison.Ordinal);
        Assert.Contains("pointerSelecting = false;", host, StringComparison.Ordinal);
        Assert.Contains("AddHandler(KeyDownEvent, HandleKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true)",
            host, StringComparison.Ordinal);
        Assert.Contains("expandedValue.IsKeyboardFocusWithin", host, StringComparison.Ordinal);
        Assert.Contains("InputMethod.SetIsInputMethodEnabled(expandedValue, true)", host, StringComparison.Ordinal);
        Assert.DoesNotContain("expandedValue.TextChanged", host, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeTextInputRemainsPresentationOwnedAndLanguageNeutral()
    {
        var host = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        var core = ReadDirectory("src/DynamicUI24.Core/DataEntry");
        Assert.Contains("InputMethod.SetIsInputMethodEnabled", host, StringComparison.Ordinal);
        Assert.Contains("expandedValue.IsKeyboardFocusWithin", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Vietnamese", core, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Pinyin", core, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Transliterate", core, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Encoding.Default", core, StringComparison.Ordinal);
    }

    [Fact]
    public void PercentageSizingUsesExistingSemanticPreferenceAndSharedGeometryPath()
    {
        var preference = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridPersonalization.cs"));
        var runtime = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRuntimePersonalization.cs"));
        var host = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("WidthScalePercent", preference, StringComparison.Ordinal);
        Assert.Contains("RowHeightScalePercent", preference, StringComparison.Ordinal);
        Assert.Contains("SetColumnWidthPercentage(VariableCode", runtime, StringComparison.Ordinal);
        Assert.Contains("var grid = CreateColumns(columns, scale);", host, StringComparison.Ordinal);
        Assert.Contains("ResolveRowHeight(row.RowKey", host, StringComparison.Ordinal);
        Assert.DoesNotContain("PayCalc24", preference, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Avalonia", preference, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RowLifecycleIsProviderOwnedRowKeySemanticAndDoesNotMutateColumnSchema()
    {
        var contract = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRowLifecycle.cs"));
        var runtime = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRuntimeRowLifecycle.cs"));
        var host = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("IGridRowLifecycleProvider", contract, StringComparison.Ordinal);
        Assert.Contains("RowKey AnchorRowKey", contract, StringComparison.Ordinal);
        Assert.Contains("InsertedRowKey", contract, StringComparison.Ordinal);
        Assert.Contains("IGridRowCalculationInvalidation", contract, StringComparison.Ordinal);
        Assert.Contains("Insert Row Above", host, StringComparison.Ordinal);
        Assert.Contains("Delete Selected Rows", host, StringComparison.Ordinal);
        Assert.Contains("ROWS_DELETED", host, StringComparison.Ordinal);
        Assert.Contains("Cmd/Ctrl+Shift+↑", host, StringComparison.Ordinal);
        Assert.Contains("Cmd/Ctrl+Shift+↓", host, StringComparison.Ordinal);
        Assert.Contains("Cmd/Ctrl+Delete", host, StringComparison.Ordinal);
        Assert.Contains("MessageKind.Confirmation", host, StringComparison.Ordinal);
        Assert.Contains("MessageResult.Confirmed", host, StringComparison.Ordinal);
        Assert.Contains("PrepareCellContext(address, logicalRowPosition)", host, StringComparison.Ordinal);
        Assert.Contains("BuildColumnWidthMenu(address.VariableCode)", host, StringComparison.Ordinal);
        Assert.Contains("BuildRowHeightMenu()", host, StringComparison.Ordinal);
        Assert.Contains("OpenExpandedCell(anchor, address, column)", host, StringComparison.Ordinal);
        Assert.Contains("CutSelectionAsync()", host, StringComparison.Ordinal);
        Assert.Contains("PasteSelectionAsync()", host, StringComparison.Ordinal);
        Assert.DoesNotContain("InsertColumn", contract + runtime + host, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteColumn", contract + runtime + host, StringComparison.Ordinal);
        Assert.DoesNotContain("Avalonia", contract + runtime, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("VisualRowIndex", contract + runtime, StringComparison.Ordinal);
    }

    [Fact]
    public void GridFindIsProviderOwnedSemanticBoundedAndSeparateFromGlobalSearch()
    {
        var contract = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridFind.cs"));
        var runtime = File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRuntimeFind.cs"));
        var host = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("IGridFindProvider", contract, StringComparison.Ordinal);
        Assert.Contains("RowKey? RowKey", contract, StringComparison.Ordinal);
        Assert.Contains("VariableCode? VariableCode", contract, StringComparison.Ordinal);
        Assert.Contains("RequestGeneration", contract, StringComparison.Ordinal);
        Assert.Contains("RequestViewportAsync", runtime, StringComparison.Ordinal);
        Assert.Contains("GRID_STALE_FIND_RESULT", runtime, StringComparison.Ordinal);
        Assert.Contains("Find in Column…", host, StringComparison.Ordinal);
        Assert.Contains("GridFindScope { CurrentRow, CurrentColumn, AllVisibleColumns }", contract, StringComparison.Ordinal);
        Assert.Contains("Find in Row…", host, StringComparison.Ordinal);
        Assert.Contains("Edit / View", host, StringComparison.Ordinal);
        Assert.Contains("CopyRowAsync(rowKey, clipboard)", host, StringComparison.Ordinal);
        Assert.Contains("ClearRowEditableValuesAsync(rowKey)", host, StringComparison.Ordinal);
        Assert.Contains("RememberFindScope", runtime + File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRuntimePersonalization.cs")), StringComparison.Ordinal);
        Assert.Contains("FindScope = scope", File.ReadAllText(Path("src/DynamicUI24.Core/DataEntry/GridRuntimePersonalization.cs")), StringComparison.Ordinal);
        Assert.Contains("Content = \"⌄\"", host, StringComparison.Ordinal);
        Assert.Contains("args.Key == Key.F", host, StringComparison.Ordinal);
        Assert.DoesNotContain("DynamicUI24.Core.Search", contract + runtime, StringComparison.Ordinal);
        Assert.DoesNotContain("Avalonia", contract + runtime, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TextBlock", contract + runtime, StringComparison.Ordinal);
    }

    [Fact]
    public void PermanentRowHeaderIsFrozenPresentationOnlyAndMaterializedByRowKey()
    {
        var host = File.ReadAllText(Path("src/DynamicUI24.Avalonia/Presentation/DataEntryGridHost.cs"));
        Assert.Contains("rowHeaderScroller", host, StringComparison.Ordinal);
        Assert.Contains("Grid.SetColumn(rowHeaderScroller, 0)", host, StringComparison.Ordinal);
        Assert.Contains("new Vector(0, scroller.Offset.Y)", host, StringComparison.Ordinal);
        Assert.Contains("BuildRowHeader(runtime.Rows[index].RowKey", host, StringComparison.Ordinal);
        Assert.Contains("runtime.ViewportStartIndex + index + 1", host, StringComparison.Ordinal);
        Assert.Contains("BuildRowMenu(rowKey)", host, StringComparison.Ordinal);
        Assert.Contains("BuildRowHeightMenu()", host, StringComparison.Ordinal);
        Assert.Contains("BuildColumnWidthMenu(code)", host, StringComparison.Ordinal);
        Assert.Contains("Insert Row Above", host, StringComparison.Ordinal);
        Assert.Contains("Find in Row…", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Text = \"№\"", host, StringComparison.Ordinal);
        Assert.Contains("Grid corner header", host, StringComparison.Ordinal);
        Assert.DoesNotContain("if (runtime.Definition.ShowRowNumbers)", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new VariableCode(\"ROW", host, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadDirectory(string relative) => string.Join('\n',
        Directory.EnumerateFiles(Path(relative), "*.cs", SearchOption.AllDirectories).OrderBy(x => x).Select(File.ReadAllText));
    private static string Path(string relative) => System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(System.IO.Path.Combine(directory.FullName, "DynamicUI24.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
