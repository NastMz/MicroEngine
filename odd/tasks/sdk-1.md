# SDK-1: first public SDK release and game distribution toolchain

Objective: MicroEngine installable and usable by outside developers (packages, `microengine` CLI, `dotnet new`
template, per-game identity, packaging for every official target), with the four demos packaged by the same public
tools. Branch `feat/sdk-1` (from `main` at 2cf4f9f). Delivery: `single-pr` (the owner asked for "PR listo para
revisión"); nothing is published to a public feed. Tests: `dotnet test MicroEngine.slnx -c Release`,
`node --test packaging/tests`, `node sdk/tests/external.mjs` (external project from a local feed).

## Specs

- S1 Mission: "Convertir MicroEngine en un SDK público, instalable y utilizable por desarrolladores externos, sin
  depender de los scripts privados de nuestras demos ni de la infraestructura específica de nuestro repositorio."
  "un desarrollador debe poder instalar MicroEngine, crear un juego, ejecutarlo, compilarlo y generar paquetes
  distribuibles mediante herramientas oficiales." "No se trata únicamente de publicar los paquetes NuGet que ya existen."
- S2 Principles: reuse .NET, PRODUCT-1 packagers and native platform tools; "GitHub Actions debe ejecutar nuestras
  herramientas públicas, no ser un requisito para utilizarlas." "No obligues al desarrollador a conocer SDL3, Raylib ni
  los detalles del empaquetado de cada backend para desarrollar un juego normal." "No reescribas infraestructura
  funcional sin una justificación concreta."
- S3 Audit before design (PRODUCT-1 packages, dependencies, platforms, INPUT-1, `packaging/`, scripts, CI/CD, releases,
  project generation, content, WebAssembly, demo metadata, Alpha docs, limitations). "No dupliques los empaquetadores
  actuales."
- S4 Packages: backend-independent core, platform adapters, correct dependencies, consistent versions, docs, symbols,
  MIT for public parts, demos apart. "un desarrollador que solo necesite SDL no debería verse obligado a instalar
  Raylib." No internal paths, private assets or repository files in packages. "Mantén el estado Alpha 0.x, sin
  prometer estabilidad de API." Research the first SDK version so it is not confused with demo or earlier engine versions.
- S5 CLI installable the .NET way; intent `microengine new MyGame`, `run`, `build`, `package windows|linux|android|web`
  ("ejemplos de intención, no firmas de API obligatorias"). Create, run, choose configuration and platform, build,
  package, validate the environment, actionable errors, versions, help, distribution artifacts. Detect missing tools
  and explain how to install them. "No escondas errores detrás de mensajes genéricos." "No prometas compilación cruzada
  completa donde el toolchain no lo permita."
- S6 Templates: evaluate `dotnet new`; a working 2D project using public packages, with a game class, a scene, drawing,
  input actions, the loop, loading resources, building and running with the documented commands, packageable. "No
  copies íntegramente la estructura interna del repositorio MicroEngine." One basic template; variants only with
  concrete value.
- S7 Identity: "Actualmente algunas aplicaciones distribuidas presentan el nombre genérico `MicroNG` y el icono
  predeterminado del sistema, en lugar del nombre e icono del videojuego." Display name, application id, version,
  description, icon, orientation, per-platform metadata; one clear configuration ("Evita dos fuentes de verdad
  contradictorias"); generated for Windows, Linux, macOS, Android, iOS and Web. "Adapta las cuatro demos a este
  mecanismo" "Los metadatos deben provenir de la configuración del juego, no estar quemados en scripts de empaquetado."
- S8 Distribution tool from PRODUCT-1: Windows (executable with dependencies and resources), Linux (documented
  dependencies), macOS ("Respetar las condiciones reales de firma y notarización"), Android (installable APK, developer
  keys, test vs distribution builds, "No generar automáticamente una identidad de distribución nueva en cada release."
  "No incluir credenciales privadas en paquetes, repositorios ni logs."), iOS (Apple tools, simulator vs device, "No
  presentar paquetes de simulador como instalables en iPhone."), WebAssembly ("No depender de que el proyecto esté
  dentro del monorepo de MicroEngine."; conventional hosting).
- S9 Independent of CI: the public commands work locally; workflows consume the same capabilities; no build logic
  duplicated between CLI, demo scripts, Actions and releases; content-addressed storage and checks keep working; "No
  sacrifiques la eficiencia del pipeline existente por introducir la CLI."
- S10 Per-platform configuration (orientation, package id, architectures, icons, backend, distribution profile, run
  options, signing, output folders) with defaults; "No expongas obligatoriamente cientos de propiedades".
- S11 Signing and secrets: developer credentials, never in packages or templates, local and CI mechanisms (environment
  variables, secure stores), "Nunca muestres secretos en logs." "MicroEngine no debe convertirse en intermediario
  obligatorio de firmado."
- S12 External project: outside the repository, install from distribution packages, create from the public template,
  build, run, input, drawing, external resources, own name/id/icon, packages through the CLI, run them where possible;
  "no puede depender de rutas del monorepo, scripts específicos de nuestras cuatro demos ni referencias directas a sus
  proyectos fuente." Several OSes on the available runners.
- S13 Demos: Meadow Run, Courtyard Key, Trail Duel and Neon Rift package through the public SDK mechanisms (own name,
  icon, resources, build, packaging, versions, integrity, running after packaging); web builds and pages keep working;
  "No cambies las reglas, mapas o mecánicas de los juegos."
- S14 Release preparation: packages, CLI, templates, coherent versions, release notes, install docs, first steps,
  examples, build/packaging commands, platform requirements, limitations, Alpha breaking-change policy, upgrade
  procedure; release workflows; "no publiques paquetes en feeds públicos ni ejecutes un lanzamiento definitivo sin mi
  autorización."
- S15 Docs and landing: real commands only; install, first project, run, configuration, resources, input, build,
  packaging, distribution, extensibility, API, migrations, Alpha versioning; user docs apart from contributor docs;
  roadmap and ADRs; "No anuncies que un paquete está disponible públicamente antes de verificar que realmente lo está."
- S16 Validation: engine, backends, demos, CLI, templates, install from packages, external build, packaging, metadata,
  resources, dependencies, artifacts, CI, WebAssembly regressions; expected failures (missing platform SDK,
  unsupported host, invalid id, missing resource, invalid icon, missing signing credentials, missing native
  dependencies) with understandable errors. "No consideres suficiente que `dotnet pack` termine exitosamente."
- S17 Out of scope: physics, UI framework, new renderers, full font system, local multiplayer, networking, editor,
  commercial publishing, store or central service. No demo-specific requirements in the public API. "No congeles
  prematuramente las APIs Alpha." "No rompas la distribución existente sin una migración comprobada."
- S18 Future demo: do not implement multiplayer, multiple pads, HD assets or external fonts now, but do not block them;
  "no presupongas que todos los juegos utilizarán la fuente bitmap interna de MicroEngine."
- S19 Autonomous on a dedicated branch from `main` after INPUT-1; finish implementation and real external use; deliver
  the 16 items (architecture, packages, CLI, templates, platforms, metadata, signing, external tests, demos, CI, docs,
  limitations, breaking changes, proposed version, PR, publishing procedure). Acceptance: "Un desarrollador que no
  conoce nuestro repositorio debe poder instalar MicroEngine y crear, ejecutar y distribuir un juego propio utilizando
  herramientas públicas y documentadas." "Las cuatro demos deben utilizar esas mismas herramientas".

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S7 S10 | inline | done | 6da2e39; AppIdentity tests RED/GREEN; Neon Rift exe icon and name checked |
| T2 | S5 S8 S9 S10 S11 | inline | done | 6da2e39, 933520e; external test all targets this PC builds |
| T3 | S6 | inline | done | 933520e; template instantiated from the feed, capture inspected |
| T4 | S7 S9 S13 | inline | done | 6da2e39, fa241a2; demos verified through the tool (Windows, Linux distros, Android emulator, web) |
| T5 | S12 S16 | inline | done | 933520e, fa241a2; sdk.yml runs it on three systems (CI result pending) |
| T6 | S4 S14 | inline | done | fa241a2; sdk-release.yml written, not run (owner's decision) |
| T7 | S14 S15 | inline | done | 6051569 + evaluation docs/evaluations/sdk-1.md |

Route evidence: one coherent design shared by every unit (identity feeds the CLI, the CLI feeds demos and CI), so no
unit is independent enough for a parallel writer; long builds and CI runs go to the background.

## Log

- L1 (2026-11-01) Owner's request: the SDK-1 mission "MicroEngine — SDK-1, First Public SDK Release & Game Distribution
  Toolchain", sections 1–18 and the final acceptance criterion, as pasted; quoted verbatim in the specs where wording matters.
- L2 Audit. Four packages pack today (`src/Directory.Build.props`, MIT) but nothing pushes them. Hosts for browser,
  Android and iOS select a game from `web/games.props` (title + C# factory expression) and build from repository paths;
  `packaging/package.mjs` packages the demos only (notices, README and licence text are demo-specific). The Android label
  is the fixed `app_name` "MicroEngine" (`platforms/android/Resources/values/strings.xml`), no package has an icon,
  and macOS/Android/iOS ids are `dev.microengine.<game>` built in scripts. Native keys hash `packaging/` and web keys
  `web/` (`site/scripts/demos.mjs`), so moving hosts changes every key once. Tags `v0.1.0`/`v0.2.0` are demo releases,
  which collide with engine versions `0.2.0-alpha`/`0.3.0-alpha`.
- L3 Decisions: identity as project properties read by MSBuild (ADR 0021); hosts as templates inside the tool; SDK
  version 0.4.0-alpha.1 with tags sdk-v<version>; publishing only through sdk-release.yml with `publish` and the nuget
  environment. Delivery switched to one PR for the whole feature, as the owner asked ("PR listo para revisión").
- L4 Local evidence (docs/evaluations/sdk-1.md): all .NET, site and planner tests; package checks; external project on
  Windows (windows, linux, android on the emulator, web in Chrome); demo packages through the tool verified on Windows,
  three Linux distributions and the Android emulator. Bugs found and fixed: dotnet new list exit 103, read before
  restore, microengine.dll vs MicroEngine.dll, escaped Android RIDs.
- L5 Next: push, PR, CI (ci.yml, sdk.yml); macOS and iOS evidence comes from CI. Pending owner decisions: merge,
  NUGET_API_KEY secret and nuget environment, running sdk-release.yml.
- L6 (2026-11-01) Owner: "prefiero NuGet Trusted Publishing mediante GitHub OIDC y la acción oficial NuGet/login, en
  lugar de mantener una NUGET_API_KEY de larga duración." "No publiques todavía". sdk-release.yml now logs in with
  NuGet/login v1.2.0 (pinned) in the `nuget` environment and publishes the reviewed draft from the same main commit
  (PR 10). Main 5c5260f: ci and sdk green (external project on Windows, Linux, macOS). sdk-release run 41342119443
  (publish off) drafted sdk-v0.4.0-alpha.1: six packages + four symbol packages, SHA256SUMS verified, packages.mjs
  passed, external project from the draft's files passed (windows, linux, android); the six ids are free on nuget.org,
  third-party dependencies exist there. Nothing pushed. After PR 10 merges, main moves: delete that draft and prepare again.
- L7 (2026-11-01) Owner: "Autorizo la publicación pública de MicroEngine SDK 0.4.0-alpha.1 en NuGet.org." The first
  publish run (41346985998) stopped in preflight: its read-only token could not list draft releases; fixed in PR 11
  (contents: write, a draft from an earlier commit of main may be published). Run 41347458774 published the draft of
  4457312 with Trusted Publishing after the owner's approval: six packages and four symbol packages pushed, GitHub
  release sdk-v0.4.0-alpha.1 public. nuget.org serves all six, signed, with contents identical file by file to the
  draft. External test from nuget.org only (`--public`): windows, linux, android and web (played in Chrome) passed;
  `dotnet add package MicroEngine.Desktop --prerelease` resolves 0.4.0-alpha.1. MicroEngine.Templates reached the
  registration index minutes after the others, which made the first public run fail at `dotnet new install`.
