# Sensitive Content

`SensitiveContentDefinition` composes with variable, column, form, search-result, notification, context-panel, and import/export field metadata. Its invariant fields are sensitivity, preferred presentation, capture fallback, reveal rules, independent copy/export/search/notification/tooltip/accessibility flags, policy code, and generic partial-mask configuration.

Absent metadata is `NORMAL` and visible for backward compatibility. Unknown sensitivity/presentation, invalid reveal duration, malformed partial mask, missing policy provider, provider exceptions, and stale context all fail closed for sensitive content. Source values are never mutated.

Protected content must not be copied into tooltip, hover/flyout text, validation text, status detail, window titles, diagnostics, automation values, notification identity, or persisted favorite/recent labels. Diagnostics should use semantic field codes, safe row keys, error codes, types, counts, or masked previews.

Grid integration resolves only visible/materialized cells. It does not allocate privacy state for 100K logical rows or reload the provider on mode changes. Selection and scrolling do not reveal. Form/Detail uses the same resolver and presenter. Context Panel consumers use `PrivacyFieldPresentation` until a full runtime exists.
