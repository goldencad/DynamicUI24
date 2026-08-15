using DynamicUI24.Core.Templates;

namespace DynamicUI24.Template.DataEntry;

public sealed class DataEntryTemplate : DynamicTemplateBase
{
    public override TemplateCode TemplateCode => StandardTemplateCodes.DataEntry;
    public override string ModuleName => typeof(DataEntryTemplate).Assembly.GetName().Name!;
    public override IReadOnlyCollection<TemplateCapability> SupportedCapabilities { get; } =
        [new("IMPORT"), new("EXPORT")];
}

public static class DataEntryTemplateRegistration
{
    public static TemplateRegistrationResult Register(TemplateRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.Register(new DataEntryTemplate());
    }
}
