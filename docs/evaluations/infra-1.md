# INFRA-1: CI/CD cost, full validation and manual releases

Date: 2026-10-31. Branch `feat/ci-cost` (from `main` b822702, after PLATFORM-2). Task record:
[`odd/tasks/infra-1.md`](../../odd/tasks/infra-1.md). Decision: [ADR 0013](../decisions/0013-ci-cost-and-manual-releases.md).
Operation: [CI, validation and releases](../ci.md).

Evidence words are used strictly. **Measured**: read from the GitHub API for finished runs (job start and end
times, runner labels) and priced at list rates. **Estimated**: computed from measured job durations for a pipeline
that has not run. **Run**: executed and its output read. None of the new workflows has run on GitHub yet
([What was not run](#what-was-not-run)).

Rates (read 2026-10-31, [runner pricing](https://docs.github.com/en/billing/reference/actions-runner-pricing)): Linux
2-core $0.006, Windows 2-core $0.010, macOS $0.062 per minute; GitHub rounds each job up to a whole minute. GitHub
Free includes $12 of compute per month for private repositories
([Actions billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions)). The run timing API
(`/actions/runs/{id}/timing`) returns zero for this account, so billed minutes are computed per job by
`ci-cost.mjs`, counting each job of a re-run attempt once.

## 1. Audit of the pipeline before this change (`main` b822702)

### Workflows and triggers

| Workflow | Triggers | Jobs |
| --- | --- | --- |
| `pipeline.yml` | every pull request; push to `main`; manual (preview or production) | Plan; .NET Linux; .NET Windows; .NET macOS (not blocking); Native Linux; Build *Game* (Windows, per missing key); Site; Deploy |
| `platforms.yml` | Mondays 06:00 on `main`; manual; pull requests changing `platforms/` or itself | macOS (tests, OpenGL probe); Android (emulator); iOS (simulator) |
| `rollback.yml` | manual | Vercel rollback |

### What ran, and when

- **A pull request** ran Plan, the checks of its changed paths (`changes.mjs`: engine → Linux, Windows, macOS,
  native; game → Linux, Windows, native; tests → Linux and Windows at least), a build for every game whose key was
  not stored, **Site on every run** (unit tests and a full build even when no browser suite was needed), and a
  **preview deployment on every run** that passed.
- **A push to `main`** ran the checks for everything the merge changed since the previous `main`: the same checks
  the pull request had just passed. The merge of #13 (run 41218921372) re-ran Plan, Linux, Windows, macOS, native
  and Site: 14 Linux, 3 Windows and 1 macOS minute, $0.176 measured, all of it already proven on the pull request.
  Then it deployed production.
- **WebAssembly builds** started for any game whose content key was missing from `demo-artifacts`: an engine or web
  host change built all four (5–7 Windows minutes each); a merge after its pull request built none.
- **The landing** was rebuilt by every run (Site) and deployed by every run that passed: production on every push
  to `main`, with packages of exactly the commit's keys. **Merging any change published new demos.**
- **Content keys** (`site/scripts/demos.mjs`): per game, a hash of the Git blobs of the engine, web host, toolchain
  files, the game's folder and registry entry; packages stored once per key with per-file SHA-256 manifests. Sound;
  kept unchanged.
- **Required jobs:** none can be required. Branch protection and rulesets answer HTTP 403 ("Upgrade to GitHub Pro or
  make this repository public"). The only gate was Deploy's own condition (Site, Linux, Windows and native passed or
  skipped).
- **Browser tests:** `site/tests/browser.mjs` in Chrome and Firefox under Xvfb, suites `layout` (documentation) or
  `layout,play,touch` (site, web host, rebuilt packages).
- **Dependencies:** Windows, macOS and native waited for Linux; Site waited for the builds; Deploy waited for Site,
  Linux, Windows and native. `platforms.yml` was independent and never blocked anything.

### Where the money went (measured)

27 runs between 2026-10-31 03:29 and 16:22 (every run since the pipeline existed): **$5.17**, about the $5 the owner
observed.

| Workflow | Runs | Linux min | Windows min | macOS min | Cost |
| --- | --- | --- | --- | --- | --- |
| `platforms.yml` (mostly the PLATFORM-2 experiment branch) | 11 | 88 | 0 | 33 | $2.57 |
| `pipeline.yml`, pull requests | 10 | 75 | 109 | 7 | $1.97 |
| `pipeline.yml`, pushes to `main` | 5 | 46 | 15 | 3 | $0.61 |
| `site.yml` (removed earlier) | 1 | 1 | 0 | 0 | $0.01 |
| **Total** | 27 | 210 ($1.26) | 124 ($1.24) | 43 (**$2.67**) | **$5.17** |

| Job | Billed | Cost | Note |
| --- | --- | --- | --- |
| iOS simulator | 22 macOS min | $1.36 | 3 minutes per successful run |
| macOS tests and OpenGL probe | 11 macOS min | $0.68 | 37 s jobs billed as 1 minute each |
| Demo builds (four games) | 85 Windows min | $0.85 | 5–7 min each |
| Android emulator | 88 Linux min | $0.53 | 7–8 min per successful run |
| .NET tests on macOS in `pipeline.yml` | 10 macOS min | $0.62 | informative only, on every engine change |

**Findings.** macOS is 52 % of the spend in 43 minutes: a 39-second informative job costs as much as ten Linux
minutes. Merges repeat their pull request. Engine changes build four packages. The platform workflow on the
experiment branch (push-triggered then) was half the total; its weekly schedule alone would be about $1.43 a month
(4 runs of the measured $0.36).

## 2. What changed

| Requirement | Change |
| --- | --- |
| Daily CI | `ci.yml`: changed paths decide; **evidence** skips a check already passed for identical inputs; Windows only for engine, backend and build changes; macOS, Android, iOS never; demo builds only for the changed game, or one canary for engine-wide changes; deploys only when the published site changes; one **CI result** gate |
| Full validation | `validate.yml`: any ref, a box per part, all checks, report per platform with experimental ones marked |
| Release | `release.yml`: version and ref; preflight (on `main`, `ci` passed); build only missing packages; draft release with hash-checked copies; site and every browser suite on exactly those packages; staged deploy, promote, publish; rollback or discard on failure |
| Landing | production serves only the latest release (`DEMOS_SOURCE=release`, enforced in `demos.mjs`); documentation and site changes deploy without building anything |
| Redundancy | PR concurrency (kept); NuGet, npm and toolchain caches (kept); evidence for .NET, Windows, native and site/browser; no Site job or preview when nothing published changed |
| Protection | CI result; deploy needs every needed check passed or proven; release refuses a ref without a passed `ci` run |
| Platform workflow | monthly and manual, callable per platform, no pull request trigger |
| Measurement | `ci-cost.mjs measure` (every CI result and validate report prints its own cost) and `estimate` |

### Coverage moved, and why it is safe

| Check | Before | After | Risk and mitigation |
| --- | --- | --- | --- |
| .NET on macOS | every engine change, not blocking | validate and monthly | macOS is not supported; the check was informative; macOS regressions are found within a month or on demand |
| .NET on Windows | every .NET change | engine, backend, their tests, build settings | games and their tests are managed code without Windows-specific paths (those are in the engine); validation runs them on Windows, demo builds compile them there |
| Native Linux | engine, game, captures | unchanged | — |
| Demo builds | every missing key | changed game, or canary for shared inputs | another game's web build broken only by an engine change is found by validation or the release build, which refuses to publish |
| Mobile on pull requests | emulator and simulator on `platforms/` changes | advisory in the plan | the owner runs platforms or validate when touching hosts |

## 3. Cost before and after (estimated)

`node .github/scripts/ci-cost.mjs estimate`. Each class is one pull request run plus the run of its merge into
`main` (up to date, so its evidence applies). Durations are medians of measured jobs; three are not measured
(CI result about 15 s, deploy with promote about 100 s, release steps about 45 s) and are rounded to their billed
minutes.

| Change | Before: jobs, L/W/M min | Before cost | After: jobs, L/W/M min | After cost | Difference |
| --- | --- | --- | --- | --- | --- |
| Documentation only | 6, 10/0/0 | $0.060 | 7, 10/0/0 | $0.060 | +0% |
| A game's tests | 10, 14/8/0 | $0.164 | 5, 6/0/0 | $0.036 | −78% |
| A game's code (demo) | 13, 25/14/0 | $0.290 | 9, 17/6/0 | $0.162 | −44% |
| Engine core | 18, 25/32/2 | $0.594 | 10, 17/10/0 | $0.202 | −66% |
| CI infrastructure | 14, 28/8/2 | $0.372 | 10, 19/4/0 | $0.154 | −59% |
| Second push to a PR: docs fix after an engine change passed | 7, 14/4/1 | $0.186 | 4, 9/0/0 | $0.054 | −71% |
| Mobile hosts (`platforms/`) | 9, 18/0/5 | $0.418 | 4, 4/0/0 | $0.024 | −94% |
| Platform workflow, per month | 12, 32/0/20 | $1.432 | 3, 8/0/5 | $0.358 | −75% |
| Full validation (manual, packages stored) | 10, 22/4/6 | $0.544 | 9, 21/4/5 | $0.476 | −13% |
| Release (manual, packages stored) | 0, 0/0/0 | $0.000 | 5, 10/0/0 | $0.060 | new |

**A month, assumed** (not measured): 10 documentation, 8 game-code, 6 engine, 4 game-test and 2 CI pull requests,
the platform schedule, and, after the change, one full validation and one release. Before: about **$9.32** (78 %
of the credit). After: about **$4.45** (37 %). Each extra push to an open pull request costs less after the change
because evidence skips what already passed. Wall-clock time also drops (no repeated tests on `main`, fewer
Windows queues), but billed minutes are the figure that matters for the credit.

## 4. Acceptance

| # | Scenario | How it was checked | Result |
| --- | --- | --- | --- |
| 1 | PR changing only documentation | planner test "documentation only"; evidence test "documentation is not a .NET input" | site and layout only; no .NET, no demo build |
| 2 | PR changing a game's test | planner test "tests of one game" | Linux .NET only |
| 3 | PR changing a demo | planner test "a game change"; demos test "plan, ci policy" | Linux .NET and native; only that game built; played in the browser; preview only |
| 4 | PR changing the engine core | planner test "engine core"; demos test "engine change builds the canary only" | Linux, Windows, native; one canary build |
| 5 | PR changing CI infrastructure | planner test "the daily workflows … run everything"; "manual workflows are linted" | everything, plus actionlint |
| 6 | Merge into `main` without new packages | evidence test "accepted only when unexpired and produced with the same workflows"; cost estimate | checks proven on the PR are skipped; production deploys only if the published site changed |
| 7 | Manual full matrix | `validate.yml` linted; report grouping tested ("platforms come from job names") | not run on GitHub |
| 8 | Manual release | demos test "a draft of the stored packages, published only on request, then served as the latest" | draft, verification, publication, latest |
| 9 | Failure during release validation | demos test "a failed release leaves no draft and the public version unchanged" (missing package, altered archive, altered deployed file) | no draft left, latest unchanged |
| 10 | Reuse of published artifacts | demos tests "sources" and "release"; production refuses non-released sources | released packages reused under their own key, verified by SHA-256 |

Commands run: `node --test .github/scripts/*.test.mjs` (27 pass), `npm test` in `site/` (30 pass), `actionlint` on every
workflow (clean), `demos.mjs plan` read-only against the real store (all four current keys stored, no release),
`ci-cost.mjs measure` on the 27 runs above.

**GitHub runs that are indispensable**, in this order, each once: (1) a pull request of this branch: every check,
evidence uploads, actionlint, the CI result summary and a preview; (2) its merge into `main`: evidence reuse, and
production skipped because no release exists; (3) release `0.1.0` with dry-run, then without: draft, verification,
staged deploy, promote, publication; (4) one documentation pull request and its merge: production from the release
without builds. A validate run (about $0.5) is optional and best done before a release that matters.

## 5. Risks of the new strategy

- **A planner or evidence rule too narrow** skips a needed check. Mitigations: exclusion-based keys, unknown paths
  run everything, `.github` changes invalidate all evidence, tested rules, validate on demand.
- **Evidence trust**: any commit with the same `.github` folder can supply evidence; anyone who can push branches can
  forge it by uploading an artifact with the right name. Acceptable for a private repository with trusted
  collaborators; it would not be for a public one.
- **Flaky tests reused**: a test that passed by luck is not re-run on the merge. Validate re-runs everything.
- **Engine regressions in the WebAssembly build of a non-canary game** surface at validation or release time.
- **macOS drift** is noticed monthly instead of per engine change.
- **First deployment** needs release `0.1.0`; until then production keeps its current deployment.
- **Unexercised paths**: evidence lookup, promote and the rollback after a failed publication have not run on
  GitHub. A failure there is visible (red CI result or release) and leaves production unchanged, except the rollback
  path, which runs only after a failed publication.

## 6. Self-hosted runners

Research only; nothing was installed or registered. The GitHub billing page (read 2026-10-31) lists self-hosted
runners as free and outside the included credit; check it again before adopting them.

| Use | What it would take | Advantages | Risks |
| --- | --- | --- | --- |
| Windows native (tests, demo builds) | the runner as a Windows service under a dedicated local user without admin rights, its own work folder, the pinned toolchain cached on disk | no Windows minutes (about $1.2 a month after this change); warm caches make demo builds faster | jobs run arbitrary code on the owner's PC; the PC must be on; builds compete with development; state leaks between jobs unless ephemeral |
| Linux in Docker or a VM | WSL2 or Hyper-V VM with Docker; one ephemeral container per job (`--ephemeral`); Xvfb and PulseAudio inside | the cheapest jobs are already cheap: little to save | container escape is the boundary; Docker Desktop licensing terms apply to some organisations |
| Android emulator | hardware acceleration: on Windows via the Windows Hypervisor Platform; in WSL2 only with nested virtualization | the emulator job is the slowest Linux job (7–8 min) | the same as above, and hypervisor settings on the host |

Security requirements if adopted: private repository only (already the case); ephemeral or just-in-time runners;
a dedicated low-privilege account or VM, never the owner's session; no secrets on the machine and no deployment
jobs on it (Vercel and release jobs stay on GitHub-hosted runners); a runner group and labels so only named jobs
target it; outbound HTTPS only (the runner needs no inbound port); automatic OS updates; disk cleanup. Apple checks
stay on GitHub's macOS runners.

## 7. Recommendation

Stay on GitHub-hosted runners. After this change the estimated monthly spend is about 37 % of the credit, and the
largest remaining items are Windows demo builds and tests (about $1.2 a month) and the monthly platform run ($0.36):
a self-hosted Windows runner would save around a dollar a month at a real security and maintenance cost. Reconsider
only if the measured monthly spend (GitHub → Settings → Billing) passes about 75 % of the credit for two months after
the new workflows run, and then start with an ephemeral Windows runner for demo builds only. Set an Actions budget
with alerts now ([Measuring cost](../ci.md#measuring-cost)).

## What was not run

- No new workflow has run on GitHub: no push, pull request or manual run was made (owner's instruction). The real
  costs of the new pipeline are therefore estimates until the runs in section 4 happen; `ci-cost.mjs measure`
  will then give measured figures, and this page should be updated with them.
- Vercel: `--skip-domain`, `promote` and the no-argument `rollback` are used as documented on vercel.com (pages read
  2026-10-31); none was executed.
- Budgets: the steps come from GitHub's documentation; whether "Stop usage when budget limit is reached" is offered
  for this account was not checked.
