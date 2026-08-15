# Company Context Integration

At application composition time:

1. adapt the application's Company source to `ICompanyContextProvider`;
2. adapt its read API to `ICompanyProfileProvider`;
3. adapt its effective policy/capability endpoint to `IAuthorizationPresentationProvider`;
4. create one `CompanyScopeCoordinator` for the signed-in user session;
5. subscribe presentation consumers to `SnapshotChanged` and resolve their metadata again after a ready/unavailable/error snapshot;
6. dispose the coordinator during shutdown so pending refreshes are cancelled.

Do not store one unqualified permission list. Each response and cache entry must retain `UserId`, `CompanyId`, and revision. When the backend revision changes, refresh and invalidate the matching old snapshot.

The coordinator prevents stale async overwrites, but the application remains responsible for cancellation-aware adapters and for reloading Company-scoped business data. Keep shell preferences—theme, culture, and current workspace—outside the Company scope so a switch does not restart or reset the shell.

For a future TS24/Odoo integration, translate the application's synced/API DTOs at this boundary. Do not introduce Odoo models or SDK references into DynamicUI24.
