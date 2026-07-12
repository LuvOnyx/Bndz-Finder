#!/usr/bin/env bash
# Cross-platform build: libraries + tests (Linux/macOS CI)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export PATH="${DOTNET_ROOT:-$HOME/.dotnet}:$PATH"

echo "==> Restoring libraries..."
dotnet restore "$ROOT/BndzFinder.Libraries.slnf"

echo "==> Building libraries..."
dotnet build "$ROOT/BndzFinder.Libraries.slnf" -c Release --no-restore

echo "==> Running tests..."
dotnet test "$ROOT/tests/BndzFinder.Core.Tests/BndzFinder.Core.Tests.csproj" -c Release --no-build
dotnet test "$ROOT/tests/BndzFinder.Shell.Tests/BndzFinder.Shell.Tests.csproj" -c Release --no-build

echo "==> Build complete."
