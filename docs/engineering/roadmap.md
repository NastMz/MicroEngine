<!-- Current authority:2026-10-15 technical delegation supersedes earlier approval-only restrictions; see autonomy.md. Historical dated evidence is not rewritten. -->
# Evolutionary roadmap

H0 is accepted by the owner (2026-10-15). Bounded H1/H2/H3 tracers and persistence/labels are technically verified; full playable-milestone acceptance is not inferred. IMG-1 is positively evaluated and integrated. SPR-1 is technically verified and positively evaluated; ME-054 completed current-main reconciliation with independent226test/13native proof. MAP-1 is positively evaluated and integrated independently of SPR-1. MAP-2 is positively evaluated and integrated. TURN-2 is positively evaluated and integrated. MAP-3 and GOAL-1 are positively evaluated and integrated. GOAL-1 validates a complete replayable external platformer course through an opt-in goal/reset, not engine game-rules or a level manager. The prototype remains at [legacy-final](history.md).

## Current executable horizon

[Backlog](backlog.md) is the only task-state source. ME-039/040 complete IMG-1 with independent technical proof. ME-041/042 complete SPR-1 optional source regions and equivalent external avatar sheet presentation with independent193test/7native/parity proof. ME-043/044 complete the reviewed MAP-1 contract and model/presentation; ME-045 completed independent201test/9native/public-probe verification and objective technical acceptance. Owner positively evaluated MAP-1 navigation; this does not accept fullH2. ME-046/047 complete MAP-2 with independent200test/9native/navigation proof. Owner positively evaluated MAP-2 navigation, not fullH2. ME-048/049 complete TURN-2 with independent206test/10native/compatible checkpoint proof; owner positively evaluated completion clarity, not fullH3. ME-050/051 complete MAP-3 with independent212test/11native/publicproof. Owner positively evaluated switch/effect clarity, not fullH2. ME-052 reviewed GOAL-1; ME-053 completed independent 218test/12native/public-probe/hash verification. GOAL-1 is technically accepted; owner positively evaluated goal clarity/reset feel; full H1 acceptance is not inferred. No independent ready task remains; refine the next concrete roadmap need from evidence rather than inventing distant capabilities. Do not assume a full asset/animation framework. See [current state](current-state.md).
An authorized future development session follows [execution](execution.md) and may continue
independent ready work. No background scheduler is installed; H0 is implemented, not a playable game.

## Milestones and candidate validation scenarios

| ID | Scenario/outcome | Candidate capabilities | Exit criteria to refine | Status |
| --- | --- | --- | --- | --- |
| H0 | External application starts/runs/stops safely | CAP-001 headless slice only | Lifecycle/failure traces, runnable public consumer and31 tests; explicit owner acceptance | Verified / owner accepted2026-10-15 |
| H1 | Simple Mario-inspired platformer | CAP-002/003/004/005/006/007/008 | Movement, obstacles, jump and animation in an external game; game owns jump rules; manual playability | First bounded playable scenario accepted2026-10-18 |
| H2 | Zelda-inspired top-down adventure | CAP-009/010 plus refined presentation/input | Exploration and transitions without engine-owned combat/progression; manual interaction | ADV-1 technically verified; subjective pending |
| H3 | Pokemon-inspired RPG | CAP-012/013 plus validated shared capabilities | Exploration, menus and turn interaction; game owns battle/turn rules; manual flow | Candidate |
| H4 | Metroidvania / modern platformer stress | Needs discovered from earlier milestones | Greater content/state/movement demands with evidence, not presumed Core modules | Deferred candidate |

H0 is an engineering prerequisite, not proof of game generality. H1-H4 are a proposed
refinement sequence, not approved game scopes, dates or capability commitments.
Select/refine each milestone only when prior evidence identifies necessary work.
Use [capability catalog](capabilities.md), [milestone template](templates/milestone.md)
and [manual delivery](milestone-delivery.md); no exhaustive future task list.

## Decisions still open

Future platform support/package distribution; broader native backends and execution beyond H1-R1;
entity model/storage; scenes/worlds; rendering, input, audio, physics and asset formats.
Decide only what a concrete increment needs. No ECS/DI/backend/solver, editor, hot reload,
networking or 3D commitment is inherited or approved here.

## Refinement

[Findings](findings.md) captures evidence and debt without adding arbitrary work.
Move concrete approved needs into bounded backlog packets with dependencies. Requirement,
ADR approval, task completion and milestone acceptance are separate records.

## Next refinement only

Proposed [H1.0 tracer](specs/h1-first-playable.md) is a subset, not completion of [H1](milestones/H1.md). ME-005/006 prepare scenario/options; ME-007 prepares [ADR-0003](decisions/0003-next-validation-tracer.md). ME-008 technical packet awaits independent review and delegated ADR-0003 approval under autonomy.md. H1.0 construction is now technically verified under accepted H1-R1; no broader H1 construction task is ready without further bounded specification.

