# Application Menu

The Application Menu is a reusable shell capability composed by `ApplicationMenuComposer` and rendered by `ApplicationMenuView`. The application identity button opens it over the workspace; opening, navigation, language/theme changes, and Company switching do not resolve or replace the active workspace.

Standard shell-owned entries cover Company context, language, appearance, general settings, account presentation, license presentation, About, and graceful Exit. Optional application entries implement `IApplicationMenuContributor`. Contributors return localization keys and semantic `IconKey` values, are ordered deterministically, are duplicate-safe, and are isolated so one failure cannot crash the shell.

`PresentationRequirement` and `AuthorizationPresentationResolver` remain the only permission/capability presentation mechanism. Missing context fails closed. Company switching and profile/authorization refresh use the existing `CompanyScopeCoordinator`, including cancellation and stale-response rejection.

Account and license contracts are presentation-only. The menu contains no authentication, entitlement enforcement, Odoo integration, persistence, or arbitrary executable metadata.
