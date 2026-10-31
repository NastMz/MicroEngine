# PLATFORM-2: cross-platform validation and efficient CI

Date: 2026-10-31. Branch `feat/platform-ci` (from `main` after PLATFORM-1). Runner experiments ran on the throwaway
branch `experiment/platform-2`. Task record: [`odd/tasks/platform-ci.md`](../../odd/tasks/platform-ci.md).

Evidence words are used strictly: **run** means it was executed and its output read; **estimated** means computed
from recorded job durations, not from a run of the new pipeline (the new `pipeline.yml` has not run yet: it runs on
pull requests and `main`, and no pull request was opened).

## 1. Audit of the pipeline before this change

Source: GitHub Actions runs of `pipeline.yml` on 2026-10-31 (41201969390 push to `main`, 41200620237 pull request
with engine changes, 41173308808 and 41169480241 pull requests without engine changes, and the demo builds of the
`feat/web-site` and `feat/platform-linux-macos` runs).

### Jobs, durations and dependencies

| Job | Runner | Duration | Needs | Ran when |
| --- | --- | --- | --- | --- |
| Plan | Linux | 19–25 s | — | always |
| .NET tests | Windows | 115–131 s (setup-dotnet 26 s, tests 89 s) | Plan | any change under `src`, `games`, `tests`, `samples`, `web/tests`, `.github` (always on `main`) |
| .NET tests | Linux | 107–113 s | Plan | same |
| .NET tests | macOS | 42–48 s | Plan | same; not blocking |
| Native Linux | Linux | 194–205 s (build 36 s, captures 129 s) | Plan | same |
| Native macOS | macOS | 21–28 s, always failing | Plan | same; not blocking |
| Build *Game* | Windows | 160–385 s each (toolchain cache hit: 160–180 s) | Plan | the game's demo key is not stored |
| Site | Linux | 180–260 s (browser checks 221 s of 260) | every job above | always |
| Deploy | Linux | 63–79 s (`vercel pull` 28 s, `vercel build` 29 s) | Site | Site passed |

Critical path of an engine change: Plan → Native Linux (≈200 s) → Site (≈220–260 s) → Deploy (≈70 s): 8–9 minutes.
The Site job waited for every test job although it only needs the demo packages.

### Redundancy and what really needs to run

- **One engine flag for everything.** Any change under `src`, `games`, `tests` or `samples` ran every .NET and native
  job on all three systems. A change to one game's tests ran macOS, both native jobs and the browser demos.
- **Browser checks on every run** (221 s of the Site job), including documentation-only pull requests and pushes to
  `main` whose packages were already played on the pull request. Documentation can break page layout (a wide table
  causing horizontal scroll), so the page checks are needed for documentation; playing the demos is not, unless a
  package was rebuilt or the web host or site changed.
- **Native macOS ran on every engine change and always failed** (no window), producing no evidence after its first run.
- **Builds repeated per job**: each .NET job and Native Linux build the solution (36–90 s each). Their runners differ
  (Windows, Linux, macOS, Linux with Xvfb), so sharing build output would save little and couple the jobs; kept.
- **Deploy rebuilds the site** with `vercel build` (29 s) after Site built it: needed, because `vercel build` produces
  Vercel's output format with the project's settings. Kept.
- **The fast tests did not gate the expensive ones**: a compile error occupied Windows and macOS runners in parallel.

### Caches

| Cache | Key | Size | Notes |
| --- | --- | --- | --- |
| npm (site) | `site/package-lock.json` | 100–167 MB (Linux), 73 KB (Windows) | setup-node |
| web toolchain | `web/toolchain.env`, `web/setup.sh`, `web/build-raylib-wasm.sh` | 1.0 GB | demo builds only |
| demo packages | content key per game (`site/scripts/demos.mjs`) | release assets | the artifact reuse mechanism |
| NuGet packages | none | — | every .NET job restored from nuget.org |

### What must invalidate existing artifacts

- A **demo package** is invalidated exactly by its content key: the engine under `src`, the web host and toolchain
  files, the game's folder (Markdown, screenshots and scripts excluded), its registry entry and the third-party
  notices. This was already right and is unchanged.
- The **web toolchain cache** by its three pinned files; the **NuGet cache** (new) by the project, props and targets
  files, `global.json` and `NuGet.config`.
