#!/usr/bin/env bash
# Builds MicroEngine web packages: one static folder per game, ready to serve or to embed in an iframe.
#
# Usage: web/build.sh [--dev] [--out DIR] [Game ...]
#   Game     ids from web/games.props (MeadowRun, CourtyardKey, TrailDuel, NeonRift); none means all of them
#   --dev    development package: ships the game's input scripts and honours URL parameters (web/README.md)
#   --out    where packages go (default dist/web); each game ends up in DIR/<Game>/
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

dev=false; out="$root/dist/web"; games=()
while [ $# -gt 0 ]; do
  case "$1" in
    --dev) dev=true ;;
    --out) out=${2:?--out needs a directory}; shift ;;
    -h|--help) sed -n '2,8p' "$0"; exit 0 ;;
    -*) die "unknown option $1 (see web/build.sh --help)" ;;
    *) games+=("$1") ;;
  esac
  shift
done
mapfile -t known < <(sed -n "s/.*'\$(Game)' == '\([A-Za-z0-9_]*\)'.*/\1/p" "$here/games.props")
[ ${#games[@]} -gt 0 ] || games=("${known[@]}")
for g in "${games[@]}"; do
  printf '%s\n' "${known[@]}" | grep -qx "$g" || die "unknown game '$g'. Known games: ${known[*]} (web/games.props)."
done

dotnet="$tools/dotnet/dotnet$exe"
lib="$tools/raylib-web/raylib.a"
[ -x "$dotnet" ] || die "no .NET SDK in $tools/dotnet. Run web/setup.sh first."
"$dotnet" --list-sdks | grep -q "^$DOTNET_SDK_VERSION " || die "the SDK in $tools/dotnet is not $DOTNET_SDK_VERSION. Run web/setup.sh."
ls -d "$tools"/dotnet/packs/Microsoft.NET.Runtime.Emscripten."$EMSCRIPTEN_VERSION".Sdk.* >/dev/null 2>&1 \
  || die "the wasm-tools workload (emscripten $EMSCRIPTEN_VERSION) is not installed in $tools/dotnet. Run web/setup.sh."
[ -f "$lib" ] || die "raylib for the web is missing ($lib). Run web/setup.sh."
export DOTNET_ROOT="$tools/dotnet" DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# Bytes of the files a browser downloads, and of the same files when the server sends the .br copies.
sizes() {
  local raw=0 br=0 f n
  while IFS= read -r -d '' f; do
    n=$(wc -c < "$f"); raw=$((raw + n))
    if [ -f "$f.br" ]; then br=$((br + $(wc -c < "$f.br"))); else br=$((br + n)); fi
  done < <(find "$1" -type f ! -name '*.br' ! -name '*.gz' -print0)
  awk -v r="$raw" -v b="$br" 'BEGIN { printf "%.1f MB, %.1f MB with Brotli", r / 1048576, b / 1048576 }'
}

mkdir -p "$out"
for g in "${games[@]}"; do
  echo "== $g"
  staging="$here/obj/$g/package"
  rm -rf "$staging"
  "$dotnet" publish "$(native "$here/Host/MicroEngine.Web.csproj")" -c Release -nologo -v:quiet \
    -p:Game="$g" -p:WebDevHooks="$dev" -p:RaylibWasmLib="$(native "$lib")" -o "$(native "$staging")"
  # Empty the folder rather than deleting it, so a local server serving it keeps working.
  mkdir -p "$out/$g"
  find "${out:?}/$g" -mindepth 1 -delete
  cp -R "$staging/wwwroot/." "$out/$g/"
  echo "$out/$g: $(sizes "$out/$g")"
done
echo "Serve locally with: node web/serve.mjs $out"
