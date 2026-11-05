#!/usr/bin/env bash
# Prepares the pinned web toolchain (web/toolchain.env) in a private folder, without touching the system .NET:
#   1. the .NET SDK, installed with Microsoft's dotnet-install script;
#   2. the wasm-tools workload (which brings emscripten) into that SDK;
#   3. SDL 3 at the commit src/MicroEngine.Sdl/native/sdl.env pins, compiled to SDL3.a with that emscripten
#      (src/MicroEngine.Sdl/native/build.sh, into sdl-web/browser-wasm/; CI builds it in its own cached job).
# Each step is skipped when its result is already in place, so running it again is cheap.
#
# Usage: web/setup.sh [--no-sdl]   (tools go to $MICROENGINE_WEB_TOOLS, default ~/.microengine/web-tools)
#   --no-sdl  skips step 3 (the CI job that builds SDL3.a itself)
# Needs: bash, curl, git; for step 3 also CMake (and Ninja on Windows); on Windows Git Bash plus PowerShell
# (for dotnet-install.ps1).
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
# shellcheck source=toolchain.env
source "$here/toolchain.env"
tools=${MICROENGINE_WEB_TOOLS:-$HOME/.microengine/web-tools}
sdl=true
for arg in "$@"; do
  case "$arg" in
    --no-sdl) sdl=false ;;
    *) echo "error: unknown option $arg (web/setup.sh [--no-sdl])" >&2; exit 2 ;;
  esac
done
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) windows=1; exe=.exe ;; *) windows=0; exe= ;; esac
dotnet="$tools/dotnet/dotnet$exe"

die() { echo "error: $*" >&2; exit 1; }
need() { command -v "$1" >/dev/null 2>&1 || die "'$1' is required ($2)."; }
need curl "to download the .NET SDK"
need git "to fetch SDL"
[ "$windows" = 0 ] || need powershell "to run dotnet-install.ps1"
mkdir -p "$tools"

echo "== .NET SDK $DOTNET_SDK_VERSION in $tools/dotnet"
if [ -x "$dotnet" ] && "$dotnet" --list-sdks | grep -q "^$DOTNET_SDK_VERSION "; then
  echo "already installed"
else
  if [ "$windows" = 1 ]; then
    curl -fsSL -o "$tools/dotnet-install.ps1" https://dot.net/v1/dotnet-install.ps1
    powershell -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$tools/dotnet-install.ps1")" \
      -Version "$DOTNET_SDK_VERSION" -InstallDir "$(cygpath -w "$tools/dotnet")" -NoPath
  else
    curl -fsSL -o "$tools/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh
    bash "$tools/dotnet-install.sh" --version "$DOTNET_SDK_VERSION" --install-dir "$tools/dotnet" --no-path
  fi
  "$dotnet" --list-sdks | grep -q "^$DOTNET_SDK_VERSION " || die "the .NET SDK $DOTNET_SDK_VERSION did not install."
fi

# The private SDK must not see machine-wide workloads or SDKs.
export DOTNET_ROOT="$tools/dotnet" DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
emsdk_pack() { ls -d "$tools"/dotnet/packs/Microsoft.NET.Runtime.Emscripten."$EMSCRIPTEN_VERSION".Sdk.* 2>/dev/null | head -1; }

echo "== wasm-tools workload (set $DOTNET_WORKLOAD_SET, emscripten $EMSCRIPTEN_VERSION)"
if [ -n "$(emsdk_pack)" ] && [ -d "$tools/dotnet/packs/Microsoft.NETCore.App.Runtime.Mono.browser-wasm/$DOTNET_RUNTIME_VERSION" ]; then
  echo "already installed"
else
  (cd "$tools" && "$dotnet" workload install wasm-tools --version "$DOTNET_WORKLOAD_SET")
  [ -n "$(emsdk_pack)" ] || die "the workload did not bring emscripten $EMSCRIPTEN_VERSION; update web/toolchain.env together with the SDK version."
  [ -d "$tools/dotnet/packs/Microsoft.NETCore.App.Runtime.Mono.browser-wasm/$DOTNET_RUNTIME_VERSION" ] \
    || die "the browser-wasm runtime pack $DOTNET_RUNTIME_VERSION is missing; update DOTNET_RUNTIME_VERSION in web/toolchain.env."
fi

if [ "$sdl" = true ]; then
  echo "== SDL for the web"
  sdl_build="$here/../src/MicroEngine.Sdl/native/build.sh"
  sdl_key=$(MICROENGINE_WEB_TOOLS="$tools" bash "$sdl_build" key browser-wasm)
  if [ -f "$tools/sdl-web/browser-wasm/SDL3.a" ] && grep -qx "key: $sdl_key" "$tools/sdl-web/browser-wasm/build.txt" 2>/dev/null; then
    echo "SDL3.a up to date ($sdl_key)"
  else
    MICROENGINE_WEB_TOOLS="$tools" bash "$sdl_build" browser-wasm --out "$tools/sdl-web"
  fi
fi

echo "Toolchain ready in $tools. Build with web/build.sh."
