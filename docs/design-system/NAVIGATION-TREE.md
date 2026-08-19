# Navigation Tree guidance

Normative authority: [v0.16 §12](../specification/DynamicUI24-Spec-v0.16.md#12-navigation-tree).

Use the framework tree host, semantic icon keys, and `NodeCode`/target identity. `NavigationTreePart` establishes the row, indentation, chevron, icon, typography, parent/leaf, badge, and context-action anatomy that 11B must standardize; shared state/density contracts cover interaction. Applications provide nodes and authorization semantics, not row templates or state styling. Later 11B work owns the physical retrofit, keyboard/focus recipes, and accessibility acceptance.
# Navigation Tree presentation

Task 11B physically maps the existing semantic tree to the v0.16 presentation standard. `NodeId`, `NodeCode`, and workspace target remain authoritative; localized labels, display order, depth, icons, and control instances never become identity.

Rows use semantic navigation typography, density-derived control height, shared card radius, subtle focus border, selected/hover surfaces, standard icon size, compact disclosure geometry, and one indentation rhythm. `GridDensityPreference` maps to the foundation Compact/Standard/Comfortable control heights and may not change selected or expanded node state. Disabled and authorization-hidden behavior remains resolver-owned.

Before Task 11B the host used local 30-pixel rows, 15-pixel icons, and ad-hoc margins. After Task 11B these values come from the theme mapping, labels trim safely with accessible full-name text, and selection, hover, focus, and disabled states share the global semantic brushes. Keyboard behavior continues to use the native hierarchical `TreeView` semantics.
