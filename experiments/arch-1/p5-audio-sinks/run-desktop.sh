#!/usr/bin/env bash
# ARCH-1 P5: builds the desktop probe, runs the offline check, the mixer cost and the live sink matrix on the default
# playback device, writes one JSON line per run to <out.jsonl> and prints the summary table.
# Usage: run-desktop.sh <out.jsonl> [runs per configuration, default 3]
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
out=${1:?output .jsonl}; runs=${2:-3}
dotnet build "$here/Desktop/P5.Desktop.csproj" -c Release -nologo -v q
dll="$here/Desktop/bin/Release/net10.0/P5.Desktop.dll"
dotnet "$dll" offline
for v in 0 16 64; do dotnet "$dll" cost "$v" 1024; done
: > "$out"
for config in "pull" "push 50" "push 150" "push 400" "feeder 40" "feeder 60" "feeder 80" "feeder 100" "feeder 150"; do
  for _ in $(seq "$runs"); do
    # shellcheck disable=SC2086
    dotnet "$dll" $config >> "$out"
  done
done
# Cross-check of the lost-audio measure: SDL's own report of missing bytes (observer callback) on the push sink.
for _ in $(seq "$runs"); do P5_OBSERVE=1 dotnet "$dll" push 150 >> "$out"; done
node "$here/summarize.mjs" < "$out"
node -e '
for (const l of require("fs").readFileSync(process.argv[1], "utf8").trim().split(/\n/)) {
  const j = JSON.parse(l);
  const ph = Object.entries(j.phases).map(([k, p]) => `${k}:starved=${Math.round(p.starvedMs)},late=${Math.round(p.lateExcessMs)}`).join(" ");
  console.log(j.sink, "| gcPauseMs", j.gcPhasePauseMs, "gcs", j.gcPhaseGen0Count, "forcedGcMs", j.gcMs, "gcFrameMs", j.phases.gc.maxHostFrameMs, "renderOnMain", j.renderOnMainThread, "|", ph);
}' "$out"
