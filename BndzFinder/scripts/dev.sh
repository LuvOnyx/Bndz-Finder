#!/usr/bin/env bash
# Dev workflow: build libraries, then instructions for Windows UI
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
"$SCRIPT_DIR/build.sh"

cat <<'EOF'

Windows UI build (requires Windows 11 x64 + Windows App SDK):
  pwsh -File BndzFinder/scripts/build.ps1
  pwsh -File BndzFinder/scripts/build.ps1 -Publish

Run on Windows:
  dotnet run --project BndzFinder/src/BndzFinder.ShellHost/BndzFinder.ShellHost.csproj
  dotnet run --project BndzFinder/src/BndzFinder.App/BndzFinder.App.csproj

EOF
