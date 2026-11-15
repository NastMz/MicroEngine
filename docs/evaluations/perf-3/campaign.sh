#!/usr/bin/env bash
# The PERF-3 measurement campaign (docs/evaluations/perf-3.md): every run of the evaluation, one after another.
#
#   docs/evaluations/perf-3/campaign.sh OUT [PART...]
#
# OUT is a folder outside the repository (results and published builds go there). PARTS, in the order they run when
# none is named: idle publish baseline limits modes e2e sustained. The baseline part writes OUT/baseline.json, the
# file to copy to docs/evaluations/perf-3/baseline.json. Nothing else may be building or testing on the machine:
# the idle part prints what it saw so the conditions can be recorded with the results.
set -u
REPO="$(cd "$(dirname "$0")/../../.." && pwd)"
OUT="${1:?usage: campaign.sh OUT [PART...]}"
shift
mkdir -p "$OUT"
OUT="$(cd "$OUT" && pwd)"
PARTS="${*:-idle publish baseline limits modes e2e sustained}"
JIT="$OUT/jit/StressBench"
AOT="$OUT/aot/StressBench"
COMMIT="$(git -C "$REPO" rev-parse --short HEAD)"

for part in $PARTS; do
  case "$part" in
    idle)
      # Ten one-second samples of total CPU use, and the build or test processes running now.
      {
        echo "## Idle check ($(date -u +%Y-%m-%dT%H:%M:%SZ), commit $COMMIT)"
        if command -v powershell.exe > /dev/null; then
          # Idle means: every sample of the machine's load is a few percent, and the build and test processes that
          # exist (build servers stay alive) used no CPU time during the ten seconds.
          powershell.exe -NoProfile -Command "\$n = 'dotnet','testhost','MSBuild','VBCSCompiler','StressBench'; \$a = @{}; Get-Process \$n -ErrorAction SilentlyContinue | ForEach-Object { \$a[\$_.Id] = \$_.CPU }; \$l = 1..10 | ForEach-Object { (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average; Start-Sleep -Seconds 1 }; \$p = @(Get-Process \$n -ErrorAction SilentlyContinue); \$used = 0; \$p | ForEach-Object { if (\$a.ContainsKey(\$_.Id)) { \$used += \$_.CPU - \$a[\$_.Id] } else { \$used += \$_.CPU } }; 'CPU load %, ten samples: ' + (\$l -join ' '); 'build and test processes: ' + \$p.Count + ', CPU seconds they used meanwhile: ' + [math]::Round(\$used, 2)" | tr -d '\r'
        else
          top -bn2 -d1 | grep -i "cpu(s)" | tail -1
          pgrep -a "dotnet|testhost" || true
        fi
      } | tee "$OUT/idle.md"
      ;;
    publish)
      dotnet publish "$REPO/benchmarks/StressBench" -c Release -o "$OUT/jit" -v:q -nologo || exit 1
      # NativeAOT needs the platform's native toolchain; on Windows run this part from a shell with a short PATH
      # (vcvarsall fails with "input line too long" under a long one). Skipped when it fails.
      dotnet publish "$REPO/benchmarks/StressBench" -c Release -r "$(dotnet --info | awk '/RID:/{print $2; exit}')" -p:PublishAot=true -o "$OUT/aot" -v:q -nologo \
        || echo "NativeAOT publish failed: the aot runs of the modes part are skipped" | tee "$OUT/aot-unavailable.md"
      ;;
    baseline)
      "$JIT" suite --mode headless,render --repeat 3 --commit "$COMMIT" --label baseline --out "$OUT/baseline.json" > "$OUT/baseline.md" 2>&1
      ;;
    limits)
      : > "$OUT/limits.md"
      for s in entity-churn world-iterate spatial-query physics sprites batch-breaks shapes text particles asset-churn integrated; do
        "$JIT" sweep --scenario "$s" --mode headless --frames 240 --warmup 60 --repeat 2 --isolate --out "$OUT/limit-$s-headless.json" >> "$OUT/limits.md" 2>&1
      done
      for s in sprites batch-breaks shapes text particles integrated; do
        "$JIT" sweep --scenario "$s" --mode render --frames 240 --warmup 60 --repeat 2 --isolate --out "$OUT/limit-$s-render.json" >> "$OUT/limits.md" 2>&1
      done
      ;;
    modes)
      : > "$OUT/modes.md"
      run() { # program, label, then environment assignments
        local program="$1" label="$2"; shift 2
        echo "### $label" >> "$OUT/modes.md"
        env "$@" "$program" run --scenario entity-churn,world-iterate,physics,asset-churn,integrated --mode headless --repeat 3 --isolate --label "$label" --out "$OUT/modes-$label-headless.json" >> "$OUT/modes.md" 2>&1
        env "$@" "$program" run --scenario sprites,batch-breaks,asset-churn,integrated --mode render --repeat 3 --isolate --label "$label" --out "$OUT/modes-$label-render.json" >> "$OUT/modes.md" 2>&1
      }
      run "$JIT" jit-default PERF3=1
      run "$JIT" jit-server-gc DOTNET_gcServer=1
      run "$JIT" jit-non-concurrent-gc DOTNET_gcConcurrent=0
      run "$JIT" jit-no-tiered-pgo DOTNET_TieredPGO=0
      run "$JIT" jit-no-tiering DOTNET_TieredCompilation=0
      if [ -x "$AOT" ] || [ -x "$AOT.exe" ]; then
        run "$AOT" aot PERF3=1
        run "$AOT" aot-non-concurrent-gc DOTNET_gcConcurrent=0
      fi
      ;;
    e2e)
      # Visible windows, real time, VSync: about a minute, and the desktop must be left alone meanwhile.
      "$JIT" run --scenario sprites,batch-breaks,particles,asset-churn,integrated --mode e2e --isolate --out "$OUT/e2e.json" > "$OUT/e2e.md" 2>&1
      ;;
    sustained)
      # One minute of game time each.
      "$JIT" run --scenario integrated --mode e2e --frames 3600 --isolate --out "$OUT/sustained-e2e.json" > "$OUT/sustained.md" 2>&1
      "$JIT" run --scenario integrated,asset-churn --mode render --frames 3600 --isolate --out "$OUT/sustained-render.json" >> "$OUT/sustained.md" 2>&1
      ;;
    *) echo "unknown part '$part'" >&2; exit 2 ;;
  esac
  echo "campaign: $part done"
done
