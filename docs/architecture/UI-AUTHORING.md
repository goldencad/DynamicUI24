# UI Authoring

Task 10H owns vendor-neutral semantic UI-definition contracts, protected draft editing, validation, preview, publish/version activation, rollback seams, safe audit hooks, and presentation integration. It does not own identity truth, backend authorization, business workflow or persistence technology.

The authoritative identities are `UiDefinitionCode`, `UiDefinitionVersion`, `UiElementCode`, and the existing workspace/command/form/field/grid/report/pane/help codes. Labels, translated text, visual order and controls are never identity. `UiDefinition`, `UiDefinitionDraft`, `UiAuthoringRuntimeState`, authorization results, preferences and controls remain distinct.

Authoring follows Draft → Validate → Preview → Publish. Preview uses the draft without activating it. Publish validates and atomically asks `IUiDefinitionRepository` to store and activate one immutable version. Rollback activates a prior version and retains later history. Storage adapters own transactions; metadata must contain no scripts, SQL, secrets or business snapshots.

Authoring presentation should use a semantic tree, bounded search, live runtime preview and grouped inspector. Native inputs and the Universal Editor preserve Unicode/IME. Preview is lazy and materializes only the selected target.

Focused tests: `/usr/local/share/dotnet/dotnet test tests/DynamicUI24.Tests/DynamicUI24.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~UiAuthoringFoundationTests`.

Common failures are mutating published metadata, activating a preview, using labels as keys, leaking protected values in diagnostics, or eagerly constructing inactive workspaces.
