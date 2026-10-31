# PUBLIC-1: readiness for a public repository

Internal document, not published on the site. Date: 2026-10-31. Branch `feat/public-1` (from `feat/ci-cost`,
INFRA-1). Task record: [odd/tasks/public-1.md](../../odd/tasks/public-1.md). Decision for the CI changes:
[ADR 0014](../decisions/0014-public-repository-ci.md). Licensing: [source-available proposal](../legal/source-available-proposal.md).

**Measured** means read from Git or the GitHub API in this study. **Not verified** names evidence that could not be
obtained here. Nothing in this study changed the repository's visibility, licence, releases, settings or history.

## Answers

| Question | Answer |
| --- | --- |
| Which licence, granting what? | One **MicroEngine License** (source-available, not open source) for source and binaries: read, build and modify to make games, evaluate or contribute; make and sell games with no royalties or revenue limit; ship the runtime inside games; no redistribution of the engine or SDK, no competing engine. Demos all rights reserved; third-party assets under their own licences. Text to be drafted by counsel (section 5). |
| Can the existing history be published with reasonable safety? | **Not yet.** Two items need a decision first: four Mixkit audio files of the old implementation (2025-11 to 2026-10) whose licence likely forbids redistributing them as files, and counsel's view on the 68 commits of the current implementation whose tree carried the MIT `LICENSE`. No secret was found. Recommended path: publish the history after counsel clears the MIT question **and** the owner chooses how to handle the Mixkit files; otherwise publish a fresh-history repository (section 3). |
| What must be withdrawn or kept private? | Rotate nothing (no leaked credential), but delete the unused `NUGET_API_KEY` secret; scope `VERCEL_TOKEN` to environments. Decide on the Mixkit files and the unattributed old textures in history. Internal records (ADRs, evaluations, `odd/` logs, cost studies) can be published but expose the business and process; the owner decides (section 1). |
| How are contributions received without losing legal control? | Issues now; outside pull requests merged only after a CLA granting relicensing rights is signed (CLA Assistant). Already stated in `CONTRIBUTING.md` and the PR template (section 6). |
| Which checks does each change run? | Path rules pick the kind of check; the project graph picks which test projects and games (section 8). |
| How do public runners stay fast? | Evidence by content, cancellation of superseded PR runs, Linux first, per-project scope, one canary demo for engine-wide changes, macOS only as an informative parallel job (section 9). |
| How do releases stay selective, safe and reproducible? | Manual `release.yml`: packages copied by content key from the store, hash-checked, the exact files browser-tested, staged, then promoted; unchanged games reuse their approved package (section 10). |
| What must hold before "Make public"? | The checklist in section 14; today **four blockers** remain (section 13). |

## 1. Audit of what would be exposed

Making the repository public publishes **all of it at once**: every commit on every ref, tags, releases and their
assets, Actions run history and logs, pull requests and issues. Measured with
`node .github/scripts/public-audit.mjs --legacy reference/pre-rebuild --rebuild 1de4c1c` and the GitHub API.

