# Icon system

Normative authority: [DynamicUI24 Specification v0.16 §15](../specification/DynamicUI24-Spec-v0.16.md#15-icons). Applications use `SemanticIcon`/icon keys and semantic sizes; approved icons must not be replaced by application-local geometry or sizing. Theme may evolve treatment without changing identity.

`IconKey` is the only icon identity allowed in reusable metadata. `IIconRegistry` maps it to a generic `IconSource`; controls do not receive asset paths.

The shared source types are:

- `SvgIconSource`: portable SVG geometry with an optional logical resource name owned by the registry/composition boundary.
- `FontGlyphIconSource`: a Unicode glyph and installed/logical font-family name. It never embeds or points metadata at a raw font file.

```csharp
registry.Register(new IconDefinition(
    new IconKey("MY_SVG"),
    new SvgIconSource("M2,2 L20,20", "icons/my-svg.svg")));
registry.Register(new IconDefinition(
    new IconKey("MY_GLYPH"),
    new FontGlyphIconSource("★", "My Installed Icon Family")));
```

Application-specific packs may add keys or deliberately replace a standard key with `replace: true`. Replacement is local to that registry; metadata remains unchanged. Unknown keys always resolve to the same deterministic fallback. Foreground color and size come from semantic tokens/action geometry, so SVG and glyph icons remain theme- and scale-compatible.

Reusable UI must not reference direct SVG filesystem paths. Action, tree, ribbon, Setup, and application-menu metadata must not embed SVG payloads, resource paths, raw font data, or font files. Those details belong behind the registry. Maintainers may add a new `IconSource` renderer without changing `IconKey` or consumer metadata.
