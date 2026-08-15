# Notification Lifecycle

The normal path is `New → Active → Acknowledged`, followed independently by `Dismissed`, `Resolved`, or `Expired`.

- `Dismissed` is presentation state initiated by the user. It does not claim the condition is fixed.
- `Resolved` is provider/runtime-owned business-condition state. Resolved items leave active/actionable surfaces.
- `Expired` is driven by definition expiration and is non-actionable.
- A repeated unresolved emission with the same provider and `DeduplicationKey` updates the same logical instance and progress.
- Missing previously active provider state resolves the in-memory item; there is no persistent history engine.

Auto-show uses an injected clock and a deterministic per-logical-instance cooldown. It does not create timer loops. The coordinator bounds retained active/recent state to avoid unbounded memory growth.

Company-scoped instances must match the current `CompanyId`. Workspace-scoped surfaces render only in their workspace, while Notification Center may retain the logical item. Authorization is resolved before copy or actions reach any renderer; unavailable authorization fails closed according to `UnauthorizedBehavior`.
