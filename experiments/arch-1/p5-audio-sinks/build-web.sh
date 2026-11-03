#!/usr/bin/env bash
# ARCH-1 P5: publishes the web probe (Web/) with the pinned web toolchain of web/setup.sh, linking SDL3.a built by
# ../p4-sdl3-web/build-sdl3-wasm.sh (no threads, Emscripten 3.1.56).
# Usage: build-web.sh <SDL3.a> <out-dir>
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
lib=${1:?path to SDL3.a}; out=${2:?output dir}
tools=${MICROENGINE_WEB_TOOLS:-$HOME/.microengine/web-tools}
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) exe=.exe; native() { cygpath -w "$1"; } ;; *) exe=; native() { echo "$1"; } ;; esac
export DOTNET_ROOT="$tools/dotnet" DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
"$tools/dotnet/dotnet$exe" publish "$(native "$here/Web/P5.Web.csproj")" -c Release -nologo \
  -p:Sdl3WasmLib="$(native "$lib")" -o "$(native "$out")"
