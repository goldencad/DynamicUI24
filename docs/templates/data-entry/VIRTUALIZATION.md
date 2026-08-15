# DataEntry viewport virtualization

## What virtualization owns

Task 10B owns viewport request orchestration, modest overscan, cancellation, generation-based stale-result rejection, a three-window least-recently-used cache, logical row positions, and bounded Avalonia materialization. `DataEntryGridHost` renders only `DataEntryGridRuntime.Rows`; it never creates placeholder objects for the logical extent.

## What it does not own

It does not own persistence, database paging syntax, business rules, formula execution, Global Search, grouping, import/export, Excel behavior, lookup virtualization, or application-specific filtering. Those remain provider/application concerns.

## Viewport contract

`GridViewportRequest` carries visible `StartIndex`, `RequestedRowCount`, before/after overscan, sort/filter definitions, and `RequestGeneration`. `MaterializedStartIndex` and `MaterializedRowCount` describe the bounded provider range. `GridViewportResult` returns its start, immutable rows, logical `TotalRowCount`, generation, previous/next flags, safe provider state, and a diagnostic code. Invalid/negative requests throw at the boundary; malformed provider ranges become `GRID_VIEWPORT_RESULT_MALFORMED` without crashing the shell.

## Provider contract

`IVirtualizedGridDataProvider` is an optional capability extending `IDataEntryGridProvider`. Existing 10A providers still use `LoadAsync`; large providers implement `LoadViewportAsync` and must honor cancellation where practical. The provider retrieves, sorts, and filters data. The framework owns state adoption and presentation.

## RowKey vs index

An index is only a position in the current sorted/filtered logical sequence. `RowKey` is stable identity. Selection and `GridEditBuffer` store `RowKey`; neither allocates state for unselected logical rows.

## Stale request rule

Every load increments runtime generation and cancels the previous linked token. A result is adopted only when both the active runtime generation and the echoed result generation match. Cancellation saves work; generation matching provides correctness even when a provider ignores cancellation.

## Cache boundary

Defaults are 60 visible rows, 20 rows of overscan on each side, at most 300 materialized rows per window, and three LRU windows. Sort, filter, refresh, Company change, and workspace deactivation invalidate the cache. Eviction is deterministic and retained rows cannot grow with the logical dataset.

## Edit buffer interaction

The single candidate is keyed by `RowKey + VariableCode`, not an Avalonia cell. It survives window replacement and is restored when the row returns. Commit updates the current row and cached copies; cancel discards only the candidate.

## Common failure modes

- Returning a window at a start other than `MaterializedStartIndex` is malformed.
- Returning more than `MaterializedRowCount`, duplicate RowKeys, or rows beyond `TotalRowCount` is malformed.
- Converting selection to indexes breaks after sort/filter.
- Wrapping a complete logical list in a non-virtualizing panel defeats the contract; only window rows belong in the host.
- Retrying a saved obsolete task can restore stale context; use `RetryViewportAsync`, which creates a current-generation request.

## Focused test commands

Run from a directory that selects an installed .NET 9 SDK:

```sh
dotnet test /absolute/path/tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --no-restore --filter 'FullyQualifiedName~DataEntryGridTests|FullyQualifiedName~GridViewportTests' -m:1
dotnet test /absolute/path/tests/DynamicUI24.ArchitectureTests/DynamicUI24.ArchitectureTests.csproj --no-restore --filter FullyQualifiedName~DataEntryGridArchitectureTests -m:1
dotnet run --project /absolute/path/benchmarks/DynamicUI24.Benchmarks/DynamicUI24.Benchmarks.csproj -c Release -- --filter '*GridViewport*'
```
