# MR-7 preparation — staggered third course

Status: technically verified and objectively accepted under delegated authority; integrated human course-quality evaluation remains later mission evidence.

## Why this slice

Meadow Run currently has two courses. Level 1 is continuous terrain; level 2 contributes one gap,
one raised coin route and one avoid-only patrol. FR-003 requires approximately three genuinely
different levels, so a third identifier or shifted geometry would not satisfy the mission.

The selected slice uses three repeated gaps with alternating landing heights. It validates sustained
jump timing and route composition while keeping existing physics, camera and presentation contracts.
The game owns all geometry and progression. No engine level manager, scene system, vertical camera,
dynamic platform, new audio event or genre abstraction is needed.

## Asset Readiness

| Question | Evidence |
| --- | --- |
| Existing content | Kenney CC0 terrain/player/background/coin/flag/spike assets and MoxieCat CC0 transient WAVs already used by Meadow Run |
| Gap | None for this composition-only slice |
| Selection | Reuse existing bytes without download, conversion or modification |
| License/provenance | Existing `provenance.json`, `patrol-provenance.json`, audio `provenance.json` and NOTICE records remain authoritative |
| Formats/budgets | Existing PNG/XML and PCM16 mono 44.1kHz profiles; no provider budget changes |
| Contextual proof | Managed public-input routes plus native floor/platform/coin pixels; integrated human play later assesses rhythm and distinction |

## Readiness decision

The [MR-7 contract](../../specs/meadow-third-course.md) fixes observable geometry, progression,
regressions, edit surfaces, checks and rollback. Construction remains pending independent technical
review and delegated approval. Independent review validated the proposed geometry with existing
physics, found one omitted active regression surface (`SessionTests.cs`), and passed the targeted
boundary correction. ME-088 delivery reconciliation closed before construction.

## Implementation candidate — 2026-10-24

`LevelContent.Third` owns four ground segments, three 96-pixel gaps, low/high/low landing solids
and three landing coins. `CampaignGame` advances 1→2→3 and resets only after level 3. Presentation
reports `/3`. Engine, assets, audio events, providers, CLI options and other games are unchanged.

Test-first evidence:

| Check | Observed result |
| --- | --- |
| Platformer managed RED before source | 8 expected failures: level 3 rejected, two-level completion, `/2` HUD |
| Platformer managed GREEN | 40/40 passed; new content, no-jump defeat, public success route, progression, HUD and renderer coverage |
| Platformer native first run | New third-course pixels/progression passed; exposed one stale patrol scenario that still expected level 2 to end the campaign |
| Platformer native corrected run | PASS: three captured gap/platform views, campaign replay, prior patrol/session/audio/CLI behavior |
| Full serial managed suite | 330/330 passed: Adventure13, Desktop50, ExampleLaunch5, Hosting31, Loop159, Platformer40, RPG22, TwoD10 |
| Existing consumer native checks | Adventure and RPG smoke wrappers PASS, including their seven CLI cases |
| Reused asset checks | Patrol PNG and audio WAV provenance/hash verifiers PASS |

The initially recorded generic command `checks/VerifyPlatformerAssets.ps1` did not exist and exited
64. No product check failed; the contract was corrected to the two actual patrol/audio verifier
names and both passed. Risk: item 3, additive campaign semantics used by
session/presentation/native consumers.

## Independent closure

A separate verifier materialized baseline `da6d6da` plus only the 14 authorized candidate paths in
a fresh system-temp copy. `git diff --check`, focused Platformer40, serial330, all three game native
wrappers, both asset verifiers, rejected-command output/exit parity and before/after tracked-data
hash invariance passed. S1-S3/S5-S6 passed directly. The first pass found one S4 evidence gap:
native frames proved gaps/platforms but not level-3 coin pixels. The correction checks every coin
before collection by scanning its 24×24 framebuffer destination for at least 20 opaque colors from
the original `coin_gold.png`. An initial exact-center comparison correctly failed because 64→24
scaling/subpixel camera placement made one sample brittle; the bounded source-color scan passed.
Targeted independent recheck found no remaining findings. No correction changed game behavior.
