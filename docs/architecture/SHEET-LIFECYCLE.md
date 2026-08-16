# Sheet lifecycle

Create, Duplicate, Save As, Rename, Reorder, Hide, Show and Delete eligibility is host metadata plus provider policy. Rename changes localization keys, never `SheetCode`; reorder and hide/show are presentation changes. Hiding or deleting the active sheet selects the first eligible sheet deterministically.

Physical creation/deletion belongs to `ISheetLifecycleProvider`. Delete first calls the authoritative calculation compatibility service and may block or require confirmation. Provider failure never publishes a half-created sheet. Applications may disable Delete and expose Hide only.
