# Permission and Capability Mapping

Use `PermissionCode` for an effective user action/right, such as `DATA.EDIT`. Use `CapabilityCode` for feature/provider/license availability, such as `REPORT.EXPORT_PDF_AVAILABLE`. Codes are application-extensible and should be stable, documented semantic strings.

Attach a `PresentationRequirement` to future menu, ribbon, tree, workspace, action, grid/column, dashboard, report, history/document, or signing metadata. Choose the unavailable behavior intentionally:

- `Hide` when the item should not be discoverable;
- `Disable` when it should remain discoverable but non-interactive;
- `ReadOnly` when safe viewing is explicitly supported without mutation.

Capability absence can have a separate behavior. If omitted, it uses the permission behavior. Missing, error, and unavailable contexts follow the same fail-closed mapping; they never optimistically enable privileged actions.

Presentation state is advisory. It improves the UI but does not authorize work. The consuming application/backend must validate the command against current authoritative policy, Company, capability/license state, and business rules at execution time.