H1.0 technical slice accepted2026-10-15 by delegated authority after independent verification; subjective experience pending. Full H1 remains candidate. Next refinement: static obstacle/spatial interaction and animation/content based on delivered tracer evidence/feedback; no distant tasks generated.

## Active bounded refinement2026-10-16

Baseline H1.0 subjective feedback received under ME-012; optional consumer jump buffer delivered under ME-016/017, without full-H1 acceptance. Owner authorized integrating the verified baseline into main (81de6a9). Next ME-018/019: [PO-1](specs/platformer-obstacles.md), consumer-owned static solids/axis movement. Specification review precedes construction; no engine solver, reusable geometry contract, coyote time, animation or broader level system inferred. Later H1 content/animation remains candidate, not a parallel speculative task list.

## Animation horizon2026-10-16

After positive PO-1 owner feedback, ME-020/021 refine [AV-1](specs/avatar-animation.md): consumer-only procedural idle/walk/air/facing and deterministic playback through existing primitive canvas. No texture/resource/provider method or generic animation library. This narrow visual demonstration does not fulfill all CAP-004/006/008 or accept full H1. Broader asset/clip needs remain candidates until concrete requirements justify their contracts.

## Next independent consumer2026-10-16

Positive AV-1 owner feedback recorded, full-H1 human acceptance not inferred. [RE-1](specs/room-explorer.md) defines H2.0 two-room exploration through the same public Loop/Desktop, separate from FirstPlayable.Model. ME-022 reviewed; ME-023 additive native up/down actions precede ME-024. [H2 packet](milestones/H2.md) separates objective tests and subjective navigation. No engine world/scene/map/asset abstraction or broad Zelda scope; later content is not yet prepared.

## Bounded manual interaction2026-10-17

H2/C-002 includes interaction beyond RE-1 traversal. ME-026/027 [IM-1](specs/marker-interaction.md) probe one source-authored room-local marker using existing Space/jump edge, no Desktop binding/engine interaction/NPC/dialogue/world/persistence API. Positive RE-1 delivery and integration policy are in main8ff1f0a; marker technical/subjective states are distinct in backlog/H2 packet. H3 menus/turn behavior remains candidate, not silently prepared here.

H3 preparation2026-10-17: H2 traversal/manual-marker slices positively evaluated/integrated; ME-028/029 [TM-1](specs/turn-menu.md) refine only first menu/turn flow via a new external consumer, private composition of existing exploration. No additional H2 gameplay or premature engine text/UI framework. [H3 packet](milestones/H3.md) separates technical/subjective evidence; no full-H3 or persistence commitment.

CP-1 next horizon2026-10-17: owner continuation refines C-003/CAP-013 into [consumer checkpoints](specs/consumer-checkpoint.md), reviewed ADR-0006. Only versioned bytes/validated state reconstruction; no engine storage, disk IO, battle or UI expansion. ME-030/031 state in backlog.

CF-1 horizon2026-10-17: [checkpoint files](specs/checkpoint-files.md)/ADR-0007 add optional consumer create-new/read storage after verified CP-1. No replacement/autosave/game controls/engine storage. Prior CP-1 and new CF-1 remain on stacked task branches until applicable main-delivery authority.

CL-1 horizon2026-10-17: [checkpoint startup/exit](specs/checkpoint-launch.md) composes verified CP-1/CF-1 for actual process restart in TurnExplorer, optional flags only. No autosave/overwrite/storage system/game rule expansion; current task states in backlog.

NL-1 horizon2026-10-17: [optional labels](specs/optional-labels.md)/ADR-0008 refines real TurnExplorer console-only names into optional cohesive presentation support. Earlier positive feedback not a defect classification; ICanvas and fallback retained. Not generic UI/font resources/localization/backends. Task/evidence state in backlog.

Next-horizon assessment2026-10-17: all prepared functional tasks are complete; ME-054 closes the remaining evaluated sprite delivery. No next independent capability has a concrete consumer requirement yet. Do not generate generic atlas/clip/decoder systems, another cosmetic tracer or speculative persistence/mechanics. A subsequent horizon needs a bounded C-001/C-002/C-003 validation objective; positive slice feedback alone does not accept full H1/H2/H3.

## Selected platformer horizon2026-10-18

Owner selected platforms→Mario-inspired playable validation before later Zelda/Pokemon. [KPF-1](specs/kenney-platformer.md), ME055/056/057, is only this next executable horizon: one real Kenney CC0 level, coins/hazard/platform progression/finish/replay. ADR0012 classifies optional general utilities versus game rules. No speculative distant task expansion; new actual visual/control feedback gates subsequent gameplay refinement. This owner request resolves the earlier unselected-objective stop. No remote/main authorization for this horizon.

