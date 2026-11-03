#!/usr/bin/env bash
# ARCH-1 P5: builds the Android probe for the connected device's ABI, installs it, runs each configuration through
# `am start` and collects the JSON line the probe writes to logcat (tag P5). Prints the summary table.
# Usage: run-android.sh <out.jsonl> [runs per configuration, default 2]   (one device or emulator attached to adb)
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
out=${1:?output .jsonl}; runs=${2:-2}
case "$(adb shell getprop ro.product.cpu.abi | tr -d '\r')" in x86_64) rid=android-x64 ;; *) rid=android-arm64 ;; esac
dotnet build "$here/Android/P5.Android.csproj" -c Release -nologo -v q -p:RuntimeIdentifier=$rid
adb install -r "$here/Android/bin/Release/net10.0-android/$rid/dev.microengine.p5audio-Signed.apk"
run() { # mode target
  adb shell am force-stop dev.microengine.p5audio
  adb logcat -c
  adb shell am start -n dev.microengine.p5audio/dev.microengine.p5.ProbeActivity --es mode "$1" --ei target "$2" > /dev/null
  for _ in $(seq 60); do
    if adb logcat -d -s P5:I | grep -q "P5 *: END"; then break; fi
    sleep 1
  done
  # Rejoin the BEGIN/MORE chunks of the report into one line.
  adb logcat -d -s P5:I | tr -d '\r' | sed -n 's/.*P5 *: \(BEGIN\|MORE\) //p' | tr -d '\n'
  echo
}
run offline 0
run cost 0
run cost 16
: > "$out"
for config in "pull 0" "push 50" "push 150" "feeder 40" "feeder 150"; do
  for _ in $(seq "$runs"); do
    # shellcheck disable=SC2086
    run $config >> "$out"
  done
done
adb shell am force-stop dev.microengine.p5audio
node "$here/summarize.mjs" < "$out"
node -e '
for (const l of require("fs").readFileSync(process.argv[1], "utf8").trim().split(/\n/)) {
  const j = JSON.parse(l);
  if (!j.phases) { console.log(l); continue; }
  console.log(j.sink, "| renderThreads", j.renderThreads, "main", j.mainThread, "onMain", j.renderOnMainThread, "| gcPauseMs", j.gcPhasePauseMs, "forcedGcMs", j.gcMs, "gcFrameMs", j.phases.gc.maxHostFrameMs, "| late", Object.values(j.phases).map(p => Math.round(p.lateExcessMs)).join("/"));
}' "$out"
