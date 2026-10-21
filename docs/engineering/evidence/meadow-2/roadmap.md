# Meadow Run minigame maturation roadmap
Owner direction2026-10-21: evolve an accepted technical scenario into a small2-3level game.
Only first slice below is executable after review. Other rows are candidates, not task packets.

| Increment | Value to validate | Content/infrastructure preparation | Promotion condition |
| --- | --- | --- | --- |
| MR-2 two-level progression | continue/retry/final replay without runtime restart | existing Kenney art, game-owned campaign, no engine changes | current next executable scope |
| Simple enemy interaction | dynamic actor behavior/contact beyond static world | choose actual enemy frames from original pack or coherent CC0 source; define patrol/contact/reset | MR-2 experiential feedback and bounded gameplay contract |
| Menu/pause/HUD flow | complete session control, not another tracer | existing labels first; define time/input/focus/restart rules before UI generalization | concrete user flow and optional capability need |
| Feedback/animation and sound | make events readable and distinguish outcomes | Asset Readiness for existing poses, selected effects/SFX; cohesive optional audio provider ADR if needed | concrete events/ownership/stop/mute contract, no game-native calls |
| Optional items/powers, third level, music | test progression variety and sustained play | select only what gives a distinct tested mechanic; music format/loop/mix evidence | earlier learning shows value; no checklist padding |

Ordering beyond MR-2 is provisional; audio need not wait for arbitrary polish but must have
its own justified infrastructure/content packet. No giant particle/audio/animation manager.
Game owns rules, authored geometry and progression. Engine candidates must demonstrate reuse,
minimal replaceable contracts, ownership/failure behavior and consumption benefit. After Meadow,
apply findings to Adventure/RPG rather than promoting platform rules into Core.

## Audio shortlist — page-level investigation2026-10-21
| Candidate | Verified publisher declaration / advertised content | Fit and remaining evidence |
| --- | --- | --- |
| [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) | CC0,100files, release1.0 | UI feedback candidate, not automatically jump/enemy feedback; inspect archive/formats/listen at audio horizon |
| [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds) | CC0,130files, release1.0 | foley/impact candidate; no asserted match to current cartoon art without listening |
| [MoxieCat8-bit Platformer SFX](https://opengameart.org/content/8-bit-platformer-sfx-0) | CC0,WAV+FamiTracker source advertised | retro sound candidate; individual files/gain/event fit to inspect before import |
| [Platformer Chiptunes](https://opengameart.org/content/platformer-chiptunes) | Guy G.Gamerson,CC0,stage MP3 files | music candidate; inspect actual format/loops/licensing/file integrity and listen before selection |

No audio archive downloaded/imported or heard, no audio backend selected. Page declarations
are preliminary availability/license evidence, not final individual-file readiness. Current
MR-2 needs no new image/audio/VFX files. Existing pack/license/hash evidence remains authority.

## Differentiated composition after MR-2 feedback
MR-2 accepted onlytransition/retry/restart; similar shiftedcourses did not validate variety.
Next [MR-3](../../specs/meadow-distinct-levels.md) separates privateauthored LevelContent from
one controller/renderer, preserving level1 and giving level2 a realfloor gap/elevatedcoin.
No editor, generic enginelevelsystem, newassets or per-levelphysics. Enemy/audio/menu needs
remain staged candidates; the nextmanualgate must be significant gameplay, not datarefactoring.
