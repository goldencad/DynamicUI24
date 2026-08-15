# DynamicUI24

DynamicUI24 is a reusable metadata-driven UI framework for cross-platform .NET/Avalonia business applications.

This repository contains the .NET 9 solution structure, enforced module boundaries, a modular template registration/resolution foundation, and a minimal Avalonia proof host. Business UI behavior is intentionally deferred to later tasks.

PayCalc24 is a future consumer/reference implementation, not a dependency.

[Specification v0.6](docs/specification/DynamicUI24-Spec-v0.6.md) is the current source of truth.

## Build

```bash
dotnet restore DynamicUI24.slnx
dotnet build DynamicUI24.slnx -c Release --no-restore
dotnet test DynamicUI24.slnx -c Release --no-build
```

## Repository layout

- `src/` — framework core, shared contracts, Avalonia integration, templates, and optional extensions.
- `samples/` — the minimal consumer/reference host.
- `tests/` — foundational and executable architecture tests.
- `benchmarks/` — benchmark harness for later performance work.
- `docs/` — specification and concise architecture/adoption guidance.

## Documentation

- [Shared UI/UX design system](docs/design-system/OVERVIEW.md)
- [Architecture index](docs/architecture/README.md)
- [Adoption index](docs/adoption/README.md)
- [Template guidance](docs/templates/README.md)
