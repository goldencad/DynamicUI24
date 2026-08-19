# DocsView24 Theme boundary

Normative authority: [DynamicUI24 Specification v0.16](../specification/DynamicUI24-Spec-v0.16.md#5-typography-authority) and [v0.16.1](../specification/DynamicUI24-Spec-v0.16.1.md).

The desktop DocsView24 application chrome consumes the DynamicUI24 Standard, semantic tokens, and an approved active Theme exactly like other TS24 desktop applications. It must not establish an independent desktop application Theme, typography scale, or component design system.

Document-native content remains renderer/document-owned. Its typography, pagination, layout, colors, and embedded styles are not replaced by DynamicUI24 application-chrome tokens.

Mobile DocsView24/Flutter does not reference DynamicUI24 desktop binaries. A mobile implementation should map the same approved TS24 semantic presentation vocabulary through mobile/Flutter-native contracts. DynamicUI24 contains no Flutter implementation.
