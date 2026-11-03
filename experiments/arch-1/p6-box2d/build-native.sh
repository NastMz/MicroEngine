#!/usr/bin/env bash
# ARCH-1 P6: builds Box2D v3 + the flat me_box2d layer (native/) for one target.
#
# Usage: build-native.sh <box2d-source> <work-dir> <target> [origin-api ON|OFF]
#   <box2d-source>  a Box2D v3 source tree (fetch-sources.sh downloads v3.2.0 and f2086ed6)
#   <work-dir>      scratch folder outside the repository; the result is copied to <work-dir>/out/<target>/
#   <target>        windows-x64 | android-arm64 | android-x64 | wasm
#   origin-api      ON for Box2D >= v3.2.0 (default), OFF for sources before upstream PR #1066 (f2086ed6)
# Needs: CMake >= 3.22 with the "Visual Studio 18 2026" generator (windows), Ninja (android, wasm: NINJA=<path>),
# the Android NDK (ANDROID_NDK, default: the newest under %LOCALAPPDATA%/Android/Sdk/ndk), and for wasm the
# Emscripten 3.1.56 packs of the .NET wasm-tools workload (MICROENGINE_WEB_TOOLS, default ~/.microengine/web-tools,
# installed by web/setup.sh).
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
SRC=${1:?box2d source}; WORK=${2:?work dir}; TARGET=${3:?target}; ORIGIN=${4:-ON}
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) win=1 ;; *) win=0 ;; esac
vs="/c/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/CMake"
if [ -z "${CMAKE:-}" ]; then
  if [ -x "/c/Program Files/CMake/bin/cmake.exe" ]; then CMAKE="/c/Program Files/CMake/bin/cmake.exe"; else CMAKE=cmake; fi
fi
if [ -z "${NINJA:-}" ]; then
  if [ -x "$vs/Ninja/ninja.exe" ]; then NINJA="$vs/Ninja/ninja.exe"; else NINJA=$(command -v ninja || true); fi
fi
build="$WORK/build-$TARGET-$(basename "$SRC")"
out="$WORK/out/$TARGET-$(basename "$SRC")"
mkdir -p "$out"
log="$out.log"
echo "building $TARGET from $SRC (log: $log)"
exec 3>&1 >"$log" 2>&1
trap 'rc=$?; [ $rc = 0 ] || { tail -40 "$log" >&3; echo "FAILED ($rc), see $log" >&3; }' EXIT
w() { if [ $win = 1 ]; then cygpath -m "$1"; else echo "$1"; fi; }
common=(-S "$(w "$here/native")" -B "$(w "$build")" -DBOX2D_SOURCE="$(w "$SRC")" -DME_ORIGIN_API="$ORIGIN"
  -DBOX2D_VALIDATE=OFF -DBOX2D_PROFILE=OFF)

case "$TARGET" in
  windows-x64)
    "$CMAKE" "${common[@]}" -G "Visual Studio 18 2026" -A x64
    "$CMAKE" --build "$build" --config Release
    cp "$build/Release/me_box2d.dll" "$out/" ;;
  android-arm64|android-x64)
    ndk=${ANDROID_NDK:-$(ls -d "$LOCALAPPDATA/Android/Sdk/ndk/"* | sort | tail -1)}
    abi=${TARGET#android-}; [ "$abi" = x64 ] && abi=x86_64; [ "$abi" = arm64 ] && abi=arm64-v8a
    "$CMAKE" "${common[@]}" -G Ninja -DCMAKE_MAKE_PROGRAM="$(w "$NINJA")" -DCMAKE_BUILD_TYPE=Release \
      -DCMAKE_TOOLCHAIN_FILE="$(w "$ndk/build/cmake/android.toolchain.cmake")" -DANDROID_ABI="$abi" \
      -DANDROID_PLATFORM=android-24
    "$CMAKE" --build "$build"
    cp "$build/libme_box2d.so" "$out/" ;;
  wasm)
    # The emscripten environment of web/build-raylib-wasm.sh and P4's build-sdl3-wasm.sh: the workload's packs.
    tools=${MICROENGINE_WEB_TOOLS:-$HOME/.microengine/web-tools}
    PACKS="$tools/dotnet/packs"
    pack() { ls -d "$PACKS"/Microsoft.NET.Runtime.Emscripten.*."$1".*/*/tools 2>/dev/null | head -1 || true; }
    SDK=$(pack Sdk); PY=$(pack Python); NODE=$(pack Node); CACHE=$(pack Cache)
    [ -n "$SDK" ] || { echo "error: no emscripten packs under $PACKS" >&2; exit 1; }
    if [ $win = 1 ]; then EXE=.exe; else EXE=; fi
    export DOTNET_EMSCRIPTEN_LLVM_ROOT="$SDK/bin" DOTNET_EMSCRIPTEN_BINARYEN_ROOT="$SDK" DOTNET_EMSCRIPTEN_NODE_JS="$NODE/bin/node$EXE"
    export EM_CACHE="$CACHE/emscripten/cache" EM_FROZEN_CACHE=1 PYTHONUTF8=1
    if [ -n "$PY" ]; then export EMSDK_PYTHON="$PY/python$EXE" PYTHONPATH="$PY" PYTHONHOME=; fi
    export PATH="$SDK/bin:$SDK/emscripten:$PATH"
    # Release (-O3), single-threaded like the .NET browser runtime; Box2D adds -msimd128 -msse2 for Emscripten.
    "$CMAKE" "${common[@]}" -G Ninja -DCMAKE_MAKE_PROGRAM="$(w "$NINJA")" -DCMAKE_BUILD_TYPE=Release \
      -DCMAKE_TOOLCHAIN_FILE="$(w "$SDK/emscripten/cmake/Modules/Platform/Emscripten.cmake")"
    "$CMAKE" --build "$build" --target me_box2d box2d
    # .NET's wasm build binds [DllImport("me_box2d")] to a NativeFileReference named me_box2d.a.
    cp "$build/libme_box2d.a" "$out/me_box2d.a"
    cp "$(find "$build" -name 'libbox2d.a' | head -1)" "$out/box2d.a" ;;
  *) echo "unknown target $TARGET" >&2; exit 1 ;;
esac
ls -l "$out" >&3
