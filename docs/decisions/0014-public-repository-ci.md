# ADR 0014: Project-graph check selection and fork-safe CI for a public repository

## Status

Accepted for the CI changes; the repository itself stays private until the owner decides (see
[PUBLIC-1](../evaluations/public-1.md)). Amends [ADR 0013](0013-ci-cost-and-manual-releases.md) (scope of the .NET
and native checks, macOS on public runners). Operation: [CI](../ci.md).

## Context

The owner decided to prepare MicroEngine for a public repository, where standard GitHub-hosted runners cost nothing.
Three things in the INFRA-1 pipeline did not fit:

- **Selection by folder.** A game change ran every test project and compared every game's captures, although the
  `.csproj` references already say that a game's tests are the only tests that depend on it.
- **Fork pull requests.** On a public repository anyone can open one. Its workflows and tests are untrusted, its
  token is read-only and it gets no secrets. INFRA-1 accepted evidence artifacts from any run (a fork run could
  upload one under any name), and a fork that changed a game could not pass CI: storing its demo package needs
  `contents: write`.
- **macOS** was dropped from daily CI for cost (ADR 0013); on public runners that reason disappears, while its
  experimental status (ADR 0012) does not.

## Decision

- **Project graph.** `.github/scripts/projects.mjs` reads every tracked `.csproj`. A changed file belongs to the
  closest project folder; the affected projects are that project and everything that references it. Path rules
  (`changes.mjs`) still decide the kind of check; the graph narrows the .NET tests to the affected test projects and
  the native captures to the affected games. A file outside every project, or a path no rule knows, still runs
  everything. Evidence keys include the selection, so a narrower run never proves a wider one.
- **Fork pull requests are untrusted.** Evidence is accepted only from runs of the repository's own code
  (`head_repository_id == repository_id`); fork runs record none. A fork stores the demo packages it builds in a
  folder, passes them to the site job as workflow artifacts and reads them through `DEMOS_OVERLAY`; the shared store
  is never written. Forks get no deployment (already so). No workflow uses `pull_request_target` or self-hosted
  runners.
- **macOS informative on public runners.** With the repository variable `PUBLIC_RUNNERS=true`, daily CI runs the
  headless .NET tests on macOS when the change concerns the engine or backend, as `continue-on-error`: reported,
  never a gate, never evidence. Android and iOS stay manual and monthly.

## Consequences

- A game-only pull request runs one test project and one game's captures instead of six and four; a documentation
  change still builds no demo; an engine change still runs everything.
- Selection depends on `ProjectReference` being complete. A dependency expressed another way (MSBuild properties,
  linked files) is invisible to the graph; such files sit outside project folders or in shared props and fall back
  to "everything". A project that starts depending on a sibling through a new mechanism must update the graph.
- Fork pull requests can pass CI, but their packages are rebuilt after merge (a fork's build is never stored or
  released).
- Turning `PUBLIC_RUNNERS` on is part of the publication procedure; while private it stays off and costs nothing.
