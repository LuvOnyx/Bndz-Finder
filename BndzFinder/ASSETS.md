# Asset Provenance

System icons and Apple Design Resource templates for Bndz-Finder.

| Asset | Source | License Status |
|-------|--------|----------------|
| Dock icon shell template | `assets/templates/dock-icon-shell.svg` | Design reference — original SVG placeholder |
| System icons (Finder, Trash, etc.) | `assets/icons/system/*.svg` | Original placeholders — replace with licensed packs from macosicons.com before shipping |
| UI sounds | `assets/sounds/` | Pending — add licensed minimize/restore sounds |
| Shaders | `assets/shaders/liquid_glass.hlsl` | Original — compiled on Windows host |

## Import pipeline

Use `AssetCatalogService` (`BndzFinder.Shell/Assets/AssetCatalogService.cs`) to resolve and stage assets:

```csharp
var catalog = new AssetCatalogService();
catalog.TryImportPlaceholder("icon-finder", targetDirectory);
```

Do not commit third-party icon packs until license is documented in this file.
