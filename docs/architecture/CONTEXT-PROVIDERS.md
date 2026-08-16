# Context Providers

`IContextPanelProvider` receives `ContextPanelRequest` and returns semantic `ContextPanelResult`; neither contract references Avalonia. The request contains company/workspace/template/navigation identifiers, stable selection keys, culture, privacy mode, permission context, help code, generation and cancellation token.

Providers return modest generic sections and items: fields, status, actions, navigation and text. They must not return controls, raw exceptions, business editors, full datasets, or sensitive diagnostics. They should honor cancellation, perform lookup by stable key, and echo generation. The coordinator remains the authoritative stale-result guard and isolates provider failure.

Provider codes are registered explicitly. Unknown codes fail safely. Results with duplicate sections or field IDs are rejected. Refresh means resolving the current semantic context with a new generation; it does not refresh the application.
