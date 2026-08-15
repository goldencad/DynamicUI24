# Dynamic Action Bars

Dynamic Action Bars are reusable presentation and dispatch regions around workspace content. A workspace may contribute metadata for a `Top` bar, a `Bottom` bar, both, or neither. The same resolver and Avalonia host render both positions; XAML does not contain action-specific controls.

The resolver receives the current workspace/template, Company Context authorization snapshot, selection count, shared presentation state, and optional status summary. Every navigation or Company change creates a fresh resolution, preventing stale permission or capability state. Actions are ordered by `DisplayOrder` and then technical `ActionCode`.

The framework is presentation/dispatch only. It contains no domain workflows, backend authorization, arbitrary metadata code, or template instantiation. Navigation uses `IWorkspaceNavigationService`; refresh and registered operations are injected services.

Malformed navigation targets and incomplete registered commands are omitted with diagnostics. Unknown registered commands return `Unavailable`, and unknown semantic icons use the shared safe fallback.

## Button and menu variants

Every action declares a UI-neutral `ActionButtonVariant`: `Button`, `DropdownButton`, `SplitButton`, `IconButton`, or `ToggleButton`. The shared host renders these variants identically in Top and Bottom bars. A split button dispatches its action's registered command from the main segment; its chevron only opens the menu. Toggle state is metadata, not business state owned by the control.

Menu items carry stable codes, localization keys, semantic `IconKey` values, display order, optional group/separator and shortcut text, permission requirements, and optional children. Construction rejects more than two menu levels. Resolution filters hidden items, retains disabled items, sorts deterministically, and reports missing commands without crashing. Keyboard Down/F4 opens a menu, Up/Down moves through executable enabled items, Escape closes it, and selecting an item uses the same injected command dispatcher as ordinary actions.
