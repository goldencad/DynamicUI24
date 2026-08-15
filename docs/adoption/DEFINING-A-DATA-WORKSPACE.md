# Defining a Data Workspace

1. Register the desired template in `TemplateRegistry` during composition.
2. Create variables with stable `VariableCode` values and the narrowest generic scope.
3. Create ordered columns that reference those codes; select `INPUT`, `FORMULA` or `SYSTEM` and valid default geometry.
4. If needed, create declarative formula metadata by selecting existing result/reference variables.
5. Create a workspace and select its template from the registry-driven choice list.
6. Validate, resolve diagnostics and publish through the Setup lifecycle.

Published geometry is a default definition; keep per-user width preferences separate. Published definitions and VariableCodes are not silently mutated. Company scope and permission requirements are resolved fail-closed. No DataEntry grid or formula runtime is created by this workflow.
