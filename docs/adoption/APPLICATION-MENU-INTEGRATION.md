# Application Menu integration

1. Create one `ApplicationMenuComposer` and register application-owned `IApplicationMenuContributor` implementations during composition.
2. Supply the existing localization, semantic icon, theme, Company context/scope, layout-reset, and graceful-exit services to `ApplicationMenuView`.
3. Assign the view to `ShellHost.ApplicationMenuContent`.
4. Optionally provide presentation-only account and license providers. If omitted or failing, the reusable UI reports an unavailable/error state safely.

Contributor codes must be unique. Display text belongs in localization catalogs, icons are semantic keys, and business commands or executable metadata do not belong in menu metadata.
