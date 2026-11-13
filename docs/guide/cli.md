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
microengine shaders [--project P] [--source DIR] [--out DIR] [--check]
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
| `web` | a static site (folder and ZIP) for any web server | any system | `wasm-tools` workload (`wasm-tools-net10` with .NET SDK 11) |

[Packaging](packaging.md) describes each package, its requirements and limits; [Signing](signing.md) the keys.

| Option | Meaning |
| --- | --- |
| `--out DIR` | where packages go (default `dist` next to the project) |
| `--debug-key` | `android`: sign with this machine's debug key, for testing on your own devices only |
| `--version-code N` | `android`, `ios`: the build number stores compare (default: minutes from 2026-01-01 to the commit, or to now outside Git, so later builds install over earlier ones) |
| `--dev` | `web`: a development build that also ships input scripts and accepts URL parameters |
| `--sdl-wasm FILE` | `web`: the SDL library to link (default: the one inside the `MicroEngine.Sdl` package) |
| `--key LABEL` | a label recorded in `build-info.json`, for your own release tooling (default `local`) |

`MICROENGINE_DOTNET` names another `dotnet` to build with (for example a private SDK with the `wasm-tools`
workload); by default the one on `PATH` is used.

## info and doctor

`microengine info` prints the identity the project declares and whether it is valid. `microengine doctor` checks
this machine for every target (or one, `microengine doctor android`): the .NET SDK, workloads, Android SDK, Java,
Xcode, and whether signing variables are set (never their values). For anything missing it prints how to install it.
It changes nothing.

## shaders

`microengine shaders` compiles custom shaders ahead of time, for
devices that declare custom shaders (the programmable SDL_GPU device; see
[the graphics device](../manual/rendering.md#the-graphics-device)). Each `name.vert.hlsl` or `name.frag.hlsl` in the
game's `Shaders` folder (or `--source`) becomes `name.vert.shader` in `Content/shaders` (or `--out`), holding DXIL for
Direct3D 12, SPIR-V for Vulkan, MSL for Metal and the resource counts the device needs. A game loads the file with
`CompiledShader.Parse` and creates it with `CompiledShader.Create(device)`, which picks the format the device runs;
nothing compiles while the game runs, and a device without custom shaders refuses with `GraphicsCapabilityException`.

Write one HLSL file per stage with the entry point `main` (shader model 6.0) and the SDL_GPU binding rules: vertex
inputs use `TEXCOORD0`, `TEXCOORD1`... for their locations; vertex-stage uniforms are `register(b0, space1)`;
fragment-stage textures and samplers are `register(t0, space2)` and `register(s0, space2)`, uniforms
`register(b0, space3)`. Matrices from `System.Numerics` are uploaded as they are and applied with `mul(M, v)`.

`--check` writes nothing and fails when a `.shader` file is missing or differs from what its HLSL compiles to, so a CI
job can catch a shader edited without recompiling.

Compiling needs Windows: the HLSL compiler (DXC) is Microsoft's `Microsoft.Direct3D.DXC` package, under Microsoft's own
licence terms, and it has Windows binaries only. The tool does not ship it: on first use it restores that exact version
into your NuGet packages folder (`MICROENGINE_DXC` names another `dxc.exe`). Elsewhere the command stops with exit code 3
and says so; commit the `.shader` files, which load on every platform. MSL is generated but has not been run on a Mac.

## Errors

Every failure is one `error:` message that says what is wrong and what to do, with exit code 1 (2 for a wrong command
line). When a build fails, the message keeps the build's own error lines; `--verbose` shows the whole output.