| Item | Measured | Verdict |
| --- | --- | --- |
| Commits | 446 on all refs (2025-11-17 to 2026-10-31); 69 local branches, 36 on origin; tags `legacy-final`, `reference/pre-rebuild`, `demo-artifacts` | Publishable once the items below are decided. Local-only branches are not published unless pushed |
| Commit metadata | one personal address in 422 commits (author and committer); GitHub no-reply addresses in 15; 9 commits authored by an AI agent address, 9 `Co-Authored-By` agent trailers | **Owner decision**: the personal address becomes public (a mailmap does not hide it); agent attribution becomes visible |
| Secrets in history | none: no token, key or assigned password pattern in any added line | Publishable |
| Credential or machine files ever committed | none (`.env`, keys, keystores, `.user`, `.vercel/`) | Publishable |
| Local paths | three `C:/Users/<user>/AppData/Local/Temp/...` scratch paths in old task records | Publishable (reveals a Windows user name only); owner may prefer to keep them |
| Repository secrets | `VERCEL_TOKEN` (used by deploy and rollback), `NUGET_API_KEY` (created 2025-11-20, **unused**: no workflow publishes NuGet, no package exists on nuget.org under the old project names) | Values are never exposed by visibility. **Delete `NUGET_API_KEY`** and revoke it on nuget.org; move `VERCEL_TOKEN` to the `production`/`preview` environments (section 2) |
| Repository variables | Vercel organisation and project IDs | Identifiers, not credentials; they become visible to readers of workflow logs only through the CLI output. Acceptable |
| Releases | one prerelease `demo-artifacts` (the content-addressed demo store) | Its assets become **public downloads**, including packages of unreleased commits. Acceptable for demos; they carry the notices |
| Actions history | 47 runs: 17 of the old implementation (2025-11 to 2026-01, "Build", mostly failed), 30 of 2026-10-31; 14 artifacts (evidence records, Android logs, browser screenshots) | Logs become public. Secrets are masked by GitHub. Optional: delete the 17 old runs |
| Pull requests and issues | 14 pull requests, no plain issues; titles and bodies describe internal milestones | Publishable |
| Current tree | engine, games, tests, site, web host, platforms, docs (51 files before this branch), `odd/` task logs (10), `experiments/` (4), `AGENTS.md` | Publishable; see internal documents below |
| Internal documents | ADRs, evaluations (costs, INFRA studies, this report), `docs/legal/*` drafts, `odd/` logs quoting the owner's requests | **Owner decision.** They contain no secret, but reveal costs, legal strategy and unfinished licence drafts. Recommended: keep ADRs and engineering docs (they help contributors); move `docs/legal/` drafts and cost studies out before publication, or accept they become public |
| Third-party content, current tree | 129 content files in three games; each one is named in its game's `CREDITS.md` (checked file by file); `THIRD-PARTY-NOTICES.md` lists raylib, Raylib-cs, .NET, Emscripten and the site's dependencies and fonts | Publishable |
| Third-party content, history only | 101 media files no longer in the tree. Notices exist for `examples/*/Assets` (CC0, OpenGameArt) and the games. **No notice** for `assets/audio` (four Mixkit files: one MP3, three WAV, 2025-11-19 to 2026-10-12), `assets/textures` (15 PNGs of the old implementation, origin not recorded) and `docs/engineering/evidence` (31 captures of the owner's own games) | **Needs review**: Mixkit's free licence permits use in projects but, as generally published, not redistribution of the files themselves; a public history redistributes them. Textures: owner to confirm they are self-made. Captures: publishable |
| Names and inspirations | Neon Rift is described in records as "Contra-inspired"; Trail Duel is a creature-collecting RPG with original art | Low risk; keep public wording to genres, not other products' names |

## 2. Security findings

| # | Finding | Severity | State |
| --- | --- | --- | --- |
| S1 | Evidence artifacts were accepted from any run; on a public repository a fork pull request (untrusted workflows and tests) could try to upload a fake `evidence-<check>-<key>` | High once public | **Fixed** (T1): only runs whose head repository is this repository count; forks record none |
| S2 | A fork pull request changing a game could not pass CI (the demo store needs `contents: write`) and would have pushed contributors towards asking for write access | Medium | **Fixed** (T1): fork builds stay in the run (`DEMOS_OVERLAY`) |
| S3 | `VERCEL_TOKEN` is a repository secret and the `preview`/`production` environments have no branch policy | Medium | Open: move the token to environment secrets; restrict `production` to `main` (checklist P7) |
| S4 | Unused `NUGET_API_KEY` | Low | Open: delete and revoke (P6) |
| S5 | Workflows use `pull_request` (never `pull_request_target`), `GITHUB_TOKEN` defaults to read, each job asks only for what it needs (`contents: write` only for the store and releases, `pull-requests: write` only for preview comments), no self-hosted runners, fork PRs get no deployment | — | Verified by reading every workflow and `gh api .../actions/permissions/workflow` (default read, cannot approve PRs) |
| S6 | Third-party actions: only `actions/*` (GitHub) and actionlint downloaded by pinned version; `allowed_actions: all`, SHA pinning not required | Low | Recommended at publication: allow only GitHub-owned actions (P9) |
| S7 | A same-repository branch can still write the demo store; a release copies packages by key | Low (only the owner has write access) | Accepted; a future collaborator with write access is trusted with the store. Builds are not byte-reproducible yet (INFRA-2), so a release cannot re-verify a package by rebuilding it |

Rotation procedure if a credential ever leaks: revoke it at the provider first (Vercel → Account → Tokens; nuget.org
→ API keys; GitHub → Developer settings), create a new one with the narrowest scope, update the environment secret,
re-run the affected workflow, then check the provider's audit log for use between leak and revocation. Rewriting
history does not un-leak a value: clones and caches keep it.

## 3. MIT history

Measured with `public-audit.mjs` and `git log --follow LICENSE`:

| Period | Implementation | LICENSE | Published? |
| --- | --- | --- | --- |
| 2025-11-17 to 2025-11-19 (68 commits before `b4bfc49`) | previous (tracer) | none | the owner states the repository was public in that era; **not verifiable from the API** |
| 2025-11-19 `b4bfc49` to 2026-10-28 (`reference/pre-rebuild`, 250 MIT commits; 11 more on old side branches) | previous | **MIT**, "Copyright (c) 2025 Kevin Martinez" | treated as published under MIT (current `LICENSE` says so); copies obtained then keep MIT rights |
| 2026-10-29 `1de4c1c` to 2026-10-30 `616a039` (**68 commits**, all pushed, all on `origin/main`) | **current** (rebuild) | the MIT file was still in the tree | **No.** The repository was private (AGENTS.md, the distribution proposal of 2026-10-30), no package was published, no release existed, and the site's only MIT claim (`1e42a63`) was removed 17 minutes later (`71937be`), before the first deployment (2026-10-31 04:43 UTC, after `71115bc`). Not verified: Vercel's own deployment list |
| 2026-10-30 `71115bc` onwards | current | all-rights-reserved notice | demos as compiled WebAssembly only |

**What rights may exist.**

- *Previous implementation:* anyone who obtained a copy while it was public under MIT may keep using, modifying and
  redistributing **that** code. Changing `LICENSE` revokes nothing for those copies; it only stops new grants.
- *Current implementation, 68 commits:* a licence is generally granted to the person who receives the work under it.
  If nobody outside received those snapshots, the MIT text was an offer nobody accepted. Publishing the history now
  would, however, put snapshots that literally contain an MIT `LICENSE` in front of everyone. A reader could argue
  that downloading commit `616a039` gives MIT rights to that snapshot. The counter-arguments (the notice was
  replaced before publication; the current `LICENSE` and README state the terms; the owner never distributed under
  MIT) are for counsel to weigh, not for this study to assume.
- *Code that never was MIT:* the current implementation is a rewrite (ADR 0001); it does not inherit MIT from the
  old code merely because the old code was MIT. Where rebuilt code reuses old code, that part may already be MIT in
  third parties' hands. Counsel may want a statement of which files, if any, were carried over.

**Options.**

| Option | Ambiguity | Cost |
| --- | --- | --- |
| A. Publish the whole history as is | highest: 68 MIT-tree snapshots of the current engine become public, plus the Mixkit files | none technically; legal review needed first |
| B. Publish the whole history, with a `LICENSE`-history note in README and release notes stating which versions were MIT and that the rebuild never was distributed | lower; still relies on counsel | small |
| C. New public repository with fresh history (one initial commit of the current tree, or history from `71115bc` onward via a graft), keep this repository private as the archive | lowest: no MIT-tree snapshot of the current engine and no Mixkit file is ever published | loses public provenance and blame before the cut; PR and issue numbers restart; two remotes to keep in sync until the private one is archived; evidence artifacts and the demo store must be recreated in the new repository (the store holds content-keyed packages, so the next CI run rebuilds them) |
| D. Rewrite this repository's history to drop the MIT file and Mixkit files | low, but alters every hash, breaks tags, PR links and evidence keys, and does not revoke anything | high; **not recommended**, and not done |

**Recommendation:** ask counsel the two questions in the proposal (section 6, items 3 and 4). If counsel is
comfortable with B and the owner either confirms the Mixkit licence allows it or prefers C for that reason, use B;
otherwise use C with history from `71115bc` onward. In both cases the private repository is kept untouched.

## 4. Licence comparison

See the [proposal](../legal/source-available-proposal.md), sections 1-2. In short: anything that forbids
redistribution or competing forks is source-available, not OSI open source or free software. The **Defold
License** is the closest precedent for a game engine (free, royalty-free games, no selling the engine) but allows
free redistribution; **PolyForm Shield** has the strongest noncompete ("compete even when free of charge") but
still allows non-competing redistribution; BUSL and FSL convert to open source, which the owner does not want;
Unreal's model matches the "fork only to contribute" access but carries royalties and accounts.

## 5. Licensing proposal

One MicroEngine License for source and binaries (rights table in the proposal, section 3); demos all rights reserved
(optionally a sample-code grant); third-party assets under their own licences. It is compatible with every
dependency (Zlib, MIT, CC0). GitHub's Terms D.5 (viewing and forking on GitHub) always apply to a public repository
and must be acknowledged by the licence. **Draft for counsel; not approved.**

## 6. Contributions

Done in this branch: `SECURITY.md` (private vulnerability reporting), issue forms (bug, idea; blank issues off,
security link), a pull request template with origin and agreement checkboxes, `CONTRIBUTING.md` "Contribution
status" (what GitHub's Terms allow, issues welcome, outside code merged only with a signed agreement), README
licence section, code of conduct fixes (removed references to files that do not exist; the reporting contact is a
marked placeholder the owner must set).

Needed before accepting outside code: the licence must allow cloning, building and modifying to prepare a
contribution; a CLA with copyright and patent licence and the right to relicense (Apache ICLA style), signed through
CLA Assistant; review by the maintainer, Conventional Commits, branches `feat/`, `fix/`, `docs/`, tests for behaviour
and captures for visual changes (already in `CONTRIBUTING.md`). The repository has a single author, so the owner can
still license everything freely today.

## 7. Public CI/CD architecture

| Flow | Trigger | Runs | Publishes |
| --- | --- | --- | --- |
| `ci.yml` | pull requests, pushes to `main` | Plan → .NET Linux → (.NET Windows, native Linux, macOS informative) in parallel; demo builds → site and browsers; preview or production site deploy; **CI result** gate | site only (production serves released demos) |
| `validate.yml` | manual, any ref | every selected check, all scopes | nothing |
| `platforms.yml` | manual, from validate, monthly | macOS diagnosis, Android emulator, iOS simulator (experimental) | nothing |
| `release.yml` | manual (version, ref) | copy packages by key, verify hashes, browser-test the exact files, stage, promote | a demo release and the site |
| `rollback.yml` | manual | promote the previous production deployment | site |

New in PUBLIC-1: the project graph (`projects.mjs`), fork-safe evidence and demo builds, macOS on public runners
behind `PUBLIC_RUNNERS`, and `public-audit.mjs`. Kept from INFRA-1: evidence by content, demo keys, manual releases,
the single required check. Not adopted: INFRA-2's local-first gate.

## 8. Validation matrix by change

| Change | .NET Linux | Windows | macOS (public) | Native Linux | Demo builds | Browser | Deploy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Documentation (`docs/`, Markdown) | — | — | — | — | — | layout | preview / production |
| One game's code | that game's tests | — | — | that game's captures | that game | all suites | preview |
| One game's tests | those tests | — | — | — | — | — | — |
| A game's screenshots or Markdown | — | — | — | — | — | layout | preview / production |
| Engine core (`src/MicroEngine`) | all | all | all (informative) | all games | canary game | all | preview |
| raylib backend | backend and game tests | same | same | all games | canary game | all | preview |
| Engine tests | engine tests | yes | yes | — | — | — | — |
| Web host or toolchain | — | — | — | — | canary game | all | preview / production |
| Site | — | — | — | — | — | all | preview / production |
| Daily workflows, scripts, build settings, unknown paths | everything | everything | everything | everything | as keys say | all | preview / production |
| Mobile hosts (`platforms/`) | advisory (run platforms.yml) | — | — | — | — | — | — |
| Fork pull request | as above | as above | as above | as above | built, kept in the run | as above | **none** |

Verified locally: planner and graph tests (40), the graph on this repository (a game change selects
`tests/MeadowRun.Tests` and captures `MeadowRun` only; an engine change selects all), and `dotnet-test.sh` with two
selected projects (140 tests passed in 14 s).

## 9. Multi-platform execution policy

| Platform | Daily CI | Full validation | Status |
| --- | --- | --- | --- |
| Linux headless .NET | when needed, first | always | gate |
| Windows headless .NET | engine, backend, build changes | always | gate; the supported platform |
| Linux native (Xvfb, Mesa, PulseAudio): real rendering, keyboard, audio | when needed, per game | always | gate; software rendering, not GPU coverage |
| macOS headless .NET | informative, engine/backend changes, public runners only | on request | experimental, never a gate |
| macOS native, OpenGL probe | — | platforms.yml | experimental |
| Android emulator, iOS simulator | never | platforms.yml (manual, monthly) | experimental; emulators are not devices |
| Browsers (Chrome, Firefox under Xvfb) | when the site or a package changes | always | gate |
| Physical hardware, real GPUs, gamepads | none | none | not covered by CI |

Speed on free runners: superseded PR runs are cancelled; Linux runs first so a broken change never occupies
Windows or macOS; checks that already passed for the same content are skipped; scope keeps a game PR to one test
project and one game; NuGet, toolchain and npm caches are keyed on their inputs. Larger runners are billed even for
public repositories and are not used. Scheduled regression detection: `platforms.yml` monthly; recommended at
publication: a weekly `validate.yml` schedule on `main` (free on public runners).

## 10. Releases and deployment

- **Separation stays:** merging never creates a release or publishes a demo; production serves the latest published
  demo release only.
- **Selective reuse:** each game's package is addressed by a key over its inputs (engine, web host, toolchain, the
  game). A release copies the stored package for every game's current key, checks the archive hash against its
  manifest and every file's hash, then browser-tests those exact files. When only one game changed, the other three
  keys are unchanged and their approved packages are reused byte for byte; nothing is rebuilt.
