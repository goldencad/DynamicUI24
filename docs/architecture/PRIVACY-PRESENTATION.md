# Privacy Presentation

## WHAT PRIVACY OWNS

Privacy owns reusable presentation metadata, requested/effective runtime mode, centralized policy resolution, temporary reveal state, safe value projection, and semantic events without raw values. `IPrivacyPolicyResolver` is the only decision point used by Grid, Form/Detail, Notification, Search, Clipboard, and Import/Export seams.

## WHAT PRIVACY DOES NOT OWN

Privacy is not authorization, endpoint DLP, storage encryption, audit storage, watermarking, DRM, redaction, AI classification, remote-desktop detection, or business classification. It never grants access and never changes source values.

## AUTHORIZATION VS PRIVACY

`Authorization ≠ Privacy Presentation ≠ Capture Protection ≠ DLP`. Authorization is resolved first. An unauthorized value never becomes raw through Privacy Mode or temporary reveal. Privacy `Off` disables optional masking only; mandatory protection still applies.

## SENSITIVITY MODEL

`NORMAL`, `CONFIDENTIAL`, and `RESTRICTED` are application-neutral classifications. Missing v0.9 metadata resolves to `NORMAL`. Invalid or unknown sensitive metadata fails closed as restricted/masked.

## PRESENTATION MODEL

`NONE`, `MASK`, `PARTIAL_MASK`, `HIDE`, and `CAPTURE_PROTECT` are presentation outcomes. Full masks use a fixed design-system placeholder and do not reveal source length. Partial masks use generic prefix/suffix counts and a fixed mask body. Hide returns localized safe text; raw content is not assigned to tooltip or accessibility properties.

## POLICY PRECEDENCE

Strictest applicable rule wins:

1. Authorization
2. Mandatory organization/application policy
3. Sensitivity minimum policy
4. Company/workspace policy
5. User-requested Privacy Mode
6. Temporary reveal eligibility
7. Platform capture capability
8. Effective presentation

Resolver failure, unknown metadata, or unavailable capture protection for restricted content resolves to mask/hide. No exception permits a raw value.

## TEMPORARY REVEAL

Reveal is explicit, bounded by metadata/policy, field-scoped in P1, and tied to the current privacy generation. Timeout, manual hide, company/workspace switch, authorization/policy invalidation, or app restart revokes it. Reveal never overrides authorization and independently resolves copy, export, search, notification, tooltip, and accessibility exposure.

## CONTEXT INVALIDATION

`IPrivacyStateService` increments a generation and clears reveal entries on company/workspace or policy invalidation. Callers must reject stale generations, so a late Company A result cannot update Company B presentation. Active reveal is runtime-only and never persisted.

## COMMON FAILURE MODES

- Implementing masking separately in controls: use `IPrivacyPolicyResolver` and `ISensitiveValuePresenter`.
- Treating visible as copyable/exportable: inspect `CanCopy` and `CanExport`.
- Storing masked/raw strings in caches: cache metadata/policy only and key by context generation.
- Claiming capture support because an API did not throw: require explicit capability/result.
- Putting raw content in tooltips, validation, logs, titles, notification identity, favorites, or recents: keep stable IDs and safe labels only.

## FOCUSED TEST COMMANDS

`dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --filter FullyQualifiedName~PrivacyFoundationTests`

`dotnet test tests/DynamicUI24.ArchitectureTests/DynamicUI24.ArchitectureTests.csproj --filter FullyQualifiedName~PrivacyArchitectureTests`
