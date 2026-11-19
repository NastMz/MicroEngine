#!/bin/sh
# CONTENT-1 (docs/evaluations/content-1.md): the measurement campaign.
#
#   dotnet build benchmarks/ContentBench -c Release
#   sh docs/evaluations/content-1/campaign.sh SCRATCH [GROUP...]
#
# SCRATCH is a folder outside the repository (the content is drawn into SCRATCH/content, about 1.5 GB with the
# candidates' payloads). Groups, in this order when none is named: generate, direct, candidates, levels, minify.
# Each group shuts the build servers down, records what else the machine was doing before and after (idle.ps1, on
# Windows), and writes SCRATCH/results-GROUP.md. Run one campaign at a time, on a machine doing nothing else.
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/../../.." && pwd)"
S="$1"; shift
[ -n "$S" ] || { echo "usage: campaign.sh SCRATCH [generate|direct|candidates|levels|minify ...]" >&2; exit 2; }
[ $# -gt 0 ] || set -- generate direct candidates levels minify
B="$REPO/benchmarks/ContentBench/bin/Release/net10.0/ContentBench"
[ -x "$B" ] || B="$B.exe"
mkdir -p "$S"

idle() {
  dotnet build-server shutdown >/dev/null 2>&1 || true
  if command -v powershell >/dev/null 2>&1; then powershell -NoProfile -ExecutionPolicy Bypass -File "$HERE/idle.ps1" | head -5; else echo "idle check: not available on this system"; fi
}

for G in "$@"; do
  {
    echo '```'; idle; echo '```'; echo
    case "$G" in
      generate) "$B" generate "$S/content" ;;
      direct)
        # The demo's music, read in place, for the audio loaders.
        "$B" direct "$S/content" --runs 7 --audio "$REPO/games/NeonRift/Content/music"; echo
        "$B" direct "$S/content" --runs 7 --device sdl-gpu ;;
      candidates) "$B" candidates "$S/content" --runs 5 ;;
      levels)
        echo "| Path | Device | Pages | On disk | Loading screen | Frames | Longest frame | Allocated | GC 0/1/2 | GC pauses | Peak working set | Texture bytes (computed) | GPU memory (measured) |"
        echo "| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: | ---: |"
        # Every path in a process of its own, three rounds, so a figure can be compared with its repeats.
        for size in 2048 4096; do
          for round in 1 2 3; do
            for path in direct preload lean fast ktx-rgba; do "$B" level "$S/content" --path $path --size $size --pages 4 --device sdl-renderer | tail -1; done
            for path in direct preload lean fast ktx-rgba ktx-bc7 rgba bc7; do "$B" level "$S/content" --path $path --size $size --pages 4 --device sdl-gpu | tail -1; done
          done
        done ;;
      minify) "$B" minify "$S/content" --size 2048 ;;
      *) echo "unknown group $G" ;;
    esac
    echo; echo '```'; idle; echo '```'
  } > "$S/results-$G.md" 2>&1
  echo "done $G: $S/results-$G.md"
done
