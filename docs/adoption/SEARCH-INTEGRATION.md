# Search Integration

1. Compose local `ISearchProvider` implementations with `SearchCoordinator`.
2. Create `SearchResultPresenter` with the shared privacy resolver/presenter.
3. Create `SearchActivationService` with existing workspace navigation and registered command infrastructure.
4. Supply `SearchPaletteView` a current `SearchQuery` factory and assign it to `ShellHost.SearchContent`.
5. Call `SearchCoordinator.Invalidate()` on company/workspace/authorization context changes.

Keep the query factory snapshot-only. Never pass Avalonia controls into Core contracts. `WorkspaceSearch` is an integration seam; application search remains outside S1.
