# Authorization Presentation

`PermissionCode` and `CapabilityCode` are normalized, extensible value objects rather than closed enums. `EffectiveAuthorizationContext` is an immutable, Company-scoped snapshot containing `UserId`, `CompanyId`, permissions, capabilities, revision, status, and an optional diagnostic code.

`IAuthorizationPresentationProvider` is an application integration boundary. DynamicUI24 does not authenticate users, assign roles, evaluate server policies, or make authoritative authorization decisions.

## Resolution rules

`AuthorizationPresentationResolver` maps `PresentationRequirement` plus an effective context to one UI state:

| Condition | Result |
| --- | --- |
| No permission/capability requirement | `VisibleEnabled` |
| All requirements present in a ready context | `VisibleEnabled` |
| Permission absent + `Hide` | `Hidden` |
| Permission absent + `Disable` | `VisibleDisabled` |
| Permission absent + `ReadOnly` | `VisibleReadOnly` |
| Capability absent | Configured capability-unavailable behavior, otherwise unauthorized behavior |
| Context missing/error/unavailable | Same explicit fail-closed behavior |

A privileged item is never enabled from an unresolved context. Safe read-only presentation remains possible only when metadata explicitly selects `ReadOnly`.

Hiding or disabling UI is not security. Applications must send every operation to an authoritative backend, and a backend rejection always wins even if the earlier presentation snapshot displayed the action as enabled.

If an application caches contexts, its key must include at least `UserId`, `CompanyId`, and `Revision`. `AuthorizationContextCacheKey` models this identity. A Company A entry must never satisfy a Company B request.
