#!/usr/bin/env bash
# ARCH-1 P4: builds SDL3 as a static library (SDL3.a) with the Emscripten that ships in the .NET 10 wasm-tools workload,
# so it can be linked into a .NET browser-wasm app through NativeFileReference.
#
# The SDL sources are the commit the repository's ppy.SDL3-CS binding was built from (its SDL3.dll reports
# "SDL-3.5.0-ea7a2da"), downloaded as a tarball, so the C# bindings and the native library agree.
#
# Usage: build-sdl3-wasm.sh <dotnet-root-with-wasm-tools> <work-dir>
#   <dotnet-root-with-wasm-tools>  e.g. ~/.microengine/web-tools/dotnet (web/setup.sh installs it)
#   <work-dir>                     scratch folder outside the repository; SDL3.a ends up in <work-dir>/out/SDL3.a
# Needs: bash, curl, tar, cmake (3.16+) and ninja on PATH (or NINJA=<path to ninja>).
set -euo pipefail
DOTNET_ROOT_DIR=${1:?dotnet root with the wasm-tools workload}
WORK=${2:?work dir}
SDL_COMMIT=${SDL_COMMIT:-ea7a2da}
NINJA=${NINJA:-$(command -v ninja || true)}
[ -n "$NINJA" ] || { echo "error: ninja not found (pip install ninja, then NINJA=<path>)" >&2; exit 1; }

# The same emscripten environment as web/build-raylib-wasm.sh: the workload's packs, its frozen cache.
PACKS="$DOTNET_ROOT_DIR/packs"
pack() { ls -d "$PACKS"/Microsoft.NET.Runtime.Emscripten.*."$1".*/*/tools 2>/dev/null | head -1 || true; }
SDK=$(pack Sdk); PY=$(pack Python); NODE=$(pack Node); CACHE=$(pack Cache)
[ -n "$SDK" ] && [ -n "$NODE" ] && [ -n "$CACHE" ] || { echo "error: no emscripten packs under $PACKS" >&2; exit 1; }
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) EXE=.exe; BAT=.bat ;; *) EXE=; BAT= ;; esac
export DOTNET_EMSCRIPTEN_LLVM_ROOT="$SDK/bin" DOTNET_EMSCRIPTEN_BINARYEN_ROOT="$SDK" DOTNET_EMSCRIPTEN_NODE_JS="$NODE/bin/node$EXE"
export EM_CACHE="$CACHE/emscripten/cache" EM_FROZEN_CACHE=1 PYTHONUTF8=1
if [ -n "$PY" ]; then export EMSDK_PYTHON="$PY/python$EXE" PYTHONPATH="$PY" PYTHONHOME=; fi
export PATH="$SDK/bin:$SDK/emscripten:$PATH"
"emcc$BAT" --version | head -1

mkdir -p "$WORK"
src="$WORK/SDL-$SDL_COMMIT"
if [ ! -f "$src/CMakeLists.txt" ]; then
  curl -fsSL -o "$WORK/sdl.tar.gz" "https://codeload.github.com/libsdl-org/SDL/tar.gz/$SDL_COMMIT"
  mkdir -p "$src" && tar xzf "$WORK/sdl.tar.gz" -C "$src" --strip-components=1
fi
grep -E "#define SDL_(MAJOR|MINOR|MICRO)_VERSION " "$src/include/SDL3/SDL_version.h"

build="$WORK/build"
# Single-threaded like the .NET browser runtime (no -pthread): SDL's Emscripten audio then runs from Web Audio's
# own callback instead of an SDL thread. Only what the MicroEngine SDL backend uses on the web is kept: video
# (WebGL2 through SDL_Renderer's OpenGL ES 2 driver), events, audio, joystick/gamepad (Gamepad API).
cmake -S "$src" -B "$build" -G Ninja -DCMAKE_MAKE_PROGRAM="$NINJA" \
  -DCMAKE_TOOLCHAIN_FILE="$SDK/emscripten/cmake/Modules/Platform/Emscripten.cmake" \
  -DCMAKE_BUILD_TYPE=MinSizeRel \
  -DCMAKE_C_FLAGS="-ffile-prefix-map=$src=SDL" \
  -DSDL_SHARED=OFF -DSDL_STATIC=ON -DSDL_TEST_LIBRARY=OFF -DSDL_TESTS=OFF -DSDL_EXAMPLES=OFF \
  -DSDL_PTHREADS=OFF -DSDL_THREADS=OFF \
  -DSDL_GPU=OFF -DSDL_CAMERA=OFF -DSDL_RENDER_GPU=OFF -DSDL_VULKAN=OFF -DSDL_RENDER_VULKAN=OFF
cmake --build "$build"
mkdir -p "$WORK/out"
# .NET's wasm build maps a P/Invoke module name to the NativeFileReference file name: [LibraryImport("SDL3")] → SDL3.a.
cp "$build/libSDL3.a" "$WORK/out/SDL3.a"
ls -l "$WORK/out/SDL3.a"
echo "built $WORK/out/SDL3.a"
