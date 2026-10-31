# WEB-2: public site (landing, demos, documentation, Vercel)

Objective: a static Astro site in `site/` that presents MicroEngine, plays the four web demos and publishes the
repository's Markdown documentation from its original files. Branch `feat/web-site` from `main` (WEB-1 merged in #8).
Delivery: work-unit commits on the branch, no push or PR. Test runner: `node --test` (site), `dotnet test` (engine).

## Specs

- S1 Research and architecture: monorepo without mass reorganisation; evaluate Astro + Starlight first; static
  output, no backend or SSR; justify the choice (ADR).
- S2 Visual identity: own, recognisable, game-engine identity; not a generic docs template or SaaS landing. Home
  communicates what MicroEngine is, the kinds of games it builds today, technical capabilities, the four demos,
  multiplatform architecture, how to start, how to explore docs and source. "No afirmes que existen
  funcionalidades que todavía no están implementadas, como un editor visual o soporte móvil completo."
- S3 Responsive and capability detection: site fully usable on desktop, tablet and phone; demos not auto-started
  on devices without a compatible input setup; clear "optimised for desktop" message with useful alternatives; do
  not rely only on `User-Agent`; consider input capabilities, browser features and available space; allow an
  at-your-own-risk attempt, stating the configuration is not validated; no touch controls in the games.
- S4 Demo integration: reuse the WEB-1 host and its messages (`ready`, `starting`, `playing`, `ended`,
  `failed`); one page per game with name and identity, description, real screenshots, controls, compatibility,
  loading state, play button, fullscreen, restart and exit; packages loaded on demand only; iframe must not
  interfere with site navigation; each demo's persistence respected.
- S5 Documentation: navigation, search, examples, code blocks, internal links, categories. Introduction,
  Getting started, Engine manual (lifecycle, scenes, rendering, input, audio, assets, UI, persistence,
  achievements, backends), Examples (real code), Technical reference (contracts, responsibilities,
  configuration/extensibility, known limitations).
- S6 Single source of documentation: the site consumes the repository Markdown directly; public vs internal
  separation (ADRs, experiments, evaluations not published automatically); new public guides live once in the
  repository; no manual copies; rebuilding after editing a source updates the site.
- S7 Demo distribution: separate (1) WebAssembly build, (2) preparing and versioning artifacts, (3) site build,
  (4) publishing; the Astro build does not need .NET/emscripten/raylib; reproducible; no generated files in Git;
  no dependence on files that only exist on a developer machine; check paths, `.wasm`, scripts, assets,
  compression, headers and hosting limits.
- S8 Vercel: reproducible build, project config and output dir, static demo files, direct navigation, headers and
  MIME types, caching of versioned assets, error pages, unknown routes, SEO, sitemap, metadata; documented
  connection steps; no claim that the public deployment works unless verified; no external changes without
  authorisation.
- S9 Performance and accessibility: no unnecessary wasm downloads, right-sized images, cheap animations; keyboard
  navigation, contrast, focus states, alt text, touch target sizes, reduced motion, mobile performance, iframe
  and fullscreen behaviour, no accidental horizontal scroll; docs readable on a phone.
- S10 Validation: Chromium and Firefox desktop; responsive mobile sizes (emulated vs real stated); the four demos
  from their pages; navigation, links, lazy loading, game states, iframe messages; engine tests if affected;
  site and packaging tests; docs update when their Markdown source changes.
- S11 Future distribution: navigation ready for NuGet packages, templates, releases and editor downloads without
  implementing them or showing download links that do not exist.
- S12 Delivery: coherent commits, no push or merge; deployment docs; summary of decisions and limitations.

## Tasks

| ID | Specs | Route | Status | Commit |
| --- | --- | --- | --- | --- |
| T1 | S5, S6 | delegated (parallel unit: engine manual pages under `docs/manual/`) | done | docs commit |
| T2 | S5, S6 | delegated (parallel unit: guide, examples and reference pages) | done | docs commit |
| T3 | S1, S6 | inline: Astro + Starlight site, Markdown loader over repository files, ADR | done | site commit |
| T4 | S2, S3, S4, S9, S11 | inline: landing, demo pages, capability detection, player | done | site commit |
| T5 | S7, S8 | inline: demo pack/fetch scripts, CI workflow, Vercel config | done (workflows and Vercel not run) | site + deploy commits |
| T6 | S10 | inline: site tests, browser validation | done | site commit |
| T7 | S8, S12 | inline: deployment guide, report | done | deploy commit |

Route evidence: T1 and T2 are independent documentation units with disjoint files, each several files of new
content needing engine API study; they run in parallel with T3-T5 (site code), so writers are justified by
parallelism. T3-T7 share the site's files and stay inline.

