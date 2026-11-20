# CONSOLIDATION-1: architectural consolidation of the 2D engine

Objective: audit, correct and consolidate the architecture and 2D capabilities of MicroEngine until an external
developer can build real games without rebuilding common engine infrastructure. Umbrella record: each initiative below
has its own issue, branch (`feat/<code>`), pull request and, when it is large, its own record in `odd/tasks/`.
Delivery: one pull request per initiative, merged when CI passes (authorized in L1, sections 3 and 8); no SDK release
without the owner. Test runner: `dotnet test MicroEngine.slnx -c Release`. Audit:
[`docs/evaluations/consolidation-1.md`](../../docs/evaluations/consolidation-1.md).

## Specs

Quoted fragments are the owner's text, verbatim (L1).

- S1 Product: "Ofrecer capacidades listas para utilizar, con valores predeterminados razonables, permitiendo que el
  desarrollador sustituya sus implementaciones cuando lo necesite." "No confundas extensibilidad con trasladar la
  integración al usuario."
- S2 Insufficient reasons: "No aceptes como justificación suficiente: «Ninguna demo lo necesita». «El sistema actual es
  suficientemente rápido». «La implementación nueva requiere infraestructura». «La biblioteca externa se puede usar
  manualmente». «Necesitamos modificar APIs». «Es una funcionalidad avanzada». «El usuario puede implementarla en su
  juego»."
- S3 Audit first: "Antes de lanzar implementaciones independientes, realiza una revisión integral de las capacidades y
  contratos existentes. No te limites a las issues abiertas." "No quiero que resuelvas diez issues independientemente
  si cinco comparten un problema de diseño fundamental. Determina qué cimientos deben corregirse antes de intervenir
  otros subsistemas."
