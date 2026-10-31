# Publishing the site

How the public site (`site/`) is built, checked and published: the landing page, one page per demo with the
playable WebAssembly build, and the documentation generated from the repository's Markdown. Decisions:
[ADR 0009](decisions/0009-public-site.md) (the site) and [ADR 0010](decisions/0010-delivery-pipeline.md) (the
pipeline).

**Publishing an update needs no manual step:** open a pull request, get it approved, merge it. The pipeline builds
whatever demo changed, checks everything, and deploys production. Local scripts stay as development tools.

## Pipeline

One workflow, [`.github/workflows/pipeline.yml`](../.github/workflows/pipeline.yml), runs only the checks a change
needs (see [What runs for a change](#what-runs-for-a-change)), in tiers: the fast tier first, so a broken change never
occupies a Windows or macOS runner, then the integration tier. The expensive, experimental platforms (macOS native
rendering, Android, iOS) are a separate workflow, [`platforms.yml`](../.github/workflows/platforms.yml), that never
takes part in a deployment ([Platform workflow](#platform-workflow)).

| Job | Tier | Runs when | What it does |
| --- | --- | --- | --- |
| Plan | fast | every run | tests the change planner, decides the checks from the changed paths, computes each game's key and lists the games whose package is not stored yet |
| .NET tests (Linux) | fast | `dotnet` | `dotnet test MicroEngine.slnx -c Release` without the native tests; skipped tests listed with their reason |
| .NET tests (Windows) | integration | `dotnet`, after Linux passed | the same on Windows |
| .NET tests (macOS) | integration | `macos`, after Linux passed | the same on macOS; never blocks a deployment |
| Native Linux | integration | `native`, after Linux passed | under Xvfb and a PulseAudio null sink: the native keyboard and audio tests, and the 15 scripted scenarios compared with the Linux visual reference |
| Build *Game* | integration | the game's key is not stored | `web/setup.sh` (cached), `web/build.sh <Game>`, then `demos.mjs publish` stores the package |
| Site | integration | every run, after the builds | unit tests, `npm run build` with the stored packages, then the browser suites Plan chose, in Chrome and Firefox |
| Deploy | — | Site passed and no needed check failed | `vercel build` and `vercel deploy --prebuilt`: a preview for pull requests, production for `main` |

A job the change does not need is skipped, and a skipped check never stops a deployment; a failed one always does,
except macOS. The NuGet package folder is cached per system, keyed on the project files.

| Event | Result |
| --- | --- |
| Pull request (this repository) | the checks for the files it changes, then a **preview** deployment linked in a pull request comment |
| Pull request from a fork | checks only; no credentials, no deployment |
| Push to `main` (an approved, merged pull request) | the checks for what the push changed since the previous `main`, then a **production** deployment |
| Manual run (Actions → pipeline → Run workflow) | every check, then a preview of any branch, or production from `main` only |

### What runs for a change

[`.github/scripts/changes.mjs`](../.github/scripts/changes.mjs) maps each changed path to the checks that can catch
a regression in it; its rules are tested (`node --test .github/scripts`). A path no rule knows runs everything, so a
new folder is never skipped by accident. Manual runs and a missing comparison base also run everything.

| Change | .NET Linux, Windows | .NET macOS | Native Linux | Demo builds | Browser suites | Platform workflow |
| --- | --- | --- | --- | --- | --- | --- |
| Engine core (`src/MicroEngine`) | yes | yes | yes | every game (keys change) | all (packages rebuilt) | — |
| raylib backend (`src/MicroEngine.Raylib`) | yes | yes | yes | every game | all | weekly or manual |
| One game's code (`games/<Name>`) | yes | — | yes | that game | all | — |
| A game's Markdown (`games/**/*.md`) | — | — | — | — | layout | — |
| Engine tests (`tests/MicroEngine.Tests`) | yes | yes | — | — | — | — |
| Backend tests (`tests/MicroEngine.Raylib.Tests`) | yes | yes | yes | — | — | — |
| Game tests, samples | yes | — | — | — | — | — |
| Desktop capture scripts (`web/tests`) | — | — | yes | — | — | — |
| Web host or toolchain (`web/`) | — | — | — | every game | all | — |
| Site (`site/`) | — | — | — | — | all | — |
| Documentation (`docs/`, other Markdown) | — | — | — | — | layout | — |
| Mobile hosts (`platforms/`, `platforms.yml`) | — | — | — | — | — | on the pull request |
| Task records, experiments (`odd/`, `experiments/`) | — | — | — | — | — | — |
| The pipeline, its scripts, build settings | everything | everything | everything | as keys say | all | — |

Browser suites (`site/tests/browser.mjs --suites`): `layout` opens every page at desktop, tablet and phone widths
and checks the diagrams; `play` loads, plays, restarts and exits each demo; `touch` checks the emulated phone and
tablet panels. A rebuilt package is always played, whatever the paths say: demo keys, not paths, decide that.

### Platform workflow

[`.github/workflows/platforms.yml`](../.github/workflows/platforms.yml) is the full tier: checks that need expensive
runners or are not stable yet. It runs every Monday on `main`, on demand (Actions → platforms → Run workflow), and on
pull requests that change the mobile hosts. A failure there is shown and diagnosed in the run's summary but never
blocks the pipeline or a deployment. Its jobs and what each one proves are listed in
[Platforms: what CI proves](guide/platforms.md#what-ci-proves).

### Demo packages are addressed by content

Each game in the registry ([`web/games.props`](../web/games.props)) has a **key**: a hash of the Git blobs of
everything its web package is built from (the engine under `src/`, the web host and toolchain files, the game's
folder, its registry entry and `THIRD-PARTY-NOTICES.md`; Markdown files, screenshots and input scripts excluded).
`node site/scripts/demos.mjs key <Game>` prints a key and its input files.

- A change to the engine or the web host changes every key; a change to one game changes only that game's key;
  documentation and site changes change no key, so those pull requests reuse every stored package and build none.
- Packages are stored as assets of the prerelease **`demo-artifacts`** of this private repository, one archive and
  one manifest per game and key (`<Game>-<key>.tar.gz`, `<Game>-<key>.json`). The manifest records the archive's
  SHA-256, every file's SHA-256, the commit that built it and the toolchain versions. Artifacts are never replaced.
- `npm run build` fetches, for each game, the artifact of the key computed from the commit being built, checks the
  archive's SHA-256 and then every extracted file against the manifest, and fails if anything is missing or
  different. The site therefore always ships the packages built from exactly its own commit's inputs.
- A new game needs its registry entry in `web/games.props` and its page data in `site/src/data/games.ts`; the plan
  picks it up and builds it. No workflow changes (a unit test fails if the two lists differ).

### Why `vercel build` and `vercel deploy --prebuilt`

The site is built inside the pipeline by `vercel build` (which runs `npm ci` and `npm run build` with Vercel's
project settings and turns `vercel.json` into Vercel's routing configuration), then the output is uploaded with
`vercel deploy --prebuilt`. Consequences:

- Vercel never reads the repository: it needs no access to it, and no source file is ever uploaded, only
  `.vercel/output`. Git deployments are disabled in `site/vercel.json`.
- A deployment can only exist after every check passed in the same run: missing or altered demo packages fail
  `npm run build` before anything is uploaded, so production can never reference packages that do not exist.
- The demo store is read with the run's own `GITHUB_TOKEN`; no GitHub token is stored in Vercel.

### Previews, production and rollback

- **Previews** are protected by Vercel's Deployment Protection (on by default for preview deployments), so
  unapproved changes are not public.
- **Production** comes only from `main`, so only merged pull requests reach it. To require a person to approve each
  production deployment as well, add required reviewers to the `production` environment (GitHub → Settings →
  Environments). Production deployments run one at a time.
- **Rollback:** Actions → rollback → Run workflow, with the URL or ID of an earlier production deployment
  (Vercel dashboard → Deployments). It switches production back without building anything
  ([`.github/workflows/rollback.yml`](../.github/workflows/rollback.yml)); Vercel's dashboard "Instant Rollback"
  does the same. On the Hobby plan Vercel only allows rolling back to the previous production deployment. To
  publish an older version of the code, revert the change on `main`: its stored packages are reused.

## One-time setup

Nothing below has been run yet. These are the values the pipeline needs.

**GitHub** (repository → Settings):

| Where | Name | Value |
| --- | --- | --- |
| Secrets and variables → Actions → Secrets | `VERCEL_TOKEN` | a Vercel access token (Vercel → Account Settings → Tokens), scoped to the team that owns the project |
| Secrets and variables → Actions → Variables | `VERCEL_ORG_ID` | the team or account ID (`orgId` in `site/.vercel/project.json` after `vercel link`) |
| Secrets and variables → Actions → Variables | `VERCEL_PROJECT_ID` | the project ID (`projectId` in the same file) |
| Secrets and variables → Actions → Variables (optional) | `BROWSER_CHECKS` | `chrome` to skip Firefox in CI if needed; default `chrome,firefox` |
| Environments | `preview`, `production` | created automatically on first use; add required reviewers to `production` if wanted |
| Actions → General → Workflow permissions | | default (read); the workflow asks for `contents: write` only in the jobs that store demo packages |

`GITHUB_TOKEN` is provided by Actions; no personal GitHub token is needed.

**Vercel:**

1. Create the project without connecting a Git repository: in `site/`, run `npx vercel@64.3.1 link` and create a new
   project. Framework Astro; leave Root Directory empty (the CLI runs inside `site/`).
2. Optional: environment variable `SITE_URL` with the canonical address if a custom domain is used (the build
   otherwise uses `VERCEL_PROJECT_PRODUCTION_URL`).
3. Keep Deployment Protection on for preview deployments (Settings → Deployment Protection).
4. Copy `orgId` and `projectId` from `site/.vercel/project.json` into the GitHub variables above. `.vercel/` is
   ignored by Git.

After that, the first push to `main` builds the four packages (one Windows job each; the toolchain is downloaded
once and then cached), stores them, and deploys production.

## What the build checks before anything is published

`check-dist.mjs` (the last step of `npm run build`) fails when any of these is true:

- a demo package, a file listed in a game's `content.txt`, a licence notice, a documentation page, the roadmap, the
  sitemap, the 404 page or the search index is missing;
- a package still carries `.br`/`.gz` copies, or the home page references `/play/` (demos must load on demand);
- a published page links to `github.com/NastMz`, says `git clone`, names the solution file, or claims open source or
  an MIT licence;
- a ` ```mermaid ` block of a public page did not become a diagram;
- an internal file type is published (`.md`, `.cs`, `.csproj`, `.props`, `.targets`, `.pdb`, `.map`, `.sh`);
- a package names a user profile folder (build-machine paths), or the value of `DEMOS_TOKEN` or `GITHUB_TOKEN`
  appears in any file.

The site publishes only the Markdown files listed in `site/src/lib/repo-docs.mjs`; a link from them to any other
repository file is published as plain text.

### Diagrams

Architecture, flow, sequence and relationship diagrams are written as ` ```mermaid ` blocks in the Markdown itself
(GitHub shows them too); file trees, console output and input scripts stay ` ```text `. On the site,
`astro-mermaid` turns each block into a diagram drawn in the browser: mermaid.js (about 200 KB compressed, flowcharts
with the dagre layout) loads only on pages that have one, after the page itself. Colours come from the documentation
theme in `site/src/styles/starlight.css`, so diagrams follow the light/dark switch. Keep a diagram under about 700 px
wide (short labels, `<br/>` with a space on each side so search still sees the words, `accTitle`/`accDescr` for
screen readers): wider ones scroll sideways on phones and shrink on desktop.

### What a package may contain

Packages are public files. `web/build.sh` compiles them without build-machine paths (no debug information in the
assemblies, `-ffile-prefix-map` for native code) and adds the third-party notices (`THIRD-PARTY-NOTICES.txt` and
`licenses/`). Emscripten still writes the location of one .NET runtime file into `dotnet.native.*.js` as a comment,
so the toolchain folder must be outside any user profile: the pipeline keeps it in the runner's temporary folder.

## Local tools

The WEB-1 scripts stay for development and diagnosis; publishing never requires them. Node.js 22.23.4 or later.

```bash
web/setup.sh && web/build.sh NeonRift                       # build one package locally
cd site && npm ci
DEMOS_DIR=../dist/web npm run build                          # site with local packages (not checked against keys)
SITE_DEMOS=none npm run build                                # documentation only
DEMOS_STORE=dir:../.demo-store node scripts/demos.mjs publish NeonRift --from ../dist/web   # a local store
DEMOS_STORE=dir:../.demo-store npm run build                 # the pipeline's verified path, offline
node scripts/demos.mjs plan                                  # keys (and, with DEMOS_STORE, what is stored)
npm run preview                                              # http://localhost:4321/
npm test                                                     # loader, capability rules, keys, store, vercel.json
npm run test:browser                                         # Chrome and Firefox (needs a build with demos)
```

To read the real store from a workstation, set `DEMOS_STORE=github` and `GITHUB_TOKEN` to a token that can read the
repository's contents.

## What Vercel serves

| Path | Behaviour |
| --- | --- |
| `/`, `/demos/<game>/`, `/docs/...` | static HTML; URLs end in `/` (`trailingSlash`), so `/docs` redirects to `/docs/` |
| unknown paths | `404.html` with status 404 |
| `/_astro/*` | fingerprinted CSS, JavaScript and images: cached for a year (`immutable`) |
| `/play/<Game>/_framework/*.<hash>.wasm` or `.js` | fingerprinted runtime and assemblies: cached for a year |
| `/play/<Game>/` (other files) | revalidated on every visit (Vercel's default), `X-Robots-Tag: noindex` |
| `.wasm` | `Content-Type: application/wasm`; compressed by Vercel (Brotli or gzip) like JS and JSON |
| `/sitemap-index.xml`, `/og.png` | sitemap of every page; social card image made from a game screenshot at build time |

`source` patterns in `vercel.json` use Vercel's path syntax, not plain regular expressions (a literal dot is
`\\.`); the tests compile them with Vercel's own routing library, the one `vercel build` uses. Every response also
carries `X-Content-Type-Options: nosniff`, `Referrer-Policy`, `X-Frame-Options: SAMEORIGIN` and a restrictive
`Permissions-Policy`. The demos are embedded from the same origin, so `SAMEORIGIN` allows them.

Limits to know: a deployment holds about 44 MB of files, about 36 MB of them the four demo packages (Neon Rift's
OGG music is most of it). WAV and OGG files are served as they are; Vercel only compresses text and WebAssembly.

## Not verified yet

- The workflows have not run on GitHub (only linted with actionlint and simulated locally): in particular
  `web/setup.sh` on a hosted Windows runner, Firefox WebGL on the Linux runner, and the first `demo-artifacts`
  upload.
- No deployment exists: `vercel build` was run locally with placeholder project settings (no account), and
  `vercel deploy`, previews and rollback need the credentials above.
- After the first deployment, check on the real domain the headers of a `.wasm` file (`content-type`,
  `content-encoding`, `cache-control`).
