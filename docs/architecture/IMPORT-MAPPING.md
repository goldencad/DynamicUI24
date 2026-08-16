# Import mapping

## Mapping contract and VariableCode rule

`ImportFieldMapping` maps a source name/path/index to one authoritative target `VariableCode`. Spreadsheet letters are not universal identities. FORMULA/SYSTEM, hidden, unauthorized or non-editable columns cannot be imported.

Auto-map is deterministic: exact `VariableCode`, normalized source name/`ColumnCode`, then explicit aliases. An ambiguous result remains unmapped and emits a warning. Duplicate target mappings are invalid.

The value pipeline is raw value → trim/null policy/default → registered converter → target data type conversion → column metadata validation → candidate value. Built-ins are text-to-number/date/boolean, trim, upper- and lowercase. Applications may register generic-safe converters; arbitrary scripts are forbidden. Converter failures retain only a bounded raw preview and exception category.
