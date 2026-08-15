# Application shell branding

Provide an `ApplicationBrand` with the consumer application's visible name and a semantic application-logo key. Register the logo path once in `IIconRegistry`; reusable shell/menu controls receive only the `IconKey` and never a filesystem asset path.

The Demo uses the neutral visible identities `DynamicUI24 Demo` and `Framework Demo`. Repository ownership is not product or vendor branding. The owner name may appear only where technically required in the repository URL `https://github.com/goldencad/DynamicUI24`.

About uses the application assembly version and generic runtime/platform information. Consumer-specific vendor, authentication, licensing, or business details must be supplied through explicit presentation contracts rather than hard-coded in the framework.
