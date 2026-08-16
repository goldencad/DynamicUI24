# DataEntry selection

## WHAT DATAENTRY OWNS
DataEntry owns active-cell navigation, compact semantic ranges, clipboard-safe selection, fill commands, and bounded select-all state.

## COLUMN IDENTITY
Range endpoints store `VariableCode`; visual bounds are re-resolved from the current visible order after reorder/hide/pin.

## ROW IDENTITY
Endpoints store opaque `RowKey` plus logical position. Position is navigation context, never backend identity.

## SELECTION CONTRACT
Ranges are rectangles with two endpoints and are not expanded into per-cell objects. Reorder changes the visual rectangle while preserving endpoint identities. Hidden columns are skipped by navigation.

## SELECT-ALL RULE
Ctrl/Cmd+A selects all rows in the current filtered result and all eligible visible columns. It sets one semantic flag; it does not allocate 100K row/cell objects. Copy or destructive editing of massive semantic selection requires an explicit supported/confirmed provider path.

## PRIVACY RULE
Keyboard and menus call the same privacy-aware clipboard path. Accessibility receives the presented/masked value only.

## IMPORT/EXPORT IDENTITY
Selection export may use semantic ranges, while import mapping remains `VariableCode`-based regardless of layout.

## 100K+ RULE
Selection size is calculated arithmetically. Fill Down/Right resolves only its bounded target and commits through the existing batch/validation/history transaction.

## FOCUSED TEST COMMANDS
`/usr/local/share/dotnet/dotnet test tests/DynamicUI24.Tests --no-restore --filter "GridEditingTests|GridViewportTests"`

## COMMON FAILURE MODES
Never allocate per selected cell, use visual index as identity, synchronously loop the dataset, bypass privacy, or bypass batch validation/history.
