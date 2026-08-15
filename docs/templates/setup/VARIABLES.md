# Variable Definitions

`VARIABLE` establishes the reusable namespace used by columns and formulas. Metadata includes `VariableCode`, localized label/description, generic data type, version/status, system state, permission and one of these scopes:

- `ROW`: one data row.
- `WORKSPACE`: one workspace instance.
- `DOCUMENT`: one document.
- `COMPANY`: the active company.
- `APPLICATION`: the application boundary.

Scopes identify lifetime/visibility only and carry no business semantics. See [VariableCode](../../architecture/VARIABLE-CODE.md).
