#!/usr/bin/env bash
# Prepares the pinned web toolchain (web/toolchain.env) in a private folder, without touching the system .NET:
#   1. the .NET SDK, installed with Microsoft's dotnet-install script;
#   2. the wasm-tools workload (which brings emscripten) into that SDK;
#   3. raylib's sources at the pinned commit, compiled to raylib.a with that emscripten.
# Each step is skipped when its result is already in place, so running it again is cheap.
#
# Usage: web/setup.sh            (tools go to $MICROENGINE_WEB_TOOLS, default ~/.microengine/web-tools)
# Needs: bash, curl, git; on Windows Git Bash plus PowerShell (for dotnet-install.ps1).
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
# shellcheck source=toolchain.env
source "$here/toolchain.env"
tools=${MICROENGINE_WEB_TOOLS:-$HOME/.microengine/web-tools}
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) windows=1; exe=.exe ;; *) windows=0; exe= ;; esac
dotnet="$tools/dotnet/dotnet$exe"

die() { echo "error: $*" >&2; exit 1; }
need() { command -v "$1" >/dev/null 2>&1 || die "'$1' is required ($2)."; }
need curl "to download the .NET SDK"
need git "to fetch raylib"
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

echo "== raylib $RAYLIB_TAG ($RAYLIB_COMMIT)"
src="$tools/raylib-src"
if [ "$(git -C "$src" rev-parse HEAD 2>/dev/null)" != "$RAYLIB_COMMIT" ]; then
  rm -rf "$src"
  git -c advice.detachedHead=false clone --quiet --depth 1 --branch "$RAYLIB_TAG" "$RAYLIB_REPO" "$src"
  [ "$(git -C "$src" rev-parse HEAD)" = "$RAYLIB_COMMIT" ] || die "raylib tag $RAYLIB_TAG is not commit $RAYLIB_COMMIT any more."
else
  echo "sources present"
fi

lib="$tools/raylib-web/raylib.a"
stamp="$RAYLIB_COMMIT emscripten-$EMSCRIPTEN_VERSION $(cksum < "$here/build-raylib-wasm.sh" | cut -d" " -f1)"
if [ -f "$lib" ] && [ "$(cat "$tools/raylib-web/stamp" 2>/dev/null)" = "$stamp" ]; then
  echo "raylib.a up to date"
else
  bash "$here/build-raylib-wasm.sh" "$tools/dotnet" "$src" "$tools/raylib-web"
  echo "$stamp" > "$tools/raylib-web/stamp"
fi

echo "Toolchain ready in $tools. Build with web/build.sh."
