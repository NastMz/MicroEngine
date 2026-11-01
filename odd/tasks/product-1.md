# PRODUCT-1 + DIST-1: product experience, documentation and multiplatform distribution

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
| T1 | S7 S15 | inline | pending | engine and game versions in one place, build info in every package |
| T2 | S6 S8 S10 | inline | pending | `packaging/`: desktop packages (Windows, Linux, macOS) and their checks |
| T3 | S6 S8 S10 | inline | pending | Android release APKs, signing from secrets, checks on the emulator |
| T4 | S9 | inline | pending | native packages in the content-addressed store, the release and the site build |
| T5 | S2 S3 S5 S7 S14 | inline | pending | landing, demo pages with downloads, compatibility page |
| T6 | S1 S4 S11 S12 S13 | inline | pending | docs audit and updates, install guide, SDK Alpha, SDK-1 plan, ADRs |
| T7 | S10 S14 | inline | pending | CI evidence on GitHub runners, site checks, evaluation |

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
