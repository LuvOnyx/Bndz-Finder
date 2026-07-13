# Apple Design Resources

## Bundled automatically (no API key)

| Asset | How |
|-------|-----|
| System dock icons (Finder, Trash, …) | `SystemIconFallbackGenerator` on first run — Apple squircle proportions |
| Default theme pack `sequoia-default` | `BundledAssetGenerator` — wallpapers, dock glass skin, icon shell |
| UI font | Segoe UI Variable Display (Windows 11); SF Pro optional via import script |
| App icon gloss shell | Generated PNG or optional Apple template import |

## Optional imports (higher fidelity)

### SF Pro fonts

```powershell
pwsh -File BndzFinder/scripts/import-apple-design-resources.ps1 -ImportSfPro
```

### Official Apple app icon shell PNG

Export from [developer.apple.com/design/resources](https://developer.apple.com/design/resources/) then:

```powershell
pwsh -File BndzFinder/scripts/import-apple-design-resources.ps1 -AppIconShellPath 'C:\path\to\shell.png'
```

### macosicons.com (optional enhancement)

```powershell
$env:MACOSICONS_API_KEY = 'your-key'
pwsh -File BndzFinder/scripts/import-macos-icons.ps1
```

Official macosicons PNGs override generated fallbacks when present in `assets/icons/system/`.

## Theme packs

Preferences → **Themes** lets you pick installed packs, switch wallpapers, import `.zip` packs, and apply accent + font settings.

Default pack installs to `%AppData%\BndzFinder\themes\sequoia-default\` on first launch.
