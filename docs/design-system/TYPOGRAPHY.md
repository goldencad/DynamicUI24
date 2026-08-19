# Typography implementation guidance

Normative authority: [DynamicUI24 Specification v0.16](../specification/DynamicUI24-Spec-v0.16.md#5-typography-authority).

Use `DesignTokens.Typography` identities. `AvaloniaPlatformFontMapping` maps UI and Code roles to OS-appropriate ordered fallback stacks ending in generic `sans-serif` and `monospace`; never put a product font into application/sample metadata. Keep text Unicode-first, verify Vietnamese diacritics and fallback glyphs, and allow text to reflow. Document/DocsView24 content retains its own typography.

The current `DuiTypographyCaption`, `Label`, `Body`, and `Title` resources are compatibility mappings. New shared work should choose the more specific semantic role before resolving a theme value.
