# CI-WASM: evidence for macOS, demo packages only for releases, built on Linux, keyed by the project graph

Objective: remove redundant CI work while keeping coverage: macOS joins the evidence mechanism; PRs and pushes to `main` never build release WebAssembly packages (the browser engine suites keep playing all four games from development packages); releases build them on Linux, only for games whose relevant inputs changed. Branch `feat/ci-evidence-wasm`.
Delivery: `single-pr`. Runners: `node --test .github/scripts/*.test.mjs`, `npm test` in `site/`, actionlint.

## Specs

- S1 macOS evidence: "Integra sus pruebas informativas en el mismo mecanismo de evidencia utilizado por las demás plataformas, con una identidad específica de macOS y las dependencias correspondientes." "Las validaciones fallidas no deben generar evidencia reutilizable." "Conserva su carácter informativo".
- S2 "Elimina las compilaciones WebAssembly de distribución de los workflows de PR y push a `main`." "Las suites de `browser-engine` deben seguir compilando y ejecutando funcionalmente los cuatro juegos en modo desarrollo". "No implementar la propuesta de un único juego canario".
- S3 "Evalúa y migra este proceso a runners Linux". "Verifica que los artefactos resultantes sean equivalentes funcionalmente y compatibles con los mecanismos actuales de publicación, ejecución y distribución."
- S4 Per-demo cache identity from "los proyectos, dependencias transitivas, recursos, herramientas y configuraciones que realmente pueden afectar su compilación WebAssembly", using "el grafo de dependencias y las capacidades existentes de .NET/MSBuild"; "Cambios exclusivos de un juego no invaliden los demás"; "Cambios relevantes de compilador, configuración, recursos o dependencias nunca permitan reutilizar un artefacto obsoleto."
- S5 Store (after PR #128): "conservar únicamente los artefactos vigentes"; in a release each demo is reused when its key proves it valid, rebuilt when its inputs changed or it is missing, and replaces the earlier package; "La reutilización nunca debe ocultar errores de almacenamiento ni permitir publicar paquetes desactualizados."
- S6 Tests and documentation updated; PR with summary, checks, residual risks and measured performance where verifiable.

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 macOS evidence (`evidence.mjs`, ci.yml, checks.yml) | S1 | inline | done | evidence tests 9/9 (2 new) |
| T2 No release packages in ci.yml and validate.yml; freeze.mjs without `--build` | S2 | inline | done | freeze RED (1 failing) then GREEN 7/7 |
| T3 Release web packages built on Linux (checks.yml `build-demos`) | S3 | inline | partial | Linux toolchain set up in Docker; package build interrupted (L4); pending a release or validation run |
| T4 Demo keys from the project graph (`demos.mjs` `webInputs`) | S4 | inline | done | 3 new key tests RED then GREEN; real keys drop Graphics3D and Physics.Box2D (L3) |
| T5 Store: an orphan archive is replaced, never reused | S5 | inline | done | orphan test RED (SHA mismatch) then GREEN |
| T6 Docs (ci.md, deployment.md, ADR 0049) and PR | S6 | inline | done | planner 96/96, site 67/67, actionlint 1.7.7 clean |

## Log

- L1 (2026-11-06) Original request: the mission "optimizar el CI/CD de MicroEngine eliminando trabajo redundante" (five decisions: macOS evidence, separate demo validation from packaging, Linux release builds, per-demo cache granularity, bounded store), quoted in the specs above.
- L2 Mapping: games reference `MicroEngine.Desktop` (SDL, raylib and the engine); the web host trims only the raylib bindings, so `MicroEngine.Desktop.dll` ships in every web package and stays an input (confirmed in release v0.4.0's MeadowRun
  manifest: `MicroEngine.Desktop`, `MicroEngine.Raylib`, `MicroEngine.Sdl`, `Raylib-cs`, `SDL3-CS` assemblies).
- L3 Keys on the real repository: inputs per demo 295→272 (MeadowRun), 306→283, 355→332, 362→339; the 23 files dropped
  are `MicroEngine.Graphics3D` and `MicroEngine.Physics.Box2D`. `plan --policy none|all` against the real store
  (read-only): every v2 key missing, so the next release rebuilds the four packages on Linux.
- L4 Local Linux check (Docker `ubuntu:24.04`, repository copy): `web/setup.sh` succeeded on Linux (the .NET workload,
  Emscripten `3.1.56` linux-x64 and SDL `3.5.0` built from source as `SDL3.a`, 1 min 54 s); `web/build.sh MeadowRun`
  started and was cut off by a Docker Desktop engine crash (`error waiting for container: unexpected EOF`, exit 125), not a
  build error. The package itself (file list against release v0.4.0's 46-file MeadowRun manifest) is not verified here;
  the first release, or `validate.yml` with web packages, builds all four on `ubuntu-latest`.
- L5 Measured on the last 60 ci runs (2026-11-05..06, partly during the full-store defect fixed by #128): 218 Windows
  WebAssembly release builds (avg 3.7 min, 817 runner-minutes; 146 on pull requests, 72 on pushes to main), sdl-web
  56 jobs; macOS informative tests 14 jobs (avg 2.3 min), 6 of them on pushes to main. Outside every game's graph: `MicroEngine.Graphics3D`, `MicroEngine.Physics.Box2D`, `packaging/`, `platforms/`, `sdk/`, `tests/`, `samples/`, `templates/`, `benchmarks/`, the Android, iOS and phone hosts. `--dev` only flips a constant and ships input scripts (`Hosts.cs`), so development packages exercise the same toolchain as release ones.