- The **Android raylib library** (platform workflow) by `web/toolchain.env` (raylib version) and
  `platforms/android/toolchain.env` (NDK, API level), plus `build-raylib.sh` and `native/microengine_main.c`. It is
  rebuilt on every platform run (about a minute), so no cache yet.

### Runner cost

GitHub bills private repositories per started minute of each job, with minute multipliers of 1 (Linux), 2 (Windows)
and 10 (macOS). In Linux-equivalent minutes (rounded up per job, from the durations above):

| Change | Before | After (estimated) |
| --- | --- | --- |
| Engine code (`src/`), no demo rebuilt | 37 (Linux 13, Windows 2×2, macOS 2×10) | 27: Native macOS moved to the weekly platform workflow |
| One game's code | 37 | 17: no macOS (managed game code already ran on Linux and Windows) |
| One game's tests | 37 | 10: .NET tests on Linux and Windows only, site without browser checks |
| Documentation only | 7–8 | 6: page layout checks only (measured locally: 86 s instead of 221 s) |
| Task records (`odd/`) only | 7–8 | 4: site build without browser checks, deploy |
| Each demo package rebuilt | +3–7 per game (Windows ×2) | unchanged |

The weekly platform workflow adds its own cost (section 3), outside the publishing path.

## 2. New pipeline

