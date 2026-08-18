using DynamicUI24.Core.ActionBars;

namespace DynamicUI24.Demo;

/// <summary>Registers the Editor Demo's semantic actions into the application's shared command registry.</summary>
public static class DemoEditorActions
{
    public const string HyperlinkOpen = "DEMO.EDITOR.HYPERLINK.OPEN";
    public const string ButtonEditBrowse = "DEMO.EDITOR.BUTTONEDIT.BROWSE";

    public static void Register(ActionCommandRegistry commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        EnsureRegistered(commands.Register(HyperlinkOpen, (_, _) => Task.FromResult(
            ActionCommandResult.Success("Hyperlink action invoked"))), HyperlinkOpen);
        EnsureRegistered(commands.Register(ButtonEditBrowse, (_, _) => Task.FromResult(
            ActionCommandResult.Success("Browse action invoked"))), ButtonEditBrowse);
    }

    private static void EnsureRegistered(bool registered, string commandCode)
    {
        if (!registered) throw new InvalidOperationException($"Demo editor command '{commandCode}' is already registered.");
    }
}
