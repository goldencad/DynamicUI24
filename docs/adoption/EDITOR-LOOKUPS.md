# Adopting Editor Lookups

Implement `IEditorLookupProvider` with bounded server/provider-side search. Echo generation, company and context revision exactly. Return stable option IDs and policy-safe display strings. Never return UI controls or require full catalog loading.

Register the provider only for the relevant editor. Query on interaction, honor cancellation, support continuation, and keep business lookup semantics in the application. Treat provider messages and diagnostics as P1-sensitive surfaces.
