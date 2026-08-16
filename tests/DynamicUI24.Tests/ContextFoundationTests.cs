using System.Collections.Immutable;
using System.Globalization;
using DynamicUI24.Core.Authorization;
using DynamicUI24.Core.Companies;
using DynamicUI24.Core.Context;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Core.Navigation;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class ContextFoundationTests
{
    private static readonly ContextPanelDefinition Definition = new("CONTEXT",
        [new("DETAILS", "Details"), new("HELP", "Help", 10)], defaultOpen: true,
        defaultWidth: 320, minWidth: 240, maxWidth: 560);

    [Fact] public void DefinitionSeparatesBoundedRuntimeState()
    {
        var state = new ContextPanelState(Definition);
        Assert.True(state.IsOpen); state.Close(); Assert.False(state.IsOpen); state.Toggle(); Assert.True(state.IsOpen);
        Assert.Equal(240, state.Resize(1)); Assert.Equal(560, state.Resize(900));
        Assert.True(state.SelectSection("help")); Assert.Equal("HELP", state.SelectedSection);
        Assert.Equal(320, Definition.DefaultWidth);
    }

    [Fact] public void InvalidWidthsAndDuplicateSectionsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContextPanelDefinition("X", [], minWidth: 500, maxWidth: 100));
        Assert.Throws<ArgumentException>(() => new ContextPanelDefinition("X", [new("A", "A"), new("a", "A2")]));
    }

    [Fact] public async Task LateProviderResultCannotOverwriteLatestSelection()
    {
        using var coordinator = new ContextPanelCoordinator([new DelayedProvider()]);
        var first = coordinator.ResolveAsync("TEST", (g, c) => Request("A", g, c));
        await Task.Delay(5);
        var second = coordinator.ResolveAsync("TEST", (g, c) => Request("B", g, c));
        await Task.WhenAll(first, second);
        Assert.Equal("B", coordinator.Current!.ContextKey);
        Assert.Equal("B", coordinator.Current.Sections[0].Items[0].Value);
    }

    [Fact] public async Task UnknownAndThrowingProviderFailSafely()
    {
        using var unknown = new ContextPanelCoordinator([]);
        Assert.Equal("CONTEXT_PROVIDER_UNKNOWN", (await unknown.ResolveAsync("NONE", (g, c) => Request("X", g, c))).DiagnosticCode);
        using var throwing = new ContextPanelCoordinator([new ThrowingProvider()]);
        Assert.Equal("CONTEXT_PROVIDER_FAILED", (await throwing.ResolveAsync("THROW", (g, c) => Request("X", g, c))).DiagnosticCode);
    }

    [Fact] public void BreadcrumbKeepsCurrentAndCollapsesMiddle()
    {
        var path = new BreadcrumbPath([new("ROOT", "Root"), new("A", "A"), new("B", "B"), new("C", "C", IsCurrent: true)]);
        var layout = BreadcrumbOverflowResolver.Resolve(path, 3);
        Assert.True(layout.HasOverflow); Assert.Equal("ROOT", layout.Visible[0].ItemCode);
        Assert.True(layout.Visible[^1].IsCurrent); Assert.Single(layout.Overflow);
    }

    [Fact] public void HelpUsesMostSpecificSemanticCode()
    {
        Assert.Equal("FIELD", HelpContextResolver.Resolve(new("FIELD"), new("SECTION"), new("WORKSPACE"), new("TEMPLATE"))!.Value.Value);
        Assert.Equal("WORKSPACE", HelpContextResolver.Resolve(null, null, new("WORKSPACE"), new("TEMPLATE"))!.Value.Value);
    }

    [Fact] public void SharedShellLayoutBoundsBothSidesAndProtectsWorkspace()
    {
        var layout = new DynamicUI24.Shared.Presentation.ShellSplitLayoutState();
        Assert.Equal(240, layout.BoundContextWidth(900, 900));
        Assert.Equal(420, layout.MinimumWorkspaceWidth);
    }

    private static ContextPanelRequest Request(string key, long generation, CancellationToken token) => new(
        new CompanyId("COMPANY"), "WORKSPACE", "TEMPLATE", "WORKSPACE", new(RowKey: key), null,
        CultureInfo.InvariantCulture, PrivacyMode.On, null, generation, token);

    private sealed class DelayedProvider : IContextPanelProvider
    {
        public string ProviderCode => "TEST";
        public async ValueTask<ContextPanelResult> GetContextAsync(ContextPanelRequest request)
        {
            await Task.Delay(request.Selection.RowKey == "A" ? 80 : 5); // intentionally ignores cancellation
            return new(ProviderCode, request.Selection.RowKey!,
                [new("DETAILS", "Details", [new("VALUE", "Value", request.Selection.RowKey)])],
                ContextLoadingState.Ready, request.Generation);
        }
    }
    private sealed class ThrowingProvider : IContextPanelProvider
    { public string ProviderCode => "THROW"; public ValueTask<ContextPanelResult> GetContextAsync(ContextPanelRequest request) => throw new InvalidOperationException("raw"); }
}
