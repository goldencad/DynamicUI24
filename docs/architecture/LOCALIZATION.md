# Runtime localization

Metadata and presentation code use `LocalizationKey`. `ILocalizationService.Get` resolves
that key for the current culture. `DictionaryLocalizationService` provides the initial
`vi-VN` and `en-US` catalogs and raises `CultureChanged` during a runtime switch.

Shell and workspace controls subscribe to that event and update visible text without
restarting or reselecting the workspace. Technical identifiers such as `TemplateCode` are
rendered directly and are not sent through localization.

Missing keys render as `[Key.Name]`, which is safe and visible during development. To add a
resource today, add the same key to both culture catalogs. A future resource-provider
implementation can replace the dictionary service without changing metadata contracts.
