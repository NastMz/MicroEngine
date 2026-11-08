# Releasing

Maintainer procedure, not public documentation: what MicroEngine publishes, when each process runs, and how to
recover one that stopped. Users read [Versions](guide/sdk.md) and the [release notes](guide/release-notes.md).
Decisions: [ADR 0022](decisions/0022-release-coordination.md) (this procedure), [ADR 0048](decisions/0048-docs-ship-with-the-sdk.md) (documentation and releases), [ADR 0019](decisions/0019-sdk-packages.md),
[ADR 0021](decisions/0021-sdk-toolchain.md), [ADR 0013](decisions/0013-ci-cost-and-manual-releases.md). Checks and
workflows: [CI](ci.md).

## Three things are published, apart

| | SDK | Demo distribution | Site |
| --- | --- | --- | --- |
| What | seven NuGet packages, one version (ADR 0019) | the four games: web packages and native downloads | documentation, landing, demo pages |
| Identified by | SemVer (`0.x.y-alpha.n` in Alpha), tag `sdk-v<version>` | `v<version>` (`v0.4.0`): a release number, not an API promise; each game keeps its own `<Version>` | the Vercel deployment of a `main` commit |
| Source of truth | nuget.org (immutable) | the latest published GitHub release `v*` | the production deployment |
| Starts | merging a pull request that documents a new version; the `nuget` environment's approval publishes it | by hand: Actions → release | every push to `main` that changes the site |
| Workflow | `sdk-release.yml` | `release.yml` | `ci.yml` (and the two others, which deploy after publishing) |

They depend on each other in one direction only: the **site** shows the documented SDK version and offers the
latest demo release. The demos are built from the engine's source at their release commit (each download records the
engine version it was built with), not from the SDK packages, so a demo release never waits for an SDK release and an
SDK release never rebuilds the demos.

| Change | Merge | Then |
| --- | --- | --- |
| Code without publication (engine internals, tool internals, tests) | pull request, **CI result** | nothing is published |
| Documentation or landing only | pull request, **CI result** | `main`'s ci deploys production, unless SDK changes are pending (below); no package, no release |
| A demo game | pull request, **CI result** | run **release** when players should get it; only the changed game's packages are built |
| The SDK (anything a game made with it can see), with its documentation | pull request, **CI result**, an entry under "Next release" in the release notes | production waits for the SDK release that ships it; **release** for the demos once that SDK release is out |
| A new SDK version | release pull request (below) | `sdk-release` runs, waits for approval, publishes, deploys the site |
| An urgent site fix | pull request, **CI result** | production deploys on merge, unless SDK changes are pending; nothing else runs |

## Documentation and releases

The site documents the SDK on nuget.org, and the two are published together, so they never disagree:

- **Documentation is written in the present tense**, as if the change were already published: no "not published
  yet", "next SDK" or "coming in" labels on any page. A pull request that changes the SDK changes its documentation
  in the same pull request and lists the change under `## Next release` in the
  [release notes](guide/release-notes.md) (and, when it breaks something, under `## Next release` in the
  [migration notes](migrations.md)).
- **Pending SDK changes hold production.** While the release notes' "Next release" section has content
  (`node sdk/version.mjs status` prints `pending: true`), every production deployment keeps the current one, with a
  warning, and a demo release is refused. Previews still deploy. The SDK release that ships those changes deploys the
  site once nuget.org serves them.
- **A site-only change never needs an SDK version.** With nothing pending, it deploys on merge. With SDK changes
  pending, it waits for their release like everything else on `main`; to publish it sooner, release the SDK (the
  release is never empty: it ships what is pending) or revert the pending change.
- `node sdk/version.mjs release <version>` turns each "Next release" section into `## <version>`, rewrites the links
  to it, and refuses to prepare a version when nothing is listed under "Next release".

## Versions

