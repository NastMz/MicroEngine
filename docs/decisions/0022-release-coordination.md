# ADR 0022: Three releases with explicit contracts, and an SDK released by one pull request and one approval

## Status

Accepted. Amends [ADR 0013](0013-ci-cost-and-manual-releases.md) (the SDK release starts after CI on `main` instead
of by hand; its approval stays human) and [ADR 0019](0019-sdk-packages.md) (versioning concepts; the six packages keep
one version). Evidence and measurements: [DEVOPS-1](../evaluations/devops-1.md). Procedure: [Releasing](../releasing.md).

## Context

MicroEngine publishes an SDK (six NuGet packages), a demo distribution (GitHub releases `v*`) and a site (Vercel).
Each pipeline worked, but an SDK release took two pull requests, three workflow runs started by hand, one approval,
a command run locally and an edit of 18 files for version references; the SDK checks were not part of the required
**CI result**; the release workflow refused a version already on nuget.org, so a publication that pushed the packages
and then failed could not be finished by the workflow. The site and the demos were already decoupled (merging deploys
the site with the latest demo release; a demo release deploys `main`'s site).

## Decision

- **Three publications, not one pipeline.** SDK, demo distribution and site keep their own workflows. Their only
  dependency is one-way: the site presents the documented SDK version and the latest demo release. A central
  orchestrator was rejected: it would couple an SDK release to a demo rebuild (20 native package jobs, 23 minutes
  measured) and a site hotfix to both, and its state would duplicate what nuget.org and GitHub releases already hold.
- **One source of truth per concept.** Prepared version: `MicroEngineVersion`. Documented version: `sdk/version.json`.
  Public version: nuget.org. Demo distribution: the latest published `v*` release. Site: the production deployment.
  The six packages keep one version (ADR 0019): independent versions would multiply the compatibility matrix the tool
  and its hosts depend on, to save pushing unchanged packages.
- **Documentation references are synchronised, not templated.** Install commands and samples must stay literal (a
  reader copies them; the samples are built outside the repository). `sdk/version.mjs` finds active references by
  pattern, rewrites them for a release and fails CI when one lags; history (release and migration notes, decisions,
  evaluations, marked paragraphs) is never read. Any mention of a known SDK version outside both fails CI.
- **A release is requested by merging documentation of an unpublished version.** The release pull request carries
  the version, the notes and the documentation together; CI builds the documented samples against the packages of
  that commit. Production deploys only when the documented version is public, so `main` may document a version before
  it is public but the public site never does. Previews are not held.
- **`sdk-release` starts after CI passes on `main` (`workflow_run`), or by hand, and is a resumable state machine.**
  Its preflight reads nuget.org package by package and the GitHub release, and continues from the first unfinished
  step. The `nuget` environment's required reviewer is the only approval; the approved draft's files are what is
  pushed, never rebuilt. After publishing it waits for nuget.org, builds a game and the documented samples from it,
  and deploys production.
- **The SDK check is a daily CI check** with evidence, gated by **CI result**; `sdk.yml` is a called workflow only.
- **Demo releases stay manual** (ADR 0013): merging never publishes a game. Their version defaults to the next minor.

## Consequences

- An SDK release is one pull request and one approval; recovering is running the same workflow again.
- Between merging a release and approving it, production does not deploy: a site hotfix waits for the approval or a
  revert. The window is the owner's to keep short.
- Every merge to `main` starts a short sdk-release run that ends with "nothing to release".
- No cross-service rollback is promised: a pushed NuGet version is immutable; a defect is fixed by a new version.
- Native demo packages are still built at release time, not on `main`; prebuilding them is a separate decision.
