# Typography implementation guidance

Normative authority: [DynamicUI24 Specification v0.16](../specification/DynamicUI24-Spec-v0.16.md#5-typography-authority).

Use `DesignTokens.Typography` identities. `AvaloniaPlatformFontMapping` maps UI and Code roles to OS-appropriate ordered fallback stacks ending in generic `sans-serif` and `monospace`; never put a product font into application/sample metadata. Keep text Unicode-first, verify Vietnamese diacritics and fallback glyphs, and allow text to reflow. Document/DocsView24 content retains its own typography.

The current `DuiTypographyCaption`, `Label`, `Body`, and `Title` resources are compatibility mappings. New shared work should choose the more specific semantic role before resolving a theme value.

## Task 11B rendered typography path

`AvaloniaTypography` resolves `AvaloniaPlatformFontMapping.UiFallbackStack` once, publishes the resolved `FontFamily` as `DuiFontFamilyUi` at each shared root, and sets Avalonia's inheritable `TextElement.FontFamily`. On macOS the ordered family is `.AppleSystemUIFont, Arial Unicode MS, sans-serif`. Native controls inherit it; `DynamicRibbonHost` also assigns it directly because vendor themes may set their own local value. Normal platform line metrics are retained so Vietnamese fallback glyphs reflow instead of being clipped.

| Surface | Semantic role | Size | Weight | Resolution source |
|---|---|---:|---|---|
| Shell application title | Title | 16 | SemiBold | Shell token + inherited `AvaloniaTypography` |
| Shell workspace subtitle/status | BodySmall | 12 | Normal | Shell token + inherited adapter |
| Ribbon/tab captions | Navigation | 13 | Normal/vendor hierarchy | Explicit shared vendor adapter |
| Search query/results | Input/Body; Caption metadata | 14/11 | Normal | Search adapter + semantic resources |
| Notification Center/cards | Body; Caption severity | 14/11 | SemiBold/Normal | Notification adapter + semantic resources |
| Settings navigation | Navigation/Body | 14 | Normal; selected theme-owned | Settings adapter + native ListBox state |
| Settings page/section/labels | PageTitle/SectionTitle/BodySmall | 24/16/12 | SemiBold | Settings semantic helpers |
| Dashboard/Overview | PageTitle/SectionTitle/Body/BodySmall | 24/16/14/12 | SemiBold/Normal | shared composition components |
| Navigation Tree | Navigation/SectionTitle | 13/16 | Normal; parent SemiBold | Tree resources + adapter |
| Breadcrumb | Body | 14 | current SemiBold | Breadcrumb adapter + Body token |
| Context Panel chrome/content | SectionTitle/Body | 16/14 | SemiBold/Normal | Context adapter + inherited Body |

All rows resolve the same macOS family stack above. System, Light, and Dark alter visual theme values, not font identity.
