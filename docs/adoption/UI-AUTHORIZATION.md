# Adopt UI Authorization

Your security layer supplies `UserSecurityContext` and may implement `IUiAuthorizationResolver`. Return semantic presentation outcomes; do not expose claims, secrets or sensitive values as reason text. Include security, Company, policy and definition generations so stale async results cannot win.

Bind features with `UiAuthorizationBinding`, then apply results consistently to all existing command/search/help/editor/grid surfaces. Always enforce authorization again in backend/provider operations. P1 masking remains independent even for authorized users.
