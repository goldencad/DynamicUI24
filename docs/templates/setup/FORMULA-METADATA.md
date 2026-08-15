# Formula Metadata

`FORMULA` stores a formula code, localized label, result `VariableCode`, declarative expression text, selected referenced variables, version/status and read-only state. The picker is populated from existing `VariableDefinition` values and shows technical codes with localized names.

Validation requires the result and every reference to exist, rejects self-reference, duplicates, unknown codes and obvious executable syntax categories. There is no preview or evaluation in Setup. See [Formula Definition Boundary](../../architecture/FORMULA-DEFINITION-BOUNDARY.md).
