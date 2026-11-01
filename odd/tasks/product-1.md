| T7 | S10 S14 | inline | partial | desktop packages in CI done; release with downloads and production publication need the merge and the owner || T6 | S1 S4 S11 S12 S13 | inline | done | docs commit; ADR 0018, 0019; evaluation product-1.md; dotnet pack + local-feed consumer || T5 | S2 S3 S5 S7 S14 | inline | done | 4a4eab1; layout suite Chrome and Firefox passed with /platforms/, /docs/downloads/, /docs/sdk/ || T4 | S9 | inline | done | c3c...; native.test.mjs 4 tests; validate runs 41265125306 (found bugs), 41265744009 (green) || T3 | S6 S8 S10 | inline | partial | b449047; four APKs played on the local emulator with a throwaway release key; CI needs the owner's signing secrets || T2 | S6 S8 S10 | inline | done | b449047 + fixes; Windows 4/4 local (138 captures), Linux 4 distros, CI run 41265744009 (12 packages) || T1 | S7 S15 | inline | done | 2fb8255, b449047; MicroEngineVersion 0.2.0-alpha, games 1.0.0; GameLauncherTests RED/GREEN |# PRODUCT-1 + DIST-1: product experience, documentation and multiplatform distribution

Objective: a public product that matches the engine after PLATFORM-3 (landing, docs, compatibility evidence), the four
demos downloadable as native packages next to their web builds, and the SDK Alpha presentation with SDK-1 prepared.
Branch `feat/product-1` (from `main` at f6b1aa4). Delivery: `ask-on-risk`. Tests: `dotnet test MicroEngine.slnx -c
Release`, `npm test` in `site/`, `node --test packaging/tests`.

## Specs

- S1 Audit: "Recorre exhaustivamente la landing y todas sus páginas de documentación." "Contrasta cada afirmación con el
  estado real del repositorio, especialmente tras PLATFORM-3." "Conserva las partes correctas." "No reescribas
  documentación técnica precisa únicamente por razones estéticas."
- S2 Landing: "MicroEngine es un motor de videojuegos que orquesta subsistemas e integra tecnologías existentes para
  ofrecer una experiencia coherente de desarrollo." Present identity, value, subsystem architecture, code-first,
  platforms, backends and profiles, `IPlatform` extensibility, demos, source and MIT, status, roadmap, getting started,
  downloads. "No presentes MicroEngine como un motor de desarrollo visual equivalente a Godot o Unity." "Tampoco lo
  presentes simplemente como un wrapper de SDL3 o Raylib."
- S3 Identity: "No basta con sustituir textos y añadir botones a la landing existente." "Conserva los elementos visuales
  que ya funcionen" "Evita un diseño genérico de dashboard empresarial."
- S4 Docs against code: every topic of section 4 (architecture, `Game`/`GameRunner`/`GameSession`, `IPlatform`,
  composition, rendering, audio, input, scenes, content, persistence, lifecycle, SDL3, raylib, the six platforms,
  selection, custom backends, testing, packaging, licences, limitations). "No inventes capacidades basándote
  exclusivamente en lo que planeamos construir." Keep game-developer and contributor docs apart.
- S5 Matrix: distinguish hardware, emulator, simulator, build, headless, rendering, input, audio, lifecycle. "No afirmes
  que MicroEngine fue validado en un iPhone físico cuando la evidencia corresponde al simulador iOS."
- S6 Demos downloadable: "Las cuatro demos de MicroEngine deben poder jugarse en el navegador y descargarse para
  ejecutarse localmente en las plataformas donde podamos producir paquetes funcionales."
- S7 Demo page: name, description, real capture, play in browser, download platforms, game version, engine revision,
  requirements, install/run instructions, limitations, licence and credits; "Download for your platform". "No publiques
  enlaces simulados ni botones para plataformas que todavía no tienen un paquete distribuible."
