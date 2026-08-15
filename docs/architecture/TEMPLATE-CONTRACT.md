# Template contract

`IDynamicTemplate` is the domain-neutral boundary between workspace metadata and an independently registered template module. It exposes the module's `TemplateCode`, displayable `TemplateVersion`, declared capabilities, definition validation, and creation of a non-visual `WorkspaceDescriptor`.

`TemplateCode` trims input, normalizes it with invariant uppercase, and accepts only letters, digits, and underscore-separated segments. Empty or malformed values are rejected. It is an open value object rather than an enum, so consumers can introduce codes such as `CALENDAR` without changing DynamicUI24.

`TemplateCapability` follows the same open, normalized-name approach. Capabilities in Task 1 are declarations only; permission checks and capability behavior belong to later tasks.

Core contracts do not reference Avalonia, template modules, or consumer applications. Actual template UI and business behavior are intentionally deferred.
