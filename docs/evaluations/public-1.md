# PUBLIC-1: readiness for a public repository

Internal record. Date: 2026-10-31. Branch `feat/public-1` (from `feat/ci-cost`, INFRA-1). Task record:
[odd/tasks/public-1.md](../../odd/tasks/public-1.md). Decisions: [ADR 0014](../decisions/0014-public-repository-ci.md)
(public CI), [ADR 0015](../decisions/0015-mit-engine-reserved-demos.md) (licensing).

**Outcome (2026-10-31).** The owner chose a new public repository instead of rewriting this one: the old
repository was archived privately, `NastMz/MicroEngine` was recreated public with the history rebuilt from
`71115bc` (the two grafts of section 3), configured as in section 14, and release `v0.1.0` was published. Sections 3
and 15 record the analysis that led there; the hashes they quote belong to the archived history.

**Measured** means read from Git or the GitHub API in this study. **Not verified** names evidence that could not be
obtained here. Nothing in this study changed the repository's visibility, settings, releases or history.

The study ran in two rounds. The first audited the repository and proposed a source-available licence; the owner
then chose the **MIT License for the engine and all rights reserved for the demo games** (ADR 0015). This document
describes the final state; the earlier licensing analysis survives only as the superseded
[proposal](../legal/source-available-proposal.md).

## Answers

| Question | Answer |
| --- | --- |
| Which licence, granting what? | The standard **MIT License** (root `LICENSE`, text unmodified) for the engine, backend, engine tests, tools, CI, web host, site code, docs and the HelloEngine sample: use, modify, fork, redistribute and sell, keeping the notice. The four demo games, their tests and builds are **all rights reserved** (`games/LICENSE`): read, build and run them to learn, reuse ideas; no redistribution, sale or renamed copies. Third-party files keep their licences (CC0 art and audio listed in `CREDITS.md`; libraries in `THIRD-PARTY-NOTICES.md`). |
| Can the existing history be published with reasonable safety? | **Not as is; rebuild it in this repository from `71115bc`** (section 3, simulated). The existing history contains four Mixkit files whose terms forbid making them available, and snapshots where the demos sit next to an MIT grant. The rebuild keeps every tree identical and removes both; GitHub Support must then remove the 14 read-only PR refs. |
| What must be withdrawn or kept private? | The history before `71115bc` (kept in an offline backup bundle); the unused `NUGET_API_KEY` secret. Owner decisions: the personal e-mail address in commit metadata (can be mapped to the GitHub no-reply address while creating the new history), and whether internal records (cost studies, legal drafts, `odd/` logs) go public. |
| How are contributions received? | Under MIT, inbound = outbound, no CLA; the pull request template asks for the origin and licence of the change; maintainers review and merge; fork runs need approval and get no secrets (section 6). |
| Which checks does each change run? | Path rules choose the kind of check, the project graph chooses which test projects and games (section 8). |
| How do public runners stay fast and reliable? | Evidence by content, cancellation of superseded PR runs, Linux first, per-project scope; on public runners every changed demo key is built and played, and macOS runs as an informative job (section 9). |
| How do releases stay selective, safe and reproducible? | Manual `release.yml`: packages copied by content key, hash-checked, the exact files browser-tested, staged, then promoted; unchanged games reuse their approved package (section 10). |
| What must hold before "Make public"? | The checklist in section 14. Real blockers left: the owner's authorisation for the history rebuild and GitHub Support's removal of the PR refs, and the repository settings (section 13). The code of conduct has no contact address by the owner's decision. |

## 1. Audit of what would be exposed

Making a repository public publishes all of it at once: every commit on every ref, tags, releases and their assets,
Actions run history and logs, pull requests and issues. Measured with
`node .github/scripts/public-audit.mjs --legacy reference/pre-rebuild --rebuild 1de4c1c` (final run: exit 0) and the
GitHub API.

