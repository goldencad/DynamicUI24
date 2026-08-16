# TS24 Dynamic UI Framework --- Specification v0.10

**Status:** Draft\
**Version:** 0.10\
**Initial Consumer:** PayCalc24\
**Purpose:** Reusable dynamic UI framework for TS24 desktop applications
built on Avalonia/Actipro.\
**Source context:** DynamicUI24 v0.9 plus the cross-application
requirement for user-controlled sensitive-content presentation, privacy
masking, capture protection, clipboard/export safety, and secure
progressive disclosure.

------------------------------------------------------------------------

## 0. Version History

### v0.10

Version 0.10 adds **Sensitive Content & Privacy Presentation** as a
first-class, application-neutral DynamicUI24 capability.

The feature is deliberately separated from authorization,
operating-system capture protection, and DLP.

The governing model is:

``` text
Authorization
    ≠
Privacy Presentation
    ≠
Capture Protection
    ≠
DLP
```

A user may be fully authorized to read a value while still choosing to
hide it temporarily from the visible UI. Conversely, Privacy Mode never
grants access to a value that authorization has denied.

Major additions:

1.  **User-controlled Privacy Mode**
    -   `OFF`, `ON`, and `AUTO`;
    -   available through Shell/Action Bar metadata rather than
        hard-coded application UI;
    -   optional temporary reveal;
    -   deterministic fallback when platform capture protection is
        unavailable;
    -   application/security policy may impose a stricter minimum than
        the user's preference.
2.  **Sensitivity metadata**
    -   `NORMAL`;
    -   `CONFIDENTIAL`;
    -   `RESTRICTED`;
    -   sensitivity is semantic metadata and is not inferred from
        captions, control types, or business names.
3.  **Privacy presentation**
    -   `NONE`;
    -   `MASK`;
    -   `PARTIAL_MASK`;
    -   `HIDE`;
    -   `CAPTURE_PROTECT`;
    -   policy resolution is centralized and reusable across templates.
4.  **Cross-surface protection**
    -   Grid;
    -   Form/editor fields;
    -   Context/Inspector Panel;
    -   Tree/detail labels where values are data-bound;
    -   Notification/Guidance;
    -   Search/Command results;
    -   Tooltip/flyout/menu secondary text;
    -   clipboard;
    -   import/export preview;
    -   report/document preview metadata where supported;
    -   accessibility text and automation exposure.
5.  **Capture-protection capability**
    -   treated as a best-effort platform capability;
    -   never described as absolute anti-capture or DLP;
    -   platform adapter isolated from Core;
    -   safe masking/hiding fallback when unsupported or unreliable.
6.  **Temporary reveal**
    -   user may reveal eligible sensitive values for a bounded
        duration;
    -   timeout is policy/metadata driven;
    -   reveal never overrides authorization or mandatory protection;
    -   context changes revoke temporary reveal.
7.  **Clipboard, Search, Notification, Export and diagnostics safety**
    -   sensitive values must not leak through secondary surfaces while
        the primary surface is protected;
    -   raw values must not be placed in framework diagnostics/logging
        merely for presentation convenience;
    -   export and copy use explicit policy resolution.
8.  **Company/workspace/context safety**
    -   Privacy Mode and resolved privacy policy are re-evaluated on
        context changes;
    -   stale async results must not re-expose values after a stricter
        context becomes active.

v0.10 is an additive evolution of the authoritative v0.9 repository
specification.

Authoritative v0.9 lineage:

``` text
docs/specification/DynamicUI24-Spec-v0.9.md
SHA-256: af99f4adf9bb4004a70c8c7d920e84894bc5aa62d5dd0ac62c329b27b94e4a0a
```

Authoritative repository baseline at the start of S0.10:

``` text
5f728a8b6b813c1380eb0bd07719d42a4cb0ee4d
```

Historical specification files remain immutable. v0.10 does not modify
v0.9. The v0.9 specification remains normative for all capabilities not
explicitly extended or clarified by v0.10.

------------------------------------------------------------------------

## 1. Objective

DynamicUI24 v0.10 provides a reusable privacy-presentation layer for
desktop applications that display information a user is authorized to
access but may not want continuously exposed on screen.

The capability must work across TS24 applications without embedding
payroll, tax, HR, accounting, signing, customer, or other business rules
into the framework.

The framework owns:

-   sensitivity metadata contracts;
-   privacy-mode state;
-   privacy-presentation policy resolution;
-   masking/hiding presentation;
-   temporary reveal state;
-   platform capture-protection abstraction;
-   privacy-aware clipboard/export/search/notification presentation;
-   privacy-aware accessibility presentation;
-   generic UI controls and semantic tokens;
-   context invalidation;
-   safe extension points.

The consuming application owns:

-   authoritative data classification;
-   authorization;
-   legal/compliance policy;
-   business-specific export rules;
-   authoritative audit requirements;
-   server-side access control;
-   DLP/MDM/EDR policy;
-   application-specific determination of which data is sensitive.

------------------------------------------------------------------------

## 2. Core Security Principles

### 2.1 Authorization is authoritative

Privacy presentation must never replace authorization.

``` text
Permission denied
→ value must not be obtained/exposed by UI

Permission allowed
→ privacy policy may still mask/hide/protect presentation
```

Privacy Mode `OFF` means "do not apply optional user privacy masking
beyond mandatory policy." It does not mean "ignore permissions."

### 2.2 Privacy is presentation state

Privacy Mode is UI/runtime state.

It must not:

-   modify authoritative business data;
-   alter formulas;
-   change persisted source values;
-   grant permissions;
-   change application ownership;
-   mutate published metadata merely because a user toggles privacy.

### 2.3 Fail closed for restricted content

When policy resolution is ambiguous, missing, stale, or failed for
`RESTRICTED` content, presentation must choose the safer outcome.

Examples:

``` text
unknown policy + RESTRICTED
→ MASK or HIDE

failed permission resolution
→ do not expose raw value

capture protection requested but unsupported
→ fallback presentation policy
```

### 2.4 No false security claims

DynamicUI24 must not claim that capture protection:

-   prevents photography by an external camera;
-   prevents all privileged capture software;
-   prevents kernel/driver-level extraction;
-   is equivalent to enterprise DLP;
-   prevents authorized users from manually transcribing visible
    information.

The correct terminology is:

-   Privacy Presentation;
-   Sensitive Content Protection;
-   Capture Protection;
-   Best-effort platform capture protection.

### 2.5 Defense in depth

Sensitive presentation must consider all surfaces, not only the main
Grid/Form.

A masked salary cell is not protected if its raw value appears in:

-   tooltip;
-   search result subtitle;
-   clipboard;
-   notification;
-   validation error;
-   accessibility name;
-   export preview;
-   diagnostic log.

------------------------------------------------------------------------

## 3. Privacy Mode

### 3.1 Enum

``` text
PrivacyMode

OFF
ON
AUTO
```

### 3.2 OFF

`OFF` disables optional user-requested masking.

Mandatory application/security policy still applies.

Therefore:

``` text
User PrivacyMode = OFF
Mandatory Restricted Policy = MASK

Effective presentation = MASK
```

### 3.3 ON

`ON` asks the framework to protect all eligible sensitive content
according to metadata and effective policy.

Typical result:

``` text
NORMAL       → visible
CONFIDENTIAL → masked/partial masked
RESTRICTED   → masked/hidden/capture protected according to policy
```

### 3.4 AUTO

`AUTO` allows the application/framework/platform policy resolver to
select the effective privacy state based on supported context signals.

AUTO must be deterministic.

Potential future signals may include:

-   presentation mode;
-   screen-sharing state when reliably detectable;
-   remote-session state when reliably detectable;
-   application lock state;
-   external display policy;
-   organization policy.

v0.10 does not require every operating system to expose every signal.

AUTO must never fabricate a signal.

### 3.5 Effective privacy mode

Runtime should distinguish:

``` text
RequestedPrivacyMode
EffectivePrivacyMode
```

The effective mode may be stricter than the requested mode.

It must never be less strict than mandatory application/security policy.

