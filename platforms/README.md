# Platform hosts

Phone hosts and probes. They run in the [platform workflow](../docs/ci.md#platform-workflow) (monthly, manual, or from
full validation), never in daily CI or a release. Decisions: [ADR 0016](../docs/decisions/0016-platform-profiles.md),
[ADR 0017](../docs/decisions/0017-sdl3-native-platform.md); evidence: [PLATFORM-3](../docs/evaluations/platform-3.md).

The phone hosts themselves are part of the SDK: `microengine package android` and `ios-simulator` (or `ios`) build
them for any game ([Packaging](../docs/guide/packaging.md)), from the templates in
[`src/MicroEngine.Cli/Hosts`](../src/MicroEngine.Cli/Hosts); this folder holds the checks that play the demos' builds
and the probes of earlier experiments. No game is copied and no gameplay code changes. Both run the game on the same SDL platform as
the desktop (`src/MicroEngine.Sdl`), with on-screen touch controls, suspension in the background, and the game's
`Content` files. The hosts are not in `MicroEngine.slnx`, because they need the `android` or `ios` workload.

| Folder | What it is |
| --- | --- |
| [`Hosts/mobile`](../src/MicroEngine.Cli/Hosts/mobile) | `MobileRun`: one run of the game with `DesktopLauncher --script` semantics (fixed step, seed 1, captures), logging `microengine:` lines (start, scenes, suspended/resumed, captures, done, exit) |
| [`Hosts/android`](../src/MicroEngine.Cli/Hosts/android) | .NET for Android host: an `SDLActivity` whose `Main` runs the game; content files as APK assets, unpacked once per install |
| [`Hosts/ios`](../src/MicroEngine.Cli/Hosts/ios) | .NET for iOS host: `SDL_RunApp` hands the app to SDL, which calls back into .NET to run the game (Metal) |
| [`android/`](android/) | `device-checks.mjs` (plays an APK on an emulator or device), `ci-play.sh`, pinned versions |
| [`ios/`](ios/) | `simulator-checks.sh` (plays a simulator app), pinned versions |
| [`macos/`](macos/) | `gl-probe.m`: why GLFW (raylib) cannot open an OpenGL window on a Mac without a GPU |
| [`software/`](software/) | the raylib software-renderer experiment of PLATFORM-2, kept to reproduce its result |

## Evidence levels

A build is not compatibility. Each level is claimed only when it was run and its output read.

| Level | Android (x86_64 emulator) | iOS (simulator) |
| --- | --- | --- |
| Toolchain configured | .NET 10 android workload, Java 21; no NDK | .NET 10 ios workload (set 10.0.401) and the Xcode it requires |
| Project compiled | yes | yes |
| Package generated | APK (x86_64, arm64-v8a) with SDL 3.5 | `.app` for `iossimulator-arm64` with `SDL3.framework` |
| App installed, started | yes (emulator API 36.1 locally, API 34 in CI) | yes (iPhone 17 Pro simulator, CI) |
| Rendering checked | Meadow Run 13/13 and Neon Rift 12/12 captures pixel-identical to the Windows SDL reference (locally), within it in CI | Meadow Run 13/13 and Neon Rift 12/12 captures within the SDL reference |
| Input checked | system keys and taps on the on-screen controls move each game from its title into play; a held system drag on the movement control moves the player | keys not checked |
| Touch checked | taps and drags at the positions the game logs (`MICROENGINE_PROBE=1`); two thumbs scripted (`touch.script`), since `adb input` has one pointer | scripted touches on the simulator's layout (`touch.script`); the simulator cannot be sent system touches by script |
| Audio checked | SDL's audio device opens; output not listened to | SDL's audio device opens; output not listened to |
| Lifecycle | Home suspends the game, relaunching resumes it | opening Settings suspends the game, relaunching resumes it |
| Content files | Neon Rift's PNG, WAV and OGG files load from the APK | Neon Rift's files load from the app bundle |
| Real device | the four APKs played by hand on a Redmi Note 14 Pro (arm64, Android 16), touch and a Bluetooth PlayStation controller | not tried |

## Android

Needs the .NET 10 `android` workload, an Android SDK (`ANDROID_HOME`) and Java 17 or later. SDL for every ABI comes
from the `ppy.SDL3-CS` package.

```bash
dotnet run --project src/MicroEngine.Cli -c Release -- package android --project games/NeonRift/NeonRift.csproj --debug-key --out dist/android
node platforms/android/device-checks.mjs --apk dist/android/NeonRift-1.0.0-android.apk --game NeonRift --out dist/android-checks
node web/tests/desktop.mjs --skip-run --games NeonRift --out dist/android-checks --reference web/tests/reference/sdl-android-x86_64.json
```

`device-checks.mjs` needs a running emulator or device and prints one line per level (installed, started, rendered,
audio, input, lifecycle, touch, touch-drag, touch-two-thumbs). How it works:

- `GameActivity` is SDL's `SDLActivity`; it loads only `libSDL3.so` and overrides `Main`, which SDL calls on its own
  thread once the surface exists. The game runs there on `SdlPlatform`.
- Launch extras select a script (`--es script smoke.script`) and environment variables (`--es env 'MEADOW_START=level'`),
  read by .NET only: .NET's environment never reaches the native one, so SDL hints are set from code
  (`MICROENGINE_SDL_EVENT_LOGGING=1` turns on SDL's event log).
- Saves go to the app's private files folder (`XDG_DATA_HOME`); content files are copied from the APK to
  `files/content` when the install changes.
- The `Raylib-cs` package's native files are excluded: the games reference raylib through the desktop launcher, and
  `android-x64` would pick up its glibc build.

## iOS

Needs macOS, the .NET 10 `ios` workload and the Xcode version that workload requires (the workflow pins the SDK and
workload set from `web/toolchain.env` and selects the matching Xcode).

```bash
dotnet run --project src/MicroEngine.Cli -c Release -- package ios-simulator --project games/NeonRift/NeonRift.csproj --out dist/ios
ditto -x -k dist/ios/NeonRift-1.0.0-ios-simulator.zip dist/ios/extracted
platforms/ios/simulator-checks.sh "dist/ios/extracted/NeonRift/NeonRift.app" NeonRift dist/ios-checks/NeonRift
node web/tests/desktop.mjs --skip-run --games NeonRift --out dist/ios-checks/NeonRift --reference web/tests/reference/sdl-ios-simulator-arm64.json
```

The app reads `--script` and `--env` after its own arguments (`xcrun simctl launch --console-pty <device>
dev.microengine.neonrift --script smoke.script --env ''`), prints `microengine:` lines, writes captures to
`Library/Caches/captures/<script>` and exits after a scripted run. The check copies the captures out of the app's
container and sends the app to the background by opening Settings.
