# Publishing the site

How the public site (`site/`) is built, checked and published: the landing page, one page per demo with the
playable WebAssembly build, and the documentation generated from the repository's Markdown. Decisions:
[ADR 0009](decisions/0009-public-site.md) (the site), [ADR 0010](decisions/0010-delivery-pipeline.md) (the
pipeline) and [ADR 0013](decisions/0013-ci-cost-and-manual-releases.md) (manual releases).

**Publishing is a decision, not a side effect of merging.** Merged documentation and site changes reach production
on their own, with the demos of the latest release; new demo packages reach it only through the release workflow.
How CI, full validation and releases work, and what they cost: [CI, validation and releases](ci.md).

## Pipeline

| Event | Site | Demos |
| --- | --- | --- |
| Pull request that changes the site or a demo | protected **preview** after its checks (none for forks) | the pull request's packages where built, else the released ones |
| Push to `main` that changes the published site | **production**, after every needed check passed | the latest release |
| Push to `main` that only changes code | nothing deployed | nothing published |
| Release workflow (manual) | **production**, staged, verified file by file, then promoted | the new release, published as the latest |
| SDK release, once nuget.org serves the documented version | **production** of `main`'s head, if its ci passed | the latest release |

**Production never presents an SDK version or feature nobody can install.** When the documentation of `main`
presents an SDK version nuget.org does not serve yet (between merging a release pull request and publishing it), or
documents SDK changes that are not released yet (the release notes' "Next release" section has content), every
production deployment keeps the current one, with a warning, and a demo release is refused; `sdk-release.yml` deploys
`main` once the packages are public ([Releasing, Documentation and releases](releasing.md#documentation-and-releases)).
Previews are not held.

The site deployed by `ci.yml`, `release.yml` and `sdk-release.yml` is built in [`deploy-site.yml`](../.github/workflows/deploy-site.yml).
Production refuses anything but released packages (`DEMOS_SOURCE=release`), and it is always staged with
`--skip-domain` and then made public with `vercel promote`, which also re-enables domain assignment after a
rollback.

### Demo packages are addressed by content

Each game in the registry (`games/<Id>/<Id>.csproj`) has a **key**: a hash of the Git blobs of
everything its web package is built from (the engine and the `microengine` tool with its browser host under `src/`,
the web toolchain files, the game's folder (its project holds its identity), `THIRD-PARTY-NOTICES.md`, `LICENSE` and `games/LICENSE`; Markdown files, screenshots and input scripts excluded).
`node site/scripts/demos.mjs key <Game>` prints a key and its input files.

- A change to the engine or the web host changes every key; a change to one game changes only that game's key;
  documentation and site changes change no key, so those pull requests reuse every stored package and build none.
- Integration packages are stored as assets of the prerelease **`demo-artifacts`** of this repository (public downloads once the repository is public), one
  archive and one manifest per game and key (`<Game>-<key>.tar.gz`, `<Game>-<key>.json`). The manifest records the
  archive's SHA-256, every file's SHA-256, the commit that built it and the toolchain versions. Artifacts are never
  replaced. A release copies the approved ones into release `v<version>` ([Releases](ci.md#releases)).
- `npm run build` fetches each game's package from the source `DEMOS_SOURCE` names: `keys` (the package of the
  key computed from the commit being built; the default for local work), `candidates` (that package when stored,
  else the released one; CI checks and previews) or `release[:<tag>]` (production). It checks the archive's
  SHA-256 and then every extracted file against the manifest, and fails if anything is missing or different.
  `/play/demos.json` records where each package came from.
- A new game needs its project (`games/<Id>/<Id>.csproj`, with its identity) and its page data in `site/src/data/games.ts`; the plan
  picks it up and builds it; production shows it after the next release. No workflow changes (a unit test fails if
  the two lists differ).
- Native downloads use the same store under `<Game>-<target>-<key>` ([packaging](../packaging/README.md),
  [ADR 0018](decisions/0018-native-demo-distribution.md)). A site built from a release (`release`, `release:<tag>`,
  and `candidates` with its latest release) writes that release's downloads into `/play/demos.json`; the demo pages,
  the demos index and the platform summary read them, so a button exists only for a released package. Versions and
  targets on the pages come from `Directory.Build.props`, the games' project files and `packaging/targets.json`.

### Why `vercel build` and `vercel deploy --prebuilt`

The site is built inside the pipeline by `vercel build` (which runs `npm ci` and `npm run build` with Vercel's
project settings and turns `vercel.json` into Vercel's routing configuration), then the output is uploaded with
`vercel deploy --prebuilt`. Consequences:

- Vercel never reads the repository: it needs no access to it, and no source file is ever uploaded, only
  `.vercel/output`. Git deployments are disabled in `site/vercel.json`.
- A deployment can only exist after every needed check passed (in the same run, or for identical inputs in an
  earlier one): missing or altered demo packages fail `npm run build` before anything is uploaded, so production
  can never reference packages that do not exist.
- The demo store is read with the run's own `GITHUB_TOKEN`; no GitHub token is stored in Vercel.

### Previews, production and rollback

- **Previews** are protected by Vercel's Deployment Protection (on by default for preview deployments), so
  unapproved changes are not public.
- **Production** comes only from `main` (and from the release workflow, which also deploys `main`'s site), so only
  merged pull requests reach it. To require a person to approve each
  production deployment as well, add required reviewers to the `production` environment (GitHub → Settings →
  Environments). Production deployments run one at a time.
- **Rollback:** Actions → rollback → Run workflow, with the URL or ID of an earlier production deployment
  (Vercel dashboard → Deployments). It switches production back without building anything
  ([`.github/workflows/rollback.yml`](../.github/workflows/rollback.yml)); Vercel's dashboard "Instant Rollback"
  does the same. On the Hobby plan Vercel only allows rolling back to the previous production deployment. Demo
  releases roll back separately ([Rolling back](ci.md#rolling-back)).

## One-time setup

Done for this repository in October 2026 (production deployments ran from the previous pipeline); kept for a new
project or a rebuild. These are the values the workflows need.

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

After that, the first pull request builds any missing package (one Windows job each; the toolchain is downloaded
once and then cached) and stores it; run the release workflow once ([First release](ci.md#first-release)), since
production deployments from `main` are skipped until a demo release exists.

## What the build checks before anything is published

`check-dist.mjs` (the last step of `npm run build`) fails when any of these is true:

- a demo package, a file listed in a game's `content.txt`, a licence notice, a documentation page, the roadmap, the
  sitemap, the 404 page or the search index is missing;
- a package still carries `.br`/`.gz` copies, or the home page references `/play/` (demos must load on demand);
- a demo page does not say its game is all rights reserved (the engine is MIT, the demos are not: ADR 0015);
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

- The previous pipeline (`pipeline.yml`, until INFRA-1) ran on GitHub: Windows toolchain setup, `demo-artifacts`
  uploads, Firefox WebGL on Linux and production deployments all worked there.
- The INFRA-1 workflows (`ci.yml`, `checks.yml`, `deploy-site.yml`, `validate.yml`, `release.yml`) have only been
  linted with actionlint and their scripts tested locally: evidence lookup through the artifacts API, production
  staged with `--skip-domain` then `vercel promote`, the rollback after a failed publication, and draft releases
  through the GitHub API have not run yet ([INFRA-1 evaluation](evaluations/infra-1.md#what-was-not-run)).
- Check on the real domain the headers of a `.wasm` file (`content-type`, `content-encoding`, `cache-control`).
