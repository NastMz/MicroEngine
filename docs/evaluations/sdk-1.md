# SDK-1: first public SDK release and game distribution toolchain

Record of SDK-1 (2026-11-01): what was built, what was run and what it showed. Task record:
[`odd/tasks/sdk-1.md`](../../odd/tasks/sdk-1.md). Decision: [ADR 0021](../decisions/0021-sdk-toolchain.md).

## What was built

| Piece | Where |
| --- | --- |
| Application identity: project properties, build targets (checks, Windows icon, assembly metadata), `AppIdentity` at run time | `src/MicroEngine/build/MicroEngine.targets`, `src/MicroEngine/Core/AppIdentity.cs`, SDL and raylib platforms |
| `microengine` (`MicroEngine.Cli`): new, run, build, package (windows, linux, macos, android, ios, ios-simulator, web), info, doctor | `src/MicroEngine.Cli` |
| Browser, Android and iOS hosts as templates of the tool | `src/MicroEngine.Cli/Hosts` (moved from `web/Host`, `platforms/android`, `platforms/ios`, `platforms/Shared`) |
| `dotnet new microengine-game` | `templates/MicroEngine.Templates` |
| Six packages at `0.4.0-alpha.1` with README, symbols, MIT; web raylib in `MicroEngine.Raylib` | `src/*`, `templates/*` |
| Package checks and the external project test | `sdk/tests/packages.mjs`, `sdk/tests/external.mjs` |
| SDK CI and release workflows | `.github/workflows/sdk.yml`, `sdk-release.yml` |
| Demos as consumers: identity and icons in their projects; `package.mjs`, `web/build.sh`, `platforms.yml` call the tool | `games/*`, `packaging/`, `web/`, `.github/workflows/platforms.yml` |

## Evidence (local, Windows 11)

| Check | Result |
| --- | --- |
| `dotnet test MicroEngine.slnx -c Release` | all passed (161 engine, 88 Trail Duel, 47 Meadow Run, 33 Neon Rift, 31 Courtyard Key, 8 desktop, 1 raylib), with the new identity tests |
| Site unit tests, CI planner tests | 36/36, 42/42 |
| Package checks on a release pack (`-p:MicroEngineRelease=true`) | all passed: one version, MIT, README, dependencies (no raylib in `MicroEngine.Sdl`), files, no build-machine path, no demo file. A local non-release pack names this machine's paths in the DLLs; the release pack does not |
| External project, `--targets windows,linux,android --device` | all passed: tool and template installed from the feed into a folder outside the repository; new game with its own title, id, version and icon; build; scripted run moved the hero (captures differ, inspected); Windows package extracted, its executable named "Sample Game", run on SDL's CPU renderer; Linux package with desktop entry and icon; APK labelled "Sample Game" with its icon and id, installed and started on the Android emulator (API 36.1); expected failures (invalid id, missing icon, non-square icon, not a PNG, no signing key, no application id, unknown target, macOS and iOS off a Mac, no wasm-tools workload, no project) each with its message |
| External project, `--targets web` (SDK with wasm-tools) | page titled "Sample Game" with favicon and content; served and played in Chrome: reached `playing`, no page errors |
| Neon Rift Windows package through `package.mjs` → `microengine` | `verify.mjs`: every level passed, 12/12 captures pixel-identical to the SDL reference |
| Meadow Run Linux package (cross-built on Windows) | `verify.mjs` structure, paths, build-info passed; `linux-distros.mjs`: Ubuntu 22.04, Debian 12, Fedora 41, 13/13 captures pixel-identical each |
| Neon Rift APK through the tool (debug key) | `verify.mjs` on the emulator: installed, started, rendered 12/12 within reference, audio, input, lifecycle, touch, drag, two thumbs |
| Meadow Run web package through `web/build.sh` → `microengine` | browser suites in Chrome: platform and scripts passed; the storage check needs Neon Rift's package, which that run did not build |
| Site build (docs only) and link check | 46 pages, `check-dist` complete |

Bugs found by these checks and fixed: `dotnet new list` exits 103 when no template matches; a project with package
references was read before restore (no targets yet); the tool's `microengine.dll` collided with `MicroEngine.dll` on
case-insensitive file systems (renamed `MicroEngine.Cli.dll`); Android runtime identifiers were escaped into one value.
A game with no scene stops on its first update (`SceneManager.Update` indexes an empty stack): present before SDK-1,
left as is.

## Not exercised

- macOS packaging, the iOS simulator app built by the tool and the macOS external project run only on CI's macOS
  runners (`sdk.yml`, `platforms.yml`); notarization and iOS device builds need an Apple developer account, which CI
  does not have: their code paths are written to Apple's documented commands but have not run.
- Android App Bundles are not built. Windows executables are not code-signed by the tool.
- Nothing was published: no package is on nuget.org and no `sdk-v*` release exists.
