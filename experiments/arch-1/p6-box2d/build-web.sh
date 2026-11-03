#!/usr/bin/env bash
# ARCH-1 P6: publishes the browser harness with the pinned web toolchain of web/setup.sh (.NET 10 + wasm-tools,
# Emscripten 3.1.56).
# Usage: build-web.sh <box2d-wasm-dir> <b2net-project> <out-dir> [extra dotnet publish arguments, e.g. -p:P6Aot=true]
#   <box2d-wasm-dir>  build-native.sh wasm output (me_box2d.a, box2d.a)
#   <b2net-project>   Box2D.NET/src/Box2D.NET/Box2D.NET.csproj (fetch-sources.sh)
# bin/obj go to <out-dir>/artifacts, never into the repository.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
lib=${1:?wasm lib dir}; b2net=${2:?Box2D.NET project}; out=${3:?output dir}; shift 3
tools=${MICROENGINE_WEB_TOOLS:-$HOME/.microengine/web-tools}
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) exe=.exe; w() { cygpath -w "$1"; } ;; *) exe=; w() { echo "$1"; } ;; esac
export DOTNET_ROOT="$tools/dotnet" DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
mkdir -p "$out"
"$tools/dotnet/dotnet$exe" publish "$(w "$here/Web/Web.csproj")" -c Release -nologo -v q \
  --artifacts-path "$(w "$out/artifacts")" -p:Box2DWasmDir="$(w "$lib")" -p:B2NetProject="$(w "$b2net")" \
  -o "$(w "$out/site")" "$@"
ls -l "$out/site/wwwroot/_framework" | grep -E 'dotnet.native.*wasm$' || true
