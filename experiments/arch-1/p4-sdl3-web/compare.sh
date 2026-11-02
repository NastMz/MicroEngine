#!/usr/bin/env bash
# ARCH-1 P4: runs measure.mjs <runs> times against each URL and prints one summary line per run.
#   PUPPETEER_DIR=<dir> compare.sh <runs> <seconds> <url>...
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
runs=${1:?runs}; seconds=${2:?seconds}; shift 2
for url in "$@"; do
  for i in $(seq "$runs"); do
    node "$here/measure.mjs" "$url" game "$seconds" | node -e '
      let s = ""; process.stdin.on("data", d => s += d).on("end", () => {
        const r = JSON.parse(s), sp = r.scriptProcessors?.[0] ?? {};
        console.log([r.url, "load", r.loadMs, "start", r.startMs, "raf50", r.rafIntervalMs?.median, "work50", r.frameWorkMs?.median,
          "work95", r.frameWorkMs?.p95, "workMax", r.frameWorkMs?.max, "ctx", (r.audio ?? []).map(a => a.states.join(">")).join("|"),
          "spBufMs", sp.bufferMs, "nonSilent", `${sp.nonSilent}/${sp.callbacks}`, "stats", r.stats, "errors", r.errors.length].join(" "));
      });'
  done
done
