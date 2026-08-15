# Optional shell services

Account and License/Entitlement are optional presentation-only sections. Register `IAccountPresentationProvider` or `ILicensePresentationProvider` only when the application has safe display data. With no provider, a null result, or an exception, the menu renders UNAVAILABLE or ERROR and keeps the shell usable.

These contracts do not authenticate users, manage credentials, enforce licenses, or authorize operations. Permission/capability presentation continues through `PresentationRequirement`; actual security remains the responsibility of the application/backend.

Register one shared `ILayoutResetService` for the Appearance reset action. The Task 4 foundation does not invent per-grid persistence. `IAppearancePreferenceService` stores generic UI scale, font size, and grid density preferences so later controls can adopt them without coupling the shell to a template.

All optional calls are asynchronous. The view ignores results after navigation has moved to another menu page, and Company refresh continues to use `CompanyScopeCoordinator` stale-response protection.
