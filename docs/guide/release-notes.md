# SDK release notes

One section per SDK version, newest first. The SDK is in Alpha: a version may change public APIs incompatibly, and
every such change is in the [migration notes](../migrations.md). Releases are tagged `sdk-v<version>`; the demo
releases (`v0.1.0`, `v0.2.0`...) are separate.

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

**Known limitations**: Android App Bundles (`.aab`) are not built; iOS device builds and macOS notarization run only
with the developer's Apple credentials and are not exercised in MicroEngine's CI; Windows executables are not
code-signed by the tool; desktop packages are x64 (Windows, Linux) and arm64 (macOS) only. See
[Status and limitations](status.md).

**Upgrading** from a game that referenced the engine's projects: replace the `ProjectReference` with
`<PackageReference Include="MicroEngine.Desktop" Version="0.4.0-alpha.1" />`, add the identity properties, and
package with `microengine package` instead of copying build output.