------------------------------------------------------------------------

## 4. Sensitivity Classification

### 4.1 Enum

``` text
Sensitivity

NORMAL
CONFIDENTIAL
RESTRICTED
```

### 4.2 NORMAL

Normal application information.

Privacy Mode normally leaves it visible.

### 4.3 CONFIDENTIAL

Information that the user is authorized to view but may reasonably want
hidden while:

-   sharing a screen;
-   working in a public/shared environment;
-   demonstrating the application;
-   switching between users;
-   showing a workspace to another person.

### 4.4 RESTRICTED

Higher-sensitivity content for which application policy may require
masking/hiding/capture protection even when Privacy Mode is `OFF`.

### 4.5 No business inference in framework

DynamicUI24 must not hard-code:

``` text
SALARY → RESTRICTED
TAX_ID → RESTRICTED
BANK_ACCOUNT → RESTRICTED
```

A consuming application may configure those classifications.

Framework only understands the semantic classification.

------------------------------------------------------------------------

## 5. Privacy Presentation Modes

``` text
PrivacyPresentation

NONE
MASK
PARTIAL_MASK
HIDE
CAPTURE_PROTECT
```

### 5.1 NONE

Display normally, subject to authorization.

### 5.2 MASK

Replace the visible value with a non-reversible presentation
placeholder.

Example:

``` text
••••••••
```

The mask is presentation only.

The underlying value is not mutated.

### 5.3 PARTIAL_MASK

Reveal only a configured safe portion.

Examples:

``` text
•••• 1234
N•••••• A
```

Partial-mask rules are metadata/policy driven.

The framework must not assume bank-account, identity-card, phone-number,
or tax-number semantics.

### 5.4 HIDE

Do not render the sensitive value.

Layout behavior may be:

-   preserve space;
-   collapse value area;
-   show localized "Hidden";
-   show a privacy icon.

The exact behavior is configurable.

### 5.5 CAPTURE_PROTECT

Request platform capture protection for the applicable surface where
supported.

`CAPTURE_PROTECT` must define a fallback:

``` text
CaptureProtectionFallback =
    MASK
    PARTIAL_MASK
    HIDE
```

A request for capture protection without a safe fallback is invalid for
sensitive content.

------------------------------------------------------------------------

## 6. Sensitive Content Metadata

Introduce a generic metadata contract conceptually similar to:

``` text
SensitiveContentDefinition

Sensitivity
PrivacyPresentation
CaptureProtectionFallback
AllowTemporaryReveal
TemporaryRevealDuration
AllowCopyWhenRevealed
AllowExportWhenRevealed
AllowSearchRawValue
AllowNotificationRawValue
AllowTooltipRawValue
AllowAccessibilityRawValue
MaskPattern?
PartialMaskDefinition?
PolicyCode?
```

The concrete implementation may use composition rather than one large
class.

The public contract must remain application-neutral.

------------------------------------------------------------------------

## 7. Metadata Attachment

Sensitivity/privacy metadata may be attached to:

-   `VariableDefinition`;
-   `ColumnDefinition`;
-   form field definition;
-   report field definition;
-   search result field descriptor;
-   notification payload field descriptor;
-   context-panel field descriptor;
-   document metadata field;
-   action/menu secondary text where data-bound;
-   future template-specific field definitions.

A more specific definition may override a general definition only
according to a documented precedence rule.

------------------------------------------------------------------------

## 8. Policy Resolution

### 8.1 Central resolver

Use a centralized resolver conceptually similar to:

``` text
IPrivacyPolicyResolver
```

Input may include:

-   sensitivity metadata;
-   requested privacy mode;
-   application mandatory policy;
-   user preference;
-   Company Context;
-   workspace/template context;
-   permission/capability result;
-   platform capture capability;
-   temporary reveal state.

Output conceptually:

``` text
ResolvedPrivacyPresentation

IsAuthorized
EffectivePrivacyMode
EffectiveSensitivity
Presentation
CanReveal
CanCopy
CanExport
CanSearchRaw
CanNotifyRaw
CanExposeToAccessibility
CaptureProtectionRequested
CaptureProtectionAvailable
FallbackApplied
PolicyReasonCode
```

### 8.2 Determinism

Same metadata + same context + same policy state must resolve to the
same presentation.

UI controls must not independently invent privacy behavior.

### 8.3 Fail-closed exceptions

A resolver exception must not cause raw `RESTRICTED` content to be
rendered.

------------------------------------------------------------------------

## 9. Policy Precedence

Recommended precedence, strictest wins:

``` text
Authorization
↓
Mandatory organization/application policy
↓
Sensitivity minimum policy
↓
Company/workspace policy
↓
User Privacy Mode
↓
Temporary Reveal eligibility
↓
Platform capability/fallback
↓
Effective presentation
```

A user preference cannot weaken mandatory policy.

------------------------------------------------------------------------

## 10. User-Controlled Privacy UI

### 10.1 Shell action

Privacy is a generic Shell action.

Recommended compact presentation:

``` text
Privacy ▼
```

or icon + tooltip.

It may appear in:

-   Top Action Bar;
-   Bottom Action Bar;
-   Application Menu;
-   Shell utility region.

Do not require all surfaces simultaneously.

### 10.2 Dynamic menu

Recommended menu:

``` text
Privacy
✓ Auto
  On
  Off
────────────
Reveal sensitive values temporarily
Privacy settings…
```

The menu must reuse DynamicUI24 v0.9 `MenuDefinition` / action
infrastructure.

Do not build a second menu framework.

### 10.3 State indication

The Shell must make effective privacy state discoverable without large
permanent banners.

Examples:

-   privacy icon state;
-   compact status text;
-   tooltip;
-   Bottom status item.

### 10.4 Mandatory-policy indication

If user selects `OFF` but mandatory policy still protects restricted
values, the UI should not misleadingly imply that all protection is
disabled.

Example semantic state:

``` text
Privacy: Off
Restricted protection: Required by policy
```

Presentation may remain compact.

------------------------------------------------------------------------

## 11. Temporary Reveal

### 11.1 Purpose

Temporary reveal allows an authorized user to inspect eligible sensitive
information without permanently turning Privacy Mode off.

### 11.2 Contract

Conceptually:

``` text
TemporaryRevealState

Scope
StartedAt
ExpiresAt
ContextGeneration
Reason?
```

### 11.3 Scope

Possible generic scopes:

``` text
FIELD
ROW
SELECTION
WORKSPACE
```

v0.10 does not require every scope in the first implementation.

### 11.4 Timeout

Timeout is configurable by policy.

Do not hard-code 30 or 60 seconds into Core.

### 11.5 Revocation

Temporary reveal must end on applicable events such as:

-   timeout;
-   user manually hides;
-   Company switch;
-   workspace switch;
-   authorization change;
-   application lock;
-   stricter policy activation.

### 11.6 No reveal for mandatory-hidden content

If mandatory policy says a field cannot be revealed, the reveal action
is hidden/disabled.

------------------------------------------------------------------------

## 12. Masking Rules

### 12.1 Masking must be presentation-only

Raw values remain in authorized application/provider state only as
required for normal operation.

Mask strings must not overwrite candidate values.

### 12.2 Type-independent behavior

Masking must support:

-   text;
-   numeric;
-   date/time;
-   boolean;
-   choice/reference display values;
-   calculated values;
-   document metadata.

### 12.3 No value-length leakage by default

Default full masking should not necessarily reveal the exact source
length.

Applications may opt into fixed-length or format-preserving masks.

### 12.4 Partial-mask provider

A generic extension may provide safe partial-mask formatting.

No business-specific mask algorithms in Core.

------------------------------------------------------------------------

## 13. Grid Integration

### 13.1 Cell presentation

Grid cells resolve privacy after permission/capability and before final
text presentation.

### 13.2 Virtualization

Privacy state must not break 100K+ virtualization.

Do not create one persistent privacy object per logical cell.

Privacy should resolve from:

``` text
Column/Variable metadata
+
row/value context where required
+
current privacy state
```

