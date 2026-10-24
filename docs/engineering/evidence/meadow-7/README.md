# MR-7 preparation — staggered third course

Status: reviewed and approved preparation; no MR-7 source implementation or acceptance is claimed.

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
boundary correction. The current MR-6 source is unchanged; MR-7 construction is ready after
ME-088 delivery reconciliation closes.
