#!/usr/bin/env bash
# Builds the native library of MicroEngine.Physics.Box2D (Box2D at the commit box2d.env pins + the me_box2d layer) for
# one runtime identifier, checks it, and writes it where the project picks it up.
#
#   build.sh <rid> [--out <dir>] [--work <dir>] [--no-test]
#       builds, writes <out>/<rid>/ (the files of runtimes/<rid>/native, SHA256SUMS, build.txt) and, when this machine
#       can run the result, runs me_box2d_selftest and keeps its output as selftest.txt
#   build.sh key <rid>
#       prints the cache key of that build: the runtime identifier, a hash of every input (box2d.env, the layer, this
#       script) and the toolchain that compiles it, so CI rebuilds exactly when one of them changes
#
# <rid>: win-x64 | linux-x64 | osx-arm64 | osx-x64 | android-arm64 | android-x64 | ios-arm64 | browser-wasm
# <out>: default artifacts/box2d in the repository (ignored by Git), where src/MicroEngine.Physics.Box2D looks.
# <work>: Box2D sources and build trees; default $MICROENGINE_BOX2D_WORK or ~/.microengine/box2d-work.
# Needs: bash, git, CMake >= 3.22, and the compiler of the target: Visual Studio's C compiler (win-x64), gcc or clang
# (linux-x64), Xcode (osx-*, ios-arm64), the Android NDK (android-*: ANDROID_NDK, or the newest under ANDROID_HOME,
# ANDROID_SDK_ROOT or %LOCALAPPDATA%/Android/Sdk), or the web toolchain of web/setup.sh (browser-wasm: emscripten from
# the .NET wasm-tools workload, the version .NET links with). Ninja is used when it is found.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/../../.." && pwd)
# box2d.env may be checked out with CRLF line ends on Windows.
# shellcheck source=box2d.env
eval "$(tr -d '\r' < "$here/box2d.env")" # not `source <(...)`: macOS's bash 3.2 reads nothing from it

die() { echo "error: $*" >&2; exit 1; }
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) win=1 ;; *) win=0 ;; esac
host() {
  case "$(uname -s)-$(uname -m)" in
    MINGW*-x86_64|MSYS*-x86_64|CYGWIN*-x86_64) echo win-x64 ;;
    Linux-x86_64) echo linux-x64 ;;
    Darwin-arm64) echo osx-arm64 ;;
    Darwin-x86_64) echo osx-x64 ;;
    *) echo unknown ;;
  esac
}
w() { if [ $win = 1 ]; then cygpath -m "$1"; else echo "$1"; fi; }
sha256() { if command -v sha256sum >/dev/null 2>&1; then sha256sum "$@"; else shasum -a 256 "$@"; fi; }

if [ -z "${CMAKE:-}" ]; then
  if [ $win = 1 ] && [ -x "/c/Program Files/CMake/bin/cmake.exe" ]; then CMAKE="/c/Program Files/CMake/bin/cmake.exe"; else CMAKE=cmake; fi
fi
if [ -z "${NINJA:-}" ]; then
  vsninja="/c/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/CMake/Ninja/ninja.exe"
  if command -v ninja >/dev/null 2>&1; then NINJA=$(command -v ninja); elif [ -x "$vsninja" ]; then NINJA=$vsninja; else NINJA=; fi
fi

ndk_root() {
  if [ -n "${ANDROID_NDK:-}" ]; then echo "$ANDROID_NDK"; return; fi
  local sdk
  for sdk in "${ANDROID_HOME:-}" "${ANDROID_SDK_ROOT:-}" "${LOCALAPPDATA:-}/Android/Sdk"; do
    [ -n "$sdk" ] && [ -d "$sdk/ndk" ] && { find "$sdk/ndk" -mindepth 1 -maxdepth 1 -type d | sort -V | tail -1; return; }
  done
}

