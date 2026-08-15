# Company Profile

`ICompanyProfileProvider` exposes read-only presentation data for a Company. `CompanyProfile` contains stable identity/contact fields and an immutable `AdditionalFields` dictionary for provider-supplied synchronized values.

The result explicitly distinguishes `Ready`, `NotFound`, and `Error`. Missing data is therefore safe and deterministic instead of becoming a null-reference failure.

This boundary deliberately has no editing, persistence, signing, Odoo SDK, database, or synchronization implementation. A consuming application may later adapt its API or synchronized TS24/Odoo read model to `ICompanyProfileProvider`; DynamicUI24 receives only the generic read model.
