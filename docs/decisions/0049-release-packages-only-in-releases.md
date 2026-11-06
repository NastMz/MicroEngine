# 0049 — Release web packages are built only by releases, on Linux, keyed by the project graph

Status: Accepted (2026-11-06). Amends [0013](0013-ci-cost-and-manual-releases.md): the canary demo build, macOS never
evidence, and demos built on Windows (the toolchain was first validated there, [0009](0009-public-site.md)).

## Context

Daily CI built release WebAssembly packages for pull requests and pushes to `main` (one canary game for an
engine-wide change on private runners, every changed key on public ones) on Windows runners, and stored them in the
`demo-artifacts` prerelease. Nothing read those packages before a release: previews and site checks played them, but
the browser engine suites already compiled every game a change reaches to WebAssembly (a development package: the
same `web/build.sh`, toolchain and `microengine package web`; `--dev` only adds input scripts and URL parameters) and
played it in Chrome, Firefox and emulated phones, on Linux. The demo key hashed almost all of `src/`, so changes to
projects no game references rebuilt every package. When the store reached GitHub's 1000-asset limit (PR #128), every
run rebuilt all four packages without storing them, about four extra minutes per merge. The informative macOS tests
were not evidence, so each merge repeated the macOS run its pull request had passed.

## Decision

- **Daily CI and validation never build a release web package** (`demos.mjs plan --policy none`). The browser engine
  suites keep compiling and playing all four games (the ones a change reaches) from development packages. Sites built
  in pull requests and validation use each game's stored current package, else the released one.
- **The release workflow builds them**, for each game whose key is not stored (`--policy all`), and plays exactly
  those packages in every browser suite before publishing. A stored key is reused; a new package replaces the
  game's earlier one in the store.
- **On Linux**, like the development packages and SDL for the web. `web/build.sh` maps the toolchain folder out of
  compiled paths on every system; the toolchain is the same pinned SDK, workload and Emscripten.
- **Keys from the project graph** (`webInputs` in `site/scripts/demos.mjs`): the game's project and its
  `ProjectReference` closure (read with `.github/scripts/projects.mjs`, including references a `Directory.Build.targets`
  adds), the packaging tool and its graph, the `Directory.Build.*` files above those projects and what MSBuild files
  there name by relative path, and the pinned tools and notices. Whole project folders count, as the SDK globs them;
  no file-, class- or method-level analysis. The key's version line moved to 2, so no package built on Windows or
  keyed the old way is reused.
- **macOS evidence**: the informative macOS tests record evidence under their own check (`macos`, same inputs as the
  .NET tests, the test scope in the key) only when they pass; they still never gate.
- **Storing a package** removes an archive left without a manifest (an interrupted upload) before writing, so a
  manifest never describes another build's archive.

## Alternatives considered

- **One canary game per engine-wide change** (the previous policy): kept a release-mode build in pull requests but
  built packages nothing consumed and left the other games for a release anyway; the browser engine suites already
  compile all four.
- **Asking MSBuild for the graph** (`dotnet msbuild -getItem:ProjectReference`): exact, but needs the pinned SDK in
  every job that computes a key, including the site build; the repository's graph reader is already the one the .NET
  test scope uses.
- **Keeping the build OS in the key**: the workflow, not the repository, chooses the runner; the version line records
  the change of build environment instead.

## Consequences

- No pull request or push to `main` builds or stores a release web package; previews no longer start for a change to
  a game's code alone (they would show the released demos).
- A regression only a release-mode package shows (the development flag aside, none is known) is found by the release,
  whose dry run builds and plays the packages without publishing anything.
- `MicroEngine.Desktop`, SDL and raylib stay inputs of every demo: the games reference `MicroEngine.Desktop`, and their
  assemblies ship in the web package. Projects outside every game's graph (`MicroEngine.Graphics3D`,
  `MicroEngine.Physics.Box2D`), native packaging, the phone hosts, tests, samples and documentation never invalidate one.
- The first release after this change rebuilds the four packages (version 2 keys).
