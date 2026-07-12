# Asset Provenance

Bndz-Finder uses **official design references** — not hand-drawn placeholder SVGs.

## System dock icons (Finder, Trash, Launchpad, …)

| Asset | Source | Status |
|-------|--------|--------|
| `assets/icons/system/*.png` | [macosicons.com](https://macosicons.com/) via API | **Import required** — run `scripts/import-macos-icons.ps1` |

Hand-made SVG placeholders have been **removed**. The dock will show broken/missing icons until you import.

```powershell
# Free API key: https://docs.macosicons.com/api-management
$env:MACOSICONS_API_KEY = 'your-key'
pwsh -File BndzFinder/scripts/import-macos-icons.ps1
```

Import writes `assets/icons/system/import-report.json` with per-icon credit and source URLs.

## Dock chrome vs icon shell

| What | Source | Notes |
|------|--------|-------|
| **Dock bar** (glass pill along screen edge) | WinUI `DockBarControl` + glass backdrop | Rendered in code — **not** an SVG squircle |
| **Dock reference** | [macosicons.com/resources](https://macosicons.com/resources) → “macOS dock template” | Layout reference only → `templates/apple-design-resources/dock-reference.png` |
| **App icon shell** (squircle gloss overlay) | [Apple Design Resources](https://developer.apple.com/design/resources/) App Icon Template | → `templates/apple-design-resources/app-icon-shell.png` |

The removed `dock-icon-shell.svg` was a incorrect hand-drawn squircle. It was **never** the dock bar, and must not be used as one.

## Fonts

| Asset | Source |
|-------|--------|
| SF Pro | [Apple Design Resources — Fonts](https://developer.apple.com/design/resources/) |

```powershell
pwsh -File BndzFinder/scripts/import-apple-design-resources.ps1 -ImportSfPro
```

## Shaders & audio

| Asset | Source |
|-------|--------|
| `assets/shaders/liquid_glass.hlsl` | Original — compiled on Windows host |
| `assets/sounds/` | Pending — add licensed minimize/restore sounds |

## Runtime resolution

`AssetCatalogService` resolves `assets/` from the app output directory (`bin/.../assets/`) after build. Missing system icons are listed via `GetMissingSystemIcons()`.

Do not commit third-party icon PNGs until `import-report.json` documents credits.
