# ADR 0019: SDK Alpha versioning and one package per backend

## Status

Accepted. Prepares SDK-1; nothing is published by this decision. Evidence: [SDK status](../guide/sdk.md),
[PRODUCT-1](../evaluations/product-1.md).

## Context

MicroEngine works on six platforms but was unversioned and used only from its repository. SDK-1 will publish it. Its
structure has to be decided before then, without promising an API stability the engine does not have.

## Decision

- **Alpha, 0.x.** The SDK version is one property, `MicroEngineVersion` in `Directory.Build.props` (`0.2.0-alpha`
  first). Every engine assembly carries it; every demo package records it next to the demo's own `<Version>`. Between
  Alpha versions public APIs may change incompatibly; each such change is listed in `docs/migrations.md`.
- **One package per project in `src/`.** `MicroEngine` (core, no native dependency), `MicroEngine.Sdl`,
  `MicroEngine.Raylib`, and `MicroEngine.Desktop` (the launcher with the official catalog, depending on both
  backends). A game that wants SDL only references `MicroEngine.Sdl` and passes its own catalog; it installs no raylib.
  All share the SDK version.
- **Default platform stays the host's.** The first profile of the catalog that can run is the default; the packages do
  not change that.
- **Templates come from samples.** `dotnet new` templates will be generated from `samples/`, so they cannot drift.
- **Publishing joins the release workflow** when SDK-1 starts: the same commit, checks and version as a demo release.

## Consequences

- `dotnet pack` already produces the four packages with MIT metadata; a project outside the repository consumed them
  from a local feed (both the full desktop package and SDL only).
- The phone and browser hosts are not packages yet; they build games of this repository.
- Bumping `MicroEngineVersion` changes every package key, so the next release rebuilds every demo package.
