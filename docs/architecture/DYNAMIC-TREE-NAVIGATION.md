# Dynamic Tree and Workspace Navigation

`TreeDefinition` is immutable, UI-neutral metadata. Nodes are stored flat with stable `NodeId` and optional `ParentNodeId`, permitting arbitrary depth while keeping validation and serialization simple. The validator rejects duplicate IDs, missing parents, self-parenting, and cycles. Siblings are ordered by `DisplayOrder`, then `NodeCode`, then `NodeId`.

`DynamicTreeResolver` applies the existing authorization presentation resolver and known workspace set. Hidden nodes are absent; disabled nodes remain visible but cannot navigate. Unknown workspace targets remain visible and safely non-navigable, with a diagnostic. Privileged nodes fail closed via the existing resolver.

The shell observes a shared workspace selection: tree selection changes the workspace, and ribbon-originated workspace changes select the matching tree node. On company re-resolution, the active workspace is preserved only when represented by a visible navigable node; otherwise the first visible navigable node in depth-first display order is selected. If none exists, the existing workspace is left untouched and the shell stays usable.

## Child overflow

`TreeOverflowOptions` configures the initial visible child count, incremental page size, and optional Show less behavior. `TreeOverflowController` keeps a separate child window per parent identity, so paging never flattens or reparents nodes. Permission/capability and Company-aware resolution runs before windowing.

`DynamicTreeHost` renders localized See more / Xem thêm and Show less / Thu gọn controls. It preserves selection, expands the window when a programmatic selection is outside the initial page, retains paging across Company/localization/theme rerenders, and restores the Tree scroll offset after a page change. Setup and future templates consume this shared host rather than implementing their own overflow list.
