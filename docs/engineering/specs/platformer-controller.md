# PC-1 — Optional jump buffering in the external platformer controller

Status: approved PC-1 by Codex root2026-10-16 under owner request/technical delegation; independent jump_buffer_review design PASS, no blockers. ME-016 completed. FR-002,NFR-001/002/004/005; [H1-R1.1](h1-runtime.md) engine contract unchanged. This is FirstPlayable.Model consumer behavior, not an engine capability or genre module.

## Observed need and decision

Owner reports comfortable immediate movement/clear primitives and occasional missed presses just before perceived landing; explicitly nonblocking gameplay observation, not confirmed engine defect. Public model tests derive the landing update from a disabled baseline at step1/60second, rather than assuming a floating-point flight duration. A press on that update begins airborne, tests jump eligibility, then resolves floor; result Grounded=true,Y268,velocity0, next empty tick stays grounded. This is compatible with reported timing, not proof of exact native event time. Loop already preserves edges through zero-update frames.

Choose configurable jump buffering in the consumer. Parameterless model stays disabled to preserve current behavior/other usages; FirstPlayable explicitly selects100ms (approximately6fixed updates), a provisional usability setting, not a universal optimum. Do not change speed180, jump impulse-300, gravity900, floor268, x-clamp0..616, render geometry or native input mappings. User feedback on the buffered version remains unobserved.

Coyote time: defer, no flag/property/no-op implementation. Current full-width floor and horizontal clamp have no walk-off/ledge transition to validate a grace interval. Revisit only with a concrete consumer movement case; user-authorized evaluation does not justify invented geometry/ground-provider APIs.

## Exact consumer surface

New public sealed FirstPlayable.Model.PlatformerOptions(TimeSpan jumpBufferWindow), immutable public TimeSpan JumpBufferWindow getter. Negative values throw ArgumentOutOfRangeException; zero disables, all positive TimeSpan values accepted without arbitrary upper limit. No coyote setting. No external packages.

GameModel retains public GameModel() (explicit ctor, delegates to zero-window options), adds public GameModel(PlatformerOptions options) (null rejected), public void ClearJumpBuffer(). Existing Update(TimeSpan delta, InputSnapshot input), getters and validation order unchanged. Options immutable, per controller; no static/global buffer. ClearJumpBuffer cancels only pending jump intent, does not move/stop/ground the character or change velocity. New instance starts empty. Existing parameterless model has exactly the existing no-buffer behavior, including ignored landing-tick edge.

## Discrete timing/order (no sub-frame precision claim)

- Use simulation-time delta, not wall-clock or engine changes. Treat each Update input at that discrete boundary; no timestamp of the physical key event is inferred. Both eligibility checks within that Update share the same boundary time.
- Before reading this Update's jump edge, age any previously pending request by delta. Expire only when age exceeds configured window: equality remains valid for this boundary. Use saturating/comparison-before-subtraction remaining ticks, avoiding overflow or unbounded accumulated time even at TimeSpan.MaxValue.
- A new Pressed jump edge refreshes the single pending request to the configured positive window. Held alone never creates intent; multiple edges within one snapshot coalesce to one pending jump. Zero window never queues intent; it preserves direct grounded-edge behavior.
- At the original pre-integration grounded check: direct new edge or unexpired pending intent launches with impulse-300, clears pending intent, then performs the existing gravity/integration once. No air jump or repeated hold behavior introduced.
- Integrate airborne motion and resolve floor exactly as before. If contact occurs and a positive-window request remains valid, consume it and set velocity-300/Grounded=false at the floor for the next tick; do not integrate a second time. Y may equal floor while new upward velocity exists until next update; document this fixed-step consequence rather than claiming sub-frame contact inference.
- One request launches at most once; a subsequent jump needs another Pressed edge. New presses may refresh an airborne request but never grant airborne eligibility. No landing animation/collision/physics/coyote logic added.

## Consumer session cancellation

Simulation pauses do not call Update, so simulation-time aging alone would preserve a pending intent. The example must explicitly clear it when its existing platform poll reports inactive or close. Add internal FirstPlayable.ControllerPlatform : IFramePlatform, constructor(IFramePlatform inner, GameModel controller); delegate Begin/Canvas/End/Dispose unchanged and Poll exactly once. Poll forwards the original frame unchanged after ClearJumpBuffer if !Active or CloseRequested. No extra native poll, injected input, new engine event, or new production backend. The wrapped resource is owned by the existing H0 factory/session; partial Desktop initialization remains Desktop's obligation.

ControllerPlatform lives in the executable; its actual source is linked into Loop.Contracts for deterministic public-interface tests, without a native project/package reference or friend assembly. It is not exported as a general engine API. Sample factory constructs this wrapper around Desktop and uses new GameModel(new PlatformerOptions(TimeSpan.FromMilliseconds(100))). Program keys/CLI/smoke/failure contracts unchanged.

## Acceptance / runnable probes

New checks/Loop.Contracts/JumpBufferTests.cs: reproduce contact-update edge drop with default/explicitzero; opt-in accepts landing-tick or prior-tick intent; exactwindow accepted, windowminus1tick expired; fresh edge refresh; held/duplicate/no immediateairjump/no latent repeat; zero/negative/null/large window validation; ClearJumpBuffer preserves visible state; inactive/close wrapper cancels pending, active/zero-tick poll preserves it and forwarding/lifetime delegates unchanged. Keep existing movement/jump/landing and all76baseline tests; no assertions removed.

Use serial Release dotnet test checks/Loop.Contracts/Loop.Contracts.csproj with --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false. Full Desktop/Hosting runners same flags. checks/Desktop.Contracts/NativeSmoke.ps1 rebuilds consumer and verifies seven scenarios/CLI, not subjective usability. New feature tests must fail observably before buffering implementation; existing baseline drop is expected old behavior, not an engine regression.

Independent consumer design readback before approval; independent scratch verification and parent spotcheck before completion. Check git diff under engine is empty and headless test dependency graph stays native-free. New buffering is technically verified through traces, never accepted subjectively by the agent. Small focused consumer spec under accepted architecture suffices; no new engine ADR required.

## Rollback / future refinement

Remove only own options/model delta/example wrapper/test-link and related documentation to restore baseline; never touch unrelated work. Do not silently enable buffering on other consumers or build a mandatory platformer controller into Core. Next broad H1 capabilities remain separate roadmap refinements; this task is narrowly driven by received gameplay feedback.

Verification2026-10-16: implemented ME-017; author93/93, native sevenPASS; independent jump_buffer_verify scratch53/53 and buffer-disabled mutationRED/restoredGREEN. No engine bytes changed. Objective contract verified, subjective buffered feel unobserved.
