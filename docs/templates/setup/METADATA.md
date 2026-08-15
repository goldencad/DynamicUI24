# Setup metadata

`SetupCategoryDefinition` describes `CategoryId`, technical `CategoryCode`, localized label key, semantic icon, order, optional parent, definition type, optional presentation requirement, visibility, and optional scope key. `SetupCategoryValidator` reports duplicate IDs, orphans, and cycles without crashing the shell. `SetupCategoryResolver` orders arbitrary-depth children and applies HIDE, DISABLE, READ_ONLY, and fail-closed authorization presentation.

The standard identities are GENERAL, MASTER_CATALOGS, WORKSPACES, COLUMNS_VARIABLES, NAVIGATION_TREE, RIBBON, ACTION_BARS, DASHBOARD, and REPORTS. They are identities rather than hard-coded editor branches.

`SetupDefinitionDescriptor` carries identity, code, display name, type, exact version, Draft/Valid/Invalid/Published/Retired status, optional effective dates, system/edit/clone flags, permission metadata, validation state, property values, category, and optional scope. Technical codes remain untranslated.

`EditorFieldDefinition` supports text, multiline text, Boolean, integer, decimal, choice, date, optional date, IconKey, and localization fields. Invalid field metadata is rejected at its boundary.