See [Publishing the site: Pipeline](../deployment.md#pipeline) for the jobs and
[What runs for a change](../deployment.md#what-runs-for-a-change) for the execution matrix. In short:

- **Fast tier**: Plan (which now also runs the planner's own tests) and the Linux .NET tests.
- **Integration tier**: Windows .NET tests, macOS .NET tests (not blocking), Native Linux, demo builds and the site with
  the browser suites Plan chose. The Windows, macOS and native jobs start only after the Linux tests passed (or were
  not needed). The site no longer waits for the .NET jobs; Deploy waits for all of them and checks each result.
- **Full tier**: `platforms.yml`, weekly, manual, and on pull requests that change `platforms/`. Never blocks.

Estimated critical path of an engine change: Plan (≈25 s) → Linux tests (≈110 s) → Native Linux (≈200 s), in
parallel with Site (≈220 s), then Deploy (≈70 s): about 6.5 minutes instead of 8–9. Documentation: about 4 minutes
instead of about 6.

Unknown paths run everything, so a new top-level folder cannot silently skip checks; the planner's rules are tested
in the Plan job on every run.

## 3. Platform workflow runs

Eight runs of `platforms.yml` on `experiment/platform-2` (41208174266 to 41213988975). The first seven failed on
problems in the new jobs themselves, each found in the run's log and fixed:

| Run | Failure | Fix |
| --- | --- | --- |
| 1 | `yes \| sdkmanager` killed by SIGPIPE under `pipefail`; the newest ios workload (27.1) needs Xcode 27.1 | licences answered from a process substitution |
| 2 | emulator: "Not enough space to create userdata partition. Available: 3546 MB, need 7372 MB" | unused toolchains removed, `disk-size: 4G` |
| 3 | first scripted run stalled; screencap black | device woken and unlocked before playing |
| 4 | ios pack 26.5.10318 "requires Xcode 26.6" (pack and Xcode versions differ); `end` aborted in raylib's audio teardown (section 5) | the job selects the Xcode the pack names; input proven by scene changes |
| 5–6 | the iOS step hid its error; `showcase` aborted in the same teardown | iOS errors printed |
| 7 | trimmer warnings (IL2104) from framework assemblies stopped the iOS build; `timeout` does not exist on macOS | IL2104 not an error in the mobile hosts; perl alarm |
| 8 | none: all three jobs passed | |

Job durations in run 8: macOS 37 s, iOS 186 s, Android 499 s. One run costs about 9 Linux minutes plus 5 macOS
minutes (×10): about 60 Linux-equivalent minutes, about 240 a month when weekly.

## 4. macOS

**Exact failure** (pipeline run 41201969390, 15 of 15 scripts): `GLFW: Error: 65545 Description: NSGL: Failed to
find a suitable pixel format`, then `Failed to initialize Window`.

**Cause**, from `platforms/macos/gl-probe.m` on the hosted runner (macOS 26.6.2, arm64, 5 cores):

| Question | Result |
| --- | --- |
| Metal device | "Apple Paravirtual device" |
| OpenGL renderers | one: `0x01020400`, not accelerated (Apple Software Renderer) |
| GLFW's request (3.2+ core, RGBA8, depth 24, double buffer, **accelerated required**) | no pixel format ("invalid pixel format") |
| The same request without the accelerated requirement | context created: Apple Software Renderer, OpenGL 4.1 APPLE-23.1.1 |

GLFW inside raylib 6.0 always adds `NSOpenGLPFAAccelerated` (`src/external/glfw/src/nsgl_context.m`, line 207). The
runner's virtual machine has no accelerated OpenGL renderer, only Apple's software renderer. The failure is therefore
**a runner limitation met by a fixed GLFW requirement**: not raylib's context configuration (its 3.3 core request is
satisfiable) and not an incompatibility of the backend with macOS. GLFW was not patched. A Mac with a GPU is expected
to open the window; that is not verified.

**Software rendering experiment.** raylib 6.0 also has a windowless platform, `PLATFORM_MEMORY`, with the rlsw
software renderer (`platforms/software/build-raylib.sh`). In a Linux container with the four games' desktop builds it
played all 15 scripts and wrote all 138 captures, but every capture is blank: `rlReadTexturePixels` (`rlgl.h`) has
branches for OpenGL 1.1/3.3 and ES2 and none for the software renderer, so reading back the render texture
MicroEngine draws into returns an unfilled buffer. It cannot give visual evidence without changing raylib or the
capture path.

| On macOS | Automatic now | Needs |
| --- | --- | --- |
| Build, headless tests (281), native audio-device test | yes: pipeline (engine changes, not blocking) and weekly | — |
| Why no window can be opened | yes: OpenGL probe, weekly | — |
| Native rendering, captures | attempted automatically only when the probe finds an accelerated renderer | a Mac with a GPU (self-hosted runner, or a hosted image with one) |
| Keyboard, gamepad, audio output, fullscreen, high DPI | no | real hardware and a person |

## 5. Android

**Route**: .NET for Android (`net10.0-android`, Mono) with raylib 6.0's own Android platform built with the NDK (r27d,
API 24). raylib's `rcore_android.c` provides `android_main` for a `NativeActivity`; a small C `main()`
(`platforms/android/native/microengine_main.c`) calls the activity's exported `runGame()` through JNI, and from there
the unchanged `RaylibPlatform` runs the game on the native-activity thread. The game comes from `web/games.props`.

| Level | Result | Evidence |
| --- | --- | --- |
| Toolchain configured | yes | NDK 27.3.13750724, android workload 36.1, Java 21 (local and CI) |
| Project compiled | yes | `platforms/android` |
| Package generated | yes | APK with x86_64 and arm64-v8a (arm64 never run) |
| Application installed | yes | emulator API 36.1 (local), API 34 (CI) |
| Application started | yes | `microengine: start MeadowRun ... on android-x64` |
| Rendering checked | yes | 13/13 captures within the Linux reference's tolerance (local and CI); 11 byte-identical to the Windows build |
| Input checked | system keys only | `adb shell input keyevent`: TitleScene → MapScene → LevelCardScene → PlayScene; no touch controls exist |
| Audio checked | device opens | raylib's audio device opened in every run; output not listened to |
| Real game run | yes | Meadow Run: four scripted runs, an interactive run, Home and relaunch |

Findings and limits:

- **raylib defect: audio teardown race.** `CloseAudioDevice` (`raudio.c`) destroys the mixer mutex before
  `ma_device_uninit` stops the device, so an audio callback in flight can lock a destroyed mutex. Android's FORTIFY
  aborts (`pthread_mutex_lock called on a destroyed mutex`, in `ma_device_handle_backend_data_callback`); on desktop
  it is a latent race. Seen at the end of 3 of about 37 scripted runs (CI runs 4 and 6, one local). The fix is
  swapping two lines in raylib; not applied here.
- **raylib and the JVM thread.** `GetCurrentMonitor` and `OpenURL` attach and then detach the calling thread from the
  JVM, which aborts a thread running .NET code. `RaylibPlatform` no longer calls `GetCurrentMonitor` on Android (the
  display's vsync paces frames); MicroEngine does not use `OpenURL`.
- **Bindings.** Raylib-cs 8.1.0 works unchanged; its NuGet native files must be excluded, because `android-x64` falls
  back to `linux-x64` and the glibc library would replace the NDK one.
- **Lifecycle.** Home and relaunch keep the process alive; raylib pauses the loop while the window is gone; rotation
  is handled by the activity. Pausing audio and saving on `onPause` are not implemented.
- **Assets.** Not exercised: Meadow Run is procedural and the host refuses games with content files. raylib's `fopen`
  wrapper reads APK assets, but MicroEngine reads content through .NET file APIs, so content would need extracting or
  reading through the asset manager.
- **Persistence.** Saves go to the app's private files folder through `XDG_DATA_HOME`; a save written and read back
  was not checked separately.
- **Touch and gamepad.** Not supported: raylib reports touch, but MicroEngine's input map has no touch bindings;
  gamepads were not tried.

**Not claimed**: Android support. This is a prototype that runs a real game on an emulator.

## 6. iOS

**Toolchain**: .NET 10 `ios` workload from workload set 10.0.401 (ios 26.5.10318), which requires Xcode 26.6; the
newest set (10.0.403, ios 27.1) requires Xcode 27.1, not on the hosted image yet. Full AOT for the simulator
(`iossimulator-arm64`); Raylib-cs and the engine compile and link unchanged.

**Blocker**: raylib 6.0 has **no iOS platform**. `src/platforms` holds Android, desktop (GLFW, SDL, RGFW, Win32), DRM,
web and memory backends, and no code path targets iOS: Android support in raylib says nothing about iOS. The iOS host
therefore runs the game on `HeadlessPlatform`.

| Level | Result | Evidence |
| --- | --- | --- |
| Toolchain configured | yes | run 41213988975 |
| Project compiled | yes | AOT, with IL2104 allowed |
| Package generated | yes | `MicroEngine.iOS.app` |
| Application installed | yes | iPhone 17 Pro simulator |
| Application started | yes | `microengine: start MeadowRun ... on iossimulator-arm64` |
| Rendering checked | no | nothing is drawn |
| Input and audio checked | no | headless |
| Real game run | game code only | 4/4 scripted runs identical to the desktop build: 570, 760, 300 and 710 updates with 4, 6, 1 and 2 captures |

Saves go to `Library/Application Support` (the sandbox's home folder is not writable). raylib interop under AOT was
not exercised at run time: no raylib library exists for iOS.

## 7. Architecture

- The same gameplay code ran on Windows, Linux, the browser, Android and iOS. Platform code stays in hosts
  (`platforms/android`, `platforms/ios`: about 200 lines of C# and 46 of C) plus one guarded line in `RaylibPlatform`.
  No game was copied: the hosts read `web/games.props`.
- `MobileRun` is shared host code, not a framework: one run with `--script` semantics and log lines for tests.

### raylib's structural limits found

1. No iOS platform in raylib 6.0.
2. GLFW on macOS requires an accelerated OpenGL renderer; GLFW has no software fallback.
3. The software renderer cannot read back textures (`rlReadTexturePixels`), so `PLATFORM_MEMORY` cannot capture.
4. Audio teardown race in `CloseAudioDevice`.
5. On Android raylib owns the activity and its thread (`android_main` calls `main`) and detaches threads from the JVM
   in `GetCurrentMonitor` and `OpenURL`: the host must call raylib from that one thread.
6. Global state: one window and one audio device per process; fine for games, limiting for tools and parallel tests.

## 8. Recommendations for the backend milestone

1. **iOS needs another platform layer.** To evaluate: raylib's SDL backend (`rcore_desktop_sdl.c`) with SDL on iOS and
   OpenGL ES (deprecated by Apple, still available), or a MicroEngine backend behind `IPlatform` on SDL3 or Metal. The
   headless iOS host already proves the managed side; only the device layer is missing.
2. **Keep raylib for desktop, browser and Android** for now: all three produce the same captures within tolerance.
3. **Decide whether to carry raylib patches.** MicroEngine already builds raylib itself for the web and Android; a
   small reviewed patch set (the audio teardown order first) would remove the Android abort. An owner decision.
4. **macOS native evidence needs a GPU**: a self-hosted Mac runner, or manual validation, before macOS is supported.
5. **Android next**: touch bindings in the input map, pause and save on `onPause`, content files from the APK, arm64
   on a real device, then a second game.