- S4 Mandatory areas: ENTITY-1 (#151, "Revisa el ADR 0059"), CONTENT-1 (#113, "No presupongas que necesitamos KTX ni
  un pipeline propietario"), FSM-1 (#171, "No confundas las reglas de una máquina de estados con la infraestructura
  reutilizable que permite ejecutarlas"), INPUT ("el ratón del sistema de acciones y rebinding"), AUDIO
  ("procesamiento, buses, efectos, ducking, crossfade y audio espacial"), NAVEGACIÓN ("evitación, tamaños de agentes,
  campos de flujo y navegación avanzada"), FÍSICAS ("articulaciones, restricciones"; "Mantén la filosofía de Tiles por
  defecto y Box2D opcional mientras la reevaluación no justifique cambiarla"), GRÁFICOS Y SHADERS ("materiales, render
  targets, stencil, MSAA, mipmaps y posprocesado"; "la ergonomía del sistema de shaders compilados mediante CLI"),
  PERSISTENCIA ("versiones, migraciones, evolución de formatos"), LOCALIZACIÓN, and "OTRAS DECISIONES HISTÓRICAS".
- S5 Method per capability: "A. Necesidad", "B. Experiencia actual", "C. Alternativas", "D. Experiencia objetivo",
  "E. Límite de responsabilidades", "F. Integración", "G. Trade-offs", "H. Decisión", "I. Validación". "No conviertas
  este método en burocracia innecesaria." "Los benchmarks deben responder dudas concretas, no dirigir toda la
  investigación."
- S6 Levels: "Conservar", "Ampliar", "Remodelar", "Reconstruir". "La intervención correcta es la menor que resuelva
  adecuadamente el problema, aunque eso signifique reconstruir un subsistema entero."
- S7 Quality of each intervention: "Cada intervención debe entregar una capacidad completa y coherente, no solamente
  un prototipo técnico." Contracts, implementation, lifecycle integration, errors, resource ownership, unit and
  integration tests, backends, NativeAOT and trimming, platforms, documentation, samples, migration of consumers.
  "Evita introducir rutas alternativas de uso innecesariamente incompatibles con las APIs normales del motor."
  "Cuando una biblioteca externa sea apropiada, intégrala mediante un módulo útil en lugar de reimplementarla sin
  necesidad. No obstante, evita abstracciones que oculten capacidades importantes o se conviertan en fachadas sin
  valor."
- S8 Games validate: "Moderniza las cuatro demos existentes con las APIs actuales." "Revisa los samples para que
  representen buenas prácticas y no soluciones manuales obsoletas." "Elimina infraestructura duplicada que ahora deba
  proporcionar el motor." "No conviertas esta misión en una campaña de mejoras estéticas o contenido nuevo de las
  demos."
- S9 Stop conditions: "Sí debes detenerte ante decisiones que impliquen compromisos externos irreversibles, costes
  económicos adicionales, exposición de secretos o cambios fuera del ámbito autorizado del repositorio." "No fuerces
  soluciones cuando las herramientas o el entorno impidan verificarlas. Documenta claramente lo que quedó sin
  comprobar."
- S10 Done when (L1 section 11): the questioned decisions were re-evaluated; real deficiencies are corrected or
  delimited with a reason; contracts and subsystems are coherent; common capabilities need no repeated integration;
  substitution works where modularity requires it; the developer experience is consistent; examples and reference
  games use the SDK correctly; no known regression is left untreated or undocumented; ADRs match what is implemented;
  the 2D core is a base for the roadmap. "La meta es una consolidación rigurosa y razonable, no perfección infinita."

Assumption (mine, not the owner's): "integrando" in L1 section 12 covers merging a pull request of this mission into
`main` once the **CI result** check passes. An SDK release to nuget.org stays with the owner (AGENTS.md).

## Tasks

Route is inline unless a line says otherwise. Status: `[ ]` pending, `[~]` in progress, `[x]` done with its evidence.

| ID | Initiative | Specs | Issue | Depends on | Status |
| --- | --- | --- | --- | --- | --- |
| T1 | Cross-cutting audit and diagnosis | S3 | — | — | [x] `docs/evaluations/consolidation-1.md` |
| T2 | MODULE-1: scene systems and module lifecycle | S3, S7 | #173 | — | [x] merged, PR #185; ADR 0060 |
| T3 | RENDER-1: one texture type, state and material on the draw path, graphics resources as assets | S3, S4, S7 | #174 | — | [~] PR #188 in CI; ADR 0064 |
| T4 | FSM-1: state machines | S4, S5 | #171 | T2 | [x] merged, PR #186; ADR 0061 |
| T5 | INPUT-2: pointer in actions and rebinding | S4 | #175 | — | [x] merged, PR #187; ADR 0062; gestures left as their own unit |
| T6 | SAVE-2: versions and migrations | S4 | #176 | — | [~] delegated; correction batch after an independent verification; ADR 0063 |
| T7 | AUDIO-2: submix, effects, ramps, position, decoders, listener | S4 | #177 | — | [ ] |
| T8 | CONTENT-1: content production | S4, S5 | #113 | T3 | [ ] |
| T9 | ENTITY-1: optional ECS module | S4, S5 | #151 | T2, T4 | [~] delegated; ADR 0065 |
| T10 | PHYS-2: dynamics, joints, queries, character controller | S4 | #173 | T2 | [~] PR pending; ADR 0060; `tests/MicroEngine.Tests/SceneSystemTests.cs` 16/16 |
| T11 | NAV-2: following, steering, clearance, reachability | S4 | #179 | — | [ ] |
| T12 | LOC-1: localization | S4 | #180 | T8 | [ ] |
| T13 | GRAPHICS-3: post-processing, present stage, mipmaps | S4 | #174 | T3 | [ ] |
| T14 | DEMO-MODERN-1: demos and samples on the current SDK | S8 | #173 | T2 to T13 | [~] PR pending; ADR 0060; `tests/MicroEngine.Tests/SceneSystemTests.cs` 16/16 |
| T15 | Closure: ADRs, limitations page, done-when review | S10 | — | all | [ ] |

## Log

- L1 (2026-11-19, owner, verbatim): the mission text "Misión: Consolidación arquitectónica integral de MicroEngine 2D"
  is kept in full in [`consolidation-1/mission.md`](consolidation-1/mission.md).
- L2 (2026-11-19): audit done by seven read-only explorations (content; entities and state patterns; past decisions;
  input and audio; physics and navigation; graphics and text; kernel, saves, localization and tools). Two parent spot
  checks: `docs/manual/input.md:794` and `src/MicroEngine.Content/Loading/AssetLoaders.cs:140` say what the reports
  quote. Nothing was measured.
- L3 (2026-11-19): diagnosis. Five structural causes explain almost every deficiency: modules cannot live past
  registration; two graphics type families; the content seam stops at loading; closed representations in input, audio
  and physics; the demos do not use the engine's contracts. Order: MODULE-1 and RENDER-1 first.
- L4 (2026-11-19): decided against a blanket interface over every façade service (audit 2.6): the seams that matter
  exist below the façades; each capability adds the seam it needs.
- L5 (2026-11-19): T2 MODULE-1 implemented inline on `feat/module-1`: `SceneSystem`, `Scene.Systems`, `Scene.Timers`, `SceneManager.AddSystem`, `Entered` and `Exited`, `Lifecycle.Started` and `Stopping`, `EngineBuilder.StateSections` (achievements moved onto it, same section name and content). The built-in coroutines, tweens, world and widgets were left as fixed members: making them list entries changes nothing a developer can do and puts the step order behind a mutable list (ADR 0060). Checks: `dotnet test MicroEngine.slnx -c Release` 15 projects, 0 failed except `TheTourOpensEveryExampleOfTheList`, which read the example list while I was editing it during the run; `tests/Samples.Tests` rerun alone 76/76. Not run: the site build, a browser build. Physics per scene through this mechanism is left to T10.
- L6 (2026-11-19): T4 FSM-1 implemented inline on `feat/fsm-1` (stacked on `feat/module-1`). Decision: a machine of the engine in the kernel (`StateMachine<TState, TEvent>`), no adapter package; Stateless documented for event workflows. Evidence: `benchmarks/FsmCompare` writes one enemy by hand, on Stateless 5.20.1 and on the engine machine, equal at each of 2,400 steps (`FsmCompareTests`); on Stateless the `switch` stays and a change of state allocates (77 bytes per step on average, one run, one machine). Checks: `dotnet test MicroEngine.slnx -c Release` 15 projects, 0 failed, no abort; `StateMachineTests` 16/16; the tour replayed and its two new captures looked at (a heading that overlapped and a culture-formatted number were fixed). Not run: site build, browser, NativeAOT publish. The demos keep their hand-written machines until T14; reading `games/CourtyardKey/Entities/Boss.cs:159-160` shows a state entered with time already spent, which wants a duration of its own.
- L7 (2026-11-19): routes. T5, T6, T3 and T9 are delegated, one writer each in its own worktree beside the repository (`feat/input-2`, `feat/save-2`, `feat/render-1`, `feat/entity-1b`), for the context backstop: the parent session carried the audit and two implementations. Each writer commits locally; the parent reads the report, repeats checks after integrating `main`, pushes and opens the pull request.
- L8 (2026-11-19): T5 INPUT-2 merged (PR #187). One list of `InputControl` per action replaces the five lists; mouse buttons, wheel and pointer motion in actions, listening, saved format 2 (format 1 still loads) and prompts; pointer edges and delta; `ICursorControl`; a radial dead zone and direction bindings for the right stick. A map without pointer controls hashes byte for byte as before (`BindingCompatibilityTests`). A defect found on the way: a non-primary press taken by a layer captured the whole pointer until the next primary release. The prompts change was outside the writer's edit surface; the parent applied its prepared patch. Gestures were evaluated and left out: their thresholds need a real touch screen. CI: `Browser engine MeadowRun` failed once on Firefox (`menus.script by keyboard: reaches PauseScene (TitleScene; keys up to 2 updates late)`) and passed when rerun; Chrome and the other three games passed both times. Treated as intermittent; if it recurs it is looked at as a timing regression of the input change. Not verified: a real mouse on a visible window, raylib's side buttons, a browser's pointer lock, other platforms.
- L9 (2026-11-19): T3 RENDER-1 in PR #188. `Texture` exposes its device texture; `IRenderer.PushState` and `PushTarget`; `Assets.Shader`, `Material` and `RenderTarget`; `.shader` format 2 with parameter names (format 1 still loads); a material on a device without shaders draws its declared fallback; `Legacy` renamed `Drawing`. Tour captures before and after: 32 of 34 byte-identical on `sdl-renderer` and `sdl-gpu`; `28-light.png` differs by 1/255 in 990 pixels (8-bit colours on `IRenderer`); `08-saving.png` differs on every run, before the change too, cause not looked at. Accepted from the writer: the four new `IRenderer` members have default implementations that throw, so a renderer double outside the engine keeps compiling. Parent checks after merging `main`: build 0 warnings, `MicroEngine.Tests` 1065 passed, `Samples.Tests` 79, the tour on `sdl-gpu` with the water and post-processing captures looked at. Left to T13: a pass over every scene's finished frame.
- L10 (2026-11-19): T6 SAVE-2 changes a persisted format (high-risk item 1), so an independent verifier probed it before any pull request. Verdict: the demos' existing saves load, in both directions between old and new code; one blocker (the plain `Save` and `Load` overloads ignore versions: a plain save replaces a newer save, and one plain save on a versioned slot makes the next load run every migration again) and four defects (protection never lifted by a later good load; a polymorphic root unreadable by the plain load; a schema over a non-object root sets a valid save aside; `"$version": null` read as version 1), plus notes (a backup overwritten silently, backups unreachable through the store, the samples' save folders changed). One correction batch was sent to the writer; a recheck limited to those items follows.
