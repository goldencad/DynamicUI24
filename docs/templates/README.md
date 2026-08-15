# Template modules

Setup, DataEntry, Report, HistoryDocument, Dashboard, and Signing are independently registered modules. In Task 1 each owns a minimal `IDynamicTemplate` implementation and registration entry point; no module references another template.

The modules begin with identity, version, capability declarations, validation, and generic descriptors. The Setup module now resolves to the Task 8 metadata-driven Setup foundation; the other specialized template UIs remain later work.

## Add a new template

Create a separate module or consumer-side class that implements `IDynamicTemplate` (or derives from `DynamicTemplateBase`), give it an open code, then register it at the consumer composition root:

```csharp
public sealed class CalendarTemplate : DynamicTemplateBase
{
    public override TemplateCode TemplateCode { get; } = new("CALENDAR");
    public override string ModuleName => "MyCompany.Template.Calendar";
}

registry.Register(new CalendarTemplate());
```

No existing standard template or host branch changes. The Demo uses this exact pattern for its sample-only `CALENDAR` proof. See [Template contract](../architecture/TEMPLATE-CONTRACT.md) and [Template registry](../architecture/TEMPLATE-REGISTRY.md).

For repeatable macOS GUI verification, the published Demo accepts `--smoke`; it selects every sample through the same ComboBox event path, reports each resolution, displays the safe unknown-code state, and exits cleanly.