- S8 Formats: Windows portable ZIP; Linux portable with documented dependencies ("No supongas que un build de CI
  funciona automáticamente en cualquier distribución Linux"); macOS app with signing/notarization researched ("No
  anuncies una aplicación como instalable sin restricciones si macOS exige pasos adicionales"); Android "APK
  instalables, firmados y versionados", correct ABIs, "No publiques claves privadas de firmado ni las almacenes en el
  repositorio"; iOS: "No presentes un paquete de simulador como descarga utilizable en un iPhone real."; Web kept
  without regressions.
- S9 Automation: reuse selective builds, content-addressed artifacts, hashes, caches, manual releases, site publication,
  rollback. "No construyas otro sistema de releases paralelo." "Cuando solo cambie un juego, reutiliza los artefactos
  aprobados de los demás cuando sea seguro." "Distingue entre generar un paquete, validar que se ejecuta y publicarlo
  como descargable."
- S10 Package tests: "Comprueba los archivos exactos que descargará el usuario." Structure, executables, dependencies,
  resources, start, rendering, input, audio, exit, integrity, versions. "Si únicamente permite compilarlos, no declares
  que fueron probados funcionalmente."
- S11 Install docs per platform; Android sideloading "sin afirmar que Android prohíbe universalmente el sideloading";
  "No confundas la distribución mediante GitHub Releases con publicar juegos en Steam, Google Play o App Store."
- S12 SDK status: "MicroEngine SDK — Alpha / 0.x" "La API pública puede sufrir cambios incompatibles entre versiones
  Alpha." "No declares estabilidad 1.0 ni compatibilidad retroactiva garantizada."
- S13 SDK-1 preparation: NuGet packages, `dotnet new` templates, versioning, per-backend dependencies, default platform,
  samples, distribution, installation, migration docs; core and adapters without forcing every backend. "No es
  necesario implementar toda la infraestructura de SDK-1 en esta misión."
- S14 Site validation: navigation, responsive, accessibility, links, docs, demos, downloads, metadata, licences,
  browsers, mobile. "No reemplaces las capturas reales de las demos por imágenes conceptuales".
- S15 Coherence: centralize versions, profiles, download availability, compatibility; "No dupliques manualmente
  información que el sistema de build ya pueda producir de forma confiable." "No construyas un sistema excesivamente
  complejo únicamente para evitar repetir unas pocas líneas."
- S16 Constraints: no new physics/UI/audio/asset systems, no core rewrite, no SDL3/raylib replacement, not a store,
  demos never MIT, "No publicar releases incompletas ni anunciar descargas que no existen.", no store account needed.

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S7 S15 | inline | done | 2fb8255, b449047: `MicroEngineVersion` 0.2.0-alpha, games 1.0.0, build-info.json; GameLauncherTests RED then GREEN |
| T2 | S6 S8 S10 | inline | done | b449047, 6934df9, 262eedd: Windows 4/4 on this PC, Linux in four distributions, CI run 41265744009 (12 packages) |
| T3 | S6 S8 S10 | inline | partial | b449047: four APKs played on the local emulator, release path tested with a throwaway key; CI needs the owner's signing secrets |
| T4 | S9 | inline | done | b449047, 4416e87: `native.test.mjs`; validation runs 41265125306 and 41265442417 (bugs found), 41265744009 green |
| T5 | S2 S3 S5 S7 S14 | inline | done | 4a4eab1: layout suite in Chrome and Firefox passed, new pages included; 375/1280 px screenshots without overflow |
| T6 | S1 S4 S11 S12 S13 | inline | done | docs commit: audit in docs/evaluations/product-1.md, downloads, SDK, migrations, ADR 0018 and 0019 |
| T7 | S10 S14 | inline | partial | desktop packages in CI done; a release with downloads and the production site need the merge and the owner |

## Log

- L1 (2026-10-31) Original request: "MicroEngine — PRODUCT-1 + DIST-1 Product Experience, Documentation & Multiplatform
  Distribution" (parts A–D, sections 1–16, deliverables 1–13 and the final acceptance criterion, as pasted by the
  owner; quoted verbatim in the specs above where wording matters).
- L2 Exploration: PLATFORM-3 is merged (PR 5). Release `v0.1.0` holds the four web packages only. The store, keys and
  release flow live in `site/scripts/demos.mjs` (web packages keyed by Git blobs); `checks.yml` builds them on
  Windows; `release.yml` drafts, verifies, deploys and publishes. Phone hosts build per game from `web/games.props`;
  the Android APK is signed with the machine's debug key and `ApplicationVersion` is fixed at 1. The landing still
  shows raylib as the desktop backend, "Desktop validated on Windows only" and "No touch controls"; the roadmap's
  mobile track predates the SDL hosts.
- L3 Local evidence: Windows packages of all four games verified on this PC (138 captures pixel-identical, audio available);
  Linux packages in Docker: Ubuntu 22.04, Debian 12, Fedora 41 138/138, Debian 11 refuses (glibc). Found: ICU dependency
  (fixed: invariant globalization), unhandled DllNotFoundException (fixed, test), Content/*.cs copied (fixed), a false
  Debian 11 pass (check now needs the glibc error). Android: four APKs on the emulator, Courtyard Key and Trail Duel differ
  from the GPU reference by a texel (half-pixel HUD, rotated sprites) as on Mesa/Metal: recorded sdl-android-x86_64.json.
- L4 CI: validate 41265125306 failed on path false positives (/home/runner inside SDL and .NET binaries) and WARP rendering
  on Windows runners; 41265442417 failed on a regex broken by a shell edit; 41265744009 green: 12 desktop packages built,
  played and stored. WARP reference recorded locally with SDL_RENDER_DIRECT3D11_WARP=1 and reproduced by CI.
- L5 Facts checked on the web (2026-11-01): Android developer verification enforced since 2026-09-30 in Brazil, Indonesia,
  Singapore and Thailand (advanced flow or adb for unregistered developers; worldwide 2027); macOS 15+ removed the
  Control-click bypass (Privacy & Security, Open Anyway). Commit 262eedd (WARP reference) also carried site work in progress.
- L6 SDK: dotnet pack produces the four packages at 0.2.0-alpha; consumed from a local feed outside the repo (full desktop,
  and SDL only without raylib). Site test files now run one at a time (two files wrote public/play concurrently).
- L7 Pending, owner decisions: Android signing secrets; push/PR/merge to main; the release that publishes downloads and the site.
- L8 Full validation 41266506438 of 6707fcb green, 27 jobs: .NET tests (Linux, Windows), native Linux, four web packages,
  browser suites (layout, play, touch) in Chrome and Firefox, the 12 desktop packages built, played and stored, macOS,
  Android and iOS platform jobs. The stored packages match the branch head, so a release of it builds no desktop package.
