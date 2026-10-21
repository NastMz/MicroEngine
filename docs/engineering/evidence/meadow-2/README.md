# MR-2 — Meadow Run two-level campaign
Technical verification in progress; owner subjective progression acceptance is not yet recorded.

## Play
```powershell
dotnet run --project C:\Proyectos\MicroEngine\examples\Platformer\Platformer.csproj -c Release
```
A/D or arrows move, Space jumps. Collect three coins in each level and reach the flag.
At the first flag, movement freezes and a fresh Space continues to level2. Death+fresh Space
retries only the current level. At the second flag, fresh Space replays the entire campaign.
Transition/retry input is consumed; held jump does not continue. Escape/window close exits.
No enemies, powers, music, effects, pause/menu or save progression are promised by this slice.

## Content and architectural evidence
[Approved contract](../../specs/meadow-progression.md), [staged roadmap/audio shortlist](roadmap.md).
All seven originalPNG images and XML/license/provenance are reused; no new assets imported.
Source roles/scaling/tile composition and seven-image cache retained. Game-owned levelcontent
and CampaignGame choose progression; MicroEngine lifecycle/input/static collision/imageleases/
animation/renderer remain unchanged. No scene/level/audio/particle module added to Core.
Existing single-level LevelGame/nativecourse/replay contracts remain usable and tested.

## Observed author checks
- Three campaign tests failed against a stub, then original6+new3 passed.
- Two presentation tests failed (stubHUD and missing same-run instance animation reset), then11passed.
- Additional constructor/immutability/defaultAPI regression brings Platformer to12passing cases.
- Counting managedcanvas proves7acquisitions across levels/finalrestart and one release perlease;
  new activeinstance with RunNumber0 resets walk animation. Native does not claim leasevisibility.
- PlatformerNativeSmoke retains original atlas/sourcepixels/defaultbudget/decoder/course/reset,
  adds twolevel publicroutes<=2000updates each, distinct terrainpixel/captures, currentlevel
  death/retry/finalreplay equivalence, samewindow and seven productionCLIcases0/1/2.
- First new smoke marker assertion had a PowerShell comma-expression mistake; corrected to
  an independent required -notmatch clause. Same native proof rerun PASS, no assertion removed.
- Full delegated own verification:289/289, all3representative native harnesses and bothotherasset
  verifiers PASS;9/9Platformer provenancehashes andall11trackedassetbytes unchanged.
- Initial sandbox testhost hang resolved with same required command outside sandbox. No skipped
  required checks or remaining author failure. Independent frozen-commit gate follows.

Reproduce with checks/PlatformerNativeSmoke.ps1 and dotnet test
checks/Platformer.Contracts/Platformer.Contracts.csproj -c Release plus documented serial flags.
The other13baseline native harnesses retain prior accepted proof, not freshly rerun this slice.

## Subjective handoff after objective closure
Only new continuation/retry/final-replay clarity, two-level pacing and control feel need owner
experience. Do not repeat automated course/hash/CLI checks manually. This second level is a
bounded geometry variation to validate progression, not a claim of rich enemy/level variety.

![Continue](continue.png)
![Level2](level2.png)
![Final campaign victory](final.png)

## Independent objective closure — 2026-10-21
Frozen bce6e82 vs8976f97: launch_final_verify S1-S6 PASS, no blockers/corrections.
Fresh archive289/289managed, Platformer/Adventure/Rpg native and both otherassetverifiersPASS.
OriginalPlatformer9/9manifesthashes,all11assetbytesunchanged. Sevenmanagedleasesreleasedonce;
extra publicbehavior probe confirms ForCampaign clears the activepost-transition buffer.
Protected90entries baseline/candidate identical SHA256
C75A9ACF974FECF41AE090A993FDE39870FD2E9FB4C61FA5BFC8A08E6D8A1051.
All requiredchecks run, zero requiredskips; otherhistoricalnativeharnesses retainednotfresh.
Sandboxtempaccess failed initially; identical escalated run passed. Verifier's optional temporary
probe initially failed to compile due its own top-level ordering, corrected outside repository
and then passed; no product correction. Parentcurrentnative-NoBuild/CLI7PASS.
Root accepts objective MR-2/ME072/073. No owner subjective acceptance yet, no main/push.
The new playable two-level progression is the intentional stopping boundary; remaining phase
roadmap rows are not prepared tasks and do not authorize more mechanics before feedback.
No manual technical repetition requested. Source ae9754f(design/process),bce6e82(behavior/tests).

Risk item3: additive game-consumer contract change, default/static API and originalreplay
independently preserved. AssetReadiness reused license/art integrity with changedcontext proof;
no audio/particle provider or enemy asset selection inferred from preliminary research.
