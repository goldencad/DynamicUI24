# VariableCode

`VariableCode` is a first-class semantic identifier independent of localized display text. Construction trims and normalizes with invariant uppercase; equality is therefore deterministic and case-insensitive inputs converge on one representation. Empty, whitespace-only and unsupported-character values are invalid.

Codes are unique within their definition scope. Once published, a code is immutable; renaming requires an explicit new version/draft. Formula references use the code rather than display names or storage identifiers.

Future Local AI tooling must resolve definitions through registries/providers, preserve published versions and stable codes, validate a draft before publication, and never silently rewrite references.
