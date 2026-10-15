# General capability catalog

IDs describe consumer needs, not modules, classes or package commitments.
CAP-001 has an approved/implemented bounded H0 slice; other entries remain candidates except confirmed product constraints in [requirements](requirements.md). Promote concrete behavior through a spec and approval, not this inventory.

| ID | Capability | General responsibility / exclusions | Horizon |
| --- | --- | --- | --- |
| CAP-001 | Application lifecycle | Initialize/run/stop/cleanup; no game rules | H0 headless implemented/automatically verified; [spec](specs/lifecycle.md); manual milestone pending |
| CAP-002 | Time and execution phases | Observable ordering, pause and deltas; no turn rules | Next candidate; [spec](specs/frame-execution.md) proposed |
| CAP-003 | Input actions | Digital/analog intent from devices; no character controls | First playable discovery |
| CAP-004 | 2D presentation | Sprites, text, cameras, ordering; no genre HUD | First playable discovery |
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
