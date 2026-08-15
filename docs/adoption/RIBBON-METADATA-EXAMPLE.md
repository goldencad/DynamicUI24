# Ribbon Metadata Example

```csharp
var ribbon = new RibbonDefinition("main", "MAIN", 1,
[
    new RibbonTabDefinition("home", "HOME", new("Ribbon.Home"),
    [
        new RibbonGroupDefinition("workspace", "WORKSPACE", new("Ribbon.Workspace"),
        [
            new RibbonCommandDefinition(
                "open-report", "OPEN_REPORT", new("Ribbon.OpenReport"),
                StandardIconKeys.Preview, RibbonCommandType.Navigate,
                targetWorkspaceId: "report-demo"),
        ]),
    ]),
]);
```

A contextual group can set `contextRule: new(TemplateCode: StandardTemplateCodes.Report)`. A command can set `permissionRequirement` with Hide, Disable, or ReadOnly behavior and `requiresSelection: true`. For custom dispatch set `CommandType.CustomRegistered` plus `registeredCommandCode`; register the matching handler during application composition.

The metadata is configuration, not executable code. Navigation resolves registered targets, and unknown targets/commands return a safe command result.
