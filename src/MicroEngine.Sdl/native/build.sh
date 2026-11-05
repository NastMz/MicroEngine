#!/usr/bin/env bash
# Builds SDL 3 as the static WebAssembly library the browser host of MicroEngine.Sdl links (SDL3.a), from the SDL commit
# sdl.env pins, with the Emscripten of the .NET wasm-tools workload (the version .NET links browser builds with; a
# newer one produces objects its wasm-opt rejects). Desktop and mobile runtimes get SDL from the ppy.SDL3-CS package.
#
#   build.sh browser-wasm [--out <dir>] [--work <dir>]
#       builds and writes <out>/browser-wasm/: SDL3.a, LICENSE.txt (SDL's licence), SHA256SUMS and build.txt (the cache
#       key, the SDL commit and the toolchain), after checking that the archive defines the functions the engine calls
#   build.sh key browser-wasm
#       prints the cache key of that build: a hash of every input (sdl.env, this script) and of the toolchain that
#       compiles it, so CI rebuilds exactly when one of them changes
#
# <out>: default artifacts/sdl in the repository (ignored by Git); web/setup.sh builds into its toolchain folder.
# <work>: SDL sources and build tree; default $MICROENGINE_SDL_WORK or ~/.microengine/sdl-work.
# Needs: bash, git, CMake >= 3.16 and the web toolchain of web/setup.sh (MICROENGINE_WEB_TOOLS). Ninja is used when it
# is found (on Windows also Visual Studio's copy, and it is required there); elsewhere Unix Makefiles.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/../../.." && pwd)
# sdl.env may be checked out with CRLF line ends on Windows; not `source <(...)`: macOS's bash 3.2 reads nothing from it.
eval "$(tr -d '\r' < "$here/sdl.env")"
die() { echo "error: $*" >&2; exit 1; }
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) win=1 ;; *) win=0 ;; esac
w() { if [ $win = 1 ]; then cygpath -m "$1"; else echo "$1"; fi; }
sha256() { if command -v sha256sum >/dev/null 2>&1; then sha256sum "$@"; else shasum -a 256 "$@"; fi; }
if [ -z "${CMAKE:-}" ]; then
  if [ $win = 1 ] && [ -x "/c/Program Files/CMake/bin/cmake.exe" ]; then CMAKE="/c/Program Files/CMake/bin/cmake.exe"; else CMAKE=cmake; fi
fi
if [ -z "${NINJA:-}" ]; then
  NINJA=
  if command -v ninja >/dev/null 2>&1; then NINJA=$(command -v ninja)
  elif [ $win = 1 ]; then
    for vs in "/c/Program Files/Microsoft Visual Studio"/*/*/Common7/IDE/CommonExtensions/Microsoft/CMake/Ninja/ninja.exe; do
      [ -x "$vs" ] && NINJA=$vs
    done
  fi
