# Action Definition

`ActionBarDefinition` contains an identifier, technical code, `Top`/`Bottom` position, display order, visibility, and an immutable ordered action collection. Identifiers are unique within their scope.

`ActionDefinition` contains stable identifiers/codes, a localization key, semantic `IconKey`, generic `ActionType`, display order, visibility, optional permission/capability requirement, selection bounds, and only the target data needed for dispatch. `Navigate` carries a workspace id; registered command types carry a registered command code. Metadata never carries executable delegates or scripts.

Supported classifications are Navigate, Refresh, Search, Filter, Add, Edit, Delete, Import, Export, Preview, Validate, Commit, ApplicationCommand, BatchAction, and CustomRegistered. Task 7 dispatches Navigate, Refresh, ApplicationCommand, and CustomRegistered; the remaining values are descriptors for future consumers.
