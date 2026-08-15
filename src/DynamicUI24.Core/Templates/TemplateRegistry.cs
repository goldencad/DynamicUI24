namespace DynamicUI24.Core.Templates;

public enum TemplateRegistrationError
{
    None,
    InvalidTemplate,
    DuplicateCode,
}

public sealed record TemplateRegistrationResult(
    bool IsSuccess,
    TemplateRegistrationError Error,
    string? Message)
{
    public static TemplateRegistrationResult Success { get; } = new(true, TemplateRegistrationError.None, null);
}

public enum TemplateResolutionError
{
    None,
    UnknownCode,
}

public sealed record TemplateResolutionResult(
    bool IsSuccess,
    IDynamicTemplate? Template,
    TemplateResolutionError Error,
    string? Message);

/// <summary>A deterministic registry for independently composed template modules.</summary>
public sealed class TemplateRegistry
{
    private readonly object syncRoot = new();
    private readonly Dictionary<TemplateCode, IDynamicTemplate> templates = [];

    public TemplateRegistrationResult Register(IDynamicTemplate? template)
    {
        if (template is null || template.TemplateCode is null ||
            string.IsNullOrWhiteSpace(template.ModuleName) || template.SupportedCapabilities is null)
        {
            return new(false, TemplateRegistrationError.InvalidTemplate, "Template metadata is incomplete.");
        }

        lock (syncRoot)
        {
            if (!templates.TryAdd(template.TemplateCode, template))
            {
                return new(
                    false,
                    TemplateRegistrationError.DuplicateCode,
                    $"Template code '{template.TemplateCode}' is already registered.");
            }
        }

        return TemplateRegistrationResult.Success;
    }

    public TemplateResolutionResult Resolve(TemplateCode templateCode)
    {
        ArgumentNullException.ThrowIfNull(templateCode);

        lock (syncRoot)
        {
            return templates.TryGetValue(templateCode, out var template)
                ? new(true, template, TemplateResolutionError.None, null)
                : new(false, null, TemplateResolutionError.UnknownCode,
                    $"No template is registered for code '{templateCode}'.");
        }
    }

    public IReadOnlyList<TemplateDescriptor> GetRegisteredTemplates()
    {
        lock (syncRoot)
        {
            return templates.Values
                .OrderBy(template => template.TemplateCode.Value, StringComparer.Ordinal)
                .Select(template => new TemplateDescriptor(
                    template.TemplateCode,
                    template.ModuleName,
                    template.TemplateVersion,
                    template.SupportedCapabilities.ToArray()))
                .ToArray();
        }
    }
}
