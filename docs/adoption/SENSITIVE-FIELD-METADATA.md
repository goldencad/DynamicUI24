# Sensitive Field Metadata

Neutral examples:

```csharp
var publicNote = new SensitiveContentDefinition();

var contactReference = new SensitiveContentDefinition(
    Sensitivity.Confidential,
    PrivacyPresentation.PartialMask,
    AllowTemporaryReveal: true,
    TemporaryRevealDuration: TimeSpan.FromSeconds(8),
    AllowCopyWhenRevealed: false,
    PartialMask: new(PreserveSuffix: 4, MaskBody: "•••• "));

var privateReference = new SensitiveContentDefinition(
    Sensitivity.Restricted,
    PrivacyPresentation.CaptureProtect,
    CaptureProtectionFallback: PrivacyPresentation.Mask);
```

Durations must be positive and policy-configurable. `CAPTURE_PROTECT` must specify a safe masking/hiding fallback. Partial masks are generic; application semantics such as account, identity, payroll, or legal classification do not belong in the framework. Omitted metadata remains normal unless an application policy classifies it more strictly.
