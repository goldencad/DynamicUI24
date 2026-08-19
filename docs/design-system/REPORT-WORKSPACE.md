# Report Workspace Design

Main result content remains dominant. A compact title/subtitle precedes a collapsible parameter area. Primary actions are Run/Refresh, Parameters, and Export; filters, layout reset, and output live in progressive disclosure. Active filter/group state must remain apparent at narrow widths.

The result reuses permanent row headers, semantic column headers, percentage sizing, horizontal scrolling, selection, copy, Find, localization, density, theme, scale, and accessibility patterns from DataEntry Grid. It removes editing and row lifecycle commands. Status exposes safe state, bounded row counts, generation time, and exporting progress without synchronous full counts.

The workspace is lazily composed as primary result content plus a collapsible parameter/filter pane, optional secondary context, and the existing operation/status surface. Runtime states use `ContentPresentationState`; authorization uses Hidden/Disabled/ReadOnly/Enabled; P1 remains authoritative. Commands, Notification, Context Panel, Search, and Quick Access are reused.

Parameters use the Universal Editor `AvaloniaEditorPresenter`. Native editor controls retain focus ownership for IME, caret, selection, clipboard shortcuts, Escape, and commit behavior. No workspace-level key handler may consume normal composed input or rebuild an active editor. The UI contains no SQL/query language or formula engine; provider/application owns data and business truth.
