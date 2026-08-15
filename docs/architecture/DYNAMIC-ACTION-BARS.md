# Dynamic Action Bars

The normative reusable UX and metadata rules are in the design-system [Buttons and Dynamic Action Bars standard](../design-system/BUTTONS.md). This document describes resolution and dispatch architecture.

Dynamic Action Bars are reusable presentation and dispatch regions around workspace content. A workspace may contribute metadata for a `Top` bar, a `Bottom` bar, both, or neither. The same resolver and Avalonia host render both positions; XAML does not contain action-specific controls.

The resolver receives the current workspace/template, Company Context authorization snapshot, selection count, shared presentation state, and optional status summary. Every navigation or Company change creates a fresh resolution, preventing stale permission or capability state. Actions are ordered by `DisplayOrder` and then technical `ActionCode`.

The framework is presentation/dispatch only. It contains no domain workflows, backend authorization, arbitrary metadata code, or template instantiation. Navigation uses `IWorkspaceNavigationService`; refresh and registered operations are injected services.

Malformed navigation targets and incomplete registered commands are omitted with diagnostics. Unknown registered commands return `Unavailable`, and unknown semantic icons use the shared safe fallback.

Variant, menu, keyboard, geometry, and scaling behavior follows the shared [button standard](../design-system/BUTTONS.md).
