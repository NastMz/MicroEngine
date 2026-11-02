# Releasing

Maintainer procedure, not public documentation: what MicroEngine publishes, when each process runs, and how to
recover one that stopped. Users read [Versions](guide/sdk.md) and the [release notes](guide/release-notes.md).
Decisions: [ADR 0022](decisions/0022-release-coordination.md) (this procedure), [ADR 0019](decisions/0019-sdk-packages.md),
[ADR 0021](decisions/0021-sdk-toolchain.md), [ADR 0013](decisions/0013-ci-cost-and-manual-releases.md). Checks and
workflows: [CI](ci.md).

## Three things are published, apart

| | SDK | Demo distribution | Site |
| --- | --- | --- | --- |
| What | six NuGet packages, one version (ADR 0019) | the four games: web packages and native downloads | documentation, landing, demo pages |
| Identified by | SemVer `0.4.0-alpha.2`, tag `sdk-v<version>` | `v<version>` (`v0.4.0`): a release number, not an API promise; each game keeps its own `<Version>` | the Vercel deployment of a `main` commit |
| Source of truth | nuget.org (immutable) | the latest published GitHub release `v*` | the production deployment |
| Starts | merging a pull request that documents a new version; the `nuget` environment's approval publishes it | by hand: Actions → release | every push to `main` that changes the site |
| Workflow | `sdk-release.yml` | `release.yml` | `ci.yml` (and the two others, which deploy after publishing) |

They depend on each other in one direction only: the **site** shows the documented SDK version and offers the
latest demo release. The demos are built from the engine's source at their release commit (each download records the
engine version it was built with), not from the SDK packages, so a demo release never waits for an SDK release and an
SDK release never rebuilds the demos.

| Change | Merge | Then |
| --- | --- | --- |
| Code without publication (engine, tool, tests) | pull request, **CI result** | nothing is published |
| Documentation or landing only | pull request, **CI result** | `main`'s ci deploys production; no package, no release |
| A demo game | pull request, **CI result** | run **release** when players should get it; only the changed game's packages are built |
| The engine, for SDK users and the demos | pull request, **CI result** | an SDK release pull request when it should ship; **release** for the demos when they should |
| A new SDK version | release pull request (below) | `sdk-release` runs, waits for approval, publishes, deploys the site |
| An urgent site fix | pull request, **CI result** | production deploys on merge; nothing else runs |

## Versions

| Concept | Where | Who changes it |
| --- | --- | --- |
| **Prepared**: the version the code packs as | `MicroEngineVersion` in `Directory.Build.props` | a release pull request, or by hand to start a cycle |
| **Documented**: the version pages, samples and install commands present | `sdk/version.json` | `node sdk/version.mjs release <version>` |
| **Public**: what anyone can install | nuget.org; GitHub release `sdk-v<version>` | `sdk-release.yml` only |
| **Demo distribution** | latest GitHub release `v<version>` | `release.yml` only |
| **Deployed site** | Vercel production, built from a `main` commit | `deploy-site.yml` only |

Rules:

- The documented version is either public or the prepared one. Production deploys only when it is public, so the
  public site never presents a version nobody can install. `node sdk/version.mjs status --public` prints all three.
- Active references (install commands, package references in the samples, "the current version is") follow the
  documented version: `node sdk/version.mjs check` fails in CI when one does not. Historical text never changes: the
  release notes, the migration notes, decisions, evaluations, and a paragraph in an active page marked with
  `<!-- sdk-version: history -->` on the line above it ("from 0.4.0-alpha.2 every change waits").
- Raising `MicroEngineVersion` alone (to start the next cycle) publishes nothing: no page documents it yet.
- The demo distribution's default version is the minor after the latest one; any new `x.y.z` can be given instead.

## How the SDK is checked

Daily CI runs the SDK check (`sdk.yml`) whenever a package input changes, and **CI result** requires it:

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
3. **The documented samples** (`sdk/tests/docs.mjs`, in the .NET Linux job): each sample the site quotes is copied
   outside the repository and built at the documented version: from nuget.org once it is public, from the packages
   this commit packs before that (a release pull request).

The four demos are packaged by the same tool; their packages are also verified level by level and played scenario by
scenario on each system ([packaging/](../packaging/README.md)).

## Releasing the SDK

Publishing uses nuget.org [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing):
the workflow exchanges a GitHub OIDC token for a nuget.org key that lives one hour. No long-lived nuget.org API key
is created or stored. Only the owner publishes: the `nuget` environment's required reviewer.

