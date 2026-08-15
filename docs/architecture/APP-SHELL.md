# Application shell

`ShellHost` keeps the active workspace mounted while the application identity button toggles a scrollable Application Menu overlay. Escape and the close button dismiss the overlay; normal button focus and activation provide keyboard access.

The shell composes these foundations without duplicating them:

- `ILocalizationService` for immediate vi-VN/en-US updates;
- `IThemeService` and `IAppearancePreferenceService` for System/Light/Dark and shared font/density preferences;
- `ICompanyContextProvider` and `CompanyScopeCoordinator` for Company selection, profile refresh, authorization refresh, cancellation, and stale-response protection;
- `IIconRegistry` for semantic icons and safe fallback;
- `IApplicationExitService` for clean desktop lifetime shutdown.

The standard menu includes Company, Language, Appearance, General Settings, Account, License, About, and Exit. Account and License are optional presentation providers and never enforce access. About reads generic assembly/runtime/platform information. General Settings is an empty extensible host until contributors are registered.

The Application Menu is global shell navigation. It is not a Ribbon, Dynamic Tree, template command surface, or file/document history.
