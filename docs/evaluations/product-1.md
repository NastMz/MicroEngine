# PRODUCT-1 + DIST-1: product experience and demo distribution

Date: 2026-11-01. Branch `feat/product-1`. Task record: [`odd/tasks/product-1.md`](../../odd/tasks/product-1.md). Decisions:
[ADR 0018](../decisions/0018-native-demo-distribution.md) (downloads) and [ADR 0019](../decisions/0019-sdk-packages.md)
(SDK Alpha and packages).

Evidence words as in [PLATFORM-3](platform-3.md): **run** means executed here (Windows 11 PC with an NVIDIA GPU, WSL2,
Docker, the Android emulator) and its output read; **CI** means GitHub-hosted runners of this repository (validation
run 41265744009 for the desktop packages).

## 1. Audit of the public site and documentation

What the site and documentation said before this work, against the repository after PLATFORM-3:

| Claim | Where | Reality | Done |
| --- | --- | --- | --- |
| The desktop backend is raylib; architecture diagram: "raylib · desktop window" | landing, introduction | SDL 3 is the default on five systems; raylib is the browser platform and a desktop alternative | landing map and introduction rewritten |
| "Desktop validated on Windows only"; Linux and macOS "may work… not verified" | landing, `Platforms.astro` | Linux and macOS are checked on CI with their own references | platform summary from one data file |
| "No touch controls"; phones "Not supported" | landing, demo pages, input manual | the SDL hosts have on-screen controls; touch is not a game-readable API | corrected everywhere, with the precise limit |
| Android: "one demo… keys only, no touch yet"; iOS "needs a new platform layer" | roadmap | both hosts run on SDL; Android has touch | roadmap rewritten |
| "Engine development setup… private MicroEngine repository" | getting-started.md | the repository is public | corrected |
| "Linux not yet supported, macOS under evaluation" | README, getting-microengine, getting-started, limitations | all six are official targets, each at a stated verification level | wording changed to levels of evidence |
| "No versioned or published API" | status, limitations | now SDK Alpha 0.2.0-alpha with migration notes; still not published | SDK status page |
| Android and iOS: "two demos" | platforms, status | four on the Android emulator (this work); iOS still two | updated |
| The engine's own definition | introduction, landing | "a compact, code-first 2D game framework" did not say what MicroEngine adds over SDL and raylib | definition: an engine that orchestrates subsystems and integrates existing technologies |

Kept as they were: the engine manual chapters (lifecycle, scenes, rendering, audio, assets, UI, persistence,
achievements), the contracts and components references, the examples and the architecture page, checked against the
code they quote (the site still reads code excerpts from the source files at build time). Changes were limited to
statements that had become false.

## 2. Native packages

| Target | Format | Built | Played locally | Played in CI |
| --- | --- | --- | --- | --- |
| Windows x64 | ZIP: single-file exe, `SDL3.dll`, `raylib.dll`, Content, notices | run, CI | all four: 138/138 captures pixel-identical to the GPU reference, Direct3D 11, audio device opened | all four on WARP: 138 captures against the WARP reference; no sound device on the runner |
| Linux x64 | tar.gz, modes fixed (0755 for the executable and libraries) | run (cross-built), CI | WSLg: Neon Rift scripted run; Docker: Ubuntu 22.04 138/138 identical, Debian 12 138/138 (135 identical), Fedora 41 138/138 identical, audio device opened in each; Debian 11 refuses with a glibc error | all four under Xvfb (138 identical) and in the same four containers |
| macOS arm64 | ZIP of an ad-hoc signed `.app` | CI only (codesign needs a Mac) | — | all four: `codesign --verify --deep --strict` passed; 138/138 captures pixel-identical to the macOS reference on Metal; audio device opened; Gatekeeper: `rejected` (not notarized, as documented) |
| Android | APK, `arm64-v8a` + `x86_64`, signed | run with a throwaway test key and the debug key; CI path ready, needs the owner's key | all four on the emulator (API 36.1): installed, started, every scripted scenario, audio device, system keys, taps on the on-screen controls, background and back; 138 captures recorded as the emulator reference | not yet: the release key is not configured |
| iOS | none | — | — | — |

Measured: Neon Rift's Windows ZIP is 44 MB, its Linux archive 46 MB, Meadow Run's APK 17 MB. Executables need glibc 2.27,
SDL 2.34, raylib 2.38 (`objdump -T`).

### Found and fixed on the way

- **ICU.** The Linux package stopped at startup on a minimal Debian 11 without `libicu`: native packages now use
  invariant globalization, as browser and phone builds already did.
- **Unloadable SDL crashed.** On glibc 2.31 SDL's `DllNotFoundException` was unhandled; the launcher now reports it
  (`error: platform 'sdl' could not load its native library`, exit 1). The container check requires exactly that.
- **Content code in output.** Three games copied `Content/*.cs` (compiled sources) next to their executables.
- **False evidence caught.** The first Debian 11 run "passed" because the package manager failed and the game never
  ran; the check now needs the glibc error itself. The first path scan flagged `/home/runner` inside SDL and .NET
  binaries (their own build machines); it now looks for this machine's paths only.
- **Renderers.** Courtyard Key's half-pixel HUD and Trail Duel's rotated translucent sprites differ by a texel on the
  Android emulator and on WARP, as on Mesa and Metal: both got their own recorded references.

### What a check does not prove

- No person played a package. System keyboard delivery to a packaged desktop game was not checked (scripts drive the
  game's actions; system keys reach packaged games on Android).
- Audio is "the device opened", not listened to. Windows CI has no sound device.
- Gatekeeper's first-launch block and SmartScreen's warning were not reproduced in a real download; they follow from
  the packages being unnotarized and unsigned and are documented.
- The `arm64-v8a` code of the APK never ran.

## 3. Pipeline

Native packages share the content-addressed store and releases ([ADR 0018](../decisions/0018-native-demo-distribution.md)):
`demos.mjs plan --native` lists missing packages per game and target, `checks.yml` builds and plays each on its own
runner and stores it only after a passing check, `release.yml` copies them into the draft under player-facing names,
hashes every download again before publishing, and checks their public addresses after. Validation run 41265744009
built, played and stored the twelve desktop packages. Unit tests: `site/tests/native.test.mjs` (keys per target, store
refusals, release completeness, site download list).

Not run yet: a release with downloads (it needs the merge to `main`), and Android in CI (it needs the signing key).

## 4. SDK

`dotnet pack` produces `MicroEngine`, `MicroEngine.Sdl`, `MicroEngine.Raylib` and `MicroEngine.Desktop` at
`0.2.0-alpha`. A project outside the repository consumed them from a local feed: with `MicroEngine.Desktop`, Hello
Engine ran headless (30 updates) and listed four platforms; with `MicroEngine.Sdl` alone and its own catalog it listed
`sdl` and `headless`, and its output had no raylib library ([SDK status](../guide/sdk.md)).

## 5. Site

The landing is organised around the map of MicroEngine's layers (game → core → `IPlatform` → integrated libraries),
keeps the existing identity (dark workbench, amber accent, Pixelify Sans and IBM Plex, real captures), and adds the
desktop catalog's real code, the platform summary, the SDK Alpha status and the start commands. `/platforms/` is the
compatibility matrix. Demo pages offer "Download for your platform" from the release only. Checks are recorded in the
task log ([`odd/tasks/product-1.md`](../../odd/tasks/product-1.md)).