- **Invalidation:** a change in `src/`, the web host or the toolchain changes every key, so all four packages are
  rebuilt (the store builds missing keys before the release).
- **Rollback:** the site through `rollback.yml`; the demos by marking the previous release latest.
- **Versioning:** keep three cycles: demo releases `v<x.y.z>` (today), the site deployed from `main` with the latest
  demo release, and a future SDK version (`sdk-v<x.y.z>`, NuGet) when the licence exists. An engine change does not
  force a demo release; a demo release records the engine commit each package was built from (`release.json`).
- **Open limits:** builds are not byte-reproducible (INFRA-2: ~90 bytes differ in managed `.wasm` files), so
  integrity rests on hashes of the stored package, not on independent rebuilds.

## 11. Technical changes in this branch

| Commit | Change |
| --- | --- |
| `d0ce2b9` | evidence only from this repository's runs; fork PRs build demos into a per-run overlay (`DEMOS_OVERLAY`) and record no evidence |
| `9ec307c` | project-graph planner (`projects.mjs`, `planScope`), `dotnet-test.sh`, per-game native captures, macOS informative job, selection in evidence keys |
| `7b06d00` | `public-audit.mjs`: reproducible, read-only audit of history, licences, addresses and media |
| `a1ddacf` | `SECURITY.md`, issue forms, PR template, contribution status, code of conduct and README fixes |
| `8a21583` | licensing proposal for counsel |
| `3e04bac` | ADR 0014, public section of `docs/ci.md` |

