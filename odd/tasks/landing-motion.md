# LANDING-MOTION: the films in the repository and a scroll-driven landing

Objective: bring the three films into the public repository and the site, and redesign the landing around
scroll-linked motion. Branch `feat/landing-motion` (from `main`), one pull request, `ask-on-risk`. Site tests:
`npm test` and `node tests/browser.mjs` in `site/`; planner tests: `node --test .github/scripts/*.test.mjs`.

## Specs

- **S1** Organize the sources of `trailer/` (Seated), `trailer-v2/` (Room for another) and `trailer-v3/` (Every step)
  "en una estructura coherente, mantenible y preparada para formar parte del repositorio público"; keep them
  reproducible; no rendered videos, caches or other regenerable files in Git.
- **S2** "Respeta la separación de licencias existente": the engine is MIT; the films may be reserved. "Verifica
  también las licencias de cualquier recurso o dependencia externa antes de publicarlo."
- **S3** Host and distribute the three final MP4s "sin introducirlos como grandes binarios en el historial"; publish
  if permissions allow; "No generes nuevas releases del motor"; no video renders in habitual CI.
- **S4** "Rediseño integral de la landing": scroll-linked animation that is part of the narrative, with the identity
  of the three films (Seated: code, transformation, result; Room for another: modular architecture; Every step: time,
  rhythm, execution).
- **S5** The three films available on the site "como producciones independientes", never V2 and V3 as replacements;
  thumbnails, titles, descriptions, sound, full screen, accessible controls; "No cargues automáticamente los tres
  videos completos al abrir la landing."
- **S6** Scroll experience: reversible; natural scroll, never blocked; responsive; reduced motion; performance;
  lazy media; no visual jumps; the supported browsers. No unnecessary dependencies.
- **S7** Limits: no engine API changes; no future capability shown as available; no functional content removed; demos
  and documentation pages keep working; independent branch; repository conventions.
- **S8** Validate in a real browser (transitions, playback, controls, navigation, responsive, reduced motion), run the
  existing tests and add pertinent ones, review performance, check the videos play from their final location.
- **S9** Deliver a short report and "una Pull Request independiente"; no automatic merge, no production deploy.

## Tasks

- [x] T1 (S1, S2) `media/`: three film folders, shared tools, catalog, notice, README. Inline.
- [x] T2 (S1) Rebuild the three films from the new structure to prove it. Inline (background run).
- [x] T3 (S3) Web encodes published as assets of release `films-1`; playback checked from there. Inline.
- [x] T4 (S4, S6) Landing: page tape, 3D opening, code-to-sprite act, board seating, the loop act on the inverted
  ground, reveals; `scripts/scroll.ts`, no library. Inline.
- [x] T5 (S5) Films section, player dialog and `/films/` page. Inline.
- [x] T6 (S8) Browser verification, site tests, new tests, performance. Inline.
- [x] T7 (S7, S9) CI planner rule for `media/`, licensing page, design notes, commits, pull request. Inline.

Route: everything inline (one author, one coherent design; no unit is independent enough to launch in parallel).

## Log

- **L1** Request (2026-11-10), "Misión: MicroEngine — Identidad audiovisual y rediseño de la landing": organize the
  productions, distribute the videos, redesign the landing with scroll-driven animation, integrate the three films,
  validate, and prepare an independent pull request without merging or deploying. (Full text in the session.)
- **L2** The release branch had already merged (`main` at 43e3d66), so the work branches from `main` in a worktree and
  the untracked `trailer*/` folders of the old checkout stay as they were.
- **L3** Motion without a library: one 100-line module sets `--p`/`--step` per element and CSS does the rest. GSAP
  ScrollTrigger would add a dependency and a second timing system for what five custom properties do; native CSS
  scroll timelines are not yet in every supported browser.
- **L4** Films are published as assets of a GitHub release of their own (`films-1`, never "latest"): free, permanent,
  outside Git history, no new service or credential. CI artifacts expire; Vercel static files would live in Git.
- **L5** Evidence (2026-11-10): the three films rebuilt from `media/` (`films.mjs build`, checks 0 errors; Seated's master is byte for byte the size of the original render); web files 13.0, 10.6 and 6.2 MB published as assets of `films-1` (not latest; `v0.5.0` still is; `demos.mjs next-version` ignores the tag). Each film played from GitHub through the site's player in Chrome and Firefox (readyState 4, seekable to its end, 0 media requests before a press). Site: `npm test` 70 pass; planner and evidence tests 59 pass; `browser.mjs --suites layout` all checks passed in Chrome and Firefox (4 widths, every page). Landing first load 251 kB; scrolling the whole page both ways at 600 px/s: median frame 10 ms, 2 frames over 33 ms with the CPU slowed 4x. Looked at in Chrome at 1440 and 375 px, light and dark, and under reduced motion (`scroll-live` absent, finished states).
- **L6** Not done: the `play` and `touch` browser suites were not run locally (demo pages are untouched; CI runs them for any `site/` change). The films' sound has still not been heard by a person.
