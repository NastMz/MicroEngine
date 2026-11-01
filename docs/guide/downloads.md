# Downloads and installation

The four demo games can be played in the browser on their [demo pages](/demos/) and downloaded to run on your own
machine. Downloads are the files of a published [GitHub release](https://github.com/NastMz/MicroEngine/releases): direct
downloads, not store listings, so no Steam, Google Play or App Store account is involved. Each demo page lists exactly
the packages of the current release; a system without a package there has no download yet.

The demos are free to play but not open source: they are all rights reserved ([licensing](licensing.md)). MicroEngine
itself is MIT licensed.

## What you download

| System | File | Requirements |
| --- | --- | --- |
| Windows | `<Game>-<version>-windows-x64.zip` | Windows 10 or 11, 64-bit, a GPU with Direct3D 11, OpenGL or Vulkan |
| Linux | `<Game>-<version>-linux-x64.tar.gz` | 64-bit x86, glibc 2.34 or later (for example Ubuntu 22.04, Debian 12, Fedora 35 or newer), X11 or Wayland with OpenGL, PulseAudio or PipeWire |
| macOS | `<Game>-<version>-macos-arm64.zip` | macOS 14 or later on Apple silicon (M1 or newer); Intel Macs are not packaged |
| Android | `<Game>-<version>-android.apk` | Android 7.0 (API 24) or later, 64-bit ARM or x86 |

Every package contains the game and everything it needs: the .NET runtime is inside it, so nothing else has to be
installed. It also contains the demo's licence notice, MicroEngine's MIT licence and the third-party notices, and a
`build-info.json` naming the game version, the engine version and the exact build. Each release lists the SHA-256 of
every file, which you can compare with your download.

## Windows

1. Extract the ZIP anywhere (right-click → Extract All).
2. Run `<Game>.exe` in the extracted folder.

The program is not signed with a code-signing certificate, so Windows SmartScreen may say the publisher is unknown:
choose **More info**, then **Run anyway**. Saves go to `%LOCALAPPDATA%\MicroEngine\<Game>`.

## Linux

```bash
tar -xzf NeonRift-1.0.0-linux-x64.tar.gz
./NeonRift/NeonRift
```

The package was built once and checked on Ubuntu 22.04, Debian 12 and Fedora 41 by playing every scripted scenario in a
container; a desktop installation normally already has what it needs: OpenGL from Mesa or your GPU driver, an X11 or
Wayland session, and PulseAudio or PipeWire for sound. On a distribution with an older C library (glibc before 2.34,
such as Debian 11) the game stops with `error: platform 'sdl' could not load its native library` and names the missing
glibc version. Without a working GPU driver, `--platform sdl-software` draws on the CPU. Saves go to
`~/.local/share/MicroEngine/<Game>` (or `$XDG_DATA_HOME`).

## macOS

1. Extract the ZIP (double-click) and move `<Title>.app` wherever you like, for example to Applications.
2. Open it. The first time, macOS refuses to open it: the app is signed ad hoc but not notarized by Apple, which needs a
   paid Apple Developer membership the project does not have.
3. Open **System Settings → Privacy & Security**, scroll to the message about the app, choose **Open Anyway** and
   confirm with your password. From then on it opens normally.

Since macOS 15, Control-click → Open no longer bypasses this check, and the button in Settings appears only after a
blocked attempt. If you prefer the Terminal, `xattr -dr com.apple.quarantine "<Title>.app"` removes the download mark
instead. Saves go to `~/Library/Application Support/MicroEngine/<Game>`.

## Android

1. Download the APK on the phone or tablet.
2. Open it. Android asks once to allow installing apps from the source you used (your browser or file manager): allow
   it, then install. Android itself allows this; some devices managed by an organisation do not.
3. Start the game from the launcher. On-screen controls appear over the game (d-pad, A, B, X, a shoulder button,
   Start); a keyboard or gamepad also works.

The APK is signed with MicroEngine's demo key, so a newer demo installs over an older one and keeps its saves.

Since 30 September 2026, certified Android devices in Brazil, Indonesia, Singapore and Thailand install apps only from
developers registered with Google, and Google plans to extend this worldwide in 2027. The demos' publisher is not
registered: in those countries the APK installs through Android's advanced installation flow for unverified developers,
or with `adb install` from a computer. Elsewhere, nothing changes yet.

The Android app has been played on one phone (Redmi Note 14 Pro) and is checked automatically on the Android emulator
([Platforms](/platforms/)).

## iOS

There is no iOS download, and this is a limit of distribution, not of the engine: the games run on iOS in Apple's
simulator (they draw, open audio and survive the background), but no iPhone has run them.

Software on an iPhone must be signed by Apple's chain. The ways to reach a device are the App Store or TestFlight,
ad-hoc builds limited to registered devices, and in the EU alternative marketplaces or web distribution of
notarized apps; every one of them needs a paid Apple Developer Program membership, and none is a file a visitor could
simply download. A simulator build cannot be installed on a device at all. A developer can build the iOS host from the
repository with Xcode and run it on their own iPhone with their own Apple ID ([Platforms](platforms.md#phones-and-tablets)).

## In the browser

Nothing to install: the demo pages load each game when you press Play. Chrome and Firefox on a desktop are verified;
the browser builds need a keyboard or a gamepad.

## Running options

The desktop builds accept the engine's command line ([Configuration](../reference/configuration.md)): `--platform raylib`
runs the game on raylib instead of SDL 3 (on Linux it needs glibc 2.38), `--platform sdl-software` uses SDL's CPU
renderer, `--list-platforms` lists what can run on the machine. F11 or Alt+Enter toggles fullscreen.
