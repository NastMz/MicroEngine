# KPF-1 reproducible evidence and subjective handoff

Branch `feat/kenney-platformer`, baseline2415072. Latest owner instruction: local commits only, no push/main integration. Contract [KPF-1](../../specs/kenney-platformer.md), [ADR-0012](../../decisions/0012-playable-platformer-infrastructure.md). No full H1 acceptance inferred.

## Run the game

```powershell
dotnet run --project examples/Platformer/Platformer.csproj -c Release
```

A/D or arrow keys move; fresh Space jumps (100ms optional-in-this-game landing buffer), Escape/window close exits. Collect three gold coins, cross steps/platforms and jump spikes; the green flag completes only with all coins. A fresh Space after death/completion restarts the entire run and consumes its update. This is one fixed1920unit level with horizontal camera follow, not a general scene/physics system. Watch collision versus artwork readability and whether any required jumps/collectibles feel unfair. Do not repeat technical tests for owner acceptance.

## Already automated (agent responsibility)

```powershell
dotnet test checks/TwoD.Contracts/TwoD.Contracts.csproj -c Release -p:UseSharedCompilation=false -m:1 -nodeReuse:false
dotnet test checks/Platformer.Contracts/Platformer.Contracts.csproj -c Release -p:UseSharedCompilation=false -m:1 -nodeReuse:false
pwsh -NoProfile -File checks/PlatformerNativeSmoke.ps1
```

Retain Loop/Desktop/Hosting suites from toolchain and all13previous native harnesses. The new harness verifies actual PNG decode/alpha, old256default rejection, expanded902sheet upload, framebuffer atlas sample and color diversity, public517step course/coins/camera/completion, consumed reset and identical replay, clean resources/window close, plus seven production CLI cases with exits0/1/2. No physical-key automation claim. Generic animation/cache/sweep tests are independent of game content.

Observed own RED/GREEN: reusable TwoD9failed->9passed; decoder/options2failed->2passed, fullDesktop38passed; initial game3failed->3passed, subsequent gating/buffer/focus coverage added without fabricatedRED. Initial test annotations mismatched MSTest and were corrected before runnableRED. Asset metadata mapping is game content, not a Kenney engine importer. General capabilities live in optional engine/TwoD; CPU PNG native loading stays in Desktop. No package/SDK changes.

`start.png` is actual native framebuffer readback at spawn (not a mockup). Original capture helper rejected an absolute TakeScreenshot filename by prefixing cwd; checked ExportImage of the same readback resolved this optional evidence output. The native RGB checks had already passed; no failed image export counted as a successful capture.

## Provenance

[Kenney New Platformer Pack](https://kenney.nl/assets/new-platformer-pack), release1.1, retrieved2026-10-18; archiveSHA256 `553b907f3f0e505ab65f56f245ccaff3123c8fe3f3a0dfce9373b996bfc18cc2`. Source PNG/XML/License original bytes and per-file hashes: [manifest](../../../../examples/Platformer/Assets/provenance.json), [license](../../../../examples/Platformer/Assets/License.txt). CC0 [official deed](https://creativecommons.org/publicdomain/zero/1.0/); no Mario commercial assets/endorsement. Narrow asset attributes preserve original XML/license bytes across Git line endings.

## Independent technical closure

Independent verification and parent spotcheck are complete; evidence follows. Only subjective controls/artwork/readability/gameplay evaluation remains owner-owned after technical closure; do not infer it from pixel checks.

KPF-1 technical closure2026-10-18: independent kenney_platformer_verify PASS S1-S6, after one source correction7843a54 for overlapping-static-solid rejection (new regression RED1->GREEN10TwoD, touching retained). Initial full Release suites Hosting31/Loop159/Desktop38/TwoD9/Platformer6 =243PASS, then targeted correctedTwoD10 and independent public edge/course probesPASS =>244effective verified cases. All14nativeharnessesPASS, including13baseline and new KPF realPNG/902atlas/default256vs2048budget/pixel/alpha/camera/517stepcourse/coins/death/gating/reset/replay plus production7CLIcases0/1/2. Fresh builds0warnings/errors. Parent corrected native+7CLI spotcheckPASS. No full-suite rerun after bounded correction claimed; unchanged suites retain initial proof, correctedmodule/route targeted. Nineoriginal workingfilehashesPASS and finalcommittedassetblobhashesPASS after431d441 restored onlyLicense canonicalnewline bytes; XML already exact. Source snapshot corrected233files paritytrue/SHA9aa9db9b0729ff1ff038d5a9b50d5218de5c3749499e84f07b5295d879192fe5 in independent system-temp scratchC:/tmp/MicroEngine-KPF-recheck-892db0258d134b029bef83b45b7a4a0b. No .git/.codegraph/build reuse. Old examples/Hosting/Loop/config/dependencies unchanged. Immutable upstreamLicense whitespace preserved by provenance; diffcheck exception limited to that file, authored code/docs clean. No physical-key or subjective/fullH1 acceptance inferred. Root accepts objective KPF-1; playable control/jump/visual/camera/progression assessment remains owner-owned. Currentmain/origin2415072 unchanged; latest instruction localcommitsonly, no push/main. Pre-existing.gitignore/.codegraph preserved. No independent ready task at this subjective gate; next gameplay refinement derives from owner experience. Memory mirror pending unavailable authoritative runtime identity.
