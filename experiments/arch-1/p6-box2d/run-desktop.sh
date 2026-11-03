#!/usr/bin/env bash
# ARCH-1 P6: builds the desktop harness into a scratch folder and runs one command.
# Usage: run-desktop.sh <work-dir> <native-lib|-> <b2net-project|-> <p6 arguments...>
#   <native-lib>     me_box2d.dll / libme_box2d.so from build-native.sh ("-" for none)
#   <b2net-project>  Box2D.NET/src/Box2D.NET/Box2D.NET.csproj from fetch-sources.sh ("-": the NuGet package 3.1.654)
# Build output goes to <work-dir>/artifacts-<tag>, never into the repository.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
WORK=${1:?work dir}; LIB=${2:?native lib or -}; B2NET=${3:?b2net project or -}; shift 3
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) w() { cygpath -w "$1"; } ;; *) w() { echo "$1"; } ;; esac
tag=$( { echo "$LIB"; echo "$B2NET"; } | cksum | cut -d' ' -f1)
art="$WORK/artifacts-$tag"
props=()
[ "$LIB" != - ] && props+=("-p:NativeLib=$(w "$LIB")")
[ "$B2NET" != - ] && props+=("-p:B2NetProject=$(w "$B2NET")")
if [ ! -f "$art/.built" ] || [ "${P6_REBUILD:-0}" = 1 ]; then
  dotnet build "$(w "$here/Desktop/Desktop.csproj")" -c Release -nologo -v q --artifacts-path "$(w "$art")" "${props[@]}" > "$art.build.log" 2>&1 \
    || { tail -30 "$art.build.log"; exit 1; }
  touch "$art/.built"
fi
exe=$(find "$art/bin" -name 'p6.dll' | head -1)
dotnet "$exe" "$@"
