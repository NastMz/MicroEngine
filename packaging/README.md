# Native demo packages

The four demos as files a player downloads and runs without .NET or the repository: a portable ZIP for Windows, a
portable archive for Linux, an application for macOS and an APK for Android. Players' instructions are in
[Downloads](../docs/guide/downloads.md); the decision is [ADR 0018](../docs/decisions/0018-native-demo-distribution.md).
iOS has no download: see [Why there is no iOS download](../docs/guide/downloads.md#ios).

| File | What it does |
| --- | --- |
| [`targets.json`](targets.json) | The downloadable targets: runtime, runner, archive format, the SDL reference its captures are compared with, requirements and install steps. Every script, the store, the release and the site read it |
| [`targets.mjs`](targets.mjs) | Rules shared by everything that touches a package: file names, the layout, `build-info.json`, Android's version code |
| [`package.mjs`](package.mjs) | Builds one package: `node packaging/package.mjs <Game> --target <id>` |
| [`verify.mjs`](verify.mjs) | Checks the archive itself, level by level, and runs it: `node packaging/verify.mjs <file> --game <Game> --target <id>` |
| [`linux-distros.mjs`](linux-distros.mjs) | Plays the Linux packages in containers of other distributions (Docker) |

Generating a package, checking that it runs and offering it for download are three separate steps. A package is stored
(`site/scripts/demos.mjs publish-native`) only after `verify.mjs` played it on its own system; a release copies stored
packages; the site offers only what a published release holds.

## What a package contains

Desktop packages are self-contained .NET 10 single-file executables (no .NET installation needed), built with
`MicroEngineNativePackage=true`: no debug information, invariant globalization (no ICU library needed), their own
build folders. Everything sits in one top folder named after the game:

```text
NeonRift/
  NeonRift.exe | NeonRift          the game, with the .NET runtime inside
  SDL3.dll | libSDL3.so            SDL 3: the default platform
  raylib.dll | libraylib.so        raylib: the alternative (--platform raylib)
  Content/...                      the game's asset files (code-first games have none)
  README.txt                       requirements, how to run it, licence, build
  build-info.json                  game, versions, target, content key, commit, .NET version, signing
  LICENSE.txt                      the demo's notice: all rights reserved (games/LICENSE)
  THIRD-PARTY-NOTICES.txt          MicroEngine's third-party list (SDL, raylib, NVorbis, CC0 assets...)
  licenses/                        MicroEngine's MIT licence, the .NET runtime's licence and notices
```

On macOS the executable and both libraries are in `Neon Rift.app/Contents/MacOS`, the content files in
`Contents/Resources/Content` (the desktop launcher finds them there), and the bundle is signed ad hoc. The APK holds
SDL for `arm64-v8a` and `x86_64`, the content files and input scripts as assets, and the same notices under
`assets/licenses/`.

Release assets are named `<Game>-<game version>-<target>.<zip|tar.gz|apk>`, for example
`NeonRift-1.0.0-windows-x64.zip`. Versions come from one place each: the SDK version (`MicroEngineVersion`) in
`Directory.Build.props`, each demo's `<Version>` in its project file.

## What a package check proves

`verify.mjs` works on the downloaded file, never on a build folder:

| Level | Desktop | Android |
| --- | --- | --- |
| integrity | size and SHA-256 of the archive | the same for the APK |
| structure | every required file, nothing else, nothing outside the top folder, every content file of the game | both ABIs, content files, notices |
| no build-machine paths | no user profile or checkout path in any file | — |
| build-info | game, target, content key, game and engine versions match the repository | the same, plus the signing key kind |
| dependencies | Linux: the newest glibc each binary needs is within what `targets.json` promises; macOS: `codesign --verify --deep --strict`, and what Gatekeeper says | `apksigner verify`, version code and name, minimum SDK |
| platforms, report | the executable starts, lists SDL as usable, runs 120 updates on SDL and opens the audio device | — |
| device | — | `platforms/android/device-checks.mjs`: install, start, scripted runs, audio device, system keys, taps on the touch controls, background and back |
| rendered | every scripted scenario of the game played by the package's own executable, captures compared with the target's SDL reference | the same scenarios on the device, compared with the emulator reference |

Input is checked through the game's own scripts (the action layer every input device feeds) on every system; system
key events and touches reach a packaged game on Android; on desktops, system keyboard delivery is covered by the native
tests of the same backend code (Linux, `checks.yml`), not by the package check. Audio is checked as "the device
opened", not listened to.

## Linux distributions

The Linux package is built once, on Ubuntu, and must not be assumed to run everywhere. `linux-distros.mjs` installs
only what a desktop already has (an X server, Mesa, PulseAudio) in containers of other distributions, extracts the
archives and plays every scripted scenario:

```bash
node packaging/linux-distros.mjs --packages dist/packages --images ubuntu:22.04,debian:12,fedora:41,debian:11
```

SDL 3 needs glibc 2.34 (measured with `objdump -T`; `targets.json` promises it); raylib needs 2.38. On an older system
(Debian 11, glibc 2.31) the game stops with `error: platform 'sdl' could not load its native library`, naming the
missing glibc version; the check expects exactly that.

## Android signing

Release APKs are signed with one key so a later demo installs over an earlier one. The key never enters the
repository: CI reads it from four repository secrets, and without them it builds no Android package (the release then
offers no Android download).

| Secret | Value |
| --- | --- |
| `ANDROID_KEYSTORE_BASE64` | the keystore file, base64-encoded |
| `ANDROID_KEYSTORE_PASSWORD` | its password |
| `ANDROID_KEY_ALIAS` | the key's alias |
| `ANDROID_KEY_PASSWORD` | the key's password (may equal the store password) |

Create the key once, on your own machine, and keep a backup outside the repository: losing it means players must
uninstall the demo before installing a newer one.

```bash
keytool -genkeypair -keystore microengine-demos.keystore -alias microengine-demos -keyalg RSA -keysize 4096 -validity 10000 -dname "CN=MicroEngine demos"
base64 -w0 microengine-demos.keystore | gh secret set ANDROID_KEYSTORE_BASE64
gh secret set ANDROID_KEYSTORE_PASSWORD
gh secret set ANDROID_KEY_ALIAS --body microengine-demos
gh secret set ANDROID_KEY_PASSWORD
```

Locally, `--debug-key` signs with the machine's debug key; such an APK is never stored or released. The version code is
the number of minutes from 2026-01-01 to the build's commit, so newer packages install as updates.

## Local work

```bash
node packaging/package.mjs NeonRift --target windows-x64            # on Windows
node packaging/package.mjs NeonRift --target linux-x64              # on any system (cross-built)
node packaging/package.mjs NeonRift --target macos-arm64            # on a Mac (codesign)
ANDROID_HOME=... node packaging/package.mjs NeonRift --target android --debug-key
node packaging/verify.mjs dist/packages/NeonRift-windows-x64.zip --game NeonRift --target windows-x64
```

The scripts import `site/scripts/demos.mjs`, so run `npm ci --prefix site` once.
