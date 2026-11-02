#!/usr/bin/env bash
# ARCH-1 P4: builds one demo for the browser on SDL3 instead of raylib, from a scratch copy of the repository.
#
#   build-demo.sh <SDL3.a> <work-dir> [Game]        (Game defaults to MeadowRun)
#
# 1. copies the repository's sources (src, games, root build files) into <work-dir>/repo, without bin/obj;
# 2. applies sdl-web.patch to the copy: the web host links SDL3.a instead of raylib.a and runs the game on
#    MicroEngine.Sdl's SdlPlatform, plus the few browser guards SdlPlatform needs (see README.md, "Adaptations");
# 3. runs that copy's `microengine package web` with the pinned web toolchain (web/setup.sh), passing SDL3.a where
#    the tool expects raylib.a (the patched Host.csproj links whatever library it is given).
# The repository itself is never modified. Output: <work-dir>/out/<Game>/ (serve it with web/serve.mjs).
# P4_BACKEND=raylib builds the unpatched copy with raylib.a instead (pass raylib.a as the first argument): the
# baseline, built by the same tool and toolchain.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
root=$(cd "$here/../../.." && pwd)
lib=${1:?path to SDL3.a}; work=${2:?work dir}; game=${3:-MeadowRun}
tools=${MICROENGINE_WEB_TOOLS:-$HOME/.microengine/web-tools}
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) exe=.exe; native() { cygpath -w "$1"; } ;; *) exe=; native() { echo "$1"; } ;; esac
dotnet="$tools/dotnet/dotnet$exe"
copy="$work/repo"

rm -rf "$copy"; mkdir -p "$copy"
(cd "$root" && tar cf - --exclude=bin --exclude=obj --exclude=node_modules \
  src games Directory.Build.props global.json NuGet.config LICENSE THIRD-PARTY-NOTICES.md) | (cd "$copy" && tar xf -)
if [ "${P4_BACKEND:-sdl}" != raylib ]; then
  # The patch has LF line ends; a Windows checkout has CRLF ones.
  for f in $(grep '^+++ b/' "$here/sdl-web.patch" | sed 's|^+++ b/||; s|\t.*||'); do sed -i 's/\r$//' "$copy/$f"; done
  (cd "$copy" && patch -p1 --forward < "$here/sdl-web.patch")
fi

export DOTNET_ROOT="$tools/dotnet" DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
"$dotnet" build "$(native "$copy/src/MicroEngine.Cli/MicroEngine.Cli.csproj")" -c Release -nologo -v:quiet
export MICROENGINE_DOTNET="$dotnet"
rm -rf "$work/staging"
"$dotnet" "$(native "$copy/src/MicroEngine.Cli/bin/Release/net10.0/MicroEngine.Cli.dll")" package web \
  --project "$(native "$copy/games/$game/$game.csproj")" --out "$(native "$work/staging")" --raylib-wasm "$(native "$lib")" "${@:4}"
rm -rf "$work/out/$game"; mkdir -p "$work/out/$game"
cp -R "$work/staging"/"$game"-*-web/. "$work/out/$game/"
echo "built $work/out/$game"
