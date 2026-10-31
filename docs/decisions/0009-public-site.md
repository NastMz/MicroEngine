# ADR 0009: Public site with Astro and Starlight, documentation from the repository

## Status

Accepted (WEB-2).

## Context

MicroEngine needs a public site: a landing page that says what the engine is, one page per demo that plays its
WebAssembly build (WEB-1, [ADR 0007](0007-browser-host.md), [ADR 0008](0008-web-packages.md)), and documentation
for developers. Requirements: static hosting on Vercel without a server, good performance on phones, search and
navigation in the documentation, and one source for every document: the Markdown already kept next to the code.
The demo packages need a toolchain (.NET wasm-tools, emscripten, rebuilt raylib) that a site build should not need.

## Decision

- **Astro with Starlight, in `site/`.** Astro renders every page to static HTML and ships JavaScript only where a
  page needs it (the demo player). Starlight provides documentation navigation, Pagefind search (built at build
  time, no service), code highlighting, previous/next links, a light and dark theme and a sitemap. The landing and
  demo pages are plain Astro pages with the same design tokens. The site sits in its own folder; no existing
  project moved.
- **Documentation is read from the repository, not copied.** A content loader (`site/src/lib/repo-docs.mjs`) lists
  the public files (guides under `docs/guide`, `docs/manual`, `docs/examples`, `docs/reference`, plus
  `docs/architecture.md`, `docs/making-a-game.md`, `web/README.md` and each game's README), takes the title from
  the first heading and rewrites links: to the site page when the target is public, to plain text otherwise (ADRs,
  evaluations, source files, internal guides), because the repository is private. ADRs, evaluations, experiments,
  `odd/`, engine-development guides and game READMEs are not published. The files carry no
  front matter, so they read the same on GitHub. Images referenced by public pages are converted at build time
  from their repository files. Landing code excerpts and the hero sprite are also read from the source files at
  build time, and the build fails if they cannot be found.
- **Demo packages are a versioned build input.** CI builds the packages on Windows (where the toolchain scripts
  were validated), packs them into `microengine-web-demos-<version>.tar.gz` and publishes it as a GitHub release
  asset; `site/demos.lock.json` pins its name and SHA-256. The site build downloads and checks it, or uses local
  packages for development. Packages ship without their `.br`/`.gz` copies because Vercel compresses WebAssembly.
- **Demos load on demand, in a same-origin iframe.** The player creates the iframe only when the visitor presses
  Play and follows the host's existing messages; the host page is unchanged.
- **Capability detection, not device detection.** The player offers the demos when the browser has WebAssembly,
  WebGL, a fine pointer with hover and room; otherwise it explains that the games need a keyboard and offers the
  screenshots and guides, plus a "Try anyway" button. A hardware key press upgrades the device. The User-Agent is
  not read.

## Alternatives considered

- **Docusaurus or VitePress.** Both are strong documentation tools, but Docusaurus ships a React runtime on every
  page, and both make a custom landing page with zero JavaScript harder than Astro does. Starlight is built on Astro,
  so landing, demos and docs are one build.
- **Copying Markdown into `site/src/content/docs` (by hand or with a sync script).** Two copies drift, and a
  generated copy in Git is noise. Reading the files at build time keeps one copy; the loader also reloads them in
  `astro dev`.
- **Building the demos during the Vercel build.** It would need about 2.5 GB of toolchain and several minutes per
  deployment, on Linux where the scripts are untested. A pinned archive makes site builds fast and reproducible.
- **Committing the packages.** About 36 MB of generated binaries per version in Git history.
- **Starting the demo with a postMessage from the page.** The iframe needs its own user gesture for audio; keeping
  the host's own Play screen avoids changing WEB-1's tested start sequence.

## Consequences

- A new public guide is a Markdown file in the repository plus one entry in `pages` (`repo-docs.mjs`); a file that
  is listed but missing fails the build.
- Publishing new demos is a tag, a release and a one-line lock change; the site never builds an unpinned archive.
- The repository is private and stays private: the Vercel build needs a read-only token for the release asset, and
  the site has no links into the repository. Public documentation describes the engine and its API; setup from
  source is internal documentation until the SDK is distributed.
- Room for later work without reorganising the site: `site/src/data/distribution.ts` switches the landing's calls to
  action to install steps when packages exist; releases and downloads can be new top-level pages next to
  `/demos/`, `/docs/` and `/roadmap/`, added to the header navigation in `SiteLayout.astro`.
