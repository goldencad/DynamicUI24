using DynamicUI24.Core.Templates;
using DynamicUI24.Core.Workspaces;
using DynamicUI24.Template.Dashboard;
using DynamicUI24.Template.DataEntry;
using DynamicUI24.Template.HistoryDocument;
using DynamicUI24.Template.Report;
using DynamicUI24.Template.Setup;
using DynamicUI24.Template.Signing;
using Xunit;

namespace DynamicUI24.Tests;

public sealed class TemplateCodeTests
{
    [Fact]
    public void StandardCodeIsNormalized() =>
        Assert.Equal(StandardTemplateCodes.DataEntry, new TemplateCode(" data_entry "));

    [Fact]
    public void CustomCodeIsSupported() =>
        Assert.Equal("CALENDAR", new TemplateCode("calendar").Value);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NOT VALID")]
    public void InvalidCodeIsRejected(string value) =>
        Assert.Throws<ArgumentException>(() => new TemplateCode(value));

    [Fact]
    public void EqualityUsesNormalizedValue() =>
        Assert.Equal(new TemplateCode("report"), new TemplateCode("REPORT"));
}

public sealed class TemplateRegistryTests
{
    [Fact]
    public void RegistersAndResolvesOneTemplate()
    {
        var registry = new TemplateRegistry();

        var registration = registry.Register(new TestTemplate(StandardTemplateCodes.Setup));
        var resolution = registry.Resolve(StandardTemplateCodes.Setup);

        Assert.True(registration.IsSuccess);
        Assert.True(resolution.IsSuccess);
        Assert.Equal(StandardTemplateCodes.Setup, resolution.Template!.TemplateCode);
    }

    [Fact]
    public void SixStandardModulesRegisterIndependently()
    {
        var registry = RegisterStandardTemplates();

        Assert.Equal(6, registry.GetRegisteredTemplates().Count);
    }

    [Fact]
    public void CustomCalendarUsesTheSameRegistrationPath()
    {
        var registry = RegisterStandardTemplates();
        var calendar = new TestTemplate(new TemplateCode("CALENDAR"));

        var registration = registry.Register(calendar);
        var resolution = registry.Resolve(new TemplateCode("calendar"));

        Assert.True(registration.IsSuccess);
        Assert.Same(calendar, resolution.Template);
    }

    [Fact]
    public void DuplicateCodeIsRejectedWithoutReplacingOriginal()
    {
        var registry = new TemplateRegistry();
        var original = new TestTemplate(StandardTemplateCodes.Report);
        registry.Register(original);

        var duplicate = registry.Register(new TestTemplate(StandardTemplateCodes.Report));

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(TemplateRegistrationError.DuplicateCode, duplicate.Error);
        Assert.Same(original, registry.Resolve(StandardTemplateCodes.Report).Template);
    }

    [Fact]
    public void UnknownCodeReturnsStableFailure()
    {
        var result = new TemplateRegistry().Resolve(new TemplateCode("UNKNOWN"));

        Assert.False(result.IsSuccess);
        Assert.Equal(TemplateResolutionError.UnknownCode, result.Error);
        Assert.Null(result.Template);
    }

    [Fact]
    public void EnumerationIsSortedAndExposesMetadata()
    {
        var registry = new TemplateRegistry();
        registry.Register(new TestTemplate(new TemplateCode("ZETA")));
        registry.Register(new TestTemplate(new TemplateCode("ALPHA")));

        var templates = registry.GetRegisteredTemplates();

        Assert.Equal(["ALPHA", "ZETA"], templates.Select(template => template.TemplateCode.Value));
        Assert.All(templates, template => Assert.Equal(new TemplateVersion(0, 1), template.TemplateVersion));
        Assert.All(templates, template => Assert.Contains(new TemplateCapability("SEARCH"), template.SupportedCapabilities));
    }

    [Fact]
    public void InvalidTemplateRegistrationReturnsFailure()
    {
        var result = new TemplateRegistry().Register(null);

        Assert.False(result.IsSuccess);
        Assert.Equal(TemplateRegistrationError.InvalidTemplate, result.Error);
    }

    internal static TemplateRegistry RegisterStandardTemplates()
    {
        var registry = new TemplateRegistry();
        Assert.True(SetupTemplateRegistration.Register(registry).IsSuccess);
        Assert.True(DataEntryTemplateRegistration.Register(registry).IsSuccess);
        Assert.True(ReportTemplateRegistration.Register(registry).IsSuccess);
        Assert.True(HistoryDocumentTemplateRegistration.Register(registry).IsSuccess);
        Assert.True(DashboardTemplateRegistration.Register(registry).IsSuccess);
        Assert.True(SigningTemplateRegistration.Register(registry).IsSuccess);
        return registry;
    }
}

public sealed class WorkspaceResolutionTests
{
    [Theory]
    [InlineData("SETUP")]
    [InlineData("REPORT")]
    public void StandardWorkspaceResolves(string code)
    {
        var resolver = new WorkspaceResolver(TemplateRegistryTests.RegisterStandardTemplates());

        var result = resolver.Resolve(new WorkspaceDefinition("workspace", "Workspace", new TemplateCode(code)));

        Assert.True(result.IsSuccess);
        Assert.Equal(new TemplateCode(code), result.Workspace!.TemplateCode);
    }

    [Fact]
    public void CalendarWorkspaceResolvesWithoutStandardModuleChanges()
    {
        var registry = TemplateRegistryTests.RegisterStandardTemplates();
        registry.Register(new TestTemplate(new TemplateCode("CALENDAR")));
        var resolver = new WorkspaceResolver(registry);

        var result = resolver.Resolve(
            new WorkspaceDefinition("calendar", "Calendar", new TemplateCode("CALENDAR")));

        Assert.True(result.IsSuccess);
        Assert.Equal("CALENDAR", result.Workspace!.TemplateCode.Value);
    }

    [Fact]
    public void UnknownWorkspaceDoesNotThrow()
    {
        var resolver = new WorkspaceResolver(new TemplateRegistry());

        var result = resolver.Resolve(
            new WorkspaceDefinition("unknown", "Unknown", new TemplateCode("UNKNOWN")));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Workspace);
        Assert.Contains("UNKNOWN", result.Error, StringComparison.Ordinal);
    }
}

internal sealed class TestTemplate(TemplateCode code) : DynamicTemplateBase
{
    public override TemplateCode TemplateCode { get; } = code;
    public override string ModuleName => "DynamicUI24.Tests.CustomTemplate";
    public override IReadOnlyCollection<TemplateCapability> SupportedCapabilities { get; } =
        [new("SEARCH")];
}
