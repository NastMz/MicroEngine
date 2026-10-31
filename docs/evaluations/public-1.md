# PUBLIC-1: readiness for a public repository

Internal record. Date: 2026-10-31. Branch `feat/public-1` (from `feat/ci-cost`, INFRA-1). Task record:
[odd/tasks/public-1.md](../../odd/tasks/public-1.md). Decisions: [ADR 0014](../decisions/0014-public-repository-ci.md)
(public CI), [ADR 0015](../decisions/0015-mit-engine-reserved-demos.md) (licensing).

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
| Can the existing history be published with reasonable safety? | **No; publish a new repository whose history starts at `71115bc`** (option B, section 3). MIT removes the engine contradiction, but the existing history still contains four Mixkit files whose terms forbid making them available, and 68 snapshots where the demos sit next to an MIT grant with no `games/LICENSE`. |
| What must be withdrawn or kept private? | The old history before `71115bc` (stays in the private archive); the unused `NUGET_API_KEY` secret. Owner decisions: the personal e-mail address in commit metadata (can be mapped to the GitHub no-reply address while creating the new history), and whether internal records (cost studies, legal drafts, `odd/` logs) go public. |
| How are contributions received? | Under MIT, inbound = outbound, no CLA; the pull request template asks for the origin and licence of the change; maintainers review and merge; fork runs need approval and get no secrets (section 6). |
| Which checks does each change run? | Path rules choose the kind of check, the project graph chooses which test projects and games (section 8). |
| How do public runners stay fast and reliable? | Evidence by content, cancellation of superseded PR runs, Linux first, per-project scope; on public runners every changed demo key is built and played, and macOS runs as an informative job (section 9). |
| How do releases stay selective, safe and reproducible? | Manual `release.yml`: packages copied by content key, hash-checked, the exact files browser-tested, staged, then promoted; unchanged games reuse their approved package (section 10). |
| What must hold before "Make public"? | The checklist in section 14. Real blockers left: the owner's authorisation for option B, the code of conduct contact, and the repository settings (section 13). |

## 1. Audit of what would be exposed

Making a repository public publishes all of it at once: every commit on every ref, tags, releases and their assets,
Actions run history and logs, pull requests and issues. Measured with
`node .github/scripts/public-audit.mjs --legacy reference/pre-rebuild --rebuild 1de4c1c` (final run: exit 0) and the
GitHub API.

