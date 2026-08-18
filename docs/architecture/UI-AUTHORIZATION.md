# UI Authorization

`IUiAuthorizationResolver` consumes application/security decisions and returns `Hidden`, `Disabled`, `ReadOnly` or `Enabled`. It controls presentation only; provider and backend APIs must authorize every protected operation themselves.

Permission, capability, policy and P1 privacy are separate ceilings. Unavailable, ambiguous, failed or stale protected resolution fails closed. Context includes security, Company, workspace, definition version, privacy and generations. `GenerationSafeUiAuthorizationService` rejects late results and caches only at semantic/context granularity.

A hidden command must be removed from Ribbon, menu, Action Bar, Search, Quick Access and shortcuts through the shared command/search infrastructure. Safe diagnostic codes must never include raw protected values.
