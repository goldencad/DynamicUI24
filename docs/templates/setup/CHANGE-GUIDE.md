# Setup change guide

To add a category, supply metadata and localized labels; do not add a host branch. To add a definition kind, implement `ISetupDefinitionEditorProvider` and register it at composition. Prefer `GenericPropertyEditorProvider` when a property form is sufficient.

Specialized designers may return `SetupEditorKind.Custom`, but their executable behavior stays outside metadata. Never place C#, SQL, scripts, persistence clients, or business publication rules in a field/category descriptor.

When changing lifecycle behavior, preserve source immutability, explicit validation, new clone identity, non-destructive retire, fail-closed permissions, and the dirty-navigation guard. Add focused tests for both successful and malformed metadata paths.