## 12. Tests run

| Test | Result |
| --- | --- |
| `node --test .github/scripts/` (planner, graph, evidence, cost, audit) | 40 passed |
| `npm test --prefix site` (demo store, overlay, releases, site) | 31 passed |
| `actionlint` on every workflow | clean |
| `dotnet-test.sh` with MeadowRun.Tests and MicroEngine.Tests | 140 passed, 1 skipped (Linux-only) |
| `public-audit.mjs` on this repository | runs in ~18 s; findings in section 1 |
| RED before GREEN | observed for the fork evidence rule, the overlay store and `planScope`; the graph module's tests were written alongside it |

Not run: any hosted workflow (no new private runner minutes were spent; the design is exercised by the unit tests
and `actionlint`); a real fork pull request (only possible once public); the site build with a real overlay.

## 13. Pending risks and blockers

**Blockers before "Make public":**

1. Counsel's answer on the 68 MIT-tree commits, or the choice of option C.
2. The Mixkit files in history: confirm their licence allows a public repository, or use option C.
3. The code of conduct contact address (placeholder `CONDUCT-CONTACT-TO-BE-SET`).
4. The public site's licensing page says the source is private ("Source code: Not distributed"): it must change
   in the same release that makes the repository public (and `check-dist` must then allow linking to the
   repository, which it forbids today).

