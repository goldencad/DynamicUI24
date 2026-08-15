# Split navigation layout

The standard desktop layout has a left navigation/tree pane, a draggable splitter, and a right definition-list/editor or main workspace. `DynamicSplitNavigationHost` accepts both content instances and `SplitNavigationLayoutState(defaultWidth, minWidth, maxWidth, splitterWidth)`.

Dragging or calling the runtime resize API clamps the left pane to its minimum and maximum. The right pane consumes remaining space. Task 8 does not persist width. Resize changes only the column width: selection, expansion, scroll, candidate edits, Company context, language, theme, and workspace instances remain alive.

Setup uses this generic primitive. Future templates should reuse it for the same navigation/workspace relationship instead of cloning the Grid/Splitter logic. Maintainers must keep sizing UI-neutral in the layout state, preserve content instances during resize, and add persistence only through a separate optional consumer-owned service in a later contract.