### 13.3 Selection

Selecting a masked cell must not automatically reveal it.

### 13.4 Editing

For an editable sensitive field:

-   entering edit mode does not automatically imply reveal;
-   application policy decides whether editor shows raw, masked, or
    requires explicit reveal;
-   candidate values must not leak into validation messages.

### 13.5 Formula/System columns

Sensitivity applies independently from `INPUT`, `FORMULA`, and `SYSTEM`.

A calculated value can still be `RESTRICTED`.

------------------------------------------------------------------------

## 14. Form / Editor Integration

Form controls must support the same effective privacy policy as Grid
cells.

Sensitive presentation must not be implemented only through password
text boxes.

Generic editors should be capable of:

-   masked display;
-   partial mask;
-   hidden display;
-   explicit reveal;
-   privacy-aware edit state.

------------------------------------------------------------------------

## 15. Context / Inspector Panel

The right-side Context/Inspector Panel introduced in v0.9 must
re-resolve sensitive fields.

A value masked in the main workspace must not appear raw in the Context
Panel unless policy explicitly permits that presentation.

------------------------------------------------------------------------

## 16. Tooltip and Hover Safety

Tooltips, hover cards, validation popovers, and flyouts are common
leakage paths.

Default rule:

``` text
Protected source value
→ secondary transient UI must not expose a less-protected representation
```

`AllowTooltipRawValue` must default conservatively for sensitive
content.

------------------------------------------------------------------------

## 17. Search Integration

### 17.1 Search indexing vs presentation

Search-provider authorization and indexing are application concerns.

DynamicUI24 controls result presentation.

### 17.2 Result masking

Search result title/subtitle/snippet must apply privacy policy.

### 17.3 No search-based bypass

A masked Grid value must not become visible merely by searching for the
record.

### 17.4 Matching without revealing

An application may support matching against authorized sensitive data
while returning a masked result.

Example:

``` text
query matches restricted record
→ result appears
→ sensitive snippet remains masked
```

### 17.5 Global/Navigation/Workspace scopes

All three v0.9 search scopes must respect effective privacy
presentation.

------------------------------------------------------------------------

## 18. Favorites, Pinned and Recent

Favorites/Pinned/Recent entries must not persist raw sensitive display
values unnecessarily.

Prefer stable semantic identifiers and localized safe labels.

Recent-history presentation must re-resolve privacy at render time.

------------------------------------------------------------------------

## 19. Notification & Guidance Integration

### 19.1 Notification payloads

Notification definitions should avoid raw sensitive values where
possible.

### 19.2 Presentation

If a notification contains a sensitive field descriptor, all surfaces
must resolve the same privacy policy:

-   Notification Center;
-   Toast;
-   Banner;
-   Alert Card;
-   Top Action Bar;
-   Bottom Action Bar.

### 19.3 No toast leakage

A workspace may be hidden while a toast is globally visible.

Therefore restricted raw values must not be placed into a toast merely
because the user can access the originating workspace.

### 19.4 Deduplication

Privacy transformations must not break logical notification
deduplication.

Dedup should use semantic IDs, not masked display strings.

------------------------------------------------------------------------

## 20. Clipboard Policy

### 20.1 Copy is separate from visibility

A value being visible does not automatically mean copy is allowed.

Resolve:

``` text
CanCopy
```

independently.

### 20.2 Privacy ON

Default recommended behavior for sensitive values:

-   copy masked representation; or
-   block copy with a compact guidance message.

Application policy chooses.

### 20.3 Temporary reveal

Temporary reveal does not automatically permit raw copy.

`AllowCopyWhenRevealed` is explicit.

### 20.4 Grid range copy

10C rectangular copy must apply privacy per cell.

A mixed range may produce masked values for protected cells without
exposing raw content.

### 20.5 Clipboard diagnostics

Do not include rejected raw values in error messages.

------------------------------------------------------------------------

## 21. Cut / Paste

Cut inherits copy policy and edit authorization.

Privacy presentation does not alter paste parsing semantics.

A protected destination may accept an authorized paste without exposing
the resulting raw value afterward.

Undo/redo history must not expose raw values through UI diagnostics.

------------------------------------------------------------------------

## 22. Import Integration

Import mapping and preview introduced in 10D must respect target
sensitivity.

### 22.1 Source preview

Raw source data may itself be sensitive.

Import preview should support sensitive target-aware masking once
mapping is known.

### 22.2 Diagnostics

Conversion/validation errors must avoid dumping full restricted raw
values.

Use bounded/safe previews.

### 22.3 Mapping UI

Target sensitivity may be indicated without exposing values.

------------------------------------------------------------------------

## 23. Export Policy

### 23.1 Export is not implied by read permission

Export permission/capability remains authoritative.

Privacy policy may additionally require:

-   confirmation;
-   masked export;
-   omission;
-   mandatory protection;
-   application-specific approval.

### 23.2 ExportDefinition integration

`ExportFieldDefinition` should be able to resolve sensitivity/privacy
policy.

### 23.3 Export preview

Export preview must not bypass masking.

### 23.4 Background/streaming export

100K+ streaming export must enforce field security before writer output.

Do not rely on Grid visual masking to secure exported data.

### 23.5 Raw export

Raw export of sensitive fields is an explicit authorized operation, not
a side effect of Privacy Mode `OFF`.

------------------------------------------------------------------------

## 24. Report / Document Presentation

Sensitive metadata used in reports/document preview should follow the
same semantic policy where technically applicable.

The framework must distinguish:

-   on-screen privacy presentation;
-   generated artifact content;
-   authoritative export/report permissions.

Masking a screen does not automatically rewrite an already-generated
document.

------------------------------------------------------------------------

## 25. Accessibility

### 25.1 Automation peers

Masked visual content must not automatically expose raw values through
accessibility automation properties.

### 25.2 Accessible name/value

Use policy-aware accessible representations.

### 25.3 Reveal

If raw accessibility exposure is permitted while temporarily revealed,
it must expire/revoke with the same reveal lifecycle.

### 25.4 Usability

Privacy state must not be conveyed by color alone.

------------------------------------------------------------------------

## 26. Logging and Diagnostics

### 26.1 Framework logging

Framework logs should prefer:

-   IDs;
-   VariableCode;
-   policy codes;
-   error categories;
-   safe lengths/counts;
-   masked previews.

### 26.2 Raw sensitive values

Do not log raw sensitive values by default.

### 26.3 Exceptions

Exception messages from providers must not be blindly surfaced to UI if
they may contain sensitive data.

### 26.4 Debug mode

Debug mode is not a blanket authorization to log restricted values.

------------------------------------------------------------------------

## 27. Capture Protection

### 27.1 Abstraction

Introduce a platform-neutral abstraction conceptually:

``` text
ICaptureProtectionService
```

Core must not reference Win32, AppKit, X11, Wayland, or
platform-specific capture APIs.

### 27.2 Capability query

Conceptually:

``` text
CaptureProtectionCapability

SUPPORTED
PARTIAL
UNSUPPORTED
UNKNOWN
```

### 27.3 Scope

Platforms may support protection only at window level.

DynamicUI24 must not promise region-level capture exclusion if the OS
only provides window-level protection.

### 27.4 Fallback

If requested protection cannot be achieved at required granularity:

``` text
CAPTURE_PROTECT
→ configured MASK/PARTIAL_MASK/HIDE fallback
```

### 27.5 No fake success

UI must not report "capture protected" merely because the request was
attempted.

------------------------------------------------------------------------

## 28. Windows Platform Adapter

Windows implementation may use supported OS window/content-affinity
capabilities where appropriate.

The adapter must:

-   be isolated from Core;
-   report actual capability/result;
-   tolerate unsupported environments;
-   not crash if remote/capture environment differs;
-   fall back to presentation masking.

Exact API selection belongs to implementation/adoption documentation,
not business metadata.

------------------------------------------------------------------------

## 29. macOS Platform Adapter

macOS implementation must use supported platform capabilities where
available.

If reliable capture exclusion is unavailable for the required scope, the
framework must use mask/hide fallback.

