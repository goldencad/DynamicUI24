# Adopt DataEntry preferences

Register an `IGridViewPreferenceStore` and choose a `GridPreferenceScope`. The framework ships `InMemoryGridViewPreferenceStore` for demos/tests; applications own durable user storage and tenancy policy.

Restore after metadata/authorization resolution with `RestoreViewAsync`, and save `CurrentViewPreference` or call `SaveViewAsync`. Always treat the returned preference as presentation state. Reset removes the overlay and restores metadata order, widths, visibility/pinning, and default sort/filter.

Schema changes are fail-safe: removed codes disappear, new codes receive metadata defaults, invalid order is normalized, width is clamped, duplicates are collapsed, and an unsupported schema version falls back to metadata. A rename needs an application-owned alias/migration because semantic identity changed.

Never store row/cell values, reveal state, localized labels, controls, or raw restricted-value suggestions in this store.
