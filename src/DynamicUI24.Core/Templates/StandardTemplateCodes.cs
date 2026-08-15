namespace DynamicUI24.Core.Templates;

/// <summary>Well-known codes shipped by the standard template modules.</summary>
public static class StandardTemplateCodes
{
    public static TemplateCode Setup { get; } = new("SETUP");
    public static TemplateCode DataEntry { get; } = new("DATA_ENTRY");
    public static TemplateCode Report { get; } = new("REPORT");
    public static TemplateCode HistoryDocument { get; } = new("HISTORY_DOCUMENT");
    public static TemplateCode Dashboard { get; } = new("DASHBOARD");
    public static TemplateCode Signing { get; } = new("SIGNING");
}