Do not introduce private/unsupported OS hacks merely to imitate another
application's capture behavior.

------------------------------------------------------------------------

## 30. Linux Platform Adapter

Linux behavior may differ between:

-   X11;
-   Wayland;
-   desktop environments;
-   compositors.

Capture protection may be unavailable or inconsistent.

The framework must:

-   report capability honestly;
-   preserve privacy through mask/hide fallback;
-   keep application behavior functional.

------------------------------------------------------------------------

## 31. Remote Desktop

Remote desktop behavior is platform/tool dependent.

DynamicUI24 may use reliable platform signals when available, but must
not assume every remote desktop tool is detectable.

Privacy Mode remains useful even without remote-session detection
because the user can explicitly select `ON`.

------------------------------------------------------------------------

## 32. Screen Sharing / Presentation

AUTO may integrate with reliable presentation/screen-sharing signals in
future adapters.

v0.10 requires the architecture seam, not universal detection.

User-controlled `ON` remains the dependable cross-platform mechanism.

------------------------------------------------------------------------

## 33. Privacy Preference Persistence

### 33.1 User preference

Requested Privacy Mode may be stored as a per-user presentation
preference.

### 33.2 Not published metadata

Changing `OFF/ON/AUTO` must not mutate shared application metadata.

### 33.3 Scope

Persistence may be:

-   global user preference;
-   application preference;
-   Company preference;
-   workspace preference.

A consuming application chooses supported scope.

### 33.4 Mandatory policy

Persisted preference must be re-resolved against current mandatory
policy every session.

------------------------------------------------------------------------

## 34. Company Context

On Company switch:

-   revoke temporary reveal;
-   increment privacy/context generation;
-   re-resolve effective privacy;
-   discard stale presentation results;
-   do not reuse sensitive display cache across Company contexts unless
    explicitly safe.

------------------------------------------------------------------------

## 35. Workspace Context

On workspace switch:

-   revoke workspace-scoped reveal;
-   re-resolve sensitivity metadata;
-   update Privacy Action state;
-   prevent stale async UI from rendering previous workspace raw values.

------------------------------------------------------------------------

## 36. Permission / Capability Changes

If authorization becomes more restrictive while a value is visible:

``` text
authorization update
→ invalidate presentation
→ hide/remove raw value
```

Privacy caches must not delay the stricter state.

------------------------------------------------------------------------

## 37. Async/Stale-State Protection

Privacy resolution that depends on async provider/context data must
carry generation/context identity.

Example:

``` text
Company A
→ resolve sensitive presentation async
→ switch Company B
→ A result returns late

A result must not update B UI.
```

------------------------------------------------------------------------

## 38. Caching

Privacy caches may store resolved policy metadata.

Avoid caching raw restricted display strings unnecessarily.

Cache keys must include all context that can affect effective privacy.

------------------------------------------------------------------------

## 39. Design Tokens

Add semantic privacy tokens rather than application colors.

Conceptual tokens:

``` text
Privacy.Mask.Foreground
Privacy.Mask.Background
Privacy.Hidden.Foreground
Privacy.Icon.Foreground
Privacy.Reveal.Focus
Privacy.Restricted.Indicator
```

Actual token naming follows existing design-system conventions.

Themes provide values.

------------------------------------------------------------------------

## 40. Icons

Public metadata uses `IconKey`.

Suggested semantic icon keys:

``` text
Privacy
PrivacyOn
PrivacyOff
PrivacyAuto
Reveal
Hide
Restricted
```

SVG/font glyph resolution remains behind the existing icon registry.

------------------------------------------------------------------------

## 41. Top Action Bar

Privacy action may be contributed to the Top Action Bar.

It must support existing:

-   size presets;
-   icon geometry;
-   dropdown behavior;
-   permission/capability state;
-   localization;
-   theme;
-   scale.

No new toolbar framework.

------------------------------------------------------------------------

## 42. Bottom Action Bar

Optional compact status:

``` text
Privacy: On
```

or icon-only state.

Bottom status may also indicate:

``` text
3 sensitive fields hidden
```

only if useful and inexpensive.

Do not create noisy counters by default.

------------------------------------------------------------------------

## 43. Application Menu

Privacy Settings may be reachable from the existing Application Menu.

Do not create a separate settings shell.

------------------------------------------------------------------------

## 44. Context / Flyout Menus

Sensitive field context menu may expose safe actions such as:

``` text
Reveal temporarily
Hide now
Copy masked value
```

Actions must be dynamically resolved.

If an action is not allowed, hide/disable according to standard
capability policy.

------------------------------------------------------------------------

## 45. Progressive Disclosure

Privacy UI follows v0.9 progressive-disclosure principles.

The normal workspace remains clean.

Advanced privacy settings should not permanently occupy primary
workspace space.

------------------------------------------------------------------------

## 46. Localization

Required:

``` text
vi-VN
en-US
```

Localize:

-   Privacy;
-   Auto/On/Off;
-   Reveal;
-   Hidden;
-   restricted-policy explanations;
-   copy/export blocked guidance;
-   capture-protection status/fallback messages.

Technical codes remain invariant.

------------------------------------------------------------------------

## 47. Theme

Required:

``` text
System
Light
Dark
```

Masking and privacy indicators must remain legible in all themes.

Do not encode sensitivity only through a specific color.

------------------------------------------------------------------------

## 48. UI Scale / Font Scale

Privacy controls and masked values must respect existing DynamicUI24
UI/font scaling.

Changing scale must not:

-   reveal content;
-   reset Privacy Mode;
-   extend reveal timeout;
-   lose policy state.

------------------------------------------------------------------------

## 49. Keyboard Interaction

Privacy controls must be keyboard accessible.

Do not reserve a global shortcut without clear value and conflict
analysis.

Menu navigation follows v0.9 menu keyboard behavior.

Temporary reveal must be explicitly invoked, not accidentally triggered
by normal navigation keys.

------------------------------------------------------------------------

## 50. Focus

Keyboard focus on sensitive content does not imply reveal.

Focus visuals must remain distinct from privacy state.

------------------------------------------------------------------------

## 51. Selection

Grid row/cell/range selection does not change sensitivity.

A selected masked cell remains masked.

------------------------------------------------------------------------

## 52. Sorting and Filtering

Sorting/filtering may operate on authorized underlying values through
provider logic.

Presentation remains privacy-aware.

Do not expose raw values in filter chips/criteria summaries unless
policy permits.

------------------------------------------------------------------------

## 53. Validation

Validation messages should identify the field safely.

Avoid:

``` text
Invalid value: 1234567890123456
```

Prefer:

``` text
Invalid value for BANK_ACCOUNT
```

or safe masked preview.

------------------------------------------------------------------------

## 54. Dirty State

Privacy toggles are presentation preference changes, not business dirty
state.

Temporary reveal is not business dirty state.

Changing sensitivity metadata in Setup may be metadata dirty state
according to normal Setup lifecycle.

------------------------------------------------------------------------

## 55. Setup Designer

Setup should eventually allow authorized designers to configure
sensitivity/privacy metadata.

v0.10 defines the metadata contract.

Implementation may be staged.

Designer must not expose raw sample production data merely to configure
masking.

------------------------------------------------------------------------

## 56. Published Metadata

Sensitivity classification may be published metadata.

User Privacy Mode is not.

Temporary reveal state is not.

------------------------------------------------------------------------

## 57. Metadata Validation

Reject/diagnose invalid combinations.

Examples:

``` text
Sensitivity = NORMAL
PrivacyPresentation = CAPTURE_PROTECT
```

may be allowed if explicitly configured.

But:

``` text
CAPTURE_PROTECT
Fallback = NONE
Sensitivity = RESTRICTED
```

should be invalid or fail closed according to policy.

------------------------------------------------------------------------

## 58. Policy Provider Extension

Applications may contribute privacy policy through a registered
provider.

Conceptually:

``` text
IPrivacyPolicyProvider
```

Providers may add stricter rules.

Providers must not weaken mandatory framework/application minimums
unless explicitly defined by authoritative policy precedence.

