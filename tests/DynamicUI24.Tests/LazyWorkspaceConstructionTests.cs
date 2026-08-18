using Avalonia.Controls;
using DynamicUI24.Avalonia;
using DynamicUI24.Avalonia.Presentation;
using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Shared.Presentation;
using Xunit;

namespace DynamicUI24.Tests;

[Collection("Avalonia native UI")]
public sealed class LazyWorkspaceConstructionTests
{
    [Fact]
    public void InactiveWorkspaceIsNotConstructedAndFirstActivationIsReused()
    {
        var localization = new DictionaryLocalizationService("en-US");
        var registry = new TemplateRegistry();
        Assert.True(registry.Register(new LazyTestTemplate("SETUP_TEST")).IsSuccess);
        Assert.True(registry.Register(new LazyTestTemplate("DATAENTRY_TEST")).IsSuccess);
        var host = new DynamicWorkspaceHost(registry, localization);
        var constructionCount = 0;
        var lazyDataEntry = new Lazy<Control>(() =>
        {
            constructionCount++;
            return new StatefulTestView();
        }, LazyThreadSafetyMode.ExecutionAndPublication);
        host.RegisterViewFactory(new("SETUP_TEST"), _ => new StatefulTestView());
        host.RegisterViewFactory(new("DATAENTRY_TEST"), _ => lazyDataEntry.Value);
        var setup = new WorkspaceDefinition("setup", "Setup", new("SETUP_TEST"));
        var dataEntry = new WorkspaceDefinition("data-entry", "DataEntry", new("DATAENTRY_TEST"));

        host.ShowWorkspace(setup);
        Assert.False(lazyDataEntry.IsValueCreated);
        Assert.Equal(0, constructionCount);

        host.ShowWorkspace(dataEntry);
        var first = Assert.IsType<StatefulTestView>(host.Content);
        first.State = "real-grid-state";
        Assert.True(lazyDataEntry.IsValueCreated);
        Assert.Equal(1, constructionCount);

        host.ShowWorkspace(setup);
        host.ShowWorkspace(dataEntry);
        Assert.Same(first, host.Content);
        Assert.Equal("real-grid-state", first.State);
        Assert.Equal(1, constructionCount);
    }

    private sealed class LazyTestTemplate(string code) : DynamicTemplateBase
    {
        private readonly TemplateCode code = new(code);
        public override TemplateCode TemplateCode => code;
        public override string ModuleName => "TEST";
        public override IReadOnlyCollection<TemplateCapability> SupportedCapabilities => [];
    }

    private sealed class StatefulTestView : Control, IRuntimeLocalizationAware
    {
        public string? State { get; set; }
        public void RefreshLocalization(System.Globalization.CultureInfo culture) { }
    }
}
