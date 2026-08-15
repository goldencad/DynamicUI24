# Ribbon Integration

1. Create a `RibbonDefinition` in application composition code.
2. Validate it against registered workspaces.
3. Register application UI handlers in an instance of `UiCommandRegistry`.
4. Supply navigation and refresh adapters to `RibbonCommandDispatcher`.
5. Create `DynamicRibbonHost` and assign it to `ShellHost.RibbonContent`.
6. Call `UpdateContext` when workspace, selection, Company, permission, or capability state changes.

Use `DisplayNameKey` for all labels and register icons through `IIconRegistry`. Unknown localization keys show the existing bracketed fallback; unknown icons use the semantic fallback glyph. Switching culture rebuilds labels and retains the selected technical tab code. Switching System/Light/Dark retains the host, workspace, Company, and definition.

To add a tab, add metadata only—no shell XAML change is needed. Keep the Application Menu separate; Language, Appearance, About, and Exit remain there.
