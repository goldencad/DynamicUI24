# Large-data DataEntry grids

The Demo exposes 100,000 logical rows without constructing a 100,000-row collection. It computes values only for the requested range, reports generated-row/request counters, supports a jump near row 90,000, and keeps the runtime window/cache bounded.

`TotalRows` and `VisibleRows` are logical counts for the provider's current filtered sequence. `Rows.Length` is materialized count. Bottom Action Bar status therefore shows the logical count, while the host's navigation summary shows the current materialized logical range. Pending changes come from the edit buffer; no provider scan is needed.

Sorting or filtering resets the viewport to logical position zero and invalidates cached mappings. Refresh invalidates and reloads only the current visible request. Theme/language changes only rebuild labels and cells. Resize, UI scale, and density recalculate visible capacity and request another bounded window; selection and edit identity remain unchanged.

Selection that leaves a window remains latent. Filtering may leave a selected RowKey latent when it is absent from the filtered result; this policy avoids rewriting identity as an index. A Company change clears selection/edit state because their provider context is no longer valid. Workspace deactivation cancels requests and releases windows.

Task 10B deliberately does not implement a perfect native 100K item scrollbar by creating 100K proxy objects. `DataEntryGridHost` uses a logical slider, previous/next controls, and end-of-window scroll loading over its bounded row stack. A future recycling data-source adapter can replace this presentation mechanism without changing Core contracts.
