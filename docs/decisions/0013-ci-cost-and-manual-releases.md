# ADR 0013: Content evidence in daily CI, manual full validation, and demo releases

## Status

Accepted. Amends [ADR 0010](0010-delivery-pipeline.md) (merging no longer publishes demos) and
[ADR 0012](0012-tiered-ci-and-platform-experiments.md) (macOS leaves daily CI; the platform workflow becomes monthly
and manual). Evidence and costs: [INFRA-1 evaluation](../evaluations/infra-1.md). Operation: [CI](../ci.md).

## Context

In the first days of the pipeline, GitHub Actions used about $5.17 of the $12 monthly credit of GitHub Free
(measured from 27 runs). macOS was 52 % of it in 43 minutes; every merge into `main` repeated the checks its pull
request had just passed; every engine change built all four WebAssembly packages on Windows; and merging any
change could put new demo packages in production. GitHub Free cannot protect branches of a private repository, so
no check can be required by GitHub.

## Decision

- **Three flows.** `ci.yml` for pull requests and `main`; `validate.yml`, manual, runs every selected check for any
  ref; `release.yml`, manual, is the only way a demo reaches production. Shared jobs live in `checks.yml` and
  `deploy-site.yml`.
- **Evidence by content.** Each check has a key over the Git blobs it can read (by exclusion, `.github/` always
  included). A passed check uploads `evidence-<check>-<key>`; a later run with the same key and an identical
  `.github` folder skips it. Merges and documentation pushes stop repeating tests.
- **Selective daily checks.** Windows .NET tests only for engine, backend and build changes; macOS, Android and
  iOS never run in daily CI (advisory in the plan); an engine-wide change builds one canary demo, a game change
  that game.
- **Demo releases.** A release `v<version>` holds copies of the approved packages and `release.json`; the latest
  published release is what production serves (`DEMOS_SOURCE=release`, enforced by `demos.mjs`). Releases are drafts
  until the site built with them passed every browser suite and was staged; `vercel promote` and publishing the
  release follow in one job, with a rollback if publishing fails.
- **One gate.** `CI result` always runs and fails when anything needed failed, was cancelled or did not run; it is
  the check to require if branch protection becomes available.
- **Platform workflow** monthly on `main` and manual, never on pull requests.

## Alternatives considered

- **Tree-level evidence** (skip everything when the whole tree passed before): simpler, but any documentation push
  would still re-run every test.
- **Actions cache for evidence**: caches from a pull request are not visible to `main`, which is where most repeats
  happened. Artifacts are, through the API.
- **A committed lock file for released demos** (ADR 0009): a release would need a commit to `main` from the
  workflow; GitHub's "latest release" is already a pointer that only the release workflow moves.
- **Building demos on Linux** (40 % cheaper per minute): the toolchain and reproducibility are proven on Windows
  only; left for a measured experiment.
- **Self-hosted runners**: researched, not adopted ([evaluation](../evaluations/infra-1.md#self-hosted-runners)).
- **Making the repository public** (free standard runners): excluded by the owner.

## Consequences

- Estimated per change class: a game's tests −78 %, an engine change with its merge −66 %, the platform workflow
  −75 % per month; documentation is unchanged (already cheap).
- Engine changes are compiled to WebAssembly for one game per pull request; a regression that breaks only another
  game's web build is found by the next validation or release, before anything is published.
- macOS regressions are found by the monthly or manual platform runs, not per change; macOS is not a supported
  platform.
- Evidence trusts any commit with the same `.github` folder: someone who can push branches can produce it. That is
  the same trust the repository already gives them.
- The first deployment after this change needs a first release (`0.1.0`); until then production keeps its current
  deployment.
