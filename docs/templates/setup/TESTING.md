# Setup testing

Run focused tests with:

```text
dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --filter FullyQualifiedName~SetupFoundationTests
```

Coverage includes hierarchy/order, 9+ catalogs, duplicate/cycle/orphan safety, permission states, definition identity validation, candidate isolation and cancel, create/clone, valid and invalid validation, publish, retire, read-only definitions, dirty navigation guard, editor resolution, all generic field types, Action Bar permissions/selection, and deterministic Company scope.

Shared Tree tests cover configurable initial/page sizes, incremental expansion, Show less, independent hierarchy windows, selection reveal, and permission/Company filtering before paging. The macOS smoke verifies Setup's 5 → 10 → 5 catalog window and preservation across vi/en and Light/Dark changes.

Architecture tests guard backend neutrality, registry-based editor resolution, semantic icons, shared Action Bars, template isolation, and absence of consumer-specific semantics.
