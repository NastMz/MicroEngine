<!-- Current authority:2026-10-15 technical delegation supersedes earlier approval-only restrictions; see autonomy.md. Historical dated evidence is not rewritten. -->
# Continue MicroEngine from a fresh session

## Read and establish scope

1. Read [index](README.md), [manifesto](manifesto.md) and [requirements](requirements.md).
2. Read [accepted decisions](decisions/README.md), [roadmap](roadmap.md) and approval evidence.
3. Identify authorized work and affected IDs. Repository transition is approved;
   new engine implementation still needs approved scope and contracts.

No previous chat or external memory is required. [History](history.md) explains access
to the retired prototype; do not restore it over active work by default.

## Propose and implement

Use the [ADR template](templates/adr.md) for significant decisions and the
[capability template](templates/capability.md) for concrete behavior. Do not require
these for typos. Keep genre rules in consumers and test public behavior/integration.
Update affected records rather than creating duplicate trackers.

## Checks

The current runner and exact serial restore/build/test commands are in [toolchain](toolchain.md). Historical prototype workflows remain in Git history only.
For documentation, read back changed files, check relative links, status consistency
and `git diff --check`. Inspect untracked files explicitly.

## Close work

Report actual evidence, affected IDs, failed/skipped checks and next work. Follow
[document policy](document-policy.md). Delivery authority is defined in [autonomy](autonomy.md), including normal task-branch publication and the standing positive-feedback main-integration rule. Do not infer PR/release authorization;
use Conventional Commits without AI attribution.

## Start an agent development session

Ask the agent to develop eligible ready tasks. It reads the [backlog](backlog.md),
[execution](execution.md) and [autonomy](autonomy.md), claims one task and continues
independent work after verified closure. Technical architecture approval is delegated under autonomy.md after independent review. Select actual ready work from the backlog, including reviewed construction packets; refine the next concrete roadmap need when appropriate. Publication and main integration follow autonomy.md, not task completion alone.

## Branch naming and delivery

