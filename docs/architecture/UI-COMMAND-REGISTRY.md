# UI Command Registry

`IUiCommandRegistry` maps a technical code to an application-supplied asynchronous handler. Ribbon metadata stores only that code; it never stores delegates or executable expressions. `UiCommandRegistry` is instance-scoped and duplicate registration is rejected.

`RibbonCommandDispatcher` routes Navigate through `IRibbonNavigationService`, Refresh through `IRibbonRefreshService`, and registered application/custom commands through the registry. Disabled, hidden, or read-only commands return Denied. Missing handlers and unsupported types return Unavailable; handler exceptions become Failed with a stable diagnostic code.

This boundary is for UI orchestration only. A handler may request an application service, but the Ribbon must never become an authorization boundary or contain business/domain logic.
