#!/usr/bin/env bash
# ARCH-1 P4: publishes SdlProbe (SDL3 + .NET 10 browser-wasm) with the pinned web toolchain of web/setup.sh.
# Usage: build-probe.sh <SDL3.a> <out-dir> [extra dotnet publish arguments, e.g. -p:ProbeSymbols=true]
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
lib=${1:?path to SDL3.a from build-sdl3-wasm.sh}; out=${2:?output dir}; shift 2
tools=${MICROENGINE_WEB_TOOLS:-$HOME/.microengine/web-tools}
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) exe=.exe; native() { cygpath -w "$1"; } ;; *) exe=; native() { echo "$1"; } ;; esac
export DOTNET_ROOT="$tools/dotnet" DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
"$tools/dotnet/dotnet$exe" publish "$(native "$here/SdlProbe/SdlProbe.csproj")" -c Release -nologo \
  -p:Sdl3WasmLib="$(native "$lib")" -o "$(native "$out")" "$@"
