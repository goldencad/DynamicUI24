using DynamicUI24.Core.Templates;

namespace DynamicUI24.Template.Report;

public sealed class ReportTemplate : DynamicTemplateBase
{
    public override TemplateCode TemplateCode => StandardTemplateCodes.Report;
    public override string ModuleName => typeof(ReportTemplate).Assembly.GetName().Name!;
    public override IReadOnlyCollection<TemplateCapability> SupportedCapabilities { get; } =
        [new("FILTER"), new("EXPORT"), new("PREVIEW")];
}

public static class ReportTemplateRegistration
{
    public static TemplateRegistrationResult Register(TemplateRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.Register(new ReportTemplate());
    }
}
