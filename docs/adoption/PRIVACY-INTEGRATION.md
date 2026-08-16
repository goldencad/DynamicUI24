# Privacy Integration

Register one `IPrivacyStateService`, `IPrivacyPolicyResolver`, `ISensitiveValuePresenter`, and platform `ICaptureProtectionService` per application/window scope. Templates consume the shared state; they must not own competing privacy modes.

Attach `SensitiveContentDefinition` to reusable field metadata. Resolve each materialized value with current authorization, company, workspace, mode, reveal, capture capability, and generation. Feed the result to `ISensitiveValuePresenter`. On permission or stricter policy change, call `InvalidatePolicy`; on company/workspace switch call `InvalidateContext` before accepting new async results.

Use `PrivacyShellDefinitions.TopAction` with the existing Action Bar and `PrivacyShellDefinitions.SettingsMenuItem` with the Application Menu. Present requested/effective state compactly. Mode/reveal changes are presentation state and must not mark business data dirty or restart on theme, culture, UI-scale changes.

## Surface rules

- Clipboard: use `PrivacyClipboardPolicy`; cut must stop if copy is blocked.
- Notifications: resolve semantic fields once and reuse across Center, Toast/Banner/Alert, Top, and Bottom. Dedup remains semantic-ID based.
- Search: keep stable ID/navigation, but store/present only safe labels/subtitles.
- Import preview: resolve mapped target metadata and keep diagnostics safe.
- Export: use `PrivacyImportExportPolicy`; visual masking never authorizes raw export. Stream rows and resolve per field/batch.
- Accessibility: assign the safe accessible value, not raw text. Reveal exposure requires explicit policy and ends with reveal.

Do not persist reveal state or raw display strings. Do not place raw values in logs, validation messages, titles, tooltip/flyouts, favorites, pins, or recent items.
