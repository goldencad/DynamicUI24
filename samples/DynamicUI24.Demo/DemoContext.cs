using DynamicUI24.Core.Context;
using DynamicUI24.Core.Privacy;

namespace DynamicUI24.Demo;

internal sealed class DemoContextProvider : IContextPanelProvider
{
    public string ProviderCode => "DEMO.CONTEXT";
    public async ValueTask<ContextPanelResult> GetContextAsync(ContextPanelRequest request)
    {
        if (request.Selection.RowKey is null) return ContextPanelResult.Empty(ProviderCode, request.SemanticKey, request.Generation);
        await Task.Delay(request.Selection.RowKey.EndsWith("000000", StringComparison.Ordinal) ? 60 : 10, request.CancellationToken);
        var suffix = request.Selection.RowKey.Split(':')[^1];
        return new(ProviderCode, request.SemanticKey,
        [new("DETAILS", "Context.Details",
          [new("PUBLIC_NOTE", "Context.PublicNote", $"Selected record {suffix}"),
           new("CONTACT_REFERENCE", "Context.ContactReference", $"CONTACT-{suffix}", SensitiveContent: new(Sensitivity.Confidential, PrivacyPresentation.Mask)),
           new("PRIVATE_REFERENCE", "Context.PrivateReference", $"PRIVATE-{suffix}", SensitiveContent: new(Sensitivity.Restricted, PrivacyPresentation.Hide))],
          new("DATAENTRY.ROW"))], ContextLoadingState.Ready, request.Generation);
    }
}

internal sealed class DemoHelpProvider : IContextualHelpProvider
{
    public string ProviderCode => "DEMO.LOCAL_HELP";
    public ValueTask<ContextualHelpResult?> GetHelpAsync(ContextualHelpRequest request)
    {
        var content = request.HelpContextCode.Value switch
        {
            "DATAENTRY.GRID" => "Select a record to inspect its safe contextual details.",
            "DATAENTRY.ROW" => "Details follow the stable RowKey and never retain a visual row.",
            "DATAENTRY.FIELD.PRIVATE_REFERENCE" => "This value is protected by the shared privacy policy.",
            _ => null,
        };
        return ValueTask.FromResult(content is null ? null : new ContextualHelpResult(request.HelpContextCode,
            "Contextual help", content, [], [], ProviderCode, request.Generation));
    }
}
