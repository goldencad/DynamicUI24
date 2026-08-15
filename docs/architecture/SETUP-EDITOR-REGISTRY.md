# Setup editor registry

`SetupEditorRegistry` maps an open, technical `DefinitionType` to `ISetupDefinitionEditorProvider`. A provider returns a descriptor for a property form, custom editor, or unavailable state. This keeps future Columns/Variables, Tree, Ribbon, Dashboard, and Report designers out of a central switch.

Resolution is safe: an unknown type or missing provider returns a localized unavailable descriptor. Registration rejects duplicate type ownership. The generic property provider validates field identity and ordering at composition time.

The Core registry contains no Avalonia types. `SetupWorkspaceHost` renders resolved descriptors and shared presentation primitives; specialized modules can supply their own provider without creating dependencies between template modules.
