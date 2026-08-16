# Quick Access Integration

Provide `IQuickAccessStore` for user-scoped preference persistence and `IQuickAccessResolver` for current metadata. Persist only `QuickAccessEntry`; labels/icons must resolve from live provider metadata through authorization and privacy presentation.

Record Recent after successful shared navigation/command activation only. Enforce a finite recent limit. Re-resolve all groups when company, workspace, permission, privacy, language, or metadata revision changes. A missing target may remain as preference identity for recovery, but must not be actionable.
