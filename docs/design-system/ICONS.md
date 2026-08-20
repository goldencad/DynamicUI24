# Icon system

Normative authority: [DynamicUI24 Specification v0.16 §15](../specification/DynamicUI24-Spec-v0.16.md#15-icons). Applications use `SemanticIcon`/icon keys and semantic sizes; approved icons must not be replaced by application-local geometry or sizing. Theme may evolve treatment without changing identity.

`IconKey` is the only icon identity allowed in reusable metadata. `DynamicUI24IconCatalog` maps framework-owned keys to canonical SVG assets, `IIconRegistry` resolves that mapping, and `SemanticIcon` is the shared renderer. Controls do not receive asset paths.

## Canonical asset catalog

Framework assets live only in `src/DynamicUI24.Avalonia/Assets/Icons/`. The central catalog owns every physical filename. Editor, tree, ribbon, action, and application code requests stable keys such as `CLOCK`, `CALENDAR`, `SEARCH`, or `CHEVRON_DOWN`; it must not mention `.svg` filenames or embed equivalent path data.

The required shared family currently includes Clock, Calendar, Chevron Down, Search, Help, Clear, Reveal, Open/Browse, Overflow, and Check. Applications may register genuinely application-specific keys, but must not copy or shadow these files in screen or sample folders.

All catalog SVGs use:

- `viewBox="0 0 24 24"`;
- optical geometry centered in the 24-unit canvas with approximately 3–4 units of breathing room;
- round caps/joins and the shared stroke rhythm for outline icons;
- `currentColor` or `none` only—no hard-coded tint;
- path geometry that remains legible at the theme-owned 16px standard size.

To replace a framework icon, update the SVG at its catalog-owned asset path. Do not change `IconKey`, editor code, metadata, or presenter code. Rebuild so the embedded resource is refreshed; every consumer of that semantic key then receives the replacement.

Missing mapped assets, invalid viewBox values, empty path geometry, and hard-coded SVG colors fail catalog construction. They never silently degrade to Unicode or a font glyph.

The shared source types are:

- `SvgIconSource`: portable SVG geometry with an optional logical resource name owned by the registry/composition boundary.
- `FontGlyphIconSource`: a Unicode glyph and installed/logical font-family name. It never embeds or points metadata at a raw font file.

Application-specific registration remains available behind `IIconRegistry`; it is not a mechanism for replacing framework catalog files per screen.

Application-specific packs may add keys or deliberately replace a standard key with `replace: true`. Replacement is local to that registry; metadata remains unchanged. Unknown keys always resolve to the same deterministic fallback. Foreground color and size come from semantic tokens/action geometry, so SVG and glyph icons remain theme- and scale-compatible.

Reusable UI must not reference direct SVG filesystem paths. Action, tree, ribbon, Setup, and application-menu metadata must not embed SVG payloads, resource paths, raw font data, or font files. Those details belong behind the registry. Maintainers may add a new `IconSource` renderer without changing `IconKey` or consumer metadata.
