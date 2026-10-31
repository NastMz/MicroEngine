#!/usr/bin/env bash
# Rebuilds raylib for the web with the emscripten that ships in the .NET wasm-tools workload.
# Why: the raylib.a inside Raylib-cs 8.1.0 was compiled by a newer emscripten; linking it with
# .NET 10's emscripten 3.1.56 fails in wasm-opt ("Unknown option '--enable-bulk-memory-opt'").
# Normally run by web/setup.sh.
#
# Usage: build-raylib-wasm.sh <dotnet-root-with-wasm-tools> <raylib-src> <output-dir>
set -euo pipefail
DOTNET_ROOT_DIR=${1:?dotnet root}; RAYLIB_SRC=${2:?raylib src}; OUT=${3:?output dir}
PACKS="$DOTNET_ROOT_DIR/packs"
pack() { ls -d "$PACKS"/Microsoft.NET.Runtime.Emscripten.*."$1".*/*/tools 2>/dev/null | head -1; }
SDK=$(pack Sdk); PY=$(pack Python); NODE=$(pack Node); CACHE=$(pack Cache)
[ -n "$SDK" ] && [ -n "$NODE" ] && [ -n "$CACHE" ] || { echo "error: no emscripten packs under $PACKS (is the wasm-tools workload installed?)" >&2; exit 1; }
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) EXE=.exe; BAT=.bat ;; *) EXE=; BAT= ;; esac
export DOTNET_EMSCRIPTEN_LLVM_ROOT="$SDK/bin" DOTNET_EMSCRIPTEN_BINARYEN_ROOT="$SDK" DOTNET_EMSCRIPTEN_NODE_JS="$NODE/bin/node$EXE"
export EM_CACHE="$CACHE/emscripten/cache" EM_FROZEN_CACHE=1 PYTHONUTF8=1
# Windows packs include their own Python; elsewhere emscripten uses the system python3.
if [ -n "$PY" ]; then export EMSDK_PYTHON="$PY/python$EXE" PYTHONPATH="$PY" PYTHONHOME=; fi
export PATH="$SDK/bin:$SDK/emscripten:$PATH"
mkdir -p "$OUT/obj"
for m in rcore rshapes rtextures rtext rmodels raudio; do
  echo "emcc $m.c"
  "emcc$BAT" -c "$RAYLIB_SRC/src/$m.c" -o "$OUT/obj/$m.o" -Os -std=gnu99 -D_DEFAULT_SOURCE \
    -DPLATFORM_WEB -DGRAPHICS_API_OPENGL_ES2 -I"$RAYLIB_SRC/src" -I"$RAYLIB_SRC/src/external/glfw/include"
done
rm -f "$OUT/raylib.a"
"emar$BAT" rcs "$OUT/raylib.a" "$OUT"/obj/*.o
echo "built $OUT/raylib.a"
