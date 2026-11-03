# ARCH-1: browser determinism (T0 groundwork)

Engineering material for the ARCH-1 dossier, validation tier T0 (`docs/evaluations/arch-1.md` §8). Nothing here is
built by `MicroEngine.slnx` or by CI, and the engine is not changed: every measurement ran on a patched scratch copy
of `main` made by `apply-probe.mjs`.

## Question

The browser suite (`web/tests/run.mjs scripts`, compared by `web/tests/desktop.mjs`) found browser captures that
differ from the desktop captures of the same input script (Meadow Run's level and map, Neon Rift's stage 1, single
Courtyard Key frames; Meadow Run's camera about 3 px off), and **which** frames differ changed between two browser runs
of the same commit. Pixel identity across backends is not a contract; simulation determinism is (T0). So:

1. Does the same build and script produce the same simulation state at every fixed step, run after run, in the browser
   and on desktop? Where not, at which step does it first diverge, and why?
2. Does the same game logic produce the same state on desktop and in the browser, or is there a documented source of
   difference (floating-point libraries, SIMD)?
3. What should T0 look like so a regression is caught at the step it happens?

## Verdict

**The game logic is deterministic; the browser host is not running scripts the way every other host does.**

1. **The frame differences are drawing, not simulation (root cause R1).** The browser host runs scripted runs in
   real time: `BrowserLauncher` always builds its platform with `new DesktopOptions()` (`FixedStep = false`), while the
   desktop launcher, Android and iOS make any scripted run fixed-step (`GameLauncher.cs`, `FixedStep = parsed.ScriptPath
   is not null || parsed.Hidden`). So in the browser the number of steps per frame and `Game.FrameBlend` come from the
   wall clock, and a capture is drawn up to one update behind, interpolated at a random blend (measured 0.00 to 0.81,
   different in each run; desktop always 1). The games draw moving things at `Lerp(previous, current, FrameBlend)`, so
   anything that moved in the last update lands in a different place: Meadow Run's camera runs at up to 152 px/s, about
   2.5 px per update, the "about 3 px" seen in CI. Applying the candidate fix F1 (a scripted run is fixed-step on every
   platform) in a scratch copy made **all 138 captures of the 15 scenarios byte-identical between two browser runs and
   between the browser and the desktop build** (Windows, Chrome on the GPU vs raylib on the GPU), against 123/138 and
   113/138 without it.
2. **Simulation state is identical at every step, run after run and desktop vs browser, except where wall-clock
   budgets feed state.** With F1 applied, the per-step hashes of time, randomness, scene stack, game fields and input
   were identical at every step of all 15 scenarios, raylib vs raylib, raylib vs SDL, browser vs browser (Trail Duel's
   game fields differ only by the save folder each run is given). Two
   wall-clock-driven sources remain:
   - **R2, music rendered in wall-clock slices (run-to-run, browser only).** Without threads, `Audio.AddMusic` renders
     songs a few milliseconds of wall time per `Audio.Update`, which runs inside `Game.Step`
     (`Audio.RenderSlices`, `SliceBudget` 4 ms, `Stopwatch`). How far a song has rendered at a given update differs per
     run (Meadow Run `showcase`, update 4: sample 20479 in one run, 16383 in the other), so `Audio` state, and through
     it `CurrentMusic` and the start of music, depends on CPU speed. Courtyard Key reads `Audio.CurrentMusic` in its
     room logic (`PlayScene.Rooms.cs`), so gameplay can observe it.
   - **R3, presentation state written while drawing.** Scenes keep caches that `Draw` writes (HUD label text, Meadow
     Run's held positions for blending, draw lists). They are correct as presentation, but a frame that draws less often
     (several steps per frame) leaves them different; a T0 hash must not include them.
3. **Known cross-platform sources of float differences exist, and they reach state updated in `Update`
   (R4).** With F1, desktop and browser differ in a few steps of a few scenarios, and every step inspected differs by last-bit float values of
   cosmetic state: Meadow Run's squash factor `_view._squash.X` (1.0171438 vs 1.017144) and Courtyard Key's title
   particles (`Velocity.X` -4.791649 vs -4.7916484). Two library behaviours explain them, both measured on the desktop side here:
   `Vector2.Lerp` on x64 with FMA hardware returns exactly `MathF.FusedMultiplyAdd(a, 1 - t, b * t)`, one ulp away from
   the unfused result in some steps (the browser value is consistent with an unfused computation); and Windows' `MathF.Sin`, `Cos`,
   `Pow` and `Exp` differ from the double result rounded to float in 1258, 1279, 131 and 76 of a million random
   inputs, so another libm (emscripten's in the browser, glibc on Linux) can give other last bits. Which of the two
   produced each observed difference was not isolated in the browser. Gameplay already uses both (`Camera2D.Follow` through
   `MathX.Damp(Vector2)`, Neon Rift's bosses with `Vector2.Lerp` and `MathF.Exp`), so the same game logic is not
   guaranteed to produce bit-identical state across platforms; in the scripts measured, gameplay state stayed equal.

Pixel identity across backends stays out of the contract (architect's ruling); this result says the browser suite's
frame comparison becomes exact once R1 is fixed, at least on one machine, and that T0 can be exact run-to-run on every
platform once R2 is fixed, and exact across platforms only for state computed without R4's operations, or with a
documented per-platform hash.

## Method

- **Probe** (`StateProbe.cs`, wired in by `apply-probe.mjs`). After every fixed step it prints one line of FNV-1a
  hashes of the state reachable by reflection, per component: `t` (game time), `rnd` (the seeded `System.Random`),
  `scn` (the scene stack), `game` (the game subclass's own fields), `aud` (`Audio`), `inp` (`InputMap`), `ast`
  (`AssetCache`); one line per frame (`steps` run in it, `FrameBlend`), and one per capture (update and
  `FrameBlend` it was drawn at). Floats are hashed by their bits, collections in enumeration order, coroutine state
  machines through their fields (never enumerated). Platform objects, GPU resources, save locations and art built at
  load are left out. `MICROENGINE_T0_DUMP=a-b` prints every field of those steps, so two runs can be diffed at the
  first diverging step; `MICROENGINE_T0_SKIPFIELDS` leaves out fields a scene writes while drawing (below);
  `MICROENGINE_T0=0` keeps only the capture lines, so frames are paced as without the probe.
- **Builds.** One scratch copy with the probe (`probe-c`) and one with the probe plus the candidate fix F1
  (`--scripted-fixed-step`, `probe-b`). Desktop: `dotnet build games/<Game>/<Game>.csproj -c Release`. Browser:
  `web/build.sh --dev` with the pinned toolchain (.NET 10.0.403, emscripten 3.1.56, Mono interpreter, no AOT).
- **Runs.** The 15 scenarios of `web/tests/scenarios.json` with their environment and seed 1, started as CI starts
  them: desktop `--script --hidden --mute --platform raylib|sdl|headless` (`run-desktop.mjs`), browser
  `?script=...&autostart=1` in headless Chrome through puppeteer (`run-browser.mjs`); `matrix.mjs` runs and compares
  the pairs, `compare.mjs` reports the first diverging update per component, `captures.mjs` the capture blends and
  byte equality with the probe silent, `diffcaps.mjs` compares capture folders.
- **Machine.** Windows 11, x64, Chrome (GPU-accelerated WebGL), raylib and SDL on the desktop GPU. CI's comparison runs
  on Linux with Mesa llvmpipe on both sides; Linux was not measured here (limitations).

## Results

### Captures at natural frame pacing (`captures.mjs`, probe silent)

| Build | Browser run 1 vs run 2 | Desktop (raylib, fixed step) vs browser | `FrameBlend` of the captures |
| --- | --- | --- | --- |
| `main` | 123/138 identical; differing: Meadow Run `menus`, `smoke`, `showcase`; Courtyard Key `fight`; Trail Duel `master-battle` (1); Neon Rift `stage1` | 113/138 | 0.00 to 1.00, different per run (Meadow Run `showcase`, `meadow.png` at update 150: 0.55 and 0.38) |
| `main` + F1 | **138/138** | **138/138** | 1.00 everywhere |

Scenes where nothing moved in the last update (menus, dialogue, Trail Duel's overworld at rest) compare equal whatever
the blend, which is why most captures matched and why *which* ones differ changes from run to run: it depends on the
blend each run happened to draw at. The CI failure of PR #33 (`fight4.png`, `boss_e.png`) is the same effect; both
appear among the differing captures here.

### First diverging step per pair (`matrix.mjs`, `compare.mjs`)

Components: `t`, `rnd`, `scn`, `game`, `aud`, `inp`, `ast` (Method). "Identical" means every step of the script.

| Pair | Without F1 (`main`) | With F1 |
| --- | --- | --- |
| desktop raylib vs raylib, raylib vs SDL | identical in all components, all 15 scenarios, except Trail Duel's `game`, which holds the save folder each run is given (environment, not state) | same |
| desktop raylib vs headless | `inp` from update 1 (the touch layout's last window rectangle: 1280×720 vs 0×0); Courtyard Key's `scn` from update 2 to 232, by the same rectangle reached through the HUD; nothing else | same |
| browser vs browser | measured on Meadow Run `showcase` only: `aud` from update 1 (R2); `scn` equal once draw-time caches are left out (`MICROENGINE_T0_SKIPFIELDS=_view,_hud,_flag,_bonus,_total`) | all 15 scenarios: every component identical except `aud` (R2) |
| desktop vs browser | measured on Meadow Run `showcase` only: `scn` from update 1, by draw-time caches (R3; the browser drew a frame before update 1 and then drew after several steps); `aud` (R2 and `--mute`), `inp`/`ast` (window rectangle, `BackgroundDecoding` false in browsers); with draw-time caches left out, `t`, `rnd`, `scn`, `game` identical at all 300 steps | all 15 scenarios: `t`, `rnd` identical; `scn` identical in Courtyard Key `opening`, `fight`, `boss`, Meadow Run `end`, Trail Duel `title` and all three Neon Rift scenarios; elsewhere it differs at some steps by last-bit floats in cosmetic state (R4): Meadow Run `showcase` from update 9 (`_view._squash.X`), `menus` from 420 and `smoke` from 390 (not inspected), Courtyard Key `title` from 81 (title particles' `Velocity.X`), `ending` at update 452 only (not inspected), Trail Duel `master-battle` from 332 (`_orbPos.X` 303.14285 vs 303.14288, `Vector2.Lerp` and `MathF.Sin` in `BattleScene.Present.cs`) and `newgame` from 482 (not inspected). The two browser runs agree with each other at every one of these steps |

The probe itself slows browser frames (one console line per step), so in traced browser runs frames grouped about five
steps and captures clamped to blend 1; the capture table above, with the probe silent, is the one that shows R1.

## Root causes and proposed fixes

| | Cause | Evidence | Engine-side fix | Game-side fix |
| --- | --- | --- | --- | --- |
| R1 | Browser scripted runs are real-time: steps per frame and `FrameBlend` from the wall clock | blends 0.00 to 0.81 per capture and run; F1 makes 138/138 captures equal | **F1:** a scripted run is fixed-step on every host. Either the loop decides it (`GameLoop`: a run with `RunOptions.Script` advances one step per frame and draws at `FrameBlend` 1, as the `Game.FrameBlend` documentation already promises for scripted runs), or `BrowserLauncher`/`GameSession` pass `FixedStep = true` to the platform when a script is given, as Android and iOS do. The loop is the better place: one rule, every host, and stage 1A's loop owns it. Then make the browser frame comparison a gate again | none |
| R2 | Songs render in wall-clock slices inside `Game.Step` in browsers; `CurrentMusic` and music start depend on CPU speed | song sample index at update 4: 20479 vs 16383 | Keep the logical music state (requested track, fades) independent of rendering: `CurrentMusic` and fades follow the request at once; whether the song is audible yet is device/presentation state. Render slices outside the step (per frame, before the updates), or, in scripted runs, by a fixed amount of song per update | Courtyard Key: compare the area's music with the track it last requested, not `Audio.CurrentMusic` |
| R3 | Draw-time caches live in scene objects | HUD text, held positions and draw lists differ only with drawing cadence | T0 hashes only what games and modules contribute (below), never reflection; a test that the hash is the same before and after `Render` catches state written while drawing | keep draw caches out of `HashState` (or out of the scene, in a view object the hash does not visit) |
| R4 | `Vector2.Lerp` fuses its multiply-add where the hardware has FMA; `MathF` transcendental functions round differently per libm (Windows UCRT, glibc, emscripten in the browser) | `_squash.X` 1.0171438 vs 1.017144; particle velocity -4.791649 vs -4.7916484; Lerp equals `FusedMultiplyAdd` on x64; Windows `MathF.Sin` differs from the correctly rounded value 1258 times per million | A deterministic math path for simulation: `MathX.Lerp`/`Damp` written as explicit unfused (or explicitly fused) multiply-adds, never `Vector2.Lerp`; and either float functions the engine owns (a polynomial or table set computed with plain float operations; `(float)Math.Sin((double)x)` only moves the question to the double libm and was not measured in the browser) or a documented rule that cross-platform T0 compares only state computed without them | gameplay code uses the engine's deterministic helpers (`MathX`) instead of `Vector2.Lerp` and raw `MathF` |

Also found, outside determinism: browser scripted runs are not muted (`desktop.mjs` passes `--mute`; the browser
host has no equivalent), so `Audio` volumes differ between the two; and `UpdateTouch` and `AssetCache.Update` use
wall-clock budgets per frame (presentation and loading; not seen to reach simulation state in these scripts, but they
are the next candidates if T0 finds a divergence after F1 and R2).

## Proposed T0 mechanism: a per-step state hash

A hash API the loop calls after every fixed step, fed by explicit contributions, recorded by scripted runs and
compared by tests and CI. Not implemented here (stage 1A owns the loop).

```csharp
namespace MicroEngine.Core;

/// <summary>Accumulates a 64-bit hash of simulation state in named sections (FNV-1a or xxHash64).</summary>
public sealed class StateHasher
{
    public void Section(string name);          // starts a named part (scene type, module name)
    public void Add(int value); public void Add(long value); public void Add(bool value);
    public void Add(float value);              // by bits: -0 and NaN payloads count
    public void Add(double value); public void Add(Vector2 value); public void Add(string? value);
    public void Add(ReadOnlySpan<byte> bytes); // bulk: tile maps, grids
}

/// <summary>Something whose simulation state takes part in the per-step hash.</summary>
public interface IStateHashed { void HashState(StateHasher hash); }

// Scene: public virtual void HashState(StateHasher hash) { }  // default: contributes nothing
// Game:  public virtual void HashState(StateHasher hash) { }  // the game's own fields
// Modules (stage 1A systems) implement IStateHashed and are registered with the game.

public sealed record StepHash(long Update, ulong Hash, IReadOnlyList<(string Section, ulong Hash)> Sections);

public interface IStateTrace { void Record(StepHash step); }   // RunOptions.StateTrace
```

- **What the engine contributes:** `UpdateCount`, `Time`, the game RNG's internal state (the engine should own a
  seeded generator whose state it can read and restore, instead of `System.Random`), the scene stack (types, then each
  scene's `HashState`), input after sampling (actions down, values, latched taps), audio's *logical* state (requested
  track, fades; never rendering progress), timers and coroutines it owns, each registered module.
- **What it never contributes:** anything written while drawing, platform or device objects, GPU resources, save
  locations, wall-clock timings, asset loading progress.
- **Loop hook:** after `Game.Step` (and `Input.EndUpdate`), when `RunOptions.StateTrace` is set, the loop computes the
  hash and records it. Cost is paid only by traced runs. Optional `RunOptions.StateTraceSections` records per-section
  hashes, so a divergence names the module (`scene PlayScene`, `module Physics`).
- **Recording:** the launcher's `--state-trace FILE` writes one line per step (`<update> <hash> [<section>=<hash>...]`);
  the browser host returns the same text from a development-only export, as captures are returned today; headless
  tests get the list in memory.
- **Comparison:** `StateTrace.FirstDivergence(a, b)` returns the first update and section that differ, or null.
- **Tests (T0, every PR, every OS):** each game script runs headless twice and must produce identical traces; the
  desktop backends (raylib, SDL, headless) and, in the browser job, the browser must produce the trace recorded by
  headless on the same OS; a property test checks that `Render` does not change the hash (catches R3); a test injects a
  wall-clock-dependent field and expects the run-to-run comparison to fail (the check checks itself).
- **Cross-platform rule:** exact for state computed with the engine's deterministic math (R4); until that exists, the
  cross-platform comparison reports, and the run-to-run comparison gates. References are never adopted automatically.

## The browser short-press check (PR #36)

The architect asked whether the intermittent CI failure of "a press shorter than a frame is seen" (run 41548493592,
run 41543489225 on `main`) has the same root cause as the frame differences. **No: the game saw the press; the harness
lost the frame that showed it.** `since()` in `web/tests/run.mjs` counted the traced frames in one page call and read
them in a second one. A frame drawn between the two calls slides the window by one, past the oldest frame after the
mark, and a press shorter than a frame is down in exactly that one frame. A recorded local failure (two suites running
side by side) shows the key events at 8735.5 and 8736.2 ms, the next frame tracing `TitleScene right`, and the check
still failing. Pausing 60 ms between the two calls makes `main`'s check fail every time (Meadow Run, Neon Rift); PR #36
counts and reads in one call (`run.mjs`, and the same helper in `mobile.mjs` for the very short tap), waits for three
updates and three frames instead of a fixed 400 ms (a stalled page could draw none, and `trace(0)` returned the whole
trace), and prints what ran with the check. The capture differences in the same CI run (`fight4.png`, `boss_e.png`) are
R1.

Separately, the engine **does** drop sub-frame presses when the page draws more than about eight frames per update:
`RaylibInput.CollectTaps` expires an unread tap after 8 frames, not after an update. With Chrome's frame cap removed
(about 1000 frames per second) 22 of 40 presses were lost on Meadow Run (11/20 Neon Rift, 12/20 Courtyard Key, 15/20
Trail Duel), and the fixed check reports them (it is not weakened). CI draws at 60 frames per second (its frame-rate
suite reports it), so this is not the CI flake; it is an engine defect (expire taps by updates or time), left for the
architect's decision.

## Limitations

- One machine (Windows 11 x64, GPU-accelerated Chrome, desktop raylib and SDL on the GPU). CI compares on Linux with
  llvmpipe on both sides; glibc's `MathF` differs from Windows', so R4's list of affected steps will differ there.
- Chrome only; Firefox was not run.
- The reflection probe is a diagnostic, not the proposed API: it needed per-game field exclusions for draw-time caches
  and left out art built at load.
- The fixes were applied only to scratch copies; nothing in the engine changed.

## Reproduce

```bash
node experiments/arch-1/browser-determinism/apply-probe.mjs --out ~/scratch/probe-c
node experiments/arch-1/browser-determinism/apply-probe.mjs --out ~/scratch/probe-b --scripted-fixed-step
# in each copy: dotnet build games/<Game>/<Game>.csproj -c Release; web/build.sh --dev --out <copy>/dist/web-dev;
# npm ci --prefix web/tests
node experiments/arch-1/browser-determinism/captures.mjs --copy ~/scratch/probe-c --out ~/scratch/caps-c
node experiments/arch-1/browser-determinism/matrix.mjs --copy ~/scratch/probe-b --traces ~/scratch/traces-b --browser chrome
node experiments/arch-1/browser-determinism/compare.mjs A.txt B.txt
```

| File | Purpose |
| --- | --- |
| `StateProbe.cs` | The reflection probe copied into the scratch engine |
| `apply-probe.mjs` | Makes a probed scratch copy of a commit, optionally with F1 |
| `run-desktop.mjs`, `run-browser.mjs` | One scenario, one trace (and captures) |
| `matrix.mjs` | All scenarios, all pairs |
| `captures.mjs`, `diffcaps.mjs` | Capture blends and byte equality with the probe silent |
| `compare.mjs` | First diverging update per component |
| `math-check/` | `dotnet run -c Release`: `Vector2.Lerp` against fused and unfused multiply-adds, and how often `MathF.Sin`, `Cos`, `Pow`, `Exp` differ from the double result rounded to float |
