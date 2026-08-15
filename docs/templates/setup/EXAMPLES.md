# Setup examples

```csharp
var categories = new[]
{
    new SetupCategoryDefinition("catalogs", "MASTER_CATALOGS",
        new("Setup.Category.MasterCatalogs"), StandardIconKeys.Catalog),
    new SetupCategoryDefinition("regions", "REGIONS", new("Setup.Catalog.Regions"),
        StandardIconKeys.Catalog, parentCategoryId: "catalogs", definitionType: "CATALOG")
};

var editors = new SetupEditorRegistry();
editors.Register(new GenericPropertyEditorProvider("CATALOG",
[
    new("name", "NAME", new("Setup.Field.Name"), EditorFieldType.Text, isRequired: true),
    new("active", "ACTIVE", new("Setup.Field.Active"), EditorFieldType.Boolean)
]));
```

The Demo creates ten catalog child categories in a loop. Framework logic has neither a nine-item constant nor a catalog limit. Each child opens its own provider context, and Company A/B return different scoped rows.
