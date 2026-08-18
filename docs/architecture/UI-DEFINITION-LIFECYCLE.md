# UI Definition Lifecycle

Published vN → create mutable Draft → validate → preview Draft → publish immutable vN+1. `UiDefinitionVersion` is stable identity, never merely a timestamp. Publish is blocked on critical diagnostics and detects active-version conflicts. `IUiDefinitionRepository.PublishAndActivateAsync` is the atomic storage seam.

Rollback calls `ActivateAsync` for a previously published valid version; it never rewrites history or business data. Schema adapters implement `IUiDefinitionMigrator`: migrate deterministically or return an actionable safe diagnostic. Lifecycle audit hooks are `DraftCreated`, `Validated`, `Previewed`, `Published` and `RollbackActivated`; applications own audit storage and actor context.
