#!/usr/bin/env bash
# Plays each game's APK on the running emulator (device-checks.mjs) and compares its captures with the SDL reference.
# Called by the platform workflow inside the emulator action, which runs its own script lines with sh.
#
# Usage: platforms/android/ci-play.sh <out> <game>...
set -uo pipefail
out=${1:?output folder}; shift
status=0
for game in "$@"; do
  apk=$(ls platforms/android/bin/"$game"/Release/net10.0-android/*-Signed.apk)
  node platforms/android/device-checks.mjs --apk "$apk" --game "$game" --out "$out/$game" || status=1
  node web/tests/desktop.mjs --skip-run --games "$game" --out "$out/$game" --reference web/tests/reference/sdl.json \
    | tee -a "${GITHUB_STEP_SUMMARY:-/dev/null}" || status=1
done
exit $status
