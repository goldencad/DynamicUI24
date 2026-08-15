# Design tokens

Semantic identities live in `DynamicUI24.Shared.Presentation.SemanticToken`. Avalonia
values live only in `Presentation/DesignTokens.axaml` in `DynamicUI24.Avalonia`.

The canonical dictionaries cover surface, raised surface, text, muted text, border,
accent, accent text, selection, hover, disabled, success, warning, error, info, grid,
read-only, and calculated roles. Reusable controls consume `Dui*Brush` dynamic resources;
they do not contain consumer brand colors.

The dictionary has Light and Dark theme dictionaries. System mode sets Avalonia's
requested variant to `Default`, allowing the platform theme to choose one. Light and Dark
set explicit Avalonia variants. Dynamic resources re-resolve while the existing visual tree
and workspace remain alive.

Consumers merge this resource URI:

```xml
<ResourceInclude Source="avares://DynamicUI24.Avalonia/Presentation/DesignTokens.axaml" />
```

An application may define a later `DuiAccentBrush` resource to override only the accent.
All remaining defaults continue to resolve from the framework dictionary.