------------------------------------------------------------------------

## 59. Privacy State Provider

Runtime may expose read-only observable privacy state to templates.

Templates consume state; they do not own the global Shell privacy state.

------------------------------------------------------------------------

## 60. Sensitive Value Presenter

Prefer a reusable presentation service/primitive rather than repeated
masking logic.

Conceptually:

``` text
ISensitiveValuePresenter
```

Input:

-   value/display value;
-   metadata;
-   context.

Output:

-   safe display representation;
-   presentation flags;
-   accessibility representation.

------------------------------------------------------------------------

## 61. Raw Value Lifetime

v0.10 does not attempt to make managed-memory values impossible to
inspect.

However, UI framework code should avoid creating unnecessary raw copies
of restricted values.

Especially avoid:

-   concatenating raw values into diagnostic strings;
-   caching raw tooltip text;
-   duplicating raw values into notification text;
-   persisting raw display history.

------------------------------------------------------------------------

## 62. Search Provider Contracts

Search providers must be able to return semantic sensitive-field
descriptors or already-safe display data.

Framework must document whether a field is:

``` text
RAW_AUTHORIZED_VALUE
SAFE_DISPLAY_VALUE
```

to avoid double masking or accidental exposure.

------------------------------------------------------------------------

## 63. Notification Provider Contracts

Same principle applies to notification providers.

Prefer semantic payload fields over preformatted strings when sensitive
data may be involved.

------------------------------------------------------------------------

## 64. Export Provider Contracts

Export security must not depend on visual masking.

Provider/writer path must receive an explicit resolved export policy.

------------------------------------------------------------------------

## 65. Import Provider Contracts

Import source diagnostics may classify source fields as sensitive after
mapping.

Framework should support safe diagnostic preview without changing parser
responsibilities.

------------------------------------------------------------------------

## 66. Report Providers

Report providers remain responsible for authoritative report generation.

DynamicUI24 may protect on-screen report parameters/metadata.

Generated artifacts require explicit application/export policy.

------------------------------------------------------------------------

## 67. Document Preview

A document may contain sensitive information internally.

Field-level privacy masking cannot necessarily redact arbitrary PDF/DOCX
pixels/text.

Therefore document preview privacy may require:

-   whole-preview hide;
-   whole-preview overlay;
-   application-provided redacted document;
-   capture-protected window if supported.

Do not claim generic field masking can redact arbitrary document
formats.

------------------------------------------------------------------------

## 68. Images

Image content cannot be safely field-masked without explicit
regions/redaction metadata.

For sensitive images, generic fallback is whole-content
hide/overlay/capture protection.

------------------------------------------------------------------------

## 69. Audit

DynamicUI24 does not define authoritative compliance audit storage.

It may emit generic events such as:

``` text
PrivacyModeChanged
SensitiveRevealStarted
SensitiveRevealEnded
RawCopyRequested
SensitiveExportRequested
```

Consuming applications decide whether/how those events become
authoritative audit records.

Do not log raw sensitive values in audit event payloads.

------------------------------------------------------------------------

## 70. Telemetry

Telemetry must use semantic event codes and counts.

Examples:

``` text
privacy_mode_changed
temporary_reveal_used
capture_protection_fallback
sensitive_copy_blocked
```

Do not include raw values.

------------------------------------------------------------------------

## 71. DLP Boundary

DynamicUI24 privacy presentation is not a DLP engine.

It does not own:

-   USB blocking;
-   print control at OS policy level;
-   endpoint file exfiltration monitoring;
-   network DLP;
-   email DLP;
-   process injection prevention;
-   kernel capture prevention;
-   MDM;
-   EDR.

Applications may integrate enterprise security products independently.

------------------------------------------------------------------------

## 72. Printing

Printing is an output/export operation.

Visibility on screen does not imply print permission.

Sensitive print behavior belongs to export/output policy.

------------------------------------------------------------------------

## 73. Screenshot Commands

If an application provides its own screenshot/export-image command, it
must respect privacy policy.

DynamicUI24 cannot guarantee control over external screenshot tools.

------------------------------------------------------------------------

## 74. Drag and Drop

Dragging sensitive values/data out of the application is an output
channel.

Future drag/drop support must resolve privacy/export policy.

v0.10 does not require generic drag/drop DLP implementation.

------------------------------------------------------------------------

## 75. Clipboard History

Once raw content is placed on the operating-system clipboard, external
clipboard-history behavior may be outside framework control.

Therefore raw sensitive copy must be an explicit policy decision.

------------------------------------------------------------------------

## 76. Autofill / Suggestion Surfaces

Future autocomplete/suggestion UI must not expose restricted historical
values while Privacy Mode requires masking.

------------------------------------------------------------------------

## 77. Error Surfaces

All error surfaces must be treated as presentation surfaces.

This includes:

-   inline validation;
-   toast;
-   banner;
-   dialog;
-   blocking notice;
-   diagnostics panel.

------------------------------------------------------------------------

## 78. Dialogs

Dialogs displaying sensitive values use the same policy resolver.

A modal dialog is not inherently safer than the main workspace.

------------------------------------------------------------------------

## 79. Navigation Labels

Navigation metadata normally should not contain sensitive business
values.

If dynamic data appears in navigation labels, it must be privacy-aware.

------------------------------------------------------------------------

## 80. Window Title / Task Switcher

Do not place raw sensitive values into window titles by default.

Window titles may be visible in:

-   task switchers;
-   desktop shell;
-   remote session;
-   screenshots.

------------------------------------------------------------------------

## 81. OS Notifications

Future native OS notifications must be considered external/high-exposure
surfaces.

Raw sensitive content should default to prohibited.

------------------------------------------------------------------------

## 82. Lock / Inactivity Integration

The architecture should permit consuming applications to force Privacy
Mode `ON` or revoke reveal after lock/inactivity.

v0.10 does not define a full session-lock engine.

------------------------------------------------------------------------

## 83. Multi-window Applications

Privacy state may be application-wide or window-scoped according to host
policy.

Mandatory policy applies to all windows.

Temporary reveal must have explicit scope.

------------------------------------------------------------------------

## 84. Multiple Monitors

Privacy Mode does not assume monitor identity.

Future AUTO policy may use reliable display context signals, but manual
`ON` remains available.

------------------------------------------------------------------------

## 85. Persistence Security

Do not persist temporary reveal state across restart.

Do not persist raw masked-display caches.

Persist only the user preference and safe policy identifiers as
appropriate.

------------------------------------------------------------------------

## 86. Serialization

Sensitivity/privacy metadata must use stable semantic codes.

Unknown future enum values should fail safely for restricted content.

------------------------------------------------------------------------

## 87. Backward Compatibility

Existing v0.9 metadata without sensitivity fields behaves as:

``` text
Sensitivity = NORMAL
```

unless an application-level policy supplies a stricter classification.

v0.10 must not make all existing UI masked by default.

------------------------------------------------------------------------

## 88. Adoption Strategy

Applications may adopt privacy incrementally:

1.  implement Shell Privacy Mode;
2.  classify highest-risk variables;
3.  enable Grid/Form masking;
4.  protect Clipboard/Notification/Search;
5.  add Export policy integration;
6.  add platform capture adapter;
7.  expand Setup designer support.

------------------------------------------------------------------------

## 89. Default Safety Recommendations

Framework defaults should favor:

``` text
CONFIDENTIAL + Privacy ON → MASK
RESTRICTED + Privacy ON   → MASK/HIDE
Capture unsupported       → MASK/HIDE fallback
Raw notification          → deny by default
Raw tooltip               → deny by default
Raw accessibility value   → deny while masked
```

Applications may configure stricter policy.

------------------------------------------------------------------------

## 90. Performance

Privacy must remain cheap enough for virtualized grids.

Requirements:

-   no O(total logical rows) privacy state;
-   no per-cell long-lived objects for 100K+ datasets;
-   policy resolution cache bounded and context-safe;
-   changing Privacy Mode invalidates presentation efficiently;
-   no full provider reload merely to mask/unmask visible values where
    avoidable.

