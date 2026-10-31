#!/usr/bin/env bash
# Builds raylib for Android as libraylib.so, one per ABI, with the NDK pinned in platforms/android/toolchain.env and
# the raylib release that Raylib-cs binds (web/toolchain.env). The library holds raylib's Android platform
# (rcore_android.c: android_main, input, EGL), the NDK's native_app_glue and native/microengine_main.c, the main()
# that hands the native-activity thread to the .NET host (GameActivity.RunGame).
#
# Usage: platforms/android/build-raylib.sh [abi ...]      (default: x86_64 arm64-v8a)
# Needs ANDROID_NDK_HOME (or ANDROID_HOME/ndk/<pinned version>). raylib's source is cloned once into
# $MICROENGINE_ANDROID_TOOLS (default ~/.microengine/android-tools); the libraries go to platforms/android/native/out.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/../.." && pwd)
# shellcheck source=/dev/null
. <(sed 's/[[:space:]]*#.*$//' "$repo/web/toolchain.env" "$here/toolchain.env" | grep '=')

sdk=${ANDROID_HOME:-${ANDROID_SDK_ROOT:-}}
ndk=${ANDROID_NDK_HOME:-}
if [ -z "$ndk" ] && [ -n "$sdk" ]; then ndk="$sdk/ndk/$NDK_VERSION"; fi
[ -n "$ndk" ] && [ -d "$ndk" ] || { echo "error: NDK $NDK_VERSION not found: set ANDROID_NDK_HOME, or install it with sdkmanager \"ndk;$NDK_VERSION\"" >&2; exit 1; }
have=$(sed -n 's/^Pkg.Revision *= *//p' "$ndk/source.properties")
[ "$have" = "$NDK_VERSION" ] || echo "warning: NDK at $ndk is $have, pinned $NDK_VERSION" >&2
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) host=windows-x86_64; exe=.exe ;; Darwin) host=darwin-x86_64; exe= ;; *) host=linux-x86_64; exe= ;; esac
llvm="$ndk/toolchains/llvm/prebuilt/$host/bin"

tools=${MICROENGINE_ANDROID_TOOLS:-$HOME/.microengine/android-tools}
src="$tools/raylib-$RAYLIB_TAG"
if [ ! -f "$src/src/platforms/rcore_android.c" ]; then
  rm -rf "$src" && mkdir -p "$tools"
  git clone -q --depth 1 --branch "$RAYLIB_TAG" "$RAYLIB_REPO" "$src"
fi
[ "$(git -C "$src" rev-parse HEAD)" = "$RAYLIB_COMMIT" ] || { echo "error: $src is not raylib $RAYLIB_COMMIT; delete it to clone again" >&2; exit 1; }

glue="$ndk/sources/android/native_app_glue"
abis=("$@"); [ ${#abis[@]} -gt 0 ] || abis=(x86_64 arm64-v8a)
for abi in "${abis[@]}"; do
  case "$abi" in
    x86_64) target=x86_64-linux-android$ANDROID_API; arch=(-march=x86-64) ;;
    arm64-v8a) target=aarch64-linux-android$ANDROID_API; arch=(-mfix-cortex-a53-835769) ;;
    *) echo "error: unsupported ABI '$abi' (x86_64, arm64-v8a)" >&2; exit 2 ;;
  esac
  out="$here/native/out/$abi"; obj="$out/obj"
  rm -rf "$out" && mkdir -p "$obj"
  # Same flags as raylib's own Makefile for PLATFORM_ANDROID; build-machine paths kept out of the library.
  cflags=(--target="$target" "${arch[@]}" -O2 -std=gnu99 -fPIC -ffunction-sections -funwind-tables -fstack-protector-strong
    -Wa,--noexecstack -Wformat -D__ANDROID__ -DPLATFORM_ANDROID -DGRAPHICS_API_OPENGL_ES2
    -I"$src/src" -I"$src/src/external/glfw/include" -I"$glue" -ffile-prefix-map="$src"=raylib -ffile-prefix-map="$ndk"=ndk)
  for m in rcore rshapes rtextures rtext rmodels raudio; do
    "$llvm/clang$exe" "${cflags[@]}" -c "$src/src/$m.c" -o "$obj/$m.o"
  done
  "$llvm/clang$exe" "${cflags[@]}" -c "$glue/android_native_app_glue.c" -o "$obj/android_native_app_glue.o"
  "$llvm/clang$exe" "${cflags[@]}" -Wall -Wextra -Werror -c "$here/native/microengine_main.c" -o "$obj/microengine_main.o"
  # -u ANativeActivity_onCreate keeps the entry point NativeActivity looks up; --wrap=fopen is required by
  # rcore_android.c (asset loading); 16 KB pages for Android 15 devices.
  "$llvm/clang$exe" --target="$target" -shared -o "$out/libraylib.so" "$obj"/*.o \
    -Wl,-soname,libraylib.so -u ANativeActivity_onCreate -Wl,--wrap=fopen -Wl,--no-undefined -Wl,--build-id=sha1 \
    -Wl,-z,noexecstack -Wl,-z,relro -Wl,-z,now -Wl,-z,max-page-size=16384 \
    -landroid -llog -lEGL -lGLESv2 -lOpenSLES -lm -ldl
  "$llvm/llvm-strip$exe" --strip-unneeded "$out/libraylib.so"
  rm -rf "$obj"
  echo "built $out/libraylib.so (raylib $RAYLIB_TAG, NDK $have, API $ANDROID_API)"
done
