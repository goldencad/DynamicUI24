# Presentation foundation

Reusable visual behavior follows the [DynamicUI24 design system](../design-system/OVERVIEW.md). Consumer applications should normally customize through metadata, tokens, registries, providers, and extension points rather than modifying shared controls.

Task 2 separates semantic presentation contracts from Avalonia rendering:

```
DynamicUI24.Shared (theme, icon, localization, state, brand, message contracts)
    -> DynamicUI24.Avalonia (resources, resolvers, controls and desktop services)
        -> consumer app (composition and optional brand overrides)
```

`ShellPresentation` is generic state for application identity, selected workspace, theme,
culture, status, and shared state. Theme and culture changes mutate those properties; they
do not recreate the shell or clear `CurrentWorkspaceId`.

`PresentationStateKind` defines Empty, Loading, Ready, Error, ReadOnly,
PermissionDenied, and Unavailable. Unavailable is an explicit state and is never inferred
from zero, false, empty, or a domain null. `ErrorPresentation` keeps a friendly message and
diagnostic code separate from optional developer details and retry availability.

`IMessageService` covers Information, Warning, Error, and Confirmation. It is intentionally
domain-neutral. `IApplicationExitService` lets the desktop implementation request graceful
Avalonia lifetime shutdown instead of calling `Environment.Exit`.
