# SDK status: Alpha

**MicroEngine SDK 0.4.0-alpha.1 — Alpha.** The version is set in one place, `MicroEngineVersion` in
[`Directory.Build.props`](../../Directory.Build.props); every package, the tool and every package built with it carry it.

Two things are true at once:

- **The engine works and is tested on six platforms.** Four complete games run on it on Windows, Linux, macOS,
  Android, iOS and in browsers ([Platforms](platforms.md)), and the SDK's own test builds, runs and packages a new
  game from the published packages on Windows, Linux and macOS.
- **Its public API is still evolving.** Between Alpha versions (0.4 → 0.5) public APIs may change incompatibly; every
  such change is recorded in a decision record and in the [migration notes](../migrations.md) of the version that
  makes it. There is no 1.0 and no promise of backward compatibility yet. The types the four demos depend on are listed
  in [Contracts](../reference/contracts.md): the most likely to stay, not guaranteed to.

> **Publication.** 0.4.0-alpha.1 is prepared, checked and ready to release, but **not yet on nuget.org**.
> [Getting MicroEngine](getting-microengine.md) shows how to install from a local build until then.

## Packages

| Package | Contains | Depends on |
| --- | --- | --- |
| `MicroEngine` | the core (loop, scenes, input actions, graphics model, world, audio synthesis, UI, saves, achievements, headless platform) and the [identity](app-identity.md) build targets | .NET only |
| `MicroEngine.Sdl` | the SDL 3 platform (desktop and phones) | `MicroEngine`, `ppy.SDL3-CS`, `NVorbis` |
| `MicroEngine.Raylib` | the raylib platform (desktop; browsers through WebAssembly), with raylib built for .NET's `wasm-tools` | `MicroEngine`, `Raylib-cs` |
| `MicroEngine.Desktop` | `DesktopLauncher` and the official desktop catalog (`sdl`, `raylib`, `sdl-software`, `headless`) | `MicroEngine.Sdl`, `MicroEngine.Raylib` |
| `MicroEngine.Cli` | the [`microengine` tool](cli.md) and the browser, Android and iOS hosts it builds with | — |
| `MicroEngine.Templates` | `dotnet new microengine-game` | — |

A game installs only what it uses: one that ships SDL only references `MicroEngine.Sdl` and gets no raylib file.
Packages carry symbols (`.snupkg`, with Source Link to the commit), XML documentation, a README and the MIT
licence. The demo games are not in any package: they stay in the repository under their own terms
([Licensing](licensing.md)).

## Versioning and compatibility

- SemVer with numbered pre-releases while in Alpha: `0.4.0-alpha.1`, `0.4.0-alpha.2`... A new minor (0.5) marks
  incompatible changes; numbered Alphas of one minor are for fixes and additions.
- All six packages share one version, and the tool builds games with the hosts of its own version: use the same
  version for the tool and the engine packages (the template does).
- A game's own version is its own (`<Version>` in its project), independent of the SDK's.
- SDK releases are tagged `sdk-v<version>`; the demo releases (`v0.1.0`, `v0.2.0`...) are a separate series.

**Upgrading** a game: change the `MicroEngine.*` package versions in its project, `dotnet tool update -g
MicroEngine.Cli --prerelease`, then read the [migration notes](../migrations.md) from your version up.

## How the SDK is checked

`sdk.yml` runs on every change to the engine, the tool, the template or these checks:

1. **Packages** (`sdk/tests/packages.mjs`): one version everywhere, MIT, README and repository metadata, exactly the
   dependencies each package should have (no raylib in `MicroEngine.Sdl`), the files that make each one work (the
   identity targets, raylib for WebAssembly, the hosts, the template), no build-machine path, no demo file.
2. **An external project** (`sdk/tests/external.mjs`) on Windows, Linux and macOS: in a folder outside the
   repository, with only the package feed and nuget.org, it installs `microengine`, creates a game with
   `microengine new`, gives it its own name, id, version and icon, builds it, runs it with scripted input (the
   captures before and after must differ: input, drawing and a sprite from Content), packages it for each target the
   runner can build (Windows, Linux, macOS, Android, web), checks each package's files and metadata, runs the desktop
   package, plays the web package in Chrome, and checks the failures a developer meets (invalid id, missing or invalid
   icon, missing signing key, a target the system cannot build, a missing workload).

The four demos are packaged by the same tool; their packages are also verified level by level and played scenario by
scenario on each system ([packaging/](../../packaging/README.md)).

## Publishing a release

Only the repository owner publishes. The procedure:

1. Set `MicroEngineVersion` to the new version and add its section to [release notes](release-notes.md) (and to the
   [migration notes](../migrations.md) when something breaks); merge to `main`.
2. Create the nuget.org API key (scoped to push the `MicroEngine*` packages) as the `NUGET_API_KEY` secret, and a
   `nuget` environment whose required reviewer is the owner (once).
3. Run **sdk-release** (Actions → sdk-release → Run workflow) with `publish` off: it checks the version is new, runs
   `sdk.yml` on that commit and drafts the GitHub prerelease `sdk-v<version>` with the checked packages and
   `SHA256SUMS`. Review the draft.
4. Run it again with `publish` on: after the `nuget` environment's approval it pushes exactly the draft's files
   (verified against `SHA256SUMS`) to nuget.org, engine packages first, then publishes the GitHub release.
5. When nuget.org lists the packages, update [Getting MicroEngine](getting-microengine.md) and the site so they no
   longer say "not yet on nuget.org".

Decisions: [ADR 0019](../decisions/0019-sdk-packages.md), [ADR 0021](../decisions/0021-sdk-toolchain.md).
