#!/usr/bin/env bash
# Reproduces the PLATFORM-2 software-rendering experiment (docs/evaluations/platform-2.md, "Software rendering"):
# raylib's windowless configuration, PLATFORM_MEMORY (no window system, no input) with the rlsw software renderer
# (GRAPHICS_API_OPENGL_SOFTWARE), built as a Linux libraylib.so that replaces runtimes/linux-x64/native/libraylib.so
# in a desktop build. Result in raylib 6.0: every game runs and writes its captures, but they are blank, because
# rlReadTexturePixels (rlgl.h) has no software-renderer branch. Not used by any workflow.
#
# Usage: platforms/software/build-raylib.sh <out-dir>
# raylib's source (web/toolchain.env) is cloned once into $MICROENGINE_SOFTWARE_TOOLS (default ~/.microengine/software-tools).
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/../.." && pwd)
# shellcheck source=/dev/null
. <(sed 's/[[:space:]]*#.*$//' "$repo/web/toolchain.env" | grep '=')
out=${1:?output directory}

tools=${MICROENGINE_SOFTWARE_TOOLS:-$HOME/.microengine/software-tools}
src="$tools/raylib-$RAYLIB_TAG"
if [ ! -f "$src/src/platforms/rcore_memory.c" ]; then
  rm -rf "$src" && mkdir -p "$tools"
  git clone -q --depth 1 --branch "$RAYLIB_TAG" "$RAYLIB_REPO" "$src"
fi
[ "$(git -C "$src" rev-parse HEAD)" = "$RAYLIB_COMMIT" ] || { echo "error: $src is not raylib $RAYLIB_COMMIT; delete it to clone again" >&2; exit 1; }

mkdir -p "$out/obj"
for m in rcore rshapes rtextures rtext rmodels raudio; do
  cc -fPIC -c "$src/src/$m.c" -o "$out/obj/$m.o" -O2 -std=gnu99 -D_GNU_SOURCE -DPLATFORM_MEMORY \
    -DGRAPHICS_API_OPENGL_SOFTWARE -I"$src/src" -ffile-prefix-map="$src"=raylib -w
done
cc -shared -o "$out/libraylib.so" "$out"/obj/*.o -lm -lpthread -ldl
rm -rf "$out/obj"
echo "built $out/libraylib.so (raylib $RAYLIB_TAG, PLATFORM_MEMORY, rlsw)"
