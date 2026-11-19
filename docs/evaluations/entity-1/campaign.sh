#!/usr/bin/env bash
# The ENTITY-1 measurement campaign (docs/evaluations/entity-1.md): every run of the evaluation, one after another.
#
#   docs/evaluations/entity-1/campaign.sh OUT [PART...]
#
# OUT is a folder outside the repository (results and published builds go there). PARTS, in the order they run when
# none is named: idle publish loads limits window aot. Nothing else may be building or testing on the machine: the idle
# part prints what it saw, and every measuring part runs it again first, so the conditions are recorded with the
# results (OUT/idle-<part>.md). WORKLOADS="move" limits the limits part to some workloads (it appends to limits.md).
set -u
REPO="$(cd "$(dirname "$0")/../../.." && pwd)"
OUT="${1:?usage: campaign.sh OUT [PART...]}"
shift
mkdir -p "$OUT"
OUT="$(cd "$OUT" && pwd)"
PARTS="${*:-idle publish loads limits window aot}"
JIT="$OUT/jit/EcsBench"
AOT="$OUT/aot/EcsBench"
COMMIT="$(git -C "$REPO" rev-parse --short HEAD)"
STORES="simple arch friflo proto"

idle() { # name of the part the check belongs to
  {
    echo "## Idle check before '$1' ($(date -u +%Y-%m-%dT%H:%M:%SZ), commit $COMMIT)"
    if command -v powershell.exe > /dev/null; then
      # Idle means: every sample of the machine's load is a few percent, and the build, test and benchmark processes
      # that exist (build servers stay alive) used no CPU time during the ten seconds.
      powershell.exe -NoProfile -Command "\$n = 'dotnet','testhost','MSBuild','VBCSCompiler','StressBench','EcsBench'; \$a = @{}; Get-Process \$n -ErrorAction SilentlyContinue | ForEach-Object { \$a[\$_.Id] = \$_.CPU }; \$l = 1..10 | ForEach-Object { (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average; Start-Sleep -Seconds 1 }; \$p = @(Get-Process \$n -ErrorAction SilentlyContinue); \$used = 0; \$p | ForEach-Object { if (\$a.ContainsKey(\$_.Id)) { \$used += \$_.CPU - \$a[\$_.Id] } else { \$used += \$_.CPU } }; 'CPU load %, ten samples: ' + (\$l -join ' '); 'build, test and benchmark processes: ' + \$p.Count + ', CPU seconds they used meanwhile: ' + [math]::Round(\$used, 2)" | tr -d '\r'
    else
      top -bn2 -d1 | grep -i "cpu(s)" | tail -1
      pgrep -a "dotnet|testhost" || true
    fi
  } | tee "$OUT/idle-$1.md"
}

names() { # workload, then stores
  local workload="$1" list="" s; shift
  for s in "$@"; do list="$list,ecs-$workload-$s"; done
  echo "${list#,}"
}

for part in $PARTS; do
  case "$part" in
    idle) idle start ;;
    publish)
      dotnet publish "$REPO/benchmarks/EcsBench" -c Release -o "$OUT/jit" -v:q -nologo || exit 1
      # The build servers the publish leaves behind keep working for a while: stop them before anything is measured.
      dotnet build-server shutdown > /dev/null 2>&1
      ;;
    loads)
      # Every workload on every store at three loads; 600 frames after 120, the best of three, each in its own process.
      idle loads
      : > "$OUT/loads.md"
      for load in 10000 100000 1000000; do
        for w in move query; do
          "$JIT" run --scenario "$(names $w $STORES)" --mode headless --load $load --repeat 3 --isolate --commit "$COMMIT" --out "$OUT/loads-$w-$load.json" >> "$OUT/loads.md" 2>&1
        done
        "$JIT" run --scenario "$(names churn simple simple-alloc arch friflo proto)" --mode headless --load $load --repeat 3 --isolate --commit "$COMMIT" --out "$OUT/loads-churn-$load.json" >> "$OUT/loads.md" 2>&1
      done
      for load in 10000 50000 250000; do
        "$JIT" run --scenario "$(names mixed simple simple-alloc arch friflo proto)" --mode headless --load $load --repeat 3 --isolate --commit "$COMMIT" --out "$OUT/loads-mixed-$load.json" >> "$OUT/loads.md" 2>&1
      done
      ;;
    limits)
      # The largest load whose 99th percentile of CPU work stays within one 60 Hz update.
      idle "limits${WORKLOADS:+-$WORKLOADS}"
      for w in ${WORKLOADS:-move churn mixed}; do
        for s in $STORES; do
          "$JIT" sweep --scenario "ecs-$w-$s" --mode headless --frames 240 --warmup 60 --repeat 2 --isolate --max-load 8000000 --out "$OUT/limit-$w-$s.json" >> "$OUT/limits.md" 2>&1
        done
      done
      ;;
    window)
      # The same objects through the real renderer (a hidden SDL window): what the store changes once frames are drawn.
      idle window
      : > "$OUT/window.md"
      for load in 20000 100000; do
        "$JIT" run --scenario "$(names move $STORES)" --mode render --load $load --repeat 3 --isolate --commit "$COMMIT" --out "$OUT/window-move-$load.json" >> "$OUT/window.md" 2>&1
        "$JIT" run --scenario "$(names mixed $STORES)" --mode render --load $load --repeat 3 --isolate --commit "$COMMIT" --out "$OUT/window-mixed-$load.json" >> "$OUT/window.md" 2>&1
      done
      ;;
    aot)
      # NativeAOT needs the platform's native toolchain; on Windows run this part from a shell with a short PATH
      # (vcvarsall fails with "input line too long" under a long one; vswhere.exe must be on it). The repository
      # builds with warnings as errors, and Friflo.Engine.ECS and its JSON dependency produce trim and AOT analysis
      # warnings, so this publish keeps them as warnings, one line each: the log is the record of what each library
      # declares (grep "warning IL").
      dotnet publish "$REPO/benchmarks/EcsBench" -c Release -r "$(dotnet --info | awk '/RID:/{print $2; exit}')" -p:PublishAot=true -p:TreatWarningsAsErrors=false -p:TrimmerSingleWarn=false -o "$OUT/aot" -nologo > "$OUT/aot-publish.log" 2>&1 \
        || { echo "NativeAOT publish failed: see aot-publish.log" | tee "$OUT/aot-unavailable.md"; continue; }
      dotnet build-server shutdown > /dev/null 2>&1
      idle aot
      : > "$OUT/aot.md"
      # One scenario per run: a store that fails under NativeAOT must not take the others' results with it.
      for w in move churn query mixed; do
        for s in $STORES; do
          "$AOT" run --scenario "ecs-$w-$s" --mode headless --repeat 3 --label aot --commit "$COMMIT" --out "$OUT/aot-$w-$s.json" >> "$OUT/aot.md" 2>&1             || echo "ecs-$w-$s: FAILED under NativeAOT (exit code $?)" >> "$OUT/aot.md"
        done
      done
      ;;
    *) echo "unknown part '$part'" >&2; exit 2 ;;
  esac
  echo "campaign: $part done"
done
