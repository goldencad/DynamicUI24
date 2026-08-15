# Ribbon Context

`RibbonResolutionContext` contains the current Company, workspace, template, effective authorization context, and selection count. `RibbonContextRule` supports declarative equality for workspace/template, a required capability, and selection. It is data only—there is no expression or scripting engine.

Resolution first applies explicit visibility and context, then delegates permission/capability presentation to the existing `AuthorizationPresentationResolver`. Hide, Disable, and ReadOnly are preserved. A missing or unavailable authorization context fails closed. Parent tab/group restrictions flow down to commands.

After a Company switch, the demo passes the latest `CompanyScopeSnapshot.AuthorizationContext` to the resolver. The current workspace and definition remain unchanged, so A→B→C→A cannot retain stale Ribbon authorization state.
