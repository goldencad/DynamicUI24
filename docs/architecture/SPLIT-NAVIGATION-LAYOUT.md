# Split navigation layout

The normative UX and extension rules are in the design-system [Split navigation layout standard](../design-system/SPLIT-NAVIGATION-LAYOUT.md).

`DynamicSplitNavigationHost` is a generic Avalonia two-pane primitive. The navigation content occupies the bounded left column, the workspace content occupies the right star-sized column, and a theme-aware `GridSplitter` between them supports desktop-style runtime resizing.

`SplitNavigationLayoutState` contains only runtime dimensions: initial width, minimum/maximum width, and splitter width. It clamps programmatic or drag-produced widths and deliberately has no file, settings, or preference persistence boundary in Task 8.

The host retains both content instances while changing only the left `ColumnDefinition.Width`. Therefore selection, edit buffers, Company context, localization/theme state, scroll state, and active workspace are not recreated during resize. Setup consumes the primitive; future templates can place any navigation and workspace controls into the same two slots.
