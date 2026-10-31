# INFRA-1: CI/CD cost optimisation and manual releases

Objective: cheap, reliable daily CI; full validation on demand; publishing only by an explicit manual release.
Branch `feat/ci-cost` (from `origin/main` b822702, after PLATFORM-2 #13). No push, PR or merge without the owner.
Test runners: `node --test .github/scripts/*.test.mjs` (planner, evidence, cost), `npm test --prefix site` (demo store,
releases), `actionlint` (workflows). Delivery strategy: ask-on-risk; work-unit commits on this branch only.

## Specs

- S1 Audit before changing workflows: "Todos los workflows existentes y sus triggers." "Qué ejecuta actualmente un PR."
  "Qué vuelve a ejecutarse al fusionar en `main`." "Qué dispara builds WebAssembly." "Qué reconstruye o publica la
  landing." "Cómo funcionan las claves de contenido y artefactos reutilizables." "Qué jobs son obligatorios para
  proteger `main`." "Qué pruebas se realizan en navegador." "Qué coste generan Windows, Linux, macOS y los
  emuladores." "Qué dependencias existen entre las pruebas y el despliegue." "Usa los tiempos registrados de GitHub
  Actions cuando estén disponibles. Distingue rigurosamente entre mediciones reales y estimaciones."
- S2 Daily CI (PR and main): "Ejecutar tests .NET pertinentes." "Detectar errores de compilación." "Validar los
  componentes afectados por los cambios." "Ejecutar checks de documentación y sitio cuando corresponda." "Evitar
  compilaciones de demos que no aporten evidencia nueva." "Evitar repetir pruebas costosas que ya pasaron para el
  mismo contenido." "No ejecutar automáticamente emuladores Android ni simuladores iOS." "No realizar releases de
  juegos." "No publicar paquetes de SDK." Evaluate Windows, macOS and Linux graphics jobs per PR; "No elimines
  cobertura sin justificar el riesgo."
- S3 Full validation: manual `workflow_dispatch` for a commit or ref: multi-platform .NET tests, native rendering where
  infrastructure exists, browser tests, the four demo builds, graphics regression, Android Emulator, iOS Simulator,
  experimental macOS. "Los fallos deben identificarse por plataforma." "La compatibilidad experimental no debe
  confundirse con soporte oficial." "Conserva la capacidad de ejecutar plataformas por separado si eso reduce costes."
- S4 Manual release: 1. version and source ref; 2. validate the selected state; 3. reuse approved artifacts when safe;
  4. build only missing or invalidated artifacts; 5. "Verificar integridad mediante hashes."; 6. "Ejecutar las pruebas
  necesarias sobre los paquetes que realmente se publicarán."; 7. publish the demos and update the site; 8. record the
  version and the published artifacts. "No crear releases ni tags automáticamente por un push o merge." "No
  introducir todavía publicación NuGet". "Una release fallida no debe dejar la versión pública en un estado
  inconsistente."
- S5 Site decoupled from game builds: "Cambio únicamente de documentación: validar el sitio y desplegarlo sin
  reconstruir demos." "Cambio visual de la landing: validar el sitio y reutilizar demos aprobadas." "Cambio del motor:
  validar código, pero no publicar automáticamente nuevas demos." "Cambio de una demo: validar lo necesario sin
  publicar automáticamente una versión pública." "Release manual: publicar los paquetes nuevos y actualizar las
  referencias pertinentes." Keep content keys; study integration vs approved artifacts; "No reutilices un paquete con
  una clave que no coincida con sus dependencias reales."
- S6 Redundancy: concurrency, changed paths, NuGet/npm/toolchain caches, WebAssembly reuse, artifacts between jobs,
  skipping unchanged jobs, "Evitar tests de navegador repetidos sobre paquetes idénticos.", build once when viable,
  "Reducir trabajo duplicado entre PR y `main`." "Conserva un comportamiento conservador ante rutas desconocidas o
  cambios en infraestructura compartida." "No utilices únicamente el nombre de un archivo como garantía de que sus
  dependencias no cambiaron."
- S7 Protection: required checks keep meaning real validation; no branch-protection problems from skipped jobs; "No
  permitas publicar si un check necesario falló, fue cancelado o no se ejecutó." Distinguish required, informative,
  legitimately skipped, experimental. "No desactives verificaciones de seguridad, integridad ni calidad solamente para
  ahorrar minutos."
- S8 `platforms.yml` frequency: decide weekly vs other vs manual from observed cost; apply and document.
- S9 Measurement: per change class: total duration, minutes per runner, estimated cost at the right rate, jobs run,
  jobs avoided, difference from the previous pipeline. "No confundas duración de pared con consumo facturable." "No
  inventes precios ni métricas". Document GitHub Billing usage, budgets and alerts.
- S10 Self-hosted runners: research only (Windows, Linux via Docker/VM, Android emulators): advantages, risks, security
  requirements. "No instales ni registres todavía un self-hosted runner." Apple stays on GitHub macOS runners.
- S11 Acceptance: docs-only PR; game-test PR; demo PR; engine-core PR; CI-infra PR; merge to `main` without new demo
  packages; manual full matrix; manual release; failure during release validation; correct reuse of published
  artifacts. Prefer planner tests and static checks; minimise real GitHub runs and say which are indispensable.
- S12 Constraints: repository stays private; no Azure DevOps; no paid services; no self-hosted runners; no gameplay,
  public API, rendering, physics or input changes; release integrity; keep Windows, Linux, macOS, Android, iOS and
  WebAssembly validation possible; do not touch unrelated local changes (`site/.gitignore`).
- S13 Deliverables: optimised workflows, daily CI, manual full validation, manual release, matrix per change type,
  cost before/after, planner tests, internal operations and publishing docs, risks, recommendation on GitHub Actions
  vs own runners.

## Tasks

| ID | Specs | Route | Status | Commit |
| --- | --- | --- | --- | --- |
| T1 | S1, S9 | inline | done: audit from runs of 2026-10-31 (Log L2) | — |
| T2 | S2, S6, S7 | inline | pending: planner checks (windows split, site, advisory macOS/mobile) | |
| T3 | S2, S6 | inline | pending: evidence keys and lookup (`evidence.mjs`) | |
| T4 | S4, S5 | inline | pending: demo releases, candidate/release sources (`demos.mjs`) | |
| T5 | S2–S8 | inline | pending: workflows (ci, checks, deploy-site, validate, release, platforms) | |
| T6 | S9 | inline | pending: `ci-cost.mjs` measure and estimate | |
| T7 | S1, S9–S13 | inline | pending: ADR 0013, evaluation, deployment and operations docs | |

## Log

- L1 (2026-10-31) User request: "MicroEngine — INFRA-1: Optimización de CI/CD, costes y releases manuales" (full text in
  the session; specs above quote it). "Trabaja en una rama independiente, con commits organizados. No hagas push ni
  merge sin autorización." "Criterio principal de éxito: poder desarrollar MicroEngine diariamente con un CI económico
  y fiable, reservando las compilaciones y validaciones completas para cuando realmente aporten valor. La publicación
  de una nueva versión debe ser una decisión explícita, no un efecto secundario de fusionar código."
- L2 Audit facts (measured from the GitHub API, jobs of 27 runs since 2026-10-31): $5.33 billable at list rates
  (platforms $2.57, pipeline Windows 143 min $1.43, Linux 117 min, macOS 10 min $0.62). Branch protection and
  rulesets return HTTP 403 (GitHub Free, private repository): no check can be enforced by GitHub, so gates must live
  in the workflows. The merge of #13 (run 41218921372) re-ran every check the pull request had passed. Rates verified
  at docs.github.com (Linux $0.006, Windows $0.010, macOS $0.062 per minute, each job rounded up to a whole minute;
  Free includes $12 per month). `vercel deploy --prod --skip-domain` and `vercel promote` exist (vercel.com docs).
- L3 Decisions: evidence of passed checks keyed by the content each check reads (artifacts, `actions: read` lookup);
  demo releases are GitHub releases `v<version>` holding copies of the approved archives and `release.json`; the
  latest published release is what production serves; candidates (integration store) only in CI and previews;
  engine-wide changes build one canary game in daily CI, every game in validation and releases; macOS leaves daily
  CI; Windows .NET tests only for engine, backend and build changes; `platforms.yml` monthly plus manual.
