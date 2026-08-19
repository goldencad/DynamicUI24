# Task 10F backlog

The foundation proves DataEntry sheet composition only. Report/document/dashboard sheet renderers, durable user-created-sheet storage, richer tab drag/drop visuals and application-specific clone progress remain application or future-task work. Formula parsing/evaluation, business-period semantics, signing, collaboration and workbook features are explicitly out of scope.

## Pre-existing DataEntry activation performance

Real-Mac triage during Task 11 confirmed baseline variance rather than a Task 11 regression. DataEntry currently performs three provider requests before `Ready`; in the synthetic Demo provider, each request performs the existing 100,000-logical-row scan. The provider/Ready portion dominates activation at approximately 637–645 ms median. Clean baseline Navigation→Ready measured 720.552 ms median versus 714.256 ms with the Task 11 worktree, with identical construction and request counts and zero Report heavy-object construction during DataEntry activation.

Future work should investigate whether the initial requests can be safely coalesced or deduplicated, including preferred-sheet materialization, authorization/context-driven rematerialization and `UpdateContextAsync` loading. It should also assess whether the Demo provider must rescan all 100,000 logical rows per initial request and measure first-visible-frame/compositor timing on real macOS. Any change must preserve authorization and privacy correctness, stale-generation safety, bounded materialization and lazy workspace construction.
