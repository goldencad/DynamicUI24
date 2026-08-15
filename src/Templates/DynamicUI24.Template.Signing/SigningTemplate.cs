using DynamicUI24.Core.Templates;

namespace DynamicUI24.Template.Signing;

public sealed class SigningTemplate : DynamicTemplateBase
{
    public override TemplateCode TemplateCode => StandardTemplateCodes.Signing;
    public override string ModuleName => typeof(SigningTemplate).Assembly.GetName().Name!;
    public override IReadOnlyCollection<TemplateCapability> SupportedCapabilities { get; } =
        [new("PREVIEW")];
}

public static class SigningTemplateRegistration
{
    public static TemplateRegistrationResult Register(TemplateRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.Register(new SigningTemplate());
    }
}
