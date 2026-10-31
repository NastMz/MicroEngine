# Publishing the site

How the public site (`site/`) is built and published: the landing page, one page per demo with the playable
WebAssembly build, and the documentation generated from the repository's Markdown. The decisions behind it are
in [ADR 0009](decisions/0009-public-site.md).

## Pipeline

The four steps are separate, so building the site never needs the .NET, emscripten and raylib toolchain.

| Step | What | Where it runs | Output |
| --- | --- | --- | --- |
| 1. Build the demos | `web/setup.sh`, `web/build.sh` | a developer machine or the `web demos` workflow (Windows) | `dist/web/<Game>/` |
| 2. Pack and version | `node site/scripts/demos.mjs pack --version X.Y.Z` | same | `microengine-web-demos-X.Y.Z.tar.gz` and its SHA-256 |
| 3. Build the site | `npm run build` in `site/` | Vercel, CI or locally (Node.js only) | `site/dist/` |
| 4. Publish | Vercel deploys `site/dist/` | Vercel | the public site |

Step 3 runs `scripts/demos.mjs fetch`, `astro build` and `scripts/check-dist.mjs`. `fetch` takes the demo packages
from the first of these sources:

| Source | Use |
| --- | --- |
| `DEMOS_DIR=<folder>` | packages built locally with `web/build.sh` (development) |
| `DEMOS_ARCHIVE=<file>` | an archive made by `pack` (checked against `site/demos.lock.json` when the names match) |
| `site/demos.lock.json` | the pinned release asset: downloaded from GitHub and checked against its SHA-256 |
| `SITE_DEMOS=none` | no demos (documentation work); demo pages say the games are not in the build. Refused when `VERCEL_ENV=production` |

Packages are copied without their `.br`/`.gz` copies: Vercel compresses JavaScript, JSON and WebAssembly itself.
`check-dist.mjs` then fails the build if a demo package, a file listed in a game's `content.txt`, a documentation
page, the sitemap, the 404 page or the search index is missing. Nothing generated is committed: `site/dist/`,
`site/public/play/` and `dist/` are ignored.

## Publishing a new demo version

1. Push a tag `demos-v<version>` (for example `demos-v0.1.0`). The `web demos` workflow
   ([`.github/workflows/web-demos.yml`](../.github/workflows/web-demos.yml)) builds the four release packages on
   Windows with the pinned toolchain, packs them and creates the GitHub release with the archive and a
   `demos.lock.json`. A manual run (`workflow_dispatch`) builds and uploads the archive as a workflow artifact
   without publishing a release.
2. Copy the release's `demos.lock.json` to `site/demos.lock.json` and commit it. The site now builds with exactly
   that archive; a different file fails the SHA-256 check.

Without the workflow, the same steps by hand:

```bash
web/build.sh
node site/scripts/demos.mjs pack --version 0.1.0 --lock   # writes dist/web-release/ and site/demos.lock.json
gh release create demos-v0.1.0 dist/web-release/microengine-web-demos-0.1.0.tar.gz
```

If the workflow later runs for a tag whose release already exists, it leaves that release untouched.

### First release: demos-v0.1.0

`demos-v0.1.0` was built and packed by hand (Windows, pinned toolchain) and is the archive the browser tests ran
against; `site/demos.lock.json` already pins it. To publish it, from the repository root on the machine that holds
`dist/web-release/`:

```bash
sha256sum dist/web-release/microengine-web-demos-0.1.0.tar.gz   # must equal sha256 in site/demos.lock.json
gh release create demos-v0.1.0 dist/web-release/microengine-web-demos-0.1.0.tar.gz site/demos.lock.json --target feat/web-site --title "Web demos 0.1.0" --notes "Web packages of the four demos. Pinned by site/demos.lock.json."
```

Use `--target main` instead once `feat/web-site` is merged. If the archive is no longer available, rebuild it
with the workflow (push the tag) and commit the release's own `demos.lock.json`, since a rebuilt archive has a
different SHA-256.

### What a package may contain

Packages are public files. `web/build.sh` compiles them without build-machine paths (no debug information in the
assemblies, `-ffile-prefix-map` for native code) and adds the third-party notices (`THIRD-PARTY-NOTICES.txt` and
`licenses/`). Emscripten still writes the location of one .NET runtime file into `dotnet.native.*.js` as a comment,
so keep the toolchain folder (`MICROENGINE_WEB_TOOLS`) outside your user profile for release builds;
`check-dist.mjs` fails the site build if a package names a user profile folder.

## Building the site locally

Requires Node.js 22.23.4 or later.

```bash
cd site
npm ci
DEMOS_DIR=../dist/web npm run build     # or DEMOS_ARCHIVE=..., or SITE_DEMOS=none
npm run preview                         # http://localhost:4321/
npm test                                # documentation loader, capability checks, packaging, vercel.json
npm run test:browser                    # Chrome and Firefox: layouts, demos, touch emulation (needs a build with demos)
```

`npm run dev` serves the site with live reload; editing a published Markdown file reloads its page.

## Connecting Vercel

