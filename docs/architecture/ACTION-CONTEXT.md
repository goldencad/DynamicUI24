# Action Context

`ActionBarResolutionContext` is a snapshot containing Company, workspace, template code, effective authorization, `SelectionCount`, shared presentation state, and optional bottom-bar status.

Authorization delegates to `AuthorizationPresentationResolver`, including Hide, Disable, and ReadOnly. A privileged action with missing or unavailable authorization fails closed. Selection rules apply `RequiresSelection`, `MinSelection`, and `MaxSelection` without knowledge of a grid or domain object.

`ActionBarStatus` uses nullable counts: `null` means unavailable, while `0` is a valid value. It supports total, visible, selected, error, warning, pending-change counts, and explicit read-only state. Status is displayed only by a bottom bar and carries no business meaning.
