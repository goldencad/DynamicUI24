# Semantic icon registry

Reusable presentation code asks `IIconRegistry` for an `IconKey`; it never names an asset
file. `SemanticIconRegistry` maps standard keys to portable SVG path data and
`SemanticIcon` renders the resolved geometry with theme-aware foreground brushes.

The initial catalog includes Search, Filter, Refresh, Add, Edit, Delete, Import, Export,
Preview, Settings, Warning, Error, Info, Success, and Formula.

Register an app-specific icon:

```csharp
registry.Register(new IconDefinition(new IconKey("MY_ICON"), "M2,2 L20,20"));
```

Override a framework icon explicitly:

```csharp
registry.Register(replacement, replace: true);
```

Unknown keys return the same deterministic missing-icon definition and never throw.
Icon identity must not drive application or business behavior.
