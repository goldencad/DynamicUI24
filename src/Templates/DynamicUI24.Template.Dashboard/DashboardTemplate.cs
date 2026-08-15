using DynamicUI24.Core.Templates;

namespace DynamicUI24.Template.Dashboard;

public sealed class DashboardTemplate : DynamicTemplateBase
{
    public override TemplateCode TemplateCode => StandardTemplateCodes.Dashboard;
    public override string ModuleName => typeof(DashboardTemplate).Assembly.GetName().Name!;
    public override IReadOnlyCollection<TemplateCapability> SupportedCapabilities { get; } =
        [new("FILTER")];
}

public static class DashboardTemplateRegistration
{
    public static TemplateRegistrationResult Register(TemplateRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.Register(new DashboardTemplate());
    }
}
