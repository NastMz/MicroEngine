# MR-7 — Staggered third course

Status: approved 2026-10-24 by delegated root authority after independent technical review, one edit-surface correction and targeted recheck PASS. Construction remains separate.
Dependencies: MR-3 distinct authored content, MR-4 patrol, MR-5 session flow, MR-6 transient audio, ADR-0004/NFR-005 and ADR-0012/NFR-006.

## Outcome

Meadow Run gains a third campaign course that is materially different from the continuous first
course and the second course's single gap plus patrol. The new course is an authored sequence of
three gaps and staggered landing platforms. It reuses the existing fixed world, horizontal camera,
static collision, jump, coin, danger, goal, animation and audio-event behavior. It adds no engine
level system, dynamic platform, vertical camera, new provider contract or shared genre rule.

## Requirements

S1. `LevelContent.For(3)` exposes immutable third-course content inside the existing 1920×480
world. Ground segments leave three 96-pixel gaps at x=384..480, 768..864 and 1200..1296.
Landing solids at `(432,384,96,48)`, `(768,336,96,96)` and `(1248,384,96,48)` create an
alternating low/high/low jump rhythm without changing physics. The existing danger and goal
profiles remain at `(1536,412,48,20)` and `(1824,336,48,96)`; level 3 has no patrol.

S2. Level 3 has exactly three coins at `(456,352,24,24)`, `(792,304,24,24)` and
`(1272,352,24,24)`. A public-input route with intentional jumps can cross every gap, collect all
coins and complete within 2,000 updates. A public-input route that keeps moving right without
jumping falls into the first gap and reaches the existing defeated state.

S3. Campaign progression becomes level 1 → level 2 → level 3 → campaign complete. Pressing jump
on completed levels 1 or 2 consumes the transition input and creates the next fresh level. Pressing
jump after final completion creates a fresh level 1 campaign. Retry, pause/resume, campaign restart,
run numbering, coin reset and consumed-transition behavior remain unchanged.

S4. Presentation reports `Level n/3`. Native pixels prove three real floor discontinuities, the
staggered platforms and visible coins rather than accepting a third identifier alone. Existing
level-1 pixels/routes and level-2 gap, raised coin and patrol evidence remain unchanged.

S5. MR-6 event semantics are unchanged. Level 3 reuses only `Jumped`, `CoinCollected`, `Defeated`
and `LevelCompleted`; no new cue, queue, durable event or audio-provider behavior is introduced.

S6. Default `new LevelGame()` and existing level-1 static aliases remain compatible. Invalid level
numbers still throw. No Core, engine, dependency, asset byte, CLI option or other game changes.

## Asset Readiness

Ready by reuse. The existing Kenney CC0 terrain, coin, flag, spikes, player frames and background
cover every required draw; the MoxieCat CC0 WAVs cover existing transient events. Provenance,
formats, hashes, leases and adapter budgets do not change. No file is downloaded or transformed.
The course distinction is authored composition, so native pixel/route evidence and later integrated
human play evaluation are required; asset existence alone is not acceptance.

## Test-first construction and checks

- Observe RED for level-3 lookup/content, no-jump defeat, successful three-gap route, campaign
  `1→2→3→complete→1`, `/3` HUD and native third-course pixels before implementation.
- Preserve the existing level-1, MR-3 level-2, MR-4 patrol, MR-5 session and MR-6 audio tests.
- Focused managed command: `dotnet test checks/Platformer.Contracts/Platformer.Contracts.csproj -c Release -p:NuGetAudit=false -p:UseSharedCompilation=false -m:1 -nodeReuse:false`.
- Native command: `pwsh -NoProfile -File checks/PlatformerNativeSmoke.ps1` with new third-course
  assertions plus every existing assertion.
- Asset commands: `pwsh -NoProfile -File checks/VerifyPlatformerPatrolAssets.ps1` and
  `pwsh -NoProfile -File checks/VerifyPlatformerAudioAssets.ps1`; MR-7 adds no asset bytes.
- Run the repository's serial managed suite and existing Adventure/RPG native smoke checks before
  objective closure because campaign helpers and public presentation are shared consumer surfaces.

## Edit and rollback boundary

Authorized construction surfaces: `examples/Platformer.Model/LevelContent.cs`,
`CampaignGame.cs`; `examples/Platformer/PlatformerPresentation.cs`;
`checks/Platformer.Contracts/CampaignTests.cs`, `DistinctLevelTests.cs`,
`CampaignPresentationTests.cs`, `SessionTests.cs`, optional new `ThirdLevelTests.cs`, and
`Course.cs` only when the public-input driver needs the three-gap route;
`checks/Platformer.NativeProbe/Program.cs` and
`checks/PlatformerNativeSmoke.ps1`; this specification, Meadow evidence, backlog, roadmap,
current-state and mission log. No engine, other-game, package or asset path is authorized.

Rollback removes level-3 content/tests/native samples and restores the two-level transition/HUD;
it does not revert MR-3 through MR-6. Risk: item 3, additive shared consumer campaign semantics.
Independent verification is required after construction.

## Approval evidence

An independent reviewer checked the exact geometry against current movement/static-collision code
in a fresh scratch copy. A simulated public-input route collected all three coins and completed in
500 updates with four intentional jumps; the no-jump route fell into the first gap after 124
updates. S1-S6 and Asset Readiness were implementable without an engine/shared-contract change.
The first review found one deterministic blocker: the two-level final-completion assertion in
`SessionTests.cs` would need updating but was outside the original edit boundary. The boundary now
includes that file; targeted recheck returned no findings. Approval covers this contract only, not
source implementation or subjective course quality.