Once, before the first release (done for this repository):

- On nuget.org (the account that owns the packages): **Trusted Publishing** → a GitHub Actions policy with repository
  owner `NastMz`, repository `MicroEngine`, workflow file `sdk-release.yml` (the file name only) and environment
  `nuget`, allowed to push new packages and new versions of `MicroEngine*`. A policy for a repository nuget.org has not
  yet seen publish from can start as temporarily active for 7 days; the first successful publish makes it permanent.
- On GitHub: the `nuget` environment (Settings → Environments) with the owner as required reviewer, deployments
  limited to `main`, and the environment secret `NUGET_USER` holding that nuget.org profile name (not the e-mail).

For every release, **one pull request and one approval**:

1. On a branch: `node sdk/version.mjs release 0.4.0-alpha.3`. It sets `MicroEngineVersion` and `sdk/version.json` and
   rewrites every active reference (the install pages, the tutorial, the samples' package references). Write the
   version's section in [release notes](guide/release-notes.md) (`## 0.4.0-alpha.3`) and, when something breaks, in
   the [migration notes](migrations.md); `node sdk/version.mjs check` passes when nothing is missing. Open the pull
   request. Its CI packs and checks the SDK, and builds the documented samples against those packages.
2. Merge it. From then until the packages are public, production keeps its deployment (the documentation of `main`
   presents a version nobody can install yet); previews still deploy.
3. When ci passes on `main`, **sdk-release** starts on its own: it checks the commit again with `sdk.yml` (Windows,
   Linux, macOS), drafts the GitHub prerelease `sdk-v<version>` with the checked packages and `SHA256SUMS`, and waits
   at **Publish to nuget.org** for the `nuget` environment.
4. Review the draft (GitHub → Releases) and approve the deployment (the run's page → Review deployments). The run
   pushes exactly the draft's files, verified against `SHA256SUMS`, engine packages first; publishes the GitHub
   release; waits until nuget.org serves all six packages; creates, builds, runs and packages a game, and builds the
   documented samples, from nuget.org alone; then deploys production with `main`'s head.

Nothing else is done by hand. To not publish: reject the deployment (step 4); the draft stays, nothing is public, and
the next merge or a manual run re-drafts. To abandon the version, revert the release pull request.

## Recovering

Run **sdk-release** by hand (Actions → sdk-release → Run workflow, on `main`). Its preflight reads what is already
done and continues from there; the summary says what it found:

| State | The run |
| --- | --- |
| Nothing public, a draft of this commit (a rejected or timed-out approval) | publishes that draft; never rebuilds it |
| Nothing public, a draft of an earlier commit (a fix merged since) | checks and drafts this commit instead |
| Some packages on nuget.org (a push failed part way), or all six with the GitHub release still a draft | pushes only the missing ones from the draft, then publishes the release; the draft's commit may be older than `main` (the summary lists the commits in between) |
| Published, but nuget.org was slow, a check failed or the site did not deploy | verifies nuget.org and deploys the site again |
| Documented version not public and not the prepared one | refused: a pull request must run `node sdk/version.mjs release` again |

Packages on nuget.org are immutable: a pushed version is never overwritten or deleted. A defect in a published
version is fixed by a new version (`0.4.0-alpha.4`), never by re-pushing; unlisting it on nuget.org (the package's
Manage page) is the owner's decision. Two cases need a person, and the preflight says so: packages on nuget.org with
no draft to finish from (push the rest by hand with `dotnet nuget push`, from files that match the published ones),
and a published GitHub release whose packages nuget.org lacks.

## Releasing the demos

Actions → **release** → Run workflow, with the ref (default `main`) and optionally the version; empty takes the
minor after the latest release. It builds only the packages missing for that commit's content keys, drafts
`v<version>`, verifies every file, deploys production with `main`'s site and publishes the release; a failure before
the switch discards the draft and changes nothing public. The full procedure, its checks and rollback are in
[CI, Releases](ci.md#releases). It is refused while the documentation of `main` presents an SDK version nuget.org does
not serve yet: let sdk-release finish first.

## Deploying the site

Merging is deploying: a push to `main` that changes the published site deploys production after its checks, with
the latest demo release. An urgent fix is an ordinary pull request; it runs only the checks its files need
([CI, What runs for a change](ci.md#what-runs-for-a-change)) and no package or release. Rollback: Actions →
**rollback** ([publishing the site](deployment.md)).
