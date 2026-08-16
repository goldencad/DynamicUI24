using Avalonia.Controls;
using Avalonia.Layout;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Core.ApplicationMenu;
using DynamicUI24.Core.Privacy;

namespace DynamicUI24.Demo;

internal sealed class DemoPrivacyPanel : UserControl
{
    private readonly IPrivacyStateService state;
    private readonly IPrivacyPolicyResolver resolver = new PrivacyPolicyResolver();
    private readonly ISensitiveValuePresenter presenter = new SensitiveValuePresenter();
    private readonly TextBlock status = new();
    private readonly StackPanel values = new() { Spacing = 8 };
    private readonly SensitiveContentDefinition confidential = new(Sensitivity.Confidential, PrivacyPresentation.PartialMask,
        AllowTemporaryReveal: true, TemporaryRevealDuration: TimeSpan.FromSeconds(8), PartialMask: new(0, 4, "•••• "));
    private readonly SensitiveContentDefinition restricted = new(Sensitivity.Restricted, PrivacyPresentation.CaptureProtect,
        PrivacyPresentation.Mask);

    public DemoPrivacyPanel(IPrivacyStateService state)
    {
        this.state = state;
        var modes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var mode in Enum.GetValues<PrivacyMode>())
        {
            var button = new Button { Content = mode.ToString(), Tag = mode };
            button.Click += (_, _) => { state.SetRequestedMode((PrivacyMode)button.Tag!); Refresh(); };
            modes.Children.Add(button);
        }
        var reveal = new Button { Content = "Reveal CONTACT_REFERENCE temporarily" };
        reveal.Click += (_, _) => { state.BeginReveal(new("CONTACT_REFERENCE", RevealScope.Field, TimeSpan.FromSeconds(8), state.Generation)); Refresh(); };
        var revoke = new Button { Content = "Hide now" };
        revoke.Click += (_, _) => { state.RevokeReveal(); Refresh(); };
        state.StateChanged += (_, _) => Refresh();
        Content = new StackPanel { Spacing = 10, Children = { new TextBlock { Text = "Sensitive content & privacy" }, status, modes, reveal, revoke, values } };
        Refresh();
    }

    private void Refresh()
    {
        status.Text = PrivacyShellDefinitions.CompactState(state.RequestedMode, state.EffectiveMode) +
            (state.RequestedMode == PrivacyMode.Off ? " · Restricted content protected by policy" : string.Empty);
        values.Children.Clear();
        Add("PUBLIC_NOTE", "Visible neutral note", null);
        Add("CONTACT_REFERENCE", "CONTACT-12345678", confidential);
        Add("PRIVATE_REFERENCE", "PRIVATE-98765432", restricted);
    }

    private void Add(string field, string raw, SensitiveContentDefinition? metadata)
    {
        var resolution = resolver.Resolve(new(true, metadata, state.RequestedMode,
            IsTemporarilyRevealed: state.IsRevealed(field, state.Generation), CaptureCapability: CaptureProtectionCapability.Unsupported,
            Generation: state.Generation));
        var safe = presenter.Present(raw, metadata, resolution);
        values.Children.Add(new TextBlock { Text = $"{field}: {safe.DisplayValue} · accessible: {safe.AccessibleValue}" });
    }
}

internal sealed class PrivacyMenuContributor : IApplicationMenuContributor
{
    public string ContributorCode => "PRIVACY";
    public IEnumerable<ApplicationMenuItem> CreateItems() => [PrivacyShellDefinitions.SettingsMenuItem()];
}
