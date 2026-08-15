using DynamicUI24.Core.Templates;

namespace DynamicUI24.Template.Setup;

public sealed class SetupTemplate : DynamicTemplateBase
{
    public override TemplateCode TemplateCode => StandardTemplateCodes.Setup;
    public override string ModuleName => typeof(SetupTemplate).Assembly.GetName().Name!;
    public override IReadOnlyCollection<TemplateCapability> SupportedCapabilities { get; } =
        [new("SEARCH"), new("FILTER"), new("CREATE"), new("EDIT"), new("CLONE"),
         new("VALIDATE"), new("PUBLISH"), new("RETIRE")];
}

public static class SetupTemplateRegistration
{
    public static TemplateRegistrationResult Register(TemplateRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.Register(new SetupTemplate());
    }
}
