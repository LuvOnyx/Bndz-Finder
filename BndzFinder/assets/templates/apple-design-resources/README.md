# Apple Design Resources (required for premium dock chrome)

Bndz-Finder does **not** render the dock from a squircle SVG. The dock bar is drawn in WinUI (`DockBarControl`) with glass/backdrop styling. This folder holds **official Apple design reference assets** used by the icon pipeline and typography.

## What to import

| Asset | Official source | Used for |
|-------|-----------------|----------|
| macOS UI Kit (dock, menus, controls) | [Apple Design Resources — macOS](https://developer.apple.com/design/resources/) | Dock chrome measurements, spacing, dark mode reference |
| App Icon Template | [Apple Design Resources — iOS App Icon Template](https://developer.apple.com/design/resources/) (Photoshop/Illustrator) | Squircle grid + gloss shell for `MacStyleIconPipeline` |
| SF Pro / SF Pro Rounded | [Apple Design Resources — Fonts](https://developer.apple.com/design/resources/) | Finder bar, labels, preferences UI |
| macOS dock + icon templates | [macosicons.com Resources](https://macosicons.com/resources) (`macOS`, `macOS dock template`) | Community-maintained dock reference aligned with macOS |

## Import script

From repo root (Windows):

```powershell
# Optional: set API key only needed for icon import, not Apple fonts
$env:MACOSICONS_API_KEY = 'your-key-from-docs.macosicons.com'

pwsh -File BndzFinder/scripts/import-apple-design-resources.ps1
pwsh -File BndzFinder/scripts/import-macos-icons.ps1
```

After import, you should have:

- `app-icon-shell.png` — extracted app icon shell overlay (1024×1024)
- `dock-reference.png` — dock chrome reference (from macosicons or Apple UI kit export)
- `../fonts/SF-Pro-*.otf` — SF Pro faces (optional but recommended)

## Removed placeholder

The old `dock-icon-shell.svg` was a **hand-drawn squircle**, not Apple's template and **not** the dock bar. It has been removed. Do not substitute it for dock UI or official icons.
