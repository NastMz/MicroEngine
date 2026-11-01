# Releasing the SDK

Maintainer procedure, not public documentation: how the SDK packages are checked and how a version reaches
nuget.org. Users read [Versions](guide/sdk.md) and the [release notes](guide/release-notes.md).

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
scenario on each system ([packaging/](../packaging/README.md)).

## Publishing a release

Only the repository owner publishes. The procedure:

Publishing uses nuget.org [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing):
the workflow exchanges a GitHub OIDC token for a nuget.org key that lives one hour. No long-lived nuget.org API key
is created or stored.

Once, before the first release:

- On nuget.org (the account that will own the packages): **Trusted Publishing** → add a GitHub Actions policy with
  repository owner `NastMz`, repository `MicroEngine`, workflow file `sdk-release.yml` (the file name only) and
  environment `nuget`, allowed to push new packages and new versions of `MicroEngine*`.
- On GitHub: the `nuget` environment (Settings → Environments) with the owner as required reviewer, deployments
  limited to `main`, and the environment secret `NUGET_USER` holding that nuget.org profile name (not the e-mail).

For every release:

1. Set `MicroEngineVersion` to the new version and add its section to [release notes](guide/release-notes.md) (and to the
   [migration notes](migrations.md) when something breaks); merge to `main`.
2. Run **sdk-release** on `main` (Actions → sdk-release → Run workflow) with `publish` off: it checks the version is
   new, runs `sdk.yml` on that commit and drafts the GitHub prerelease `sdk-v<version>` with the checked packages and
   `SHA256SUMS`. Review the draft.
3. Run it again on `main` with `publish` on: it finds the draft, waits for the `nuget` environment's approval, logs
   in with Trusted Publishing, pushes exactly the draft's files (verified against `SHA256SUMS`), engine packages
   first, and publishes the GitHub release. The packages are never rebuilt: if `main` has moved on since the draft
   (its commit must still be in `main`'s history), the run lists the commits in between in its summary.
4. When nuget.org serves the packages, run `node sdk/tests/external.mjs --public --version <version>`: it creates,
   builds, runs and packages a game from nuget.org alone. Then update the version in
   [Getting MicroEngine](guide/getting-microengine.md), [Versions](guide/sdk.md) and the [release notes](guide/release-notes.md).

A policy for a repository nuget.org has not yet seen publish from can start as temporarily active for 7 days; the
first successful publish makes it permanent.

Decisions: [ADR 0019](decisions/0019-sdk-packages.md), [ADR 0021](decisions/0021-sdk-toolchain.md).
