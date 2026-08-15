# Company Context

`DynamicUI24.Core.Companies` defines a consumer-neutral Company identity and runtime selection boundary. `CompanyId` is an immutable value, while `CompanyDescriptor` carries only selection metadata: code, display name, optional tax code, and active status.

`ICompanyContextProvider` exposes the current Company, the available list, asynchronous switching, and one deterministic `CompanyChanged` notification after a successful change. Unknown and inactive targets produce explicit rejection results. Re-selecting the current Company succeeds without publishing a duplicate change.

## Runtime flow

`CompanyScopeCoordinator` owns the framework flow:

1. switch the `ICompanyContextProvider`;
2. publish `Loading` for the new Company;
3. refresh the Company profile and effective authorization context concurrently;
4. discard old Company-scoped results;
5. publish `Ready`, `Unavailable`, or `Error` to presentation consumers.

Every refresh receives a monotonically increasing version and a linked cancellation token. A result is published only when both its version and Company identity still match the latest request. Therefore a slow B response cannot overwrite C after a rapid A → B → C sequence.

The framework owns these contracts and sequencing rules. The consuming application owns discovery of Companies, persisted selection, identity/session management, API calls, and reload of its Company-scoped business data. No static global Company context is used.

Theme, language, and workspace are independent shell state and are not reset by Company switching.