## Log

- L1 (2026-10-30) Original request: "MicroEngine — WEB-2: Landing, documentación y distribución web" (full text in
  the session; specs above quote its requirements).
- L2 WEB-1 is merged in `origin/main` (PR #8); branch `feat/web-site` created from `87be497`.
- L3 Findings: the repository is private on GitHub; Vercel compresses `application/wasm` on the fly (not WAV/OGG/PNG),
  so the site ships packages without `.br`/`.gz` copies. Games already have real screenshots in
  `games/<Game>/screenshots/`.
- L4 Docs writers delivered 23 pages (docs/guide, docs/manual, docs/examples, docs/reference). They found two
  inaccuracies in existing docs, fixed here: music switching is fade-out then fade-in (not a crossfade), and the
  default `action` binding is C/L (not C/L/X).
- L5 Evidence: `npm test` 18/18; `npm run test:browser` 504 checks pass in Chrome and Firefox (headless, installed
  browsers): no horizontal scroll or clipped content at 1280/768/375/320 px, alt text, 24 px targets, skip link,
  no /play/ request before Play, all four demos reach `ready` and `playing` from their pages, restart and exit,
  404 status; touch phone/tablet are Chrome device emulation only (no real devices). `dotnet test` exit 0 (no engine
  code changed). Docs rebuild and `astro dev` live reload verified after editing `docs/manual/scenes.md`.
- L6 Not verified: Vercel deployment (no account access), the GitHub workflows (not run), the GitHub release
  download path in `demos.mjs fetch` (no release exists; the archive and local-folder paths were run). Safari and
  real mobile devices not tested.
- L7 (2026-10-30) Owner review "MicroEngine — WEB-2: Revisión final de producto, documentación y publicación": the
  repository is private and stays private; MicroEngine is not open source (future: free, commercial use, no
  royalties, compiled packages, closed source); remove open source and private-repository links from the site;
  separate public and internal documentation; add `/roadmap`; update platform compatibility (gamepad validated by
  hand in a Chromium-based browser); keep the visual identity; licence only as a proposal for approval.
- L8 Changes: public docs list rewritten (internal: engine-development setup, repository structure, web/README,
  game READMEs; new public: Getting MicroEngine, CC0 credits pages); links from public pages to internal files are
  published as plain text; no GitHub links, edit links or social icon; landing CTAs are demos, docs and roadmap;
  `distribution.ts` switches CTAs to install steps when an SDK exists; `/roadmap/` with completed, planned,
  exploring and vision levels and no dates; licensing proposal in docs/legal (not published, LICENSE unchanged).
  Bug found and fixed: links whose text contained inline code were never rewritten (raw relative links on pages).
- L9 Evidence: `npm test` 20/20 (includes a fake private GitHub API: token used only for the API, not sent to the
  redirected storage host, never printed; wrong token and SHA mismatch fail clearly); production-style build with a
  dummy DEMOS_TOKEN passes check-dist (no repository links, no open source or MIT claims in pages, no internal files,
  token value absent); `npm run test:browser` 732 checks pass in Chrome and Firefox. No engine code changed.
- L10 (2026-10-30) Owner approved the distribution model (proprietary private source; free personal, educational
  and commercial use; no royalties; games belong to creators; runtime shippable inside games; no standalone
  redistribution or resale; optional "Made with MicroEngine"; name MicroEngine SDK License). Earlier MIT copies keep
  their rights; the current implementation is a greenfield rewrite never published as source. Asked: prepare the
  licence replacement (final text pending legal review), add third-party notices to the site and web packages,
  keep public docs aligned, prepare demos-v0.1.0 and Vercel, verify no internal files, source or credentials leak;
  no public deployment or final licence.
- L11 Done: LICENSE is an all-rights-reserved holding notice; SDK licence draft for counsel; THIRD-PARTY-NOTICES.md
  (published at /docs/third-party-notices/, copied into every package with the .NET, Emscripten and raylib licence
  files); /docs/licensing/ states the planned model. Found and fixed: packages embedded build-machine paths (PDB
  locations, C assertion file names, the user profile folder); web builds now compile without debug information in
  separate folders and with -ffile-prefix-map, and check-dist rejects user-profile paths. demos-v0.1.0 rebuilt,
  packed and pinned (sha256 2ca15d76…). Evidence: dotnet test 279/279; npm test 20/20; browser suite 732 checks
  pass in Chrome and Firefox against the pinned archive; production-style build with a dummy token passes all
  exposure checks. Not done (owner): push, GitHub release, Vercel project, final licence text.
