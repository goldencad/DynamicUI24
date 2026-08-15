# Tree navigation

Every reusable tree uses a full-width row surface with fixed padding, border thickness, minimum height, and rounded corners. Shared semantic tokens define Normal, Hover, Selected, Selected+Hover, Disabled, and KeyboardFocus. Hover never changes row dimensions. Disabled nodes remain visible but cannot navigate; selection and expand/collapse remain independent.

`TreeOverflowOptions(initialVisibleChildCount, pageSize, showLess)` bounds large child collections. `DynamicTreeHost` inserts localized See more / Xem thêm and, where enabled, Show less / Thu gọn rows without flattening hierarchy. Paging occurs after permission/capability and Company filtering. Each parent owns its own child window.

Programmatic selection reveals a selected child outside the initial window. Expansion, selection, per-parent page size, and scroll offset remain stable during paging and presentation rerenders. Localization and Light/Dark/System changes update labels/tokens while keeping technical identity and state.

Applications configure tree metadata and overflow options; they do not restyle individual consumer rows. Maintainers must extend `TreeRowVisualStateResolver`, semantic tokens, and `DynamicTreeHost` together so the global tree, Setup, overflow rows, mouse, and keyboard remain consistent.
