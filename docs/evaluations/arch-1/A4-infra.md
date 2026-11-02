# A4 — Validation and delivery infrastructure audit (ARCH-1, Phase I)

Auditor domain: tests, captures and visual references, scripted playthroughs, headless platform, timing, audio and
input checks, benchmarks, CI workflows, packaging, platform experiments, SDK consumer tests, site checks.
Repository state: `C:\Proyectos\MicroEngine`, branch `docs/arch-1` (includes unmerged ASSET-1), audited 2026-11-02.
Read-only on the repository. Experiments live in
`<scratch>\exp-infra\`.

Guiding question (request §7): does the infrastructure prove that subsystems meet their contracts, or only that output
is accidentally identical to a recorded history?

**Short answer:** the infrastructure is unusually strong on *delivery mechanics* (content-keyed evidence, SDK
acceptance from outside the repository, package verification, documentation integrity) and on *simulation*
(deterministic headless loop, simulated clocks, captured audio requests). It is weak exactly where an engine with
replaceable backends needs to be strong: there is **no backend conformance suite**. Rendering correctness across
backends is defined as *exact pixel equality with another backend* or *closeness to a recorded per-platform
fingerprint*; the fingerprint metric has measured blind spots and false alarms; the default desktop backend's input and
audio output are not checked natively; the browser build's frames are not compared automatically; and the
release-gating package checks are currently red on `main` because recorded references drifted.

---

## 1. Inventory of test layers

Measured locally on Windows 11 (this machine has a display and audio device), Release build, 2026-11-02.
Scratch log: `...\exp-infra\timing.txt` and `...\exp-infra\test-*.log`.

| Layer | Where | Count | Local runtime | What it actually proves |
| --- | --- | --- | --- | --- |
| Engine unit/simulation | `tests/MicroEngine.Tests` (27 files) | 277 tests (276 pass, 1 skipped) | 0.43 s exec, ~2 s wall | Engine logic against `HeadlessPlatform` and hand-written fakes: scenes, world, input mapping, touch, assets (ASSET-1), pacing with simulated clocks, profiler, allocation budgets, captured audio requests. Rendering only as *call counts* (see F5). |
| Game rules/playthroughs | `tests/{MeadowRun,CourtyardKey,NeonRift,TrailDuel}.Tests` | 53 + 35 + 38 + 100 = 226 | 2-8 s each | Game rules, autopilot playthroughs, interpolation (`DrawBetweenUpdatesTests`) through the headless platform; games are reached through `InternalsVisibleTo` (game internals, not engine internals). |
| Samples | `tests/Samples.Tests` | 12 | 0.2 s | Documented samples and Asset Gallery run headlessly; gallery loads every asset kind. |
| SDL + cross-backend native | `tests/MicroEngine.Desktop.Tests` (category `Native`) | 12 | 16 s (real windows) | SDL draws a 64x36 probe with engine conventions (per-pixel asserts); every SDL GPU driver and raylib produce **exactly** the same RGB; texture filters on SDL and raylib; SDL virtual gamepad hot-plug; SDL audio allocation budget; macOS bundle content lookup. |
| raylib native | `tests/MicroEngine.Raylib.Tests` (`Native`) | 6 (5 skipped on Windows: Linux-only) | 0.2 s here | raylib keyboard through XTEST (held, sub-frame tap, two keys); raylib audio device opens; a 440 Hz tone recorded from a PulseAudio sink by RMS and zero crossings. |
| Scripted desktop captures | `web/tests/desktop.mjs` + `scenarios.json` + `captures.mjs` | 15 scenarios, 138 captures, **7 reference files** (~342 KB each) | 127 s (Linux CI, both backends) | Each game's scripted run on a real backend, summarised as size + exact hash + 32x18 cell-mean grid, compared at tolerance 3/255 against a per-platform reference. |
| Browser engine suites | `web/tests/run.mjs` (platform, scripts, autopilot, audio, framerate, keyboard) | 6 suites | manual | Real key events, autoplay policy, 60 updates/s, audio signal level, captures byte-equal to desktop raylib. **Run by no workflow** (F6). |
| Site browser suites | `site/tests/browser.mjs` (layout, play, touch) | 3 suites, ~20 pages x 3-4 widths | ~2 min CI | Site layout, iframe lifecycle of demos, touch panels. Game frames are screenshotted for human review only. |
| Mobile web | `web/tests/mobile.mjs` | 4 games x 4 emulated devices | ~7.5 min (platforms.yml) | Multi-touch reaches controls; screenshots as artifacts, not compared. |
| JS tooling tests | `.github/scripts/*.test.mjs`, `site/tests/*.test.mjs`, `sdk/tests/version.test.mjs`, `packaging/*.mjs` | ~120 `test(` calls | seconds | CI planner rules, evidence keys, cost estimates, demo store, repo-docs link/snippet rules, version tool. |
| SDK acceptance | `sdk/tests/packages.mjs`, `external.mjs`, `docs.mjs` | 3 scripts (~60 checks in external) | 1.7-4.5 min per OS | Packages packed and inspected; a game created from the template **outside the repository** from the feed, built, run (captures before/after input differ), packaged, failure messages checked; documented samples build against nuget.org. |
| Package verification | `packaging/verify.mjs`, `linux-distros.mjs` | per target | 2-6 min per package | Integrity, structure, no build paths, build-info, glibc floor / codesign, executable starts, audio device opens, scenarios replayed by the package against the target's SDL reference. |
| Phone platforms | `platforms/android/device-checks.mjs`, `platforms/ios/simulator-checks.sh` | 2 games each by default | 5-15 min per game | Install, start, captures against emulator/simulator references, system keys (Android), touches, lifecycle, audio device opens. Experimental, never gating. |
| Benchmarks | `benchmarks/PerfBench`, `benchmarks/AssetBench` | 2 programs | manual | Synthetic load with `--profile` JSON/CSV. Referenced by no workflow (F9). |
| Documentation integrity | `site/scripts/snippets.mjs`, `check-dist.mjs`, `api-reference.mjs --check`, `sdk/version.mjs check` | n/a | in CI | Quoted code equals sample regions; no dead links; no engineering material on public pages; `docs/api` matches XML comments; version references consistent. |

Totals: **533 .NET tests in ~45 s wall locally** (build 15 s incremental). The .NET layer is fast; it is not where CI
time goes.

### CI wall-clock (from `gh run list` / `gh run view`, 2026-11-02)

| Workflow | Recent runs | Wall-clock | Notes |
| --- | --- | --- | --- |
| `ci.yml` pull request | 41522728636 (feat/asset-1) | 14.5 min | Critical path = WebAssembly builds (4.6-5.9 min each) then site; .NET Linux 2.0 min, Windows 2.4, native Linux 3.2, SDK external 1.7-4.5. Native packages **skipped**. |
| `ci.yml` push to `main` | 41525164003, 41468588480, 41386577422 | 2.9-3.5 min (mostly evidence hits), 14.7 min when rebuilding | Evidence reuse works as designed. |
| `validate.yml` | 41515138843 (main), 41515347239, 41510176255 | 22.6-47.2 min, **all three failed** | See F1. |
| `platforms.yml` | 41515353091 ok, 41510181375 failed | 12.6-37.6 min | iOS jobs dominate (8-15 min; one cancelled at 21-43 min). |

Flakiness signals: 5 of the last 30 `ci` runs failed and 6 were cancelled (cancellations are superseded pushes by
design). Failures were on feature branches during development; no evidence of a recurring flaky test was found in the
sample, but see F12 for a latent one.

---

## 2. Findings summary

| ID | Finding | Class | Severity |
| --- | --- | --- | --- |
| F1 | Release-gating package checks are red on `main`: per-platform references drifted and daily CI never runs the checks that would notice | Confirmed problem | High |
| F2 | The visual oracle (32x18 cell means, tolerance 3/255) misses real regressions and fails benign changes (measured) | Confirmed problem | High |
| F3 | Seven per-platform references for two renderers; every renderer evolution (SDL_GPU, linear, MSAA, sRGB) means re-recording all of them on their own runners | Risk / design limitation | High |
| F4 | Backend "equivalence" is exact pixel equality; the only semantic probe is SDL-only, non-reusable, and too small: no conformance suite a new backend can run | Missing capability / confirmed problem | High |
| F5 | Headless rendering counts calls only; tests assert exact draw-call counts (implementation coupling); 500+ headless tests prove nothing about pixels | Legitimate specialization + risk | Medium |
| F6 | The browser build's frames, keyboard, audio and frame rate are checked by a suite that no workflow runs | Confirmed problem | High |
| F7 | The default desktop backend (SDL) has no native keyboard test and no audio-output (signal) test; only raylib does | Confirmed problem | Medium-High |
| F8 | Scripted input forces actions on `InputMap`: captures prove logic and drawing, not input; no latency measure; no player/device dimension | Limitation / missing capability | Medium |
| F9 | No performance regression detection: benchmarks are manual; only allocation budgets gate | Missing capability | Medium |
| F10 | Windows rendering (the default D3D11 path) is not checked in daily CI | Risk | Medium |
| F11 | Native conformance tests are Inconclusive on macOS because of test-runner threading | Limitation | Low-Medium |
| F12 | Latent flaky bound in `AssetGalleryTests` (120 x 1 ms) where a sibling test needed 30 s on busy runners | Risk | Low |
| F13 | Demos consume the engine by project reference with a hand-imported targets file; CLI has no unit tests (dangling `InternalsVisibleTo MicroEngine.Cli.Tests`) | Legitimate specialization + limitation | Low-Medium |
| F14 | Documentation drift: `docs/ci.md` says gamepads are covered by no check; `docs/ci.md` "Browser tests" implies an automated byte comparison that does not run | Confirmed problem (docs) | Low |
| F15 | 3D readiness: oracles, headless renderer and scripts are 2D-shaped | Missing capability (later) | Medium |
| S1 | Strengths to keep: evidence-keyed CI, simulated-clock pacing tests, `CaptureAudioDevice`, `TextureFilterTests`, external SDK acceptance, package verification levels, docs integrity | Legitimate | — |

---

## 3. Findings in detail

### F1 — Release-gating package checks are red on `main` (High, confirmed)

1. **Exists.** Native packages are checked by `packaging/verify.mjs`, which replays every scenario with the package's
   executable and compares against the target's SDL reference (`packaging/targets.json:12,27,43,58`; Windows runners
   use `sdl-windows-ci.json`, `.github/workflows/checks.yml:405`). These jobs are "Supported" in `validate.yml` and in
   `release.yml`, i.e. they gate a release (`docs/ci.md`, "Gates and required checks").
2. **Problem.** Validate run **41515138843 on `main`** failed: every desktop package job for Windows and Linux failed
   (8 jobs, 1.7-4.1 min each). The ASSET-1 record attributes it to "Windows WARP capture references recorded before
   PERF-2 and the Debian 11 glibc message check; not caused by ASSET-1, left to their owners"
   (`odd/tasks/asset-1.md:93-94`). A release from `main` today would be refused. The drift went unnoticed because daily
   CI skips package builds (`Package ${{ matrix.game }}` skipped in run 41522728636) and validate is manual only.
3. **Evidence.** `gh run view 41515138843`; `odd/tasks/asset-1.md:93`; also 41515347239 and 41510176255 failed the
   same jobs.
4. **Other projects.** Godot and Chromium-style projects run the expensive matrix on a schedule (nightly) precisely so
   reference drift surfaces within a day, and attribute failures to the commit range; references owned by "whoever
   changes visuals" decay when the check that consumes them is not on the change's path.
5. **Alternatives.** (a) Run validate nightly on `main` (runners are free for this public repo,
   `memory: public-repo-ci`); (b) add one package check per changed backend to daily CI; (c) make the package check
   compare against the *same* reference the daily native job uses, so a re-record in daily CI covers packages.
6. **Proposal.** (a) + (c) now: a scheduled validate on `main`, and collapse package references into the renderer-family
   references of the new strategy (section 4) so one re-record serves both the daily job and the package job.
7. **Consequences.** Drift is caught within 24 h; the "who owns the reference" problem disappears because there is one
   reference per renderer family, re-recorded in the PR that changes visuals.

### F2 — The visual oracle has measured blind spots and false alarms (High, confirmed by experiment)

1. **Exists.** `web/tests/captures.mjs:10-11,54-69,76-93`: each capture is reduced to the mean RGB of each cell of a
   32x18 grid; a run passes when no cell's mean moves by more than 3/255. Its header claims "A missing sprite, a wrong
   frame or a changed HUD digit moves a cell well past the tolerance" (`captures.mjs:5`). ADR 0011 justifies it as
   catching "the regressions that matter".
2. **Problem.** At the demos' capture size (960x540, `reference/*.json`: 126 of 138 captures), a cell is 30x30 = 900
   pixels. A cell mean is invariant to any rearrangement of pixels *inside* the cell and dilutes small changes.
3. **Evidence (experiment).** `...\exp-infra\fingerprint\sensitivity.mjs` builds a synthetic pixel-art frame
   (scale 3, like a 320x180 game at 960x540), mutates it and runs the repository's own `summarise`/`compare`
   (imported read-only). Output (`...\exp-infra\fingerprint\result.txt`):

   | Change | Verdict | Largest cell delta |
   | --- | --- | --- |
   | 12x12 px coin missing | detected | 37 |
   | 6x6 px bullet missing | detected | 9 |
   | HUD "13" -> "73" | detected | 10 |
   | **HUD "13" -> "18"** | **passes** | 3 |
   | hero moved 3 px across a cell boundary | detected | 20 |
   | **gem moved 12 px (4 source pixels) inside one cell** | **passes** | **0** |
   | **3x3 blur (stand-in for nearest -> linear sampling)** | **passes** | 2 |
   | +2 on every channel (driver rounding) | passes | 2 |
   | **+4 on every channel (benign sRGB/gamma rounding)** | **fails** | 4 |

   So the oracle (i) cannot see positional regressions that stay within a cell, (ii) cannot see some HUD digit changes
   (contradicting its own comment), (iii) cannot see a sampling-filter regression of the ASSET-1 kind, and (iv) fails on
   a uniform 4/255 shift that no player can see. It is simultaneously too loose and too strict, in the wrong places.
4. **Other projects.** Perceptual metrics (NVIDIA FLIP, SSIM/MS-SSIM, pixelmatch with anti-aliasing detection, Skia
   Gold's fuzzy matching with per-test parameters) weight local structure rather than cell means; WebKit/Chromium
   layout tests use per-test fuzzy specs ("max difference, max pixel count"). Another agent covers this in depth.
5. **Alternatives.** Finer grid (cheap, but still permutation-blind inside cells and stricter on noise); store PNG
   references and use a structural/perceptual metric with per-scenario thresholds and masks; replace many pixel checks
   with *state* probes (entity positions, HUD values logged by the game script) plus a few image goldens.
6. **Proposal.** Section 4, tier T2: keep fingerprints only as a fast pre-filter; decide pass/fail with a per-pixel
   fuzzy rule (count of pixels whose difference exceeds a threshold, plus a perceptual score) on stored PNG
   references, and add scripted state assertions so "the hero is at x" is checked as a number, not as pixels.
7. **Consequences.** Requires storing references as images (ADR 0011 rejected ~7 MB per system; with one reference per
   renderer family and PNG-8/WebP-lossless this shrinks, or references live in the release/artifact store keyed by
   content like demo packages). Diffs become human-diagnosable images instead of "cell 23,13 moved by 37".

### F3 — Per-platform references multiply with every renderer evolution (High, risk)

1. **Exists.** Seven references, each with all 138 captures: `linux-x64` (raylib), `sdl-linux-x64`, `sdl`
   (Windows GPU: D3D11/OpenGL/Vulkan identical), `sdl-windows-ci` (WARP), `sdl-macos-arm64`, `sdl-android-x86_64`,
   `sdl-ios-simulator-arm64`.
2. **Problem.** An intended visual change must be re-recorded per environment, some of which exist only on hosted
   runners or emulators; 14 of the 259 public commits touch `web/tests/reference`, often as series ("re-record ... on
   Windows", "... Linux (raylib)", "... Linux (SDL)", "... iOS simulator": 6796dd2, 73c9033, c8bdd62, 2923ac3). The
   stale WARP reference of F1 is the predictable outcome. A planned renderer change (SDL_GPU renderer, linear filtering
   default, MSAA, sRGB-correct blending) would change most of 7 x 138 fingerprints at once, and the metric cannot
   separate "renderer changed as intended" from "renderer regressed".
3. **Evidence (experiment).** `...\exp-infra\crossref\crossref.mjs` compares each reference against every other with the
   repository's own `compare` (`result.txt`):
   - raylib Linux vs SDL Linux: 136 of 138 within tolerance, **0 failing** — one reference would serve both backends.
   - SDL macOS vs iOS simulator: 111 identical; the 27 failures are the touch-first Controls screens and CourtyardKey.
   - Across other pairs, failing captures have a median worst-cell delta of **6-12**, concentrated in CourtyardKey
     (all scenarios) and the Controls screens (`names.mjs` output): just above the tolerance of 3.
   So most per-platform references exist because the metric is over-strict on driver noise (F2-iv), not because the
   platforms draw different games. Content differences (touch-first Controls screens) are legitimate and should be
   modelled as *variants of a scenario*, not as whole new references.
4. **Other projects.** Skia Gold and Chromium keep per-configuration baselines but triage them with a service and fuzzy
   matching; Godot's rendering tests are few and mostly manual; Unity's Graphics Test Framework keeps per
   platform/API reference images *and* per-test `ImageComparisonSettings` (per-pixel threshold, allowed bad pixels),
   which is the part MicroEngine lacks.
5. **Alternatives.** Keep per-platform references (status quo, high maintenance); one reference per renderer family
   with per-scenario tolerances; reference-free invariants (geometry/state) for most checks.
6. **Proposal.** One golden set per *renderer family* (CPU reference rasterizer, SDL_Renderer family, raylib/GL family)
   compared with justified fuzzy tolerances; platform-specific screens as declared scenario variants; per-platform
   references only where a measured delta justifies one, recorded in the reference file with the measurement.
7. **Consequences.** Renderer evolution becomes one re-record and a reviewable image diff, not seven; the WARP/GPU
   split disappears unless measured necessary.

### F4 — No backend conformance suite; equivalence is exact equality (High)

1. **Exists.** `tests/MicroEngine.Desktop.Tests/PlatformEquivalenceTests.cs`:
   `SdlDrawsEveryRendererOperationWithTheEngineConventions` (lines 151-177) asserts specific pixels of a 64x36 probe
   within ±1 per channel — but only for SDL. `TheSameGameDrawsTheSameFrameOnSdlAndRaylib` (lines 196-201) and
   `EverySdlGpuDriverDrawsTheSameFrame` (lines 180-194) require `AssertSameColours` (lines 124-136): **exact** RGB
   equality on every pixel. `TextureFilterTests.cs` is the one well-shaped contract test: one `Check` (lines 78-92) run
   against both SDL and raylib (lines 95-98), asserting properties (hard edge vs monotone ramp, region update), not
   bytes.
2. **Problem.** The replaceability promise (request §6) needs a suite a *new* backend can run to prove it implements
   `IRenderer`. Today raylib is only "correct" because it equals SDL, and SDL is only checked on integer-aligned,
   nearest-sampled, axis-aligned cases: rotation is drawn but not asserted (line 63), circle/line only at their centre,
   no sub-pixel positions, no camera zoom/rotation, no render target, no text, no premultiplied alpha, no
   `DrawStretched`. A conforming backend that rasterizes edges differently (SDL_GPU with its own shaders, MSAA, a
   Vulkan or WebGPU backend, a future 3D renderer providing the 2D API) fails exact equality while being correct.
3. **Evidence.** Lines cited above; `docs/ci.md` "Native desktop checks" describes equivalence only in terms of captures.
4. **Other projects.** Vulkan CTS / dEQP (Android's GPU conformance) define correctness per operation with explicit
   precision rules and reference rasterizers, not "equal to another driver"; wgpu's GPU tests and Mesa's piglit run the
   same parameterized tests against every backend with per-test tolerances; MonoGame keeps reference-image tests per
   platform with tolerance.
5. **Alternatives.** Keep pairwise equality (cheap, brittle); a semantic conformance suite parameterized by
   `PlatformProfile`; a CPU reference rasterizer as oracle.
6. **Proposal.** Tier T1 in section 4: an abstract `RendererConformance` test class (MSTest supports inheritance or
   `DynamicData` over profiles) that every backend instantiates, including headless-with-CPU-raster; each test states
   the contract (coverage rule, colour error bound, filter property, blend equation, clip containment, transform) and
   its tolerance. Pairwise exact equality becomes an informative report.
7. **Consequences.** New backends get a checklist; renderer rewrites can be judged against contracts; requires writing
   down the engine's rasterization conventions (pixel centres, edge rules), which is itself an architectural gain.

### F5 — Headless rendering counts calls; tests couple to call counts (Medium)

1. **Exists.** `HeadlessRenderer` (`src/MicroEngine/Core/HeadlessPlatform.cs:221-270`) increments `DrawCalls` and
   tracks camera balance; blend and clip are ignored (lines 267-269); `HeadlessGraphicsDevice` textures have no pixels
   (lines 184-218).
2. **Problem.** The 500+ headless tests prove simulation and API usage, not what is drawn. Where they do test drawing,
   they assert implementation counts: `LightMapTests.cs:38` expects `180 / 4` draw calls, `ScalableFontTests.cs:41`
   "one quad per glyph", `ContentLoadingTests.cs:226` three copies. A batched sprite renderer, a shader-based light
   map, or SDF text would break these tests while being correct.
3. **Evidence.** Lines cited; 10 `DrawCalls` assertions across tests.
4. **Other projects.** MonoGame/FNA and many engines test against a *recording* graphics device (a command list with
   parameters) so tests can assert "a quad covering X with colour C was drawn" independently of batching.
5. **Alternatives.** Recording renderer (command list); CPU reference rasterizer behind the headless device.
6. **Proposal.** Headless renderer records commands (operation, transformed bounds, colour, texture, blend, clip,
   camera) and optionally rasterizes them on the CPU reference rasterizer; rewrite the call-count assertions as
   coverage/bounds assertions.
7. **Consequences.** Headless tests can assert visual intent without a GPU; the CPU rasterizer doubles as the T1 oracle.

### F6 — Browser build frames and real browser input are not checked automatically (High, confirmed)

1. **Exists.** `web/tests/run.mjs` has platform, scripts (byte comparison with desktop raylib), autopilot, audio
   (signal level), framerate and keyboard suites (`run.mjs:5-17`).
2. **Problem.** No workflow runs it: `grep "web/tests"` over `.github/workflows` finds only `desktop.mjs` and
   `mobile.mjs`. Daily CI runs `site/tests/browser.mjs` (layout/play/touch), whose game frames are screenshots "for
   review" (`browser.mjs:7,189`); `mobile.mjs` screenshots are artifacts. The browser is the public demo surface, and
   WebAssembly-specific regressions (AOT, WebGL context, audio unlock, key events, timing) are covered only by
   "loads, plays, restarts, exits".
3. **Evidence.** grep output above; `docs/ci.md` "Browser tests" states the captures "are compared byte for byte with
   the desktop build" without saying it is manual (F14).
4. **Other projects.** Emscripten/Godot web exports are smoke-tested in headless browsers in CI; Playwright/puppeteer
   visual checks with fuzzy thresholds are standard.
5. **Alternatives.** Wire `run.mjs platform,scripts,keyboard` into the site job for rebuilt packages; drop byte equality
   in favour of the T2 comparison against the raylib/GL family reference.
6. **Proposal.** Now: run `run.mjs scripts,keyboard,framerate` on every rebuilt demo package in daily CI (Chrome; Firefox
   in validate), judged by the T2 metric rather than bytes. Audio suite in validate.
7. **Consequences.** +3-5 min on demo-building runs (already the critical path; can run beside the site job).

### F7 — SDL (the default desktop backend) has no native keyboard or audio-output check (Medium-High, confirmed)

1. **Exists.** SDL is the default desktop platform (`src/MicroEngine.Desktop/DesktopLauncher.cs:9,18`). Native keyboard
   checks (`tests/MicroEngine.Raylib.Tests/KeyboardTests.cs`, XTEST) and the tone-reaches-the-sink check
   (`AudioTests.cs:28-80`) exist only for raylib. SDL has `SdlAudioTests` (allocation budget only) and `GamepadTests`
   (virtual joystick: good).
2. **Problem.** The backend players actually run is not checked for key delivery (held/tap/sub-frame), nor for audible
   output, mixing, panning or pitch. `packaging/README.md` states audio is checked as "the device opened, not listened
   to"; Android/iOS likewise (`platforms/README.md`, evidence table).
3. **Evidence.** Lines cited; no keyboard test in `tests/MicroEngine.Desktop.Tests`.
4. **Other projects.** SDL's own test suite injects events with `SDL_PushEvent` and uses the `disk` audio driver to
   capture output deterministically; FNA/MonoGame test audio through loopback or offline mixing.
5. **Alternatives.** Port the raylib tests to SDL (XTEST on Linux); use SDL's `disk` audio driver (deterministic file
   output, no sound server) for signal-level checks on every OS.
6. **Proposal.** T1 input and audio conformance suites parameterized by backend: SDL keyboard via XTEST on Linux and
   `SDL_PushEvent` elsewhere; audio via SDL `disk` driver and PulseAudio monitor, checking level, frequency, pan and
   pitch against `CaptureAudioDevice`'s expected signal.
7. **Consequences.** Audio becomes verifiable without hearing it on every OS; the mixer's backend fidelity (resampling,
   pan law) becomes a contract.

### F8 — Scripted input bypasses devices (Medium, limitation)

1. **Exists.** `InputScript.Apply` calls `InputMap.SetForced(action, down)` (`src/MicroEngine/Core/GameRunner.cs:541-546`);
   touches go through `VirtualControls` hit testing (lines 503-517). Every capture scenario, game playthrough and
   package check drives input this way.
2. **Problem.** Captures prove logic and drawing, not bindings, device sampling, edge timing or latency. Scripts have
   no player or device dimension, so local multiplayer or simultaneous devices cannot be scripted. Input latency
   (event to presented frame) is measured nowhere.
3. **Evidence.** Lines cited; `docs/ci.md` "Scripts force actions on InputMap".
4. **Other projects.** Unity's Input System test fixture injects device events (`InputTestFixture`); Godot's
   `Input.parse_input_event` injects at the event level.
5. **Alternatives.** A device-level script mode (keys/buttons per device id) through `HeadlessInput`/backend injection.
6. **Proposal.** Add device-level script commands (`key`, `pad <n>`) routed through `IInputSource` sources, keep action
   forcing for game tests; a latency probe in PerfBench (event timestamp to present, SDL only) as T4.
7. **Consequences.** Input conformance and multiplayer become testable; small script format extension.

### F9 — No performance regression detection (Medium, missing capability)

1. **Exists.** `FrameProfiler` and `--profile` (ADR 0023), PerfBench and AssetBench; allocation budgets
   (`PerformanceTests.cs:252` `< 256` bytes per frame; `WorldTests` zero allocation; `SdlAudioTests`).
2. **Problem.** No workflow runs the benchmarks (grep: only `docs/decisions/0023`, `docs/evaluations/perf-*.md`,
   `odd/tasks/*`). CPU time of update/render, asset load time and GPU frame time can regress silently.
3. **Evidence.** grep output.
4. **Other projects.** Godot's benchmark repository publishes per-commit results; .NET runtime uses BenchmarkDotNet
   with trend storage and regression alerts; shared runners require relative comparison (same job, base vs head).
5. **Alternatives.** Absolute thresholds (flaky on shared runners); base-vs-head comparison in the same job; trend
   storage with alerts.
6. **Proposal.** Later: a CI job that runs PerfBench headless and on llvmpipe for base and head in the same runner,
   comparing deterministic metrics (allocations, draw commands, simulated steps) strictly and CPU time with a wide
   relative band; publish the JSON as an artifact for trends.
7. **Consequences.** Catches gross regressions; GPU frame time stays manual on real hardware.

### F10 — Windows rendering absent from daily CI (Medium, risk)

`dotnet-windows` runs `TestCategory!=Native` (`checks.yml:155`), so `PlatformEquivalenceTests` and
`TextureFilterTests` never run on Windows in CI; the Windows GPU reference `sdl.json` is used only by the package job
(`packaging/targets.json:12`) and WARP only in validate/release (`checks.yml:405`). The developer's primary platform and
SDL's Direct3D 11 default are validated by hand and by the manual validate run that is currently red (F1). Proposal:
run the native category on the Windows job with `SDL_RENDER_DIRECT3D11_WARP=1` (already used for packages) and judge
by T1 contracts plus T2 fuzzy goldens.

### F11 — Conformance tests are Inconclusive on macOS (Low-Medium, limitation)

`PlatformEquivalenceTests.NotOnMacOS` and `TextureFilterTests.NotOnMacOS` mark tests Inconclusive because windows must
be opened on the main thread and MSTest runs tests elsewhere (`PlatformEquivalenceTests.cs:26-34`). A conformance suite
must not depend on the test runner's threading. Proposal: run T1 suites as a small *conformance runner executable*
(main thread, any backend, JSON results) that MSTest, CI and phone hosts can all invoke; the same runner can run on
Android/iOS hosts.

### F12 — Latent flaky timing bound (Low, risk)

`AssetGalleryTests.cs:58` waits at most 120 iterations of `StepForTest` + `Thread.Sleep(1)` for a background load,
while `AssetCacheTests.cs:181-187` had to move to a 30 s deadline because "workers may start late on a busy two-core
runner". Same failure mode, weaker bound. Proposal: deadline-based waits everywhere, or a deterministic synchronous
decode mode for tests that do not test threading.

### F13 — Demos as SDK consumers; CLI testing (Low-Medium)

Demos use `ProjectReference` to `MicroEngine.Desktop` and import `MicroEngine.targets` by hand
(`games/Directory.Build.targets`), whereas packages import it automatically: the demos do not exercise the package
shape. Samples reference packages and switch to source inside the repository (`samples/Directory.Build.targets`), and
`sdk/tests/docs.mjs`/`external.mjs` exercise the package shape for samples and the template game. This is a
legitimate trade-off (fast feedback on engine changes) as long as the package path is covered, which it is, but no
*demo* is ever built from packages, so content/packaging behaviour that only demos use (CC0 content folders, scripts
dir, splash branding) is checked only in source form. Separately, `src/MicroEngine.Cli/MicroEngine.Cli.csproj:29`
grants `InternalsVisibleTo MicroEngine.Cli.Tests`, a project that does not exist; the 1,884-line CLI is tested only
end-to-end by `external.mjs` (2-5 min per OS). Proposal: one demo built from the local feed in the SDK job
(`MicroEngineUsePackages`-style switch for games); a fast CLI unit test project or removal of the dangling grant.

### F14 — Documentation drift in `docs/ci.md` (Low, confirmed)

"Gamepads are covered by none of these checks" (`docs/ci.md`, "Native desktop checks") is contradicted by
`GamepadTests.Pads_connect_are_recognised_press_and_hand_over_when_unplugged`, which runs in native Linux CI
(`checks.yml:245`). The "Browser tests" section describes byte-for-byte browser/desktop comparisons that run in no
workflow (F6). Validation documentation must say what runs where, or readers overestimate coverage.

### F15 — 3D readiness of the test infrastructure (Medium, later)

The oracle is a 2D colour grid; `HeadlessRenderer` mirrors the 2D `IRenderer`; scripts address 2D touch controls.
3D needs depth/stencil, geometry invariants (projected vertex positions, triangle coverage, z-order), material/shader
compilation checks per backend, and tolerance-based image comparison (3D rasterization differs across GPUs far more
than 2D blits). None of these need implementing now, but the T1 harness (contract tests parameterized by backend,
command recording, CPU reference raster) and the T2 fuzzy metric are prerequisites; the current exact-equality and
cell-mean approach would not survive the first lit 3D scene.

### S1 — What to keep (legitimate)

- **Evidence-keyed CI** (`.github/scripts/evidence.mjs`, `changes.mjs`, `projects.mjs`): checks run once per content;
  merge to `main` re-runs nothing proven (3 min pushes). Tested rules (22 + 6 + 8 tests).
- **Deterministic simulation and timing tests**: pacing, jitter and refresh-rate behaviour tested with simulated
  clocks (`PerformanceTests.cs:74-157`), not wall time — the right way to test frame pacing.
- **`CaptureAudioDevice`**: audio *requests* verified deterministically by game time (`PerformanceTests.cs:268-284`):
  "checks that are never heard" is a sound principle for engine semantics; it only lacks the backend-fidelity half (F7).
- **`TextureFilterTests`**: a property-based contract run against both backends — the model for T1.
- **SDK acceptance from outside the repository** (`sdk/tests/external.mjs`): isolates NuGet cache, home, secrets;
  checks developer-facing failure messages. Rare and valuable.
- **Package verification levels** (`packaging/README.md`): integrity, structure, no build paths, dependency floors.
- **Documentation integrity**: snippet equality, link checks, API reference freshness, version consistency.

---

## 4. Proposed validation strategy

Principle: each tier proves a *contract* with a *justified* tolerance; recorded output is a regression net, never the
definition of correctness.

### Tiers

| Tier | Proves | Oracle | Tolerance policy | Runs |
| --- | --- | --- | --- | --- |
| T0 Simulation | engine and game logic, timing, lifecycle, asset cache, audio requests | assertions on state; simulated clock; `CaptureAudioDevice` | exact (deterministic) | every change, Linux + Windows (+ macOS informative) |
| T1 Conformance (per subsystem, per backend) | a backend implements the contract: renderer, texture/filter, input devices, audio output, window/lifecycle, storage, asset decoders | semantic probes + CPU reference rasterizer for pixel expectations; synthetic signals for audio; injected events for input | per test, written next to the assertion and justified: exact for nearest-sampled integer-aligned texels; ±1-2/255 for blend equations; edge position ±0.5 px for rasterization; monotonicity/ramp properties for linear filtering; frequency ±5 %, RMS bands for audio; same-update or next-update for input edges | every change to the backend or the contract: headless+CPU raster everywhere; SDL and raylib on Linux (llvmpipe/Xvfb) and Windows (WARP); macOS/phones through the conformance runner in `platforms.yml` |
| T2 Scene goldens | the demos and samples still look right end to end | stored PNG goldens per renderer family + scenario variants (touch-first screens) + scripted state assertions (positions, HUD values logged by the script) | fuzzy: per-pixel threshold + max differing pixels + a perceptual score (FLIP or SSIM, chosen by the perceptual-diff research), masks for animated/noisy regions; fingerprints kept as a fast pre-filter and change summary | every change reaching a game; one re-record per renderer family in the PR that intends the change; scheduled nightly on all platforms |
| T3 Platform and package smoke | the shipped artefact starts, presents frames, opens devices on each target | process exit, logs, T2 subset on the package | as T2 | release, nightly validate, plus one target per changed backend in daily CI |
| T4 Performance | no gross regression in CPU time, allocations, load time; pacing quality | base-vs-head in the same job; trend artefacts | strict for deterministic counters (allocations, commands, steps); wide relative band (for example 15-25 %) for CPU time | nightly; on demand in PRs touching the loop, renderer or assets |

### Harness pieces this needs

1. **Recording headless renderer + CPU reference rasterizer** (replaces call counting, F5): gives T0 visual intent and
   T1 an oracle that is not "another GPU driver".
2. **Conformance suites as reusable code**: abstract test classes (or a conformance library) parameterized by
   `PlatformProfile`, plus a **conformance runner executable** that runs them on the main thread and writes JSON
   (fixes macOS, enables phones, lets third-party backends run the same suite: F4, F11).
3. **Device-level script commands** (F8) and a backend event-injection interface for tests (XTEST, `SDL_PushEvent`,
   virtual joysticks).
4. **Audio capture backends**: SDL `disk` driver and PulseAudio monitor behind one test helper (F7).
5. **Image goldens store**: PNG goldens keyed by renderer family; stored like demo packages (content-addressed release
   assets or Git LFS), diff images uploaded on failure (F2, F3).
6. **Documented coverage matrix** generated from the suites (what runs where), replacing prose that drifts (F14).

### CI shape

**Now (before or with Phase II's first renderer change):**
- Scheduled nightly `validate` on `main` (free runners) so reference and package drift surfaces within a day (F1).
- Wire `web/tests/run.mjs scripts,keyboard,framerate` into daily CI for rebuilt demo packages (F6).
- Run the `Native` category on the Windows job with WARP (F10).
- Port keyboard and audio-signal checks to SDL (F7) as the first T1 suites; generalize `TextureFilterTests` and the
  probe of `PlatformEquivalenceTests` into a parameterized renderer conformance class; demote exact cross-backend
  equality to an informative report (F4).
- Fix `AssetGalleryTests` bound (F12) and `docs/ci.md` drift (F14).

**Before replacing renderers or changing defaults (SDL_GPU, linear filtering, MSAA):**
- Recording headless renderer + CPU reference rasterizer; rewrite call-count assertions (F5).
- T2 fuzzy goldens per renderer family with scenario variants; retire the per-platform cell-mean gate except as a
  pre-filter (F2, F3).
- Device-level scripts (F8).

**Later (3D, broader platforms):**
- 3D conformance (depth, geometry invariants, shader compilation per backend), T4 trend tracking with GPU timings on real
  hardware, input-latency probe, conformance runner on Android/iOS hosts (F9, F15).

### Expected cost and wall-clock

The .NET tiers are cheap (533 tests in ~45 s locally); the CI critical path is the WebAssembly build (~5-6 min) and
package/phone jobs. Adding T1 native suites costs roughly the current native Linux job again (~3 min) and runs in
parallel; browser `run.mjs` adds 3-5 min beside the site job; nightly validate costs nothing in money for this public
repository and ~45 min of wall-clock nobody waits for.

---

## 5. Scratch artefacts (outside the repository)

- `...\exp-infra\timing.txt`, `...\exp-infra\build.log`, `...\exp-infra\test-*.log`: local build and per-project test
  timing.
- `...\exp-infra\fingerprint\sensitivity.mjs`, `result.txt`, `png\`: oracle sensitivity experiment (F2).
- `...\exp-infra\crossref\crossref.mjs`, `names.mjs`, `result.txt`: cross-reference comparison (F3).

(`...` = `<scratch>`.)
The local `dotnet build MicroEngine.slnx -c Release` wrote only to git-ignored `bin/` and `obj/` folders in the
repository; no tracked file changed (`git status` shows only `odd/tasks/arch-1.md`, which this auditor did not create).
