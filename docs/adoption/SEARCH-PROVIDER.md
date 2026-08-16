# Adding a Search Provider

Use a stable uppercase provider code and declare only the kinds the provider can return. Implement async cancellation, return semantic IDs/targets, attach permission/company/privacy metadata, and choose explicit `CanFavorite`, `CanPin`, and `CanRecordRecent` eligibility. Use a `DeduplicationKey` only when two results truly target the same semantic destination.

Do not return raw sensitive labels that cannot be safely presented, trust presentation filtering as backend authorization, expose exception details, or execute a result inside the provider. Test empty/exact/prefix/contains queries, cancellation, oversized output, duplicates, failure, permission loss, and company changes.
