#!/usr/bin/env bash
# Checks the iOS host in the iOS simulator, level by level (platforms/README.md, "Evidence levels"):
#   installed   the app installs
#   started     the .NET host prints "microengine: start"
#   rendered    each scripted run of the game (web/tests/scenarios.json) ends with all its captures; they are copied to
#               <out>/desktop/<Game>/<script> so `web/tests/desktop.mjs --skip-run` compares them with a reference
#   audio       SDL opened the audio device (not listened to)
#   lifecycle   opening another app suspends the game and bringing it back resumes it
#
# Usage: platforms/ios/simulator-checks.sh <app-dir> [game] [out]   (on macOS with Xcode; the app built for iossimulator-arm64)
#   touch-script  touch.script, scripted touches on the simulator's own control layout (safe area, density): movement and
#               a second control held at once, and the player moved. simctl cannot send real touches, so this checks
#               the controls on this screen and the game's response, not iOS's delivery of fingers.
# Exit code 1 if any level failed.
set -uo pipefail

newest_iphone() {
  xcrun simctl list devices available -j | node -e '
    const d = JSON.parse(require("fs").readFileSync(0, "utf8")).devices;
    const ios = Object.keys(d).filter(k => k.includes("iOS")).sort().reverse();
    for (const k of ios) { const p = d[k].find(x => x.name.startsWith("iPhone")); if (p) { console.log(p.udid); break; } }'
}

app=${1:?path to the .app}; game=${2:-MeadowRun}; out=${3:-dist/ios-checks}
repo=$(cd "$(dirname "$0")/../.." && pwd)
bundle=dev.microengine.$(echo "$game" | tr '[:upper:]' '[:lower:]')
summary=${GITHUB_STEP_SUMMARY:-/dev/null}
failed=0
report() { echo "$1 $2: $3"; echo "| $2 | $([ "$1" = PASS ] && echo pass || echo fail) | $3 |" >> "$summary"; [ "$1" = PASS ] || failed=1; }
printf '### iOS simulator checks\n\n| Level | Result | Detail |\n| --- | --- | --- |\n' >> "$summary"
rm -rf "$out"; mkdir -p "$out"

device=$(newest_iphone)
[ -n "$device" ] || { report FAIL simulator "no iPhone simulator available"; exit 1; }
xcrun simctl boot "$device" 2>/dev/null; xcrun simctl bootstatus "$device" -b > /dev/null
echo "simulator: $(xcrun simctl list devices | grep "$device" | sed 's/^ *//')"

if xcrun simctl install "$device" "$app"; then report PASS installed "$(basename "$app") on $(xcrun simctl list devices | grep "$device" | sed 's/ (.*//;s/^ *//')"
else report FAIL installed "simctl install failed"; exit 1; fi
data=$(xcrun simctl get_app_container "$device" "$bundle" data)

