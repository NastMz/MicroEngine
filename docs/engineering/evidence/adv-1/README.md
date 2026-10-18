# ADV-1 — Courtyard Key playable reuse evidence

Baseline821450d, local feat/courtyard-adventure. [Contract](../../specs/adventure.md), [Meadow Run acceptance](../kpf-1/acceptance.md), [backlog](../../backlog.md). No push/main integration authorized for this delivery.

Current presentation refinement: [ADV-ART-1 art/evidence](art.md) supersedes the original front-facing Kenney appearance below. Original ADV-1 proof remains historical; gameplay is preserved.

## Run and subjective-only evaluation

```powershell
dotnet run --project examples/Adventure/Adventure.csproj -c Release
```

WASD/arrows move in four directions with normalized diagonals. Stand on the lever and press a fresh Space to open the gold gate. Walk through the cyan east passage; in the Shrine stand on the golden key and press Space, walk around the central pillar to the blue altar and press Space. Completing the shrine freezes the game; fresh Space replays from the Courtyard. Escape closes. Held Space does not repeatedly activate objects. Returning between areas retains key/gate state for this run only.

Evaluate navigation/sliding and transition feel, lever/passage/key/altar discoverability, clarity of contextual prompts and whether the short puzzle/flow makes sense. No requirement to repeat technical checks. Front-facing beige walk art is deliberately limited; this horizon does not claim directional character art, combat, inventory framework, persistence, menu/audio or full H2 acceptance.

## Architecture reuse and boundary

Engine/Hosting/Loop/TwoD/Desktop are byte-unchanged from accepted Meadow Run821450d. New game uses public StaticWorld sweeps/strict overlap, FrameAnimation playback/reset, ImageSet keyed lease ownership, DesktopImageDecoder/public image regions/labels/FrameLoop/input. No game collision solver/clock/cache/decoder/native calls or engine Zelda abstractions. Room/state transitions, gate/lever/key/altar rules, movement normalization/speed and completion/restart are game content/policy. Three immutable content worlds share one execution/window and one image cache; Area is not a generalized scene framework.

Six original PNG assets plus CC0 License selected from the same locally retained Kenney1.1 archive (no new download). [Provenance](../../../../examples/Adventure/Assets/provenance.json) includes original per-file/archive hashes; [license](../../../../examples/Adventure/Assets/License.txt). Fixed source regions are consumer content, not a new metadata importer. Native resource count verifies six acquisitions total across visits/reset/replay.

## Automated checks (agent-owned)

```powershell
dotnet test checks/Adventure.Contracts/Adventure.Contracts.csproj -c Release -p:UseSharedCompilation=false -m:1 -nodeReuse:false
pwsh -NoProfile -File checks/AdventureNativeSmoke.ps1
```

Own model9runnableRED->9GREEN; presentation2 additional checksGREEN (no fabricatedRED). Native actual gateclosed/open framebuffer pixels/HUD/artwork, two areas/key/altar/public route/reset/identical replay/shared six-image cache/clean native closePASS; productionCLI7cases exact0/1/2PASS. Build0warnings/errors. Rendering tested pure with/without optional labels and motion-driven playback; old solid view remains immutable after gate changes. `courtyard.png`/`shrine.png` are native framebuffer exports inspected for layout only, not subjective product acceptance.

Independent full regression255/255/all15nativeharnesses/public-boundary/sourcehash checksPASS, no blockers. Technical closure complete; new subjective adventure evaluation remains pending. No unobserved physical-key automation or acceptance claimed. Runtime memory mirror pending unavailable authoritative session identity.

ADV-1 technical closure2026-10-18: independent kenney_platformer_verify PASS S1-S4 on frozen0139683 vs821450d, no blockers/correction. Fresh serial Release Hosting31/Loop159/Desktop38/TwoD10/Platformer6/Adventure11 =255/255PASS; builds0warnings/errors; all15nativeharnessesPASS (prior14retained plusAdventure actualPNG/gateRGB/HUD/twoarea/key/altar/completion/reset/identicalreplay/shared6imagecache/nativeclose and7productionCLIcases0/1/2). Independent public probes PASS gateclosing/immutableoldviews, normalizedmovement/sliding/corners/fullbodyopening/transitionconsumption, finalposition/strict/fresh/held-case-duplicate/invalidatomicity, missingkey/visitpersistence/completedfreeze/consumedreset/replay/renderpure/motionclock/cache. Seven original assets and committedGitblobs match SHA256manifest/CC0; upstreamlicensewhitespace retained. Fresh system-temp snapshot C:/tmp/MicroEngine-ADV1-verify-d64d4d0573c94a91bed0fd680e02b31a,260files/workspaceparitytrue SHA29356a28066bc60a08a173e49bab06c97d586af177ac23af579bda32ac0b5e7d; excluded.git/.codegraph/bin/obj/.vs. Protected engine/alloldgames/config/dependencies unchanged. Parent currentnative -NoBuild +7CLIspotcheckPASS, changed-doclinks/assetblobhash/diffcheck(exactupstreamLicenseexception)PASS. Root accepts objective ADV1/ME060complete, subjective navigation/interaction/readability/flow and fullH2 acceptance unobserved; no physicalkey claim. Meadow Run firstscenario alreadyaccepted/closed against ownerfeedback+public-boundaryaudit. Main/origin2415072 unchanged, no push/main; preserved.gitignore/.codegraph. No independent readytask at newplayable subjectivegate, no speculative futurecombat/audio/persistence tasks. Runtime memory mirror pending unavailable authoritative identity.
