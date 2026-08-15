# Setup definition lifecycle

```text
source definition -> candidate edit buffer -> validate -> save draft / publish
                                              |
                                              +-> diagnostics (info/warning/error)
published definition -> clone -> new identity + next draft version
published definition -> retire -> retained identity with Retired state
```

Edits update only the candidate's immutable value map. Cancel restores the source exactly. Navigation is blocked while the candidate is dirty, so category or definition changes cannot silently discard work.

Publish always validates first and rejects candidates with error diagnostics. The provider boundary returns the authoritative published result. Retire is a provider transition, not deletion. Effective dates are optional metadata; the foundation validates/presents them but does not implement an effective-date resolver.
