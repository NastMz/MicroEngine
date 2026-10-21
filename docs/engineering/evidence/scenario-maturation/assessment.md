# Cross-scenario maturation assessment

The three bounded scenarios validate reuse without genre modules. The clearest repeated
infrastructure is consumer launch parsing, not missing battle, scene or tilemap machinery.
Next executable slice: [LAUNCH-1](../../specs/example-launch.md), shared example-only parsing.
This improves a reproducible foundation for further minigames, not their gameplay completeness.

## Actual reuse and ownership
| Capability | Meadow Run | Courtyard Key | Trail Duel |
| --- | --- | --- | --- |
| Hosting/Loop lifecycle, fixed updates, snapshots, report/cleanup | yes | yes | yes |
| Desktop provider and optional public canvas/image regions/labels | yes | yes | yes |
| TwoD ImageSet resource leases and FrameAnimation | yes | yes | yes |
| TwoD static AABB world/sweeps | platform geometry | gate/room geometry | no new world; existing Explorer |
| Existing consumer model | own LevelGame | own AdventureModel | RoomExplorer.Model.Explorer composition |

Source evidence: engine/Loop/Contracts.cs:30-85 and FrameLoop.cs:7-88;
engine/TwoD/ImageSet.cs:4-45, FrameAnimation.cs:4-35, StaticWorld.cs:6-55;
examples/Platformer/Program.cs:6-28, Adventure/Program.cs:6-35, Rpg/Program.cs:7-37.
Explorer reuse is consumer-to-consumer reuse, not evidence of an engine scene/map service.

Games own gravity/jump policy, coins/hazards/finish/replay (Platformer.Model/LevelGame.cs:5-60),
lever/gate/key/altar/progression (Adventure.Model/AdventureModel.cs:9-82), and encounter,
menu, HP, actions/outcomes/retry (Rpg.Model/RpgSession.cs:6-66). None belongs in Core by genre.
Art loading, dimensions/budgets, source-cell roles, animation reset, draw order and HUD/menu
composition remain explicit game infrastructure. Providers own native handles; ImageSet caches
and releases leases; execution owns provider acquisition/cleanup. No native API in game rules.

## Duplication versus justified variation
| Observation | Classification | Action |
| --- | --- | --- |
| Three identical smoke/failure flag parsers | real infrastructure duplication | extract one internal example support source; strict compatibility tests |
| Repeated fixed-loop options, counters and diagnostics | real duplication, already works | retain now; do not bundle generic runner/ownership changes into parser slice |
| ImageSet/animation objects and source dictionaries | use of shared contracts, not duplicate resource manager | retain per-game selection/ownership |
| Tile loops/source rectangles/HUD/menu layout | content-specific composition plus limited presentation tooling | no UI/map framework justified by current accepted scenarios |
| Game state/replay/transition branches | intentional distinct rules | do not unify as genre systems or force ECS/scenes |

## Proven boundaries, not invented defects
Current contracts are planar static AABBs, explicit authored image regions, optional labels
and provider-bound image leases. Map semantic metadata, dynamic bodies, depth/occlusion,
logical elevation, remapping, audio, save UX, export and other backends are not demonstrated
requirements of these accepted scenarios. Prior F014/F015 remain candidates; H3 did not
promote them. Integer source bounds/pixel fidelity do not prove artistic edge compatibility.
The art work improved explicit compound-region interpretation without changing Core.

## Next maturation horizon and promotion tests
1. Execute only LAUNCH-1: remove identical parsing while preserving independent launch,
   usage/exits, native cleanup, asset ownership and all gameplay. No new engine public API.
2. Reassess actual minigame startup/report duplication after this slice; any extraction must
   pin diagnostics, startup/partial failures and ownership, not introduce a backend preference.
3. Richer minigames may need title/pause/restart/settings, persistence or richer interaction;
   these are unprepared candidates, not tasks. Promote only with a concrete scenario and
   observable acceptance case; subjective product scope must remain explicit.
No fourth demo, more RPG combat/art or invented completion criteria is authorized by this assessment.

## Review/evidence
Read-only independent cross_scenario_map source assessment agrees; no source tests rerun for
this passive assessment. Baseline278 managed/three native art proofs retained. LAUNCH-1 will
record new RED/GREEN and native/CLI verification separately. Acceptance does not imply new
platform support or acceptance of absent future capabilities.

## LAUNCH-1 implementation evidence
Root observed five contract tests RED against an unimplemented stub, then five GREEN on the
actual linked parser. The same source is linked by all three executables, with usage strings,
loop/report/startup/cleanup and gameplay unchanged. No engine/dependency/model/asset changes.
Own verification delegated to cross_scenario_map for long suites:283/283 across eight projects,
three unchanged native scripts PASS, zero build warnings/errors. Initial sandbox VSTest failed
to connect to testhost; the same check outside sandbox succeeded. No required failure remains.
Independent frozen-commit final verification is still pending. No new subjective gate is needed
for identical CLI behavior. Broader runner/GUI extraction remains unprepared, not next by inertia.

## Independent closure — 2026-10-21
Frozen dc670cc against4453e85: independent launch_final_verify PASS on S1-S5.
283/283 managed tests, all three unchanged native scripts and both asset verifiers PASS.
21/21 baseline/candidate direct executable invalid-argument comparisons were byte-identical:
exit2, empty stdout, exact per-game Usage and no native startup. Existing valid cases and
current-culture integer grammar passed. No required check skipped; initial sandbox temp-access
failure resolved by identical escalated fresh git-archive verification, not weakened coverage.
Protected-tree SHA256:48ED9D1B7F8B122A5BCC57540F801F998CB24D4E253A2DF745F7D6C0746FCE40.
All Program suffixes from try through report/cleanup identical; models/assets/engine/dependencies
and native scripts unchanged. Parent current RpgNativeSmoke-NoBuild/CLI7 spot check PASS.
Root accepts objective LAUNCH-1/ME070. Risk item3 shared CLI, compatibility independently proven.
No new manual test is needed for unchanged behavior. Original H3 functional/visual acceptance
stands separately. Earlier final-verifier attempt returned no verdict because of a usage limit;
this new independent frozen verification supplies the missing proof, not an inferred PASS.

## Next work boundary
No remaining prepared implementation task. Shared startup/reporting is demonstrated duplication,
but a broader runner is not automatically necessary after this small extraction. The next
product-facing maturation candidate is a bounded reusable pause/restart flow in one existing
minigame, only after specifying pause-time/input/reset/ownership behavior and assessing actual
reuse; not an approved engine scene/UI capability or a new prepared task. Audio, save UX,
metadata/depth and additional backends retain their earlier concrete promotion requirements.
Do not add another technical demo or Pokemon rules merely to create work. This assessment and
LAUNCH-1 close the current executable horizon, with no new subjective experience to evaluate.
