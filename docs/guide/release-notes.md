# SDK release notes

One section per SDK version, newest first. The SDK is in Alpha: a version may change public APIs incompatibly, and
every such change is in the [migration notes](../migrations.md). Releases are tagged `sdk-v<version>`; the demo
releases (`v0.1.0`, `v0.2.0`...) are separate.

## 0.4.0-alpha.2

Published 2026-11-01 on nuget.org (GitHub release `sdk-v0.4.0-alpha.2`): fixes found while writing the developer manual. **Fixed**: scene changes requested during a
transition keep their order; `CoroutineRunner.StopAll` stops coroutines that had not run yet; a tap shorter than a
frame reports `Released`; `PlayMusic` with no fade starts at full volume; `Animator.Speed` refuses negative values;
`Synth.Render` gives no NaN samples for a sound of length 0; `Synth.Layer` refuses clips it would mix wrongly;
`ContentFolder.Exists` stays inside its folder and `PathOf` no longer accepts a sibling folder with the same prefix;
`SaveStore.Delete` lets an unreadable slot be saved again; launcher errors name the option with the wrong number;
`SelectionList` keeps its index valid; the template's first scene describes `CreateInput` correctly. Behaviour changes
are in the [migration notes](../migrations.md).

## 0.4.0-alpha.1

Published 2026-11-01 on nuget.org (GitHub release `sdk-v0.4.0-alpha.1`). The first public SDK: MicroEngine can be
installed, and a game created, run and distributed, with public tools.

```bash
dotnet tool install -g MicroEngine.Cli --prerelease
microengine new MyGame
```

**Packages** (all at 0.4.0-alpha.1, MIT, .NET 10):

| Package | What it is |
| --- | --- |
| `MicroEngine` | the backend-independent core, and the build targets for the game's identity |
| `MicroEngine.Sdl` | the SDL 3 platform (Windows, Linux, macOS, Android, iOS) |
| `MicroEngine.Raylib` | the raylib platform (desktop alternative, browsers), with raylib for WebAssembly |
| `MicroEngine.Desktop` | the desktop launcher with SDL 3, raylib, SDL's CPU renderer and headless |
| `MicroEngine.Cli` | `microengine`: new, run, build, package, info, doctor |
| `MicroEngine.Templates` | `dotnet new microengine-game` |

**New**

- `microengine package windows | linux | macos | android | ios | ios-simulator | web` ([Packaging](packaging.md)).
- [Application identity](app-identity.md) declared once in the project: name, id, version, description, icon,
  orientation; turned into each system's metadata and used at run time (`AppIdentity`, `PlatformRequest.Identity`).
- Signing with the developer's own keys from environment variables ([Signing](signing.md)).
- `microengine doctor` checks a machine for every target and explains how to install what is missing.

**Changed**: hosts and demo packaging; see the [migration notes](../migrations.md#040-alpha1).

**Known limitations**: Android App Bundles (`.aab`) are not built; iOS device builds and macOS notarization need the
developer's Apple credentials; Windows executables are not code-signed by the tool; desktop packages are x64 (Windows, Linux) and arm64 (macOS) only. See
[Requirements and limitations](../reference/limitations.md).

**Upgrading** from a game that referenced the engine's projects: replace the `ProjectReference` with
`<PackageReference Include="MicroEngine.Desktop" Version="0.4.0-alpha.1" />`, add the identity properties, and
package with `microengine package` instead of copying build output.
