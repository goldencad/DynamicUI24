# Module dependencies

Dependency direction for the template foundation is:

```text
DynamicUI24.Core <- DynamicUI24.Avalonia <- consumer
       ^                          ^
       |                          |
independent template modules ----+
```

The current standard templates require only Core and Shared. They do not reference one another. `DynamicUI24.Avalonia.DynamicWorkspaceHost` depends only on the Core registry/resolver contract and therefore cannot dispatch on concrete templates. A consumer such as the Demo references and composes the framework and the template modules it chooses.

Framework projects never reference the Demo or another consumer. Core never references Avalonia, a template module, or an extension. Architecture tests enforce these project and compiled-assembly boundaries.
