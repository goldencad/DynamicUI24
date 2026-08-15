# Application branding

Create an `ApplicationBrand` during app composition. Framework defaults work without any
consumer configuration:

```csharp
var brand = ApplicationBrand.Default;
```

To override name, semantic logo, and accent:

```csharp
var logoKey = new IconKey("MY_APP_LOGO");
icons.Register(new IconDefinition(logoKey, "M2,2 L22,12 L2,22 Z"));
var brand = new ApplicationBrand("My Application", logoKey, "#7C3AED");
```

Define `DuiAccentBrush` after the merged framework token dictionary in application
resources. Pass the brand to `ShellPresentation`. Do not place consumer assets, names, or
colors in framework or template projects. A missing logo key renders the standard safe
fallback.