**Risks accepted or open:** the licence text does not exist yet, so for some time the public code is "all rights
reserved" (readable, not usable); internal documents become public unless moved; the personal e-mail address in
history becomes public; enforcement of a source-available licence relies on the owner noticing misuse; macOS
results are informative only; builds are not byte-reproducible.

## 14. Publication checklist

Each item has an objective pass condition. All must pass.

| # | Item | Pass condition |
| --- | --- | --- |
| P1 | No secrets in history | `node .github/scripts/public-audit.mjs` exits 0 on the final `main` |
| P2 | MIT history decided | written answer from counsel, or option C chosen and executed |
| P3 | Third-party media in history decided | Mixkit and `assets/textures` cleared in writing, or option C |
| P4 | Licence in place | `LICENSE` contains the approved licence text (or the owner accepts publishing under the all-rights-reserved notice) and README states "source-available, not open source" |
| P5 | Contribution path | CLA published and CLA Assistant installed, or `CONTRIBUTING.md` still says outside code is not merged |
| P6 | Unused credentials removed | `gh secret list` shows no `NUGET_API_KEY`; key revoked on nuget.org |
| P7 | Deploy secrets scoped | `VERCEL_TOKEN` exists only as an environment secret; `production` has a branch policy allowing `main` only |
| P8 | Fork workflows approved by hand | Actions → General → "Require approval for all external contributors" |
| P9 | Actions restricted | allowed actions: GitHub-owned only (or SHA-pinned list); default token read-only (already) |
| P10 | `main` protected | ruleset: pull request required, required check **CI result**, no force push, no deletion |
| P11 | Security features | private vulnerability reporting on; secret scanning and push protection on |
| P12 | Public runners on | variable `PUBLIC_RUNNERS=true`; a weekly `validate.yml` schedule added if wanted |
| P13 | Internal documents decided | owner's list of files to move out (or keep) applied on `main` |
| P14 | Site consistent | licensing page and `check-dist` updated in the same change; site build passes |
| P15 | Contacts set | code of conduct contact filled; `SECURITY.md` reachable |
| P16 | Collaborators | only intended people have write or admin access (today: the owner) |
| P17 | Last private validation | `validate.yml` on the final `main` passes |

