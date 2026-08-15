# Dynamic Tree and Workspace Navigation

`TreeDefinition` is immutable, UI-neutral metadata. Nodes are stored flat with stable `NodeId` and optional `ParentNodeId`, permitting arbitrary depth while keeping validation and serialization simple. The validator rejects duplicate IDs, missing parents, self-parenting, and cycles. Siblings are ordered by `DisplayOrder`, then `NodeCode`, then `NodeId`.

`DynamicTreeResolver` applies the existing authorization presentation resolver and known workspace set. Hidden nodes are absent; disabled nodes remain visible but cannot navigate. Unknown workspace targets remain visible and safely non-navigable, with a diagnostic. Privileged nodes fail closed via the existing resolver.

The shell observes a shared workspace selection: tree selection changes the workspace, and ribbon-originated workspace changes select the matching tree node. On company re-resolution, the active workspace is preserved only when represented by a visible navigable node; otherwise the first visible navigable node in depth-first display order is selected. If none exists, the existing workspace is left untouched and the shell stays usable.
