using System.Text.RegularExpressions;
using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class DesignSystemArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void StandardAndThemeContractsAreOwnedBySharedAndRemainSeparate()
    {
        var contract = Read("src/DynamicUI24.Shared/Presentation/DesignSystemContracts.cs");
        Assert.Contains("interface IPresentationStandard", contract, StringComparison.Ordinal);
        Assert.Contains("interface IThemeDefinition", contract, StringComparison.Ordinal);
        Assert.Contains("class ThemeResolver", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("Avalonia", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("Actipro", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("DevExpress", contract, StringComparison.Ordinal);
    }

    [Fact]
    public void StandardPublishesTypographyLayoutAndAllSharedComponentAuthorities()
    {
        var contract = Read("src/DynamicUI24.Shared/Presentation/DesignSystemContracts.cs");
        Assert.Contains("Typography.GridHeader", contract, StringComparison.Ordinal);
        Assert.Contains("Form.SectionGap", contract, StringComparison.Ordinal);
        Assert.Contains("Control.Height.Standard", contract, StringComparison.Ordinal);
        Assert.Contains("enum ComponentRole", contract, StringComparison.Ordinal);
        Assert.Contains("NavigationTree", contract, StringComparison.Ordinal);
        Assert.Contains("HelpValidation", contract, StringComparison.Ordinal);
        Assert.Contains("enum NavigationTreePart", contract, StringComparison.Ordinal);
    }

    [Fact]
    public void PhysicalFontMappingIsPlatformOwnedAndFallbackSafe()
    {
        var mapping = Read("src/DynamicUI24.Avalonia/Presentation/AvaloniaPlatformFontMapping.cs");
        Assert.Contains("OperatingSystem.IsMacOS", mapping, StringComparison.Ordinal);
        Assert.Contains("OperatingSystem.IsWindows", mapping, StringComparison.Ordinal);
        Assert.Contains("sans-serif", mapping, StringComparison.Ordinal);
        Assert.DoesNotContain("PayCalc24", mapping, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThemeLifecycleCoreIsVendorAndPersistenceNeutral()
    {
        var lifecycle = Read("src/DynamicUI24.Core/DesignSystem/ThemeLifecycle.cs");
        Assert.DoesNotContain("Avalonia", lifecycle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Actipro", lifecycle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DevExpress", lifecycle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MariaDB", lifecycle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DbContext", lifecycle, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Data", lifecycle, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT ", lifecycle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT ", lifecycle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PreviewUsesSemanticSessionIdentityAndNoVisualControlIdentity()
    {
        var lifecycle = Read("src/DynamicUI24.Core/DesignSystem/ThemeLifecycle.cs");
        Assert.Contains("interface IThemePreviewSession", lifecycle, StringComparison.Ordinal);
        Assert.Contains("ThemePreviewSessionId", lifecycle, StringComparison.Ordinal);
        Assert.DoesNotContain("Avalonia.Controls", lifecycle, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\b(Control|StyledElement|Visual)\s+(Preview|Identity|Session)", lifecycle);
    }

    [Fact]
    public void ThemeAuthorizationReusesTask10HCapabilitiesWithoutRoleNames()
    {
        var lifecycle = Read("src/DynamicUI24.Core/DesignSystem/ThemeLifecycle.cs");
        var authorization = Read("src/DynamicUI24.Core/Authoring/UiAuthorization.cs");
        Assert.Contains("StandardUiCapabilities.CanPublishTheme", lifecycle, StringComparison.Ordinal);
        Assert.Contains("CapabilityCode CanPublishTheme", authorization, StringComparison.Ordinal);
        Assert.DoesNotContain("Administrator", lifecycle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ViewerRole", lifecycle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThemeCannotRedefineStandardOrOwnAuditStorage()
    {
        var lifecycle = Read("src/DynamicUI24.Core/DesignSystem/ThemeLifecycle.cs");
        var mappingsStart = lifecycle.IndexOf("public sealed record ThemeMappings", StringComparison.Ordinal);
        var mappingsEnd = lifecycle.IndexOf("public sealed record ThemeVersionDefinition", StringComparison.Ordinal);
        var mappings = lifecycle[mappingsStart..mappingsEnd];
        Assert.DoesNotContain("Anatomy", mappings, StringComparison.Ordinal);
        Assert.DoesNotContain("Command", mappings, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", mappings, StringComparison.Ordinal);
        Assert.Contains("interface IThemeAuditSink", lifecycle, StringComparison.Ordinal);
        Assert.DoesNotContain("ThemeAuditDatabase", lifecycle, StringComparison.Ordinal);
        Assert.DoesNotContain("ThemeAuditRepository", lifecycle, StringComparison.Ordinal);
    }

    [Fact]
    public void DocsViewDesktopAndMobileThemeBoundaryIsDocumentedWithoutFlutterImplementation()
    {
        var guidance = Read("docs/design-system/DOCSVIEW24-THEME-BOUNDARY.md");
        Assert.Contains("desktop DocsView24 application chrome consumes", guidance, StringComparison.Ordinal);
        Assert.Contains("Document-native content remains renderer/document-owned", guidance, StringComparison.Ordinal);
        Assert.Contains("Mobile DocsView24/Flutter does not reference DynamicUI24 desktop binaries", guidance, StringComparison.Ordinal);

        var flutterFiles = Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".dart" or ".yaml")
            .ToArray();
        Assert.Empty(flutterFiles);
    }

    [Fact]
    public void ApplicationAndSampleCodeDoNotOwnFontFamilies()
    {
        var files = SourceFiles("samples").Concat(SourceFiles("src/Templates"));
        var violations = files.Where(path => File.ReadAllText(path).Contains("FontFamily", StringComparison.Ordinal)).ToArray();
        Assert.Empty(violations);
    }

    [Fact]
    public void RawColorsAreConfinedToThemeLayerWithReviewedLegacyBrandMapping()
    {
        var candidates = SourceFiles("src").Concat(SourceFiles("samples"));
        var violations = candidates
            .SelectMany(path => File.ReadLines(path).Select((line, index) => (path, line, index)))
            .Where(item => Regex.IsMatch(item.line, "#[0-9A-Fa-f]{6,8}"))
            .Where(item => !Relative(item.path).Equals("src/DynamicUI24.Avalonia/Presentation/DesignTokens.axaml", StringComparison.Ordinal))
            .Where(item => !(Relative(item.path).Equals("samples/DynamicUI24.Demo/App.axaml", StringComparison.Ordinal) &&
                             item.line.Contains("DuiAccentBrush", StringComparison.Ordinal)))
            .Where(item => !(Relative(item.path).Equals("samples/DynamicUI24.Demo/MainWindow.axaml.cs", StringComparison.Ordinal) &&
                             item.line.Contains("ApplicationBrand", StringComparison.Ordinal)))
            .Select(item => $"{Relative(item.path)}:{item.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationMetadataDoesNotUseAbsolutePositioningAuthority()
    {
        var metadataFiles = SourceFiles("samples").Concat(SourceFiles("src/Templates"));
        var prohibited = new[] { "Canvas.Left", "Canvas.Top", "AbsoluteX", "AbsoluteY", "PixelPosition" };
        var violations = metadataFiles.Where(path => prohibited.Any(value =>
            File.ReadAllText(path).Contains(value, StringComparison.Ordinal))).ToArray();
        Assert.Empty(violations);
    }

    [Fact]
    public void ComponentRoleTaxonomiesCannotBeRedefinedByApplications()
    {
        var applicationText = string.Join('\n', SourceFiles("samples").Concat(SourceFiles("src/Templates")).Select(File.ReadAllText));
        Assert.DoesNotMatch(@"\b(enum|class|record)\s+(ButtonRole|EditorRole|GridRole|DensityRole)\b", applicationText);
    }

    [Fact]
    public void SpecificationAndGuidancePublishRequiredAuthorities()
    {
        var spec = Read("docs/specification/DynamicUI24-Spec-v0.16.md");
        Assert.Contains("STANDARD != THEME != APPLICATION METADATA", spec, StringComparison.Ordinal);
        Assert.Contains("Typography.GridHeader", spec, StringComparison.Ordinal);
        Assert.Contains("Navigation Tree", spec, StringComparison.Ordinal);
        Assert.Contains("physical acceptance", spec, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SourceFiles(string relativeDirectory) =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot, relativeDirectory), "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".axaml")
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(RepositoryRoot, relativePath));
    private static string Relative(string path) => Path.GetRelativePath(RepositoryRoot, path).Replace('\\', '/');

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "DynamicUI24.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
