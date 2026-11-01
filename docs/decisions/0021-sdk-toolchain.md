# ADR 0021: The SDK toolchain: identity in the game's project, one `microengine` tool, demos as its consumers

## Status

Accepted. Amends [ADR 0019](0019-sdk-packages.md) (templates, hosts, versioning) and [ADR 0018](0018-native-demo-distribution.md)
(how demo packages are built). Evidence: [SDK-1](../evaluations/sdk-1.md).

## Context

After PRODUCT-1 the engine packed into four NuGet packages, but nothing outside this repository could use the rest:
browser, Android and iOS hosts picked a demo from `web/games.props` and built from repository paths; native packages
came from `packaging/package.mjs`, which wrote demo-specific notices, names and ids. Distributed apps showed a
generic name (the Android launcher's `MicroEngine`) and the system's default icon. SDK-1 must let an outside developer
create, run and distribute a game with public tools, and the demos must use those same tools.

## Decision

- **Identity lives in the game's project file.** `ApplicationTitle`, `ApplicationId`, `Version`, `Description`,
  `MicroEngineIcon` (one square PNG), `MicroEngineOrientation` and `MicroEngineGameFactory` are MSBuild properties,
  the names .NET's own Android and iOS builds use where one exists. The `MicroEngine` package's build targets
  (`build/MicroEngine.targets`, imported by every game that references the engine) validate them, write them into the
  game assembly and give the Windows executable its icon; platforms read them at run time (`AppIdentity`). Nothing
  else holds a game's name, id or icon: packaging reads the project with MSBuild. `web/games.props` is removed.
- **One tool, `microengine` (`MicroEngine.Cli`, a .NET tool).** It wraps `dotnet new`, `run`, `build` and `publish`
  and each system's own tools (Android SDK, Xcode, `codesign`, `notarytool`), and builds packages for `windows`,
  `linux`, `macos`, `android`, `ios`, `ios-simulator` and `web`. The browser, Android and iOS hosts become templates
  inside it: it copies one into the game's `obj/microengine/<target>` folder and writes only what is the game's
  (identity, paths, `GameEntry.cs`). It refuses what a host cannot do (a Windows package off Windows, an Apple package
  off a Mac) instead of producing an incomplete package.
- **Signing stays the developer's.** Keys come only from environment variables; the tool never creates or stores a
  key, never prints a secret, and needs no MicroEngine service. Without a key an Android build stops unless
  `--debug-key` asks for a test build; an iPhone build stops without an Apple certificate and profile.
- **The demos are consumers.** Each demo declares its identity in its project; `packaging/package.mjs` and
  `web/build.sh` call `microengine package` and add only what the release pipeline needs (content keys, store names).
  Verification (`packaging/verify.mjs`), the content-addressed store and releases are unchanged.
- **Templates are their own source.** `MicroEngine.Templates` holds a complete game (`templates/`); ADR 0019's plan to
  generate templates from `samples/` is dropped: the external project test (`sdk/tests/external.mjs`) instantiates
  the template from the packages, which proves it builds, runs and packages, while `samples/` stays a sample.
- **Versioning.** The first public SDK version is `0.4.0-alpha.1`: 0.x Alpha as before, a minor bump because this
  change breaks the host and packaging interfaces, and a numbered pre-release so later Alphas sort
  (`alpha.1` < `alpha.2` < `alpha.10`). SDK releases are tagged `sdk-v<version>`, apart from the demo releases
  (`v0.1.0`, `v0.2.0`), whose numbers had collided with engine versions. All six packages share the version.
- **Publishing** is `sdk-release.yml`, run by hand: it checks the packages on three systems, drafts the GitHub
  release and pushes to nuget.org only when asked, behind the `nuget` environment's approval.

## Consequences

- Every demo content key changes once (hosts moved into `src`); demo releases rebuild every package after this change.
- A game outside the repository is checked on every SDK change (`sdk.yml`): installed from a feed, created from the
  template, built, run with scripted input, packaged for each target the runner can build.
- Notarization, iOS device builds and App Store or Play distribution are supported by the tool's arguments but only
  checked as far as the runners allow (no Apple developer account or store account in CI); the documentation says so.
- The phone and browser hosts follow the engine version through the tool: a game built with tool `X` uses hosts `X`.