| Item | Measured | Verdict |
| --- | --- | --- |
| Commits | 446 on all refs before this branch (2025-11-17 to 2026-10-31); 69 local branches, 36 on origin; tags `legacy-final`, `reference/pre-rebuild`, `demo-artifacts` | Existing history: not publishable as is (section 3) |
| Commit metadata | one personal address (422 commits); GitHub no-reply addresses (15); an AI agent address authors 9 commits and appears in 9 `Co-Authored-By` trailers | **Owner decision.** In option B the personal address can be replaced by the GitHub no-reply address at no extra cost |
| Secrets in history | none (tokens, private keys, assigned passwords); the audit's own test fixtures are excluded by design | Pass |
| Credential or machine files ever committed | none (`.env`, keys, keystores, `.user`, `.vercel/`) | Pass |
| Local paths | three Windows scratch paths with a user name, only in old task records (none in any tree from `71115bc` on) | Acceptable; absent in option B |
| Repository secrets | `VERCEL_TOKEN` (deploy, rollback); `NUGET_API_KEY` (2025-11-20, **unused**; no package exists on nuget.org under the old project names) | Delete `NUGET_API_KEY`; scope `VERCEL_TOKEN` to environments |
| Releases | one prerelease `demo-artifacts` (content-addressed demo store) | Becomes public downloads, including unreleased builds; every package ships the licence notices (now including `games/LICENSE` and the engine's MIT text) |
| Actions history | 47 runs (17 of the old implementation, 30 of 2026-10-31); 14 artifacts | Stays with the private archive in option B |
| Pull requests and issues | 14 pull requests, no plain issues | Stay with the private archive in option B |
| Third-party content, current tree | 129 files in three games, each named in its game's `CREDITS.md` (CC0 1.0, checked file by file); libraries and fonts in `THIRD-PARTY-NOTICES.md` | Pass: CC0 allows public redistribution |
| Third-party content, history only | 101 media files no longer in the tree. Notices for the old `examples/*/Assets` (CC0). **No notice:** `assets/audio` (four Mixkit files and two unattributed WAVs), `assets/textures` (15 PNGs, origin not recorded), `docs/engineering/evidence` (captures of the owner's own games) | Mixkit: **real blocker** for publishing that history (section 3). Textures and WAVs: owner to confirm; moot in option B |
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

| | A. Publish the existing repository | B. New public repository, history from `71115bc` |
| --- | --- | --- |
| Mixkit files | public | absent (kept only in the private archive) |
| Demo snapshots beside MIT | public (68 commits) | absent |
| Engine provenance | complete | from 2026-10-30 22:18; earlier history in the private archive, referenced from the README |
| Old implementation (MIT) | public | private (it may be published separately later; it is MIT) |
| Personal e-mail in metadata | public | can be mapped to the no-reply address while creating the history |
| Pull requests, issues, stars, Actions history | kept | start empty; the 14 PRs stay in the archive |
| Demo store, evidence | kept | recreated: the first CI run builds the four packages (once per key) |
| Work | settings only | one graft and push, secrets/variables/environments copied, the old repository renamed and archived |
| Making A safe instead | needs a history rewrite of this repository (removing four files from 24 commits and the 68-commit problem), which changes every hash, breaks tags and PR links and is not recommended | — |

**Recommendation: B.** The cost is a shorter public history (57 commits instead of 446) and a fresh issue tracker;
the gain is a history with no third-party file under restrictive terms and no ambiguous demo licence. This study
created no repository and rewrote nothing; the procedure is in section 15 and needs the owner's authorisation.

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

1. Authorise option B (or accept A's risks in writing): create the new repository from `71115bc`.
2. Set the code of conduct contact (`CONDUCT-CONTACT-TO-BE-SET` in `CODE_OF_CONDUCT.md`).
3. Apply the repository settings P6-P11 (secrets, environments, fork approval, actions, ruleset, security
   features).

**Acceptable warnings:** the personal e-mail address in metadata (mappable in option B); internal records in the
tree (owner's choice); macOS only informative; Linux native is software rendering; builds not byte-reproducible;
the site's code is MIT while game screenshots on it are reserved (stated in the footer and demo pages).

## 14. Publication checklist

| # | Item | Pass condition |
| --- | --- | --- |
| P1 | No secrets | `node .github/scripts/public-audit.mjs` exits 0 on the history being published |
| P2 | History | the public repository's history starts at `71115bc` (option B), or the owner accepted option A in writing |
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
| P14 | Contacts | code of conduct contact set; `SECURITY.md` reachable |
| P15 | Collaborators | only intended people have write or admin access (today: the owner) |
| P16 | Validation | `validate.yml` passes on the commit being published |
| P17 | Site | after publication, the production site is redeployed from the public `main` (its links point to the public repository) |

## 15. Publication procedure (option B) and rollback

1. Merge `feat/ci-cost` and `feat/public-1` into `main` through pull requests in the private repository; run
   `validate.yml` on `main` (P16). Production does not change yet (no demo release is created).
2. Rename the private repository (for example `MicroEngine-archive`) so the public one can take the name
   `NastMz/MicroEngine` that the site and docs link to; GitHub redirects the old name until it is reused.
3. Create an empty **private** repository `NastMz/MicroEngine`.
4. In a fresh clone, make `71115bc` a root commit (`git replace --graft 71115bc` then `git filter-repo --force`, or
   an equivalent), optionally map the personal address to the no-reply address (`--mailmap`), keep only `main`, no
   tags, and push. Run `public-audit.mjs` on that clone (P1, P3).
5. Copy secrets (only `VERCEL_TOKEN`, as environment secrets), variables (`VERCEL_ORG_ID`, `VERCEL_PROJECT_ID`,
   `PUBLIC_RUNNERS=true`) and environments; apply P8-P11; check P15.
6. Run `ci.yml` (a push to `main`): it rebuilds the four demo packages into the new store. Run `release.yml` with
   `0.1.0` to publish them; production then serves them.
7. Settings → Change visibility → Public. Apply the ruleset (P10) if it needed a public repository. Open a test pull
   request from another account: it must run only after approval, without secrets, deploy nothing and record no
   evidence.
8. Add to the README of the archive: "Archived history; the public repository is NastMz/MicroEngine". Archive it.

**Rollback.** Before step 7 everything is private and reversible (delete the new repository, rename the archive
back). After step 7, switching back to private does not recall forks, clones or caches made meanwhile; the
operational rollback is to make it private again, rotate `VERCEL_TOKEN`, set `PUBLIC_RUNNERS` off and roll the site
back with `rollback.yml`. That is why every blocker is resolved before step 7.

## Public documentation for DOCS-2

| Audience | Content | Where |
| --- | --- | --- |
| Game makers | introduction, getting started (now: build from the repository), manual, examples, contracts, licensing | the site |
| Contributors | `CONTRIBUTING.md`, `docs/architecture.md`, `SECURITY.md`, code of conduct | the repository; architecture also on the site |
| Maintainers | ADRs, `docs/ci.md`, `docs/deployment.md`, evaluations, `odd/` | the repository |

Links from site pages to repository files now point to GitHub, so DOCS-2 can keep one source per topic and link to
code instead of copying it; it should still remove maintainer notes (ADR numbers, internal task names) from the
user pages.
