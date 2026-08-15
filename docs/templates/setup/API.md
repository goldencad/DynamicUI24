# Setup API

Applications provide `ISetupDefinitionProvider` for category queries and Save Draft, Publish, and Retire transitions. They provide `ISetupDefinitionValidator` for diagnostics and register `ISetupDefinitionEditorProvider` instances in `SetupEditorRegistry`.

The framework never assumes a database or publication backend. Providers decide storage and authoritative scope. The Demo implementation is deterministic and in-memory.

`SetupDefinitionLifecycle` creates and clones drafts, owns `SetupEditBuffer`, validates candidates, blocks invalid publication, and retires without deleting identity. Published, retired, or system/read-only definitions cannot be saved directly; cloning creates a new identity and next draft version.

Missing editor registrations resolve to `SetupEditorKind.Unavailable` and a localized message. New editor types are added by registry registration; the generic host has no definition-type switch.
