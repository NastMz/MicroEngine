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

## Pending before closure

Independent whole-feature verification, parent spotcheck and final managed/native counts will be recorded below. Only subjective controls/artwork/readability/gameplay evaluation remains owner-owned after technical closure; do not infer it from pixel checks.
