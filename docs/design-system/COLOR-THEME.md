# Color and theme guidance

Normative authority: [v0.16 §§3–4](../specification/DynamicUI24-Spec-v0.16.md#3-theme-contract).

Application and sample code reference semantic roles, never hex values. Concrete Light/Dark values remain in `DynamicUI24.Avalonia/Presentation/DesignTokens.axaml`. System mode delegates to the platform. A theme declares its ID, version, Standard compatibility, supported modes, and values; it does not redefine component semantics.

`ThemeResolver` selects a registered versioned `IThemeDefinition` and rejects unknown themes or unsupported modes; it owns no application state. `CurrentThemeCompatibility` registers the accepted visual generation and maps its Avalonia resources to v0.16 keys until later retrofit phases. Validate contrast, focus visibility, and state distinction physically in every mode.

The v0.16.1 lifecycle uses `ThemeCode`, immutable `ThemeVersionDefinition`, mutable `ThemeDraft`, `ThemeGeneration`, `ThemeValidator`, isolated `ThemePreviewSession`, and `IThemeLifecycleRepository`. Publish/activation requests carry expected generation and idempotency identity. Repository implementations must allocate from retained history and mutate history/activation atomically; Task 11A supplies no database or Theme Studio.

The future physical surface belongs under Developer UI Authoring → Design System → Standard, Themes, Typography, Colors, Spacing & Sizing, Components, Density, Motion, and Preview Lab. Task 11F remains its expected owner.
