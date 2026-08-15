# Template registry

`TemplateRegistry` is created by an application's composition root. Each selected module registers one `IDynamicTemplate`; the registry then resolves workspace `TemplateCode` values without a central switch or template-specific host logic.

```text
WorkspaceDefinition -> TemplateCode -> TemplateRegistry -> IDynamicTemplate -> WorkspaceDescriptor
```

Duplicate codes return `TemplateRegistrationError.DuplicateCode` and never replace the first registration. Unknown codes return `TemplateResolutionError.UnknownCode`. Both are normal, deterministic results suitable for presentation by a UI. Registered templates are enumerated in ordinal code order.

The registry has no static global instance. Registration and snapshot reads are synchronized; normal application composition registers modules before runtime reads.
