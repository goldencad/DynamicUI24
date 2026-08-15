# Range selection

## What range selection owns

`GridSelectionState` owns active cell, anchor, compact rectangles, legacy selected row keys, selection mode, and symbolic select-all. A cell is always identified by `RowKey + VariableCode`; controls and recycled viewport indexes are never identities.

`GridRangeEndpoint.LogicalRowPosition` captures the current sorted/filtered order when the endpoint is selected. It is navigation context, not permanent identity. Materialized endpoints are re-positioned by `RowKey` after load/sort/filter. A virtual endpoint outside the active window keeps its semantic identity and captured position until the provider/window can resolve it again.

## Interaction policy

- Click activates one cell and resets the anchor.
- Shift+click or Shift+Arrow extends one rectangle from the anchor.
- Ctrl-click and Cmd-click add a compact range foundation; copy/paste use the primary (last) range in 10C.
- Arrow moves; Tab/Shift+Tab visits editable cells; Enter starts editing; Escape cancels edit, then cell selection.
- Ctrl/Cmd+A creates a symbolic whole-grid selection. It does not allocate per-cell state.

Selection can span unloaded rows. Rendering asks whether each materialized cell intersects a rectangle, so scroll recycling does not own selection state. Huge symbolic selections cannot be copied synchronously.

## What it does not own

No fill handle, series generation, grouping, formulas, layout persistence, collaborative selection, or application rules. Full non-contiguous range polish and header selection are tracked in the 10C backlog.

## Common failure modes

Do not retain cell controls, treat viewport indexes as identity, expand a range into cell objects, include hidden columns, or preserve stale positions when a known `RowKey` has moved.

Focused tests: `dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --filter FullyQualifiedName~GridEditingTests`.
