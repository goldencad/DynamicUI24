# DataEntry Grid Core

Task 10A provides the reusable metadata-driven table runtime used by `DATA_ENTRY` workspaces. The flow is `GridDefinition + Task 9 ColumnDefinition + IDataEntryGridProvider + company/workspace/authorization context → DataEntryGridRuntime → DataEntryGridHost`.

## What Grid Core owns

- safe metadata resolution, `VariableCode` value binding and column presentation;
- async load states, provider failure isolation and generation-based stale-response rejection;
- `RowKey` selection, sort/filter request state and shared Action Bar status;
- one active cell candidate, generic required/type validation, commit and cancel;
- a localized, themed Avalonia table adapter with scrolling and keyboard basics.

## What it does not own

It does not own application authorization, persistence, business validation/calculation, formula execution, imports/exports, Global Search, Excel range behavior, layout persistence, grouping, pivoting, charting or 100K-row virtualization. Providers remain application composition concerns.

## Public contracts

Core contracts are in `DynamicUI24.Core.DataEntry`: `GridDefinition`, `GridSortDefinition`, `GridFilterDefinition`, `RowKey`, `GridRow`, `GridDataRequest`, `GridDataResult`, `IDataEntryGridProvider`, `DataEntryGridRuntime` and `GridEditBuffer`. `DataEntryGridHost` is the optional renderer in `DynamicUI24.Avalonia.Presentation`.

## Safe extension points

Add metadata without localized-title binding; add providers without UI types; add render/editor fallbacks in the Avalonia adapter; add generic validation only when it is metadata-level. Put application commands in the existing Action Bar command registry and guidance in the existing Notification infrastructure.

## Common failure modes

Duplicate columns/rows, invalid geometry, unknown enum values, missing `VariableCode` values, unavailable authorization and provider exceptions become diagnostics or safe states. Do not surface raw provider exceptions. A company change clears selection/edit state and starts a new generation.

## 10B virtualization extension point

`GridDataRequest` is the request boundary. Task 10B can add an optional viewport/window descriptor and return window metadata while retaining `GridDefinition`, Task 9 `ColumnDefinition`, `RowKey`, selection and edit contracts. Task 10A deliberately materializes only a modest current row set.

Focused commands are documented in [TESTING.md](TESTING.md).
