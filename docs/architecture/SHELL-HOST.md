# Shell host

`ShellHost` owns application identity, optional semantic logo, application name,
workspace content, status, graceful Exit, and the standard Application Menu entry/overlay.
It receives `ShellPresentation`, `ILocalizationService`, `IIconRegistry`, and
`IApplicationExitService` through composition. The separately composed menu view is assigned
through `ApplicationMenuContent`, keeping application contributors out of the framework shell.

The central `WorkspaceContent` accepts any Avalonia control. `DynamicWorkspaceHost` remains
a generic adapter over Task 1's `WorkspaceResolver`; it shows workspace title, unchanged
template code, template version, resolved module, capabilities, and safe resolution errors.
There is no template-specific visual branching.

The Demo selectors remain developer proof controls outside `ShellHost`; they are not a Ribbon,
Tree, or final business navigation system. Application Menu Exit reuses the same lifetime service.
