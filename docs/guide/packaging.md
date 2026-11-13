# Packaging a game

`microengine package <target>` turns a game project into the file a player installs. Every package takes the game's
[identity](app-identity.md) from its project file, carries its `Content` files, and holds a `build-info.json` that
says which game, version, SDK and commit it was built from. The four demos are packaged by these same commands.

```bash
microengine package windows    # dist/StarCourier-1.2.0-windows-x64.zip
microengine package linux      # dist/StarCourier-1.2.0-linux-x64.tar.gz
microengine package android    # dist/StarCourier-1.2.0-android.apk
microengine package web        # dist/StarCourier-1.2.0-web/ and .zip
```

## Desktop: Windows, Linux, macOS

A self-contained .NET single-file executable: players need no .NET. Next to it: SDL 3 (the default platform) and
raylib (`--platform raylib`), the `Content` folder, `README.txt` (how to run it), `build-info.json`, the game's
`LICENSE.txt` when it has one, `THIRD-PARTY-NOTICES.txt` and `licenses/` (MicroEngine's MIT licence, the .NET
runtime's licence and notices, and SDL's and raylib's licence files). Everything is in one folder named after the
game. Builds use invariant globalization, so no ICU library is needed on the player's system, and carry no debug
information or build-machine path. Android and iOS packages carry SDL's licence file with their other notices
(`assets/licenses/` in an APK, `licenses/` in the app).

| | Windows | Linux | macOS |
| --- | --- | --- | --- |
| Architecture | x64 | x86_64 | Apple silicon (arm64) |
| Built on | Windows (only there can .NET write the executable's icon and version) | any system | a Mac (`codesign`) |
| Identity | executable icon, product name and description | `<id>.desktop` and `icon.png`; window icon and app id at run time | `Info.plist`, `AppIcon.icns`, bundle id |
| Player needs | Windows 10/11 64-bit, a GPU with Direct3D 11, OpenGL or Vulkan | glibc 2.34+ (Ubuntu 22.04, Debian 12, Fedora 35 or newer), X11 or Wayland with OpenGL, PulseAudio or PipeWire; raylib needs glibc 2.38 | macOS 14+ |
| First launch | SmartScreen may warn about an unsigned program | — | an app that is not notarized needs Open Anyway in Privacy & Security ([Signing](signing.md)) |

The Linux package is portable (extract and run); its `.desktop` file is ready to copy to
`~/.local/share/applications` once its `Exec=` and `Icon=` lines point to where the game was extracted. A system
older than glibc 2.34 gets a clear error naming the missing version, not a crash.

## Android

An APK for arm64-v8a (phones) and x86_64 (emulators, Chromebooks), Android 7.0+ (`MicroEngineAndroidMinApi`), built
with .NET for Android: install the `android` workload (`dotnet workload install android`), an Android SDK
(`ANDROID_HOME`) and Java 17+. The game runs on SDL 3 with the engine's on-screen touch controls; keyboards and
gamepads work too. The launcher shows `ApplicationTitle` and the icon; the package name is `ApplicationId`;
`versionName` is `Version` and `versionCode` grows with every commit (or `--version-code`). Content files travel as
APK assets and are unpacked once per installed version. Signing is your key ([Signing](signing.md)); `--debug-key`
builds a test APK.

Google Play takes Android App Bundles (`.aab`), which the tool does not build yet; the APK installs directly
(side-loading) and through stores that accept APKs. Android's developer verification rules apply to who may install it.

## iOS

`ios-simulator` builds a `.app` for Xcode's Simulator on Apple silicon: useful to try the game, and **never
installable on an iPhone**. `ios` builds an `.ipa` for devices, signed with your Apple certificate and provisioning
profile; without them the tool stops and says what is missing. Both need a Mac with Xcode and the `ios` workload.
The app icon is made from `MicroEngineIcon` (enlarged to 1024 pixels without blur); supported orientations follow
`MicroEngineOrientation`. Distribution through the App Store or TestFlight is done with Apple's own tools from the
`.ipa` and requires an Apple Developer Program membership.

## Web

A static site: `index.html` titled with the game's name, its icon as favicon, the WebAssembly runtime, the game and
its `Content` files, `build-info.json` and the licence files of everything inside (the .NET runtime, Emscripten,
SDL). Upload the folder to any static host (GitHub Pages, Netlify, an S3 bucket, nginx); files are also
pre-compressed (`.br`, `.gz`) for servers that serve them. The game runs on SDL 3 compiled to WebAssembly, in the
same page in desktop and mobile browsers, with touch controls.

It needs the `wasm-tools` workload (`dotnet workload install wasm-tools`); with a newer .NET SDK than the game's
.NET 10, the workload for .NET 10 instead (`dotnet workload install wasm-tools-net10` with .NET SDK 11), because
`wasm-tools` serves only the SDK's own .NET. `microengine doctor web` names the one your SDK needs. SDL for the browser comes inside the
`MicroEngine.Sdl` package, built with the same emscripten as that workload, so any game that references
`MicroEngine.Sdl` or `MicroEngine.Desktop` can be packaged for the web (`--sdl-wasm FILE` names another SDL library).
