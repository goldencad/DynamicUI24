# Setup integration

At application composition, provide categories, an `ISetupDefinitionProvider`, an `ISetupDefinitionValidator`, editor registrations, current Company/authorization context, localization, and semantic icon registry to `SetupWorkspaceHost`. Register the host as the view factory for `StandardTemplateCodes.Setup`.

Re-resolve the host when Company or effective authorization changes. `ScopeKey` is opaque framework metadata: the consuming provider owns whether definitions are global or Company-scoped. An unavailable authorization context is intentionally fail-closed.

Supply `SETUP.VIEW`, `SETUP.CREATE`, `SETUP.EDIT`, `SETUP.VALIDATE`, `SETUP.PUBLISH`, and `SETUP.RETIRE` as application authorization codes. Setup commands are declared in shared top/bottom Action Bar metadata and dispatched through registered command boundaries.

Add en-US and vi-VN strings for category, field, action, status, diagnostic, and unavailable keys. Register only semantic `IconKey` identities; never put vector paths in Setup metadata.
