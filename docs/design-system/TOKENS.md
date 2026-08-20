# Design tokens and global scaling

Reusable controls use semantic resources; they never hard-code application colors. Required roles include surface, raised surface, text, muted text, border, accent, hover, selected, selected-hover, disabled, focus, success, warning, error, spacing, radius, and typography. Avalonia resources use stable `Dui*` names in `DesignTokens.axaml`; action size presets use `ActionControlTokenCatalog`.

Light and Dark dictionaries provide color values. System mode delegates variant choice to the platform. Dynamic resources re-resolve without replacing workspace controls. Consumer overrides must retain semantic meaning and sufficient contrast; brand values should normally override accent roles, not rewrite shared templates.

Spacing/radius tokens define stable geometry across interaction states. Typography tokens are Caption, Label, Body, and Title. XS/Small/Medium/Large/XL action presets combine height, icon size, padding, and gap tokens. Metadata overrides are expressed in logical units, then global `UiScale` is applied. Font Size additionally scales typography. Controls must never counteract or divide out those global preferences.

Universal Editor form composition uses stable semantic roles mapped by `DuiEditor*` and `DuiPopup*` theme resources. `EditorPresentationTokens` supplies only Standard relationships and compatibility defaults; rendered controls bind to the theme resources. Small semantic values remain compact instead of stretching to available workspace width. DateRange wraps its two compact semantic groups when the host narrows.

When adding a token, maintainers provide a stable semantic name, Light/Dark behavior where applicable, safe default, documentation, and regression coverage. Renaming or changing an existing token's meaning breaks the public styling contract.