| Item | Measured | Verdict |
| --- | --- | --- |
| Commits | 446 on all refs before this branch (2025-11-17 to 2026-10-31); 69 local branches, 36 on origin; tags `legacy-final`, `reference/pre-rebuild`, `demo-artifacts` | Existing history: not publishable as is (section 3) |
| Commit metadata | one personal address (422 commits); GitHub no-reply addresses (15); an AI agent address authors 9 commits and appears in 9 `Co-Authored-By` trailers | **Owner decision.** The rebuild keeps the 43 post-rebuild commits by this address; an optional `--env-filter` in the same rewrite can map it to the GitHub no-reply address |
| Secrets in history | none (tokens, private keys, assigned passwords); the audit's own test fixtures are excluded by design | Pass |
| Credential or machine files ever committed | none (`.env`, keys, keystores, `.user`, `.vercel/`) | Pass |
| Local paths | three Windows scratch paths with a user name, only in old task records (none in any tree from `71115bc` on) | Acceptable; absent after the rebuild |
| Repository secrets | `VERCEL_TOKEN` (deploy, rollback); `NUGET_API_KEY` (2025-11-20, **unused**; no package exists on nuget.org under the old project names) | Delete `NUGET_API_KEY`; scope `VERCEL_TOKEN` to environments |
| Releases | one prerelease `demo-artifacts` (content-addressed demo store) | Becomes public downloads, including unreleased builds; every package ships the licence notices (now including `games/LICENSE` and the engine's MIT text) |
| Actions history | 47 runs (17 of the old implementation, 30 of 2026-10-31); 14 artifacts | Deleted in procedure step 5 (they only reference old hashes) |
| Pull requests and issues | 14 pull requests, no plain issues | Stay (conversations are harmless); their read-only refs need GitHub Support (section 3) |
| Third-party content, current tree | 129 files in three games, each named in its game's `CREDITS.md` (CC0 1.0, checked file by file); libraries and fonts in `THIRD-PARTY-NOTICES.md` | Pass: CC0 allows public redistribution |
| Third-party content, history only | 101 media files no longer in the tree. Notices for the old `examples/*/Assets` (CC0). **No notice:** `assets/audio` (four Mixkit files and two unattributed WAVs), `assets/textures` (15 PNGs, origin not recorded), `docs/engineering/evidence` (captures of the owner's own games) | Mixkit: **real blocker** for publishing that history (section 3). Textures and WAVs: owner to confirm; all removed by the rebuild |
| Internal documents in the tree | ADRs, evaluations (costs), superseded legal drafts, `odd/` logs quoting the owner's requests, `AGENTS.md` | **Owner decision**, not a blocker: no secrets, but they reveal process and cost |
| Names and inspirations | records call Neon Rift "Contra-inspired"; Trail Duel is a creature-collecting RPG with original art | Low risk; keep public wording to genres |

## 2. Security findings

| # | Finding | State |
| --- | --- | --- |
| S1 | Evidence artifacts were accepted from any run; a fork pull request could try to upload fake evidence | **Fixed**: only runs whose head repository is this repository count; forks record none |
| S2 | A fork pull request changing a game could not pass CI (storing packages needs `contents: write`) | **Fixed**: fork builds stay in the run (`DEMOS_OVERLAY`) |
| S3 | `VERCEL_TOKEN` is a repository secret; environments `preview` and `production` have no branch policy | Open: checklist P7 |
| S4 | Unused `NUGET_API_KEY` | Open: checklist P6 |
| S5 | `pull_request` only (never `pull_request_target`); default `GITHUB_TOKEN` read-only and unable to approve pull requests; per-job permissions (`contents: write` only for the store and releases, `pull-requests: write` only for preview comments); no self-hosted runners; forks get no deployment | Verified by reading every workflow and the Actions API |
| S6 | Only `actions/*` and a version-pinned actionlint download are used; `allowed_actions: all` | Recommended: allow GitHub-owned actions only (P9) |
| S7 | Same-repository branches can write the demo store, and releases copy by key | Accepted: only people with write access; builds are not byte-reproducible yet (INFRA-2) |

Rotation if a credential ever leaks: revoke it at the provider (Vercel, nuget.org, GitHub), create a narrower one,
update the environment secret, re-run the workflow, check the provider's audit log. Rewriting history never
un-leaks a value.

## 3. History: publish the existing repository or a new one

| Period | Implementation | Root `LICENSE` | Notes |
| --- | --- | --- | --- |
| 2025-11-17 to 2025-11-19 | previous | none | |
| 2025-11-19 `b4bfc49` to 2026-10-28 (`reference/pre-rebuild`, 250 commits; 11 more on old branches) | previous | MIT | treated as published under MIT while the repository was public; still MIT, consistent with the new engine licence. **Contains the Mixkit files** (24 commits on `main`'s ancestry, 2025-11-19 to 2026-10-12) |
| 2026-10-29 `1de4c1c` to 2026-10-30 `616a039` (68 commits) | current | MIT, no `games/LICENSE` | all four demos first appear here (Meadow Run `9c6f14d`, Courtyard Key `9e231da`, Trail Duel `08bd6a8`, Neon Rift `e57d28a`); never public: the repository was private and the site's only MIT claim was removed before the first deployment |
| 2026-10-30 `71115bc` onward (57 commits on this branch, 46 on `origin/main`) | current | all-rights-reserved notice, then MIT + `games/LICENSE` (this branch) | no Mixkit file, no demo next to an MIT grant |

**Why MIT does not remove the need for a clean history.**

1. **Mixkit.** Mixkit's terms (read 2026-10-31 at mixkit.co/llm-info; the licence page itself did not render) allow
   use in projects but forbid making items available to third parties and reselling them unaltered. A public
   history makes the four files downloadable. Whether they were already exposed while the old repository was public
   cannot be verified; continuing to distribute them is the risk.
2. **Demos beside an MIT grant.** In the 68 commits the demos sit in a tree whose only licence is MIT. Anyone could
   argue those snapshots of the games are MIT, which is exactly what `games/LICENSE` now prevents. The snapshots
   were never distributed, so the argument is weak, but publishing them creates it.
3. **Unattributed assets** (`assets/textures`, two WAVs) and old process records ride along with the old history.

### Rebuilding the history in this repository (recommended)

The owner prefers to keep `NastMz/MicroEngine`. A rebuild was simulated in an isolated bare clone of a full
mirror (`git clone --mirror`, which also fetches GitHub's `refs/pull/*`); nothing was pushed.

**Pull requests.** All 14 are merged, authored by the owner, and belong to the greenfield rebuild (#1 merged the
rebuild over `reference/pre-rebuild`); none is open, none came from a fork. GitHub keeps one read-only
`refs/pull/N/head` per PR (no `merge` refs remain).

| PR | Branch | Merged (UTC) | Commits | Kept in the new history | Commits with demos beside MIT |
| --- | --- | --- | --- | --- | --- |
| #1 | feat/engine-rebuild | 2026-10-30 13:48 | 10 | 0 | 7 |
| #2 | feat/engine-rebuild | 2026-10-30 14:51 | 2 | 0 | 2 |
| #3 | feat/run-and-gun | 2026-10-30 16:16 | 6 | 0 | 6 |
| #4 | feat/visual-excellence | 2026-10-30 19:30 | 16 | 0 | 16 |
| #5 | feat/evolution-lines | 2026-10-30 20:14 | 3 | 0 | 3 |
| #6 | fix/prize-money | 2026-10-30 20:18 | 1 | 0 | 1 |
| #7 | feat/achievements | 2026-10-30 21:24 | 3 | 0 | 3 |
| #8 | feat/web-support | 2026-10-31 02:05 | 13 | 0 | 13 |
| #9 | feat/web-site | 2026-10-31 04:28 | 17 | 11 (from `71115bc`) | 6 |
| #10 | fix/pipeline-deploy | 2026-10-31 04:44 | 1 | 1 | 0 |
| #11 | claude/mermaid-microengine-diagrams-zufo8v | 2026-10-31 05:43 | 1 | 1 | 0 |
| #12 | feat/platform-linux-macos | 2026-10-31 12:37 | 8 | 8 | 0 |
| #13 | feat/platform-ci | 2026-10-31 16:16 | 13 | 13 | 0 |
| #14 | feat/ci-cost | 2026-10-31 17:36 | 7 | 7 | 0 |

**References today** (mirror of 2026-10-31): 27 branches, 2 tags (`demo-artifacts` on `87be497`,
`reference/pre-rebuild`), 14 `refs/pull/N/head`, 1 release (`demo-artifacts`). **Every one of the 43 refs reaches
the 24 commits that contain the Mixkit files**; `main`, five branches, the `demo-artifacts` tag and PRs #1-#14 also
reach the commits with demos beside an MIT grant (up to 65).

**The cut.** `71115bc` alone is not enough: the merge of PR #9 (`1d4facd`) has `87be497` as first parent, which
leads back into the old history. Two grafts are needed: `71115bc` becomes a root, and `1d4facd` keeps only its second
parent (`9bc055e`). `87be497` is an ancestor of `71115bc`, so no change is lost, and trees are never altered. No other
commit after `71115bc` reaches older history (all 46 are on its ancestry path).

**Simulation results** (`git replace --graft` twice, then `git filter-branch -- main feat/public-1`; `git filter-repo`
is not installed and is not needed for two grafts):

| Check | Result |
| --- | --- |
| Tree of `main` | identical (`34f8212`) |
| Tree of `feat/public-1` | identical (`073b3a3`) |
| Commits | 60: the root, the 46 commits after it, the 13 PUBLIC-1 commits; one root (the licence change of 2026-10-30) |
| Commits with Mixkit / with MIT but no `games/LICENSE` | 0 / 0 |
| Pack size | 42.8 MiB → 14.1 MiB |
| Authors, dates, messages | unchanged; only hashes change. The 6 GitHub-signed merge commits lose their signature |
| Tests on the rewritten history | scripts 41 passed; site 31 passed; .NET 282 passed (headless); site build passed; actionlint clean |
| `public-audit.mjs` on the rewritten history | exit 0; no media only in history; 6 commits with MIT, all with `games/LICENSE` |

**What can be fixed and what cannot.**

| Kind | Items | How |
| --- | --- | --- |
| Git refs we can replace by force push | `main`, `feat/public-1` (rewritten); tag `demo-artifacts` (moved to the new `main`; its release follows the tag name) | `git push --force` |
| Git refs we can delete | 26 other branches, tag `reference/pre-rebuild` (all old or merged) | `git push --delete` |
| GitHub data we can delete | 47 Actions runs and their logs and artifacts, 17 caches, 10 deployments (they reference old hashes; none holds Mixkit content) | REST API |
| GitHub refs we cannot modify | `refs/pull/1-14/head`: read-only, each reaches the full old history including the Mixkit files | **GitHub Support**: "dereference or delete" the PRs, garbage collection, purge cached views (their documented procedure for rewritten history) |
| Old content still reachable after the force push | through the PR refs until Support acts; by old hash in cached views (hashes appear in PR pages, the activity log, run pages); in local clones | Support's garbage collection; re-clone every local copy |
| Real publication risk | the four Mixkit files and the demos-beside-MIT snapshots, reachable only through the PR refs and cached hashes | resolved by Support before the visibility switch |
| Harmless historical references | commit messages ("Merge pull request #9"), old hashes quoted in docs, release manifests' `commit` fields, the PR conversations themselves | no action; docs that quote old hashes are updated after the rebuild |

**The one dependency.** GitHub states it removes PR refs when every other ref is clean and no fork holds the data
(forks: 0), but also that it acts on "sensitive data". Whether it accepts restrictively licensed third-party media is
the only open point. The repository stays private until Support confirms; if Support declines, the remaining
choices are to accept that the four Mixkit files stay fetchable through `refs/pull/*` once public, or a new repository.

## 4. Licence choice

The owner rejected custom, source-available and restrictive licences. The engine uses the **standard MIT text**
(verified: the root `LICENSE` equals the original MIT file of `b4bfc49` except the year range of the copyright line).
MIT already requires every copy or substantial portion to keep the copyright notice and the licence, which is the
protection the owner asked for against presenting the code as one's own; nothing is added to it. MIT grants no
trademark rights, so "MicroEngine" as a name is not licensed by it (no clause was added; it is simply outside MIT).

## 5. Licence structure

| File | Covers | Says |
| --- | --- | --- |
| `LICENSE` | the repository except the demo games and third-party files | standard MIT |
| `games/LICENSE` | `games/<Name>/`, `tests/<Name>.Tests/`, the games' builds and browser packages, except the files in each `CREDITS.md` | all rights reserved; read, build, run and learn; reuse ideas; no redistribution, sale or renamed copies; engine code and third-party files unaffected |
| `games/<Name>/CREDITS.md` | the third-party image and audio files of that game | source, author, CC0 1.0 |
| `games/<Name>/README.md` | — | a "License" section pointing to `games/LICENSE` and `CREDITS.md` |
| `THIRD-PARTY-NOTICES.md` | libraries, fonts, assets | their licences; points to both MicroEngine notices |
| `README.md` | — | the licence scope table and what game makers must ship |
| Browser packages | each demo | `LICENSE.txt` (the game's notice), `licenses/microengine-LICENSE.txt` (MIT), third-party notices; both licence files are inputs of every demo key and required by `demos.mjs` and `check-dist` |
| Site | licensing page, demo pages, footer, landing | MIT engine, reserved demos; `check-dist` fails if a demo page lacks "all rights reserved" |

`samples/` needs no file of its own: the root MIT covers it and the README table and the HelloEngine page say so.
Consistency checked: no file claims the demos are MIT; no file claims the engine is proprietary (the superseded
legal drafts carry a "not in force" banner); the third-party files are excluded wherever the reserved notice applies.

## 6. Contributions

- **Inbound = outbound under MIT** (GitHub Terms D.6), stated in `CONTRIBUTING.md`; no CLA. A CLA would only matter
  if the owner planned to relicense contributions under other terms; under MIT everyone, the owner included, already
  has every right the licence gives, so no concrete need exists.
- **Origin and licence** are confirmed by the pull request template's checkboxes (lighter than a DCO sign-off; the
  DCO can be added later with a GitHub App if provenance becomes a concern).
- **Demo games** accept fixes; the contribution is MIT, the game as a whole stays reserved. New third-party assets
  need a licence that allows public redistribution and a `CREDITS.md` entry.
- **Control** stays with the maintainers: reviews, the required **CI result** check, approval before fork runs.
- `SECURITY.md` (private vulnerability reporting), issue forms and the code of conduct are in place; the conduct
  contact is still a placeholder (blocker).

## 7. Public CI/CD architecture

| Flow | Trigger | Runs | Publishes |
| --- | --- | --- | --- |
| `ci.yml` | pull requests, pushes to `main` | Plan → .NET Linux → (.NET Windows, native Linux, macOS informative) in parallel; demo builds → site and browsers; preview or production site deploy; **CI result** gate | site only (production serves released demos) |
| `validate.yml` | manual, any ref | every selected check, all scopes | nothing |
| `platforms.yml` | manual, from validate, monthly | macOS diagnosis, Android emulator, iOS simulator (experimental) | nothing |
| `release.yml` | manual (version, ref) | copy packages by key, verify hashes, browser-test the exact files, stage, promote | a demo release and the site |
| `rollback.yml` | manual | promote the previous production deployment | site |

Added in PUBLIC-1: the project graph (`projects.mjs`), fork-safe evidence and demo builds, macOS and "every changed
demo key" behind the repository variable `PUBLIC_RUNNERS`, `public-audit.mjs`. Kept from INFRA-1: evidence by
content, demo keys, manual releases, one required check. Not adopted: INFRA-2's local-first gate.

## 8. Validation matrix by change

| Change | .NET Linux | Windows | macOS (public) | Native Linux | Demo builds (public) | Browser | Deploy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Documentation | — | — | — | — | — | layout | preview / production |
| Licence files | — | — | — | — | every game (keys change) | all suites | preview / production |
| One game's code | that game's tests | — | — | that game's captures | that game | all | preview |
| One game's tests | those tests | — | — | — | — | — | — |
| Engine core | all | all | all, informative | all games | all four | all | preview |
| raylib backend | backend and game tests | same | same | all games | all four | all | preview |
| Engine tests | engine tests | yes | yes | — | — | — | — |
| Web host or toolchain | — | — | — | — | all four | all | preview / production |
| Site | — | — | — | — | — | all | preview / production |
| Workflows, scripts, build settings, unknown paths | everything | everything | everything | everything | changed keys | all | preview / production |
| Mobile hosts | advisory (run platforms.yml) | — | — | — | — | — | — |
| Fork pull request | as above | as above | as above | as above | built, kept in the run | as above | **none** |

While private (`PUBLIC_RUNNERS` unset) macOS is advisory and engine-wide changes build one canary demo.

## 9. Multi-platform policy

| Platform | Daily CI | Full validation | Status |
| --- | --- | --- | --- |
| Linux headless .NET | when needed, first | always | gate |
| Windows headless .NET | engine, backend, build changes | always | gate; the supported platform |
| Linux native (Xvfb, Mesa, PulseAudio) | when needed, per game | always | gate; software rendering, not GPU coverage |
| macOS headless .NET | informative on public runners | on request | experimental, never a gate |
| macOS native, Android emulator, iOS simulator | never | platforms.yml | experimental; emulators are not devices |
| Chrome and Firefox | when the site or a package changes | always | gate |
| Physical hardware, real GPUs, gamepads | none | none | not covered |

Speed: superseded PR runs are cancelled; Linux runs first; checks already passed for the same content are skipped;
NuGet, toolchain and npm caches are keyed on their inputs; larger runners (billed even when public) are not used.
Recommended at publication: a weekly `validate.yml` schedule on `main`.

## 10. Releases and deployment

Merging never creates a release; production serves the latest published demo release. A release copies each game's
package for its current key, checks the archive and every file against the manifest, browser-tests those files,
stages and promotes. A change to one game rebuilds only that key; the other packages are reused byte for byte. A
change to the engine, the web host, the toolchain or a licence file changes every key. Rollback: `rollback.yml` for
the site, "set as latest" on the previous release for the demos. Cycles: demo releases `v<x.y.z>`, the site from
`main`, and engine releases (`engine-v<x.y.z>`, NuGet) when packages exist. Builds are not byte-reproducible yet.

## 11. Changes in this branch

| Commit | Change |
| --- | --- |
| `d0ce2b9` | evidence only from this repository's runs; fork demo builds through `DEMOS_OVERLAY` |
| `9ec307c` | project-graph planner, `dotnet-test.sh`, per-game captures, macOS informative job |
| `7b06d00`, `61f24f3` | `public-audit.mjs`; its own fixtures excluded |
| `a1ddacf` | `SECURITY.md`, issue forms, PR template, code of conduct fixes |
| `8a21583` | source-available proposal (superseded by ADR 0015) |
| `3e04bac` | ADR 0014, public section of `docs/ci.md` |
| `767878d` | first PUBLIC-1 report |
| `c472e23` | MIT `LICENSE`, `games/LICENSE`, notices in every package, README scope table, ADR 0015 |
| `3804c47` | site and documentation for MIT engine and reserved demos; repository links; demo notice check |
| `e668acd` | contributions under MIT, no CLA |
| `39fff89` | every changed demo key built on public runners |

## 12. Tests run

| Test | Result |
| --- | --- |
| `node --test .github/scripts/` (planner, graph, evidence, cost, audit) | 41 passed |
| `npm test --prefix site` (store, overlay, releases, links, check-dist) | 31 passed |
| `actionlint` on every workflow | clean |
| `SITE_DEMOS=none npm run build` (site build and `check-dist`) | passed: 29 documentation pages, demo pages carry the notice, links to `LICENSE` and `games/LICENSE` point to GitHub |
| Browser layout suite, Chrome and Firefox, every page at three widths | all checks passed (one overflow on the landing at 320 px found and fixed) |
| `dotnet-test.sh` with two selected projects | 140 passed |
| `public-audit.mjs` on this repository | exit 0; findings in section 1 |

Not run: hosted workflows; the play and touch browser suites (they need the demo packages, which are rebuilt by CI
because the licence files changed every key); a real fork pull request (possible only once public).

## 13. Blockers and warnings

**Real blockers (must be resolved by the owner before "Make public"):**

1. Authorise the history rebuild in this repository (section 15) and file the GitHub Support request for the 14 PR
   refs; publication waits for Support's confirmation.
2. (Resolved) The code of conduct has no contact address by the owner's decision; maintainers moderate.
3. Apply the repository settings P6-P11 (secrets, environments, fork approval, actions, ruleset, security
   features).

**Acceptable warnings:** the personal e-mail address in metadata (mappable during the rebuild); internal records in the
tree (owner's choice); macOS only informative; Linux native is software rendering; builds not byte-reproducible;
the site's code is MIT while game screenshots on it are reserved (stated in the footer and demo pages).

## 14. Publication checklist

| # | Item | Pass condition |
| --- | --- | --- |
| P1 | No secrets | `node .github/scripts/public-audit.mjs` exits 0 on the history being published |
| P2 | History | every ref except `refs/pull/*` has one root (the rebuilt `71115bc`); GitHub Support confirmed the PR refs are dereferenced and garbage collected |
| P3 | Third-party files | no Mixkit or unattributed media in the published history (`public-audit.mjs` lists no "no: review" folder) |
| P4 | Licences | `LICENSE` is the standard MIT text; `games/LICENSE` exists; every demo README has its "License" section; `check-dist` passes |
| P5 | Contributions | `CONTRIBUTING.md` states MIT inbound = outbound; PR template present |
| P6 | Unused credentials | `gh secret list` shows no `NUGET_API_KEY`; key revoked on nuget.org |
| P7 | Deploy secrets | `VERCEL_TOKEN` only as an environment secret; `production` limited to `main` |
| P8 | Fork approval | Actions → General → "Require approval for all external contributors" |
| P9 | Actions | GitHub-owned actions only; default token read-only |
| P10 | `main` protected | ruleset: pull request required, required check **CI result**, no force push, no deletion |
| P11 | Security features | private vulnerability reporting, secret scanning and push protection on |
| P12 | Public runners | variable `PUBLIC_RUNNERS=true`; weekly `validate.yml` schedule if wanted |
| P13 | Internal records | owner's list of files to keep or remove applied (task logs, cost studies, legal drafts) |
| P14 | Contacts | no placeholder left in `CODE_OF_CONDUCT.md` (no contact by decision); `SECURITY.md` reachable |
| P15 | Collaborators | only intended people have write or admin access (today: the owner) |
| P16 | Validation | `validate.yml` passes on the commit being published |
| P17 | Site | after publication, the production site is redeployed from the public `main` (its links point to the public repository) |

## 15. Publication procedure (history rebuilt in this repository) and rollback

Each step names its check. Nothing here has been run; it needs the owner's go-ahead.

**0. Preconditions.** No open pull requests (today: 0), no forks (today: 0), no Actions run in progress, no one else
pushing. Variables: `REPO=https://github.com/NastMz/MicroEngine.git`, `PUBLIC1=<path of the feat/public-1 checkout>`.

**1. Backup (keep it outside GitHub, it holds the old history).**

```bash
git clone --mirror "$REPO" MicroEngine-archive.git
git -C MicroEngine-archive.git bundle create ../MicroEngine-archive-2026-10-31.bundle --all
git bundle verify MicroEngine-archive-2026-10-31.bundle
```

**2. Rebuild in a fresh bare clone.**

```bash
git clone --bare "$REPO" rewrite.git && cd rewrite.git
git fetch "$PUBLIC1" feat/public-1:feat/public-1
for b in $(git for-each-ref --format='%(refname:short)' refs/heads | grep -vx -e main -e feat/public-1); do git branch -D "$b"; done
git tag -d $(git tag)
git replace --graft 71115bc
git replace --graft 1d4facd "$(git rev-parse 1d4facd^2)"
FILTER_BRANCH_SQUELCH_WARNING=1 git filter-branch -- main feat/public-1
git replace -d $(git replace -l); rm -rf refs/original
git reflog expire --expire=now --all && git gc --prune=now
```

**3. Verify before pushing** (all must hold; the simulation met them): the trees of `main` and `feat/public-1` equal
the originals (`git rev-parse main^{tree}` against the mirror); `git rev-list --max-parents=0 --all` prints one
commit; `node .github/scripts/public-audit.mjs` in a checkout of `feat/public-1` exits 0 and lists no media only in
history; `git rev-list --objects --all | grep -ci mixkit` prints 0.

**4. Replace and delete the Git refs.**

```bash
git push --force "$REPO" main feat/public-1
git push --force "$REPO" main:refs/tags/demo-artifacts
git push "$REPO" --delete refs/tags/reference/pre-rebuild $(git ls-remote --heads "$REPO" | awk '{print $2}' | grep -vx -e refs/heads/main -e refs/heads/feat/public-1)
```

Check: a new `git clone --mirror` shows only `main`, `feat/public-1`, tag `demo-artifacts` and `refs/pull/*`; every
ref except `refs/pull/*` passes step 3.

**5. Delete GitHub data that references the old hashes** (owner's token): the 47 workflow runs
(`gh api -X DELETE repos/NastMz/MicroEngine/actions/runs/<id>`), the 17 caches (`gh cache delete --all`), the 10
deployments (mark inactive, then delete), and the `NUGET_API_KEY` secret.

**6. GitHub Support ticket** (Support portal → "Removing sensitive data"): owner and repository; "14 pull requests
(#1-#14) reference history removed by a rewrite; please dereference or delete them, run garbage collection and purge
cached views"; first changed commit `71115bc` (and the merge `1d4facd`); reason: third-party audio files whose licence
forbids redistribution, and demo snapshots under a superseded licence; state that no forks exist and all other refs
are clean. Wait for confirmation; then a fresh mirror shows no `refs/pull/*` reaching the old history, and old hashes
return 404.

**7. Local copies.** Re-clone or reset every checkout (`C:\Proyectos\MicroEngine`, the `infra-2` and `public-1`
worktrees): their 70 local branches still hold the old history and must never be pushed. Delete or keep them only in
the backup.

**8. Merge PUBLIC-1 on the clean history.** Open a pull request `feat/public-1` → `main` (its ref is clean), let CI
run (it compares with a base that exists; the first push of the rebuild has an unknown base and runs everything),
merge. Then update docs that quote pre-rebuild hashes to call them archived.

**9. Publish.** Apply checklist P6-P15, run `validate.yml` (P16), set `PUBLIC_RUNNERS=true`, run `release.yml`
`0.1.0`, switch the visibility, apply the ruleset (P10) and test a fork pull request.

**Rollback.** Until step 4 nothing has changed on GitHub. After step 4 and before step 6, the old history can be
restored from the bundle (`git push --force --mirror` from `MicroEngine-archive.git`, except `refs/pull/*`, which
never changed). After Support's garbage collection the old objects exist only in the bundle and local clones. After
the visibility switch, making the repository private again does not recall forks or clones made meanwhile; the
operational rollback is then: private again, rotate `VERCEL_TOKEN`, `PUBLIC_RUNNERS` off, `rollback.yml` for the site.

**When a new repository would still be needed:** only if GitHub Support declines to remove the PR refs and the owner
does not accept that the four Mixkit files stay fetchable through them.

## Public documentation for DOCS-2

| Audience | Content | Where |
| --- | --- | --- |
| Game makers | introduction, getting started (now: build from the repository), manual, examples, contracts, licensing | the site |
| Contributors | `CONTRIBUTING.md`, `docs/architecture.md`, `SECURITY.md`, code of conduct | the repository; architecture also on the site |
| Maintainers | ADRs, `docs/ci.md`, `docs/deployment.md`, evaluations, `odd/` | the repository |

Links from site pages to repository files now point to GitHub, so DOCS-2 can keep one source per topic and link to
code instead of copying it; it should still remove maintainer notes (ADR numbers, internal task names) from the
user pages.