------------------------------------------------------------------------

## 91. 100K Grid Compatibility

A 100K+ DataEntry grid must retain:

-   viewport virtualization;
-   bounded materialization;
-   bounded cache;
-   selection state;
-   edit transactions;
-   copy/paste;
-   import/export streaming.

Privacy toggling must not materialize all rows.

------------------------------------------------------------------------

## 92. Import/Export 100K Compatibility

Privacy policy must integrate with streaming import/export without
buffering all records.

For export, security resolution is applied per field/batch.

For import preview, diagnostics remain bounded.

------------------------------------------------------------------------

## 93. Threading

UI presentation changes occur through appropriate UI-dispatch
mechanisms.

Core privacy policy contracts remain UI-framework neutral where
practical.

Do not place platform capture API calls in Core.

------------------------------------------------------------------------

## 94. Cancellation

Long-running privacy-aware operations such as export remain cancellable
according to their existing capability.

Privacy mode changes during a long operation must follow documented
snapshot/live-policy semantics.

Recommended conservative rule for export:

-   capture policy/context at authorization/confirmation;
-   abort or revalidate if context becomes materially stricter.

------------------------------------------------------------------------

## 95. Platform Matrix

v0.10 inherits v0.7+ platform policy.

Tier-1/P0:

-   Windows x64;
-   Ubuntu LTS x64;
-   macOS Apple Silicon.

Tier-2/P1:

-   Windows ARM64;
-   macOS Intel.

Publish validation continues for:

``` text
win-x64
win-arm64
osx-arm64
osx-x64
linux-x64
```

Capture-protection feature parity is not required where OS capabilities
differ.

Mask/hide privacy presentation is required cross-platform.

------------------------------------------------------------------------

## 96. Testing --- Core Policy

Tests must cover:

-   OFF;
-   ON;
-   AUTO;
-   requested vs effective mode;
-   mandatory policy stricter than user mode;
-   NORMAL;
-   CONFIDENTIAL;
-   RESTRICTED;
-   NONE;
-   MASK;
-   PARTIAL_MASK;
-   HIDE;
-   CAPTURE_PROTECT fallback;
-   resolver failure fail-closed.

------------------------------------------------------------------------

## 97. Testing --- Temporary Reveal

Cover:

-   allowed reveal;
-   prohibited reveal;
-   timeout;
-   manual hide;
-   Company switch revoke;
-   workspace switch revoke;
-   authorization change revoke;
-   reveal does not imply copy;
-   reveal does not imply export.

------------------------------------------------------------------------

## 98. Testing --- Grid/Form

Cover:

-   visible normal value;
-   masked confidential value;
-   restricted hidden value;
-   selection does not reveal;
-   editing policy;
-   validation safe message;
-   virtualization preserved;
-   Privacy toggle with 100K logical rows.

------------------------------------------------------------------------

## 99. Testing --- Clipboard

Cover:

-   normal raw copy;
-   masked copy;
-   blocked copy;
-   mixed range;
-   temporary reveal with copy denied;
-   temporary reveal with copy explicitly allowed;
-   no raw value in failure diagnostics.

------------------------------------------------------------------------

## 100. Testing --- Search

Cover:

-   masked search result;
-   navigation result;
-   workspace result;
-   no raw sensitive subtitle leakage;
-   Company switch stale result ignored;
-   recent/favorite safe labels.

------------------------------------------------------------------------

## 101. Testing --- Notification

Cover all supported surfaces:

-   Toast;
-   Banner;
-   Alert Card;
-   Blocking Notice;
-   Notification Center;
-   Top Action Bar;
-   Bottom Action Bar.

One logical notification must not expose different raw sensitivity
levels accidentally across surfaces.

------------------------------------------------------------------------

## 102. Testing --- Import/Export

Cover:

-   sensitive import preview;
-   safe diagnostic;
-   export field exclusion;
-   masked export policy if supported;
-   raw export explicit authorization;
-   100K streaming remains bounded;
-   context change invalidation.

------------------------------------------------------------------------

## 103. Testing --- Accessibility

Cover:

-   masked value does not expose raw automation value;
-   hidden value safe;
-   privacy action accessible;
-   effective state announced;
-   reveal lifecycle reflected safely.

------------------------------------------------------------------------

## 104. Testing --- Capture Adapter

Use capability/fake adapter tests.

Cover:

-   supported;
-   unsupported;
-   partial;
-   failure;
-   fallback;
-   no false success state.

Native platform smoke validates only supported behavior available in the
test environment.

------------------------------------------------------------------------

## 105. Architecture Guards

Add guards ensuring:

1.  Core does not reference platform capture APIs.
2.  Privacy policy contracts remain application-neutral.
3.  Privacy does not grant authorization.
4.  Templates do not implement independent ad-hoc masking policy.
5.  Grid virtualization remains intact.
6.  Notification/Search/Clipboard use shared privacy resolution.
7.  Export security does not depend on Grid visual state.
8.  no business-specific sensitivity names in framework.
9.  no PayCalc24/Odoo dependency.
10. no DLP claims/implementation.
11. no arbitrary reflection/plugin loading for privacy.
12. no raw restricted values in framework test diagnostics where
    avoidable.
13. capture protection has safe fallback.
14. user OFF cannot override mandatory protection.
15. temporary reveal state is runtime-only.

------------------------------------------------------------------------

## 106. Demo Requirements

Provide neutral demo fields, for example:

``` text
PUBLIC_NOTE        NORMAL
CONTACT_REFERENCE  CONFIDENTIAL
PRIVATE_REFERENCE  RESTRICTED
```

Avoid payroll/tax-specific demo semantics.

Demo should prove:

-   Privacy OFF;
-   Privacy ON;
-   Privacy AUTO;
-   mandatory restricted protection;
-   Grid;
-   Form/detail;
-   Context Panel if available;
-   notification;
-   search result if runtime available;
-   copy;
-   export preview;
-   temporary reveal;
-   timeout/revoke;
-   capture capability/fallback;
-   vi-VN/en-US;
-   System/Light/Dark;
-   100K grid remains bounded.

------------------------------------------------------------------------

## 107. UX Guidance

Privacy should feel like a quiet utility, not a warning system.

Preferred qualities:

-   compact;
-   discoverable;
-   reversible where policy permits;
-   clear effective state;
-   no permanent large security banner;
-   no repeated modal prompts for ordinary toggles;
-   progressive disclosure for settings.

------------------------------------------------------------------------

## 108. Privacy Menu Example

``` text
Privacy
────────────────────────
✓ Auto
  On
  Off
────────────────────────
Reveal temporarily
Privacy settings…
```

When mandatory policy prevents full OFF:

``` text
Privacy: Off
Restricted content remains protected by policy
```

------------------------------------------------------------------------

## 109. Grid Example

Normal:

``` text
Employee | Department | Reference
A        | Operations | 123456789
```

Privacy ON:

``` text
Employee | Department | Reference
A        | Operations | ••••••••
```

The framework example is illustrative only and does not classify real
application data.

------------------------------------------------------------------------

## 110. Partial Mask Example

Metadata:

``` text
Sensitivity: CONFIDENTIAL
PrivacyPresentation: PARTIAL_MASK
PartialMask:
  PreserveSuffixCharacters: 4
```

Presentation:

``` text
•••• 6789
```

No business meaning is implied.

------------------------------------------------------------------------

## 111. Capture Fallback Example

``` text
Requested:
CAPTURE_PROTECT

Platform:
UNSUPPORTED

Fallback:
MASK

Effective:
MASK
```

This is a successful safe fallback, not successful capture exclusion.

------------------------------------------------------------------------

## 112. Permission + Privacy Example

``` text
Permission = DENY
PrivacyMode = OFF

Result:
NO VALUE
```

Privacy cannot override denial.

------------------------------------------------------------------------

## 113. Mandatory Policy Example

``` text
Permission = ALLOW
User PrivacyMode = OFF
Sensitivity = RESTRICTED
Mandatory minimum = HIDE

Result:
HIDE
```

