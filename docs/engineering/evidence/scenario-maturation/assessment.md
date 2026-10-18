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
