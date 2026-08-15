# Column Designer

`COLUMN` metadata covers stable `VariableCode`, localized labels, data type, editor kind, order, visibility, requirement, permissions, formatting, defaults, declarative validation and an optional formula definition reference.

Modes are:

- `INPUT`: runtime editing is allowed subject to permission.
- `FORMULA`: runtime read-only; a future calculation engine may supply the value.
- `SYSTEM`: runtime read-only; the host system supplies the value.

`Width`, `MinWidth` and `MaxWidth` are published defaults and must be positive and consistent. Future user preferences may override presentation only; they never mutate the published column definition.
