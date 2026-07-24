# Figma → Bndz-Finder asset drop zone

File: https://www.figma.com/design/60c5Bc1Hr12j9tmVFS5qvW/Bndz-Finder

Cloud agents **cannot** authenticate Figma MCP. Drop exports here (or zip + import script) and the app will prefer these over generated fallbacks.

## Required folder layout

```
figma/
  tokens.json                 # optional design tokens (see tokens.example.json)
  menu-bar/
    apple-mark.png            # 24×24 or @2x 48×48, white/light glyph on transparent
    control-center.png        # optional status glyph
    spotlight.png             # optional search glyph
  dock/
    dock-glass.png            # 9-slice friendly frosted pill reference / skin
    separator.png             # optional thin divider
  icons/
    finder.png                # 1024×1024 preferred (or 256+)
    launchpad.png
    safari.png
    messages.png
    mail.png
    maps.png
    photos.png
    facetime.png
    calendar.png
    contacts.png
    notes.png
    apple-tv.png
    music.png
    podcasts.png
    reminders.png
    stocks.png
    app-store.png
    settings.png              # aka preferences / System Settings
    news.png
    voice-memos.png
    downloads.png             # folder stack
    trash.png
    weather.png
  wallpapers/
    sequoia-day.jpg
    sequoia-night.jpg
    tahoe-dark.jpg
```

## How to export from Figma (Dev Mode)

1. Open the **Bndz-Finder** file → select frame (`Menu Bar`, `Dock`, or icon components).
2. In the right panel → **Export** → PNG @ **2x** (icons also export @1x 1024 if designed at that size).
3. Name exports exactly as above (lowercase + hyphens), or drop into a zip and run:

```powershell
pwsh -File BndzFinder/scripts/import-figma-assets.ps1 -SourceZip 'C:\path\to\exports.zip'
# or
pwsh -File BndzFinder/scripts/import-figma-assets.ps1 -SourceDir 'C:\path\to\export-folder'
```

4. Commit the files under `BndzFinder/assets/figma/` and rebuild (`.\run.cmd`).

## Priority / what the runtime uses first

| Need | Uses first | Fallback |
|------|------------|----------|
| System dock icons | `figma/icons/*.png` | `icons/system/*.png` → Skia generator |
| Dock glass skin | `figma/dock/dock-glass.png` | theme pack / acrylic |
| Apple menu mark | `figma/menu-bar/apple-mark.png` | vector Path in XAML |
| Wallpapers | `figma/wallpapers/*` | BundledAssetGenerator |

## Still working on icons?

Drop whatever is ready. Partial sets are fine — missing icons keep the generated squircle until you add the PNG.
