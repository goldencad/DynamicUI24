# Editor Lookup

`IEditorLookupProvider` accepts semantic editor/consumer identity, search, filters, offset/continuation, bounded window, company/context revision, generation and cancellation. It returns semantic option IDs plus safe display text—never controls.

`EditorLookupCoordinator` caps every window at 200, checks result generation, company and context, and isolates provider exceptions behind safe states. Cancellation improves efficiency; validation of returned generation/context provides correctness. P1-safe labels must be produced by policy/provider and remain safe in suggestions, secondary text, accessibility and diagnostics.

The demo provider represents 100,000 logical records but materializes only the requested window. It does not build a 100,000-item ComboBox.