web_packs() {
  local tools=${MICROENGINE_WEB_TOOLS:-$HOME/.microengine/web-tools}
  echo "$tools/dotnet/packs"
}
emsdk_pack() { ls -d "$(web_packs)"/Microsoft.NET.Runtime.Emscripten.*."$1".*/*/tools 2>/dev/null | head -1 || true; }

# The compiler (and its version) a runtime identifier is built with: part of the cache key.
toolchain() {
  local rid=$1
  echo "cmake: $("$CMAKE" --version | head -1)"
  case "$rid" in
    win-x64)
      local vswhere="/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe"
      [ -x "$vswhere" ] || die "Visual Studio is not installed (no vswhere)."
      echo "msvc: $("$vswhere" -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property catalog_productDisplayVersion | tr -d '\r')" ;;
    linux-x64) echo "cc: $(${CC:-cc} --version | head -1)"; echo "libc: $(ldd --version 2>&1 | head -1)" ;;
    osx-*|ios-*) echo "xcode: $(xcodebuild -version 2>/dev/null | tr '\n' ' ')"; echo "cc: $(xcrun clang --version | head -1)" ;;
    android-*)
      local ndk; ndk=$(ndk_root); [ -n "$ndk" ] || die "no Android NDK (set ANDROID_NDK)."
      echo "ndk: $(grep Pkg.Revision "$ndk/source.properties" | tr -d '\r')" ;;
    browser-wasm)
      local sdk; sdk=$(emsdk_pack Sdk); [ -n "$sdk" ] || die "no emscripten under $(web_packs): run web/setup.sh first."
      echo "emscripten: $(basename "$(dirname "$(dirname "$sdk")")") $(basename "$(dirname "$sdk")")" ;;
    *) die "unknown runtime identifier '$rid'." ;;
  esac
}

inputs_hash() {
  (cd "$here" && cat box2d.env CMakeLists.txt me_box2d.h me_box2d.c me_box2d_selftest.c build.sh) | tr -d '\r' | sha256 | cut -c1-16
}

key() {
  local rid=$1 tool
  tool=$(toolchain "$rid" | sha256 | cut -c1-12)
  echo "box2d-$rid-${BOX2D_COMMIT:0:12}-$(inputs_hash)-$tool"
}

fetch() {
  local src=$1
  if [ "$(git -C "$src" rev-parse HEAD 2>/dev/null || true)" = "$BOX2D_COMMIT" ]; then return; fi
  rm -rf "$src"
  mkdir -p "$src"
  git -C "$src" init --quiet
  git -C "$src" fetch --quiet --depth 1 "$BOX2D_REPO" "refs/tags/$BOX2D_TAG"
  git -C "$src" -c advice.detachedHead=false checkout --quiet FETCH_HEAD
  local got; got=$(git -C "$src" rev-parse HEAD)
  [ "$got" = "$BOX2D_COMMIT" ] || die "Box2D tag $BOX2D_TAG is commit $got, not the pinned $BOX2D_COMMIT (box2d.env)."
}

[ $# -ge 1 ] || { sed -n '2,20p' "$0"; exit 1; }
if [ "$1" = key ]; then key "${2:?runtime identifier}"; exit 0; fi

rid=$1; shift
out="$repo/artifacts/box2d"; work=${MICROENGINE_BOX2D_WORK:-$HOME/.microengine/box2d-work}; test=1
while [ $# -gt 0 ]; do
  case "$1" in
    --out) out=$2; shift 2 ;;
    --work) work=$2; shift 2 ;;
    --no-test) test=0; shift ;;
    *) die "unknown option $1" ;;
  esac
done
toolchain "$rid" > /dev/null
mkdir -p "$work"
src="$work/box2d-${BOX2D_COMMIT:0:12}"
build="$work/build-$rid"
dest="$out/$rid"
fetch "$src"
rm -rf "$build" "$dest"
mkdir -p "$dest"

common=(-S "$(w "$here")" -B "$(w "$build")" -DBOX2D_SOURCE="$(w "$src")" -DCMAKE_BUILD_TYPE=Release)
ninja=(); [ -n "$NINJA" ] && ninja=(-G Ninja -DCMAKE_MAKE_PROGRAM="$(w "$NINJA")")
selftest=()
case "$rid" in
  win-x64)
    "$CMAKE" "${common[@]}" -A x64
    "$CMAKE" --build "$build" --config Release
    cp "$build/Release/me_box2d.dll" "$dest/"
    selftest=("$build/Release/me_box2d_selftest.exe") ;;
  linux-x64)
    "$CMAKE" "${common[@]}" "${ninja[@]}"
    "$CMAKE" --build "$build"
    cp "$build/libme_box2d.so" "$dest/"
    selftest=("$build/me_box2d_selftest") ;;
  osx-arm64|osx-x64)
    arch=${rid#osx-}; [ "$arch" = x64 ] && arch=x86_64
    "$CMAKE" "${common[@]}" "${ninja[@]}" -DCMAKE_OSX_ARCHITECTURES="$arch" -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0
    "$CMAKE" --build "$build"
    cp "$build/libme_box2d.dylib" "$dest/"
    selftest=(arch "-$arch" "$build/me_box2d_selftest") ;;
  ios-arm64)
    "$CMAKE" "${common[@]}" "${ninja[@]}" -DCMAKE_SYSTEM_NAME=iOS -DCMAKE_OSX_ARCHITECTURES=arm64 \
      -DCMAKE_OSX_DEPLOYMENT_TARGET=13.0 -DCMAKE_OSX_SYSROOT=iphoneos
    "$CMAKE" --build "$build" --target me_box2d box2d
    cp "$build/libme_box2d.a" "$dest/libme_box2d.a"
    cp "$(find "$build" -name libbox2d.a | head -1)" "$dest/libbox2d.a"
    test=0 ;;
  android-arm64|android-x64)
    ndk=$(ndk_root)
    abi=${rid#android-}; [ "$abi" = x64 ] && abi=x86_64; [ "$abi" = arm64 ] && abi=arm64-v8a
    [ -n "$NINJA" ] || [ $win = 0 ] || die "Ninja is needed for Android builds on Windows (NINJA=<path>)."
    "$CMAKE" "${common[@]}" "${ninja[@]}" -DCMAKE_TOOLCHAIN_FILE="$(w "$ndk/build/cmake/android.toolchain.cmake")" \
      -DANDROID_ABI="$abi" -DANDROID_PLATFORM=android-24 -DANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON
    "$CMAKE" --build "$build"
    cp "$build/libme_box2d.so" "$dest/"
    # The self-test runs on a device or emulator of that ABI: adb push it with the library, run it with
    # LD_LIBRARY_PATH=. (docs/manual/physics-box2d.md); this script does not start one.
    test=0 ;;
  browser-wasm)
    # The emscripten environment of web/build-raylib-wasm.sh: the packs of the .NET wasm-tools workload.
    SDK=$(emsdk_pack Sdk); PY=$(emsdk_pack Python); NODE=$(emsdk_pack Node); CACHE=$(emsdk_pack Cache)
    if [ $win = 1 ]; then EXE=.exe; else EXE=; fi
    export DOTNET_EMSCRIPTEN_LLVM_ROOT="$SDK/bin" DOTNET_EMSCRIPTEN_BINARYEN_ROOT="$SDK" DOTNET_EMSCRIPTEN_NODE_JS="$NODE/bin/node$EXE"
    export EM_CACHE="$CACHE/emscripten/cache" EM_FROZEN_CACHE=1 PYTHONUTF8=1
    if [ -n "$PY" ]; then export EMSDK_PYTHON="$PY/python$EXE" PYTHONPATH="$PY" PYTHONHOME=; fi
    export PATH="$SDK/bin:$SDK/emscripten:$PATH"
    [ -n "$NINJA" ] || [ $win = 0 ] || die "Ninja is needed for the browser build on Windows (NINJA=<path>)."
    # Single-threaded like the .NET browser runtime; Box2D adds -msimd128 -msse2 for Emscripten (wasm SIMD).
    "$CMAKE" "${common[@]}" "${ninja[@]}" -DCMAKE_TOOLCHAIN_FILE="$(w "$SDK/emscripten/cmake/Modules/Platform/Emscripten.cmake")"
    "$CMAKE" --build "$build"
    # .NET's browser build binds [DllImport("me_box2d")] to the NativeFileReference named me_box2d.a.
    cp "$build/libme_box2d.a" "$dest/me_box2d.a"
    cp "$(find "$build" -name libbox2d.a | head -1)" "$dest/box2d.a"
    selftest=("$NODE/bin/node$EXE" "$build/me_box2d_selftest.js") ;;
esac

{
  echo "rid: $rid"
  echo "box2d: $BOX2D_TAG $BOX2D_COMMIT"
  echo "layer: api $(sed -n 's/^#define ME_API_VERSION \([0-9]*\).*/\1/p' "$here/me_box2d.h"), inputs $(inputs_hash)"
  toolchain "$rid"
  echo "key: $(key "$rid")"
} > "$dest/build.txt"
(cd "$dest" && sha256 $(ls | grep -v -e '^build.txt$' -e '^SHA256SUMS$') > SHA256SUMS)

if [ $test = 1 ]; then
  runnable=0
  case "$rid" in
    browser-wasm) runnable=1 ;;
    osx-x64) [ "$(host)" = osx-x64 ] || { [ "$(host)" = osx-arm64 ] && arch -x86_64 /usr/bin/true 2>/dev/null; } && runnable=1 ;;
    *) [ "$rid" = "$(host)" ] && runnable=1 ;;
  esac
  if [ $runnable = 1 ]; then
    "${selftest[@]}" | tee "$dest/selftest.txt"
  else
    echo "not run: $rid cannot run on $(host)" | tee "$dest/selftest.txt"
  fi
else
  echo "not run: built only (the self-test of $rid needs a device)" | tee "$dest/selftest.txt"
fi
echo "built $rid into $dest"
cat "$dest/SHA256SUMS"
