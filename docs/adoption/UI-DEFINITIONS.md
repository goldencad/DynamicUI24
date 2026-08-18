# Adopt UI Definitions

Compose small immutable `UiElementDefinition` records under a `UiDefinitionCode` and stable version. Reference existing semantic command, editor, grid, report, pane and help codes. Specify only application intent; framework defaults fill omitted layout and personalization values.

Provide `IUiDefinitionRepository` for application storage and optionally `IUiDefinitionReferenceCatalog` for resolvable validation. Create drafts through `UiDefinitionLifecycleService`, validate, preview without business persistence, then publish. Implement `IUiDefinitionMigrator` when schema versions change. Keep secrets, code, SQL and business data out of definitions.
