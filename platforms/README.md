# Experimental platform hosts

Hosts and probes from PLATFORM-2 ([evaluation](../docs/evaluations/platform-2.md)). **None of these platforms is
supported.** They exist to gather evidence, and they run in the weekly
[platform workflow](../docs/deployment.md#platform-workflow), never in the publishing pipeline.

Like the browser host, each mobile host runs one game chosen with `-p:Game=<Id>` from
[`web/games.props`](../web/games.props) (default `MeadowRun`): no game is copied and no gameplay code changes. A game
with files in its `Content` folder is refused at build time: the mobile hosts do not ship content files yet.
These projects are not in `MicroEngine.slnx`, because they need the `android` or `ios` workload.

| Folder | What it is |
| --- | --- |
| [`Shared/`](Shared/) | `MobileRun`: one run of the game with `DesktopLauncher --script` semantics (fixed step, seed 1, captures), logging `microengine:` lines |
| [`android/`](android/) | .NET for Android host: a `NativeActivity` whose native library is raylib built with the NDK |
| [`ios/`](ios/) | .NET for iOS host: the game on the headless platform in the simulator (raylib has no iOS platform) |
| [`macos/`](macos/) | `gl-probe.m`: why GLFW cannot open an OpenGL window on a Mac without a GPU |
| [`software/`](software/) | the raylib software-renderer experiment (`PLATFORM_MEMORY` + rlsw), kept to reproduce its result |

## Evidence levels

A build is not compatibility. Each level is claimed only when it was run and its output read.

| Level | Android (x86_64 emulator) | iOS (simulator) |
| --- | --- | --- |
| Toolchain configured | NDK r27d, .NET 10 android workload | .NET 10 ios workload with the Xcode it names |
| Project compiled | yes | yes |
| Package generated | APK (x86_64, arm64-v8a) | `.app` for `iossimulator-arm64` |
| App installed | yes | see the evaluation |
| App started | yes | see the evaluation |
| Rendering checked | 13 scripted captures of Meadow Run within the Linux reference's tolerance, 11 byte-identical to the Windows build | no: nothing is drawn |
| Input checked | system key events (`adb shell input keyevent`) drive the menus; no touch controls | no |
| Audio checked | raylib's audio device opens; output not listened to | no |
| Real game run | Meadow Run, four scripted runs and an interactive one; background and resume | Meadow Run's code, headless, compared with the desktop build |

## Android

Needs the .NET 10 `android` workload, an Android SDK (`ANDROID_HOME`), the NDK pinned in
[`android/toolchain.env`](android/toolchain.env) (`sdkmanager "ndk;27.3.13750724"`) and Java 17 or later.

```bash
platforms/android/build-raylib.sh                    # libraylib.so for x86_64 and arm64-v8a, into android/native/out
dotnet build platforms/android -c Release -p:AndroidSdkDirectory="$ANDROID_HOME"
node platforms/android/device-checks.mjs --apk platforms/android/bin/MeadowRun/Release/net10.0-android/dev.microengine.meadowrun-Signed.apk --out dist/android-checks
node web/tests/desktop.mjs --skip-run --games MeadowRun --out dist/android-checks --reference web/tests/reference/linux-x64.json
```

`device-checks.mjs` needs a running emulator or device and prints one line per evidence level. How it works:

- raylib's Android platform (`rcore_android.c`) owns the activity: its `android_main` creates the window and EGL
  context, then calls `main()`. [`native/microengine_main.c`](android/native/microengine_main.c) is that `main()`: it
  calls `GameActivity.runGame()` through JNI, so the .NET game loop runs on the native-activity thread, which is where
  raylib must be called.
- `GameActivity` runs the game on `RaylibPlatform`, as on desktop. Launch extras select a script (`--es script
  smoke.script`) and environment variables (`--es env 'MEADOW_START=level;MEADOW_FORM=bloom'`). Saves go to the
  app's private files folder (`XDG_DATA_HOME`).
- Native libraries are extracted (`extractNativeLibs`), so .NET's P/Invoke and `NativeActivity` load the same
  `libraylib.so` file and share the one raylib instance.
- The `Raylib-cs` package's native files are excluded: `android-x64` falls back to `linux-x64` in the runtime graph,
  and the package's glibc `libraylib.so` would otherwise replace the NDK build (`dlopen failed: library "libc.so.6"
  not found`).

## iOS

Needs macOS, the .NET 10 `ios` workload and the Xcode version that workload requires (the workflow pins the SDK and
workload set from `web/toolchain.env` and selects the matching Xcode).

```bash
dotnet build games/MeadowRun -c Release
dotnet build platforms/ios -c Release
platforms/ios/simulator-checks.sh platforms/ios/bin/MeadowRun/Release/net10.0-ios/iossimulator-arm64/MicroEngine.iOS.app MeadowRun
```

The app reads `--script` and `--env` after its own arguments (`xcrun simctl launch --console-pty <device>
dev.microengine.meadowrun --script smoke.script --env ''`), runs the game on `HeadlessPlatform`, prints `microengine:`
lines and exits after a scripted run. The check compares updates, frames and captures with the desktop build's
`--headless` run of the same script.
