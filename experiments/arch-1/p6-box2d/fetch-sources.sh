#!/usr/bin/env bash
# ARCH-1 P6: downloads the exact sources the prototype was measured with, as tarballs (no Git needed).
#   box2d-v3.2.0     upstream release v3.2.0 (2026-10-19)
#   box2d-f2086ed6   upstream commit f2086ed6 "clean up UI code (#1062)" (2026-05-29): the newest upstream change
#                    Box2D.NET's main branch has ported, used for the managed-vs-native comparison at equal version
#   b2net-154bacd0   Box2D.NET main at 154bacd0 (2026-09-29)
# Usage: fetch-sources.sh <work-dir>   (scratch folder outside the repository)
set -euo pipefail
WORK=${1:?work dir}
mkdir -p "$WORK"
get() { # name url
  [ -f "$WORK/$1/CMakeLists.txt" ] || [ -f "$WORK/$1/README.md" ] && return 0
  mkdir -p "$WORK/$1"
  curl -fsSL -o "$WORK/$1.tgz" "$2"
  tar xzf "$WORK/$1.tgz" -C "$WORK/$1" --strip-components=1
}
get box2d-v3.2.0 https://codeload.github.com/erincatto/box2d/tar.gz/refs/tags/v3.2.0
get box2d-f2086ed6 https://codeload.github.com/erincatto/box2d/tar.gz/f2086ed6
get b2net-154bacd0 https://codeload.github.com/ikpil/Box2D.NET/tar.gz/154bacd0
ls "$WORK"
