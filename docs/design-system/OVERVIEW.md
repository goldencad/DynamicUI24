# DynamicUI24 design system

These standards are the reusable UX contract for the shell, Setup, and future templates. Consumer applications should normally customize DynamicUI24 through metadata, tokens, registries, providers, and extension points rather than modifying shared framework controls.

Application developers configure stable technical codes, localization keys, `IconKey`, action geometry, permissions, providers, and registered commands. They may override documented semantic tokens or register an application icon pack at composition time. Business behavior remains outside reusable controls.

Framework maintainers and Local AI must preserve those public contracts when extending renderers. Add optional metadata with safe defaults, validate it at construction boundaries, resolve it through generic services, and keep existing keys and presets source-compatible. Never make a shared control inspect a consumer namespace, filesystem icon path, raw font file, or business command implementation.

Standards:

- [Buttons and action variants](BUTTONS.md)
- [Icon system](ICONS.md)
- [Tree navigation](TREE-NAVIGATION.md)
- [Split navigation layout](SPLIT-NAVIGATION-LAYOUT.md)
- [Design tokens and scaling](TOKENS.md)
