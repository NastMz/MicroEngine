# The `microengine` tool

`microengine` creates, runs, builds and packages MicroEngine games. It is a .NET tool (`MicroEngine.Cli`) that drives
the .NET SDK and each system's own tools; it needs no MicroEngine account or service, and works the same on your
machine and in any CI. [Getting MicroEngine](getting-microengine.md) shows how to install it.

```text
microengine new <Name> [--id ID] [--title TITLE] [--output DIR]
microengine run [--project P] [-c Debug|Release] [--platform NAME] [-- game options]
microengine build [--project P] [-c Release|Debug]
microengine package <target> [--project P] [--out DIR] [options]
microengine info [--project P]
microengine doctor [target]
microengine version
```

Every command works on the `.csproj` in the current folder, or on `--project` (a `.csproj` or its folder).
`--verbose` prints each command the tool runs and its full output; `microengine <command> --help` describes one.

## new

Creates a game from the `MicroEngine.Templates` package, which it installs at its own version when `dotnet new`
does not have it yet. The game is a complete project: `MainGame.cs` loads a sprite from `Content/` and a sound made
in code, `Scenes/PlayScene.cs` moves the hero with input actions (keyboard, gamepad, on-screen touch controls) and
draws text, `icon.png` is its icon, and the project file holds its [identity](app-identity.md).

```bash
microengine new StarCourier --id com.yourstudio.starcourier --title "Star Courier"
```

The same template works without the tool: `dotnet new install MicroEngine.Templates`, then
`dotnet new microengine-game -n StarCourier --app-id com.yourstudio.starcourier`.

## run and build

`microengine run` is `dotnet run` (Debug by default); options after `--` go to the game, for example
`microengine run -- --list-platforms` or `microengine run --platform raylib -- --scale 4`. `microengine build`
compiles in Release and checks the identity (an invalid id or icon stops the build with an error naming it).

## package

Builds a file a player installs, into `dist/` next to the project (or `--out`), named
`<Game>-<version>-<target>.<ext>`; the last line printed is `package: <path>`.

| Target | Builds | Where it can be built | Needs |
| --- | --- | --- | --- |
| `windows` | ZIP: single-file executable (with the .NET runtime), SDL 3, raylib, content, notices | Windows | — |
| `linux` | tar.gz for x86_64, glibc 2.34+, with a `.desktop` entry and the icon | any system | — |
| `macos` | ZIP holding the `.app` for Apple silicon, signed ad hoc or with your Developer ID | macOS | Xcode command-line tools |
| `android` | APK for arm64-v8a and x86_64, signed with your key | any system | `android` workload, Android SDK, Java 17+ |
| `ios` | `.ipa` for iPhone and iPad, signed for your devices or the App Store | macOS | Xcode, `ios` workload, Apple Developer account |
| `ios-simulator` | ZIP holding the `.app` for Xcode's Simulator; **not installable on a device** | macOS | Xcode, `ios` workload |
| `web` | a static site (folder and ZIP) for any web server | any system | `wasm-tools` workload |

[Packaging](packaging.md) describes each package, its requirements and limits; [Signing](signing.md) the keys.

| Option | Meaning |
| --- | --- |
| `--out DIR` | where packages go (default `dist` next to the project) |
| `--debug-key` | `android`: sign with this machine's debug key, for testing on your own devices only |
| `--version-code N` | `android`, `ios`: the build number stores compare (default: minutes from 2026-01-01 to the commit, or to now outside Git, so later builds install over earlier ones) |
| `--dev` | `web`: a development build that also ships input scripts and accepts URL parameters |
| `--raylib-wasm FILE` | `web`: the raylib library to link (default: the one inside the `MicroEngine.Raylib` package) |
| `--key LABEL` | a label recorded in `build-info.json` (MicroEngine's own release pipeline records content keys) |

`MICROENGINE_DOTNET` names another `dotnet` to build with (for example a private SDK with the `wasm-tools`
workload); by default the one on `PATH` is used.

## info and doctor

`microengine info` prints the identity the project declares and whether it is valid. `microengine doctor` checks
this machine for every target (or one, `microengine doctor android`): the .NET SDK, workloads, Android SDK, Java,
Xcode, and whether signing variables are set (never their values). For anything missing it prints how to install it.
It changes nothing.

## Errors

Every failure is one `error:` message that says what is wrong and what to do, with exit code 1 (2 for a wrong command
line). When a build fails, the message keeps the build's own error lines; `--verbose` shows the whole output.
