# AV-1 — Procedural avatar presentation

Status: approved2026-10-16 by Codex root under technical delegation after independent animation_design_review PASS; ME-020 completed, ME-021 ready. Scope owner continuation2026-10-16, roadmap H1/CAP-008, FR-002/NFR-004/005, ADR-0003/0004. Consumer-only demonstration, not an engine animation or asset capability.

## Outcome and alternatives

Show distinguishable idle, two-frame walking, airborne and left/right-facing poses in FirstPlayable using existing ICanvas.Fill. Animation consumes resolved game state; it never changes movement/input/geometry or jump rules. Keep all108baseline tests and native seven-case harness. Procedurally authored rectangle frames are source content, not external media or an asset format.

Choose a small consumer animator plus executable renderer. Merely changing color does not meaningfully demonstrate a walk cycle. Sprite textures would require provider/loading/ownership/error contracts without a demonstrated need for this first pose/timing slice; defer them rather than add mandatory methods to ICanvas. A generic clip/state machine or configurable pipeline is premature. No new package/project/backend, texture IO, atlas, interpolation, ECS, scenes, audio or full-H1 acceptance.

## Exact animator contract

New public enum FirstPlayable.Model.AvatarPose { Idle, Walk, Air }. New public sealed AvatarAnimation has get-only public AvatarPose Pose, bool FacingLeft, int WalkFrame (private mutation allowed). Initial Idle/right/frame0, zero phase. New public void Update(TimeSpan delta,float displacementX,bool grounded).

Validate delta>0 first (ArgumentOutOfRangeException(nameof(delta))), then finite displacement (ArgumentOutOfRangeException(nameof(displacementX))). Invalid calls leave every public state and timing phase unchanged. Accept every positive TimeSpan including MaxValue; no wall-clock/global state.

- displacementX>0 selects right, <0 selects left; zero preserves facing, including when airborne. Use actual resolved displacement, not held intent. Negative zero counts as zero.
- !grounded selects Air regardless of displacement. Otherwise nonzero displacement selects Walk, zero selects Idle. Changing facing during continuous Walk preserves gait phase.
- Entering Walk from any other pose sets phase0/frame0. Do not age the new walk interval by that same update's delta. On subsequent continuous Walk updates, phase advances by delta modulo240ms; frame0 for phase[0,120ms), frame1 for[120ms,240ms). Equality120ms changes frame, equality240ms wraps0. Compute delta modulo cycle before adding bounded remainder to avoid overflow at TimeSpan.MaxValue. Large delta skips directly to equivalent phase, no per-frame loop.
- Idle/Air always reset phase/frame0. Returning to Walk begins frame0; no latent gait across idle, airborne time or inactive simulation. No forced animation update from presentation: repeated renders cannot advance state. Independent instances never share phase.

## Exact drawing/integration seam

New internal static FirstPlayable.AvatarRenderer.Draw(ICanvas canvas,float x,float y,AvatarAnimation animation) in executable. Link its actual source into Loop.Contracts (like ControllerPlatform), without Desktop/native reference. Reject null canvas, then null animation, then nonfinite x/y before any draw; argument exceptions, no partial draws. All frames are drawn relative to top-left(x,y) inside the unchanged24x32collision envelope. No background fill, model mutation or canvas ownership/lifetime operations.

Authored content (local x,y,width,height, Rgba): head(4,2,16,10,240/180/65), torso(6,12,12,12,210/140/45), eye(16,5,2,3,24/28/40); feet color240/180/65. Idle feet(5,24,5,8),(14,24,5,8). Walk0 feet(2,24,8,8),(15,24,6,5); Walk1 feet(3,24,6,5),(14,24,8,8). Air feet(3,24,7,5),(14,24,7,5). Draw head, torso, eye, then left/right local feet in that order. FacingLeft mirrors each rectangle's local x to24-localX-width, preserving order. Five Fill calls per pose. Bounds remain a collision envelope, not pixel-perfect contact geometry.

Program creates animator beside game. Each fixed update: remember old game.X, game.Update, then animation.Update(frame.Delta,game.X-oldX,game.Grounded). Drawing callback calls renderer at game.X/Y with animator. The existing gold player becomes a recognizable primitive figure; floor/obstacle colors, sample geometry/window/CLI and input unchanged. Rendering zero-update frames retains previous pose. Inactive loop already freezes simulation; a pure visual gait phase may resume on focus, but no inactive time is accumulated (unlike jump intent, no cancellation hook required).

## Public probes and evidence

New checks/Loop.Contracts/AvatarAnimationTests.cs, observable public animator and recording ICanvas:

- Initial state; signed actual displacement/facing persistence; idle/walk/air precedence; wall-clamped actual movement0 selects idle and retains facing; changing direction in Walk preserves phase.
- Exact120/240ms boundaries, partitioned equivalent continuous-Walk deltas, TimeSpan.MaxValue bounded arithmetic; idle/air reset; independent instances; invalid calls atomic including phase.
- Every pose/frame: exact five rectangles/colors/order; all remain inside24x32; left poses are exact mirrored coordinates; walk frames differ and air distinct. Repeated render leaves animator/model state unchanged. Null/nonfinite inputs issue no partial draws.
- Existing108tests unchanged, all suites plus native seven-case harness; no engine/GameModel/PlatformerOptions/obstacle code changes. Actual sample integration uses resolved displacement and existing render lifecycle, no backend-specific code outside Desktop.

Runnable RED before implementation, focused/full GREEN. Serial commands: dotnet test checks/Loop.Contracts/Loop.Contracts.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false; Desktop.Contracts/Hosting.Contracts same flags; checks/Desktop.Contracts/NativeSmoke.ps1 rebuilds sample. Independent scratch verification, parent spotcheck, local link/diff checks before closure. Runtime output is not subjective art/animation acceptance. Deliver manual handoff only for movement/pose/facing readability, never ask owner to repeat automation.

## Risk, review and recovery

Risk item3: externally visible sample presentation; independent design and implementation review. No significant engine architecture change; AV-1 under accepted ADRs suffices. Scope exact new examples/FirstPlayable.Model/AvatarAnimation.cs, examples/FirstPlayable/AvatarRenderer.cs, checks/Loop.Contracts/AvatarAnimationTests.cs; examples/FirstPlayable/Program.cs; checks/Loop.Contracts/Loop.Contracts.csproj; docs/engineering/{specs/avatar-animation.md,backlog.md,current-state.md,roadmap.md,milestones/H1.md,capabilities.md}; odd/tasks/h1-avatar-animation.md. Rollback only own animator/render/tests/link/Program/doc deltas to restore old gold box; preserve collision/buffer and unrelated work. Findings may inform later sprite/content needs; they do not silently authorize extraction or a full asset manager.
