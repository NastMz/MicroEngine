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