fi
tools=${MICROENGINE_WEB_TOOLS:-$HOME/.microengine/web-tools}
emsdk_pack() { ls -d "$tools"/dotnet/packs/Microsoft.NET.Runtime.Emscripten.*."$1".*/*/tools 2>/dev/null | head -1 || true; }

# The compiler (and its version) the library is built with: part of the cache key.
toolchain() {
  local sdk; sdk=$(emsdk_pack Sdk); [ -n "$sdk" ] || die "no emscripten under $tools/dotnet/packs: run web/setup.sh first."
  echo "emscripten: $(basename "$(dirname "$(dirname "$sdk")")") $(basename "$(dirname "$sdk")")"
  echo "cmake: $("$CMAKE" --version | head -1)"
}
inputs_hash() { (cd "$here" && cat sdl.env build.sh) | tr -d '\r' | sha256 | cut -c1-16; }
key() { echo "sdl-browser-wasm-${SDL_COMMIT:0:12}-$(inputs_hash)-$(toolchain | sha256 | cut -c1-12)"; }

fetch() {
  local src=$1
  if [ "$(git -C "$src" rev-parse HEAD 2>/dev/null || true)" = "$SDL_COMMIT" ]; then return; fi
  rm -rf "$src"
  mkdir -p "$src"
  git -C "$src" init --quiet
  git -C "$src" fetch --quiet --depth 1 "$SDL_REPO" "$SDL_COMMIT"
  git -C "$src" -c advice.detachedHead=false checkout --quiet FETCH_HEAD
  [ "$(git -C "$src" rev-parse HEAD)" = "$SDL_COMMIT" ] || die "SDL fetch did not produce commit $SDL_COMMIT."
}

[ $# -ge 1 ] || { sed -n '2,19p' "$0"; exit 1; }
if [ "$1" = key ]; then
  [ "${2:-}" = browser-wasm ] || die "only browser-wasm is built here (other runtimes come from ppy.SDL3-CS)."
  key; exit 0
fi
rid=$1; shift
[ "$rid" = browser-wasm ] || die "only browser-wasm is built here (other runtimes come from ppy.SDL3-CS)."
out="$repo/artifacts/sdl"; work=${MICROENGINE_SDL_WORK:-$HOME/.microengine/sdl-work}
while [ $# -gt 0 ]; do
  case "$1" in
    --out) out=$2; shift 2 ;;
    --work) work=$2; shift 2 ;;
    *) die "unknown option $1" ;;
  esac
done
toolchain > /dev/null
build_key=$(key)
mkdir -p "$work"
src="$work/sdl-${SDL_COMMIT:0:12}"
build="$work/build-$rid"
dest="$out/$rid"
fetch "$src"
rm -rf "$build" "$dest"
mkdir -p "$dest"

# The emscripten environment of web/build-raylib-wasm.sh: the packs of the .NET wasm-tools workload, its frozen cache.
SDK=$(emsdk_pack Sdk); PY=$(emsdk_pack Python); NODE=$(emsdk_pack Node); CACHE=$(emsdk_pack Cache)
[ -n "$NODE" ] && [ -n "$CACHE" ] || die "incomplete emscripten packs under $tools/dotnet/packs: run web/setup.sh."
if [ $win = 1 ]; then EXE=.exe; else EXE=; fi
export DOTNET_EMSCRIPTEN_LLVM_ROOT="$SDK/bin" DOTNET_EMSCRIPTEN_BINARYEN_ROOT="$SDK" DOTNET_EMSCRIPTEN_NODE_JS="$NODE/bin/node$EXE"
export EM_CACHE="$CACHE/emscripten/cache" EM_FROZEN_CACHE=1 PYTHONUTF8=1
if [ -n "$PY" ]; then export EMSDK_PYTHON="$PY/python$EXE" PYTHONPATH="$PY" PYTHONHOME=; fi
export PATH="$SDK/bin:$SDK/emscripten:$PATH"
generator=()
if [ -n "$NINJA" ]; then generator=(-G Ninja -DCMAKE_MAKE_PROGRAM="$(w "$NINJA")")
elif [ $win = 1 ]; then die "Ninja is needed on Windows (NINJA=<path>, or Visual Studio's CMake tools)."
else generator=(-G "Unix Makefiles"); fi
# Single-threaded like the .NET browser runtime (no -pthread): SDL's Emscripten audio then runs from Web Audio's own
# callback, not an SDL thread. Only what the engine uses on the web is kept: video (WebGL through SDL_Renderer's OpenGL
# ES 2 driver), events, keyboard, mouse, touch, joystick and gamepad (the Gamepad API) and audio. No build-machine path
# is compiled in (-ffile-prefix-map): assertion messages embed source file names.
"$CMAKE" -S "$(w "$src")" -B "$(w "$build")" "${generator[@]}" \
  -DCMAKE_TOOLCHAIN_FILE="$(w "$SDK/emscripten/cmake/Modules/Platform/Emscripten.cmake")" \
  -DCMAKE_BUILD_TYPE=MinSizeRel -DCMAKE_C_FLAGS="-ffile-prefix-map=$(w "$src")=SDL" \
  -DSDL_SHARED=OFF -DSDL_STATIC=ON -DSDL_TEST_LIBRARY=OFF -DSDL_TESTS=OFF -DSDL_EXAMPLES=OFF -DSDL_INSTALL=OFF \
  -DSDL_PTHREADS=OFF -DSDL_THREADS=OFF \
  -DSDL_GPU=OFF -DSDL_RENDER_GPU=OFF -DSDL_CAMERA=OFF -DSDL_VULKAN=OFF -DSDL_RENDER_VULKAN=OFF
"$CMAKE" --build "$(w "$build")"
# .NET's browser build binds [LibraryImport("SDL3")] to the NativeFileReference whose file is named SDL3.a.
cp "$build/libSDL3.a" "$dest/SDL3.a"
cp "$src/LICENSE.txt" "$dest/LICENSE.txt"

# The archive must define what MicroEngine.Sdl calls in a browser; a configuration that dropped a subsystem fails here,
# not in a player's page.
nm="$SDK/bin/llvm-nm$EXE"
symbols=$("$nm" --defined-only "$dest/SDL3.a" 2>/dev/null | awk '{ print $NF }' | sort -u)
missing=()
for f in SDL_Init SDL_CreateWindow SDL_CreateRenderer SDL_RenderGeometry SDL_PollEvent SDL_AddEventWatch \
         SDL_OpenAudioDevice SDL_OpenGamepad SDL_GetGamepads SDL_GetVersion; do
  grep -qx "$f" <<< "$symbols" || missing+=("$f")
done
[ ${#missing[@]} -eq 0 ] || die "SDL3.a lacks ${missing[*]}; the CMake options dropped a subsystem the engine uses."
version=$(grep -E "#define SDL_(MAJOR|MINOR|MICRO)_VERSION " "$src/include/SDL3/SDL_version.h" | awk '{ print $3 }' | paste -sd. -)
[ "$version" = "$SDL_VERSION" ] || die "the sources are SDL $version, not $SDL_VERSION (sdl.env)."

(cd "$dest" && sha256 SDL3.a LICENSE.txt > SHA256SUMS)
{
  echo "key: $build_key"
  echo "sdl: $SDL_VERSION $SDL_COMMIT"
  toolchain
  echo "size: $(wc -c < "$dest/SDL3.a") bytes"
} > "$dest/build.txt"
cat "$dest/build.txt"
echo "built $dest/SDL3.a"
