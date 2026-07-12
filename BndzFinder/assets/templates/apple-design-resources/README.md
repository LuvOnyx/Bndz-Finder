# Apple Design Resources (required for premium parity)

## What is implemented in code today

| Piece | Source | Status |
|-------|--------|--------|
| Dock pill metrics (24px radius, margins, shadow) | `AppleDesignMetrics` — aligned with Apple UI Kit | **In code** |
| App icon squircle (1024 canvas, 824 content, 22.37% radius) | Apple App Icon Template proportions | **In code** via `AppleIconShellRenderer` |
| App icon gloss/shadow overlay | Programmatic shell OR imported PNG | **Programmatic fallback**; PNG optional |
| System icons (Finder, Trash, …) | [macosicons.com](https://macosicons.com/) | **Import required** — see below |
| Dock chrome reference PNG | Apple Figma/Sketch UI Kit export | **Manual import** |

## What you must import (not bundled — licensing)

### 1. System icons — macosicons.com

```powershell
$env:MACOSICONS_API_KEY = 'your-key'   # https://docs.macosicons.com/api-management
pwsh -File BndzFinder/scripts/import-macos-icons.ps1
```

### 2. Apple app icon shell PNG (optional, improves gloss fidelity)

Export the **App Icon Template** layer from [developer.apple.com/design/resources](https://developer.apple.com/design/resources/) as `app-icon-shell.png` (1024×1024), then:

```powershell
pwsh -File BndzFinder/scripts/import-apple-design-resources.ps1 -AppIconShellPath 'C:\path\to\shell.png'
```

### 3. Dock reference (layout tuning only)

Download **macOS dock template** from [macosicons.com/resources](https://macosicons.com/resources) or export from Apple macOS UI Kit, save as `dock-reference.png`.

## Removed placeholders

The old hand-drawn `dock-icon-shell.svg` and `icons/system/*.svg` files were **not** official assets and have been removed.
