# DataEntry grid

Headers expose label plus sort, filter, and pin state to accessibility. Header overflow contains sorting, filter/clear, pin/unpin, hide, columns, width reset, and layout reset. Cell menus contain only eligible privacy/permission/editability-resolved commands.

Use progressive disclosure: compact active-filter/status indicators and secondary actions in overflow. Column chooser is a keyboard-operable checked list of authorized columns; mandatory columns remain checked and disabled. Never enumerate cell values in the chooser or restricted filter suggestions.

Widths use metadata min/max bounds and stored widths are clamped. Left-pinned width must leave a useful center viewport; reaching the deterministic budget disables further pinning. Narrow layouts keep horizontal scrolling and preserve active-state visibility.

Active cell uses a focus outline, range uses selection fill, and neither relies on color alone. Accessible cell text combines row context, column label, editability, and the privacy-presented value. Only materialized viewport rows produce accessibility nodes.