------------------------------------------------------------------------

## 114. Temporary Reveal Example

``` text
Permission = ALLOW
Sensitivity = CONFIDENTIAL
PrivacyMode = ON
AllowTemporaryReveal = true

User invokes Reveal
→ raw display allowed for configured duration

Timeout
→ MASK
```

If `AllowCopyWhenRevealed = false`, copy remains blocked/masked during
reveal.

------------------------------------------------------------------------

## 115. Search Example

``` text
Search matches record
↓
Provider returns authorized semantic result
↓
Privacy resolver
↓
Title visible
Sensitive subtitle masked
↓
Navigate remains available
```

------------------------------------------------------------------------

## 116. Notification Example

Unsafe:

``` text
"Payment account 123456789 failed"
```

Preferred semantic model:

``` text
Notification:
  MessageKey: operation.failed
  Fields:
    - Code: account_reference
      Sensitivity: RESTRICTED
      Value: <authorized value>
```

Renderer applies safe presentation per surface.

------------------------------------------------------------------------

## 117. Export Example

``` text
Grid visually masked
↓
User selects Export
↓
Export permission
↓
Privacy/export policy
↓
Resolved field policy
↓
Writer
```

Never:

``` text
Grid is masked
→ assume export is safe
```

------------------------------------------------------------------------

## 118. Accessibility Example

Visual:

``` text
••••••••
```

Automation value must not silently be:

``` text
123456789
```

unless explicit policy permits raw accessibility exposure.

------------------------------------------------------------------------

## 119. Privacy Event Model

Generic runtime events may include:

``` text
PrivacyModeChanging
PrivacyModeChanged
PrivacyPolicyInvalidated
TemporaryRevealStarted
TemporaryRevealExpired
TemporaryRevealRevoked
CaptureProtectionChanged
```

Events carry semantic identifiers, not raw values.

------------------------------------------------------------------------

## 120. State Preservation

Theme/language/UI-scale changes preserve:

-   requested Privacy Mode;
-   effective mode;
-   selection;
-   navigation;
-   valid reveal expiry timestamp.

They must not restart reveal timeout.

------------------------------------------------------------------------

## 121. Application Restart

On restart:

-   restore allowed user Privacy Mode preference;
-   do not restore active temporary reveal;
-   re-resolve mandatory policy;
-   re-query platform capture capability.

------------------------------------------------------------------------

## 122. Unknown Metadata

Unknown sensitivity/presentation codes:

-   for clearly non-sensitive legacy metadata, preserve
    backward-compatible behavior;
-   for metadata explicitly marked sensitive but containing an unknown
    protection code, fail closed.

------------------------------------------------------------------------

## 123. Migration from v0.9

No destructive migration is required.

v0.9 definitions without privacy metadata remain valid.

Applications opt in by adding sensitivity/privacy metadata and
registering policy/capture services as required.

------------------------------------------------------------------------

## 124. Setup Adoption

Recommended future Setup fields:

``` text
Sensitivity
Privacy Presentation
Capture Fallback
Temporary Reveal Allowed
Reveal Duration
Copy Policy
Export Policy
Policy Code
```

Use standard editor registry and metadata designer patterns from Tasks
8--9.

------------------------------------------------------------------------

## 125. Provider Failure Isolation

One application privacy-policy provider failure must not crash Shell.

For sensitive content, failed provider contribution resolves
conservatively.

Provider failure may generate one deduplicated guidance notification.

Do not expose provider exception details containing data.

------------------------------------------------------------------------

## 126. Notification for Privacy Failures

Examples appropriate for N1:

-   capture protection unavailable, masking applied;
-   sensitive copy blocked;
-   export requires additional permission;
-   privacy policy could not be resolved.

Do not spam per field/cell.

------------------------------------------------------------------------

## 127. No Notification for Routine Masking

Normal Privacy Mode masking is expected state.

Do not generate a toast for every masked value or every privacy toggle.

------------------------------------------------------------------------

## 128. Privacy Settings

Settings UI may include:

-   default Privacy Mode;
-   optional preferred reveal duration within policy bounds;
-   optional behavior when capture protection unavailable;
-   application-provided privacy information.

Organization-mandated settings are read-only.

------------------------------------------------------------------------

## 129. Policy Bounds

Application policy may define:

``` text
MinimumPrivacyMode
MaximumRevealDuration
AllowUserPrivacyOff
AllowTemporaryReveal
AllowedCaptureFallbacks
```

The framework resolves user preferences inside those bounds.

------------------------------------------------------------------------

## 130. Local-AI Maintainability

Documentation must clearly state:

``` text
WHAT PRIVACY OWNS
WHAT PRIVACY DOES NOT OWN
AUTHORIZATION VS PRIVACY
SENSITIVITY MODEL
PRESENTATION MODEL
POLICY PRECEDENCE
TEMPORARY REVEAL
CAPTURE PROTECTION BOUNDARY
CLIPBOARD RULE
SEARCH RULE
NOTIFICATION RULE
EXPORT RULE
ACCESSIBILITY RULE
CONTEXT INVALIDATION
PLATFORM FALLBACK
FOCUSED TEST COMMANDS
COMMON FAILURE MODES
```

A future local AI/Dev must not need to reverse-engineer the whole
repository to understand the security boundary.

------------------------------------------------------------------------

## 131. Required Documentation for Implementation

A future implementation task should create/update at least:

``` text
docs/architecture/PRIVACY-PRESENTATION.md
docs/architecture/SENSITIVE-CONTENT.md
docs/architecture/CAPTURE-PROTECTION.md
docs/architecture/PRIVACY-POLICY-RESOLUTION.md

docs/adoption/PRIVACY-INTEGRATION.md
docs/adoption/SENSITIVE-FIELD-METADATA.md

docs/design-system/PRIVACY-STATES.md

docs/backlog/TASK-P1-BACKLOG.md
```

Exact file names may follow repository conventions.

------------------------------------------------------------------------

## 132. P1 Implementation Boundary

The first implementation task after S0.10 should focus on:

-   privacy contracts;
-   resolver;
-   Shell privacy state;
-   masking;
-   partial masking;
-   hiding;
-   temporary reveal;
-   Grid/Form integration;
-   clipboard integration;
-   notification/search safe presentation where runtime exists;
-   export-policy seam;
-   platform capture abstraction;
-   one or more real platform adapters only where reliable;
-   fallback;
-   tests/docs/demo.

P1 must not become a full enterprise DLP project.

------------------------------------------------------------------------

## 133. P1 Non-goals

Do not include:

-   endpoint DLP;
-   MDM;
-   EDR;
-   screenshot detection arms race;
-   webcam/camera detection;
-   watermarking engine unless separately specified;
-   DRM;
-   document redaction engine;
-   OCR redaction;
-   AI sensitivity classification;
-   automatic legal classification;
-   server-side encryption redesign;
-   database row-level security redesign;
-   audit database;
-   SIEM;
-   CASB;
-   remote desktop product detection matrix.

------------------------------------------------------------------------

## 134. Future Extensions

Potential future capabilities, not required by v0.10 implementation:

-   organization privacy policy administration;
-   watermark overlay;
-   privacy-aware print templates;
-   secure presentation mode;
-   reliable remote-session AUTO signals;
-   external-display policy;
-   document redaction providers;
-   privacy-aware drag/drop;
-   native OS notification protection;
-   privacy telemetry dashboards.

These must remain separate tasks.

------------------------------------------------------------------------

## 135. Acceptance Criteria for v0.10 Specification

S0.10 specification is acceptable when it clearly defines:

