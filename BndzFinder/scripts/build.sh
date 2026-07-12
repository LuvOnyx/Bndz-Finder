#!/usr/bin/env bash
# Cross-platform build: libraries + tests (Linux/macOS CI)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

ensure_dotnet10() {
  if dotnet --version 2>/dev/null | grep -qE '^10\.0\.'; then
    return 0
  fi
  echo "==> Installing .NET 10 SDK for CI..."
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0 --install-dir "$DOTNET_ROOT"
  export PATH="$DOTNET_ROOT:$PATH"
}

ensure_dotnet10

echo "==> Using SDK $(dotnet --version)"
echo "==> Restoring libraries..."
dotnet restore "$ROOT/BndzFinder.Libraries.slnf"

echo "==> Building libraries..."
dotnet build "$ROOT/BndzFinder.Libraries.slnf" -c Release --no-restore

echo "==> Running tests..."
dotnet test "$ROOT/tests/BndzFinder.Core.Tests/BndzFinder.Core.Tests.csproj" -c Release --no-build
dotnet test "$ROOT/tests/BndzFinder.Shell.Tests/BndzFinder.Shell.Tests.csproj" -c Release --no-build

echo "==> Build complete."