| Concept | Where | Who changes it |
| --- | --- | --- |
| **Prepared**: the version the code packs as | `MicroEngineVersion` in `Directory.Build.props` | a release pull request, or by hand to start a cycle |
| **Documented**: the version pages, samples and install commands present | `sdk/version.json` | `node sdk/version.mjs release <version>` |
| **Public**: what anyone can install | nuget.org; GitHub release `sdk-v<version>` | `sdk-release.yml` only |
| **Demo distribution** | latest GitHub release `v<version>` | `release.yml` only |
| **Deployed site** | Vercel production, built from a `main` commit | `deploy-site.yml` only |

Rules:

- The documented version is either public or the prepared one. Production deploys only when it is public and
  nothing is pending ([Documentation and releases](#documentation-and-releases)), so the public site never presents a
  version or a feature nobody can install. `node sdk/version.mjs status --public` prints all three and `pending`.
- Active references (install commands, package references in the samples, "the current version is") follow the
  documented version: `node sdk/version.mjs check` fails in CI when one does not. Historical text never changes: the
  release notes, the migration notes, decisions, evaluations, and a paragraph in an active page marked with
  `<!-- sdk-version: history -->` on the line above it ("from version N every change waits").
- Raising `MicroEngineVersion` alone (to start the next cycle) publishes nothing: no page documents it yet.
- The demo distribution's default version is the minor after the latest one; any new `x.y.z` can be given instead.

## How the SDK is checked

Daily CI runs the SDK check (`sdk.yml`) whenever a package input changes, and **CI result** requires it:

1. **Packages** (`sdk/tests/packages.mjs`): one version everywhere, MIT, README and repository metadata, exactly the
   dependencies each package should have (no raylib in `MicroEngine.Sdl`), the files that make each one work (the
   identity targets, raylib for WebAssembly, the hosts, the template), no build-machine path, no demo file. To run it
   on a local feed, pack as the workflow does (`dotnet pack MicroEngine.slnx -c Release -o <feed>
   -p:MicroEngineRelease=true`): without that flag the build keeps this machine's paths and the path check fails.
2. **An external project** (`sdk/tests/external.mjs`) on Windows, Linux and macOS: in a folder outside the
   repository, with only the package feed and nuget.org, it installs `microengine`, creates a game with
   `microengine new`, gives it its own name, id, version and icon, builds it, runs it with scripted input (the
   captures before and after must differ: input, drawing and a sprite from Content), packages it for each target the
   runner can build (Windows, Linux, macOS, Android, web), checks each package's files and metadata, runs the desktop
   package, plays the web package in Chrome, and checks the failures a developer meets (invalid id, missing or invalid
   icon, missing signing key, a target the system cannot build, a missing workload).
3. **The documented samples** (`sdk/tests/docs.mjs`, in the .NET Linux job): each sample the site quotes is copied
   outside the repository and built against the packages this commit packs (the SDK built from source), and, once the
   documented version is public, also from nuget.org at that version. The nuget.org build is only reported while the
   release notes list changes under `## Next release`, or while the documented version predates breaking changes the
   samples already use ([CI, documentation checks](ci.md#documentation-checks)).

The four demos are packaged by the same tool; their packages are also verified level by level and played scenario by
scenario on each system ([packaging/](../packaging/README.md)).

## Publication freeze

While [`.github/phase-freeze`](../.github/phase-freeze) exists on `main`, none of the three is published, whatever
merges: `main`'s ci deploys no production site, **sdk-release** stops in its preflight with a "Frozen" summary (after
a merge or by hand: no check, draft, nuget.org push, GitHub release or site), and **release** refuses. Pull request
checks and previews run as usual. The full list of effects is in [CI, Publication freeze](ci.md#publication-freeze).

To lift it, the owner merges a pull request that deletes the file. That merge deploys production with `main`'s site
(unless the documented SDK version is not on nuget.org yet, as always) and runs sdk-release: a version documented
during the freeze is then checked, drafted and waits for the `nuget` environment's approval as in
[Releasing the SDK](#releasing-the-sdk). Demo releases can be run again by hand. To freeze again, add the file back
with its reason on the first line that is not a `#` comment.

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

1. On a branch: `node sdk/version.mjs release <version>`. It sets `MicroEngineVersion` and `sdk/version.json`,
   renames the "Next release" sections of the [release notes](guide/release-notes.md) and the
   [migration notes](migrations.md) to `## <version>` (with the links to them), and rewrites every active reference
   (the install pages, the tutorial, the samples' package references). Then `node site/scripts/api-reference.mjs`
   (the API reference names the version). `node sdk/version.mjs check` passes when nothing is missing. Open the pull request. Its CI packs and checks the SDK, and builds the documented samples
   against those packages.
2. Merge it. From then until the packages are public, production keeps its deployment (the documentation of `main`
   presents a version nobody can install yet); previews still deploy.
3. When ci passes on `main`, **sdk-release** starts on its own: it checks the commit again with `sdk.yml` (Windows,
   Linux, macOS), drafts the GitHub prerelease `sdk-v<version>` with the checked packages and `SHA256SUMS`, and waits
   at **Publish to nuget.org** for the `nuget` environment.
4. Review the draft (GitHub → Releases) and approve the deployment (the run's page → Review deployments). The run
   pushes exactly the draft's files, verified against `SHA256SUMS`, engine packages first; publishes the GitHub
   release; waits until nuget.org serves all seven packages; creates, builds, runs and packages a game, and builds the
   documented samples, from nuget.org alone; then deploys production with `main`'s head.

Nothing else is done by hand. To not publish: reject the deployment (step 4); the draft stays and nothing is public.
A manual run of the same commit asks for approval of that draft again; the next merge with a new commit re-drafts. To abandon the version, revert the release pull request.

One item of [ARCH-1 §12.7 (c)](evaluations/arch-1.md#final-state) is left for the demos, not the SDK: the site's demo
notice check (`noticeFiles` in `site/scripts/demos.mjs`) requires `licenses/SDL-LICENSE.txt` from the first demo
release built on SDL in browsers.

## Recovering

Run **sdk-release** by hand (Actions → sdk-release → Run workflow, on `main`). Its preflight reads what is already
done and continues from there; the summary says what it found:

| State | The run |
| --- | --- |
| Nothing public, a draft of this commit (a rejected or timed-out approval) | publishes that draft; never rebuilds it |
| Nothing public, a draft of an earlier commit (a fix merged since) | checks and drafts this commit instead |
| Some packages on nuget.org (a push failed part way), or all seven with the GitHub release still a draft | pushes only the missing ones from the draft, then publishes the release; the draft's commit may be older than `main` (the summary lists the commits in between) |
| Published, but nuget.org was slow, a check failed or the site did not deploy | verifies nuget.org and deploys the site again |
| Documented version not public and not the prepared one | refused: a pull request must run `node sdk/version.mjs release` again |

Packages on nuget.org are immutable: a pushed version is never overwritten or deleted. A defect in a published
version is fixed by a new version, never by re-pushing; unlisting it on nuget.org (the package's
Manage page) is the owner's decision. Two cases need a person, and the preflight says so: packages on nuget.org with
no draft to finish from (push the rest by hand with `dotnet nuget push`, from files that match the published ones),
and a published GitHub release whose packages nuget.org lacks.

## Releasing the demos

Actions → **release** → Run workflow, with the ref (default `main`) and optionally the version; empty takes the
minor after the latest release. It builds only the packages missing for that commit's content keys, drafts
`v<version>`, verifies every file, deploys production with `main`'s site and publishes the release; a failure before
the switch discards the draft and changes nothing public. The full procedure, its checks and rollback are in
[CI, Releases](ci.md#releases). It is refused while the documentation of `main` presents an SDK version nuget.org does
not serve yet, or while SDK changes are pending: let sdk-release ship them first.

## Deploying the site

Merging is deploying: a push to `main` that changes the published site deploys production after its checks, with
the latest demo release. An urgent fix is an ordinary pull request; it runs only the checks its files need
([CI, What runs for a change](ci.md#what-runs-for-a-change)) and no package or release. Exceptions: while SDK changes
are pending, or an SDK release is merged but not yet approved, production is held for every deployment; release the
SDK (or approve the release), or revert the pending change, to let the fix through. Rollback: Actions →
**rollback** ([publishing the site](deployment.md)).