The site has not been deployed yet. The repository is private and stays private; these are the exact steps for
the owner. Nothing here has been run against Vercel or GitHub yet.

1. **Publish the first demo release** (once): push the tag `demos-v0.1.0` (or run the steps by hand, above), wait
   for the `web demos` workflow, then commit the release's `demos.lock.json` as `site/demos.lock.json`. Until this
   file holds a version and SHA-256, every Vercel build fails with "no demo release is pinned".
2. **Create the token.** GitHub → Settings → Developer settings → Fine-grained personal access tokens:
   - Resource owner: `NastMz`; Repository access: **Only select repositories** → `MicroEngine`.
   - Repository permissions: **Contents: Read-only** (Metadata: Read-only is added automatically). Nothing else.
   - An expiry date you will renew; a new token only needs updating in Vercel.
3. **Import the project.** Vercel → Add New → Project → import `NastMz/MicroEngine`. The Vercel GitHub app must be
   allowed to read this private repository (GitHub → Settings → Applications → Vercel → Repository access).
4. **Root Directory:** `site`. Keep "Include files outside the root directory in the Build Step" enabled: the site
   reads `docs/`, the games' `CREDITS.md` and screenshots and `samples/HelloEngine` at build time.
5. The framework (Astro), install command (`npm ci`), build command (`npm run build`) and output directory
   (`dist`) come from [`site/vercel.json`](../site/vercel.json). Node.js: 22.x or later (`engines` in
   `site/package.json` asks for 22.23.4 or later).
6. **Environment variables** (Project → Settings → Environment Variables):
   - `DEMOS_TOKEN` = the token from step 2, marked **Sensitive**, for Production and Preview. It is read only by
     `scripts/demos.mjs` during the build, sent only to `api.github.com` (never to the storage host the download
     redirects to), and never printed. It has no `PUBLIC_` prefix, so Astro cannot put it in pages, and
     `check-dist.mjs` fails the build if its value appears in any published file.
   - `SITE_URL` (optional): the canonical address when a custom domain is used. Without it the build uses
     Vercel's production domain (`VERCEL_PROJECT_PRODUCTION_URL`) for the sitemap, canonical links and social cards.
7. **Deploy.** A production build fails, rather than publishing a site without games, if the release cannot be
   downloaded or its SHA-256 differs from the lock file.

### What the build checks before anything is published

`check-dist.mjs` fails the build when any of these is true:

- a demo package, a file listed in a game's `content.txt`, a documentation page, the roadmap, the sitemap, the
  404 page or the search index is missing;
- a package still carries `.br`/`.gz` copies, or the home page references `/play/` (demos must load on demand);
- a published page links to `github.com/NastMz`, says `git clone`, names the solution file, or claims open source or
  an MIT licence;
- an internal file type is published (`.md`, `.cs`, `.csproj`, `.props`, `.targets`, `.pdb`, `.map`, `.sh`);
- the value of `DEMOS_TOKEN` or `GITHUB_TOKEN` appears in any file.

The site publishes only the Markdown files listed in `site/src/lib/repo-docs.mjs`; a link from them to any other
repository file is published as plain text.

## What Vercel serves

| Path | Behaviour |
| --- | --- |
| `/`, `/demos/<game>/`, `/docs/...` | static HTML; URLs end in `/` (`trailingSlash`), so `/docs` redirects to `/docs/` |
| unknown paths | `404.html` with status 404 |
| `/_astro/*` | fingerprinted CSS, JavaScript and images: cached for a year (`immutable`) |
| `/play/<Game>/_framework/*.<hash>.wasm|js` | fingerprinted runtime and assemblies: cached for a year |
| `/play/<Game>/` (other files) | revalidated on every visit (Vercel's default), `X-Robots-Tag: noindex` |
| `.wasm` | `Content-Type: application/wasm`; compressed by Vercel (Brotli or gzip) like JS and JSON |
| `/sitemap-index.xml`, `/og.png` | sitemap of every page; social card image made from a game screenshot at build time |

Every response also carries `X-Content-Type-Options: nosniff`, `Referrer-Policy`, `X-Frame-Options: SAMEORIGIN` and
a restrictive `Permissions-Policy`. The demos are embedded from the same origin, so `SAMEORIGIN` allows them.

Limits to know: a deployment holds about 44 MB of files, about 36 MB of them the four demo packages (Neon Rift's
OGG music is most of it and is not compressed further). WAV and OGG files are served as they are; Vercel only
compresses text and WebAssembly types.

## Checks before publishing

- `npm test` and `npm run test:browser` pass (the browser suite also writes full-page screenshots to
  `site/test-results/` for review).
- After the first deployment, open each demo page on the deployed site, press Play and check the response headers
  of a `.wasm` file (`content-type`, `content-encoding`, `cache-control`). These have only been checked against the
  local stand-in server (`site/tests/serve-dist.mjs`), not on Vercel.
- Third-party notices: the web packages ship the .NET runtime and Emscripten code (MIT) and the site ships OFL fonts;
  a notices page or file is recommended before the public launch ([licensing proposal](legal/distribution-proposal.md),
  section 4).
