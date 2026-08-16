# Quick Access

## Quick Access identity

`QuickAccessEntry` stores semantic entry ID and target code plus provider/company/workspace scope. It never persists localized or sensitive labels. `IQuickAccessResolver` resolves current safe metadata at render time, so permission, privacy, retirement, localization, and company changes remain authoritative.

## Favorite vs Pin

Favorite means “important to me.” Pinned means persistent high visibility. They are independent sets: an entry can be either, both, or neither. Pinned entries have explicit deterministic order and support pin, unpin, and move.

## Recent rule

Recent is bounded, newest-first, deduplicated convenience history—not an audit record. Only successful activation with `CanRecordRecent` is recorded. Failed navigation and ineligible commands are ignored.

## Preference boundary

`IQuickAccessStore` is a user-preference seam. S1 provides an in-memory store for Demo; applications can provide persistence without mutating published metadata. Company-scoped entries resolve only in their company. Retired or unauthorized targets return unavailable/hidden safely.