-   [ ] v0.9 lineage and immutable SHA;
-   [ ] repository baseline lineage;
-   [ ] user-controlled OFF/ON/AUTO;
-   [ ] requested vs effective mode;
-   [ ] NORMAL/CONFIDENTIAL/RESTRICTED;
-   [ ] NONE/MASK/PARTIAL_MASK/HIDE/CAPTURE_PROTECT;
-   [ ] mandatory-policy precedence;
-   [ ] temporary reveal;
-   [ ] bounded reveal duration;
-   [ ] reveal revocation;
-   [ ] capture capability abstraction;
-   [ ] safe fallback;
-   [ ] no absolute anti-capture claim;
-   [ ] Grid;
-   [ ] Form;
-   [ ] Context Panel;
-   [ ] Tooltip/flyout;
-   [ ] Search;
-   [ ] Favorites/Pinned/Recent;
-   [ ] Notification;
-   [ ] Clipboard;
-   [ ] Import preview;
-   [ ] Export;
-   [ ] Report/document boundary;
-   [ ] Accessibility;
-   [ ] diagnostics/logging;
-   [ ] Company/workspace stale-state protection;
-   [ ] permission/privacy separation;
-   [ ] 100K virtualization compatibility;
-   [ ] cross-platform fallback;
-   [ ] vi-VN/en-US;
-   [ ] System/Light/Dark;
-   [ ] architecture guards;
-   [ ] implementation documentation requirements;
-   [ ] P1 boundary;
-   [ ] explicit DLP non-goals.

------------------------------------------------------------------------

## 136. Normative Summary

DynamicUI24 v0.10 establishes the following invariant:

``` text
A user being authorized to access a value
does not require that value to remain continuously exposed.
```

The framework therefore provides a reusable privacy-presentation layer:

``` text
Authorized Value
      ↓
Sensitivity Metadata
      ↓
Mandatory Policy
      ↓
User Privacy Preference
      ↓
Context / Temporary Reveal
      ↓
Platform Capability
      ↓
Resolved Privacy Presentation
      ↓
Grid / Form / Search / Notification / Clipboard / Export / Accessibility
```

The security boundary remains explicit:

``` text
Authorization decides whether the user may access the data.

Privacy Presentation decides how authorized data is shown now.

Capture Protection requests platform assistance against supported capture paths.

DLP governs broader exfiltration controls outside DynamicUI24.
```

This separation is mandatory for all future privacy-related DynamicUI24
work.

------------------------------------------------------------------------

## Appendix A --- Canonical Privacy Contracts

Conceptual contracts:

``` text
PrivacyMode
  OFF
  ON
  AUTO

Sensitivity
  NORMAL
  CONFIDENTIAL
  RESTRICTED

PrivacyPresentation
  NONE
  MASK
  PARTIAL_MASK
  HIDE
  CAPTURE_PROTECT

CaptureProtectionCapability
  SUPPORTED
  PARTIAL
  UNSUPPORTED
  UNKNOWN
```

------------------------------------------------------------------------

## Appendix B --- Conceptual Resolved Policy

``` text
ResolvedPrivacyPresentation
{
    IsAuthorized
    RequestedPrivacyMode
    EffectivePrivacyMode
    EffectiveSensitivity
    Presentation
    CanReveal
    RevealExpiresAt?
    CanCopy
    CanExport
    CanSearchRaw
    CanNotifyRaw
    CanExposeToAccessibility
    CaptureProtectionRequested
    CaptureProtectionAvailable
    CaptureFallbackApplied
    PolicyReasonCode
}
```

This is conceptual. Implementation may split the contract into smaller
immutable types.

------------------------------------------------------------------------

## Appendix C --- Privacy Policy Resolution Example

``` text
function ResolvePrivacy(context):

    if authorization != ALLOW:
        return NO_VALUE

    mandatory = resolveMandatoryPolicy(context)
    sensitivity = resolveSensitivity(context.metadata)

    requested = context.userPrivacyMode
    effectiveMode = maxPrivacy(requested, mandatory.minimumMode)

    presentation =
        resolvePresentation(
            sensitivity,
            effectiveMode,
            mandatory)

    if context.temporaryReveal is valid:
        presentation =
            applyRevealIfAllowed(
                presentation,
                mandatory,
                context.temporaryReveal)

    if presentation == CAPTURE_PROTECT:
        capability = captureService.getCapability()

        if capability cannot satisfy required scope:
            presentation = configuredSafeFallback

    return resolved safe presentation
```

`maxPrivacy` is semantic policy precedence, not numeric enum comparison.

------------------------------------------------------------------------

## Appendix D --- Example Metadata

``` text
VariableCode: PRIVATE_REFERENCE

Sensitivity: RESTRICTED

Privacy:
  Presentation: CAPTURE_PROTECT
  CaptureFallback: MASK
  AllowTemporaryReveal: true
  TemporaryRevealDuration: PT30S
  AllowCopyWhenRevealed: false
  AllowExportWhenRevealed: false
  AllowNotificationRawValue: false
  AllowTooltipRawValue: false
  AllowAccessibilityRawValue: false
```

The example does not prescribe a business classification.

------------------------------------------------------------------------

## Appendix E --- Surface Matrix

  ------------------------------------------------------------------------
  Surface                       Must resolve privacy Raw exposure default
                                                     for sensitive content
  --------------------- ---------------------------- ---------------------
  Grid                                           Yes No while protected

  Form                                           Yes No while protected

  Context Panel                                  Yes No while protected

  Tooltip/Hover                                  Yes No

  Search result                                  Yes No

  Recent/Favorite label                          Yes Avoid raw values

  Notification Center                            Yes No

  Toast/Banner/Alert                             Yes No

  Clipboard                                      Yes Explicit policy

  Import preview                                 Yes Safe/masked where
                                                     applicable

  Export preview                                 Yes Explicit policy

  Export writer             Security policy required Explicit
                                                     authorization

  Accessibility                                  Yes No while protected
  automation                                         

  Framework logs                                 Yes No raw value by
                                                     default

  Window title                                   Yes Avoid raw values
  ------------------------------------------------------------------------

------------------------------------------------------------------------

## Appendix F --- Platform Fallback Matrix

  Requested presentation   Platform capability           Effective result
  ------------------------ ----------------------------- --------------------------
  MASK                     Any                           MASK
  PARTIAL_MASK             Any                           PARTIAL_MASK
  HIDE                     Any                           HIDE
  CAPTURE_PROTECT          Supported at required scope   CAPTURE_PROTECT
  CAPTURE_PROTECT          Partial/unsupported           Configured safe fallback
  CAPTURE_PROTECT          Unknown/error                 Configured safe fallback

------------------------------------------------------------------------

## Appendix G --- Privacy vs Authorization Examples

  Authorization   Privacy   Mandatory policy                Result
  --------------- --------- ------------------------------- ---------------------
  DENY            OFF       None                            No value
  DENY            ON        Any                             No value
  ALLOW           OFF       None                            Normal presentation
  ALLOW           ON        Confidential mask               Mask
  ALLOW           OFF       Restricted hide                 Hide
  ALLOW           AUTO      Capture required, unsupported   Safe fallback

------------------------------------------------------------------------

## Appendix H --- Temporary Reveal Rules

Temporary reveal:

-   is explicit;
-   is bounded;
-   is revocable;
-   is context-scoped;
-   never grants authorization;
-   never automatically grants copy;
-   never automatically grants export;
-   never persists across restart;
-   must fail closed when policy becomes stale or stricter.

------------------------------------------------------------------------

## Appendix I --- Privacy UX Principle

The default UI should remain visually clean.

Privacy is a compact utility:

``` text
[Privacy ▼]
```

not a permanent security dashboard.

Advanced controls belong behind progressive disclosure.

------------------------------------------------------------------------

## Appendix J --- Compatibility Statement

All v0.1--v0.9 requirements remain normative unless v0.10 explicitly
extends or clarifies them.

The immutable v0.9 specification remains the detailed source for:

-   Shell/Search/Favorites/Pinned/Recent;
-   Dynamic Menu;
-   Notification/Guidance;
-   Setup;
-   Dynamic Action Bars;
-   DataEntry;
-   History/Document;
-   Dashboard;
-   Signing;
-   report/document designer;
-   platform matrix;
-   design-system behavior.

v0.10 adds the cross-cutting privacy-presentation contract that those
capabilities must consume when sensitive content is present.

------------------------------------------------------------------------

**End of TS24 Dynamic UI Framework --- Specification v0.10**
