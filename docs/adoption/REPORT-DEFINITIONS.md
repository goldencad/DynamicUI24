# Defining Reports

Choose stable uppercase semantic codes independent of translated labels and ordering. Define typed parameters, 10+ dynamic columns where useful, defaults, typed sort/filter, grouping, provider-supplied aggregates, drill-down targets, and explicit export scopes. Optional `VariableCode` links a report column to an existing application semantic; database field names must not become Core identity.

Parameters are `EditorDefinition` instances resolved by the Universal Editor foundation; Report owns no parameter-type or presenter hierarchy. Report, parameter, and column metadata project through the existing `UiElementKind` authoring seams, `HelpContextCode`, Dynamic Authorization bindings, and P1 metadata.

Keep definitions business-neutral and immutable. Preferences overlay order, percentage width, visibility, grouping, and Find scope; they never modify definitions or contain row data/raw sensitive parameter values. Metadata evolution ignores removed columns, applies defaults to new columns, and cannot resurrect unauthorized columns. Report metadata contains no SQL/query language and no formula engine; provider/application code owns acquisition and authoritative business calculations.
