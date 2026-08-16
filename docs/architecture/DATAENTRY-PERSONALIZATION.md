# DataEntry personalization

## WHAT DATAENTRY OWNS
DataEntry owns the presentation overlay: semantic column order, bounded width, visibility, left pinning, density, and reset. `GridViewPreferenceResolver` repairs persisted input against currently authorized metadata.

## WHAT IT DOES NOT OWN
It does not own row data, application metadata, permissions, tenancy, formulas, queries, reports, or a durable preference database.

## COLUMN IDENTITY
Every operation and stored item uses `VariableCode`. Localized labels, visual indexes, and Avalonia controls are never identities.

## ROW IDENTITY
Rows and active cells retain opaque `RowKey`; the active cell is `RowKey + VariableCode`.

## PREFERENCE OVERLAY
Metadata is resolved first, then the user overlay. Removed columns are ignored, new columns use metadata defaults, duplicate/invalid items are repaired, widths are clamped, unauthorized columns stay hidden, and schema mismatch falls back safely. Pin overflow is unpinned deterministically. Demo scope is `USER + GRID`; `GridPreferenceScopeKind` also supports global-user and user-company-grid policies.

## PRIVACY RULE
Preferences contain semantic layout and typed query metadata only—never rows, cell values, sensitive suggestions, or reveal state.

## CONTEXT PANEL RULE
Layout changes do not replace `RowKey` or `VariableCode`. Hiding the active column relocates focus to the nearest eligible visible column so context can re-resolve.

## 100K+ RULE
Personalization changes only the small column projection. It never enumerates, copies, or materializes logical rows.

## FOCUSED TEST COMMANDS
`/usr/local/share/dotnet/dotnet test tests/DynamicUI24.Tests --no-restore --filter GridPersonalizationTests`

## COMMON FAILURE MODES
Do not persist localized headers, mutate `GridDefinition`, trust stored widths/orders, let preference override authorization, duplicate pinned cells, or rebuild the row dataset after a layout change.
