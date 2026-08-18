# Adopting Editor Definitions

Create a stable `EditorCode` for reusable policy and a consumer-owned `EditorSemanticId` such as `SheetCode:VariableCode`, `ReportCode:ParameterCode`, or `FormCode:FieldCode`. Select a generic `EditorValueType`; add an explicit kind only for a compatible presentation choice.

Declare chrome, formatting, nullability, validation, privacy, permission, help and semantic embedded actions. Keep business services and persistence out of the definition. Resolve through the single shared resolver and retain `EditorRuntimeState` across rematerialization. Consumers decide when a valid editor commit becomes a form/grid save.
