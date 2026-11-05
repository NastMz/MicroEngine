#!/usr/bin/env bash
# Builds the demos' web packages: one static folder per game, ready to serve or to embed in an iframe. Each is built by
# `microengine package web` (src/MicroEngine.Cli, docs/guide/packaging.md), as any game made with the SDK, with the
# pinned toolchain of web/setup.sh.
#
# Usage: web/build.sh [--dev] [--out DIR] [--backend sdl|raylib] [Game ...]
#   Game       demo ids (games/<Id>/<Id>.csproj: MeadowRun, CourtyardKey, TrailDuel, NeonRift); none means all of them
#   --dev      development package: ships the game's input scripts and honours URL parameters (web/README.md)
#   --out      where packages go (default dist/web); each game ends up in DIR/<Game>/
#   --backend  the platform the page runs on (default: $MICROENGINE_WEB_BACKEND, else the tool's default)
# Needs the toolchain from web/setup.sh (same MICROENGINE_WEB_TOOLS folder).
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
root=$(dirname "$here")
# shellcheck source=toolchain.env
source "$here/toolchain.env"
tools=${MICROENGINE_WEB_TOOLS:-$HOME/.microengine/web-tools}
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) windows=1; exe=.exe ;; *) windows=0; exe= ;; esac
native() { if [ "$windows" = 1 ]; then cygpath -w "$1"; else echo "$1"; fi; }
die() { echo "error: $*" >&2; exit 1; }

dev=false; out="$root/dist/web"; games=(); backend=${MICROENGINE_WEB_BACKEND:-}
while [ $# -gt 0 ]; do
  case "$1" in
    --dev) dev=true ;;
    --out) out=${2:?--out needs a directory}; shift ;;
    --backend) backend=${2:?--backend needs sdl or raylib}; shift ;;
    -h|--help) sed -n '2,9p' "$0"; exit 0 ;;
    -*) die "unknown option $1 (see web/build.sh --help)" ;;
    *) games+=("$1") ;;
  esac
  shift
done
mapfile -t known < <(cd "$root/games" && for d in */; do d=${d%/}; [ -f "$d/$d.csproj" ] && echo "$d"; done)
[ ${#games[@]} -gt 0 ] || games=("${known[@]}")
for g in "${games[@]}"; do
  printf '%s\n' "${known[@]}" | grep -qx "$g" || die "unknown game '$g'. Known games: ${known[*]} (games/<Id>/<Id>.csproj)."
done

case "$backend" in ''|sdl|raylib) ;; *) die "unknown backend '$backend' (sdl or raylib)." ;; esac
dotnet="$tools/dotnet/dotnet$exe"
lib="$tools/raylib-web/raylib.a"
sdl="$tools/sdl-web/browser-wasm/SDL3.a"
[ -x "$dotnet" ] || die "no .NET SDK in $tools/dotnet. Run web/setup.sh first."
"$dotnet" --list-sdks | grep -q "^$DOTNET_SDK_VERSION " || die "the SDK in $tools/dotnet is not $DOTNET_SDK_VERSION. Run web/setup.sh."
ls -d "$tools"/dotnet/packs/Microsoft.NET.Runtime.Emscripten."$EMSCRIPTEN_VERSION".Sdk.* >/dev/null 2>&1 \
  || die "the wasm-tools workload (emscripten $EMSCRIPTEN_VERSION) is not installed in $tools/dotnet. Run web/setup.sh."
[ -f "$lib" ] || die "raylib for the web is missing ($lib). Run web/setup.sh."
[ -f "$sdl" ] || die "SDL for the web is missing ($sdl). Run web/setup.sh."
export DOTNET_ROOT="$tools/dotnet" DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
# Packages are published: keep build-machine paths out of them. Native code the workload compiles embeds source
# file names in assertion messages, and assemblies with debug information name their .pdb file.
# (On Windows emcc is a native program: it gets the C:/ form, which also matches C:\ paths.)
if [ "$windows" = 1 ]; then export EMCC_CFLAGS="-ffile-prefix-map=$(cygpath -m "$tools")=/web-tools"
else export EMCC_CFLAGS="-ffile-prefix-map=$tools=/web-tools"; fi

# Bytes of the files a browser downloads, and of the same files when the server sends the .br copies.
sizes() {
  local raw=0 br=0 f n
  while IFS= read -r -d '' f; do
    n=$(wc -c < "$f"); raw=$((raw + n))
    if [ -f "$f.br" ]; then br=$((br + $(wc -c < "$f.br"))); else br=$((br + n)); fi
  done < <(find "$1" -type f ! -name '*.br' ! -name '*.gz' -print0)
  awk -v r="$raw" -v b="$br" 'BEGIN { printf "%.1f MB, %.1f MB with Brotli", r / 1048576, b / 1048576 }'
}

# The tool itself, built and run with the pinned SDK, which it also uses for the games (MICROENGINE_DOTNET).
"$dotnet" build "$(native "$root/src/MicroEngine.Cli/MicroEngine.Cli.csproj")" -c Release -nologo -v:quiet
tool="$root/src/MicroEngine.Cli/bin/Release/net10.0/MicroEngine.Cli.dll"
export MICROENGINE_DOTNET="$dotnet"
dev_flag=(); [ "$dev" = true ] && dev_flag=(--dev)
backend_flag=(); [ -n "$backend" ] && backend_flag=(--web-backend "$backend")
staging="$here/obj/packages"
mkdir -p "$out"
for g in "${games[@]}"; do
  echo "== $g"
  rm -rf "$staging"
  # The package carries its licences: the game's own notice (games/LICENSE: the demos are not MIT), MicroEngine's
  # MIT licence and third-party list, and the licence files of the .NET runtime, Emscripten and the platform inside it.
  "$dotnet" "$(native "$tool")" package web --project "$(native "$root/games/$g/$g.csproj")" --out "$(native "$staging")" \
    --raylib-wasm "$(native "$lib")" --sdl-wasm "$(native "$sdl")" "${backend_flag[@]}" "${dev_flag[@]}"
  # Empty the folder rather than deleting it, so a local server serving it keeps working.
  mkdir -p "$out/$g"
  find "${out:?}/$g" -mindepth 1 -delete
  cp -R "$staging"/"$g"-*-web/. "$out/$g/"
  echo "$out/$g: $(sizes "$out/$g")"
done
rm -rf "$staging"
echo "Serve locally with: node web/serve.mjs $out"
