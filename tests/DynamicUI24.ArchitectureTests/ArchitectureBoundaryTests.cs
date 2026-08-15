using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using Xunit;

namespace DynamicUI24.ArchitectureTests;

public sealed class ArchitectureBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly IReadOnlyDictionary<string, ProjectInfo> Projects = LoadProjects();

    [Fact]
    public void CoreDoesNotReferenceAvalonia()
    {
        AssertProjectAndAssemblyReferencesExclude("DynamicUI24.Core", static name =>
            name.Equals("Avalonia", StringComparison.Ordinal) ||
            name.StartsWith("Avalonia.", StringComparison.Ordinal));
    }

    [Fact]
    public void CoreDoesNotReferenceTemplateProjects()
    {
        AssertProjectAndAssemblyReferencesExclude("DynamicUI24.Core", static name =>
            name.StartsWith("DynamicUI24.Template.", StringComparison.Ordinal));
    }

    [Fact]
    public void CoreDoesNotReferenceExtensions()
    {
        AssertProjectAndAssemblyReferencesExclude("DynamicUI24.Core", static name =>
            IsExtension(name));
    }

    [Fact]
    public void SharedHasNoConsumerCoreOrTemplateDependencies()
    {
        AssertProjectAndAssemblyReferencesExclude("DynamicUI24.Shared", static name =>
            name.Equals("DynamicUI24.Core", StringComparison.Ordinal) ||
            name.Equals("DynamicUI24.Demo", StringComparison.Ordinal) ||
            name.StartsWith("DynamicUI24.Template.", StringComparison.Ordinal));
    }

    [Fact]
    public void PresentationFrameworkHasNoConsumerOrTemplateDependency()
    {
        AssertProjectAndAssemblyReferencesExclude("DynamicUI24.Avalonia", static name =>
            name.Equals("DynamicUI24.Demo", StringComparison.Ordinal) ||
            name.StartsWith("DynamicUI24.Template.", StringComparison.Ordinal) ||
            name.Contains("PayCalc24", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FrameworkProjectsDoNotReferenceDemo()
    {
        foreach (var project in FrameworkProjects())
        {
            Assert.DoesNotContain(project.References, reference =>
                reference.Name.Equals("DynamicUI24.Demo", StringComparison.Ordinal));
            Assert.DoesNotContain(ReadAssemblyReferences(project.Name), reference =>
                reference.Equals("DynamicUI24.Demo", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void DynamicUI24ProjectsDoNotReferencePayCalc24()
    {
        foreach (var project in Projects.Values)
        {
            Assert.DoesNotContain("PayCalc24", File.ReadAllText(project.Path), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(ReadAssemblyReferences(project.Name), reference =>
                reference.Contains("PayCalc24", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void TemplateProjectGraphIsAcyclicAndIsolated()
    {
        var templates = Projects.Values
            .Where(project => project.Name.StartsWith("DynamicUI24.Template.", StringComparison.Ordinal))
            .ToArray();

        Assert.All(templates, project =>
            Assert.DoesNotContain(project.References, reference =>
                reference.Name.StartsWith("DynamicUI24.Template.", StringComparison.Ordinal)));

        AssertAcyclic(templates);
    }

    [Fact]
    public void GenericWorkspaceHostCannotReferenceConcreteTemplateModules()
    {
        foreach (var projectName in new[] { "DynamicUI24.Core", "DynamicUI24.Avalonia" })
        {
            AssertProjectAndAssemblyReferencesExclude(projectName, static name =>
                name.StartsWith("DynamicUI24.Template.", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void RegistryAssemblyCannotReferenceConsumerApplications()
    {
        AssertProjectAndAssemblyReferencesExclude("DynamicUI24.Core", static name =>
            name.Equals("DynamicUI24.Demo", StringComparison.Ordinal));
    }

    [Fact]
    public void ConsumerSpecificNamespacesAreAbsentFromFrameworkAssemblies()
    {
        foreach (var project in FrameworkProjects())
        {
            var namespaces = ReadTypeNamespaces(project.Name);
            Assert.DoesNotContain(namespaces, value => value.Contains("PayCalc24", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(namespaces, value => value.StartsWith("DynamicUI24.Demo", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CompanyAndAuthorizationContractsRemainFrameworkGeneric()
    {
        foreach (var projectName in new[] { "DynamicUI24.Core", "DynamicUI24.Shared" })
        {
            AssertProjectAndAssemblyReferencesExclude(projectName, static name =>
                name.Equals("DynamicUI24.Demo", StringComparison.Ordinal) ||
                name.Contains("Avalonia", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Odoo", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("PayCalc24", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void FrameworkDoesNotImplementConsumerRoleAdministrationOrProviderModels()
    {
        foreach (var project in FrameworkProjects())
        {
            var namespaces = ReadTypeNamespaces(project.Name);
            Assert.DoesNotContain(namespaces, value =>
                value.Contains("Odoo", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("PayCalc24", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("RoleAdministration", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("UserAdministration", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ApplicationMenuIsOwnedByFrameworkShellAndNotTemplates()
    {
        var frameworkNamespaces = new[] { "DynamicUI24.Core", "DynamicUI24.Avalonia" }
            .SelectMany(ReadTypeNamespaces)
            .Where(value => value.Contains("ApplicationMenu", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(frameworkNamespaces);

        foreach (var template in Projects.Values.Where(project =>
                     project.Name.StartsWith("DynamicUI24.Template.", StringComparison.Ordinal)))
        {
            Assert.DoesNotContain(ReadTypeNamespaces(template.Name), value =>
                value.Contains("ApplicationMenu", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ApplicationMenuFrameworkHasNoAuthenticationOrLicenseEnforcementBackend()
    {
        foreach (var projectName in new[] { "DynamicUI24.Core", "DynamicUI24.Avalonia" })
        {
            Assert.DoesNotContain(ReadTypeNamespaces(projectName), value =>
                value.Contains("Authentication", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("LicenseEnforcement", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("EntitlementGuard", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void RepositoryContainsNoOwnerAccountBrandingOutsideRepositoryUrls()
    {
        var ownerAccount = "\u0067oldencad";
        var permittedUrl = $"github.com/{ownerAccount}/DynamicUI24";
        var textExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".csproj", ".axaml", ".md", ".json", ".props", ".targets", ".slnx", ".xml", ".yml", ".yaml",
        };
        var violations = Directory.EnumerateFiles(RepositoryRoot, "*", SearchOption.AllDirectories)
            .Where(path => textExtensions.Contains(Path.GetExtension(path)))
            .Where(path => !IsGeneratedOrRepositoryMetadata(path))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new { path, line, index }))
            .Where(item => item.line.Contains(ownerAccount, StringComparison.OrdinalIgnoreCase))
            .Where(item => !item.line.Contains(permittedUrl, StringComparison.OrdinalIgnoreCase))
            .Select(item => $"{Path.GetRelativePath(RepositoryRoot, item.path)}:{item.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void RibbonModelAndDispatchRemainGenericCoreTypes()
    {
        var namespaces = ReadTypeNamespaces("DynamicUI24.Core");
        Assert.Contains("DynamicUI24.Core.Ribbon", namespaces);
        AssertProjectAndAssemblyReferencesExclude("DynamicUI24.Core", static name =>
            name.Contains("Actipro", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Avalonia", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Odoo", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("PayCalc24", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RibbonHostIsFrameworkOwnedAndAbsentFromTemplateModules()
    {
        Assert.Contains(ReadTypeNamespaces("DynamicUI24.Avalonia"), value =>
            value.Equals("DynamicUI24.Avalonia.Presentation", StringComparison.Ordinal));
        foreach (var template in Projects.Values.Where(project =>
                     project.Name.StartsWith("DynamicUI24.Template.", StringComparison.Ordinal)))
        {
            Assert.DoesNotContain(ReadTypeNamespaces(template.Name), value =>
                value.Contains("Ribbon", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(Directory.EnumerateFiles(Path.GetDirectoryName(template.Path)!, "*.cs", SearchOption.AllDirectories),
                path => File.ReadAllText(path).Contains("Ribbon", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ActionBarFoundationIsGenericAndFrameworkOwned()
    {
        var coreNamespaces = ReadTypeNamespaces("DynamicUI24.Core");
        Assert.Contains("DynamicUI24.Core.ActionBars", coreNamespaces);
        Assert.Contains(ReadTypeNamespaces("DynamicUI24.Avalonia"), value =>
            value.Equals("DynamicUI24.Avalonia.Presentation", StringComparison.Ordinal));
        AssertProjectAndAssemblyReferencesExclude("DynamicUI24.Core", static name =>
            name.Contains("Actipro", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Avalonia", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Odoo", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("PayCalc24", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ActionBarHostDoesNotCoupleToTemplatesTreeRibbonOrSvgPaths()
    {
        var path = Path.Combine(RepositoryRoot, "src", "DynamicUI24.Avalonia", "Presentation",
            "DynamicActionBarHost.cs");
        var source = File.ReadAllText(path);
        Assert.DoesNotContain("DynamicUI24.Template.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DynamicTree", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DynamicRibbon", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Geometry.Parse", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SvgPathData", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedActionVariantsRemainMetadataDrivenAndTemplateNeutral()
    {
        var core = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "DynamicUI24.Core", "ActionBars",
            "ActionBarDefinitions.cs"));
        var host = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "DynamicUI24.Avalonia", "Presentation",
            "DynamicActionBarHost.cs"));
        Assert.All(new[] { "DropdownButton", "SplitButton", "IconButton", "ToggleButton" }, value =>
            Assert.Contains(value, core, StringComparison.Ordinal));
        Assert.Contains("at most two hierarchy levels", core, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DynamicUI24.Core.Setup", host, StringComparison.Ordinal);
        Assert.DoesNotContain("SetupActionCodes", host, StringComparison.Ordinal);
    }

    [Fact]
    public void ActionGeometryAndIconSourcesPreserveSemanticMetadataBoundary()
    {
        var actionDirectory = Path.Combine(RepositoryRoot, "src", "DynamicUI24.Core", "ActionBars");
        var actionSource = string.Join("\n", Directory.EnumerateFiles(actionDirectory, "*.cs").Select(File.ReadAllText));
        var iconContract = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "DynamicUI24.Shared", "Presentation", "IconKey.cs"));
        Assert.Contains("ActionControlSizePreset", actionSource, StringComparison.Ordinal);
        Assert.Contains("ActionControlGeometry", actionSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".svg", actionSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".ttf", actionSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SvgIconSource", iconContract, StringComparison.Ordinal);
        Assert.Contains("FontGlyphIconSource", iconContract, StringComparison.Ordinal);
        Assert.Contains("IIconRegistry", iconContract, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedDesignSystemStandardsAreDiscoverable()
    {
        var directory = Path.Combine(RepositoryRoot, "docs", "design-system");
        var required = new[] { "OVERVIEW.md", "BUTTONS.md", "ICONS.md", "TREE-NAVIGATION.md",
            "SPLIT-NAVIGATION-LAYOUT.md", "TOKENS.md" };
        Assert.All(required, file => Assert.True(File.Exists(Path.Combine(directory, file)), file));
        var rootReadme = File.ReadAllText(Path.Combine(RepositoryRoot, "README.md"));
        var overview = File.ReadAllText(Path.Combine(directory, "OVERVIEW.md"));
        Assert.Contains("docs/design-system/OVERVIEW.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("metadata, tokens, registries, providers, and extension points", overview, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalAndSetupTreesUseSharedTokenBasedRowInteraction()
    {
        var treeXaml = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "DynamicUI24.Avalonia", "Presentation",
            "DynamicTreeHost.axaml"));
        var setupHost = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "DynamicUI24.Avalonia", "Presentation",
            "SetupWorkspaceHost.cs"));
        Assert.DoesNotContain("#", treeXaml, StringComparison.Ordinal);
        Assert.All(new[] { "DuiHoverBrush", "DuiSelectionBrush", "DuiSelectionHoverBrush", "DuiFocusBrush", "DuiDisabledSurfaceBrush" },
            token => Assert.Contains(token, treeXaml, StringComparison.Ordinal));
        Assert.Contains("DynamicTreeHost", setupHost, StringComparison.Ordinal);
        Assert.DoesNotContain("SetupTreeRow", setupHost, StringComparison.Ordinal);
    }

    [Fact]
    public void ActionBarNavigationAndDispatchUseGenericCoreServices()
    {
        var path = Path.Combine(RepositoryRoot, "src", "DynamicUI24.Core", "ActionBars",
            "ActionBarCommands.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("IWorkspaceNavigationService", source, StringComparison.Ordinal);
        Assert.Contains("IActionCommandRegistry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new DynamicTemplate", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DynamicTree", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DynamicRibbon", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupFoundationRemainsGenericAndBackendFree()
    {
        var directory = Path.Combine(RepositoryRoot, "src", "DynamicUI24.Core", "Setup");
        var source = string.Join("\n", Directory.EnumerateFiles(directory, "*.cs").Select(File.ReadAllText));
        Assert.DoesNotContain("PayCalc24", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Odoo", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFramework", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.Data", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SvgPathData", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupEditorResolutionUsesRegistryWithoutDefinitionTypeSwitch()
    {
        var registryPath = Path.Combine(RepositoryRoot, "src", "DynamicUI24.Core", "Setup", "SetupEditors.cs");
        var hostPath = Path.Combine(RepositoryRoot, "src", "DynamicUI24.Avalonia", "Presentation", "SetupWorkspaceHost.cs");
        var registry = File.ReadAllText(registryPath);
        var host = File.ReadAllText(hostPath);
        Assert.Contains("SetupEditorRegistry", registry, StringComparison.Ordinal);
        Assert.Contains("editors.Resolve(definition)", host, StringComparison.Ordinal);
        Assert.DoesNotContain("DefinitionType switch", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Geometry.Parse", host, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupHostConsumesSharedActionBarAndSemanticIconPrimitives()
    {
        var path = Path.Combine(RepositoryRoot, "src", "DynamicUI24.Avalonia", "Presentation", "SetupWorkspaceHost.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("DynamicActionBarHost", source, StringComparison.Ordinal);
        Assert.Contains("DynamicTreeHost", source, StringComparison.Ordinal);
        Assert.Contains("TreeOverflowOptions", source, StringComparison.Ordinal);
        Assert.Contains("SetupActionBarDefinitions", source, StringComparison.Ordinal);
        Assert.Contains("IIconRegistry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SvgPathData", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SplitNavigationLayoutIsGenericReusableAndRuntimeOnly()
    {
        var hostPath = Path.Combine(RepositoryRoot, "src", "DynamicUI24.Avalonia", "Presentation",
            "DynamicSplitNavigationHost.cs");
        var statePath = Path.Combine(RepositoryRoot, "src", "DynamicUI24.Shared", "Presentation",
            "SplitNavigationLayout.cs");
        var setupPath = Path.Combine(RepositoryRoot, "src", "DynamicUI24.Avalonia", "Presentation",
            "SetupWorkspaceHost.cs");
        var host = File.ReadAllText(hostPath);
        var state = File.ReadAllText(statePath);
        var setup = File.ReadAllText(setupPath);
        Assert.Contains("GridSplitter", host, StringComparison.Ordinal);
        Assert.Contains("DynamicSplitNavigationHost", setup, StringComparison.Ordinal);
        Assert.DoesNotContain("DynamicUI24.Core.Setup", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Settings", state, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("File.", state, StringComparison.Ordinal);
    }

    [Fact]
    public void TemplateModulesContainNoActionBarBehavior()
    {
        foreach (var template in Projects.Values.Where(project =>
                     project.Name.StartsWith("DynamicUI24.Template.", StringComparison.Ordinal)))
        {
            Assert.DoesNotContain(Directory.EnumerateFiles(Path.GetDirectoryName(template.Path)!, "*.cs", SearchOption.AllDirectories),
                path => File.ReadAllText(path).Contains("ActionBar", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ApplicationMenuAndRibbonHaveSeparateShellRegions()
    {
        var shellXaml = File.ReadAllText(Path.Combine(RepositoryRoot,
            "src", "DynamicUI24.Avalonia", "Presentation", "ShellHost.axaml"));
        Assert.Contains("ApplicationMenuPresenter", shellXaml, StringComparison.Ordinal);
        Assert.Contains("RibbonPresenter", shellXaml, StringComparison.Ordinal);
        Assert.NotEqual(shellXaml.IndexOf("ApplicationMenuPresenter", StringComparison.Ordinal),
            shellXaml.IndexOf("RibbonPresenter", StringComparison.Ordinal));
    }

    private static void AssertProjectAndAssemblyReferencesExclude(
        string projectName,
        Func<string, bool> isForbidden)
    {
        var project = Projects[projectName];
        Assert.DoesNotContain(project.References, reference => isForbidden(reference.Name));
        Assert.DoesNotContain(ReadAssemblyReferences(projectName), reference => isForbidden(reference));
    }

    private static IEnumerable<ProjectInfo> FrameworkProjects() =>
        Projects.Values.Where(project => project.Path.StartsWith(
            Path.Combine(RepositoryRoot, "src") + Path.DirectorySeparatorChar,
            StringComparison.Ordinal));

    private static bool IsExtension(string name) =>
        name is "DynamicUI24.Excel" or "DynamicUI24.Reporting" or "DynamicUI24.Documents" or "DynamicUI24.Batch";

    private static bool IsGeneratedOrRepositoryMetadata(string path)
    {
        var relative = Path.GetRelativePath(RepositoryRoot, path);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment => segment is ".git" or "bin" or "obj");
    }

    private static IReadOnlyDictionary<string, ProjectInfo> LoadProjects()
    {
        var roots = new[] { "src", "samples", "tests", "benchmarks" };
        var paths = roots
            .SelectMany(root => Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot, root), "*.csproj", SearchOption.AllDirectories))
            .ToArray();

        var projects = paths.ToDictionary(
            path => Path.GetFileNameWithoutExtension(path),
            path => new ProjectInfo(Path.GetFileNameWithoutExtension(path), path, []),
            StringComparer.Ordinal);

        foreach (var project in projects.Values)
        {
            var document = XDocument.Load(project.Path);
            var references = document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(include => include is not null)
                .Select(include => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.Path)!, include!)))
                .Select(path => projects[Path.GetFileNameWithoutExtension(path)])
                .ToArray();
            project.References.AddRange(references);
        }

        return projects;
    }

    private static void AssertAcyclic(IEnumerable<ProjectInfo> projects)
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var project in projects)
        {
            Visit(project, visiting, visited);
        }
    }

    private static void Visit(ProjectInfo project, HashSet<string> visiting, HashSet<string> visited)
    {
        if (visited.Contains(project.Name))
        {
            return;
        }

        Assert.True(visiting.Add(project.Name), $"Circular project dependency detected at {project.Name}.");
        foreach (var reference in project.References.Where(reference =>
                     reference.Name.StartsWith("DynamicUI24.Template.", StringComparison.Ordinal)))
        {
            Visit(reference, visiting, visited);
        }

        visiting.Remove(project.Name);
        visited.Add(project.Name);
    }

    private static IReadOnlyList<string> ReadAssemblyReferences(string projectName)
    {
        using var stream = File.OpenRead(GetAssemblyPath(projectName));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        return reader.AssemblyReferences
            .Select(handle => reader.GetString(reader.GetAssemblyReference(handle).Name))
            .ToArray();
    }

    private static IReadOnlyList<string> ReadTypeNamespaces(string projectName)
    {
        using var stream = File.OpenRead(GetAssemblyPath(projectName));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        return reader.TypeDefinitions
            .Select(handle => reader.GetString(reader.GetTypeDefinition(handle).Namespace))
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string GetAssemblyPath(string projectName)
    {
        var configuration = Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))!.Name;
        var project = Projects[projectName];
        return Path.Combine(Path.GetDirectoryName(project.Path)!, "bin", configuration, "net9.0", projectName + ".dll");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DynamicUI24.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed record ProjectInfo(string Name, string Path, List<ProjectInfo> References);
}
