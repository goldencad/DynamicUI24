# Shell host

`ShellHost` owns only application identity, optional semantic logo, application name,
workspace content, status, and graceful Exit. It receives `ShellPresentation`,
`ILocalizationService`, `IIconRegistry`, and `IApplicationExitService` through composition.

The central `WorkspaceContent` accepts any Avalonia control. `DynamicWorkspaceHost` remains
a generic adapter over Task 1's `WorkspaceResolver`; it shows workspace title, unchanged
template code, template version, resolved module, capabilities, and safe resolution errors.
There is no template-specific visual branching.

The Demo selectors are developer proof controls outside `ShellHost`. They are not a Ribbon,
Application Menu, Tree, or final navigation system. Future Application Menu Exit can reuse
the same lifetime service.
