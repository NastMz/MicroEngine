# General capability catalog

IDs describe consumer needs, not modules, classes or package commitments.
CAP-001 has an approved/implemented bounded H0 slice; CAP-002/003/004 now have narrow verified H1-R1 slices; remaining behavior remains candidate except confirmed product constraints in [requirements](requirements.md). Promote concrete behavior through a spec and approval, not this inventory.

| ID | Capability | General responsibility / exclusions | Horizon |
| --- | --- | --- | --- |
| CAP-001 | Application lifecycle | Initialize/run/stop/cleanup; no game rules | H0 headless implemented/automatically verified; [spec](specs/lifecycle.md); owner accepted2026-10-15 |
| CAP-002 | Time and execution phases | Observable ordering, pause and deltas; no turn rules | H1-R1 fixed-step/clamp/drop/alpha slice verified; broader proposals remain |
| CAP-003 | Input actions | Digital/analog intent from devices; no character controls | H1-R1 digital held/ordered-edge/focus slice verified; analog/remapping not implemented |
| CAP-004 | 2D presentation | Sprites, text, cameras, ordering; no genre HUD | H1-R1 primitive canvas slice verified; sprites/text/cameras not implemented |
| CAP-005 | Entity composition | Identity, data and behavior composition; ECS choice open | First playable discovery |
| CAP-006 | Assets and ownership | Acquisition/sharing/release; no inherited format | First playable discovery |
| CAP-007 | Spatial queries and movement | Geometry, collision queries, optional kinematic mechanisms; no jump rules | Platformer validation |
| CAP-008 | Animation | Clips and playback; no combat-state library | Platformer validation |
| CAP-009 | Worlds and transitions | Content lifetime and handoff; no adventure progression | Adventure validation |
| CAP-010 | Maps | General spatial content; no prescribed tile format | Adventure validation |
| CAP-011 | Audio | Clips, independent playback, music and lifecycle | Playable milestone refinement |
| CAP-012 | UI primitives | Interaction/navigation/render needs; no battle menus | RPG discovery |
| CAP-013 | Persistence | Versioned consumer data and safe storage; no RPG save model | RPG discovery |
| CAP-014 | Physics dynamics | Optional solver/contact/filter semantics; no mandatory movement model | Need-driven candidate |
| CAP-015 | Diagnostics | Errors, traces, metrics and measurable budgets | Cross-cutting acceptance |
| CAP-016 | Extensions/configuration | Public seams enabling external libraries | Cross-cutting acceptance |

Metroidvania and modern platformer are stress scenarios, not new Core capability names.
No inventory row creates an implementation promise, dependency graph or technology selection.

H1-R1 evidence: [contract](specs/h1-runtime.md), [delivery](milestones/H1.md), [native provenance](evidence/h1-0/README.md). Full catalog entries are not globally fulfilled by narrow slices.

AV-1/ME-020/021 scoped discovery: consumer-authored procedural frame content and deterministic avatar pose/gait playback through existing primitive presentation. Not a general engine clip/player library or asset capability; no texture/format/ownership choice. Evidence and actual task state in backlog/H1 packet.

RE-1/H2.0 validates a second top-down consumer using existing CAP-002/003/004 slices plus consumer-owned room transition policy (CAP-009 discovery). It does not add reusable engine worlds/maps/scene lifetime or imply CAP-009/010 completion. Native up/down actions are additive Desktop mappings, not mandatory engine action vocabulary.

IM-1/H2.1 is consumer-owned manual interaction discovery under C-002: one strict-overlap marker and in-memory activation flag using existing input/presentation. No reusable engine interaction/scene/world/save capability or new action vocabulary selected.

TM-1/H3.0 proposed discovery covers consumer-owned menu selection/turn indication using current primitive rendering and console labels. It does not implement CAP-012 engine UI or CAP-013 storage, nor select font/text/scene APIs. Task-state/review evidence belongs to backlog and H3 packet.

CP-1 is consumer-owned versioned checkpoint discovery under CAP-013, not engine persistence or disk durability. Format/factories reviewed in ADR-0006; state/evidence in backlog.

CF-1 probes CAP-013 disk IO as optional consumer adapter referencing CP-1 only. Create-new/read, bounded payload, no existing destination replacement; not a general engine save provider or portability/durability promise.

CL-1 integrates consumer checkpoint adapters at executable startup/shutdown, not an engine storage API or compulsory game service. Same versioned data and no-overwrite contract; no native input additions.

NL-1 adds a bounded optional ILabelCanvas profile under CAP-004/012 for short printable-ASCII labels through provider default face. Primitive providers need not implement it; generic text/layout/fonts/assets remain candidates. No native dependency in foundation or game rules; ADR-0008 describes finite scope.

IMG-1 candidate (ME-039/040): CAP-004/006 bounded optional in-memory raster/profile and context-bound leases; CAP-016 public provider substitution retained. No file decoding, asset catalogs, atlas, mandatory image support or full CAP fulfillment. [Contract](specs/optional-images.md), [ADR-0009](decisions/0009-optional-image-resources.md); see backlog for current verification state.

SPR-1 candidate: CAP-004/006/008/016 bounded optional integer source-region profile, external avatar sheet selection driven by existing simulation state. No generic clips, animation state, loaders or atlas metadata in the engine. [SPR-1](https://github.com/NastMz/MicroEngine/blob/aa0243b577bcdee7c9e1b895325b8813fa9a3e45/docs/engineering/specs/sprite-regions.md), [ADR-0010](https://github.com/NastMz/MicroEngine/blob/aa0243b577bcdee7c9e1b895325b8813fa9a3e45/docs/engineering/decisions/0010-optional-image-regions.md); task state remains in backlog.

MAP-1 candidate probes CAP-009/010 through separate consumer-owned immutable fixed maps and camera/viewport translation. It does not fulfill a reusable map/scene/loader/camera API or choose a tile format. [MAP-1](specs/map-viewport.md); backlog/current-state record actual verification.

MAP-2 candidate: CAP-007/009/010 spatial navigation tested by explicit consumer-owned solid rectangles and swept-axis resolution in MapExplorer; no engine collision/map/physics abstraction or collision contract for other games. Decorative geometry remains separate. [MAP-2](specs/map-navigation.md).

TURN-2 candidate refines C-003/CAP-012/013: one game-owned deterministic completion, compatible versioned checkpoint outcome and existing optional labels. No engine encounter/battle/progression/storage subsystem. [TURN-2](specs/turn-outcome.md), [ADR-0011](decisions/0011-compatible-turn-outcome.md).
