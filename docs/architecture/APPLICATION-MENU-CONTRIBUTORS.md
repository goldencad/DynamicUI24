# Application Menu contributors

Applications extend the shell menu through `IApplicationMenuContributor`. Each contributor has a unique, stable code and returns immutable `ApplicationMenuItem` descriptors containing a localization key, semantic `IconKey`, display order, item type, target page code, and optional `PresentationRequirement`.

```csharp
var composer = new ApplicationMenuComposer();
composer.Register(new DemoPreferencesContributor());
var items = composer.Compose(companyScope.Snapshot.AuthorizationContext);
```

Registration rejects duplicate codes case-insensitively. Composition sorts by display order and code, catches contributor failures independently, and uses the existing `AuthorizationPresentationResolver` for HIDE, DISABLE, and READ_ONLY presentation. Missing privileged context fails closed. Metadata does not contain delegates, scripts, or arbitrary executable commands.

Standard shell items are not consumer contributors. The Demo's `DEMO_PREFERENCES` item exists only in the sample application and is not part of the framework standard.
