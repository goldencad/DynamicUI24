using DynamicUI24.Core.Templates;

namespace DynamicUI24.Template.HistoryDocument;

public sealed class HistoryDocumentTemplate : DynamicTemplateBase
{
    public override TemplateCode TemplateCode => StandardTemplateCodes.HistoryDocument;
    public override string ModuleName => typeof(HistoryDocumentTemplate).Assembly.GetName().Name!;
    public override IReadOnlyCollection<TemplateCapability> SupportedCapabilities { get; } =
        [new("SEARCH"), new("PREVIEW")];
}

public static class HistoryDocumentTemplateRegistration
{
    public static TemplateRegistrationResult Register(TemplateRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.Register(new HistoryDocumentTemplate());
    }
}
