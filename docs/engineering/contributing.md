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
