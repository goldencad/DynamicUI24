# DataEntry testing

Run from a shell whose current directory does not select an incompatible SDK if necessary; this repository requires .NET SDK 9.0.200 in normal development/CI.

```sh
dotnet build samples/DynamicUI24.Demo/DynamicUI24.Demo.csproj --no-restore -m:1
dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --no-restore --filter 'FullyQualifiedName~DataEntryGridTests' -m:1
dotnet test tests/DynamicUI24.ArchitectureTests/DynamicUI24.ArchitectureTests.csproj --no-restore --filter 'FullyQualifiedName~DataEntryGridArchitectureTests' -m:1
dotnet run --project samples/DynamicUI24.Demo/DynamicUI24.Demo.csproj --no-build -- --smoke
```

The focused suite covers metadata, permission hiding, provider states/failures, duplicate row keys, stable/stale contexts, selection modes, editing/validation/commit/cancel and sort-selection behavior. The one macOS ARM64 smoke covers the real renderer, actions/status, company generation, localization and themes. CI owns the full test matrix and five-RID publishing.

Common test failures: a provider returning duplicate keys; using a localized label as a value key; a formula/system column remaining editable; UI-platform references leaking into Core; or a test runner denied its local loopback socket by a sandbox.
