#!/usr/bin/env bash
# Checks the iOS host in the iOS simulator, level by level (platforms/README.md, "Evidence levels"): installed,
# started, and each scripted run of the game (web/tests/scenarios.json) finishing with the same updates, frames and
# captures as the desktop build on the headless platform. Nothing is drawn: raylib 6.0 has no iOS platform.
#
# Usage: platforms/ios/simulator-checks.sh <app-dir> [game]      (on macOS with Xcode; the app built for iossimulator-arm64)
# The desktop game must be built (dotnet build MicroEngine.slnx -c Release). Exit code 1 if any level failed.
set -uo pipefail
app=${1:?path to the .app}; game=${2:-MeadowRun}
repo=$(cd "$(dirname "$0")/../.." && pwd)
bundle=dev.microengine.$(echo "$game" | tr '[:upper:]' '[:lower:]')
summary=${GITHUB_STEP_SUMMARY:-/dev/null}
failed=0
report() { echo "$1 $2: $3"; echo "| $2 | $([ "$1" = PASS ] && echo pass || echo fail) | $3 |" >> "$summary"; [ "$1" = PASS ] || failed=1; }
printf '### iOS simulator checks\n\n| Level | Result | Detail |\n| --- | --- | --- |\n' >> "$summary"

device=$(xcrun simctl list devices available -j | node -e '
  const d = JSON.parse(require("fs").readFileSync(0, "utf8")).devices;
  const ios = Object.keys(d).filter(k => k.includes("iOS")).sort().reverse();
  for (const k of ios) { const p = d[k].find(x => x.name.startsWith("iPhone")); if (p) { console.log(p.udid); break; } }')
[ -n "$device" ] || { report FAIL simulator "no iPhone simulator available"; exit 1; }
xcrun simctl boot "$device" 2>/dev/null; xcrun simctl bootstatus "$device" -b > /dev/null
echo "simulator: $(xcrun simctl list devices | grep "$device" | sed 's/^ *//')"

if xcrun simctl install "$device" "$app"; then report PASS installed "$(basename "$app") on $(xcrun simctl list devices | grep "$device" | sed 's/ (.*//;s/^ *//')"
else report FAIL installed "simctl install failed"; exit 1; fi

started=0; runs=0; matching=0; details=""
dir="$repo/games/$game/bin/Release/net10.0"
exe="$dir/$game"
while IFS=$'\t' read -r script env; do
  name=${script%.script}
  out=$(timeout 300 xcrun simctl launch --console-pty --terminate-running-process "$device" "$bundle" --script "$script" --env "$env" 2>&1 | tr -d '\r')
  echo "$out" | grep microengine: | sed "s/^/  ios $name: /"
  grep -q "microengine: start" <<< "$out" && started=1
  ios=$(grep -o 'microengine: done [0-9]* updates, [0-9]* frames, [0-9]* captures' <<< "$out" | sed 's/microengine: done //')
  # The same script on the desktop build, headless, with the same environment.
  saves=$(mktemp -d)
  desk=$(env $(tr ';' ' ' <<< "$env") MEADOW_SAVE_DIR="$saves" COURTYARD_SAVE_DIR="$saves" TRAIL_SAVE_DIR="$saves" RIFT_SAVE_DIR="$saves" \
    "$exe" --headless --script "$repo/games/$game/scripts/$script" 2>&1)
  rm -rf "$saves"
  dcap=$(grep -c '^capture:' <<< "$desk"); dupd=$(sed -n 's/^done: \([0-9]*\) updates, \([0-9]*\) frames$/\1 updates, \2 frames/p' <<< "$desk")
  runs=$((runs + 1))
  if [ -n "$ios" ] && [ "$ios" = "$dupd, $dcap captures" ]; then matching=$((matching + 1)); details+="$name: $ios; "
  else details+="$name: iOS '${ios:-no result}' vs desktop '$dupd, $dcap captures'; "; fi
done < <(node -e '
  const s = require(process.argv[1]).scenarios.filter(s => s.game === process.argv[2]);
  for (const x of s) console.log(`${x.script}\t${Object.entries(x.env).map(([k, v]) => `${k}=${v}`).join(";")}`);' "$repo/web/tests/scenarios.json" "$game")

[ $started = 1 ] && report PASS started "the .NET host logged its start" || report FAIL started "no \"microengine: start\" on the console"
[ $runs -gt 0 ] && [ $matching = $runs ] && report PASS simulated "$matching/$runs scripted runs match the desktop build: $details" \
  || report FAIL simulated "$matching/$runs scripted runs match the desktop build: $details"
xcrun simctl shutdown "$device" > /dev/null 2>&1
exit $failed