started=0; runs=0; rendered=0; audio=1; details=""
while IFS=$'\t' read -r script env; do
  name=${script%.script}
  # macOS has no `timeout`; perl's alarm ends a launch that never exits.
  log=$(perl -e 'alarm shift; exec @ARGV' 300 xcrun simctl launch --console-pty --terminate-running-process "$device" "$bundle" --script "$script" --env "$env" 2>&1 | tr -d '\r')
  echo "$log" > "$out/$name.log"
  if grep -q microengine: <<< "$log"; then grep microengine: <<< "$log" | grep -v 'microengine: scene' | sed "s/^/  ios $name: /"
  else echo "  ios $name: no microengine line; launch output:"; head -20 <<< "$log" | sed 's/^/    /'; fi
  grep -q "microengine: start" <<< "$log" && started=1
  grep -q "audio available" <<< "$log" || audio=0
  done=$(grep -o 'microengine: done [0-9]* updates, [0-9]* frames, [0-9]* captures' <<< "$log" | sed 's/microengine: done //')
  expected=$(grep -c 'microengine: capture ' <<< "$log")
  mkdir -p "$out/desktop/$game"
  cp -R "$data/Library/Caches/captures/$name" "$out/desktop/$game/$name" 2>/dev/null
  pulled=$(ls "$out/desktop/$game/$name"/*.png 2>/dev/null | wc -l | tr -d ' ')
  runs=$((runs + 1))
  if [ -n "$done" ] && [ "$expected" -gt 0 ] && [ "$pulled" = "$expected" ] && grep -q "microengine: exit 0" <<< "$log"; then
    rendered=$((rendered + 1)); details+="$name: ${done%%,*}, $pulled/$expected captures; "
  else details+="$name: ${done:-no result}, $pulled/$expected captures; "; fi
done < <(node -e '
  const s = require(process.argv[1]).scenarios.filter(s => s.game === process.argv[2]);
  for (const x of s) console.log(`${x.script}\t${Object.entries(x.env).map(([k, v]) => `${k}=${v}`).join(";")}`);' "$repo/web/tests/scenarios.json" "$game")

[ $started = 1 ] && report PASS started "the .NET host logged its start" || report FAIL started "no \"microengine: start\" on the console"
[ $runs -gt 0 ] && [ $rendered = $runs ] && report PASS rendered "$details" || report FAIL rendered "$rendered/$runs runs complete: $details"
[ $runs -gt 0 ] && [ $audio = 1 ] && report PASS audio "SDL opened the audio device in every run" || report FAIL audio "no audio device in at least one run"

# Touch: the game's touch.script with the gameplay shortcut each game needs, probing actions and the player's position.
case "$game" in
  MeadowRun) tenv="MEADOW_START=level"; pair="right jump" ;;
  NeonRift) tenv="RIFT_START=stage;RIFT_GOD=1"; pair="up fire" ;;
  CourtyardKey) tenv="COURTYARD_START=road"; pair="down down" ;;
  *) tenv="TRAIL_DEBUG=town"; pair="down down" ;;
esac
tlog=$(perl -e 'alarm shift; exec @ARGV' 300 xcrun simctl launch --console-pty --terminate-running-process "$device" "$bundle" --script touch.script --env "$tenv;MICROENGINE_PROBE=1" 2>&1 | tr -d '\r')
echo "$tlog" > "$out/touch-script.log"
read -r first second <<< "$pair"
together=$(grep 'microengine: probe' <<< "$tlog" | grep -E "\[([^]]*,)?$first(,[^]]*)?\]" | grep -cE "[[,]$second[],]")
states=$(grep 'microengine: probe' <<< "$tlog" | sed 's/.*\] //' | sort -u | wc -l | tr -d ' ')
controls=$(grep -c 'microengine: control' <<< "$tlog")
if grep -q "microengine: exit 0" <<< "$tlog" && [ "$together" -gt 0 ] && [ "$states" -gt 2 ]; then
  report PASS touch-script "$controls controls laid out; '$first' with '$second' in $together probes; $states distinct player states (scripted touches)"
else report FAIL touch-script "exit: $(grep -o 'microengine: exit [0-9]*' <<< "$tlog"); together $together; states $states; controls $controls"; fi

# Lifecycle: an interactive run sent to the background by another app, then brought back.
lifelog="$out/interactive.log"
xcrun simctl launch --console-pty --terminate-running-process "$device" "$bundle" > "$lifelog" 2>&1 &
console=$!
# Each step waits for the game's own log line rather than a fixed time.
await_line() { for _ in $(seq 1 "$2"); do grep -q "$1" "$lifelog" && return 0; sleep 0.5; done; return 1; }
await_line 'microengine: scene' 120
xcrun simctl launch "$device" com.apple.Preferences > /dev/null 2>&1; await_line 'microengine: suspended' 40
xcrun simctl launch "$device" "$bundle" > /dev/null 2>&1; await_line 'microengine: resumed' 40
xcrun simctl io "$device" screenshot "$out/resumed.png" > /dev/null 2>&1
xcrun simctl terminate "$device" "$bundle" > /dev/null 2>&1; sleep 1; kill $console 2>/dev/null
if grep -q "microengine: suspended" "$lifelog" && grep -q "microengine: resumed" "$lifelog"; then report PASS lifecycle "another app suspended the game, relaunching resumed it (resumed.png)"
else report FAIL lifecycle "no suspended/resumed pair: $(grep microengine: "$lifelog" | tr '\r\n' '  ' | cut -c1-300)"; fi
xcrun simctl shutdown "$device" > /dev/null 2>&1
exit $failed