Use feat/<topic>, fix/<topic>, docs/<topic> as appropriate, not codex/*. Establish the actual checkout with `git branch --show-current` and `git status --short`; do not assume a historical branch is current. Push without force to the same task branch on authorized origin using configured Git authentication. Preserve unrelated local work and verify outgoing commits. Apply the standing positive-feedback rule in autonomy.md for evaluated verified main delivery; no PR/release/history rewrite or remote deletion follows by inference.

## H0 authorized development

ADR-0002 delegates technical H0 choices. See toolchain.md for exact restore/build/test commands and current-state.md for observed checks/limitations. Hosting and contract tests now exist; the external probe/H0 was accepted by the owner on2026-10-15 based on existing evidence; no manual rerun is required. Do not use historical planning-only statements above as current execution authority.

## H2.0 exploration tracer

When RE-1/ME-024 delivery is recorded in backlog, run:

~~~powershell
dotnet run --project examples/RoomExplorer/RoomExplorer.csproj -c Release
# Automated native lifecycle/CLI proof, not manual playability:
pwsh -NoProfile -File checks/RoomExplorerSmoke.ps1
~~~

Arrows/WASD move. Cross the cyan passage at the Courtyard's right edge or Hall's left edge, with the whole body aligned inside the opening. Room changes are printed to stdout. Escape/window close exits. --smoke N(1..600) stops after Npresentations; --fail-update injects callback failure; normal/failure/usage exits0/1/2. No extra SDK version, native backend or package is introduced by this consumer. Windows native validation only; headless Loop model tests remain native-free.

## H3.0 menu/turn tracer

~~~powershell
dotnet run --project examples/TurnExplorer/TurnExplorer.csproj -c Release
pwsh -NoProfile -File checks/TurnExplorerSmoke.ps1
~~~

Arrows/WASD explore; Space at the Courtyard marker opens the menu. Up/down selects top Advance (one square) or bottom Return (two squares); Space confirms. Menu pauses movement; Advance moves the cyclic four-pip indicator, Return resumes exploration on the next update. Console names options/traces; no in-window text API. Escape/window close exits. --smoke N(1..600), --fail-update and normal/failure/usage exits0/1/2 match the other tracers. No new package/native backend/SDK constraint. Task/evidence state in backlog ME-028/029 and milestones/H3.md.

## Consumer checkpoint file checks

CF-1 uses owned unique TEMP directories only; no user save files. Focused automated command:

~~~powershell
dotnet test checks/Loop.Contracts/Loop.Contracts.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false --filter FullyQualifiedName~CheckpointFileTests
~~~

Optional examples/TurnExplorer.Storage references only the consumer model. CheckpointFile.CreateNew(path, session.Capture()) refuses existing destinations; CheckpointFile.Read(path) validates CP-1 bytes, then TurnSession.Restore creates a new session. No game keybindings/automatic paths/overwrite API. See [CF-1](specs/checkpoint-files.md).

## TurnExplorer checkpoint launch

Opt-in syntax: --load PATH restores before opening the window; --save-new PATH writes after successful exit, never replaces an existing file. Omit both for unchanged behavior. Filesystem failures exit1; invalid syntax exits2; update/cleanup failure skips saving. No IO in frame callbacks. Automated evidence command (owned TEMP files only):

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File checks/TurnExplorerPersistenceSmoke.ps1
~~~

[CL-1](specs/checkpoint-launch.md) defines exact traces/error/ordering; no manual repetition of this harness is required.

## Optional label verification

NL-1 leaves ICanvas unchanged. Consumers may type-test ILabelCanvas; unsupported providers keep primitive presentation. Desktop provides short ASCII labels using its default face. Native pixel/readback proof:

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File checks/LabelNativeSmoke.ps1
~~~

The probe owns its window/images, samples RGB not opaque alpha, and closes/unloads them. It proves drawing, not subjective legibility or other platform support. In TurnExplorer, open the marker menu to see Advance/Return names; no new controls. See [NL-1](specs/optional-labels.md), [ADR-0008](decisions/0008-optional-label-canvas.md) and backlog for actual verification state.

## Optional image verification (IMG-1)

Images are opt-in through `IImageCanvas`; primitive-only providers remain valid. See [IMG-1](specs/optional-images.md) for pixel format, context-bound lease ownership and Desktop budgets. No file loader or mandatory asset manager is introduced.

Run the serial Release suites from toolchain.md, then `powershell -File checks/ImageNativeSmoke.ps1`. The native probe builds its separate executable, checks actual RGB/alpha/point-sampling pixels, releases resources explicitly and through window teardown, and requires exit0 and clean close. A PASS is technical evidence, not aesthetic acceptance. For subjective appearance only, run `dotnet run --project examples/FirstPlayable -c Release`: the existing moving avatar now has a tiny turquoise/white/dark badge; movement and procedural poses are unchanged.

## SPR-1 verification — separate unintegrated feature branch

This section applies only on feat/sprite-regions. Its source-region probe and renderer are not included in main or MAP branches; the immutable historical contract is linked below.

Run `powershell -File checks/RegionNativeSmoke.ps1` after the serial managed suites. The separate probe selects the interior of a colored source image and checks exact framebuffer edges/orientation, point sampling, transparent overlay and half-alpha blending, then requires clean resource/window teardown. Existing ImageNativeSmoke proves whole-image behavior independently. Both are automated technical checks, not owner tasks.

FirstPlayable now selects eight source-authored cells for existing idle/walk0/walk1/air/facing states. `dotnet run --project examples/FirstPlayable -c Release` is only useful for subjective presentation feedback; no new controls or gameplay rules. Providers omitting IImageRegionCanvas retain the previous primitive avatar and optional whole-image badge. See [SPR-1](https://github.com/NastMz/MicroEngine/blob/aa0243b577bcdee7c9e1b895325b8813fa9a3e45/docs/engineering/specs/sprite-regions.md) for strict source bounds and lifetime semantics.

## Explore larger maps (MAP-1)

Run `dotnet run --project examples/MapExplorer -c Release`. Move with arrows/WASD; the camera follows the gold24x32 player through a1280x720 Meadow and a960x540 Ruins in a640x360 window. Colored landmarks are decorative, not collision walls. In Meadow move right at initial vertical position until the cyan edge passage; in Ruins move left through its passage to return. Escape/window close exits. Camera follows/clamps without new controls. This separate sample has no marker interaction, save feature or combat.

Technical checks are agent-owned: serial managed suites; `pwsh -NoProfile -File checks/MapExplorerSmoke.ps1` checks native lifecycle/callback failures/CLI exits, and `pwsh -NoProfile -File checks/MapViewportNativeSmoke.ps1` reads actual scroll/clipping/player/portal pixels. Scripted states do not automate physical-key playability. Owner feedback, if requested, concerns only whether following the player, locating passages and navigating between different map sizes feels clear; no requirement to repeat technical checks. [Contract](specs/map-viewport.md).

## Solid navigation (MAP-2)

MapExplorer now has distinct gray barriers that block motion; existing colored landmarks remain decorative. At the initial Meadow position, hold right until stopped at the wall, move up above its top (or below its bottom), pass it, then return toward the cyan right-edge passage. The camera follows and clamps as before. In Ruins, the left passage returns to Meadow. Engine physics is not enabled or required: this is consumer-owned source-authored navigation.

`pwsh -NoProfile -File checks/MapNavigationNativeSmoke.ps1` automates a scripted contact/detour/portal/return journey, state/camera assertions,22actual RGB samples and native cleanup. Existing viewport and process harnesses still run. Subjective handoff only: assess whether gray walls are distinguishable from decorative landmarks and wall sliding/detours feel predictable. Do not repeat the agent-run technical checks. [MAP-2 contract](specs/map-navigation.md) explicitly describes the Y-then-X policy, not a continuous 2D solver.

## Durable turn completion (TURN-2)

Run `dotnet run --project examples/TurnExplorer -c Release`. Move to the courtyard marker, press Space to open, then make four fresh Space confirmations on Advance. The menu closes, all four pips become green and "Completed" appears when labels are supported. Return leaves pending progress unchanged. A completed encounter cannot reopen; movement/room passages remain active. This is one consumer-owned outcome, not a combat/stat system.

Existing optional `--save-new PATH` writes a new24byte v2 file after clean exit; `--load PATH` accepts old v1 and new v2 without rewriting the source. Never overwrite/delete an old save just to upgrade; use a distinct destination. Valid v1 Encode is unchanged; old binaries may reject v2, so downgrade requires a compatible reader. Native/managed technical proof is agent-owned: `pwsh -NoProfile -File checks/TurnOutcomeNativeSmoke.ps1` checks scripted public-input completion, actual green pips/Completed glyph/no overlay, clean close and temp-file save/reload; existing persistence harness now checks24cases. Neither harness injects physical gameplay keys. Subjective-only feedback concerns whether completion is clear and the one-shot flow feels coherent. [TURN-2 contract](specs/turn-outcome.md).

## Switch-controlled map shortcut (MAP-3)

Run MapExplorer as before. Move right toward the gray Meadow barrier; find the blue switch just to its left. Stand overlapping the blue rectangle and press Space once: the switch turns green, the gate disappears, and the HUD says "Gate open" when labels are supported. You can now cross the former wall directly. Existing upper/lower detours still work. Gate state survives Ruins/Meadow visits during the run; a new process starts closed. Ruins' separate gray barrier never opens. There is no map save feature in this increment.

Agent-run `pwsh -NoProfile -File checks/MapSwitchGateNativeSmoke.ps1` verifies closed/open pixels, old blocking-view preservation, direct crossing, portal return, Ruins wall and fresh-session reset alongside all previous viewport/navigation samples (28RGB checks). No physical-key automation is claimed. Subjective feedback only: is the switch discoverable, is its effect clear, and does the newly opened shortcut feel coherent? [MAP-3](specs/map-switch-gate.md).

## Complete and replay the platformer (GOAL-1)

Run `dotnet run --project examples/FirstPlayable -c Release`. Move with Left/Right or A/D, use Space to jump onto/over the obstacle, then reach the cyan goal at the right while grounded. The goal turns green, the avatar becomes idle and movement freezes. Release Space and press it again to reset at the original start; the reset update does not also jump or move. The same obstacle/buffer configuration remains, ready for another run. Existing GameModel users do not gain goal behavior unless they explicitly opt in.

`pwsh -NoProfile -File checks/GoalNativeSmoke.ps1` is agent-run proof: bounded source-controlled obstacle-to-goal input, cyan/green goal and HUD/avatar pixels, held-input freeze, consumed reset, neutral next step and identical replay/native close. It does not automate physical gameplay keys. Subjective-only feedback concerns goal discoverability, whether completion is clear, and restart feel. [GOAL-1](specs/platformer-goal.md). No scores, lives, saves or level manager are introduced.

## Integrated optional sprites

FirstPlayable selects the eight equivalent source-authored avatar cells through optional IImageRegionCanvas; primitive/whole-image fallback remains supported. Goal completion and consumed Space reset remain unchanged. Automated `pwsh -NoProfile -File checks/RegionNativeSmoke.ps1` verifies native crop/orientation/RGBA/lifetime; GoalNativeSmoke verifies course/reset with actual sprites. See [SPR-1](specs/sprite-regions.md). No repeated owner checks are required after positive feedback.

## Real-art platformer (KPF-1)

Run `dotnet run --project examples/Platformer/Platformer.csproj -c Release`. See [controls, evidence and owner-only evaluation](evidence/kpf-1/README.md). New automated checks: TwoD.Contracts, Platformer.Contracts and `pwsh -NoProfile -File checks/PlatformerNativeSmoke.ps1`, in addition to all existing regressions. Pack assets copy to output; execution does not download resources. Windows validation only. No SDK/package changes; latest scope permits local commits, not push/main.

## Courtyard Key top-down adventure (ADV-1)

Run `dotnet run --project examples/Adventure/Adventure.csproj -c Release`. WASD/arrows move; fresh Space interacts at lever/key/altar and replays after victory; Escape closes. [Full run/reuse/subjective packet](evidence/adv-1/README.md). Automated new checks: Adventure.Contracts plus `pwsh -NoProfile -File checks/AdventureNativeSmoke.ps1`; retain all earlier suites/harnesses. Asset files are local output content, no runtime download. No new engine dependencies or APIs.

ADV-ART-1 refreshes only Courtyard Key art using original ArMM1998 CC0 sheets and retained Kenney interaction icons. Same Adventure run command/controls; no engine/model change or runtime download. Run `pwsh -NoProfile -File checks/VerifyAdventureAssets.ps1`, Adventure.Contracts and AdventureNativeSmoke alongside existing regressions. [Selection, provenance caveat, evidence and subjective-only handoff](evidence/adv-1/art.md).

ADV-ART-2 keeps the same Adventure run command/controls and adds bounded authored room composition only. [Comparison, technical checks and pipeline lessons](evidence/adv-1/composition.md). Existing Adventure native harness now also requires four source-pixel scene composition samples; all prior course/CLI/cleanup checks remain. No extra assets, importer, editor or engine API.

RPG-1 Trail Duel: `dotnet run --project examples/Rpg/Rpg.csproj -c Release`. [Controls, deterministic outcomes, technical proof and owner-only evaluation](evidence/rpg-1/README.md). New checks: Rpg.Contracts and `pwsh -NoProfile -File checks/RpgNativeSmoke.ps1`; retain old regressions. Public Explorer supplies navigation; combat/UI/progression stay in the new consumer. No engine change, package update, runtime download or save support. Images/labels required by this example only.

## Meadow Run phase2
[MR-2 run and evidence](evidence/meadow-2/README.md): same Platformer project now runs two levels;
Space after first flag continues, death retries current level, final Space restarts campaign.
The original standalone LevelGame remains a regression API. Current technical/product status
is in backlog and evidence; no audio/enemy/pause/menu implementation is implied.

## MR-4 patrol candidate
[Run/evidence](evidence/meadow-4/README.md): level2 spikedslime is avoid-only, no stomp.
Current technical/product status in backlog; nextmanualexperience is enemyavoidance afterproof.

## Meadow Run session flow
[MR-5 controls/evidence](evidence/meadow-5/README.md): Space starts, P pauses/resumes, R restarts
campaign; whilepaused Up/Down+Space choose Resume/Restart. Escapeexits. Sessionflowusescurrent
art/labels anddoesnotchange standaloneCampaignGame or previousconsumercontrols.
