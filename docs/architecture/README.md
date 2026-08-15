# Architecture

- [Presentation foundation](PRESENTATION-FOUNDATION.md)
- [Design tokens](DESIGN-TOKENS.md)
- [Icon registry](ICON-REGISTRY.md)
- [Localization](LOCALIZATION.md)
- [Application Menu](APPLICATION-MENU.md)
- [Application Menu contributors](APPLICATION-MENU-CONTRIBUTORS.md)
- [Application shell](APP-SHELL.md)
- [Shell host](SHELL-HOST.md)
- [Company context](COMPANY-CONTEXT.md)
- [Authorization presentation](AUTHORIZATION-PRESENTATION.md)
- [Company profile](COMPANY-PROFILE.md)
- [Dynamic Ribbon](DYNAMIC-RIBBON.md)
- [Ribbon definition](RIBBON-DEFINITION.md)
- [Ribbon context](RIBBON-CONTEXT.md)
- [UI command registry](UI-COMMAND-REGISTRY.md)
- [Dynamic Action Bars](DYNAMIC-ACTION-BARS.md)
- [Action definition](ACTION-DEFINITION.md)
- [Action context](ACTION-CONTEXT.md)
- [Supported platforms](SUPPORTED-PLATFORMS.md)
- [Cross-platform dependencies](CROSS-PLATFORM-DEPENDENCIES.md)

DynamicUI24 is split into independently maintainable framework modules.

- `DynamicUI24.Core` is the domain-neutral foundation and has no UI dependency.
- `DynamicUI24.Shared` contains reusable cross-module contracts and primitives.
- `DynamicUI24.Avalonia` is the Avalonia hosting/presentation integration layer and depends only on Core and Shared within the repository.
- Template modules depend on Core and Shared but not on other templates.
- Extension modules are optional reusable capabilities and depend on Core and Shared.
- `DynamicUI24.Demo` is a consumer; no framework project may reference it.

Architecture tests inspect project-reference graphs and compiled assembly metadata to enforce these boundaries. DynamicUI24 never depends on PayCalc24 or another consumer application.

The Task 1 modular flow is documented in the [template contract](TEMPLATE-CONTRACT.md), [template registry](TEMPLATE-REGISTRY.md), and [module dependency rules](MODULE-DEPENDENCIES.md).