## Accepted platformer and selected topdown horizon2026-10-18

Owner normal/death-reset/win/visual/control feedback plus public-boundary audit closes the first bounded H1/C001 Meadow Run scenario. No menu/audio/polish expansion or exhaustive CAP completion. [ADV1](specs/adventure.md), ME059/060, selects only two-area topdown movement, lever/gate, key/altar and replay using unchanged TwoD/Loop/Desktop. No new engine capability, Zelda module, combat/persistence/inventory framework or generic scene manager. Continue ready scoped work to new subjective navigation/interaction/flow gate; no remote/main delivery inferred.

ADV1 closure: ME058/059/060 complete after independent255tests/15native/publicreuse/hashPASS, engine unchanged. New subjective two-area navigation/interaction/flow gate ready; first Meadow Run scenario accepted/closed, broader futurecapacities unchanged. Do not refine more combat/state/rendering/Pokemon work before the new playable evidence identifies a need; no independent ready task now.

ADV-ART-1 is a bounded C-002 presentation refinement, not a new gameplay horizon: [licensed adventure art](specs/adventure-art.md), [evidence](evidence/adv-1/art.md). It reuses existing image/atlas/animation/scaling capabilities without widening H2 or adding Core game/style dependencies. Current technical state remains in backlog.

Owner validated the ADV functional cycle. ADV-ART-2 is one bounded visual composition pass within C-002, not permission to extend this example indefinitely. [Content scope/limits](specs/adventure-composition.md); after technical proof, only new composition needs subjective evaluation. No new combat/persistence/editor horizon inferred.

## Tileset learning checkpoint2026-10-18
Owner asks to retain the functional scenario and learn from its visual limitations, not another art pass. [F014/F015 assessment](evidence/adv-1/tileset-assessment.md) classifies composition/atlas interpretation as primary; no new rendering API requirement established. Tile metadata/layers/depth/logical surfaces remain need-driven candidates, not H3 prerequisites. ME063 records the findings. No remaining ready implementation packet exists; H3's next bounded consumer objective must be refined against existing menu/turn/checkpoint tracers rather than adding arbitrary mechanics or polishing Courtyard Key indefinitely. No new visual approval gate is imposed on independent planning, and fullH2 acceptance is not inferred.

## Current selected horizon — RPG-1,2026-10-18
Owner closes tileset evaluation and selects a minimal H3 technical RPG. [RPG-1](specs/rpg-encounter.md)/ADR0013 approve two-room public Explorer reuse, explicit encounter, game-owned Attack/Guard/Retreat, victory/defeat/retreat result and exploration resumption. ME064/065/066 are the only executable horizon; no full Pokemon, persistence extension, new engine UI/battle/world/map/depth system or tooling implied. Independent designPASS; technical implementation/proof and new subjective flow gate follow. Current owner additionally permits mature localmain integration without routine approval; product acceptance remains separate.

RPG-ART-1 now precedes replacement presentation: [candidate/selection evidence](evidence/rpg-1/assets.md), [bounded content contract](specs/rpg-art.md), ME067/068. It preserves the already working RPG1 encounter and selects matchedTinyTown/Dungeon+retainedanimatedhero, not sixstyles or new mechanics. Future interiors/NPCs/status/effects are resource inventory only, not implementation scope. No semanticmap/depth/editor/tools work reopened.

## Post-scenario maturation — 2026-10-18
H3 bounded RPG is owner-accepted functionally and visually: [record](evidence/rpg-1/acceptance.md).
[Transverse assessment](evidence/scenario-maturation/assessment.md) separates actual reuse,
consumer policy and demonstrated duplication. Next executable horizon is [LAUNCH-1](specs/example-launch.md),
example-only compatible parsing; no genre modules or speculative completeness features.

## Phase2: complete minigame validation2026-10-21
Owner accepts cross-scenario assessment/LAUNCH-1 and selects Meadow Run maturation toward2-3levels,
enemies/progression/feedback/UI/audio as incremental objectives, not simultaneous construction.
[Phase roadmap/assets](evidence/meadow-2/roadmap.md), [MR-2 first packet](specs/meadow-progression.md).
Permanent [Asset Readiness](asset-readiness.md) precedes content-dependent implementation.
Only two-level game-owned progression is prepared now; no engine scene/UI/audio API by inference.

MR3current2026-10-21: [distinct authored gap/elevatedroute](evidence/meadow-3/README.md) is
technically verified293tests on feature, with unchangedengine/controller/campaign and reusedart.
MR2acceptance is onlyprogression; do not equate identifierswithvariety. One specificdifferent
challenge now exists, not a demonstrated richcampaign/editor. Nextnewmanualgate is its actual
playability; datarefactor alone needed no ownerapproval. Enemy/audio/UI remain futurecandidates.
