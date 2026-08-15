# Architecture

- [Presentation foundation](PRESENTATION-FOUNDATION.md)
- [Design tokens](DESIGN-TOKENS.md)
- [Icon registry](ICON-REGISTRY.md)
- [Localization](LOCALIZATION.md)
- [Shell host](SHELL-HOST.md)

DynamicUI24 is split into independently maintainable framework modules.

- `DynamicUI24.Core` is the domain-neutral foundation and has no UI dependency.
- `DynamicUI24.Shared` contains reusable cross-module contracts and primitives.
- `DynamicUI24.Avalonia` is the Avalonia hosting/presentation integration layer and depends only on Core and Shared within the repository.
- Template modules depend on Core and Shared but not on other templates.
- Extension modules are optional reusable capabilities and depend on Core and Shared.
- `DynamicUI24.Demo` is a consumer; no framework project may reference it.

Architecture tests inspect project-reference graphs and compiled assembly metadata to enforce these boundaries. DynamicUI24 never depends on PayCalc24 or another consumer application.

The Task 1 modular flow is documented in the [template contract](TEMPLATE-CONTRACT.md), [template registry](TEMPLATE-REGISTRY.md), and [module dependency rules](MODULE-DEPENDENCIES.md).