## 15. Publication and operational rollback procedure

**Publication (owner, after P1-P17):**

1. Merge the branches that carry the work (`feat/ci-cost`, `feat/public-1`) through pull requests.
2. Re-run `public-audit.mjs`; run `validate.yml` on `main`.
3. Apply settings P6-P12 (they work on a private repository too, except rulesets on GitHub Free).
4. If option C: create the new repository, push the chosen history, copy secrets, variables and environments,
   run `ci.yml` once to refill the demo store, then archive the old repository privately.
5. Settings → General → Danger zone → Change visibility → Public.
6. Immediately: enable the ruleset (P10), private vulnerability reporting and push protection (P11); open a test
   pull request from a fork account and check that it runs without secrets, deploys nothing and records no evidence.
7. Deploy the site change of P14 through a release or a `main` push.

**Rollback (operational, not legal):** switching back to private is possible but does not undo the exposure: forks
made meanwhile stay public and detached, clones and caches remain, stars and watchers are lost. So the real
rollback is limited to: make the repository private again; revoke and rotate `VERCEL_TOKEN` if any workflow
misbehaved; set `PUBLIC_RUNNERS` off; revert the site page. Anything published must be treated as permanently
public, which is why every blocker above is resolved before step 5, not after.

## Public documentation plan (preparation for DOCS-2)

The site publishes 29 pages from `docs/`, the credits and the notices. Proposed organisation, without rewriting:

| Audience | Content | Where |
| --- | --- | --- |
| Engine users and game developers | introduction, getting started, manual, examples, contracts, limitations, licensing | the site (already); DOCS-2 removes ADR numbers, class-internal paths and maintainer notes from these pages |
| Contributors | `CONTRIBUTING.md`, `docs/architecture.md`, `SECURITY.md`, code of conduct | the repository; architecture also on the site |
| Maintainers | ADRs, `docs/ci.md`, `docs/deployment.md`, evaluations, `odd/` | the repository only (or a private repository, P13) |

Rules for DOCS-2: one source per topic (the site reads the repository's files; never copy), user pages link to
contributor pages and not the reverse, and public pages keep passing `check-dist` (no repository links until P14).
