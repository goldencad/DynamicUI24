# Adoption

- [Application branding](BRANDING.md)
- [Company context integration](COMPANY-CONTEXT-INTEGRATION.md)
- [Permission and capability mapping](PERMISSION-CAPABILITY-MAPPING.md)
- [Ribbon integration](RIBBON-INTEGRATION.md)
- [Ribbon metadata example](RIBBON-METADATA-EXAMPLE.md)
- [Platform compatibility](PLATFORM-COMPATIBILITY.md)

Consumer applications reference only the DynamicUI24 modules they need and supply their own business behavior, data access, permissions, and branding.

The sample application composes the six standard modules plus a consumer-owned `CALENDAR` template, then resolves workspace metadata through the generic Avalonia host. PayCalc24 is a future consumer/reference implementation and is not part of the framework dependency graph.
