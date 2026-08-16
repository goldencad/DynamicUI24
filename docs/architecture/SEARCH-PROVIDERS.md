# Search Providers

## Provider contract

`ISearchProvider` declares a stable provider code, supported typed result kinds, and an async `SearchAsync(SearchQuery, CancellationToken)` method. Providers return immutable semantic candidates and remain UI-framework-free. Backend authorization remains authoritative.

## Query contract

`SearchQuery` carries text, scope, company/workspace/template/navigation context, culture, permission/privacy context, and generation. Providers must treat context as a snapshot and honor cancellation where practical.

## Result contract

`SearchResult` carries stable identity, kind, provider, label/key, icon, rank, navigation/command targets, authorization/company/privacy metadata, semantic deduplication key, actionability, and Quick Access eligibility. Empty labels and invalid enum values are rejected by the coordinator. Unknown icons use the existing safe icon fallback.

## Merge and isolation

Provider calls run concurrently. Each failure is isolated and reported only as a safe provider code; other results remain usable. Deduplication uses explicit semantic identity, never matching display text. Deterministic limits protect the Shell from oversized providers.

## Extension points

Applications may add record/document/report providers or a workspace-search provider without changing Shell UI. Do not add a database/indexing dependency to Core. A provider must not use reflection, scripting, assembly loading, arbitrary URLs, or Demo types.
